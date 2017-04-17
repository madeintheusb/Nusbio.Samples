//#define DO_NOT_ENCRYPT
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Security;

namespace MadeInTheUSB {

    public class DecryptionException : System.Exception
    {
        public DecryptionException(string message) : base(message)
        {
            
        }
    }

    public class PublicEncryptor {

        public static SecureString ConvertToSecureString(string p)
        {
            if (p == null)
                throw new ArgumentNullException("password");

            unsafe
            {
                fixed (char* pChars = p)
                {
                    var sp = new SecureString(pChars, p.Length);
                    sp.MakeReadOnly();
                    return sp;
                }
            }
        }

        public static string ConvertToUnsecureString(SecureString sp)
        {
            if (sp == null)
                throw new ArgumentNullException("securePassword");

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(sp);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        public static byte[] C(SecureString pw, byte[] buff)
        {
            #if DO_NOT_ENCRYPT
            return buff.ToList().ToArray();
            #else
            return StringCipher.Encrypt(buff, ConvertToUnsecureString(pw));
            #endif
        }

        public static byte[] DC(SecureString pw, byte[] buffer)
        {
            try
            {
                #if DO_NOT_ENCRYPT
                    return buffer.ToList().ToArray();
                #else
                    return StringCipher.Decrypt(buffer, ConvertToUnsecureString(pw));
                #endif
            }
            catch(System.OverflowException ox)
            {
                throw new DecryptionException(string.Format("Buffer len:{0}", buffer.Length));       
            }
        }

        public static byte[] CAndZip(SecureString pw, byte[] inputBuffer)
        {
            var b1 = Zip(inputBuffer);
            var b2 = C(pw, b1);
            return b2;
        }

        public static byte[] DCAndUnZip(SecureString pw, byte[] inputBuffer)
        {
            try
            {
                var b1 = DC(pw, inputBuffer);
                var b2 = UnzipAsByte(b1);
                return b2;
            }
            catch (System.Exception ex)
            {
                throw new DecryptionException("Cannot decrypte buffer");
            }
        }

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static byte[] Zip(byte [] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }
                return mso.ToArray();
            }
        }

        public static byte[] UnzipAsByte(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }
                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes)
        {
            return ByteToString(UnzipAsByte(bytes));
        }

        public static string ByteToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

     
        
       
       

    }
}
