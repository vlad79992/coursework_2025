﻿
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static user.Data;

namespace user
{
    public partial class Data
    {
        public List<Tuple<IPAddress[] /* node id */, ushort /* node port */, byte[] /* part hash */>> dataLocation; 
        public byte[] SendData(byte[] data, Tuple<IPAddress, ushort> manager, int partsCount = 3, int nodesPerPart = 3)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IPAddressConverter());
            try
            {
                int nodesCount = nodesPerPart * (partsCount + 1); // +1 для метаданных
                List<Tuple<byte[], Tuple<IPAddress, ushort>[]>> dataLocations = new();

                Tuple<IPAddress, ushort>[] nodes = GetNodes(nodesCount, manager);
                if (nodes == null)
                {
                    throw new NullReferenceException("GetNodes returned null");
                }
                if (nodes.Length < nodesCount)
                {
                    throw new Exception("Not enough nodes available");
                }

                DataPart[] dataParts = DataPart.SplitData(data, partsCount);

                int success = 0;

                for (int i = 0; i < partsCount; ++i)
                {
                    Tuple<IPAddress, ushort>[] nodesSend = nodes.Skip(i * nodesPerPart)
                        .Take(nodesPerPart)
                        .ToArray();
                    success += SendToNodes(dataParts[i].ToByteArray(),
                        nodesSend) ? 1 : 0;
                    dataLocations.Add(new Tuple<byte[], Tuple<IPAddress, ushort>[]>
                        (ComputeHash(dataParts[i].ToByteArray()), nodesSend));
                }

                var byteLocations = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dataLocations, settings));

                success += SendToNodes(byteLocations, nodes.Skip(nodesCount - nodesPerPart).ToArray()) ? 1 : 0;
                success += SendLocation(manager, 
                    ComputeHash(byteLocations),
                    byteLocations
                    ) ? 1 : 0;
                if (success == partsCount + 1 + 1)
                {
                    return ComputeHash(byteLocations);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
        private bool SendToNodes(byte[] data, Tuple<IPAddress, ushort>[] nodes)
        {
            try
            {
                Request request = new("PUT", data);
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

                    string answer = Encoding.UTF8.GetString(response.type).TrimEnd(' ');

                    if (answer == "SUCCESS")
                    {
                        Console.WriteLine($"Data successfully sent to {node.Item1}:{node.Item2}");
                    }
                    else
                    {   
                        Console.WriteLine($"Error at {node.Item1}:{node.Item2}\n\t{Encoding.UTF8.GetString(response.data)}. \n Node answer: {answer}");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return false;
        }
        private bool SendLocation(Tuple<IPAddress, ushort> manager, byte[] infoHash, byte[] infoLocations)
        {
            try
            {
                Request request = new("LOCATION", infoHash.Concat(infoLocations).ToArray());
                var requestBytes = request.ToByteArray();

                using TcpClient client = new TcpClient();
                client.Connect(manager.Item1, manager.Item2);

                using NetworkStream stream = client.GetStream();

                stream.Write(requestBytes);

                byte[] header = new byte[16];
                stream.ReadExactly(header);

                Request response = Request.FromByteArray(header);

                stream.ReadExactly(response.data);

                string answer = Encoding.UTF8.GetString(response.type).TrimEnd(' ');

                if (answer == "SUCCESS")
                {
                    Console.WriteLine($"Data successfully sent to {manager.Item1}:{manager.Item2}");
                }
                else
                {
                    Console.WriteLine($"Error at {manager.Item1}:{manager.Item2}\tNode answer: {answer}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return false;
        }
        public byte[] GetData(Tuple<IPAddress, ushort> manager, byte[] hash)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IPAddressConverter());
            try
            {
                Console.WriteLine($"Getting {Convert.ToBase64String(hash)} from manager");
                Request request = new("GET", hash);
                var requestBytes = request.ToByteArray();

                using TcpClient client = new TcpClient();
                client.Connect(manager.Item1, manager.Item2);

                using NetworkStream stream = client.GetStream();

                stream.Write(requestBytes);

                byte[] header = new byte[16];
                stream.ReadExactly(header);

                Request response = Request.FromByteArray(header);

                stream.ReadExactly(response.data);

                string answer = Encoding.UTF8.GetString(response.type).TrimEnd(' ');

                if (answer != "NODES")
                {
                    Console.WriteLine($"Error at {manager.Item1}:{manager.Item2}\tNode answer: {answer}");
                    return null;
                }

                List<Tuple<byte[], Tuple<IPAddress, ushort>[]>> dataLocations = new();
                
                dataLocations = JsonConvert.DeserializeObject<List<Tuple<byte[], Tuple<IPAddress, ushort>[]>>>(Encoding.UTF8.GetString(response.data), settings)
                                           .ToList();

                var dataParts = new DataPart[dataLocations.Count];

                foreach (var location in dataLocations)
                {
                    var dataPart = GetPart(location.Item1, location.Item2);
                    dataParts[dataPart.partNum] = dataPart;
                }

                byte[] output = new byte[dataParts.Sum(x => x.dataPart.Length)];
                var bytesRead = 0;
                foreach (var part in dataParts)
                {
                    Buffer.BlockCopy(part.dataPart, 0, output, bytesRead, part.dataPart.Length);
                    bytesRead += part.dataPart.Length;
                }

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return null;
        }
        private DataPart GetPart(byte[] partHash, Tuple<IPAddress, ushort>[] nodes)
        {
            try
            {
                Request request = new("GET", partHash);

                foreach (var node in nodes)
                {
                    using TcpClient client = new TcpClient();
                    client.Connect(node.Item1, node.Item2);
                    using NetworkStream stream = client.GetStream();

                    stream.Write(request.ToByteArray());

                    byte[] header = new byte[16];

                    stream.ReadExactly(header);
                    Request response = Request.FromByteArray(header);
                    stream.ReadExactly(response.data);

                    if (Encoding.UTF8.GetString(response.type).TrimEnd(' ') != "DATA")
                        throw new Exception("Unexpected response type");

                    DataPart dataPart = DataPart.FromByteArray(response.data);
                    if (ComputeHash(dataPart.dataPart).SequenceEqual(dataPart.partHash))
                    {
                        return dataPart;
                    }
                    else
                    {
                        Console.WriteLine($"Data corruption detected at node {node.Item1.ToString()}:{node.Item2}");
                    }
                }
                throw new Exception("No valid nodes found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return null;
        }
        private Tuple<IPAddress, ushort>[] GetNodes(int count, Tuple<IPAddress, ushort> manager)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IPAddressConverter());

            try
            {
                Request request = new("GETNODES", BitConverter.GetBytes(count));

                using TcpClient client = new TcpClient();
                client.Connect(manager.Item1, manager.Item2);
                using NetworkStream stream = client.GetStream();

                stream.Write(request.ToByteArray());

                byte[] header = new byte[16];

                stream.ReadExactly(header);
                Request response = Request.FromByteArray(header);
                stream.ReadExactly(response.data);

                string json = Encoding.UTF8.GetString(response.data);
                //Tuple<IPAddress, ushort>[] nodes = JsonConvert.DeserializeObject<List<Tuple<string, ushort>>>(json)
                //                                              .Select(n => new Tuple<IPAddress, ushort>(IPAddress.Parse(n.Item1), n.Item2))
                //                                              .ToArray();
                Tuple<IPAddress, ushort>[] nodes = JsonConvert.DeserializeObject<List<Tuple<IPAddress, ushort>>>(json, settings).ToArray();

                return nodes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
        public void PrintAllAvailableNodes(Tuple<IPAddress, ushort> manager)
        {
            try
            {
                Request request = new("ALLNODES", null);

                using TcpClient client = new TcpClient();
                client.Connect(manager.Item1, manager.Item2);
                using NetworkStream stream = client.GetStream();

                stream.Write(request.ToByteArray());

                byte[] header = new byte[16];

                stream.ReadExactly(header);
                Request response = Request.FromByteArray(header);
                stream.ReadExactly(response.data);

                string json = Encoding.UTF8.GetString(response.data);
                Tuple<IPAddress, ushort>[] nodes = JsonConvert.DeserializeObject<List<Tuple<string, ushort>>>(json)
                                                              .Select(n => new Tuple<IPAddress, ushort>(IPAddress.Parse(n.Item1), n.Item2))
                                                              .ToArray();

                foreach (var node in nodes)
                {
                    Console.WriteLine($"node {node.Item1.ToString}:{node.Item2}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
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
                    if (buffer.Length == 16 + request.size)
                    {
                        Buffer.BlockCopy(buffer, 16, request.data, 0, request.data.Length);
                    }
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
        public class IPAddressConverter : JsonConverter<IPAddress>
        {
            public override void WriteJson(JsonWriter writer, IPAddress value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString());
            }

            public override IPAddress ReadJson(JsonReader reader, Type objectType, IPAddress existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                JToken token = JToken.Load(reader);
                return IPAddress.Parse(token.Value<string>());
            }
        }
    }
}
