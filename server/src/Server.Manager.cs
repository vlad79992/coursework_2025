using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using static user.Data;

namespace server
{
    internal class ServerManager
    {
        private TcpListener listener;
        private List<Node.NodeInfo> nodes;
        private readonly object nodeLock;
        private Dictionary<string /* data hash */, byte[] /* nodes where information about data is located */> dataLocations;
        public ServerManager(int port)
        {
            listener = new(IPAddress.Any, port);
            nodes = new();
            dataLocations = new();
            nodeLock = new();
        }

        #region networking
        public void Start()
        {
            listener.Start();
            Console.WriteLine($"Server is running on {listener.Server.LocalEndPoint.ToString()}");
            Task.Run(() => ListenForConnections());
        }
        private async Task ListenForConnections()
        {
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleConnections(client));
            }
        }
        private void HandleConnections(TcpClient client)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IPAddressConverter());

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
                    case "REGISTER":
                        if (request.data.Length > 0)
                        {
                            string json = Encoding.UTF8.GetString(request.data);
                            Node.NodeInfo nodeInfo = JsonConvert.DeserializeObject<Node.NodeInfo>(json, settings);
                            RegisterNode(nodeInfo);
                        }
                        break;

                    case "GETNODES":
                        if (request.data.Length >= 4)
                        {
                            int count = BitConverter.ToInt32(request.data, 0);
                            SendNodesToClient(stream, count);
                        }
                        break;
                    case "ALLNODES":
                        if (request.data.Length >= 4)
                        {
                            int count = BitConverter.ToInt32(request.data, 0);
                            SendALLNodesToClient(stream, count);
                        }
                        break;
                    case "UPDATE":
                        if (request.data.Length > 0)
                        {
                            string json = Encoding.UTF8.GetString(request.data);
                            Node.NodeInfo nodeInfo = JsonConvert.DeserializeObject<Node.NodeInfo>(json, settings);
                            UpdateNode(nodeInfo);
                        }
                        break;
                    case "GET":
                        stream.Write(new user.Data.Request("NODES", dataLocations[Convert.ToBase64String(request.data)]).ToByteArray());
                        break;
                    case "LOCATION":
                        UpdateLocations(request.data, stream);
                        break;
                    default:
                        stream.Write(new user.Data.Request("ERROR", Encoding.UTF8.GetBytes("Unknown command"))
                                        .ToByteArray());
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"Error handling client: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                client.Close();
            }
        }
        private void SendALLNodesToClient(NetworkStream stream, int count)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(nodes.Select(n => new Tuple<string, ushort>(n.IP.ToString(), n.Port)));
            byte[] response = Encoding.UTF8.GetBytes(json);

            stream.Write(new user.Data.Request("NODES", response).ToByteArray());
        }
        private void SendNodesToClient(NetworkStream stream, int count)
        {
            List<Tuple<IPAddress, ushort>> selectedNodes;
            try
            {
                lock (nodeLock)
                {
                    selectedNodes = SelectOptimalNodes(
                        nodes
                        .Where(n => (DateTime.UtcNow - n.LastActive).TotalSeconds < 30)
                        .ToList(), count
                        );
                }
                
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(selectedNodes.Select(n => new Tuple<string, ushort>(n.Item1.ToString(), n.Item2)));
                
                byte[] response = Encoding.UTF8.GetBytes(json);
                stream.Write(new user.Data.Request("NODES", response).ToByteArray());
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"Error sending nodes: {ex.Message}");
                Console.ResetColor();
            }
        }
        #endregion
        #region work with nodes (field)
        private void UpdateNode(Node.NodeInfo nodeInfo)
        {
            lock (nodeLock)
            {
                Console.WriteLine($"Node {nodeInfo.NodeID} updated at {DateTime.UtcNow}"); //!!! ONLY for debug !!!
                var existing = nodes.FirstOrDefault(n => n.NodeID == nodeInfo.NodeID);
                if (existing != null)
                    existing.LastActive = DateTime.UtcNow;
            }
        }
        private void RegisterNode(Node.NodeInfo nodeInfo)
        {
            lock (nodeLock)
            {
                if (!nodes.Any(n => n.NodeID == nodeInfo.NodeID))
                {
                    nodes.Add(nodeInfo);
                    Console.WriteLine($"Node {nodeInfo.NodeID} added");
                }
            }
        }
        private List<Tuple<IPAddress, ushort>> SelectOptimalNodes(List<Node.NodeInfo> activeNodes, int count)
        {
            var random = new Random();
            return activeNodes
                .OrderBy(_ => random.Next())
                .Take(count)
                .Select(n => new Tuple<IPAddress, ushort>(n.IP, n.Port))
                .ToList();
        }




        #endregion
        private void UpdateLocations(byte[] data, NetworkStream stream)
        {
            if (data.Length < SHA256.HashSizeInBytes)
            {
                throw new ArgumentException("Invalid data format");
            }
            byte[] hash = new byte[SHA256.HashSizeInBytes];
            Buffer.BlockCopy(data, 0, hash, 0, hash.Length);
            var nodeInfo = data.Skip(hash.Length).ToArray();

            bool added = dataLocations.TryAdd(Convert.ToBase64String(hash), nodeInfo);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Added {Convert.ToBase64String(hash)} to dataLocations");
            Console.ResetColor();
            stream.Write(new Request(added ? "SUCCESS" : "ERROR", null).ToByteArray());
        }
    }
}