# CCDASTRO Temma Mount Driver

An ASCOM telescope driver for Takahashi Temma and Temma 2 equatorial mounts. The driver is a C# local COM server that retains the native Temma serial protocol while replacing the original VB6/ActiveX implementation.

Current version: **1.0.28**

## What it supports

- Connect and initialize supported Temma mounts
- GOTO slews and slew-abort handling
- Coordinate synchronization
- Sidereal tracking on/off through Temma standby control
- Manual axis motion and pulse guiding
- Configurable mount model and speed selection
- OTA East, OTA West, Counterweight Down, and Counterweight West startup references
- Driver-side ASCOM park and unpark state
- Multi-client ASCOM local-server operation and optional diagnostic logging

Supported mount selections include EM-11, EM-11 Temma 2M, EM-200, EM-200 Temma 2M, NJP, and EM-500.

## Requirements

- Windows
- ASCOM Platform installed
- .NET Framework 4.7.2 developer/runtime support for building from source
- A Takahashi Temma-compatible mount and serial connection

The Visual Studio project targets x86 because it is an ASCOM local COM server. It can serve both 32-bit and 64-bit ASCOM client applications through COM.

## Serial connection

The Temma serial connection is configured as:

| Setting | Value |
| --- | --- |
| Baud rate | 19200 |
| Data bits | 8 |
| Parity | Even |
| Stop bits | 1 |
| Line ending | CRLF |
| DTR | Enabled |

Select the COM port and mount configuration in the driver's ASCOM Setup dialog before connecting.

All clients served by the local COM server share one physical serial connection. The first
client opens and initializes the mount; later clients reuse that connection. The port is
closed only after the last connected client disconnects.

## Startup reference

Temma controllers need a known physical reference after startup. Select the actual mount position in Setup before connecting:

- **OTA East** or **OTA West** establishes the corresponding Temma pier reference.
- **Counterweight Down** uses the physical celestial-pole reference: true north / site latitude in the northern hemisphere, or the corresponding south-pole reference in the southern hemisphere.
- **Counterweight West** follows the legacy Temma reference geometry from the original driver.

The driver sends the required Temma `T`, `I`, `Z`, and `D` initialization commands and validates the `R0` sync acknowledgement.

## Tracking and standby

Temma uses inverted standby terminology:

| Command | Temma state | ASCOM meaning |
| --- | --- | --- |
| `STN-OFF` | Standby off; RA motor enabled | Tracking on |
| `STN-ON` | Standby on; RA motor stopped | Tracking off |
| `STN-COD` | Query standby state | Query tracking state |

The driver presents normal ASCOM `Tracking` semantics to client applications.

## Park and unpark

Temma has no dedicated protocol-level park command. Park and unpark are maintained by the driver:

- A configured park position is stored as physical Alt/Az plus pier-side information.
- Parking stops motion and turns tracking off.
- While parked, the driver reports the stored park position rather than a potentially stale raw mount coordinate.
- Unpark restores the Temma sky-coordinate reference from the saved physical park position and restores tracking when appropriate.

For a meaningful park azimuth, avoid an altitude of exactly 90 degrees: azimuth is undefined at the zenith. Use a value such as 85 to 89 degrees instead.

## Building and registering from source

1. Open `ASCOM.CCDASTRO.Temma.sln` in Visual Studio.
2. Build the **Release | Any CPU** configuration. The project itself targets x86.
3. From an elevated Command Prompt, register the generated local-server executable:

   ```text
   ASCOM.CCDASTROTemma.exe /regserver
   ```

4. Open an ASCOM client such as N.I.N.A., choose **CCDASTRO Temma Mount Driver**, and configure it through Setup.

To unregister the local server:

```text
ASCOM.CCDASTROTemma.exe /unregserver
```

Do not use `RegAsm` for this local-server executable.

## Diagnostic logging

Enable tracing in the driver Setup dialog when diagnosing a connection or protocol issue. ASCOM logs capture serial transmit/receive traffic, initialization, sync, slew verification, park, and unpark activity.

Useful files are normally named similarly to:

```text
ASCOM.Temma.Driver.*.txt
ASCOM.CCDASTROTemma.LocalServer.*.txt
```

## Protocol behavior

The driver follows the Temma command/reply model:

- `E` reads coordinates.
- `P` starts a GOTO and expects `R0` through `R5` status responses.
- `D` synchronizes coordinates and expects `R0`.
- `PS` aborts a slew and is sent without waiting for a reply.

Slew completion is determined from observed settled mount motion rather than requiring an unrealistically exact target-coordinate match. This matches the proven behavior of the original VB6 driver and accommodates normal encoder resolution and pointing error.

## Validation status

Version 1.0.28 supports simultaneous RA and Declination PulseGuide operations with coordinated Temma stop/restart handling and independent per-axis pulse state. Version 1.0.27 resets target-coordinate first-use state after every successful client connection, including automatic unpark initialization, and logs target initialization state for diagnostics. Version 1.0.26 returns ASCOM-specific invalid-operation exceptions when target coordinates are read before being set. Version 1.0.25 retains usable MoveAxis slew rates when a guide rate is set to zero and gives ASCOM's unset-target error precedence while parked. Version 1.0.24 accepts the Temma's full 0% to 99% programmable guide-rate range and preserves ASCOM's unset target-coordinate state while restoring a parked coordinate cache. Version 1.0.23 improves ASCOM Conform behavior with standards-compliant property validation and exceptions, target-coordinate initialization, axis-rate reporting, side-of-pier prediction, and asynchronous pulse guiding. Version 1.0.22 queries and programs the Temma's adjustable RA and Declination correction speeds through the `la`/`lb` and `LA`/`LB` commands, converting correctly between the mount's percentage-of-sidereal representation and ASCOM degrees per second. Version 1.0.21 added reference-counted, serialized sharing of one physical Temma connection across multiple ASCOM clients. Version 1.0.20 was exercised with a real Temma mount for connection, OTA-East initialization, sync, repeated GOTO slews, slew completion, and abort behavior. Counterweight-reference and park/unpark/reconnect behavior were also verified with the companion Temma simulator.

## Author

Chuck Faranda  
CCD Astro Observatory Automation  
<https://ccdastro.net>
