using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace EmpPortal.Helpers
{
    public class EncryptionHelper
    {
        private readonly string _key;
        private readonly string _iv;

        public EncryptionHelper(IOptions<EncryptionSettings> options)
        {
            _key = options.Value.AESKey;
            _iv = options.Value.AESIV;
        }
        public string Encrypt(string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = Encoding.UTF8.GetBytes(_iv);

            using MemoryStream ms = new();
            using CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using StreamWriter sw = new(cs);
            sw.Write(plainText);

            return Convert.ToBase64String(ms.ToArray());
        }
        public string Decrypt(string cipherText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = Encoding.UTF8.GetBytes(_iv);

            byte[] buffer = Convert.FromBase64String(cipherText);
            using MemoryStream ms = new(buffer);
            using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader sr = new(cs);

            return sr.ReadToEnd();
        }
    }
    public class EncryptionSettings
    {
        public string AESKey { get; set; }
        public string AESIV { get; set; }
    }
}
