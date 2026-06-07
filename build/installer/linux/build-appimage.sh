#!/usr/bin/env bash
# Construit l'AppImage Piscine .NET (mode online ou offline).
#
# Usage : build-appimage.sh <online|offline> <AppDir> <sortie.AppImage>
#   - <AppDir>/usr/bin doit déjà contenir l'app desktop publiée (Piscine.Desktop + deps, gitshim/).
#   - offline : la machine de BUILD doit avoir libwebkit2gtk-4.1 + gtk installés (apt) pour les bundler ;
#     l'AppImage résultant tourne ensuite SANS webkit système (hors-ligne).
#   - online : AppImage léger, s'appuie sur le webkit système au lancement.
# Outils (linuxdeploy, plugin gtk, appimagetool) téléchargés ici : le build a internet, pas la cible.
set -euo pipefail

MODE="${1:?mode requis: online|offline}"
APPDIR="${2:?chemin AppDir requis}"
OUT="${3:?chemin de sortie .AppImage requis}"
HERE="$(cd "$(dirname "$0")" && pwd)"
WORK="$(mktemp -d)"
export APPIMAGE_EXTRACT_AND_RUN=1   # pas de FUSE requis (conteneurs/CI)

echo ">> build-appimage mode=$MODE appdir=$APPDIR"

# ── Outils ──────────────────────────────────────────────────────────────────
wget -q -O "$WORK/linuxdeploy" \
  https://github.com/linuxdeploy/linuxdeploy/releases/download/continuous/linuxdeploy-x86_64.AppImage
wget -q -O "$WORK/appimagetool" \
  https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
chmod +x "$WORK/linuxdeploy" "$WORK/appimagetool"
if [ "$MODE" = "offline" ]; then
  wget -q -O "$WORK/linuxdeploy-plugin-gtk.sh" \
    https://raw.githubusercontent.com/linuxdeploy/linuxdeploy-plugin-gtk/master/linuxdeploy-plugin-gtk.sh
  chmod +x "$WORK/linuxdeploy-plugin-gtk.sh"
fi
export PATH="$WORK:$PATH"

# ── Métadonnées (.desktop + icône) ──────────────────────────────────────────
cp "$HERE/piscine.desktop" "$APPDIR/piscine.desktop"
if [ ! -f "$APPDIR/piscine.png" ]; then
  convert -size 128x128 xc:'#1d76db' "$APPDIR/piscine.png"   # imagemagick (build env)
fi

# La sonde de tracing LTTng de .NET (libcoreclrtraceptprovider.so) lie liblttng-ust.so.0, absente des
# distros récentes ; elle est OPTIONNELLE (diagnostics perf) → on la retire pour ne pas bloquer le
# bundling de dépendances. L'app tourne normalement sans elle.
rm -f "$APPDIR"/usr/bin/libcoreclrtraceptprovider.so

# ── Bundling ────────────────────────────────────────────────────────────────
if [ "$MODE" = "offline" ]; then
  # Photino.Native.so a une dependance ELF DIRECTE vers libwebkit2gtk (4.0 pour Photino.Blazor 3.2.0) :
  # linuxdeploy la bundle AUTOMATIQUEMENT si le webkit correspondant est installe sur la machine de build
  # (Ubuntu 22.04 a libwebkit2gtk-4.0-37 ; 24.04 ne l'a plus). On detecte la version presente.
  WK="$(find /usr/lib -regextype posix-extended -regex '.*/libwebkit2gtk-4\.[01]\.so\.[0-9]+' 2>/dev/null | head -1)"
  [ -n "$WK" ] || { echo "ERREUR: libwebkit2gtk introuvable (build env). Sur Ubuntu 22.04: apt install libwebkit2gtk-4.0-37"; exit 2; }
  WKVER="$(echo "$WK" | grep -oE '4\.[01]' | head -1)"
  echo ">> webkit detecte: $WK (4.$(echo "$WKVER" | cut -d. -f2))"
  # linuxdeploy exclut certaines libs « baseline » (fontconfig/freetype…) supposées présentes ; pour un
  # AppImage vraiment indépendant (machines minimales), on les force dans le bundle via --library.
  EXTRA=""
  for soname in libfontconfig.so.1 libfreetype.so.6; do
    p="$(find /usr/lib -name "$soname" 2>/dev/null | head -1)"
    [ -n "$p" ] && EXTRA="$EXTRA --library $p"
  done
  "$WORK/linuxdeploy" --appdir "$APPDIR" \
    -e "$APPDIR/usr/bin/Piscine.Desktop" -d "$APPDIR/piscine.desktop" -i "$APPDIR/piscine.png" \
    $EXTRA \
    --plugin gtk
  # Process auxiliaires WebKit (executables d'un libexec — non capturés par linuxdeploy).
  for d in /usr/lib/*/webkit2gtk-${WKVER} /usr/libexec/webkit2gtk-${WKVER} "$(dirname "$WK")/webkit2gtk-${WKVER}"; do
    if [ -d "$d" ]; then cp -rn "$d" "$APPDIR/usr/lib/"; echo ">> helpers WebKit: $d"; break; fi
  done
else
  # ONLINE : AUCUN bundling — l'AppImage s'appuie sur le webkit/gtk SYSTÈME du poste (apt si besoin).
  # On ne lance pas linuxdeploy (qui re-bundlerait webkit, dep directe) : AppImage léger.
  echo ">> mode online : pas de bundling (webkit2gtk-4.0 + gtk système requis sur le poste)"
fi

# AppRun maison (env webkit/gtk/gio/git/content) — écrase celui éventuellement généré par linuxdeploy.
cp "$HERE/AppRun" "$APPDIR/AppRun"
chmod +x "$APPDIR/AppRun"

# Icône AppImage (.DirIcon), requise par appimagetool — surtout en online (sans linuxdeploy).
cp -f "$APPDIR/piscine.png" "$APPDIR/.DirIcon"

# ── Assemblage final ────────────────────────────────────────────────────────
"$WORK/appimagetool" "$APPDIR" "$OUT"
echo ">> AppImage produit : $OUT"
