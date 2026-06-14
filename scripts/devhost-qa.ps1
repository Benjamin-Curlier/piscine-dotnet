#requires -version 7
<#
.SYNOPSIS
    Lance le DevHost (hôte dev/test, non livré) dans un état QA déterministe.

.DESCRIPTION
    Crée un PISCINE_HOME temporaire isolé, fixe les variables d'environnement
    (PISCINE_HOME / PISCINE_WORKSPACE / PISCINE_CONTENT / PISCINE_QA_PROFILE) et démarre
    `dotnet run` sur src/Piscine.DevHost. Au démarrage, le hook QA du DevHost seede l'état
    du profil demandé via les types réels du moteur. Le temp est nettoyé à l'arrêt (Ctrl-C).

.PARAMETER Profile
    Profil de seed : fresh | mixed | exo-fail | exo-pass | push-result | done.

.PARAMETER Port
    Port HTTP local (défaut 5240).

.EXAMPLE
    pwsh scripts/devhost-qa.ps1 -Profile mixed -Port 5240
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)]
  [ValidateSet('fresh','mixed','exo-fail','exo-pass','push-result','done')]
  [string]$Profile,
  [int]$Port = 5240
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot\..").Path
$qaHome = Join-Path ([System.IO.Path]::GetTempPath()) "piscine-qa-$Profile-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force -Path (Join-Path $qaHome 'workspace') | Out-Null

$env:PISCINE_HOME       = $qaHome
$env:PISCINE_WORKSPACE  = Join-Path $qaHome 'workspace'
$env:PISCINE_CONTENT    = Join-Path $repo 'content'
$env:PISCINE_QA_PROFILE = $Profile

Write-Host "[devhost-qa] profil=$Profile  home=$qaHome  url=http://localhost:$Port/"
try {
  & dotnet run --project (Join-Path $repo 'src/Piscine.DevHost') --urls "http://localhost:$Port"
}
finally {
  Remove-Item -Recurse -Force $qaHome -ErrorAction SilentlyContinue
}
