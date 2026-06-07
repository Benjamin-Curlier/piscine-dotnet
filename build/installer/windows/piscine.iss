; Inno Setup — installeur Piscine .NET (Windows).
; Construit en deux modes via ISCC :
;   ISCC /DMODE=offline /DAPPVERSION=v.. /DPAYLOAD=<dir>   -> bundle le runtime WebView2 Fixed Version
;   ISCC /DMODE=online  /DAPPVERSION=v.. /DPAYLOAD=<dir>   -> lance le bootstrapper Evergreen si absent
; <PAYLOAD> contient : piscine.exe (+ CLI), content\, desktop\ (+ gitshim\), mingit\, lanceurs,
;   et selon le mode : webview2\ (offline) OU MicrosoftEdgeWebview2Setup.exe (online).

#ifndef MODE
  #define MODE "offline"
#endif
#ifndef APPVERSION
  #define APPVERSION "0.0.0"
#endif
#ifndef PAYLOAD
  #define PAYLOAD "payload"
#endif

[Setup]
AppName=Piscine .NET
AppVersion={#APPVERSION}
AppPublisher=Piscine .NET
DefaultDirName={autopf}\Piscine .NET
DefaultGroupName=Piscine .NET
DisableProgramGroupPage=yes
; Installation sans droits administrateur (per-utilisateur) — pas de prompt UAC.
PrivilegesRequired=lowest
OutputDir=dist
OutputBaseFilename=piscine-{#APPVERSION}-win-x64-{#MODE}-setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le Bureau"; GroupDescription: "Raccourcis supplémentaires :"

[Files]
; Tout le payload (app de bureau + CLI + content + MinGit + shim + webview/bootstrapper selon le mode).
Source: "{#PAYLOAD}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
; L'app de bureau via le lanceur (qui met git sur le PATH et, en offline, pointe WebView2 Fixed Version).
Name: "{group}\Piscine .NET"; Filename: "{app}\start-piscine-desktop.cmd"; WorkingDir: "{app}"; IconFilename: "{app}\desktop\Piscine.Desktop.exe"
Name: "{group}\Piscine (terminal git)"; Filename: "{app}\start-piscine.cmd"; WorkingDir: "{app}"
Name: "{group}\Désinstaller Piscine .NET"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Piscine .NET"; Filename: "{app}\start-piscine-desktop.cmd"; WorkingDir: "{app}"; IconFilename: "{app}\desktop\Piscine.Desktop.exe"; Tasks: desktopicon

#if MODE == "online"
[Run]
; Mode ONLINE : installer le runtime WebView2 Evergreen (télécharge) s'il est absent. Hors-ligne ? -> mode offline.
Filename: "{app}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; Check: WebView2Missing; StatusMsg: "Installation du runtime WebView2 (connexion requise)..."; Flags: waituntilterminated

[Code]
function WebView2Missing: Boolean;
var v: String;
begin
  // WebView2 Evergreen est présent si la valeur 'pv' du client EdgeUpdate existe (HKLM 64-bit ou HKCU).
  Result := not (
    RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', v) or
    RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', v));
end;
#endif
