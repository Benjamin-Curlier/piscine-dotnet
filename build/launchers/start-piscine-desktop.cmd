@echo off
rem Lanceur Piscine Desktop (Windows) : place le git portable (MinGit) sur le PATH,
rem puis lance l'application de bureau (terminal embarque + cours + verification).
set "PISCINE_DIR=%~dp0"
set "PATH=%PISCINE_DIR%mingit\cmd;%PISCINE_DIR%;%PATH%"
rem Installeur OFFLINE : runtime WebView2 Fixed Version embarque -> on le pointe (sans admin, sans
rem internet). Absent (zip / mode online) : no-op, WebView2 systeme utilise.
if exist "%PISCINE_DIR%webview2\msedgewebview2.exe" set "WEBVIEW2_BROWSER_EXECUTABLE_FOLDER=%PISCINE_DIR%webview2"
start "" "%PISCINE_DIR%desktop\Piscine.Desktop.exe"
