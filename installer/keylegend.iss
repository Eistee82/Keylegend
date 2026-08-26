; Keylegend installer — Inno Setup 6
;
; Built by .github/workflows/release.yml, which passes the version and the staging directory:
;
;   iscc /DVersion=<version> /DSource=..\out\staging installer\keylegend.iss
;
; Per-user by design. Keylegend keeps its settings in %APPDATA% and its autostart entry under
; HKCU, so a machine-wide install would put the program somewhere its own uninstaller could not
; fully clean up: the Run values of every *other* user would stay behind, pointing at a path
; that no longer exists. Per-user also means no elevation prompt for a lighting utility, which
; is the right amount of privilege to ask for.

#ifndef Version
  #define Version "0.0.0-dev"
#endif

#ifndef Source
  #define Source "..\out\staging"
#endif

; VersionInfoVersion goes into the Windows file version resource, which takes digits and dots and
; nothing else. A version like 1.1.0-rc1 is perfectly good for a tag and a file name, so the
; suffix is trimmed here rather than forbidden there.
#if Pos("-", Version) > 0
  #define FileVersion Copy(Version, 1, Pos("-", Version) - 1)
#else
  #define FileVersion Version
#endif

#define AppName "Keylegend"
#define Publisher "Keylegend contributors"
#define Url "https://github.com/Eistee82/Keylegend"
#define AppExe "Keylegend.exe"

