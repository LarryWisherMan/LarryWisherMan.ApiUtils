param
(
    [Parameter()]
    [System.String]
    $ProjectName = (property ProjectName ''),

    [Parameter()]
    [System.String]
    $SourcePath = (property SourcePath ''),

    [Parameter()]
    [System.String]
    $OutputDirectory = (property OutputDirectory (Join-Path $BuildRoot 'output')),

    [Parameter()]
    [System.String]
    $BuiltModuleSubdirectory = (property BuiltModuleSubdirectory ''),

    [Parameter()]
    [System.Management.Automation.SwitchParameter]
    $VersionedOutputDirectory = (property VersionedOutputDirectory $true),

    [Parameter()]
    [System.String]
    $BuildModuleOutput = (property BuildModuleOutput (Join-Path $OutputDirectory $BuiltModuleSubdirectory)),

    [Parameter()]
    [System.String]
    $ModuleVersion = (property ModuleVersion ''),

    [Parameter()]
    [System.String]
    $Configuration = (property Configuration 'Release'),

    [Parameter()]
    [System.String]
    $DotNetVerbosity = (property DotNetVerbosity 'minimal'),

    [Parameter()]
    [System.Collections.Hashtable]
    $BuildInfo = (property BuildInfo @{ })
)

# Task: Validate .NET SDK availability
task Validate_DotNet_SDK {
    if (-not (Get-Command 'dotnet' -ErrorAction SilentlyContinue)) {
        throw '.NET SDK not found. Please install .NET SDK 6.0 or later.'
    }
    Write-Build Green ".NET SDK found"
}

# Task: Initialize build variables and paths
task Initialize_Build_Variables {
    . Set-SamplerTaskVariable -AsNewBuild

    Write-Build DarkGray "Initialized build variables"
    Write-Build DarkGray "  Project: $ProjectName"
    Write-Build DarkGray "  Source: $SourcePath"
    Write-Build DarkGray "  Configuration: $Configuration"
}

# Task: Get version information from GitVersion
task Get_Version_Information {
    . Set-SamplerTaskVariable -AsNewBuild
    $DotVersion = dotnet gitversion | ConvertFrom-Json
    $VersionParts = ($ModuleVersion -split "-")
    $version = $VersionParts[0]
    $PreRelease = $VersionParts[1]

    # Store in build info for other tasks to use
    $BuildInfo.DotVersion = $DotVersion
    $BuildInfo.Version = $version
    $BuildInfo.PreRelease = $PreRelease
    $BuildInfo.VersionedOutPutFolder = Join-Path -Path (Join-Path -Path $BuildModuleOutput -ChildPath $ProjectName) -ChildPath $version

    Write-Build Green "Version: $version"
    if ($PreRelease) {
        Write-Build Yellow "Pre-release: $PreRelease"
    }
}

