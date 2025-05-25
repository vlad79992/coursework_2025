using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace user
{
    public partial class Data
    {
        public static byte[] ComputeHash(byte[] data)
        {
            byte[] hashBytes = SHA256
                .Create()
                .ComputeHash(data);
            return hashBytes;
        }

        public static byte[] EncryptData(byte[] data, byte[] key)
        {
            using Aes aes = Aes.Create();
            //iv - инициализирующий вектор, случайное значение, нужное, чтобы
            //две одинаковые пары данных и пароля давали разные зашифрованные значения
            aes.Key = ComputeHash(key);
            byte[] iv = aes.IV;

            using MemoryStream memoryStream = new();
            memoryStream.Write(iv, 0, iv.Length);

            using (CryptoStream cryptoStream = new(
                memoryStream,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
            }
            return memoryStream.ToArray();
        }

        public static byte[] DecryptData(byte[] encryptedData, byte[] key)
        {
            using Aes aes = Aes.Create();
            byte[] iv = new byte[aes.IV.Length];

            using MemoryStream memoryStream = new(encryptedData);
            memoryStream.Read(iv, 0, iv.Length);

            using CryptoStream cryptoStream = new(
                memoryStream,
                aes.CreateDecryptor(ComputeHash(key), iv),
                CryptoStreamMode.Read);
            
            using MemoryStream decryptedStream = new();
            cryptoStream.CopyTo(decryptedStream);
            return decryptedStream.ToArray();
        }

        public static byte[] EncryptKey(byte[] key, byte[] publicKey)
        {
            using RSACryptoServiceProvider rsa = new();
            rsa.ImportRSAPublicKey(publicKey, out _);
            return rsa.Encrypt(key, RSAEncryptionPadding.Pkcs1);
        }
        public static byte[] DecryptKey(byte[] encryptedKey, byte[] PrivateKey)
        {
            using RSACryptoServiceProvider rsa = new();
            rsa.ImportPkcs8PrivateKey(PrivateKey, out _);
            return rsa.Decrypt(encryptedKey, RSAEncryptionPadding.Pkcs1);
        }
    }
}
