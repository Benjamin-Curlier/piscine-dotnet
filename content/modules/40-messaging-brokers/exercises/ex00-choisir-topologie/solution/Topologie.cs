using System;

var count = int.Parse(Console.ReadLine()!);
for (var index = 0; index < count; index++)
{
    var choix = Console.ReadLine()! switch
    {
        "diffusion-ephemere" => "NATS_CORE",
        "file-metier-durable" => "RABBITMQ",
        "pair-a-pair-embarque" => "ZEROMQ",
        "stream-rejouable" => "NATS_JETSTREAM",
        _ => "INCONNU"
    };
    Console.WriteLine(choix);
}
