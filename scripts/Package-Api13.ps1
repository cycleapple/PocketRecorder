param([string]$OutputDirectory = 'bin/x64/Release')
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$manifest = Get-Content "$OutputDirectory/PocketRecorder.json" -Raw | ConvertFrom-Json
if ($manifest.DalamudApiLevel -ne 13) { throw 'Expected Dalamud API 13' }
$version = $manifest.AssemblyVersion
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName((Resolve-Path "$OutputDirectory/PocketRecorder.dll")).Version.ToString()
if ($version -ne $assemblyVersion) { throw 'Assembly and manifest version mismatch' }
if ($env:GITHUB_REF_TYPE -eq 'tag' -and $env:GITHUB_REF_NAME -ne "api13-v$version") { throw 'Tag and package version mismatch' }
$required = @('PocketRecorder.dll', 'NativeRecorder.abi13.dll', 'NAudio.Core.dll', 'NAudio.Wasapi.dll', 'avformat-*.dll', 'avcodec-*.dll', 'avutil-*.dll', 'swresample-*.dll', 'libwinpthread-1.dll', 'libvpl.dll', 'images/icon.png', 'images/star-normal.png', 'images/star-hover.png', 'images/star-active.png')
foreach ($pattern in $required) {
    if (-not (Get-ChildItem "$OutputDirectory/$pattern" -File -ErrorAction SilentlyContinue)) { throw "Missing package file: $pattern" }
}
foreach ($pattern in @('Dalamud*.dll', 'FFXIVClientStructs.dll', 'Lumina*.dll', 'TerraFX*.dll', 'OmenTools.dll')) {
    if (Get-ChildItem "$OutputDirectory/$pattern" -File -ErrorAction SilentlyContinue) { throw "Host assembly must not ship: $pattern" }
}
New-Item -ItemType Directory -Force artifacts | Out-Null
Compress-Archive -Path "$OutputDirectory/*" -DestinationPath 'artifacts/latest.zip' -Force
$download = "https://github.com/cycleapple/PocketRecorder/releases/download/api13-v$version/latest.zip"
foreach ($property in @('DownloadLinkInstall', 'DownloadLinkUpdate', 'DownloadLinkTesting')) {
    $manifest | Add-Member -NotePropertyName $property -NotePropertyValue $download -Force
}
ConvertTo-Json -InputObject @($manifest) -Depth 10 | Set-Content artifacts/pluginmaster.json -Encoding utf8
Get-FileHash artifacts/latest.zip -Algorithm SHA256 | Format-List