# Task: Create private data for module manifest
task Create_Private_Data Get_Version_Information, {
    $PrivateData = @{
        Prerelease           = $BuildInfo.PreRelease
        GitSha               = $BuildInfo.DotVersion.Sha
        BuildDate            = (Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')
        InformationalVersion = $BuildInfo.DotVersion.InformationalVersion
    }

    $BuildInfo.PrivateData = $PrivateData

    Write-Build DarkGray "Private data created with GitSha: $($BuildInfo.DotVersion.Sha)"
}

# Task: Restore NuGet packages
task Restore_NuGet_Packages Validate_DotNet_SDK, {
    . Set-SamplerTaskVariable -AsNewBuild
    $CSPROJPath = (Join-Path -Path $SourcePath -ChildPath "$ProjectName.csproj")

    Write-Build DarkGray "Restoring NuGet packages..."
    exec { dotnet restore $CSPROJPath }
}

# Task: Build .NET assembly
task Build_DotNet_Project Restore_NuGet_Packages, {
    . Set-SamplerTaskVariable -AsNewBuild
    $CSPROJPath = (Join-Path -Path $SourcePath -ChildPath "$ProjectName.csproj")
    $dotNetProject = $BuildInfo.DotNetConfig.Project
    $configuration = $BuildInfo.DotNetConfig.Configuration

    Write-Build Green "Building .NET assembly: $dotNetProject"
    Write-Build DarkGray "Building assembly..."

    $buildArgs = @(
        'build'
        $CSPROJPath
        '--configuration', $configuration
        '--no-restore'
        '--verbosity', $DotNetVerbosity
    )

    # Add GitVersion properties if available
    if ($env:GITVERSION_SEMVER) {
        $buildArgs += '-p:Version=' + $env:GITVERSION_SEMVER
        $buildArgs += '-p:AssemblyVersion=' + $env:GITVERSION_ASSEMBLYSEMVER
        $buildArgs += '-p:FileVersion=' + $env:GITVERSION_ASSEMBLYSEMVER
        $buildArgs += '-p:InformationalVersion=' + $env:GITVERSION_INFORMATIONALVERSION

        Write-Build Cyan "Using GitVersion properties"
    }

    exec { dotnet @buildArgs }
}

# Task: Locate build artifacts
task Locate_Build_Artifacts Build_DotNet_Project, {
    . Set-SamplerTaskVariable -AsNewBuild
    # Build the bin path - need to check multiple possible locations
    $possibleBinPaths = @(
        (Join-Path $SourcePath "bin\$Configuration"),
        (Join-Path $SourcePath "bin\$Configuration\net6.0"),
        (Join-Path $SourcePath "bin\$Configuration\net7.0"),
        (Join-Path $SourcePath "bin\$Configuration\net8.0"),
        (Join-Path $SourcePath "bin\$Configuration\netstandard2.0"),
        (Join-Path $SourcePath "bin\$Configuration\netstandard2.1")
    )

    $BinPath = $null
    $SourcePSD1 = $null
    $dllItem = $null

    foreach ($path in $possibleBinPaths) {
        if (Test-Path $path) {
            Write-Build DarkGray "Checking path: $path"
            $tempPSD1 = Get-ChildItem $path -Filter "*.psd1" -Recurse -ErrorAction SilentlyContinue
            $tempDLL = Get-ChildItem $path -Filter "$ProjectName.dll" -Recurse -ErrorAction SilentlyContinue

            if ($tempPSD1 -and $tempDLL) {
                $BinPath = $path
                $SourcePSD1 = $tempPSD1
                $dllItem = $tempDLL
                break
            }
        }
    }

    if (-not $SourcePSD1) {
        Write-Build Yellow "Available paths:"
        Get-ChildItem (Join-Path $SourcePath "bin") -Recurse | ForEach-Object { Write-Build Yellow "  $($_.FullName)" }
        throw "Could not find module manifest (.psd1) in any of the expected locations"
    }

    if (-not $dllItem) {
        Write-Build Yellow "Available DLLs:"
        Get-ChildItem (Join-Path $SourcePath "bin") -Filter "*.dll" -Recurse | ForEach-Object { Write-Build Yellow "  $($_.FullName)" }
        throw "Could not find compiled assembly ($ProjectName.dll) in any of the expected locations"
    }

    # Store results in BuildInfo for other tasks
    $BuildInfo.BinPath = $BinPath
    $BuildInfo.SourcePSD1 = $SourcePSD1
    $BuildInfo.DllItem = $dllItem

    Write-Build Green "Found artifacts:"
    Write-Build Green "  Manifest: $($SourcePSD1.FullName)"
    Write-Build Green "  Assembly: $($dllItem.FullName)"
}

# Task: Build final module
task Build_Final_Module Locate_Build_Artifacts, Get_Version_Information, {
    Build-Module -SourcePath $BuildInfo.SourcePSD1 -OutputDirectory $BuildModuleOutput -VersionedOutputDirectory -SemVer $BuildInfo.DotVersion.InformationalVersion -CopyPaths $BuildInfo.DllItem

    Write-Build Green "Module built successfully"
    Write-Build Blue "Source Path: $SourcePath"
}

# Main task that orchestrates the entire build
task Build_DotNet_Assembly Initialize_Build_Variables, Get_Version_Information, Create_Private_Data, Build_Final_Module

# Alternative task for just building the .NET project without module packaging
task Build_DotNet_Only Validate_DotNet_SDK, Initialize_Build_Variables, Get_Version_Information, Build_DotNet_Project

# Task for cleaning build output
task Clean_Build_Output {
    . Set-SamplerTaskVariable
    $pathsToClean = @(
        $BuildModuleOutput,
        (Join-Path $SourcePath "bin"),
        (Join-Path $SourcePath "obj")
    )

    foreach ($path in $pathsToClean) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force
            Write-Build Yellow "Cleaned: $path"
        }
    }
}

# Task for full clean and rebuild
task Rebuild Clean_Build_Output, Build_DotNet_Assembly
