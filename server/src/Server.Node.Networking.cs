using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static user.Data;

namespace server
{
    internal partial class Node
    {
        private TcpListener listener;

        public void Start()
        {
            listener = new(nodeInfo.IP, nodeInfo.Port);
            listener.Start();
            Console.WriteLine($"Node {nodeInfo.NodeID} is running on {nodeInfo.IP}:{nodeInfo.Port}");
            Task.Run(() => ListenForDataConnections());
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
                Console.WriteLine($"Error handling client: {ex.Message}");
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

        private static void PrintError(NetworkStream stream, Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            stream.Write(new Request("ERROR", Encoding.UTF8.GetBytes(ex.Message)).ToByteArray());
        }
    }
}
