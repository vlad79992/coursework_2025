using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static user.Data;

namespace server
{
    internal partial class Node
    {
        private TcpListener listener;

        public void Start(Tuple<IPAddress, ushort> manager)
        {
            Console.WriteLine($"Node {nodeInfo.NodeID} is running on {nodeInfo.IP}:{nodeInfo.Port}");
            listener = new(nodeInfo.IP, nodeInfo.Port);
            RegisterNodeWithServer(manager);

            listener.Start();
            Task.Run(() => ListenForDataConnections());
            Task.Run(() => SendUpdates(manager));
        }
        private async Task ListenForDataConnections()
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleDataClient(client));
            }
        }
        private void HandleDataClient(TcpClient client)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                byte[] header = new byte[16];
                stream.ReadExactly(header, 0, 16);

                user.Data.Request request = user.Data.Request.FromByteArray(header);

                request.data = new byte[request.size];

                stream.ReadExactly(request.data);

                string command = Encoding.UTF8.GetString(request.type)
                    .TrimEnd(' ');

                switch (command)
                {
                    case "PUT":
                        PutData(stream, request.data);
                        break;
                    case "GET":
                        GetData(stream, request.data);
                        break;
                    case "DELETE":
                        DeleteData(stream, request.data);
                        break;
                    default:
                        stream.Write(new user.Data.Request("ERROR", Encoding.UTF8.GetBytes("Unknown command\n"))
                            .ToByteArray());
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error handling client: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                client.Close();
            }
        }
        private void PutData(NetworkStream stream, byte[] data)
        {
            try
            {
                storage.Add(data);
                stream.Write(new user.Data.Request("SUCCESS", null)
                                .ToByteArray());
            }
            catch (Exception ex)
            {
                PrintError(stream, ex);
            }
        }
        private void GetData(NetworkStream stream, byte[] hash)
        {
            try
            {
                stream.Write(new user.Data.Request("DATA", storage.Get(hash))
                                .ToByteArray());
            }
            catch (Exception ex)
            {
                PrintError(stream, ex);
            }
        }
        private void DeleteData(NetworkStream stream, byte[] hash)
        {
            try
            {
                storage.Delete(hash);
                stream.Write(new user.Data.Request("SUCCESS", null)
                                .ToByteArray());
            }
            catch (Exception ex)
            {
                PrintError(stream, ex);
            }
        }
        private void RegisterNodeWithServer(Tuple<IPAddress, ushort> manager)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new user.Data.IPAddressConverter());

            using TcpClient client = new();
            client.Connect(manager.Item1, manager.Item2);

            using NetworkStream stream = client.GetStream();
            
            var nodeJson = JsonConvert.SerializeObject(nodeInfo, settings);
            var request = new user.Data.Request("REGISTER", Encoding.UTF8.GetBytes(nodeJson));

            stream.Write(request.ToByteArray());
        }
        private async Task SendUpdates(Tuple<IPAddress, ushort> manager)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IPAddressConverter());
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(15));
                
                try
                {
                    nodeInfo.LastActive = DateTime.UtcNow;

                    string json = JsonConvert.SerializeObject(nodeInfo, settings);
                    byte[] data = Encoding.UTF8.GetBytes(json);

                    using var client = new TcpClient();
                    await client.ConnectAsync(manager.Item1, manager.Item2);
                    using var stream = client.GetStream();

                    var request = new Request("UPDATE", data);
                    var requestBytes = request.ToByteArray();
                    //Console.WriteLine("Sending UPDATE request");
                    await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки UPDATE: {ex.Message}");
                }
            }
        }
        private static void PrintError(NetworkStream stream, Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            stream.Write(new Request("ERROR", Encoding.UTF8.GetBytes(ex.Message)).ToByteArray());
        }
    }
}
