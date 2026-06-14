// Helpers d'export de la page de rapport (S5).
// - print()    : ouvre le dialogue d'impression du navigateur (feuille @media print → PDF/papier).
// - copy(text) : copie le Markdown dans le presse-papiers ; renvoie true/false selon le succès.
// - download(name, text) : déclenche un téléchargement du Markdown via un blob + lien temporaire.

export function print() {
    window.print();
}

export async function copy(text) {
    try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            await navigator.clipboard.writeText(text);
            return true;
        }
    } catch (e) {
        // Repli ci-dessous (presse-papiers indisponible / refusé dans le webview).
    }

    try {
        const ta = document.createElement('textarea');
        ta.value = text;
        ta.setAttribute('readonly', '');
        ta.style.position = 'absolute';
        ta.style.left = '-9999px';
        document.body.appendChild(ta);
        ta.select();
        const ok = document.execCommand('copy');
        document.body.removeChild(ta);
        return ok;
    } catch (e) {
        return false;
    }
}

export function download(name, text) {
    const blob = new Blob([text], { type: 'text/markdown;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = name;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}
