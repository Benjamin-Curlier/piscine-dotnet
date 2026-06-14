#!/usr/bin/env bash
# Lance le DevHost (hôte dev/test, non livré) dans un état QA déterministe (parité Linux/CI du .ps1).
# Crée un PISCINE_HOME temporaire isolé, fixe les env vars QA et démarre dotnet run sur le DevHost ;
# le hook QA du DevHost seede l'état du profil via les types réels du moteur. Temp nettoyé à l'arrêt.
#
# Usage: scripts/devhost-qa.sh <fresh|mixed|exo-fail|exo-pass|push-result|done> [port]
set -euo pipefail

profile="${1:?usage: devhost-qa.sh <fresh|mixed|exo-fail|exo-pass|push-result|done> [port]}"
port="${2:-5240}"

case "$profile" in
  fresh|mixed|exo-fail|exo-pass|push-result|done) ;;
  *) echo "Profil inconnu : $profile (attendu : fresh|mixed|exo-fail|exo-pass|push-result|done)" >&2; exit 2 ;;
esac

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
qa_home="$(mktemp -d "${TMPDIR:-/tmp}/piscine-qa-${profile}-XXXXXX")"
mkdir -p "$qa_home/workspace"

export PISCINE_HOME="$qa_home"
export PISCINE_WORKSPACE="$qa_home/workspace"
export PISCINE_CONTENT="$repo/content"
export PISCINE_QA_PROFILE="$profile"

echo "[devhost-qa] profil=$profile home=$qa_home url=http://localhost:$port/"
cleanup() { rm -rf "$qa_home"; }
trap cleanup EXIT
dotnet run --project "$repo/src/Piscine.DevHost" --urls "http://localhost:$port"
