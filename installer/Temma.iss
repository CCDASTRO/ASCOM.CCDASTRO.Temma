; Driver-only ASCOM Temma installer. Build the Release driver before compiling.
#define AppVersion "1.0.30"
#define DriverExe SourcePath + "..\ASCOM.CCDASTRO.Temma\bin\Release\ASCOM.CCDASTROTemma.exe"
#ifndef AscomResources
  #define AscomResources GetEnv("ProgramFiles(x86)") + "\ASCOM\Developer\Installer Generator\Resources"
#endif

[Setup]
AppID={{08513ca2-7051-4056-b02c-8b49162eb39a}
AppName=ASCOM CCDASTRO Temma Mount Driver
AppVerName=ASCOM CCDASTRO Temma Mount Driver {#AppVersion}
AppVersion={#AppVersion}
AppPublisher=Chuck Faranda <ccd@ccdastro.net>
AppPublisherURL=mailto:ccd@ccdastro.net
AppSupportURL=https://ascomtalk.groups.io/g/Help
AppUpdatesURL=https://ascom-standards.org/
VersionInfoVersion={#AppVersion}
MinVersion=6.1.7601
DefaultDirName="{cf}\ASCOM\Telescope"
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=CCDASTRO.Temma.Setup
Compression=lzma
SolidCompression=yes
WizardImageFile={#AscomResources}\WizardImage.bmp
LicenseFile={#AscomResources}\CreativeCommons.txt
UninstallFilesDir="{cf}\ASCOM\Uninstall\Telescope\CCDASTRO Temma"

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{cf}\ASCOM\Uninstall\Telescope\CCDASTRO Temma"

[Files]
Source: "{#DriverExe}"; DestDir: "{app}"


[Run]

Filename: "{app}\ASCOM.CCDASTROTemma.exe"; Parameters: "/register"



[UninstallRun]

Filename: "{app}\ASCOM.CCDASTROTemma.exe"; Parameters: "/unregister"



[Code]
const
   REQUIRED_PLATFORM_VERSION = 6.2;    // Set this to the minimum required ASCOM Platform version for this application

//
// Function to return the ASCOM Platform's version number as a double.
//
function PlatformVersion(): Double;
var
   PlatVerString : String;
begin
   Result := 0.0;  // Initialise the return value in case we can't read the registry
   try
      if RegQueryStringValue(HKEY_LOCAL_MACHINE_32, 'Software\ASCOM','PlatformVersion', PlatVerString) then 
      begin // Successfully read the value from the registry
         Result := StrToFloat(PlatVerString); // Create a double from the X.Y Platform version string
      end;
   except                                                                   
      ShowExceptionMessage;
      Result:= -1.0; // Indicate in the return value that an exception was generated
   end;
end;

//
// Before the installer UI appears, verify that the required ASCOM Platform version is installed.
//
function InitializeSetup(): Boolean;
var
   PlatformVersionNumber : double;
 begin
   Result := FALSE;  // Assume failure
   PlatformVersionNumber := PlatformVersion(); // Get the installed Platform version as a double
   If PlatformVersionNumber >= REQUIRED_PLATFORM_VERSION then	// Check whether we have the minimum required Platform or newer
      Result := TRUE
   else
      if PlatformVersionNumber = 0.0 then
         MsgBox('No ASCOM Platform is installed. Please install Platform ' + Format('%3.1f', [REQUIRED_PLATFORM_VERSION]) + ' or later from https://www.ascom-standards.org', mbCriticalError, MB_OK)
      else 
         MsgBox('ASCOM Platform ' + Format('%3.1f', [REQUIRED_PLATFORM_VERSION]) + ' or later is required, but Platform '+ Format('%3.1f', [PlatformVersionNumber]) + ' is installed. Please install the latest Platform before continuing; you will find it at https://www.ascom-standards.org', mbCriticalError, MB_OK);
end;

// Code to enable the installer to uninstall previous versions of itself when a new version is installed
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  UninstallExe: String;
  UninstallRegistry: String;
begin
  if (CurStep = ssInstall) then // Install step has started
	begin
      // Create the correct registry location name, which is based on the AppId
      UninstallRegistry := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}' + '_is1');
      // Check whether an extry exists
      if RegQueryStringValue(HKLM, UninstallRegistry, 'UninstallString', UninstallExe) then
        begin // Entry exists and previous version is installed so run its uninstaller quietly after informing the user
          MsgBox('Setup will now remove the previous version.', mbInformation, MB_OK);
          Exec(RemoveQuotes(UninstallExe), ' /SILENT', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode);
          sleep(1000);    //Give enough time for the install screen to be repainted before continuing
        end
  end;
end;
