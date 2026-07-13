// Chrome de fenêtre custom. Inerte hors hôte Photino (pas de window.external.sendMessage).
(function () {
  var isPhotino = !!(window.external && window.external.sendMessage);
  document.documentElement.setAttribute('data-host', isPhotino ? 'photino' : 'browser');
  window.__isPhotino = isPhotino;

  function send(msg) { try { window.external.sendMessage(msg); } catch (e) { } }

  // Appelé par les boutons (onclick="winControl('minimize'|'togglemax'|'close')").
  window.winControl = function (action) {
    if (!isPhotino) return;
    send('PISCINE_WIN:' + action);
  };

  if (!isPhotino) return; // navigateur : pas de drag, contrôles masqués par CSS.

  // Déplacement : pointerdown sur une zone drag (hors no-drag) → envoie des deltas écran à l'hôte.
  var dragging = false, lastX = 0, lastY = 0;
  function isDragZone(el) {
    for (var n = el; n && n !== document.documentElement; n = n.parentElement) {
      if (n.matches && n.matches('.no-drag, button, a, input, select, textarea, [contenteditable]')) return false;
      if (n.matches && n.matches('.titlebar-drag')) return true;
    }
    return false;
  }
  document.addEventListener('pointerdown', function (e) {
    if (e.button !== 0 || !isDragZone(e.target)) return;
    dragging = true; lastX = e.screenX; lastY = e.screenY;
    try { e.target.setPointerCapture && e.target.setPointerCapture(e.pointerId); } catch (x) { }
  });
  document.addEventListener('pointermove', function (e) {
    if (!dragging) return;
    var dx = e.screenX - lastX, dy = e.screenY - lastY;
    if (dx || dy) { send('PISCINE_WIN:dragby:' + dx + ',' + dy); lastX = e.screenX; lastY = e.screenY; }
  });
  document.addEventListener('pointerup', function () { dragging = false; });
  document.addEventListener('dblclick', function (e) { if (isDragZone(e.target)) send('PISCINE_WIN:togglemax'); });

  // Redimensionnement par poignées (chromeless : pas de bordure de resize OS). On envoie le delta
  // TOTAL depuis le début du geste (resizeto), et l'hôte l'applique à une ancre figée au pointerdown
  // (resizestart) : la fenêtre suit exactement le curseur, sans la dérive/lag des deltas incrémentaux
  // appliqués sur win.Width. Poignées masquées/no-op en navigateur.
  ['e', 's', 'se'].forEach(function (edge) {
    document.addEventListener('pointerdown', function (e) {
      var h = e.target.closest && e.target.closest('.rh-' + edge); if (!h) return;
      var sx = e.screenX, sy = e.screenY; e.preventDefault();
      send('PISCINE_WIN:resizestart:' + edge);
      function mv(ev) { send('PISCINE_WIN:resizeto:' + edge + ':' + (ev.screenX - sx) + ',' + (ev.screenY - sy)); }
      function up() { document.removeEventListener('pointermove', mv); document.removeEventListener('pointerup', up); }
      document.addEventListener('pointermove', mv); document.addEventListener('pointerup', up);
    });
  });

  // L'hôte annonce l'état agrandi → bascule une classe pour le style (coins/ombre).
  window.__winState = function (state) {
    document.documentElement.classList.toggle('is-maximized', state === 'maximized');
  };

  // Réception des messages hôte→page (Photino : window.external.receiveMessage). L'hôte émet
  // "PISCINE_WIN_STATE:maximized|normal" lors d'un changement d'état agrandi (voir WindowChromeHost).
  try {
    window.external.receiveMessage(function (message) {
      if (typeof message === 'string' && message.indexOf('PISCINE_WIN_STATE:') === 0) {
        window.__winState(message.substring('PISCINE_WIN_STATE:'.length));
      }
    });
    // Lancement plein écran : demander l'agrandissement une fois la page affichée (l'hôte maximise via
    // l'OS et renvoie l'état pour styler coins/poignées).
    send('PISCINE_WIN:maximizeonstart');
  } catch (e) { /* receiveMessage indisponible : le style d'état reste inerte. */ }
})();
