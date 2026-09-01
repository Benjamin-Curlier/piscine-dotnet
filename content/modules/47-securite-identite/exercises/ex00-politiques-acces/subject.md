# ex00 — Policy d'émission

Écris une extension qui configure JwtBearer avec une authority et une audience issues de la
configuration. HTTPS reste obligatoire. Ajoute la policy `send-command`, réservée à une identité
authentifiée possédant le scope `asteria.command.send`, puis applique-la à la route passée en argument.

N'affaiblis ni validation d'issuer/audience ni HTTPS et n'autorise pas l'accès anonyme.
