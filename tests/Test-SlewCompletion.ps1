# Run with 32-bit Windows PowerShell against the Release x86 driver.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $root 'ASCOM.CCDASTRO.Temma/bin/Release/ASCOM.CCDASTROTemma.exe'))
$type = $assembly.GetType('ASCOM.CCDASTROTemma.Telescope.Telescope', $true)
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
$driver = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($type)
# No COM server or hardware is created by this test fixture.
[GC]::SuppressFinalize($driver)
function SetField($name, $value) { $type.GetField($name,$flags).SetValue($driver,$value) }
function GetField($name) { $type.GetField($name,$flags).GetValue($driver) }
function Call($name, $values) { $type.GetMethod($name,$flags).Invoke($driver,$values) }
function Assert($condition,$message) { if (-not $condition) { throw $message } }
SetField 'coordinateUpdateLock' ([object]::new())
SetField 'slewCompletionLock' ([object]::new())
SetField 'slewVerificationLock' ([object]::new())
SetField 'connectedState' $true
SetField 'isSlewing' $true
[void](Call 'ConfirmSlewStarted' @())
[void](Call 'AcceptCoordinateSample' @([double]5,[double]20))
[void](Call 'AcceptCoordinateSample' @([double]5,[double]20))
SetField 'slewCompletionSettledSinceUtc' ([DateTime]::UtcNow.AddSeconds(-3))
# Cached RA/DEC/Slewing reads alone must not count as fresh stationary samples.
[void]$type.GetProperty('RightAscension').GetValue($driver,$null)
[void]$type.GetProperty('Declination').GetValue($driver,$null)
Assert ($type.GetProperty('Slewing').GetValue($driver,$null)) 'Cached values incorrectly completed the slew'
# The next real coordinate sample completes the slew, regardless of its caller.
[void](Call 'AcceptCoordinateSample' @([double]5,[double]20))
Assert (-not $type.GetProperty('Slewing').GetValue($driver,$null)) 'Fresh stationary sample did not complete the slew'
SetField 'isSlewing' $true
[void](Call 'ConfirmSlewStarted' @())
[void](Call 'AcceptCoordinateSample' @([double]5,[double]20))
SetField 'slewCompletionSettledSinceUtc' ([DateTime]::UtcNow.AddSeconds(-3))
[void](Call 'AcceptCoordinateSample' @([double]5.1,[double]20))
Assert (GetField 'isSlewing') 'Moving mount incorrectly completed'
Assert ((GetField 'slewCompletionSettledSinceUtc') -eq [DateTime]::MinValue) 'Movement did not reset settling'
# Busy GOTO now reports an error rather than disappearing silently.
try {
    $type.GetMethod('SlewToCoordinatesAsync').Invoke($driver,@([double]6,[double]21))
    throw 'Busy GOTO unexpectedly accepted'
} catch {
    Assert ($_.Exception.ToString().Contains('previous slew is still in progress')) 'Busy GOTO did not report its reason'
}
# A new generation starts with fresh completion evidence.
[void](Call 'ResetSlewCompletionTracking' @())
[void](Call 'AcceptCoordinateSample' @([double]6,[double]21))
Assert (GetField 'isSlewing') 'Unconfirmed slew incorrectly completed'
[void](Call 'ConfirmSlewStarted' @())
[void](Call 'AcceptCoordinateSample' @([double]6,[double]21))
[void](Call 'AcceptCoordinateSample' @([double]6,[double]21))
SetField 'slewCompletionSettledSinceUtc' ([DateTime]::UtcNow.AddSeconds(-3))
[void](Call 'AcceptCoordinateSample' @([double]6,[double]21))
Assert (-not (GetField 'isSlewing')) 'Second slew did not complete'
'PASS: polling order, cached samples, moving samples, busy request, and second-slew state.'
