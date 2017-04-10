using System;
using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

namespace MadeInTheUSB
{
    public static class StringCipher
    {
        private const int Keysize = 256;
        private const int DerivationIterations = 1000;

        static byte [] _saltStringBytes = new byte[32] {14,123,14,178,23,24,16,42,1,125,13,183,15,16,18,10,14,51,14,17,58,19,127,8,211,188,206,134,27,169,91,19};
        static byte [] _ivStringBytes   = new byte[32] {20,30,19,5,7,24,66,20,14,16,11,22,17,13,25,17,20,10,99,67,2,127,115,75,6,20,202,20,15,23,149,15,};

        public static byte[] Encrypt(byte []plainTextBytes, string passPhrase)
        {
            using (var password = new Rfc2898DeriveBytes(passPhrase, _saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.None;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, _ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                var cipherTextBytes = new byte[0];
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return cipherTextBytes;
                            }
                        }
                    }
                }
            }
        }

        public static byte [] Decrypt(byte []cipherTextBytesWithSaltAndIv, string passPhrase)
        {
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            //var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            //var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            //var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
            // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
            var cipherTextBytes = cipherTextBytesWithSaltAndIv;

            using (var password = new Rfc2898DeriveBytes(passPhrase, _saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.None;
                    using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, _ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                var plainTextBytes = new byte[cipherTextBytes.Length];
                                var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                memoryStream.Close();
                                cryptoStream.Close();
                                return plainTextBytes;
                            }
                        }
                    }
                }
            }
        }

        private static byte[] Generate256BitsOfRandomEntropy()
        {
            var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(randomBytes);
            }
            var s = new StringBuilder();
            foreach(var b in randomBytes)
                s.AppendFormat("{0},", b);
            Debug.WriteLine(s);
            return randomBytes;
        }
    }
}