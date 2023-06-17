using System.Net.Sockets;
using System.Net;

var host = IPAddress.Loopback;
var port = 7777;

using TcpClient client = new();
Console.Write("Input name, please: ");
string? name = Console.ReadLine();
Console.WriteLine($"Welcome to chat {name}");
StreamReader? reader = null;
StreamWriter? writer = null;

try
{
    client.Connect(host, port);
    reader = new StreamReader(client.GetStream());
    writer = new StreamWriter(client.GetStream());
    
    if (reader is null || writer is null) return;

    Task.Run(() => ReceiveMessageAsync(reader));
    await SendMessageAsync(writer);
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
}

reader?.Close();
writer?.Close();




async Task SendMessageAsync(StreamWriter writer)
{
    await writer.WriteLineAsync(name);
    await writer.FlushAsync();

    Console.WriteLine("Input messages and Enter for send");

    while(true)
    {
        string message = Console.ReadLine();
        await writer.WriteLineAsync(message);
        await writer.FlushAsync();
    }
}

async Task ReceiveMessageAsync(StreamReader reader)
{
    while(true)
    {
        try
        {
            string? message = await reader.ReadLineAsync();
            if (String.IsNullOrEmpty(message)) continue;
            Console.WriteLine(message);
        }
        catch
        {
            break;
        }
    }
}