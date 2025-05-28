namespace user
{
    public partial class Data
    {
        public class DataInfo
        {
            public DateTime CreationTime { get; set; }
            public DateTime UpdateTime { get; set; }
            public byte[] DataHash { get; set; }
        }

        private byte[] EncryptedData { get; set; }
        private DataInfo dataInfo;

        public Data(byte[] encryptedData)
        {
            dataInfo = new();
            EncryptedData = encryptedData;
            dataInfo.DataHash = ComputeHash(encryptedData);
            dataInfo.CreationTime = DateTime.UtcNow;
            dataInfo.UpdateTime = DateTime.UtcNow;
        }
        public void UpdateData(byte[] encryptedData)
        {
            EncryptedData = encryptedData;
            dataInfo.DataHash = ComputeHash(encryptedData);
            dataInfo.UpdateTime = DateTime.UtcNow;
        }
        public bool VerifyData(byte[] encryptedData)
        {
            byte[] hash = ComputeHash(encryptedData);
            return dataInfo.DataHash.SequenceEqual(hash);
        }
    }
}
