using System;
using System.IO;
using System.Net.Sockets;

// args[0] = hôte du serveur d'écho (ex. "127.0.0.1"), args[1] = port (ex. "54321").
var host = args[0];
var port = int.Parse(args[1]);

// TODO : connecte-toi au serveur avec un TcpClient (ConnectAsync) et récupère son NetworkStream.
// TODO : pour chaque ligne lue sur l'entrée standard (Console.ReadLine, jusqu'à null) :
//        envoie-la au serveur, lis l'écho renvoyé, puis affiche-le.
// Pense à libérer le TcpClient avec `using`, et à travailler en async/await.
