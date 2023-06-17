using System.Net;
using System.Net.Sockets;

TcpServer server = new();
await server.ListenAsync();

class TcpServer
{
    TcpListener listener = new(IPAddress.Any, 7777);
    List<TcpUser> users = new List<TcpUser>();

    public async Task ListenAsync()
    {
        try
        {
            listener.Start();
            Console.WriteLine($"Server start...");
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                TcpUser user = new TcpUser(client, this);
                users.Add(user);
                Task.Run(user.ProcessAsync);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            Disconnect();
        }
    }

    public void Disconnect()
    {
        foreach (var user in users)
            user.Close();
        listener.Stop();
    }

    public void RemoveUser(string id)
    {
        TcpUser? user = users.FirstOrDefault(u => u.Id == id);
        if (user != null) users.Remove(user);
        user?.Close();
    }

    public async Task BroadcastMessageAsync(string message, string id)
    {
        foreach(var user in users)
        {
            if(user.Id != id)
            {
                await user.Writer.WriteLineAsync(message);
                await user.Writer.FlushAsync();
            }
        }
    }
}

class TcpUser
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public StreamReader Reader { get; }
    public StreamWriter Writer { get; }

    TcpClient client;
    TcpServer server;

    public TcpUser(TcpClient client, TcpServer server)
    {
        this.client = client;
        this.server = server;
        var stream = client.GetStream();
        Reader = new StreamReader(stream);
        Writer = new StreamWriter(stream);
    }

    public async Task ProcessAsync()
    {
        try
        {
            string? name = await Reader.ReadLineAsync();
            string? message = $"User {name} join to chat";
            await server.BroadcastMessageAsync(message, Id);
            Console.WriteLine(message);

            while(true)
            {
                try
                {
                    message = await Reader.ReadLineAsync();
                    if (message == null) continue;
                    message = $"{name}: {message}";
                    Console.WriteLine(message);
                    await server.BroadcastMessageAsync(message, Id);
                }
                catch
                {
                    message = $"User {name} leave chat";
                    await server.BroadcastMessageAsync(message, Id);
                    Console.WriteLine(message);
                    break;
                }
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            server.RemoveUser(Id);
        }
    }

    public void Close()
    {
        Writer.Close();
        Reader.Close();
        client.Close();
    }
}