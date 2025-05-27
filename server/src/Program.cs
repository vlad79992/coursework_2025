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
            byte[] bytes = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
            var serialized = JsonConvert.SerializeObject(bytes);
            Console.WriteLine(serialized);
            byte[] bytesDeserialized = JsonConvert.DeserializeObject<byte[]>(serialized);

            foreach (var item in bytes) Console.Write($"[{item}]");
            Console.WriteLine();
            foreach (var item in bytesDeserialized) Console.Write($"[{item}]");
        }
    }
}