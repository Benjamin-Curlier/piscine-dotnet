// Interop de la page Réglages (S6) : miroir localStorage du thème + échelle de police, lu par le
// bootstrap anti-flash (avant le premier paint) et par theme.js. La SOURCE DE VÉRITÉ durable reste
// settings.json (SettingsService) ; ces helpers ne font que synchroniser le miroir rapide côté client.
// Les fonctions globales (setThemePreference / setFontScale) sont définies dans theme.js (RCL).

export function applyTheme(pref) {
    if (window.setThemePreference) {
        window.setThemePreference(pref);
    }
}

export function applyFontScale(scale) {
    if (window.setFontScale) {
        window.setFontScale(scale);
    }
}

// Marqueur de disponibilité interactive : l'E2E attend window.__settingsReady avant d'interagir
// (le circuit Blazor est alors monté → @onclick/@onchange câblés).
export function markReady() {
    window.__settingsReady = true;
}
