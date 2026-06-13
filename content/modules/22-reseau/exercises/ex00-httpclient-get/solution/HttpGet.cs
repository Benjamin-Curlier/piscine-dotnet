using System.Net.Http;

var baseUrl = args[0];
var path = args[1];
using var client = new HttpClient();
var body = await client.GetStringAsync(baseUrl + path);
System.Console.Write(body);
