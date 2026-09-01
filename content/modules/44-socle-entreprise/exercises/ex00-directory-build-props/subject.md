# ex00 — Politique de build partagée

Complète `Directory.Build.props` afin que tous les projets descendants ciblent `net10.0`, activent
nullable, traitent les avertissements comme des erreurs, utilisent le niveau d'analyse
`latest-recommended` et appliquent le style pendant le build.

Ne masque pas les diagnostics avec `NoWarn` et ne désactive pas les avertissements bloquants.
