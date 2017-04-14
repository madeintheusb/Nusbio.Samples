/*
   Copyright (C) 2017 MadeInTheUSB LLC
   by FT for MadeInTheUSB

   The MIT License (MIT)

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in
        all copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
        THE SOFTWARE.
  
    MIT license, all text above must be included in any redistribution
*/

using System;
using System.Diagnostics;
using MadeInTheUSB;
using MadeInTheUSB.Adafruit;
using MadeInTheUSB.GPIO;
using System.Linq;
using MadeInTheUSB.spi;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
using size_t = System.Int16;
using MadeInTheUSB.WinUtil;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;


namespace MadeInTheUSB.Security
{
    public class PrivatePublicKeyHelper
    {
        public string PrivateKey { get { return keys.PrivateKey; } set { keys.PrivateKey = value; } }
        public string PublicKey { get { return keys.PublicKey; } set { keys.PublicKey = value; } }
        EncryptorKeys keys;

        public enum KeyType
        {
            Public,
            Private
        }

        public bool SetKeys(string privateKey, string publicKey)
        {
            this.keys.PrivateKey = privateKey;
            this.keys.PublicKey = publicKey;
            return UnitTest();
        }

        public const int KeySize = 2048;

        public PrivatePublicKeyHelper(bool generateNewKeys)
        {
            if (generateNewKeys)
            {
                keys = EncryptorRSA.GenerateKeys(KeySize);
            }
        }

        public byte[] DecryptBuffer(byte[] buffer, string privateKey)
        {
            return EncryptorRSA.DecryptBuffer(buffer, privateKey);
        }
        public byte[] EncryptBuffer(byte[] buffer, string publicKey)
        {
            return EncryptorRSA.EncryptBuffer(buffer, publicKey);
        }

        public bool UnitTest()
        {
            string data0 = @"To Sherlock Holmes she is always the woman. I have seldom heard him mention her under any other name. In his eyes she eclipses and predominates the whole of her sex. It was not that he felt any emotion akin to love for Irene Adler. All emotions, and that one particularly, were abhorrent to his cold, precise but admirably balanced mind. He was, I take it, the most perfect reasoning and observing machine that the world has seen, but as a lover he would have placed himself in a false position. He never spoke of the softer passions, save with a gibe and a sneer. They were admirable things for the observer—excellent for drawing the veil from men’s motives and actions. But for the trained reasoner to admit such intrusions into his own delicate and finely adjusted temperament was to introduce a distracting factor which might throw a doubt upon all his mental results. Grit in a sensitive instrument, or a crack in one of his own high-power lenses, would not be more disturbing than a strong emotion in a nature such as his. And yet there was but one woman to him, and that woman was the late Irene Adler, of dubious and questionable memory.";

            var encoder = new System.Text.UnicodeEncoding();

            var b = EncryptBuffer(encoder.GetBytes(data0), keys.PublicKey);
            var b2 = DecryptBuffer(b, keys.PrivateKey);

            var data1 = encoder.GetString(b2);

            var r = data0 == data1;
            return r;
        }
    }

