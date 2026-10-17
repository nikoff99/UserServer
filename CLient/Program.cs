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
                Console.WriteLine("Connected...");

                var stream = client.GetStream();
                var bw = new BinaryWriter(stream);
                var br = new BinaryReader(stream);

                Console.Write("Username: ");
                var username = Console.ReadLine();

                Console.Write("Password: ");
                var password = Console.ReadLine();

                bw.Write(username ?? "");
                bw.Write(password ?? "");
                bw.Flush();

                var reader = Task.Run(() => {
                    try {
                        while (true) {
                            var result = br.ReadString();
                            Console.WriteLine($"\nFrom Server: {result}");
                            Console.Write("Message : ");
                        }
                    }
                    catch {
                        Console.WriteLine("\nConnection closed.");
                    }
                });

                var writer = Task.Run(() => {
                    while (true) {
                        Console.Write("Message : ");
                        var text = Console.ReadLine();

                        bw.Write(text ?? "NoMessage");
                        bw.Flush();

                        if (text?.ToLower() == "exit") {
                            client.Close();
                            break;
                        }
                    }
                });

                Task.WaitAll(writer, reader);
            }
            catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}