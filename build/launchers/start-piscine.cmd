@echo off
rem Lanceur Piscine (Windows) : place le git portable (MinGit) et piscine sur le PATH,
rem puis ouvre une invite prete a l'emploi.
set "PISCINE_DIR=%~dp0"
set "PATH=%PISCINE_DIR%mingit\cmd;%PISCINE_DIR%;%PATH%"
echo Piscine prete. Exemples : piscine init   puis   piscine start ^<exo^>
cmd /k "cd /d %PISCINE_DIR%"
