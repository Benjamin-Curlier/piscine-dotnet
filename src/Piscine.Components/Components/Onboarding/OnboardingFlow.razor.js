// Interop de l'onboarding 1ᵉʳ lancement (S7) : marqueur de disponibilité interactive + gestion du
// focus de la modale (focus initial, piège Tab/Shift+Tab, Escape → « Plus tard »).
// L'E2E attend window.__onboardingReady avant d'interagir (le circuit Blazor est alors monté →
// les @onclick sont câblés). La logique d'affichage vit côté C#.

let _trapHandler = null;
let _card = null;
let _dotnet = null;

export function markReady() {
    window.__onboardingReady = true;
}

// Éléments focalisables de la carte (accessibilité clavier). Exclut les tabindex=-1.
function focusable(container) {
    const selector =
        'a[href], button:not([disabled]), input:not([disabled]), textarea:not([disabled]),' +
        ' select:not([disabled]), [tabindex]:not([tabindex="-1"])';
    return Array.from(container.querySelectorAll(selector)).filter((el) => el.tabIndex !== -1);
}

// Arme la gestion du focus de la modale : focus initial sur le 1ᵉʳ focalisable de la carte, piège
// Tab dans la carte, Escape → Dismiss (« Plus tard »). dotnet = DotNetObjectReference<OnboardingFlow>.
export function activate(overlay, dotnet) {
    const card = overlay && overlay.querySelector ? overlay.querySelector(".onboarding-card") : null;
    if (!card) {
        return;
    }

    deactivate();
    _card = card;
    _dotnet = dotnet;

    // Focus initial : 1ᵉʳ élément focalisable de la carte (l'overlay vient d'être rendu → différé).
    const items = focusable(card);
    if (items.length > 0) {
        requestAnimationFrame(() => items[0].focus());
    }

    // Capture au niveau document : robuste même si le focus est retombé sur <body> après un
    // changement d'étape (le bouton focalisé a été retiré du DOM).
    _trapHandler = (e) => {
        if (!_card) {
            return;
        }
        if (e.key === "Escape") {
            e.preventDefault();
            if (_dotnet) {
                _dotnet.invokeMethodAsync("DismissFromJs");
            }
            return;
        }
        if (e.key !== "Tab") {
            return;
        }
        const focusables = focusable(_card);
        if (focusables.length === 0) {
            e.preventDefault();
            return;
        }
        const first = focusables[0];
        const last = focusables[focusables.length - 1];
        const active = document.activeElement;
        if (e.shiftKey) {
            if (active === first || !_card.contains(active)) {
                e.preventDefault();
                last.focus();
            }
        } else if (active === last || !_card.contains(active)) {
            e.preventDefault();
            first.focus();
        }
    };
    document.addEventListener("keydown", _trapHandler, true);
}

// Désarme la gestion du focus (overlay fermé ou composant disposé).
export function deactivate() {
    if (_trapHandler) {
        document.removeEventListener("keydown", _trapHandler, true);
    }
    _trapHandler = null;
    _card = null;
    _dotnet = null;
}
