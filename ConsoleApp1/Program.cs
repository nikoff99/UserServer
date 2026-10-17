using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace ConsoleApp1 {
    internal class Program {
        class User {
            public User(string? username, string? password, bool isOnline) {
                Username = username;
                Password = password;
                IsOnline = isOnline;
            }

            public string? Username { get; set; }
            public string? Password { get; set; }
            public bool IsOnline { get; set; }
            public string? RemoteEndPoint { get; set; }
        }

        static void MyJsonSerializer(List<User> users) {
            var serializer = new JsonSerializer();
            using (var sw = new StreamWriter("Users.json"))
            using (var jw = new JsonTextWriter(sw)) {
                jw.Formatting = Newtonsoft.Json.Formatting.Indented;
                serializer.Serialize(jw, users);
            }
        }

        static List<User> MyJsonDeSerializer() {
            var serializer = new JsonSerializer();
            using (var sr = new StreamReader("Users.json"))
            using (var jr = new JsonTextReader(sr)) {
                return serializer.Deserialize<List<User>>(jr) ?? new List<User>();
            }
        }

        static TcpListener listener = null;
        static List<TcpClient> clients = new List<TcpClient>();
        static object locker = new object();

        static void Main(string[] args) {
            List<User> userList = new List<User>();

            try {
                userList = MyJsonDeSerializer();
            }
            catch {
                Console.WriteLine("File is not exist");

                User user1 = new User("Messi", "fcbarcelona2010", false);
                User user2 = new User("KDB", "kingofassist17", false);
                User user3 = new User("Hazard", "slayahazard09", false);

                userList.Add(user1);
                userList.Add(user2);
                userList.Add(user3);
                MyJsonSerializer(userList);
            }

            Console.WriteLine("Server...");
            var ip = IPAddress.Parse("192.168.0.113");
            var port = 27001;
            var ep = new IPEndPoint(ip, port);

            listener = new TcpListener(ep);
            listener.Start();
            Console.WriteLine("Listening:" + listener.LocalEndpoint);

            Task.Run(() => {
                while (true) {
                    Thread.Sleep(5000); // 5 saniyə gözləyir

                    lock (locker) {
                        try {
                            var currentUsers = MyJsonDeSerializer();
                            Console.WriteLine("\n===================== USER STATUS LIST =====================");
                            foreach (var u in currentUsers) {
                                Console.WriteLine($"User: {u.Username,-10} | Status: {(u.IsOnline ? "ONLINE" : "OFFLINE"),-8} | IP: {u.RemoteEndPoint ?? "None"}");
                            }
                            Console.WriteLine("============================================================\n");
                        }
                        catch {}
                    }
                }
            });

            while (true) {
                var client = listener.AcceptTcpClient();
                clients.Add(client);

                Task.Run(() => {
                    var stream = client.GetStream();
                    var reader = new BinaryReader(stream);

                    string username = reader.ReadString();
                    string password = reader.ReadString();

                    User? joined_user = null;

                    lock (locker) {
                        joined_user = userList.FirstOrDefault(u => u.Username == username && u.Password == password);

                        if (joined_user?.IsOnline == true) {
                            Console.WriteLine($"[Refused] {username} is already online.");
                            return;
                        }
                        else if (joined_user != null) {
                            joined_user.RemoteEndPoint = client.Client.RemoteEndPoint?.ToString();
                            joined_user.IsOnline = true;
                            MyJsonSerializer(userList);

                            Console.WriteLine("Logined Successfully !!!");
                            Console.WriteLine("Username: " + joined_user.Username);
                            Console.WriteLine("Endpoint: " + joined_user.RemoteEndPoint);
                            Console.Write($"Is {joined_user.Username} Online :");
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(" YES");
                            Console.ResetColor();
                        }
                        else {
                            Console.WriteLine("Wrong username or password...");
                            return;
                        }
                    }

                    Task.Run(() => {
                        while (true) {
                            var message = Console.ReadLine();
                            foreach (var item in clients) {
                                var stream_s = item.GetStream();
                                var bw = new BinaryWriter(stream_s);
                                bw.Write(message ?? "noMessage");
                            }
                        }
                    });

                    Task.Run(() => {
                        var br = new BinaryReader(stream);
                        while (true) {
                            try {
                                var message = br.ReadString();

                                if (message.ToLower() == "exit") {
                                    lock (locker) {
                                        joined_user.IsOnline = false;
                                        MyJsonSerializer(userList);
                                    }
                                    Console.WriteLine($"{username} disconnected.");
                                    break;
                                }

                                Console.WriteLine($"Message From {username} : {message}");
                            }
                            catch {
                                lock (locker) {
                                    joined_user.IsOnline = false;
                                    MyJsonSerializer(userList);
                                }
                                Console.WriteLine($"{username} disconnected.");
                                break;
                            }
                        }
                    });
                });
            }
        }
    }
}