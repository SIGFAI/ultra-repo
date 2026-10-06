param([string]$Python='python')
$ErrorActionPreference='Stop'
$taskRoot = $PSScriptRoot
& $Python -X utf8 (Join-Path $taskRoot 'Tools/preflight.py')
if($LASTEXITCODE -ne 0) { throw 'Sheet preflight failed; build cancelled.' }
dotnet build (Join-Path $taskRoot 'UltraHaul.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0) { throw 'Build failed.' }
