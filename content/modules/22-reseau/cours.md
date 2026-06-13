# Module 22 — Réseau : sockets, TCP/UDP et HttpClient

Jusqu'ici, tes programmes tournaient **seuls**, sur ta machine. Mais l'informatique d'aujourd'hui
est **connectée** : un navigateur parle à un serveur web, une application mobile interroge une API,
deux jeux échangent des messages en partie multijoueur. Tout cela passe par le **réseau**.

Dans ce module, tu vas découvrir **comment un programme C# communique avec un autre programme**, qu'il
soit sur la même machine ou à l'autre bout du monde. On commencera par les fondations (les
**sockets**), puis on montera vers des outils plus simples et plus courants (**`HttpClient`**).

> **Module mixte.** On **lit**, on **comprend**, et on pratique : la checklist de fin de cours
> s'explore en local. L'exercice auto-noté (`ex00-httpclient-get`) porte sur la section 4 —
> `HttpClient` — et est corrigé par le harnais HTTP du moteur.

## 1. Le modèle réseau en deux minutes

Quand deux programmes communiquent, l'un joue le rôle de **serveur** et l'autre celui de **client** :

- Le **serveur** attend, à une adresse connue, que des clients se présentent. Il *écoute*.
- Le **client** prend l'initiative : il se *connecte* au serveur et lui envoie une demande.

C'est exactement comme un restaurant : le restaurant (serveur) a une **adresse** fixe et attend ; le
client se déplace jusqu'à cette adresse pour passer commande.

### Adresse IP et port

Pour joindre un programme sur le réseau, il faut **deux informations** :

- l'**adresse IP** : elle identifie la **machine** (ex. `192.168.1.10`, ou `127.0.0.1` qui désigne
  *ta propre machine*, aussi appelée *localhost*) ;
- le **port** : un nombre entre 0 et 65535 qui identifie **quel programme** sur cette machine (ex.
  `80` pour le web HTTP, `443` pour HTTPS). Plusieurs programmes peuvent tourner sur une même machine,
  le port permet de savoir auquel on parle.

> **Jargon — `localhost` / `127.0.0.1`.** C'est l'adresse de « chez toi ». Un client et un serveur
> lancés sur le **même PC** peuvent dialoguer via `127.0.0.1` sans aucune connexion Internet. C'est
> parfait pour s'entraîner, et c'est ce qu'on fera dans ce module.

### TCP vs UDP

Une fois l'adresse et le port connus, il reste à choisir **comment** transporter les données. Deux
grands protocoles existent :

- **TCP** (*Transmission Control Protocol*) : c'est une communication **fiable** et **ordonnée**. On
  établit d'abord une **connexion** (comme un coup de fil : on décroche, puis on parle). TCP garantit
  que **tous** les octets arrivent, **dans le bon ordre**. Idéal pour le web, les fichiers, le chat.
- **UDP** (*User Datagram Protocol*) : c'est l'envoi de petits messages indépendants (**datagrammes**),
  **sans connexion** préalable et **sans garantie** : un message peut se perdre ou arriver dans le
  désordre. En échange, c'est **léger et rapide**. Utilisé pour le jeu vidéo, la voix, la vidéo en
  direct, où on préfère la vitesse à la perfection.

| | TCP | UDP |
|---|---|---|
| Connexion | Oui (établie avant) | Non |
| Fiabilité | Garantie (tout arrive, en ordre) | Aucune (peut se perdre) |
| Image | Un coup de téléphone | Une carte postale |
| Usage typique | Web, fichiers, chat | Jeux, voix, streaming |

### Le socket

Le **socket** (« prise » en anglais) est le point de branchement entre ton programme et le réseau.
C'est l'objet logiciel par lequel tu **envoies** et **reçois** des octets. En C#, tu manipules
rarement le socket brut directement : la bibliothèque te fournit des classes plus confortables
(`TcpListener`, `TcpClient`, `UdpClient`…) qui s'appuient dessus. Toutes vivent dans :

```csharp
using System.Net;            // adresses IP, IPEndPoint
using System.Net.Sockets;    // TcpListener, TcpClient, UdpClient, NetworkStream
```

## 2. Sockets TCP : un mini serveur et un mini client

En TCP, le serveur **écoute** avec un `TcpListener`, et le client **se connecte** avec un `TcpClient`.
Une fois la connexion établie, chacun obtient un **`NetworkStream`** : un flux d'octets par lequel on
lit et on écrit, exactement comme on lit/écrit dans un fichier.

