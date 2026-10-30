$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$powershell32 = Join-Path $env:WINDIR 'SysWOW64/WindowsPowerShell/v1.0/powershell.exe'
& $powershell32 -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Test-SlewCompletion.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Slew completion regression failed' }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
$output = Join-Path $root 'output/ServerLifetimeTests.exe'
New-Item -ItemType Directory -Path (Split-Path $output) -Force | Out-Null
& $compiler /nologo "/out:$output" (Join-Path $PSScriptRoot 'ServerLifetimeTests.cs') (Join-Path $root 'ASCOM.CCDASTRO.Temma/ReferenceCountedObject.cs')
if ($LASTEXITCODE -ne 0) { throw 'Lifetime test compilation failed' }
& $output
if ($LASTEXITCODE -ne 0) { throw 'Lifetime regression failed' }
