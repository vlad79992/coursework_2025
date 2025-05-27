using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static user.Data;

namespace user
{
    public partial class Data
    {
        public class DataPart
        {
            public byte[] dataHash;
            public byte[] partHash;
            public byte[] dataPart;
            static public DataPart[] SplitData(byte[] data, int count)
            {
                DataPart[] parts = new DataPart[count];
                int partSize = (int)Math.Ceiling((double)data.Length / count);

                for (int i = 0; i < count; i++)
                {
                    int offset = i * partSize;
                    int length = Math.Min(partSize, data.Length - offset);

                    parts[i].dataPart = new byte[length];
                    Buffer.BlockCopy(data, offset, parts[i].dataPart, 0, length);

                    parts[i].dataHash = ComputeHash(data);
                    parts[i].partHash = ComputeHash(parts[i].partHash);
                }

                return parts;
            }

            public byte[] ToByteArray()
            {
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dataPart));
            }

            public static DataPart FromByteArray(byte[] data)
            {
                string json = Encoding.UTF8.GetString(data);
                return JsonConvert.DeserializeObject<DataPart>(json);
            }
        }
    }
}