[Setup]
AppId={{8B4F2C31-9E7A-4D26-B5C8-1F3A6D0E9B47}
AppName={#AppName}
AppVersion={#Version}
AppVerName={#AppName} {#Version}
AppPublisher={#Publisher}
AppPublisherURL={#Url}
AppSupportURL={#Url}/issues
AppUpdatesURL={#Url}/releases
VersionInfoVersion={#FileVersion}

; Per-user throughout: no elevation, and the uninstaller can reach everything it created.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto

OutputDir=..\out
OutputBaseFilename=Keylegend-{#Version}-setup
SetupIconFile=..\src\Keylegend.App\keylegend.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}

Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

; Closes a running copy rather than failing on a locked file, and restarts it afterwards. The
; application has no installer-visible mutex, so this works on the files being in use — which is
; exactly the situation, since Keylegend is the sort of program that is always running.
;
; "force" rather than "yes": with yes, a silent install or uninstall cannot ask and so leaves the
; program running, and the uninstaller then walks away from a directory it could not delete.
; Tested exactly that way — the folder stayed behind with the process still in it. Interactively
; the user still sees the usual page listing what will be closed.
CloseApplications=force
; Not "yes": restarting the program is a courtesy after an install and plainly wrong during an
; uninstall, where it would put the files back in use while they are being deleted. The [Run]
; entry already offers to start it after installing, which is the same courtesy under the user's
; control.
RestartApplications=no
CloseApplicationsFilter=*.exe,*.dll

LicenseFile={#Source}\LICENSE

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[CustomMessages]
en.RuntimeMissing=Keylegend needs the .NET %1 Desktop Runtime, which does not appear to be installed.%n%nThe download page will open after this dialog. Install the runtime, then run this setup again.
de.RuntimeMissing=Keylegend benötigt die .NET-%1-Desktop-Runtime, die nicht installiert zu sein scheint.%n%nNach diesem Dialog öffnet sich die Downloadseite. Installiere die Runtime und starte dieses Setup danach erneut.
en.RemoveSettings=Also delete your settings, colours and profile edits?
de.RemoveSettings=Auch deine Einstellungen, Farben und Profiländerungen löschen?
en.LaunchAfter=Start {#AppName}
de.LaunchAfter={#AppName} starten

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
; The whole staging directory. The keyboard itself is not packaged: Keylegend reads the
; attached one from Razer Synapse at run time, which is also where its drawing comes from.
Source: "{#Source}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchAfter}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Left beside the binaries by version 1.0.0, which had a calibration mode. Nothing writes it any
; more, but an installation upgraded from that version still has it, and it is ours to remove.
Type: files; Name: "{app}\calibration-findings.txt"

; Then the directory itself. Removing the tracked files leaves the satellite-assembly folders
; behind — empty, but there: they are deleted at the moment their last file is still going away,
; and the removal quietly fails. The install directory is ours alone, created by this installer
; and holding nothing a user would put there, so taking the whole thing is both safe and the
; only way it ends up actually gone.
Type: filesandordirs; Name: "{app}"

[Code]
const
  RuntimeVersion = '10';
  RuntimeUrl = 'https://dotnet.microsoft.com/download/dotnet/10.0/runtime?runtime=desktop';
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
  RunValue = 'Keylegend';

function GetSubDirsOfDir(const Path: String; var Names: TArrayOfString): Boolean;
var
  Search: TFindRec;
  Count: Integer;
begin
  Count := 0;
  SetArrayLength(Names, 0);
  Result := False;

  if FindFirst(AddBackslash(Path) + '*', Search) then
  begin
    try
      Result := True;
      repeat
        if (Search.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0)
          and (Search.Name <> '.') and (Search.Name <> '..') then
        begin
          SetArrayLength(Names, Count + 1);
          Names[Count] := Search.Name;
          Count := Count + 1;
        end;
      until not FindNext(Search);
    finally
      FindClose(Search);
    end;
  end;
end;

{ Looks for the Windows Desktop runtime rather than the base one: this is a WPF application and
  the base runtime alone will not start it. The shared-framework folder is the reliable place to
  look — the registry entries differ between installer flavours, and "dotnet --list-runtimes"
  needs dotnet on the PATH, which a runtime-only install does not guarantee. }
function DesktopRuntimeInstalled(): Boolean;
var
  Folders: TArrayOfString;
  Base: String;
  I: Integer;
begin
  Result := False;

  Base := ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if not DirExists(Base) then
    Base := ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App');

  if not DirExists(Base) then
    Exit;

  if not GetSubDirsOfDir(Base, Folders) then
    Exit;

  for I := 0 to GetArrayLength(Folders) - 1 do
    if Copy(Folders[I], 1, Length(RuntimeVersion) + 1) = RuntimeVersion + '.' then
    begin
      Result := True;
      Exit;
    end;
end;

function InitializeSetup(): Boolean;
var
  Dummy: Integer;
begin
  Result := True;

  if DesktopRuntimeInstalled() then
    Exit;

  { Not fatal on its own — somebody may be installing ahead of the runtime on purpose — but
    saying nothing would mean the program fails to start with no explanation of why. }
  if SuppressibleMsgBox(FmtMessage(CustomMessage('RuntimeMissing'), [RuntimeVersion]),
                        mbConfirmation, MB_OKCANCEL, IDOK) = IDOK then
  begin
    ShellExec('open', RuntimeUrl, '', '', SW_SHOW, ewNoWait, Dummy);
    Result := False;
  end
  else
    Result := False;
end;

{ Asks a running copy to close, and insists if it will not.

  CloseApplications does this for Setup, but during a silent uninstall it leaves the program
  running and the uninstaller then walks away from a directory it cannot delete — verified by
  doing exactly that: the folder stayed behind with the process still inside it.

  Politely first. taskkill without /F posts a close request, which is what clicking the window's
  X does, and the settings file is written on every change rather than at shutdown, so nothing
  is lost either way. /F is only for a copy that ignores the request. }
procedure StopRunningKeylegend();
var
  Code: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/IM Keylegend.exe',
       '', SW_HIDE, ewWaitUntilTerminated, Code);

  { Long enough for a WPF window to finish closing, short enough not to look stuck. }
  Sleep(2500);

  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM Keylegend.exe',
       '', SW_HIDE, ewWaitUntilTerminated, Code);

  Sleep(500);
end;

procedure CurUninstallStepChanged(CurStep: TUninstallStep);
var
  Settings: String;
begin
  if CurStep <> usUninstall then
    Exit;

  StopRunningKeylegend();

  { The autostart entry names the executable that is about to be deleted. Leaving it would mean
    a failed start at every logon, with nothing left on disk to explain it. Only this value is
    touched, and only under the user doing the uninstalling — which is the same user who
    installed it, since this is a per-user install. }
  if RegValueExists(HKEY_CURRENT_USER, RunKey, RunValue) then
    RegDeleteValue(HKEY_CURRENT_USER, RunKey, RunValue);

  { Settings are the user's, not ours, so they survive by default and go only on request. }
  Settings := ExpandConstant('{userappdata}\Keylegend');

  if DirExists(Settings) then
    if SuppressibleMsgBox(CustomMessage('RemoveSettings'), mbConfirmation, MB_YESNO, IDNO) = IDYES then
      DelTree(Settings, True, True, True);
end;
