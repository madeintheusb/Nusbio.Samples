using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Security;

namespace MadeInTheUSB
{
    public class MadeInTheAES
    {
        class AESWrapper
        {
            AesManaged _aes = new AesManaged();

            bool _keySet = false;
           
            public byte[] Key
            {
                get
                {
                    if (!_keySet)
                        throw new InvalidOperationException();
                    return _aes.Key;
                }
                set
                {
                    if (value.Length != 128 / 8)
                    {
                        throw new ArgumentException("Requires 128-bit key.");
                    }
                    _aes.Key = value;
                    _keySet = true;
                }
            }

            public byte[] IV
            {
                get { return _aes.IV; }
                set { _aes.IV = value; }
            }

            public int BlockSize
            {
                get { return _aes.BlockSize; }
            }

            public ICryptoTransform CreateEncryptor()
            {
                return _aes.CreateEncryptor();
            }

      
            public ICryptoTransform CreateDecryptor(byte[] key, byte[] iv)
            {
                return _aes.CreateDecryptor(key, iv);
            }
        }

        AESWrapper _wrappedAes = new AESWrapper();

        public byte[] Key
        {
            get { return _wrappedAes.Key; }
            set { _wrappedAes.Key = value; }
        }

      
        public byte[] IV
        {
            get { return _wrappedAes.IV; }
            set { _wrappedAes.IV = value; }
        }
        
        public MadeInTheAES()
        {
        }
        
        public MadeInTheAES(byte[] key)
            :this()
        {
            Key = key;
        }
        
        public MadeInTheAES(string key)
            : this(MadeInTheUSBMD5.Compute(key))
        {
        }

        public MadeInTheAES(SecureString key)
            : this(MadeInTheUSBMD5.Compute(key))
        {
        }

        static public string EncryptToBase64(byte[] plainData, string key)
        {
            return new MadeInTheAES(key).EncryptToBase64(plainData);
        }

        static public string EncryptToBase64(byte[] plainData, SecureString key)
        {
            return new MadeInTheAES(key).EncryptToBase64(plainData);
        }

        static public string EncryptToBase64(string plainText, string key)
        {
            return new MadeInTheAES(key).EncryptToBase64(plainText);
        }
    
        static public string EncryptToBase64(string plainText, SecureString key)
        {
            return new MadeInTheAES(key).EncryptToBase64(plainText);
        }
        
        static public byte[] Encrypt(byte[] plainBytes, string key)
        {
            return new MadeInTheAES(key).Encrypt(plainBytes);
        }

        static public byte[] Encrypt(byte[] plainBytes, SecureString key)
        {
            return new MadeInTheAES(key).Encrypt(plainBytes);
        }

        static public byte[] Encrypt(string plainText, string key)
        {
            return Encrypt(Encoding.UTF8.GetBytes(plainText), key);
        }

        
        static public byte[] Encrypt(string plainText, SecureString key)
        {
            return Encrypt(Encoding.UTF8.GetBytes(plainText), key);
        }
        
        public byte[] Encrypt(byte[] inBytes)
        {
            MemoryStream msEncrypted = new MemoryStream();
            Encrypt(new MemoryStream(inBytes), msEncrypted);
            return msEncrypted.ToArray();
        }

        public string EncryptToBase64(string inString)
        {
            return EncryptToBase64(Encoding.UTF8.GetBytes(inString));
        }

        public string EncryptToBase64(byte[] inBytes)
        {
            return Convert.ToBase64String(Encrypt(inBytes));
        }

  
        public void Encrypt(Stream inStream, Stream outStream)
        {
            ICryptoTransform transform = _wrappedAes.CreateEncryptor();
            long originalPosition = outStream.Position;

            outStream.Write(BitConverter.GetBytes((Int32)_wrappedAes.IV.Length), 0, sizeof(Int32));
            outStream.Write(_wrappedAes.IV, 0, _wrappedAes.IV.Length);

            using (CryptoStream cryptoStream = new CryptoStream(outStream, transform, CryptoStreamMode.Write))
            {
                int count = 0;
                int blockSizeBytes = _wrappedAes.BlockSize;
                byte[] data = new byte[blockSizeBytes];
                while ((count = inStream.Read(data, 0, blockSizeBytes)) > 0)
                {
                    cryptoStream.Write(data, 0, count);
                }
                cryptoStream.FlushFinalBlock();
                cryptoStream.Close();
            }
        }

        static public string DecryptToString(byte[] encrypted, string key)
        {
            return new MadeInTheAES(key).DecryptToString(encrypted);
        }

        static public string DecryptToString(byte[] encrypted, SecureString key)
        {
            return new MadeInTheAES(key).DecryptToString(encrypted);
        }

        static public string DecryptToString(string base64String, string key)
        {
            return new MadeInTheAES(key).DecryptToString(base64String);
        }

        
        static public string DecryptToString(string base64String, SecureString key)
        {
            return new MadeInTheAES(key).DecryptToString(base64String);
        }

        static public byte[] Decrypt(byte[] encryptedBytes, string key)
        {
            return new MadeInTheAES(key).Decrypt(encryptedBytes);
        }


        static public byte[] Decrypt(byte[] encryptedBytes, SecureString key)
        {
            return new MadeInTheAES(key).Decrypt(encryptedBytes);
        }

        static public byte[] Decrypt(string base64String, string key)
        {
            return new MadeInTheAES(key).Decrypt(base64String);
        }

        static public byte[] Decrypt(string base64String, SecureString key)
        {
            return new MadeInTheAES(key).Decrypt(base64String);
        }

       
        public byte[] Decrypt(byte[] encrypted)
        {
            MemoryStream msDecrypted = new MemoryStream();
            Decrypt(new MemoryStream(encrypted), msDecrypted);
            return msDecrypted.ToArray();
        }

        public byte[] Decrypt(string strBase64)
        {
            return Decrypt(Convert.FromBase64String(strBase64));
        }

        
        public string DecryptToString(string strBase64)
        {
            return Encoding.UTF8.GetString(Decrypt(strBase64));
        }

        public string DecryptToString(byte[] encrypted)
        {
            return Encoding.UTF8.GetString(Decrypt(encrypted));
        }

        
        public void Decrypt(Stream inStream, Stream outStream)
        {
            byte[] ivLengthBytes = new byte[sizeof(Int32)];
            inStream.Read(ivLengthBytes, 0, ivLengthBytes.Length);
            Int32 ivLength = BitConverter.ToInt32(ivLengthBytes, 0);
            byte[] iv = new byte[ivLength];
            inStream.Read(iv, 0, ivLength);

            int count = 0;
            int blockSizeBytes = _wrappedAes.BlockSize;
            byte[] data = new byte[blockSizeBytes];

            ICryptoTransform transform = _wrappedAes.CreateDecryptor(Key, iv);
            using (CryptoStream cryptoStream = new CryptoStream(outStream, transform, CryptoStreamMode.Write))
            {
                while ((count = inStream.Read(data, 0, blockSizeBytes)) > 0)
                {
                    cryptoStream.Write(data, 0, count);
                }
                cryptoStream.FlushFinalBlock();
                cryptoStream.Close();
            }
        }
    }
}
