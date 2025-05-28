using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace user
{
    internal class Program
    {
        static void Main(string[] args)
        {
            byte[] testData = Encoding.UTF8.GetBytes("Data example");

            var manager = new Tuple<IPAddress, ushort>(
                IPAddress.Parse("127.0.0.1"),
                5000
            );

            byte[] key = Data.ComputeHash(Encoding.UTF8.GetBytes("key"));
            byte[] encryptedData = Data.EncryptData(testData, key);

            Data data = new(encryptedData);

            byte[] sendedHash = data.SendData(
                encryptedData,
                manager,
                partsCount: 2,
                nodesPerPart: 1);

            if (sendedHash != null)
            {
                Console.WriteLine("Data successfully sent");
            }
            else
            {
                Console.WriteLine("Error sending data");
                return;
            }

            byte[] receivedData = data.GetData(manager, sendedHash);

            if (receivedData != null)
            {
                Console.WriteLine("Hash equals: " +
                    BitConverter.ToString(data.dataInfo.DataHash)
                    .SequenceEqual(BitConverter.ToString(Data.ComputeHash(receivedData))));
                Console.WriteLine("Recieved data:");
                Console.WriteLine(Encoding.UTF8.GetString(Data.DecryptData(receivedData, key)));
            }
            else
            {
                Console.WriteLine("Error recieving data");
            }
        }

        public void CryptographyTest()
        {
            byte[] str = Encoding.UTF8.GetBytes("data");
            byte[] key = Data.ComputeHash(Encoding.UTF8.GetBytes("key"));
            Console.WriteLine($"Initial data:               {Encoding.UTF8.GetString(str)}");
            Console.WriteLine($"Initial data    (base64):   {Convert.ToBase64String(str)}");


            byte[] encryptedData = Data.EncryptData(str, key);
            Console.WriteLine($"Encrypted data  (base64):   {Convert.ToBase64String(encryptedData)}");

            byte[] decryptedData = Data.DecryptData(encryptedData, key);
            Console.WriteLine($"Decrypted data:             {Encoding.UTF8.GetString(decryptedData)}");

            Console.WriteLine();

            using var rsa = new RSACryptoServiceProvider(2048);

            byte[] publicKey = rsa.ExportRSAPublicKey();
            byte[] privateKey = rsa.ExportPkcs8PrivateKey();

            var encryptedKey = Data.EncryptKey(key, publicKey);
            var decryptedKey = Data.DecryptKey(encryptedKey, privateKey);

            Console.WriteLine($"Initial key     (base64):   {Convert.ToBase64String(key)}");
            Console.WriteLine($"Encrypted key   (base64):   {Convert.ToBase64String(encryptedKey)}");
            Console.WriteLine($"Decrypted key   (base64):   {Convert.ToBase64String(decryptedKey)}");

            Console.WriteLine("------------------");
        }
    }
}
