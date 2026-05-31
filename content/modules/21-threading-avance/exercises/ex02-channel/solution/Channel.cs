using System.Threading.Channels;
using System.Threading.Tasks;

var n = int.Parse(System.Console.ReadLine());

var channel = System.Threading.Channels.Channel.CreateUnbounded<int>();

for (var i = 0; i < n; i++)
{
    var x = int.Parse(System.Console.ReadLine());
    await channel.Writer.WriteAsync(x);
}
channel.Writer.Complete();

await foreach (var x in channel.Reader.ReadAllAsync())
{
    System.Console.WriteLine(x * 2);
}
