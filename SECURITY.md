# Politique de sécurité

## Versions prises en charge

Seule la dernière version publiée reçoit des correctifs de sécurité.

| Version            | Prise en charge     |
| ------------------ | ------------------- |
| Dernière *release* | :white_check_mark:  |
| Versions antérieures | :x:               |

## Modèle de confiance de la moulinette

`Piscine.Sandbox` exécute le code corrigé dans un processus enfant jetable et tue son arbre au
timeout. Ce mécanisme protège la disponibilité du processus parent et fiabilise le verdict ; ce
n'est pas un confinement de sécurité Windows/Linux. Le code possède les droits du compte qui lance
la Piscine et peut donc accéder à ses fichiers, au réseau et aux processus autorisés.

L'application est destinée à l'auto-correction du code de l'apprenant. N'y exécute pas une soumission
non fiable provenant d'un tiers. Pour ce cas, lance la Piscine dans une VM, un conteneur durci ou sous
un compte éphémère à privilèges minimaux.

## Signaler une vulnérabilité

Merci de **ne pas** ouvrir d'*issue* publique pour une faille de sécurité.

Signale-la en privé, au choix :

- via l'onglet **Security → Report a vulnerability** du dépôt (*private
  vulnerability reporting* GitHub), ou
- par e-mail à **bencurlier@gmail.com**.

Merci d'inclure une description du problème, les étapes de reproduction et, si
possible, une piste de correctif. Nous nous efforçons d'accuser réception sous
quelques jours et de te tenir informé·e de la prise en charge.
