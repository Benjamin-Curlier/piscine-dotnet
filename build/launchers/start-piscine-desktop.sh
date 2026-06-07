#!/bin/sh
# Lanceur Piscine Desktop (Linux/macOS). Lance l'application de bureau.
# Prerequis webview (libwebkit2gtk-4.1 sous Linux) : voir docs/mise-en-oeuvre.md.
DIR="$(cd "$(dirname "$0")" && pwd)"
exec "$DIR/desktop/Piscine.Desktop"
