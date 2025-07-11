param
(
    [Parameter()]
    [System.String]$ProjectName = (property ProjectName $buildInfo.ProjectName),

    [Parameter()]
    [System.String]$SourcePath = (property SourcePath (Get-SamplerAbsolutePath -Path src)),

    [Parameter()]
    [System.String]
    $OutputDirectory = (property OutputDirectory (Join-Path $BuildRoot 'output')),

    [Parameter()]
    [System.String]
    $BuiltModuleSubdirectory = (property BuiltModuleSubdirectory $BuildInfo.BinaryModuleBuildSettings.BuiltModuleSubdirectory),

    [Parameter()]
    [System.String]
    $PrimaryProject = (property PrimaryProject $BuildInfo.BinaryModuleBuildSettings.PrimaryProject),

    [Parameter()]
    [string]$ProjectRoot = (property ProjectRoot (Join-Path $SourcePath $PrimaryProject)),

    [Parameter()]
    [string[]]
    $TargetFrameworks = (property TargetFrameworks $BuildInfo.BinaryModuleBuildSettings.TargetFrameworks),

    [Parameter()]
    [System.String]
    $Configuration = (property Configuration $BuildInfo.BinaryModuleBuildSettings.Configuration),

    [Parameter()]
    [string]$ModuleStagingSubdirectory = (property ModuleStagingSubdirectory $BuildInfo.BinaryModuleBuildSettings.ModuleStagingSubdirectory),


    [Parameter()]
    [System.String]
    $DotNetVerbosity = (property DotNetVerbosity $BuildInfo.BinaryModuleBuildSettings.DotNetVerbosity),

    [Parameter()]
    [bool]$IncludePdb = (property IncludePdb $BuildInfo.BinaryModuleBuildSettings.CopyOptions.IncludePdb),

    [Parameter()]
    [bool]$IncludeDepsJson = (property IncludeDepsJson $BuildInfo.BinaryModuleBuildSettings.CopyOptions.IncludeDepsJson)
)

function FormatPSD1
{

    # Ensure module is imported
    Import-Module PSScriptAnalyzer -Force

    $tempPath = "$psd1Path.tmp.ps1"

    # Copy to .ps1 so formatter treats it as script
    Copy-Item $psd1Path $tempPath -Force

    # Read the text
    # Normalize line endings to LF or CRLF (choose one, here we use CRLF)
    $text = Get-Content $psd1Path -Raw -Encoding UTF8 -ErrorAction Stop
    $text = $text -replace "`r?`n", "`r`n"  # Normalize to CRLF


    $settings = @{
        includeDefaultRules = $true
        IncludeRules        = @(
            # DSC Resource Kit style guideline rules.
            'PSAvoidDefaultValueForMandatoryParameter',
            'PSAvoidDefaultValueSwitchParameter',
            'PSAvoidInvokingEmptyMembers',
            'PSAvoidNullOrEmptyHelpMessageAttribute',
            'PSAvoidUsingCmdletAliases',
            'PSAvoidUsingComputerNameHardcoded',
            'PSAvoidUsingDeprecatedManifestFields',
            'PSAvoidUsingEmptyCatchBlock',
            'PSAvoidUsingInvokeExpression',
            'PSAvoidUsingPositionalParameters',
            'PSAvoidShouldContinueWithoutForce',
            'PSAvoidUsingWMICmdlet',
            'PSAvoidUsingWriteHost',
            'PSDSCReturnCorrectTypesForDSCFunctions',
            'PSDSCStandardDSCFunctionsInResource',
            'PSDSCUseIdenticalMandatoryParametersForDSC',
            'PSDSCUseIdenticalParametersForDSC',
            'PSMisleadingBacktick',
            'PSMissingModuleManifestField',
            'PSPossibleIncorrectComparisonWithNull',
            'PSProvideCommentHelp',
            'PSReservedCmdletChar',
            'PSReservedParams',
            'PSUseApprovedVerbs',
            'PSUseCmdletCorrectly',
            'PSUseOutputTypeCorrectly',
            'PSAvoidGlobalVars',
            'PSAvoidUsingConvertToSecureStringWithPlainText',
            'PSAvoidUsingPlainTextForPassword',
            'PSAvoidUsingUsernameAndPasswordParams',
            'PSDSCUseVerboseMessageInDSCResource',
            'PSShouldProcess',
            'PSUseDeclaredVarsMoreThanAssignments',
            'PSUsePSCredentialType',

            'Measure-*'
        )
    }


    # Now it's safe to format
    $formatted = Invoke-Formatter -ScriptDefinition $text -Settings $settings
    Set-Content -Path $psd1Path -Value $formatted -Encoding UTF8


    # Format the script contents
    $formatted = Invoke-Formatter -ScriptDefinition $text

    # Save the result back
    Set-Content -Path $psd1Path -Value $formatted -Encoding UTF8

    # Clean up temporary file
    Remove-Item $tempPath
}


