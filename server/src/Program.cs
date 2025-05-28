using Newtonsoft.Json;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServerManager manager = new(5000);
            manager.Start();

            StartNode("127.0.0.1",
                      6000,
                      new Tuple<IPAddress, ushort>(IPAddress.Parse("127.0.0.1"), 5000));
            StartNode("127.0.0.1",
                      6001,
                      new Tuple<IPAddress, ushort>(IPAddress.Parse("127.0.0.1"), 5000));
            StartNode("127.0.0.1",
                      6002,
                      new Tuple<IPAddress, ushort>(IPAddress.Parse("127.0.0.1"), 5000));
            
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("""
                              ███████████████████████████████████████████████████
                              █Система запущена. Нажмите Enter для завершения...█
                              ███████████████████████████████████████████████████
                              """);
            Console.ResetColor();
            Console.ReadLine();
        }
        static void StartNode(string ipAddress, int port, Tuple<IPAddress, ushort> manager)
        {
            Task.Run(() =>
            {
                Node node = new Node(ipAddress, (ushort)port);
                node.Start(manager);
            });
        }
    }
}