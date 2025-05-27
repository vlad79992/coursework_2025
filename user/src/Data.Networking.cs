
using Newtonsoft.Json;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using static user.Data;

namespace user
{
    public partial class Data
    {
        public List<Tuple<IPAddress[] /* node id */, short /* node port */, byte[] /* part hash */>> dataLocation; 
        public bool SendData(byte[] data, Tuple<IPAddress, short> manager, int partsCount = 3, int nodesPerPart = 3)
        {
            
            int nodesCount = nodesPerPart * partsCount;
            Tuple<IPAddress, short>[] nodes = GetNodes(nodesCount, manager);
            DataPart[] dataParts = DataPart.SplitData(data, partsCount);

            for (int i = 0; i < partsCount; ++i)
            {
                SendToNodes(dataParts[i], 
                    nodes.Skip(i * nodesPerPart)
                    .Take(nodesPerPart)
                    .ToArray());
            }
        }
        private bool SendToNodes(DataPart dataPart, Tuple<IPAddress, short>[] nodes)
        {
            try
            {
                Request request = new("PUT", dataPart.ToByteArray());
                var requestBytes = request.ToByteArray();
                
                foreach (var node in nodes)
                {
                    using TcpClient client = new TcpClient();
                    client.Connect(node.Item1, node.Item2);

                    using NetworkStream stream = client.GetStream();

                    stream.Write(requestBytes);

                    byte[] header = new byte[16];
                    stream.ReadExactly(header);

                    Request response = Request.FromByteArray(header);
                    
                    stream.ReadExactly(response.data);

                    string command = Encoding.UTF8.GetString(response.type).TrimEnd(' ');

                    if (command == "SUCCESS")
                    {
                        Console.WriteLine($"Data successfully sent to {node.Item1}:{node.Item2}");
                        return true;
                    }
                    else
                    {   
                        Console.WriteLine($"Error at {node.Item1}:{node.Item2}\n\t{Encoding.UTF8.GetString(response.data)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return false;
        }
        public byte[] GetData(byte[] dataHash)
        {
            throw new NotImplementedException();
        }
        private DataPart GetPart(byte[] partHash, Tuple<IPAddress, short>[] nodes)
        {
            try
            {
                Request request = new("GET", partHash);

                foreach (var node in nodes)
                {
                    using TcpClient client = new TcpClient();
                    client.Connect(node.Item1, node.Item2);
                    using NetworkStream stream = client.GetStream();

                    byte[] header = new byte[16];

                    stream.ReadExactly(header);
                    Request response = Request.FromByteArray(header);
                    stream.ReadExactly(response.data);

                    DataPart dataPart = DataPart.FromByteArray(response.data);
                    if (ComputeHash(dataPart.dataPart) == dataPart.partHash)
                    {
                        return dataPart;
                    }
                }
                throw new Exception($"Cannot read data from nodes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return null;
        }
        private Tuple<IPAddress, short>[] GetNodes(int count, Tuple<IPAddress, short> manager)
        {
            try
            {
                Request request = new("GETNODES", null);

                
                using TcpClient client = new TcpClient();
                client.Connect(manager.Item1, manager.Item2);
                using NetworkStream stream = client.GetStream();

                byte[] header = new byte[16];

                stream.ReadExactly(header);
                Request response = Request.FromByteArray(header);
                stream.ReadExactly(response.data);

                string json = Encoding.UTF8.GetString(request.data);
                Tuple<IPAddress, short>[] nodes = JsonConvert.DeserializeObject<List<Tuple<IPAddress, short>>>(json).ToArray();

                return nodes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        public class Request
        {
            public byte[]   type; //8 байт
            public long     size; //8 байт размер данных
            public byte[]   data; //данные
            public Request()
            {
                type = new byte[8];
                size = 0;
                data = Array.Empty<byte>();
            }
            public Request(string typeStr, byte[]? data)
                : this()
            {
                Array.Copy(Encoding.UTF8.GetBytes(typeStr.PadRight(8, ' ')), this.type, 8);
                this.data = data ?? Array.Empty<byte>();
                this.size = this.data.Length;
            }
            public byte[] ToByteArray()
            {
                byte[] result = new byte[16 + (data?.Length ?? 0)];
                Buffer.BlockCopy(type, 0, result, 0, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(size), 0, result, 8, 8);
                if (data != null)
                    Buffer.BlockCopy(data, 0, result, 16, data.Length);
                return result;
            }
            public static Request FromByteArray(byte[] buffer)
            {
                if (buffer == null || buffer.Length < 16)
                    throw new ArgumentException("Invalid buffer length");

                Request request = new Request();

                Buffer.BlockCopy(buffer, 0, request.type, 0, 8);

                request.size = BitConverter.ToInt64(buffer, 8);

                if (request.size > 0)
                {
                    request.data = new byte[request.size];
                    Buffer.BlockCopy(buffer, 16, request.data, 0, request.data.Length);
                }
                else
                {
                    request.data = Array.Empty<byte>();
                }

                return request;
            }
            public void ReplaceData(byte[]? data)
            {
                this.data = data ?? Array.Empty<byte>();
                this.size = this.data.Length;
            }
        }
    }
}
