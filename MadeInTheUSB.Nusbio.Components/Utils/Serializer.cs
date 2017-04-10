using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Xml.Schema;


namespace MadeInTheUSB
{
    /// <summary>
    /// 
    /// </summary>
    public class BinSerializer
    {
        public static int ComputeFileSizePerSector(int size, int PAGE_SIZE)
        {
            return ((size / PAGE_SIZE) + 1) * PAGE_SIZE;
        }
        
        public static List<byte> MakeBufferMultilpleOf(List<byte> buffer, int pageSize)
        {
            return BinSerializer.MakeBufferUpTo(buffer, ((buffer.Count / pageSize) + 1) * pageSize);
        }

        /// <summary>
        /// // If b[0] >= 128 then it b[1]*128+b[0]
        /// </summary>
        /// <param name="s"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public static byte[] SerializeStringInUTF8(string s, int maxSize = -1, char padChar = ' ', byte paddingValue = 0) {

            if(maxSize != -1)
            {
                //if( GetSerializedStringInUTF8Length(s) < maxSize )
                //{
                //    while( GetSerializedStringInUTF8Length(s) < maxSize )
                //    {
                //        s += padChar;
                //    }
                //}
                if( GetSerializedStringInUTF8Length(s) > maxSize )
                {
                    while(GetSerializedStringInUTF8Length(s) > maxSize )
                    {
                        s = s.Substring(0, s.Length-1);
                    }
                }
            }
            
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    bw.Write(s);
                }
                byte [] buffer = ms.ToArray();

                // For euro or asian language we not be able to the exact buffer size
                // requested because 1 char is a nultiple of 2 or 3, thefore we may end up
                // with less byte. so now we pad it with 0
                if(maxSize != -1 && buffer.Length < maxSize)
                {
                    buffer = PadBuffer(buffer, maxSize, paddingValue);
                }