task GetBuildInfo {
    . Set-SamplerTaskVariable -AsNewBuild

    $global:BuildInfoCopy = $buildInfo
    $global:ParamsCopy = Get-Variable -Scope Script

    $BuildInfoCopy.ProjectRoot = $projectRoot
}


# ─────────────────────────────────────────────────────────────────────────────
# GitVersion Resolve
# ─────────────────────────────────────────────────────────────────────────────
task Resolve_Module_Version {
    . Set-SamplerTaskVariable -AsNewBuild

    $global:GitVersion = & DotNet gitversion /output json | ConvertFrom-Json
    $global:ResolvedVersion = $GitVersion.MajorMinorPatch
    $global:ResolvedSemVer = $GitVersion.NuGetVersionV2
    $global:ResolvedPrerelease = $GitVersion.NuGetPreReleaseTagV2
    $global:OutputPath = (Join-Path "$BuiltModuleSubdirectory\$ProjectName" $global:ResolvedVersion)
}

# ─────────────────────────────────────────────────────────────────────────────
# Build Solution
# ─────────────────────────────────────────────────────────────────────────────
task Build_Solution Resolve_Module_Version, {
    . Set-SamplerTaskVariable -AsNewBuild
    $SolutionPath = Join-Path $BuildRoot "$ProjectName.sln"
    Start-Process dotnet -ArgumentList @(
        'build', $SolutionPath,
        '-c', $Configuration,
        '-v', $DotNetVerbosity,
        "-p:Version=$($global:ResolvedSemVer)",
        "-p:AssemblyVersion=$($global:ResolvedVersion).0",
        "-p:FileVersion=$($global:ResolvedVersion).0",
        "-p:InformationalVersion=$($global:GitVersion.InformationalVersion)"
    ) -NoNewWindow -Wait

}

task DotNet_Clean {
    . Set-SamplerTaskVariable -AsNewBuild

    $solutionPath = Join-Path $BuildRoot "$ProjectName.sln"

    if (-not (Test-Path $solutionPath))
    {
        throw "Solution file not found at: $solutionPath"
    }

    Write-Host "[clean] Running dotnet clean on: $solutionPath" -ForegroundColor DarkGray
    dotnet clean $solutionPath

    Write-Host "[Restore] Running dotnet clean on: $solutionPath" -ForegroundColor DarkGray
    dotnet restore
}


# ─────────────────────────────────────────────────────────────────────────────
# Stage DLLs by TFM
# ─────────────────────────────────────────────────────────────────────────────

task Stage_DotNet_Output Clean_Staging_Root, Build_Solution, {
    . Set-SamplerTaskVariable -AsNewBuild

    $stagingRoot = Join-Path $ProjectRoot $ModuleStagingSubdirectory

    $libRoot = Join-Path $stagingRoot 'lib'
    $binBase = Join-Path $projectRoot 'bin' $Configuration

    # Clean existing staging
    if (Test-Path $stagingRoot)
    {
        Remove-Item $stagingRoot -Recurse -Force
    }

    foreach ($tfm in $TargetFrameworks)
    {
        $src = Join-Path $binBase $tfm
        $dst = Join-Path $libRoot $tfm

        if (-not (Test-Path $src))
        {
            Write-Warning "Missing TFM output at: $src"
            continue
        }

        $null = New-Item -ItemType Directory -Path $dst -Force

        Copy-Item "$src/*.dll" -Destination $dst -Force -ErrorAction SilentlyContinue
        if ($IncludePdb)
        {
            Copy-Item "$src/*.pdb" -Destination $dst -Force -ErrorAction SilentlyContinue
        }
        if ($IncludeDepsJson)
        {
            Copy-Item "$src/*.deps.json" -Destination $dst -Force -ErrorAction SilentlyContinue
        }

        Write-Host "[stage] $tfm → $dst" -ForegroundColor Gray
    }


    # Copy psd1/psm1 to staging root
    $psd1Path = Join-Path $projectRoot "$ProjectName.psd1"
    $psm1Path = Join-Path $projectRoot "$ProjectName.psm1"

    write-build yellow $psd1path
    if (Test-Path $psd1Path)
    {
        Copy-Item $psd1Path -Destination $stagingRoot -Force
    }

    if (Test-Path $psm1Path)
    {
        Copy-Item $psm1Path -Destination $stagingRoot -Force
    }

    # Store for use in Patch-Manifest or later tasks
    Write-Host "[stage] Module staged to → $stagingRoot" -ForegroundColor Cyan
}


