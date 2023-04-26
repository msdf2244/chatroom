using System.Net;
using System.Net.Sockets;
using System.Text;

class Client {
    public int scroll = 0;
    private const int PORT = 9000;
    private Socket socket;
    public string Name;
    public Client(IPAddress ip, string username) {
        socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(ip, PORT);
        this.Name = username;
        Console.WriteLine($"Connected to {ip}:{PORT}");
    }
    private void Send(string data) {
        socket.Send(Encoding.ASCII.GetBytes(data));
    }
    public void Connect() {
        Send($"CONNECT|{Name}|");
    }
    public void Say(string message) {
        Send($"SAY|{message}|");
    }
    public void Exit() {
        Send($"EXIT|");
        socket.Close();
    }
    public void Close() {
        socket.Close();
    }
    public string[] ParseNextRequest() {
        byte[] buffer = new byte[1024];
        socket.Receive(buffer); 
        string data = Encoding.ASCII.GetString(buffer);
        string[] fields = data.Split("|");
        fields[0] = fields[0].ToUpper();
        return fields;
    }
    public bool HandleResponse() {
        Console.SetCursorPosition(0, Math.Max(scroll - 1,0));
        string[] fields = ParseNextRequest();
        switch (fields[0].ToUpper()) {
            case "CONNECTED":
                Console.WriteLine($"Joined chat as {fields[1]}");
                break;
            case "REJECTED":
                Console.WriteLine($"Join rejected: {fields[2]}");
                return false;
            case "PUBLIC":
                Console.WriteLine($"[{fields[1]}] {fields[2]}");
                break;
            case "JOINED":
                Console.WriteLine($"{fields[1]} joined the chat");
                break;
            case "LEFT":
                Console.WriteLine($"{fields[1]} left the chat");
                break;
            case "ERROR":
                Console.WriteLine($"Unknown command: {fields[1]}");
                break;
        }
        return true;
    }
}

class Program {
    static void UiHandler(object? obj) {
        if (obj == null) {
            throw new NullReferenceException("Client should not be null");
        }
        Client client = (Client) obj;
        while (true) {
            Console.Write($"{client.Name}> ");
            string? line = Console.ReadLine();
            if (line == "" || line == null) {
                break;
            }
            while (line.Contains("|")) {
                line.Remove('|');
            }
            client.Say(line);
            client.scroll +=1;
        }
        client.Exit();
    }
    static void Listener(object? obj) {
        if (obj == null) {
            throw new NullReferenceException("Client should not be null");
        }
        Client client = (Client) obj;
        while (client.HandleResponse()) {
            client.scroll+=1;
        }
        client.Close();
    }
    static void Main(string[] args) {
        Console.Clear();
        if (args.Length != 2) {
            Console.WriteLine("usage: dotnet run <ip> <username>");
            return;
        }
        Client client = new Client(IPAddress.Parse(args[0]), args[1]);
        client.Connect();
        Thread uiThread = new Thread(new ParameterizedThreadStart(UiHandler));
        uiThread.Start(client);
        Thread listenerThread = new Thread(new ParameterizedThreadStart(Listener));
        listenerThread.Start(client);
    }
}