// Module ESM colocalisé du composant CommandPalette.
//
// Rôle : enregistrer le raccourci GLOBAL Ctrl/⌘+K (et quelques raccourcis de navigation) au niveau
// document, et fournir un piège de focus dans l'overlay. Le composant .NET reste maître de l'état
// (ouvert/fermé, filtrage) ; ce module ne fait que relayer les frappes globales vers .NET et gérer le
// focus DOM, qui n'est pas accessible proprement côté serveur.
//
// Piège (spec §9) : ⌘K ne doit PAS voler les frappes destinées au terminal embarqué (xterm). On
// ignore donc le raccourci quand le focus est à l'intérieur de .piscine-terminal.

let _handler = null;
let _dotnet = null;
let _previousFocus = null;

function isInTerminal(target) {
    return !!(target && target.closest && target.closest(".piscine-terminal"));
}

function isEditable(target) {
    if (!target) return false;
    const tag = target.tagName;
    return tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT" || target.isContentEditable;
}

// Enregistre le raccourci global. dotnet = DotNetObjectReference<CommandPalette>.
export function register(dotnet) {
    _dotnet = dotnet;

    _handler = (e) => {
        // Ctrl+K (Win/Linux) ou ⌘K (mac) → ouvrir la palette.
        if ((e.ctrlKey || e.metaKey) && !e.altKey && (e.key === "k" || e.key === "K")) {
            if (isInTerminal(e.target)) {
                return; // laisser le terminal capturer ses propres frappes
            }
            e.preventDefault();
            _dotnet.invokeMethodAsync("OpenFromJs");
            return;
        }

        // Raccourcis de navigation globaux (hors champ de saisie, hors terminal).
        if (e.defaultPrevented || isEditable(e.target) || isInTerminal(e.target)) {
            return;
        }

        // Alt+Flèche droite / gauche : exercice suivant / précédent.
        if (e.altKey && !e.ctrlKey && !e.metaKey && (e.key === "ArrowRight" || e.key === "ArrowLeft")) {
            e.preventDefault();
            _dotnet.invokeMethodAsync("ShortcutFromJs", e.key === "ArrowRight" ? "next" : "prev");
            return;
        }

        // "/" ouvre la palette focalisée sur la recherche (raccourci « focus recherche »).
        if (e.key === "/" && !e.ctrlKey && !e.metaKey && !e.altKey) {
            e.preventDefault();
            _dotnet.invokeMethodAsync("OpenFromJs");
            return;
        }

        // "b" : aller au tableau de bord (board).
        if ((e.key === "b" || e.key === "B") && !e.ctrlKey && !e.metaKey && !e.altKey) {
            e.preventDefault();
            _dotnet.invokeMethodAsync("ShortcutFromJs", "board");
        }
    };

    document.addEventListener("keydown", _handler, true);

    // Marqueur de disponibilité : le handler global est attaché. Permet à un test E2E (ou à du code
    // d'orchestration) d'attendre que ⌘K soit réellement opérationnel avant de presser la touche
    // (le circuit interactif Blazor peut mettre un instant à monter l'îlot de la palette).
    window.__cmdkReady = true;
}

// Donne le focus au champ de recherche et mémorise l'élément focalisé précédent (restauré à la fermeture).
export function focusInput(input) {
    _previousFocus = document.activeElement;
    if (input && typeof input.focus === "function") {
        // Différé d'un tick : l'overlay vient d'être rendu.
        requestAnimationFrame(() => input.focus());
    }
}

// Restaure le focus sur l'élément actif avant ouverture (accessibilité).
export function restoreFocus() {
    if (_previousFocus && typeof _previousFocus.focus === "function") {
        try { _previousFocus.focus(); } catch { /* élément retiré du DOM */ }
    }
    _previousFocus = null;
}

export function dispose() {
    if (_handler) {
        document.removeEventListener("keydown", _handler, true);
        _handler = null;
    }
    _dotnet = null;
    _previousFocus = null;
    window.__cmdkReady = false;
}