    [Serializable]
    public class EncryptorKeys
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }

    }

    public static class EncryptorRSA
    {
        private static bool _optimalAsymmetricEncryptionPadding = false;

        public static EncryptorKeys GenerateKeys(int keySize)
        {
            if (keySize % 2 != 0 || keySize < 512)
                throw new Exception("Key should be multiple of two and greater than 512.");

            var response = new EncryptorKeys();
            using (var provider = new RSACryptoServiceProvider(keySize))
            {
                response.PublicKey = provider.ToXmlString(false);
                response.PrivateKey = provider.ToXmlString(true);
            }
            return response;
        }

        const int MAX_CHAR_ENCRYPTABLE = 128;
        const int MAX_CHAR_DEENCRYPTABLE = 256;

        public static byte[] DecryptBuffer(byte[] buffer, string privateKey)
        {
            string publicAndPrivateKeyXml = privateKey;

            using (var provider = new RSACryptoServiceProvider(PrivatePublicKeyHelper.KeySize))
            {
                provider.FromXmlString(publicAndPrivateKeyXml);

                var allBuffer      = new List<byte>();
                var tmpBuffer      = new byte[MAX_CHAR_DEENCRYPTABLE];
                var bufferX        = 0;
                var bufferOffSet   = 0;
                bool invalidBuffer = false;

                while (buffer.Length - bufferOffSet > 0)
                {
                    for (var i = 0; i < MAX_CHAR_DEENCRYPTABLE; i++)
                    {
                        tmpBuffer[i] = 0;
                        if (i + bufferOffSet < buffer.Length)
                            tmpBuffer[i] = buffer[i + bufferOffSet];
                        else
                        {
                            tmpBuffer[i] = 0;
                            invalidBuffer = true;
                        }
                    }

                    if (!invalidBuffer)
                    {
                        bufferOffSet += MAX_CHAR_DEENCRYPTABLE;

                        if (tmpBuffer == null || tmpBuffer.Length == 0) throw new ArgumentException("Data are empty", "data");
                        if (!IsKeySizeValid(PrivatePublicKeyHelper.KeySize)) throw new ArgumentException("Key size is not valid", "keySize");
                        if (String.IsNullOrEmpty(publicAndPrivateKeyXml)) throw new ArgumentException("Key is null or empty", "publicAndPrivateKeyXml");

                        allBuffer.AddRange(provider.Decrypt(tmpBuffer, _optimalAsymmetricEncryptionPadding));
                    }
                }
                return allBuffer.ToArray();
            }
        }

        public static byte[] EncryptBuffer(byte[] buffer, string key)
        {
            string publicKeyXml = key;
            var allBuffer = new List<byte>();

            while (buffer.Length > 0)
            {
                var tmpBuffer = buffer.Take(MAX_CHAR_ENCRYPTABLE).ToArray();
                var encrypted = Encrypt(tmpBuffer, PrivatePublicKeyHelper.KeySize, publicKeyXml);
                allBuffer.AddRange(encrypted);
                buffer = buffer.Skip(MAX_CHAR_ENCRYPTABLE).ToArray();
            }
            return allBuffer.ToArray();
        }

        public static string EncryptText(string text, string publicKey)
        {
            string publicKeyXml = "";
            var allBuffer       = new List<byte>();
            var buffer          = Encoding.UTF8.GetBytes(text);

            while (buffer.Length > 0)
            {
                var tmpBuffer = buffer.Take(MAX_CHAR_ENCRYPTABLE).ToArray();
                var encrypted = Encrypt(tmpBuffer, PrivatePublicKeyHelper.KeySize, publicKeyXml);
                allBuffer.AddRange(encrypted);
                buffer = buffer.Skip(MAX_CHAR_ENCRYPTABLE).ToArray();
            }
            return Convert.ToBase64String(allBuffer.ToArray());
        }

        private static byte[] Encrypt(byte[] data, int keySize, string publicKeyXml)
        {
            if (data == null || data.Length == 0) throw new ArgumentException("Data are empty", "data");
            int maxLength = GetMaxDataLength(keySize);
            if (data.Length > maxLength) throw new ArgumentException(String.Format("Maximum data length is {0}", maxLength), "data");
            if (!IsKeySizeValid(keySize)) throw new ArgumentException("Key size is not valid", "keySize");
            if (String.IsNullOrEmpty(publicKeyXml)) throw new ArgumentException("Key is null or empty", "publicKeyXml");

            using (var provider = new RSACryptoServiceProvider(keySize))
            {
                provider.FromXmlString(publicKeyXml);
                return provider.Encrypt(data, _optimalAsymmetricEncryptionPadding);
            }
        }

        private static byte[] Decrypt(byte[] data, int keySize, string publicAndPrivateKeyXml)
        {
            if (data == null || data.Length == 0) throw new ArgumentException("Data are empty", "data");
            if (!IsKeySizeValid(keySize)) throw new ArgumentException("Key size is not valid", "keySize");
            if (String.IsNullOrEmpty(publicAndPrivateKeyXml)) throw new ArgumentException("Key is null or empty", "publicAndPrivateKeyXml");

            using (var provider = new RSACryptoServiceProvider(keySize))
            {
                provider.FromXmlString(publicAndPrivateKeyXml);
                return provider.Decrypt(data, _optimalAsymmetricEncryptionPadding);
            }
        }

        public static int GetMaxDataLength(int keySize)
        {
            if (_optimalAsymmetricEncryptionPadding)
            {
                return ((keySize - 384) / 8) + 7;
            }
            return ((keySize - 384) / 8) + 37;
        }

        public static bool IsKeySizeValid(int keySize)
        {
            return keySize >= 384 &&
                    keySize <= 16384 &&
                    keySize % 8 == 0;
        }

        private static string IncludeKeyInEncryptionString(string publicKey, int keySize)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(keySize.ToString() + "!" + publicKey));
        }

    }
}