> **Rappel (module 12).** Les opérations réseau sont **lentes** (on attend l'autre machine). On les
> écrit donc en **asynchrone** avec `async`/`await`. Tu retrouveras `await`, `Task` et les méthodes
> en `...Async`. Si ce n'est pas clair, relis le module 12.

### Le serveur d'écho (côté serveur)

Un serveur **d'écho** est l'exemple classique : il renvoie au client exactement ce qu'il a reçu. Petit
mais complet, il montre toutes les étapes.

```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// 1. On écoute sur localhost (127.0.0.1), port 5000.
var listener = new TcpListener(IPAddress.Loopback, 5000);
listener.Start();
Console.WriteLine("Serveur en écoute sur 127.0.0.1:5000...");

// 2. On attend (sans bloquer) qu'un client se connecte.
using TcpClient client = await listener.AcceptTcpClientAsync();
Console.WriteLine("Client connecté !");

// 3. Le NetworkStream est le tuyau pour lire/écrire des octets.
using NetworkStream flux = client.GetStream();

// 4. On lit ce que le client envoie (jusqu'à 1024 octets ici).
var tampon = new byte[1024];
int nbLus = await flux.ReadAsync(tampon, 0, tampon.Length);
string message = Encoding.UTF8.GetString(tampon, 0, nbLus);
Console.WriteLine($"Reçu : {message}");

// 5. On renvoie le même message (l'« écho »).
byte[] reponse = Encoding.UTF8.GetBytes(message);
await flux.WriteAsync(reponse, 0, reponse.Length);

// 6. On arrête d'écouter. (client et flux sont fermés par le `using`.)
listener.Stop();
```

Décortiquons les points importants :

- **`IPAddress.Loopback`** vaut `127.0.0.1` : on reste sur la machine locale. Pour écouter sur toutes
  les interfaces réseau, on utiliserait `IPAddress.Any`.
- **`AcceptTcpClientAsync()`** *bloque logiquement* jusqu'à ce qu'un client arrive — mais grâce à
  `await`, le thread n'est pas gelé.
- Sur le réseau, on n'échange **que des octets** (`byte[]`). Le texte doit donc être **encodé** en
  octets à l'envoi et **décodé** à la réception. C'est le rôle de `Encoding.UTF8` (rappel : UTF-8 est
  l'encodage de texte standard).
- **`ReadAsync` renvoie le nombre d'octets réellement lus.** Il faut l'utiliser : `tampon` fait peut-être
  1024 octets, mais le message n'en remplit que quelques-uns. On décode `nbLus` octets, pas 1024.

### Le client (côté client)

```csharp
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// 1. On se connecte au serveur (même adresse, même port).
using var client = new TcpClient();
await client.ConnectAsync("127.0.0.1", 5000);

// 2. Même tuyau : un NetworkStream.
using NetworkStream flux = client.GetStream();

// 3. On envoie un message (encodé en octets).
byte[] message = Encoding.UTF8.GetBytes("Bonjour serveur");
await flux.WriteAsync(message, 0, message.Length);

// 4. On lit la réponse (l'écho).
var tampon = new byte[1024];
int nbLus = await flux.ReadAsync(tampon, 0, tampon.Length);
Console.WriteLine($"Réponse : {Encoding.UTF8.GetString(tampon, 0, nbLus)}");
```

### Fermer proprement avec `using`

Une connexion réseau est une **ressource système** : tant qu'elle est ouverte, elle consomme un port
et de la mémoire. Si on oublie de la fermer, on **fuit** des ressources. La règle d'or :

> **Toujours libérer un `TcpClient` / `TcpListener` / `NetworkStream` avec `using`.** Le mot-clé
> `using` garantit que la connexion est **fermée automatiquement**, même en cas d'erreur.

Deux écritures équivalentes existent :

```csharp
// Forme « using declaration » : fermé à la fin du bloc englobant.
using var client = new TcpClient();

// Forme « using block » : fermé à la fin des accolades.
using (var client = new TcpClient())
{
    // ... utilisation ...
}   // <- ici, client est fermé/libéré
```

Sans `using`, il faudrait penser à appeler `client.Dispose()` (ou `Close()`) à la main, ce qui est
fragile. Laisse `using` s'en charger.

## 3. UDP : envoyer des datagrammes, sans connexion

Avec UDP, **pas de connexion** ni de `NetworkStream` : on envoie directement des **datagrammes** (des
petits paquets d'octets) à une adresse et un port. La classe est **`UdpClient`**.

### Côté récepteur (un « serveur » UDP)

```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// On écoute les datagrammes arrivant sur le port 5001.
using var recepteur = new UdpClient(5001);
Console.WriteLine("En attente d'un datagramme sur le port 5001...");

// ReceiveAsync renvoie les octets reçus ET l'adresse de l'expéditeur.
UdpReceiveResult resultat = await recepteur.ReceiveAsync();
string message = Encoding.UTF8.GetString(resultat.Buffer);
Console.WriteLine($"Reçu de {resultat.RemoteEndPoint} : {message}");
```

### Côté émetteur

```csharp
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using var emetteur = new UdpClient();
byte[] datagramme = Encoding.UTF8.GetBytes("Coucou en UDP");

// On envoie directement, sans se « connecter » d'abord.
await emetteur.SendAsync(datagramme, datagramme.Length, "127.0.0.1", 5001);
```

Remarque les différences avec TCP :

- **aucun `Accept`, aucun `Connect`** obligatoire : on envoie « dans le vide », vers une adresse ;
- **pas de garantie** : si le récepteur n'est pas là ou si le paquet se perd, l'émetteur ne le saura
  pas. C'est le prix de la légèreté ;
- chaque `Send` correspond (en général) à **un datagramme** ; on raisonne par messages, pas par flux
  continu comme en TCP.

## 4. `HttpClient` : parler à des sites et des API web

Les sockets, c'est la fondation. Mais dans la vraie vie, on consomme surtout des **API web** en
**HTTP**, le protocole du web (au-dessus de TCP). Plutôt que de bricoler des octets, .NET fournit une
classe haut niveau et confortable : **`HttpClient`**.

```csharp
using System.Net.Http;
using System.Threading.Tasks;
```

### Une requête GET {#httpclient-get}

`GET` sert à **récupérer** une ressource (une page, des données JSON…).

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;

var client = new HttpClient();

// Récupère le corps de la réponse sous forme de chaîne.
string contenu = await client.GetStringAsync("https://api.github.com/zen");
Console.WriteLine(contenu);
```

`GetStringAsync` fait tout le travail : ouvrir la connexion, envoyer la requête, lire la réponse et la
renvoyer en `string`. Tu retrouves `await` : un appel réseau est lent, donc asynchrone.

Pour avoir plus de contrôle (code de statut, en-têtes…), on utilise `GetAsync` qui renvoie un
**`HttpResponseMessage`** :

```csharp
HttpResponseMessage reponse = await client.GetAsync("https://example.com");
reponse.EnsureSuccessStatusCode();           // lève une exception si le statut n'est pas 2xx
string corps = await reponse.Content.ReadAsStringAsync();
Console.WriteLine($"Statut : {(int)reponse.StatusCode}");
```

### Une requête POST

`POST` sert à **envoyer** des données au serveur (créer une ressource, soumettre un formulaire…). On
joint un **corps de requête**, souvent du **JSON** (rappel module 16).

```csharp
using System.Net.Http;
using System.Net.Http.Json;   // extensions PostAsJsonAsync / ReadFromJsonAsync
using System.Threading.Tasks;

var client = new HttpClient();

var nouvelle = new { Titre = "Apprendre le réseau", Termine = false };

// PostAsJsonAsync sérialise l'objet en JSON et l'envoie en POST.
HttpResponseMessage reponse = await client.PostAsJsonAsync("https://exemple/api/taches", nouvelle);
reponse.EnsureSuccessStatusCode();
```

> **Lien avec le module 16 — la sérialisation sur le fil.** Sur le réseau, on n'échange que des
> octets. Pour transmettre un **objet**, on le **sérialise** (le plus souvent en JSON) à l'envoi, et on
> le **désérialise** à la réception. C'est exactement la sérialisation vue au module 16, appliquée à la
> communication. `System.Net.Http.Json` automatise ce va-et-vient : `PostAsJsonAsync` sérialise pour
> toi, et `ReadFromJsonAsync<T>` reconstruit l'objet :

```csharp
using System.Net.Http;
using System.Net.Http.Json;

// Récupère du JSON et le transforme directement en objet C#.
Tache? t = await client.GetFromJsonAsync<Tache>("https://exemple/api/taches/1");

class Tache
{
    public string Titre { get; set; } = string.Empty;
    public bool Termine { get; set; }
}
```

### Bonne pratique capitale : ne crée PAS un `HttpClient` par requête

C'est l'erreur de débutant la plus fréquente, et elle a de vraies conséquences :

```csharp
// MAUVAIS : un nouvel HttpClient à chaque appel.
for (var i = 0; i < 100; i++)
{
    using var client = new HttpClient();          // <- à NE PAS faire
    var s = await client.GetStringAsync("https://exemple/api");
}
```

Pourquoi c'est mauvais ? Chaque `HttpClient` ouvre des connexions réseau sous le capot. En en créant à
la chaîne, on **épuise les sockets disponibles** (un problème connu sous le nom de *socket exhaustion*)
et les performances s'effondrent. La règle :

> **Un `HttpClient` est conçu pour être réutilisé.** On en crée **un seul** et on s'en sert pour
> **toutes** les requêtes pendant la vie de l'application.

Dans un vrai projet (ASP.NET Core, applications structurées), la bonne réponse est **`IHttpClientFactory`** :
elle gère pour toi le cycle de vie et le recyclage des connexions.

```csharp
// Dans une application configurée avec l'injection de dépendances (rappel module 18).
public class ServiceMeteo(IHttpClientFactory fabrique)
{
    public async Task<string> LireAsync()
    {
        HttpClient client = fabrique.CreateClient();   // la fabrique gère les connexions
        return await client.GetStringAsync("https://exemple/api/meteo");
    }
}
```

Retiens les deux niveaux : **pour t'entraîner**, un seul `HttpClient` partagé suffit ; **dans un vrai
projet**, on passe par `IHttpClientFactory`.

## 5. Checklist pratique (à faire sur ta machine)

Ces exercices ne sont **pas notés** : ils se pratiquent **en local** pour comprendre par l'expérience.
Crée un petit projet console et essaie.

**A. Mini écho TCP (serveur + client) en local.**

1. Crée **deux** programmes console (deux dossiers de projet) : un `serveur`, un `client`.
2. Dans le `serveur`, recopie le code de la section 2 (écoute sur `127.0.0.1:5000`, lit, renvoie l'écho).
3. Dans le `client`, recopie le code client (se connecte, envoie un message, affiche la réponse).
4. **Lance d'abord le serveur**, puis le client (dans deux terminaux). Vérifie que le client reçoit bien
   l'écho de son message.
5. Bidouille : change le message, fais répondre le serveur en MAJUSCULES (`message.ToUpper()`), gère
   plusieurs lignes dans une boucle.

**B. Une requête GET vers une API publique.**

1. Dans un programme console, crée **un seul** `HttpClient`.
2. Fais `await client.GetStringAsync(...)` vers une API publique simple, par exemple
   `https://api.github.com/zen` (renvoie une courte phrase) ou un service de test comme
   `https://httpbin.org/get`.
3. Affiche la réponse. Observe que c'est du **texte** (souvent du **JSON**).
4. Pour aller plus loin : récupère du JSON et désérialise-le en objet avec `GetFromJsonAsync<T>`
   (lien direct avec le module 16).

> **Astuce.** Si une connexion échoue, vérifie dans l'ordre : le serveur est-il **bien lancé** ? le
> **port** est-il le même des deux côtés ? un **pare-feu** ne bloque-t-il pas ? Pour HTTP, l'URL est-elle
> correcte (`https://`) et le site accessible ?

## En résumé

- Le réseau, c'est un **client** qui se connecte à un **serveur**, identifié par une **adresse IP** et
  un **port**.
- **TCP** = fiable, ordonné, avec connexion (`TcpListener` + `TcpClient` + `NetworkStream`). **UDP** =
  léger, sans connexion, sans garantie (`UdpClient`, datagrammes).
- Sur le réseau on n'échange que des **octets** : on **encode/décode** le texte (`Encoding.UTF8`) et on
  **sérialise/désérialise** les objets (JSON, module 16).
- Les appels réseau sont **lents** : ils s'écrivent en **`async`/`await`** (module 12).
- Pour le web, **`HttpClient`** simplifie tout : `GetStringAsync`, `GetAsync`, `PostAsJsonAsync`. **Ne
  crée pas** un `HttpClient` par requête : réutilise-en un, ou utilise **`IHttpClientFactory`**.
- **Ferme toujours** tes connexions avec **`using`**.

## Références externes

- Microsoft Learn — *Socket : communication réseau de bas niveau* :
  <https://learn.microsoft.com/fr-fr/dotnet/fundamentals/networking/sockets/socket-services>
- Microsoft Learn — *Écrire un client/serveur TCP avec TcpListener et TcpClient* :
  <https://learn.microsoft.com/fr-fr/dotnet/fundamentals/networking/sockets/tcp-classes>
- Microsoft Learn — *Effectuer des requêtes HTTP avec la classe HttpClient* :
  <https://learn.microsoft.com/fr-fr/dotnet/fundamentals/networking/http/httpclient>
- Microsoft Learn — *Recommandations sur l'utilisation de HttpClient* :
  <https://learn.microsoft.com/fr-fr/dotnet/fundamentals/networking/http/httpclient-guidelines>
- Vidéo — Nick Chapsas, *How to use HttpClient properly in .NET* :
  <https://www.youtube.com/watch?v=Z6Y2adsMnAA>
