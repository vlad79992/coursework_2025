namespace user
{
    public partial class Data
    {

        public Guid ID { get; private set; }

        public byte[] EncryptedData { get; private set; }
        public byte[] DataHash { get; private set; }
        public DateTime CreationTime { get; private set; }
        public DateTime UpdateTime { get; private set; }

        public Data(byte[] encryptedData)
        {
            ID = Guid.NewGuid();
            EncryptedData = encryptedData;
            DataHash = ComputeHash(encryptedData);
            CreationTime = DateTime.UtcNow;
            UpdateTime = DateTime.UtcNow;

        }

        public void UpdateData(byte[] encryptedData)
        {
            EncryptedData = encryptedData;
            DataHash = ComputeHash(encryptedData);
            UpdateTime = DateTime.UtcNow;
        }

        public bool VerifyData(byte[] encryptedData)
        {
            byte[] hash = ComputeHash(encryptedData);
            return DataHash == hash;
        }
    }
}
