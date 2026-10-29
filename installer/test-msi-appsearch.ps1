[CmdletBinding()]
param([string]$Path = "$PSScriptRoot/../dist/CCDASTRO.Temma.Setup.msi")
$ErrorActionPreference = 'Stop'
$Path = (Resolve-Path -LiteralPath $Path).Path
# Run only the read-only prerequisite search, never INSTALL or custom actions.
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class TemmaSearchTest {
 [DllImport("msi.dll", CharSet=CharSet.Unicode)] public static extern uint MsiOpenPackageExW(string path, uint options, out uint handle);
 [DllImport("msi.dll", CharSet=CharSet.Unicode)] public static extern uint MsiDoActionW(uint handle, string action);
 [DllImport("msi.dll", CharSet=CharSet.Unicode)] public static extern uint MsiGetPropertyW(uint handle, string name, StringBuilder value, ref uint size);
 [DllImport("msi.dll")] public static extern uint MsiSetInternalUI(uint level, IntPtr window);
 [DllImport("msi.dll")] public static extern uint MsiCloseHandle(uint handle);
}
'@
$previous = [TemmaSearchTest]::MsiSetInternalUI(2, [IntPtr]::Zero)
[uint32]$handle = 0
try {
    $code = [TemmaSearchTest]::MsiOpenPackageExW($Path, 1, [ref]$handle)
    if ($code -ne 0) { throw "Open package failed: $code" }
    $code = [TemmaSearchTest]::MsiDoActionW($handle, 'AppSearch')
    if ($code -ne 0) { throw "AppSearch failed: $code" }
    'AppSearch: PASS (0)'
    foreach ($name in @('TEMMA_ASCOM','TEMMA_EXISTING32','TEMMA_EXISTING64')) {
        $value = [Text.StringBuilder]::new(4096)
        [uint32]$size = $value.Capacity
        $code = [TemmaSearchTest]::MsiGetPropertyW($handle, $name, $value, [ref]$size)
        if ($code -ne 0) { throw "Read $name failed: $code" }
        "$name=$value"
    }
} finally {
    if ($handle) { [void][TemmaSearchTest]::MsiCloseHandle($handle) }
    [void][TemmaSearchTest]::MsiSetInternalUI($previous, [IntPtr]::Zero)
}
