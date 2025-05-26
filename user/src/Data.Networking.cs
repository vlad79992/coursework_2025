
using System.Text;

namespace user
{
    public partial class Data
    {
        ///отвечает за хранение узлов, на которых хранятся данные частей данных
        public List<Tuple<Guid[] /* node id */, byte[] /* part hash */>> dataLocation; 
        private void SendToNodes(DataPart[] dataParts, Guid[] nodes)
        {
            foreach (var part in dataParts)
            {
                //TODO отправить на узел
                //может изменить nodes с Guid на что-то нормальное
                throw new NotImplementedException();
            }
        }
        private void GetFromNodes()
        {
            throw new NotImplementedException();
        }
        private void GetNodes(int count)
        {
            throw new NotImplementedException();
        }

        public class Request
        {
            public byte[]  type; //8 байт
            public long    size; //8 байт размер данных
            public byte[]  data; //данные
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
        }
    }
}
