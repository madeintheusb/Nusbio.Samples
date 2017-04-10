using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;

namespace MadeInTheUSB
{
    public class BufferCleaner : IDisposable
    {
        byte[] _bytes;
        public byte[] Bytes
        {
            get { return _bytes; }
        }

        public BufferCleaner(SecureString secString)
        {
            IntPtr secStringPtr = Marshal.SecureStringToBSTR(secString);
            if (secStringPtr == IntPtr.Zero)
                throw new InvalidOperationException(String.Format("Unable to allocate"));
            char[] charBuffer = new char[secString.Length];
            try
            {
                GCHandle handle = GCHandle.Alloc(charBuffer, GCHandleType.Pinned);
                try
                {
                    Marshal.Copy(secStringPtr, charBuffer, 0, charBuffer.Length);
                }
                finally
                {
                    handle.Free();
                }
                _bytes = Encoding.UTF8.GetBytes(charBuffer); 
            }
            finally
            {
                Array.Clear(charBuffer, 0, charBuffer.Length);
            }
        }
       
      
        public static implicit operator byte[](BufferCleaner obj)
        {
            return obj.Bytes;
        }

        public void Dispose()
        {
            Array.Clear(_bytes, 0, _bytes.Length);
        }

    }
}
