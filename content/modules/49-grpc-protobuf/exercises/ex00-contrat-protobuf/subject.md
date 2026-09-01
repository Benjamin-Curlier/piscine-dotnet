# ex00 — Schéma versionné

Complète `commands.proto` : package `asteria.commands.v1`, namespace C#, service `CommandService`,
appel unaire `Submit` et streaming serveur `Watch`.

La requête contient la clé d'idempotence au champ 1, le type au champ 2 et le payload au champ 3.
Réserve le numéro 4 et le nom `legacy_operator` retirés d'une ancienne version.
