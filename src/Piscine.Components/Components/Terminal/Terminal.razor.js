// Module ESM colocalise du composant Terminal.
//
// Vendorisation : on charge les builds UMD de @xterm/xterm 6.0.0 et @xterm/addon-fit 0.11.0
// (auto-suffisants, sans chunk) vendorises dans la RCL. Choix UMD plutot qu'ESM : un seul
// fichier par paquet, pas de bundler, et le chargement par <script> reste robuste dans une RCL
// servie sous _content/ (les .mjs s'importeraient aussi mais l'UMD evite tout souci de chemin).
//
// Pieges d'API (verifies dans le .tgz npm, ils contredisent l'hypothese du plan) :
//   - xterm UMD recopie ses exports sur le global → window.Terminal est la classe Terminal.
//   - addon-fit UMD fait `e.FitAddon = t()` ou t() EST la classe → window.FitAddon est la CLASSE,
//     PAS window.FitAddon.FitAddon. On gere les deux formes par prudence.

const XTERM_JS = "./_content/Piscine.Components/lib/xterm/xterm.js";
const ADDON_FIT_JS = "./_content/Piscine.Components/lib/xterm/addon-fit.js";

let _libPromise = null;

function loadScript(src) {
    return new Promise((resolve, reject) => {
        // Idempotence : si le script est deja injecte, on ne le recharge pas.
        const existing = document.querySelector(`script[data-piscine-xterm="${src}"]`);
        if (existing) {
            if (existing.dataset.loaded === "true") {
                resolve();
            } else {
                existing.addEventListener("load", () => resolve());
                existing.addEventListener("error", reject);
            }
            return;
        }
        const s = document.createElement("script");
        s.src = src;
        s.dataset.piscineXterm = src;
        s.addEventListener("load", () => { s.dataset.loaded = "true"; resolve(); });
        s.addEventListener("error", reject);
        document.head.appendChild(s);
    });
}

async function ensureLib() {
    if (_libPromise) {
        return _libPromise;
    }
    _libPromise = (async () => {
        if (!window.Terminal) {
            await loadScript(XTERM_JS);
        }
        if (!window.FitAddon) {
            await loadScript(ADDON_FIT_JS);
        }
    })();
    return _libPromise;
}

// addon-fit UMD : window.FitAddon EST la classe ; certaines versions exposent { FitAddon }.
function resolveFitAddonCtor() {
    const g = window.FitAddon;
    if (typeof g === "function") {
        return g;                  // forme observee en 0.11.0 : la classe directement
    }
    if (g && typeof g.FitAddon === "function") {
        return g.FitAddon;         // forme alternative { FitAddon }
    }
    throw new Error("FitAddon introuvable (addon-fit non charge ?).");
}

export async function createTerminal(host, dotnet) {
    await ensureLib();

    const term = new window.Terminal({ convertEol: true, fontSize: 14, cursorBlink: true });
    const FitAddonCtor = resolveFitAddonCtor();
    const fit = new FitAddonCtor();
    term.loadAddon(fit);
    term.open(host);
    fit.fit();

    term.onData(d => dotnet.invokeMethodAsync("HandleData", d));

    const pushResize = () => {
        fit.fit();
        dotnet.invokeMethodAsync("HandleResize", term.cols, term.rows);
    };
    const ro = new ResizeObserver(pushResize);
    ro.observe(host);
    pushResize(); // dimensions initiales

    return { term, fit, ro };
}

export function write(handle, base64) {
    const bin = atob(base64);
    const bytes = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) {
        bytes[i] = bin.charCodeAt(i);
    }
    handle.term.write(bytes);
}

export function dispose(handle) {
    try { handle.ro.disconnect(); } catch { /* deja deconnecte */ }
    try { handle.term.dispose(); } catch { /* deja dispose */ }
}
