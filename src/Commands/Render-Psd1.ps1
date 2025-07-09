param (
    [string]$TemplatePath,
    [string]$OutputPath,
    [string]$Version,
    [string]$VersionTag
)

$content = Get-Content $TemplatePath -Raw

$content = $content -replace '#\{ModuleVersion\}', $Version
$content = $content -replace '#\{VersionTag\}', $VersionTag

Set-Content -Path $OutputPath -Value $content -Encoding UTF8
