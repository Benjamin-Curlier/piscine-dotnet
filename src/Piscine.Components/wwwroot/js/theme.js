// Thème partagé RCL — toggle et icône (DevHost + Photino)
// __applyTheme est aussi défini ici pour que les hôtes puissent le charger avant le premier paint.
window.__applyTheme = function () {
    var t = localStorage.getItem('theme')
        || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    document.documentElement.setAttribute('data-theme', t);
};

window.updateThemeIcon = function () {
    var b = document.getElementById('theme-toggle');
    if (b) {
        b.textContent = document.documentElement.getAttribute('data-theme') === 'dark' ? '☀' : '☾';
    }
};

window.toggleTheme = function () {
    var next = document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
    localStorage.setItem('theme', next);
    document.documentElement.setAttribute('data-theme', next);
    window.updateThemeIcon();
};
