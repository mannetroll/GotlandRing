$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName System.IO.Compression.FileSystem
$buildRoot = Join-Path $projectRoot 'Build'
if (-not (Test-Path (Join-Path $buildRoot 'GotlandRing.exe'))) { throw 'Build the Unity game first.' }
$payload = Join-Path $projectRoot 'PortableLauncher\Game.zip'
$stream = [IO.File]::Open($payload,[IO.FileMode]::Create)
$archive = [IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
try {
 Get-ChildItem -LiteralPath $buildRoot -Recurse -File | Where-Object { ($_.Extension -notin '.png','.log') -or $_.Name -eq 'gotland_ring_validation.png' } | ForEach-Object {
  $entry = [IO.Path]::GetRelativePath($buildRoot,$_.FullName).Replace('\','/')
  [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$_.FullName,$entry,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
 }
} finally { $archive.Dispose(); $stream.Dispose() }
Write-Output "Updated $payload"
