using System.Net;
using System.Net.Sockets;

namespace Client {
    internal class Program {
        static void Main(string[] args) {
            Console.WriteLine("Client...");

            var ip = IPAddress.Parse("192.168.0.113");
            var port = 27001;
            var ep = new IPEndPoint(ip, port);

            var client = new TcpClient();

            try {
                client.Connect(ep);

                while (true) {
                    if (client.Connected) {
                        Console.WriteLine("Connected...");

                        var stream = client.GetStream();

                        Console.Write("Username: ");
                        var username = Console.ReadLine();

                        Console.Write("Password: ");
                        var password = Console.ReadLine();

                        var bw = new BinaryWriter(stream);
                        bw.Write(username);
                        bw.Write(password);
                        bw.Flush(); // prosesi dayandirmadan melumat oturur

                        // Servere melumat gonderecek
                        var writer = Task.Run(() => {
                            while (true) {
                                Console.Write("Message : ");
                                var text = Console.ReadLine();

                                var stream = client.GetStream();
                                var bw = new BinaryWriter(stream);

                                bw.Write(text ?? "NoMessage");
                                bw.Flush(); // prosesi dayandirmadan melumat oturur

                                if (text?.ToLower() == "exit") {
                                    client.Close();
                                    break;
                                }
                            }
                        });

                        // Serverden melumat qebul edib cap edecek
                        var reader = Task.Run(() => {
                            try {
                                while (true) {
                                    var stream = client.GetStream();
                                    var br = new BinaryReader(stream);

                                    var result = br.ReadString();
                                    Console.WriteLine($"From Server: {result}");
                                }
                            }
                            catch {
                                Console.WriteLine("Server connection closed.");
                            }
                        });

                        Task.WaitAll(writer, reader);
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}