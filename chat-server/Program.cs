using System.Net;
using System.Net.Sockets;
using System.Text;

class Server {
    const int MAX_MESSAGE_LENGTH = 200;
    const int MAX_NAME_LENGTH = 50;

    public class User {
        public Socket Connection;
        public string Name;
        public User(Socket connection, string name) {
            this.Connection = connection;
            this.Name = name;
        }
        public void Send(string data) {
            Connection.Send(Encoding.ASCII.GetBytes(data));
        }
    }

    private const int MAXLINE = 1024;
    private const int PORT = 9000;

    private Socket socket;
    public List<User> Users = new List<User>();
		private IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, PORT);

    public Server() {
        socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(ipEndPoint);
    }

    private string[] ParseNextRequest(Socket connection) {
        byte[] buffer = new byte[MAXLINE];
        connection.Receive(buffer); 
        string data = Encoding.ASCII.GetString(buffer);
        string[] fields = data.Split("|");
        fields[0] = fields[0].ToUpper();
        return fields;
    }

    private void HandleUser(object? obj) {
        User user = (User) obj;
        string[] fields = ParseNextRequest(user.Connection);
        string command = fields[0];
        while (true) {
            switch (command) {
                case "SAY": {
                    string message = fields[1];
                    if (message.Length > MAX_MESSAGE_LENGTH) {
                        user.Send($"REJECTED|{user.Name}|Message is too long|");
                        break;
                    }
                    Console.WriteLine($"[{user.Name}]  {message}");
                    foreach (User otherUser in Users) {
                        if (otherUser != user) {
                            otherUser.Send($"PUBLIC|{user.Name}|{message}|");
                        }
                    }
                    break;
                }
                case "EXIT": {
                    Console.WriteLine($"User [{user.Name}] left.");
                    Users.Remove(user);
                    foreach (User otherUser in Users) {
                        otherUser.Send($"LEFT|{user.Name}|");
                    }
                    user.Connection.Close();
                    return;
                }
                case "LIST": {
                    int count = Users.Count;
                    StringBuilder userList = new StringBuilder();
                    foreach (User otherUser in Users) {
                        userList.Append(otherUser.Name + "|");
                    }
                    Console.WriteLine($"[{user.Name}] requested user list.");
                    user.Send($"LIST|{count}|{userList.ToString()}");
                    break;
                }
                case "TIME": {
                    DateTime now = DateTime.Now;
                    string timeString = now.ToString("yyyy-MM-dd HH:mm::ss");
                    Console.WriteLine($"[{user.Name}] requested the time");
                    user.Send($"TIME|{timeString}|");
                    break;
                }
                case "PRIVATE": {
                    string name = fields[1];
                    string message = fields[2];
                    Console.WriteLine($"[{user.Name}] -> [{name}] {message}");
                    User? recipient = Users.Find((u) => u.Name == name);
                    if (recipient == null) {
                        Console.WriteLine($"User [{name}] does not exist");
                        user.Send($"PRIVERR|{name}|User not connected|");
                        break;
                    }
                    if (message.Length > MAX_MESSAGE_LENGTH) {
                        Console.WriteLine($"Private message is too long");
                        user.Send($"PRIVERR|{name}|Message is too long|");
                        break;
                    }
                    recipient.Send($"PRIVATE|{user.Name}|{message}|");
                    break;

                }
                default: {
                    Console.WriteLine($"Unknown command {command}");
                    user.Send($"ERROR|{command}|");
                    break;
                }
            }
            fields = ParseNextRequest(user.Connection);
            command = fields[0];
        }
    }

    private void HandleConnection(object? obj) {
        if (obj == null) {
            throw new NullReferenceException("Connection should not be null");
        }
        Socket connection = (Socket) obj;
        while (true) {
            string[] fields = ParseNextRequest(connection);
            if (fields[0] == "CONNECT") {
                string username = fields[1];
                if (Users.Select((user) => user.Name).Contains(username)) {
                    Console.WriteLine(
                        $"Username [{username}] is already taken. " +
                        "Rejecting connection.");
                    string response = 
                        $"REJECTED|{username}|Name is Taken|";
                    connection.Send(Encoding.ASCII.GetBytes(response));
                    continue;
                }
                if (username.Length > MAX_NAME_LENGTH) {
                    Console.WriteLine(
                        $"Username [{username}] is greater than max length." +
                        "Rejecting connection.");
                    string response = $"REJECTED|{username}|Name length too long|";
                    connection.Send(Encoding.ASCII.GetBytes(response));
                    continue;
                }
                User newUser = new User(connection, username);
                Console.WriteLine($"New user [{username}] joined.");
                newUser.Send($"CONNECTED|{username}|");
                foreach (User user in Users) {
                    user.Send($"JOINED|{newUser.Name}|");
                }
                Users.Add(newUser);
                try {
                    HandleUser(newUser);
                } catch {
                    Console.WriteLine($"Connection broken. Dropping [{newUser.Name}]");
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                break;
            }
        }
    }
    public void Start() {
        List<Thread> threads = new List<Thread>();
        socket.Listen();
        Console.WriteLine($"Server is listening on {ipEndPoint}");
        while (true) {
            try {
                Socket connection = socket.Accept();
                Console.WriteLine(
                    $"Accepted connection from {connection.RemoteEndPoint}");
                    Thread thread = 
                        new Thread(new ParameterizedThreadStart(HandleConnection));
                    thread.Start(connection);
                    threads.Add(thread);
            } catch (Exception e) {
                Console.WriteLine(e);
                break;
            }
        }
        foreach (Thread thread in threads) {
            thread.Join();
        }
        socket.Shutdown(SocketShutdown.Both);
        socket.Close();
    }
}

class Program {

    static void Main(string[] args) {
        Server server = new Server();
        server.Start();
    }
}