# ─────────────────────────────────────────────────────────────────────────────
# Patch PSD1 Manifest
# ─────────────────────────────────────────────────────────────────────────────
task Patch-Manifest Stage_DotNet_Output, {
    . Set-SamplerTaskVariable -AsNewBuild

    $stagingRoot = Join-Path $ProjectRoot $ModuleStagingSubdirectory

    $psd1Path = Join-Path $stagingRoot "$ProjectName.psd1"

    if (-not (Test-Path $psd1Path))
    {
        throw "Expected psd1 not found at: $psd1Path"
    }

    Update-ModuleManifest -Path $psd1Path -ModuleVersion $global:ResolvedVersion

    if ($global:ResolvedPrerelease)
    {
        Update-ModuleManifest -Path $psd1Path -Prerelease $global:ResolvedPrerelease
    }


    Write-Host "[manifest] Patched version → $psd1Path"
}

task Publish_Module_Output Patch-Manifest, {
    . Set-SamplerTaskVariable -AsNewBuild

    $stagingRoot = Join-Path $ProjectRoot $ModuleStagingSubdirectory


    $destination = $global:OutputPath

    Copy-Item -Path $stagingRoot -Destination $destination -Recurse -Force

    Write-Host "[publish] Final module copied to → $destination" -ForegroundColor Green
}

task Clean_Staging_Root {
    . Set-SamplerTaskVariable -AsNewBuild

    $stagingRoot = Join-Path $ProjectRoot $ModuleStagingSubdirectory

    if (Test-Path $stagingRoot)
    {
        Write-Host "[clean] Removing staging directory: $stagingRoot" -ForegroundColor DarkGray
        Remove-Item -Path $stagingRoot -Recurse -Force -ErrorAction Stop
    }
    else
    {
        Write-Host "[clean] Staging directory not found, nothing to clean." -ForegroundColor Gray
    }
}

task Run_DotNet_Tests {
    param (
        [string[]] $Projects = $buildInfo.DotNetTest.Projects,
        [string]   $Config = $buildInfo.DotNetTest.Configuration,
        [bool]     $NoBuild = $buildInfo.DotNetTest.NoBuild,
        [string]   $OutputDir = (Join-Path $OutputDirectory 'TestResults'),
        [string]   $CoverageFormat = $buildInfo.DotNetTest.Output.CoverageFormat,
        [string]   $CoverageFileName = $buildInfo.DotNetTest.Output.CoverageFileName
    )

    $null = New-Item -ItemType Directory -Path $OutputDir -Force

    foreach ($project in $Projects)
    {
        $projectPath = Join-Path $BuildRoot "Tests" $Project "$Project.csproj"
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
        $fileName = $CoverageFileName -replace '\$\(ProjectName\)', $projectName
        $fileName = $fileName -replace '\$\(VersionTag\)', $global:GitVersion.SemVer
        $coveragePath = Join-Path $OutputDir $fileName

        $Params = @(
            'test', "`"$projectPath`"",
            '--configuration', $Config,
            '--results-directory', "`"$OutputDir`"",
            '--logger:trx',
            "/p:CollectCoverage=true",
            "/p:CoverletOutputFormat=$CoverageFormat",
            "/p:CoverletOutput=$coveragePath"
        )

        if ($NoBuild)
        {
            $Params += '--no-build'
        }

        write-build Yellow ($Params -join " ")
        Start-Process 'dotnet' -ArgumentList $Params -NoNewWindow -Wait
        Write-Host "[test] Coverage file → $coveragePath" -ForegroundColor Green
    }
}


task Clean_TestResults {
    param (
        [string] $ResultsPath = (Join-Path $OutputDirectory 'TestResults')
    )

    if (Test-Path $ResultsPath)
    {
        Write-Host "[clean] Removing test results at: $ResultsPath" -ForegroundColor DarkGray
        Remove-Item -Path $ResultsPath -Recurse -Force -ErrorAction SilentlyContinue
    }

    # Recreate empty folder
    $null = New-Item -ItemType Directory -Path $ResultsPath -Force
    Write-Host "[clean] Created fresh test results directory → $ResultsPath" -ForegroundColor Green
}



# ─────────────────────────────────────────────────────────────────────────────
# Aggregate Task
# ─────────────────────────────────────────────────────────────────────────────
task Build_BinaryModule Publish_Module_Output
