using System.Net;

namespace server
{
    internal partial class Node
    {
        public NodeInfo nodeInfo;
        private Storage storage;
        public Node(string ipAddress, ushort port)
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
            public ushort Port { get; set; }
            public DateTime LastActive { get; set; }
        }
    }
    #endregion
    #region Storage
    internal partial class Node
    {
        class Storage
        {
            private Dictionary<string /*hash*/, byte[]/*data*/> data = new();

            public void Add(byte[] dataPart)
            {
                string hashKey = Convert.ToBase64String(user.Data.ComputeHash(dataPart));
                if (!data.ContainsKey(hashKey))
                {
                    data.Add(hashKey, dataPart);
                }
            }
            public byte[] Get(byte[] partHash)
            {
                return data.TryGetValue(
                    Convert.ToBase64String(partHash), out var d) ? d : null;
            }
            public bool Delete(byte[] partHash)
            {
                return data.Remove(Convert.ToBase64String(partHash));
            }
        }
    }
    #endregion
}