using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Security;

namespace MadeInTheUSB
{
    static public class MadeInTheUSBMD5
    {
        static public byte[] Compute(string str)
        {
            return Compute(Encoding.UTF8.GetBytes(str));
        }
      
        static public byte[] Compute(byte[] bytes)
        {
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            return hash.Take(16).ToArray();
        }
     
        static public byte[] Compute(SecureString secureString)
        {
            using (BufferCleaner ary = new BufferCleaner(secureString))
            {
                return Compute(ary.Bytes);
            }
        }
    }
}
