$ErrorActionPreference = 'Stop'
$projectDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = Join-Path $projectDirectory 'Assets\Branding\ProjectIcon-office-ribbon-1024.png'
$icon = Join-Path $projectDirectory 'Assets\Branding\ProjectIcon-office-ribbon.ico'
$manifest = Join-Path $projectDirectory 'Package.appxmanifest'
foreach ($path in @($source, $icon, $manifest)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing icon input: $path" }
}
# The same transparent artwork is used for both Windows theme variants.
& winapp manifest update-assets $source --light-image $source --manifest $manifest
if ($LASTEXITCODE -ne 0) { throw 'Windows icon asset generation failed' }
# Keep the supplied, hand-prepared multi-size ICO unchanged.
Copy-Item -LiteralPath $icon -Destination (Join-Path $projectDirectory 'Assets\AppIcon.ico') -Force
