using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ARMCommon.Helpers
{
    public class AES
    {
        public string EncryptString(string plainText, string key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.Mode = CipherMode.CFB; // Choose the appropriate mode
                aesAlg.GenerateIV();

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }

                    byte[] iv = aesAlg.IV;
                    byte[] encryptedBytes = msEncrypt.ToArray();

                    // Combine IV and encrypted data
                    byte[] combinedData = new byte[iv.Length + encryptedBytes.Length];
                    Array.Copy(iv, 0, combinedData, 0, iv.Length);
                    Array.Copy(encryptedBytes, 0, combinedData, iv.Length, encryptedBytes.Length);

                    return Convert.ToBase64String(combinedData);
                }
            }
        }

        public string DecryptString(string cipherText, string key)
        {
            byte[] combinedData = Convert.FromBase64String(cipherText);
            byte[] iv = new byte[16]; // Assuming 128-bit AES
            byte[] encryptedBytes = new byte[combinedData.Length - iv.Length];

            Array.Copy(combinedData, iv, iv.Length);
            Array.Copy(combinedData, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.Mode = CipherMode.CFB; // Choose the appropriate mode
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(encryptedBytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
