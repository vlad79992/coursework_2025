using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace server
{
    internal class ServerManager
    {
        private TcpListener listener;
        private List<Node.NodeInfo> nodes;
        private readonly object nodeLock;

        public ServerManager(int port)
        {
            listener = new(IPAddress.Any, port);
            nodes = new();
            nodeLock = new();
        }

        #region networking
        public void Start()
        {
            listener.Start();
            Console.WriteLine("Server is running");
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
                            Node.NodeInfo nodeInfo = JsonConvert.DeserializeObject<Node.NodeInfo>(json);
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
                    case "UPDATE":
                        if (request.data.Length > 0)
                        {
                            string json = Encoding.UTF8.GetString(request.data);
                            Node.NodeInfo nodeInfo = JsonConvert.DeserializeObject<Node.NodeInfo>(json);
                            UpdateNode(nodeInfo);
                        }
                        break;
                    default:
                        stream.Write(new user.Data.Request("ERROR", Encoding.UTF8.GetBytes("Unknown command"))
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
        private void SendNodesToClient(NetworkStream stream, int count)
        {
            List<Tuple<IPAddress, short>> selectedNodes;
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
                
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(selectedNodes);
                
                byte[] response = Encoding.UTF8.GetBytes(json);
                stream.Write(new user.Data.Request("NODES", response)
                                .ToByteArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending nodes: {ex.Message}");
            }
        }
        #endregion
        #region work with nodes (field)
        private void UpdateNode(Node.NodeInfo nodeInfo)
        {
            lock (nodeLock)
            {
                var existing = nodes.FirstOrDefault(n => n.NodeID == nodeInfo.NodeID);
                if (existing != null)
                    existing.LastActive = DateTime.UtcNow;
            }
        }
        private void RegisterNode(Node.NodeInfo nodeInfo)
        {
            if (!nodes.Any(n => n.NodeID == nodeInfo.NodeID))
            {
                lock (nodeLock)
                {
                    nodes.Add(nodeInfo);
                }
                Console.WriteLine($"Node {nodeInfo.NodeID} added");
            }
        }
        private List<Tuple<IPAddress, short>> SelectOptimalNodes(List<Node.NodeInfo> activeNodes, int count)
        {
            var random = new Random();
            return activeNodes
                .OrderBy(_ => random.Next())
                .Take(count)
                .Select(n => new Tuple<IPAddress, short>(n.IP, n.Port))
                .ToList();
        }
        #endregion
    }
}