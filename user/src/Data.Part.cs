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
            public byte[]   dataHash;
            public byte[]   partHash;
            public int      partNum;
            public byte[]   dataPart;
            static public DataPart[] SplitData(byte[] data, int count)
            {
                DataPart[] parts = new DataPart[count];
                int partSize = (int)Math.Ceiling((double)data.Length / count);

                for (int i = 0; i < count; i++)
                {
                    parts[i] = new DataPart();

                    int offset = i * partSize;
                    int length = Math.Min(partSize, data.Length - offset);

                    parts[i].dataPart = new byte[length];
                    Buffer.BlockCopy(data, offset, parts[i].dataPart, 0, length);

                    parts[i].dataHash = ComputeHash(data);
                    parts[i].partHash = ComputeHash(parts[i].dataPart);
                    parts[i].partNum = i;
                }

                return parts;
            }

            public byte[] ToByteArray()
            {
                const int hashSize = SHA256.HashSizeInBytes;
                byte[] combined = new byte[hashSize * 2 + dataPart.Length + sizeof(int)];
                Buffer.BlockCopy(dataHash, 0, combined, 0, hashSize);
                Buffer.BlockCopy(partHash, 0, combined, hashSize, hashSize);
                Buffer.BlockCopy(BitConverter.GetBytes(partNum), 0, combined, hashSize * 2, sizeof(int));
                Buffer.BlockCopy(dataPart, 0, combined, hashSize * 2 + sizeof(int), dataPart.Length);
                return combined;
            }

            public static DataPart FromByteArray(byte[] data)
            {
                const int hashSize = SHA256.HashSizeInBytes;

                DataPart part = new DataPart();
                part.dataHash = new byte[hashSize];
                part.partHash = new byte[hashSize];
                part.dataPart = new byte[data.Length - hashSize * 2];
                Buffer.BlockCopy(data, 0, part.dataHash, 0, hashSize);
                Buffer.BlockCopy(data, hashSize, part.partHash, 0, hashSize);
                Buffer.BlockCopy(data, hashSize * 2, part.dataPart, 0, part.dataPart.Length);
                return part;
            }
        }
    }
}
