using System.Net;
using System.Net.Sockets;
using System.Text;

public class ChatClient
{
    private TcpClient client;
    private StreamReader reader;
    private StreamWriter writer;
    private string name;

    public ChatClient(string name, string serverIp, int serverPort)
    {
        client = new TcpClient(serverIp, serverPort);
        reader = new StreamReader(client.GetStream(), Encoding.ASCII);
        writer = new StreamWriter(client.GetStream(), Encoding.ASCII);
        this.name = name;
    }

    public void Run()
    {
        // Send connect packet
        writer.WriteLine("CONNECT|{0}|", name);
        writer.Flush();

        while (true)
        {
            string line = reader.ReadLine();
            if (line == null)
            {
                break;
            }

            // Parse server response
            string[] parts = line.Split('|');
            if (parts.Length == 0)
            {
                continue;
            }

            switch (parts[0].ToUpper())
            {
                case "CONNECTED":
                    Console.WriteLine("Joined chat as {0}", parts[1]);
                    break;
                case "REJECTED":
                    Console.WriteLine("Join rejected: {0}", parts[2]);
                    break;
                case "PUBLIC":
                    Console.WriteLine("{0}: {1}", parts[1], parts[2]);
                    break;
                case "JOINED":
                    Console.WriteLine("{0} joined the chat", parts[1]);
                    break;
                case "LEFT":
                    Console.WriteLine("{0} left the chat", parts[1]);
                    break;
                case "ERROR":
                    Console.WriteLine("Unknown command: {0}", parts[1]);
                    break;
            }
        }

        // Send exit packet
        writer.WriteLine("EXIT|");
        writer.Flush();

        // Close the client socket
        client.Close();
    }

    public void Send(string message)
    {
        // Send message packet
        writer.WriteLine("SAY|{0}|", message);
        writer.Flush();
    }
}

class Client {
    private const int PORT = 9000;
    private Socket socket;
    public string username;
    public Client(IPAddress ip, string username) {
        socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(ip, PORT);
        this.username = username;
        Console.WriteLine($"Connected to {ip}:{PORT}");
    }
    private void Send(string data) {
        Console.WriteLine($"{data}");
        socket.Send(Encoding.ASCII.GetBytes(data));
    }
    public void Connect() {
        Send($"CONNECT|{username}|");
    }
    public void Say(string message) {
        Send($"SAY|{message}|");
    }
    public void Exit() {
        Send($"EXIT|");
        socket.Close();
    }
}

class Program {
    static void Main(string[] args) {
        if (args.Length != 2) {
            Console.WriteLine("usage: dotnet run <ip> <username>");
            return;
        }
        Client client = new Client(IPAddress.Parse(args[0]), args[1]);
        client.Connect();
        while (true) {
            string line = Console.ReadLine();
            if (line == "" || line == null) {
                break;
            }
            while (line.Contains("|")) {
                line.Remove('|');
            }
            client.Say(line);
        }
        client.Exit();
    }
}