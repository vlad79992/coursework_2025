using System.Security.Cryptography;
using System.Text;

namespace user
{
    internal class Program
    {
        static void Main(string[] args)
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
