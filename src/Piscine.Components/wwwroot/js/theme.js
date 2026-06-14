// Thème + échelle de police partagés RCL (DevHost + Photino).
// La SOURCE DE VÉRITÉ durable est settings.json (SettingsService, S6) ; localStorage n'est qu'un
// MIROIR rapide lu avant le premier paint (anti-flash) — la page /reglages écrit les deux.
//
// Clés localStorage :
//   theme     = 'system' | 'light' | 'dark'   (préférence ; 'system' suit prefers-color-scheme)
//   fontScale = nombre (ex. '1', '1.25')      (échelle de police, 1 = standard)
//
// __applyTheme est aussi défini ici pour que les hôtes puissent le charger avant le premier paint.

window.__resolveTheme = function (pref) {
    // 'system' (ou valeur inconnue) → résout selon la préférence OS ; sinon respecte le choix explicite.
    if (pref === 'light' || pref === 'dark') {
        return pref;
    }
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
};

window.__clampFontScale = function (raw) {
    var n = parseFloat(raw);
    if (isNaN(n)) {
        return 1;
    }
    // Bornes alignées sur AppSettings.MinFontScale / MaxFontScale (0.8 – 1.5).
    return Math.min(1.5, Math.max(0.8, n));
};

window.__applyTheme = function () {
    var pref = localStorage.getItem('theme') || 'system';
    document.documentElement.setAttribute('data-theme', window.__resolveTheme(pref));

    var scale = window.__clampFontScale(localStorage.getItem('fontScale'));
    document.documentElement.style.setProperty('--font-scale', scale);
};

window.updateThemeIcon = function () {
    var b = document.getElementById('theme-toggle');
    if (b) {
        b.textContent = document.documentElement.getAttribute('data-theme') === 'dark' ? '☀' : '☾';
    }
};

// Écrit la préférence de thème (miroir localStorage) puis l'applique. Appelé par la page /reglages.
window.setThemePreference = function (pref) {
    localStorage.setItem('theme', pref);
    window.__applyTheme();
    window.updateThemeIcon();
};

// Écrit l'échelle de police (miroir localStorage) puis l'applique. Appelé par la page /reglages.
window.setFontScale = function (scale) {
    localStorage.setItem('fontScale', window.__clampFontScale(scale));
    window.__applyTheme();
};

// Lit l'échelle de police courante (miroir) — utile à la page /reglages pour s'aligner sur le miroir.
window.getFontScale = function () {
    return window.__clampFontScale(localStorage.getItem('fontScale'));
};

// Bouton de la barre : bascule clair ↔ sombre en écrivant un choix EXPLICITE (plus 'system').
// Reste piloté par localStorage ; la page /reglages persiste en plus dans settings.json.
window.toggleTheme = function () {
    var next = document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
    window.setThemePreference(next);
};
