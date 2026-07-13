using System;
using System.IO;
using System.Net.Sockets;

var host = args[0];
var port = int.Parse(args[1]);

// On se connecte au serveur d'écho (même adresse, même port).
using var client = new TcpClient();
await client.ConnectAsync(host, port);

// Le NetworkStream est le tuyau : on lit et on écrit dessus.
using var flux = client.GetStream();
var lecteur = new StreamReader(flux);
var ecrivain = new StreamWriter(flux) { AutoFlush = true, NewLine = "\n" };

// Pour chaque ligne de l'entrée standard : on l'envoie, on lit l'écho, on l'affiche.
string? ligne;
while ((ligne = Console.ReadLine()) is not null)
{
    await ecrivain.WriteLineAsync(ligne);
    var echo = await lecteur.ReadLineAsync();
    Console.WriteLine(echo);
}
