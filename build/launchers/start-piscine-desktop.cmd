@echo off
rem Lanceur Piscine Desktop (Windows) : place le git portable (MinGit) sur le PATH,
rem puis lance l'application de bureau (terminal embarque + cours + verification).
set "PISCINE_DIR=%~dp0"
set "PATH=%PISCINE_DIR%mingit\cmd;%PISCINE_DIR%;%PATH%"
start "" "%PISCINE_DIR%desktop\Piscine.Desktop.exe"
