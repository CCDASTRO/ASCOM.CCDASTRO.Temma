#requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$AdvancedInstaller)
# Requires PowerShell 7 and Advanced Installer Freeware 24.1 or later.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
& $AdvancedInstaller /build (Join-Path $PSScriptRoot 'Temma.aip')
if ($LASTEXITCODE -ne 0) { throw "Advanced Installer build failed: $LASTEXITCODE" }
$source = Join-Path $root 'dist\msi-base\Temma.msi'
$output = Join-Path $root 'dist\CCDASTRO.Temma.Setup.msi'
Copy-Item -LiteralPath $source -Destination $output -Force
$installer = New-Object -ComObject WindowsInstaller.Installer
$db = $installer.OpenDatabase($output, 1)
function Sql([string]$query) {
    $view = $db.OpenView($query)
    try { $view.Execute() } finally { $view.Close() }
}
function EnsureTable([string]$name, [string]$ddl) {
    $v = $db.OpenView("SELECT ``Name`` FROM ``_Tables`` WHERE ``Name``='$name'")
    $v.Execute(); $exists = $v.Fetch(); $v.Close()
    if (-not $exists) { Sql $ddl }
}
try {
    # Release product identity; previous installers must be removed first.
    Sql "UPDATE ``Property`` SET ``Value``='{D50F8511-728B-4601-9880-BAB73616FC62}' WHERE ``Property``='ProductCode'"
    Sql "UPDATE ``Property`` SET ``Value``='{5CC1980C-D242-4305-8245-AB3D68DC08ED}' WHERE ``Property``='UpgradeCode'"
    Sql "UPDATE ``Property`` SET ``Value``='CCDASTRO Temma Mount Driver' WHERE ``Property``='ProductName'"
    Sql "UPDATE ``CustomAction`` SET ``Target``='[CommonFilesFolder]ASCOM\Telescope\Temma' WHERE ``Action``='SET_APPDIR'"
    Sql "UPDATE ``Component`` SET ``ComponentId``='{0A726027-7120-46AB-B8AA-9257D96E9A32}' WHERE ``Component``='ASCOM.CCDASTROTemma.exe'"
    Sql "UPDATE ``Component`` SET ``ComponentId``='{0C62A177-A107-4328-9760-36C8BE12BDA3}' WHERE ``Component``='APPDIR'"
    # Migration from previous installers requires uninstall first.
    Sql 'DELETE FROM `Upgrade`'
    Sql "DELETE FROM ``InstallExecuteSequence`` WHERE ``Action``='RemoveExistingProducts'"
    # AppSearch requires this table even for raw registry values. Keep it empty:
    # matching Signature rows would turn these registry searches into file searches.
    EnsureTable 'Signature' 'CREATE TABLE `Signature` (`Signature` CHAR(72) NOT NULL, `FileName` CHAR(255) NOT NULL, `MinVersion` CHAR(20), `MaxVersion` CHAR(20), `MinSize` LONG, `MaxSize` LONG, `MinDate` LONG, `MaxDate` LONG, `Languages` CHAR(255) PRIMARY KEY `Signature`)'
    EnsureTable 'RegLocator' 'CREATE TABLE `RegLocator` (`Signature_` CHAR(72) NOT NULL, `Root` SHORT NOT NULL, `Key` CHAR(255) NOT NULL, `Name` CHAR(255), `Type` SHORT PRIMARY KEY `Signature_`)'
    EnsureTable 'AppSearch' 'CREATE TABLE `AppSearch` (`Property` CHAR(72) NOT NULL, `Signature_` CHAR(72) NOT NULL PRIMARY KEY `Property`, `Signature_`)'
    Sql "INSERT INTO ``RegLocator`` (``Signature_``,``Root``,``Key``,``Name``,``Type``) VALUES ('TemmaAscom',2,'SOFTWARE\ASCOM','PlatformVersion',2)"
    Sql "INSERT INTO ``AppSearch`` (``Property``,``Signature_``) VALUES ('TEMMA_ASCOM','TemmaAscom')"
    foreach ($bitness in @(32,64)) {
        $type = if ($bitness -eq 64) {18} else {2}
        Sql "INSERT INTO ``RegLocator`` (``Signature_``,``Root``,``Key``,``Type``) VALUES ('TemmaExisting$bitness',2,'SOFTWARE\Classes\CLSID\{c41150b3-378c-4a3a-a44c-55e99b8c7554}\LocalServer32',$type)"
        Sql "INSERT INTO ``AppSearch`` (``Property``,``Signature_``) VALUES ('TEMMA_EXISTING$bitness','TemmaExisting$bitness')"
    }
    Sql "INSERT INTO ``LaunchCondition`` (``Condition``,``Description``) VALUES ('Installed OR TEMMA_ASCOM','Install ASCOM Platform before installing this driver.')"
    Sql "INSERT INTO ``LaunchCondition`` (``Condition``,``Description``) VALUES ('Installed OR (NOT TEMMA_EXISTING32 AND NOT TEMMA_EXISTING64)','An existing Temma driver registration was found. Uninstall the previous Temma driver before installing this package. Your ASCOM settings will be preserved.')"
    Sql "UPDATE ``Property`` SET ``Value``='OLDPRODUCTS;AI_NEWERPRODUCTFOUND;TEMMA_ASCOM;TEMMA_EXISTING32;TEMMA_EXISTING64' WHERE ``Property``='SecureCustomProperties'"
    $component = 'ASCOM.CCDASTROTemma.exe'
    $installCondition = '($' + $component + '=3)'
    $freshCondition = 'NOT Installed AND ' + $installCondition
    # ? is installed component state; & refers to a feature's action state.
    $removeCondition = '($' + $component + '=2) AND (?' + $component + '=3)'
    # Type 18: installed executable. 1024: deferred. 2048: no impersonation.
    # Rollback adds 256. All actions are synchronous and check exit codes.
    $actions = @(
        @('TemmaUndoRemoval',3346,'/register',$removeCondition,3480),
        @('TemmaUnregister',3090,'/unregister',$removeCondition,3490),
        @('TemmaUndoInstall',3346,'/unregister',$freshCondition,5990),
        @('TemmaRegister',3090,'/register',$installCondition,5991)
    )
    foreach ($a in $actions) {
        Sql "INSERT INTO ``CustomAction`` (``Action``,``Type``,``Source``,``Target``) VALUES ('$($a[0])',$($a[1]),'$component','$($a[2])')"
        Sql "INSERT INTO ``InstallExecuteSequence`` (``Action``,``Condition``,``Sequence``) VALUES ('$($a[0])','$($a[3])',$($a[4]))"
    }
    $db.Commit()
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($db)
    $db = $null
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class TemmaMsiSummary {
 [DllImport("msi.dll", CharSet=CharSet.Unicode)] public static extern uint MsiGetSummaryInformationW(IntPtr db, string path, uint count, out uint handle);
 [DllImport("msi.dll", CharSet=CharSet.Unicode)] public static extern uint MsiSummaryInfoSetPropertyW(uint handle, uint property, uint type, int number, IntPtr time, string value);
 [DllImport("msi.dll")] public static extern uint MsiSummaryInfoPersist(uint handle);
 [DllImport("msi.dll")] public static extern uint MsiCloseHandle(uint handle);
}
"@
    [uint32]$handle=0
    $code=[TemmaMsiSummary]::MsiGetSummaryInformationW([IntPtr]::Zero,$output,1,[ref]$handle)
    if($code -ne 0){throw "Open summary failed: $code"}
    try {
        $code=[TemmaMsiSummary]::MsiSummaryInfoSetPropertyW($handle,9,30,0,[IntPtr]::Zero,([guid]::NewGuid().ToString('B').ToUpperInvariant()))
        if($code -ne 0){throw "Set package code failed: $code"}
        $code=[TemmaMsiSummary]::MsiSummaryInfoPersist($handle)
        if($code -ne 0){throw "Persist summary failed: $code"}
    } finally { [void][TemmaMsiSummary]::MsiCloseHandle($handle) }
} finally {
    if ($null -ne $db) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($db) }
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)
}
Get-FileHash -LiteralPath $output
