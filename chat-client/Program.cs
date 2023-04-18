using System.Net;
using System.Net.Sockets;
using System.Text;

class Client {
    private const int PORT = 9000;
    private Socket socket;
    public Client(IPAddress ip) {
        socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(ip, PORT);
        Console.WriteLine($"Connected to {ip}:{PORT}");
    }
    private void Send(string data) {
        Console.WriteLine($"{data}");
        socket.Send(Encoding.ASCII.GetBytes(data));
    }
    public void Connect(string username) {
        Send($"CONNECT|{username}|");
    }
    public void Say(string message) {
        Send($"SAY|{message}|");
    }
    public void Exit() {
        Send($"EXIT|");
    }
}

class Program {
    static void Main(string[] args) {
        if (args.Length != 2) {
            Console.WriteLine("usage: dotnet run <ip> <username>");
            return;
        }
        Client client = new Client(IPAddress.Parse(args[0]));
        client.Connect(args[1]);
    }
}