                return buffer;
            }
        }

        public static bool OverwriteFile(string fileName)
        {
            var fi = new FileInfo(fileName);
            var buffer = MakeBuffer((int)fi.Length, 55);
            System.IO.File.WriteAllBytes(fileName, buffer);
            buffer = MakeBuffer((int)fi.Length, 155);
            System.IO.File.WriteAllBytes(fileName, buffer);
            return true;
        }
        
        public static byte [] MakeBuffer(int count, byte val)
        {
            var l = new byte[count];
            for(var i=0; i<count; i++)
                l[i] = val;
            return  l;
        }
        public static string DeSerializeStringInUTF8(byte [] buffer)
        {
            using (var ms = new MemoryStream(buffer))
            {
                using (var br = new BinaryReader(ms, Encoding.UTF8))
                {
                    var s = br.ReadString();
                    return s;
                }
            }
        }

        public static int GetSerializedStringInUTF8Length(string s) {

            byte[] buffer;

            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    bw.Write(s);
                }
                buffer = ms.ToArray();
                return buffer.Length;
            }
        }

        public static  byte [] PadBuffer(byte[] buffer, int count, byte val = 0)
        {
            var l = new List<byte>();
            l.AddRange(buffer);
            while(l.Count < count)
            {
                l.Add(val);
            }
            return l.ToArray();
        }

        public static List<byte> RemovePadding(List<byte> buffer, byte val = 0)
        {
            while(buffer.Count > 0 && buffer[buffer.Count-1] == val)
                buffer.RemoveAt(buffer.Count-1);
            return buffer;
        }

        public static  List<byte> MakeBufferUpTo(List<byte> buffer,  int count, byte val = 0)
        {
            while(buffer.Count < count)
                buffer.Add(val);
            return buffer;
        }

        public static UInt32 MakeBufferMultipleOf(UInt32 val, UInt32 sectorSize = 512)
        {
            if(val == 0)
                return 0;

            if(val < sectorSize)
                return 1;

            if(val % sectorSize == 0)
                return val / sectorSize;

            return (val / sectorSize)+1;
        }

        public static List<byte> MakeBufferMultipleOf(List<byte> buffer, int sectorSize = 512, byte val = 0)
        {
            if(buffer.Count == 0)
                return buffer;

            var sectorCount = 0;

            if(buffer.Count % sectorSize == 0)
                sectorCount = buffer.Count / sectorSize;
            else
                sectorCount = (buffer.Count / sectorSize) + 1;

            var totalByte = sectorCount * sectorSize;

            return  MakeBufferUpTo(buffer, totalByte, val);
        }

        public static bool Compare(string s1, string s2)
        {
            if(s1.Length!=s2.Length)
                return false;

            int errorCount = 0;

            for(var i=0; i<s1.Length; i++)
            {
                if(s1[i]!=s2[i])
                {
                    Debug.WriteLine("Compare string issue [{0}] '{1}' '{2}' ", i, s1[i], s2[i]);
                    errorCount++;
                }
            }
            return errorCount == 0;
        }

        internal static void SerializeToFile(string fileName, object m, string encryptionKey)
        {
            if (String.IsNullOrEmpty(encryptionKey))
            {
                FileStream fs = System.IO.File.Create(fileName);
                BinaryFormatter formatter = new BinaryFormatter();
                try
                {
                    formatter.Serialize(fs, m);
                }
                finally
                {
                    fs.Close();
                }
            }
            else
            {
                MemoryStream ms = new MemoryStream();
                BinaryFormatter formatter = new BinaryFormatter();
                try
                {
                    formatter.Serialize(ms, m);
                    ms.Close();
                    byte[] buffer = ms.ToArray();
                    //buffer = Encryptor.C(encryptionKey, buffer);
                    FileStream fs = System.IO.File.Create(fileName);
                    fs.Write(buffer, 0, buffer.Length);
                    fs.Close();
                }
                finally
                {

                }
            }
        }
        public static void OverwriteBuffer(byte [] buffer)
        {
            for(var i=0; i< buffer.Length; i++)
            {
                buffer[i] = (byte)(i % 128);
            }
        }
        public static byte[] Serialize(object m, string encryptionKey)
        {
            var ms        = new MemoryStream();
            var formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(ms, m);
                ms.Close();
                byte[] buffer = ms.ToArray();

                if(encryptionKey != null)
                {
                    //var buffer2 = Encryptor.C(encryptionKey, buffer);
                    var buffer2 = buffer;
                    OverwriteBuffer(buffer);
                    return  buffer2;
                }
                else
                    return buffer;
            }
            catch(System.Exception ex)
            {
                throw;
            }
            finally
            {

            }
            return null;
        }

        internal static object Deserialize(byte [] buffer, string encryptionKey)
        {
            try
            {
                //if(encryptionKey != null) buffer = Encryptor.DC(encryptionKey, buffer);
            }
            catch (System.Exception ex)
            {
                throw new System.Exception(String.Format("Cannot decrypte bufferm error:{0}", ex.Message));
            }
            MemoryStream ms = new MemoryStream(buffer);
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(ms);
        }

         public static T BytesToObject<T>(byte[] bytes, string encryptionKey)
         {
             //bytes = Encryptor.DC(encryptionKey, bytes);
             MemoryStream ms = new MemoryStream();
             BinaryFormatter bf = new BinaryFormatter();
             ms.Write(bytes, 0, bytes.Length);
             ms.Seek(0, SeekOrigin.Begin);
 
             var obj = bf.Deserialize(ms);
             var retObj = (T)obj;
 
             return retObj;
         }

        internal static object DeserializeFromFile(string fileName, string encryptionKey)
        {
            if (String.IsNullOrEmpty(encryptionKey))
            {
                FileStream fs = System.IO.File.OpenRead(fileName);
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    return formatter.Deserialize(fs);
                }
                finally
                {
                    fs.Close();
                }
            }
            else
            {
                byte[] buffer = LoadFile(fileName);
                try
                {
                    //buffer = Encryptor.DC(encryptionKey, buffer);
                }
                catch (System.Exception ex)
                {
                    throw new System.Exception(String.Format("Cannot decrypte file:'{0}', error:{1}", fileName, ex.Message));
                }
                MemoryStream ms = new MemoryStream(buffer);
                BinaryFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(ms);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static byte[] LoadFile(string fileName)
        {

            using (Stream fileStream = System.IO.File.OpenRead(fileName))
            {

                fileStream.Seek(0, SeekOrigin.Begin);

                byte[] buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, (int)fileStream.Length);
                fileStream.Close();
                return buffer;
            }
        }

    }
}


