; Inno Setup — installeur Piscine .NET (Windows), modes offline / online (via ISCC /DMODE).
;   ISCC /DMODE=offline /DAPPVERSION=v.. /DPAYLOAD=<dir>
;   ISCC /DMODE=online  /DAPPVERSION=v.. /DPAYLOAD=<dir>
; <PAYLOAD> contient toujours : piscine.exe (+ CLI), content\, desktop\ (+ gitshim\), mingit\,
; lanceurs, et un "webview2-setup.exe" = selon le mode, l'installeur **Standalone Evergreen**
; (OFFLINE, full, hors-ligne) OU le **bootstrapper Evergreen** (ONLINE, léger, télécharge).
; Lancé en silencieux à l'install SI WebView2 est absent ; ignoré si déjà présent (Win11 / Win10
; récents l'ont déjà). Sans process élevé -> install per-utilisateur (sans admin) quand c'est possible.

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
; Per-utilisateur, sans prompt UAC.
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
; Tout le payload (app de bureau + CLI + content + MinGit + shim + installeur WebView2 du mode).
Source: "{#PAYLOAD}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\Piscine .NET"; Filename: "{app}\start-piscine-desktop.cmd"; WorkingDir: "{app}"; IconFilename: "{app}\desktop\Piscine.Desktop.exe"
Name: "{group}\Piscine (terminal git)"; Filename: "{app}\start-piscine.cmd"; WorkingDir: "{app}"
Name: "{group}\Désinstaller Piscine .NET"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Piscine .NET"; Filename: "{app}\start-piscine-desktop.cmd"; WorkingDir: "{app}"; IconFilename: "{app}\desktop\Piscine.Desktop.exe"; Tasks: desktopicon

[Run]
; Installe le runtime WebView2 (silencieux) SEULEMENT s'il est absent. OFFLINE = installeur full
; (hors-ligne) ; ONLINE = bootstrapper (télécharge). Ignoré si WebView2 déjà présent (cas courant).
Filename: "{app}\webview2-setup.exe"; Parameters: "/silent /install"; Check: WebView2Missing; StatusMsg: "Installation du runtime WebView2 si nécessaire..."; Flags: waituntilterminated

[Code]
function WebView2Missing: Boolean;
var v: String;
begin
  // WebView2 Evergreen présent si la valeur 'pv' (>0.0.0.0) du client EdgeUpdate existe (HKLM 64-bit ou HKCU).
  Result := not (
    RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', v) or
    RegQueryStringValue(HKCU, 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', v));
end;
