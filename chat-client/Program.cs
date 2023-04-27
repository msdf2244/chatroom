using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using Terminal.Gui;

class Client {
    public int scroll = 0;
    private const int PORT = 9000;
    private Socket socket;
    public string Name;
    public TextView Log;
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
    public void List() {
        Send($"LIST|");
    }
    public void Time() {
        Send($"TIME|");
    }
    public void Exit() {
        Send($"EXIT|");
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
    public void HandleResponse() {
        string[] fields = ParseNextRequest();
        Application.MainLoop.Invoke(() => {
            switch (fields[0].ToUpper()) {
                case "CONNECTED":
                    Log.Text += $"Joined chat as {fields[1]}\n";
                    break;
                case "REJECTED":
                    Log.Text += $"Join rejected: {fields[2]}. If username is taken, press C-Q and try with a new name\n";
                    return;
                case "PUBLIC":
                    Log.Text += $"[{fields[1]}] {fields[2]}\n";
                    break;
                case "JOINED":
                    Log.Text += $"{fields[1]} joined the chat\n";
                    break;
                case "LEFT":
                    Log.Text += $"{fields[1]} left the chat\n";
                    break;
                case "ERROR":
                    Log.Text += $"Unknown command: {fields[1]}\n";
                    break;
                case "LIST":
                    Log.Text += $"List of connected users: {fields[1]}\n";
                    break;
                case "TIME":
                    Log.Text += $"Currnet server time: {fields[1]}\n";
                    break;
            }
            return;
        });
    }
}

class Program {
    static void Listener(object? obj) {
        if (obj == null) {
            throw new NullReferenceException("Client should not be null");
        }
        Client client = (Client) obj;
        while (true) {
            client.HandleResponse();
        }
        client.Exit();
    }
    static void SetupGui(Client client) {
        Application.Init();
        var messages = new Window("Messages") {
            X = 0,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        var input = new Window("input") {
            X = 0,
            Y = 0,  
            Width = Dim.Fill(),
            Height = 3,
        };
        var text_area = new TextField() {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        input.Add(text_area);
        var log = new TextView() {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        messages.Add(log);
        client.Log = log;
        log.Text += "Welcome to the chatroom. Press C-Q to quit.\n";
        log.ReadOnly = true;
        text_area.KeyUp += (e) => {
            if (e.KeyEvent.Key == Key.Enter && text_area.Text != "") {
                log.Text += $"<ME> {text_area.Text}\n";
                client.Say(text_area.Text.ToString());
                text_area.Text= "";
            }
        };
        Application.Top.Add(input, messages);
    }
    static void Main(string[] args) {
        if (args.Length != 2) {
            Console.WriteLine("usage: dotnet run <ip> <username>");
            return;
        }
        Client client = new Client(IPAddress.Parse(args[0]), args[1]);
        client.Connect();
        SetupGui(client);
        Thread thread = new Thread(new ParameterizedThreadStart(Listener));
        thread.Start(client);
        Application.Run();
        client.Exit();
        Application.Shutdown();
    }
}
