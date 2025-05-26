using System.Net;

namespace server
{
    internal partial class Node
    {
        public NodeInfo nodeInfo;
        private Storage storage;
        public Node(string ipAddress, int port)
        {
            nodeInfo = new();
            nodeInfo.NodeID = Guid.NewGuid();
            nodeInfo.IP = IPAddress.Parse(ipAddress);
            nodeInfo.Port = port;
            nodeInfo.LastActive = DateTime.UtcNow;
            storage = new();
        }
    }
    #region NodeInfo
    internal partial class Node
    {
        public class NodeInfo
        {
            public Guid NodeID { get; set; }
            public IPAddress IP { get; set; }
            public int Port { get; set; }
            public DateTime LastActive { get; set; }
        }
    }
    #endregion
    #region Storage
    internal partial class Node
    {
        class Storage
        {
            private Dictionary<byte[] /*hash*/, byte[]/*data*/> data = new();

            public void Add(byte[] dataPart)
            {
                data.Add(user.Data.ComputeHash(dataPart), dataPart);
            }
            public byte[] Get(byte[] partHash)
            {
                return data.TryGetValue(partHash, out var d) ? d : null;
            }
            public bool Delete(byte[] partHash)
            {
                return data.Remove(partHash);
            }
        }
    }
    #endregion
}