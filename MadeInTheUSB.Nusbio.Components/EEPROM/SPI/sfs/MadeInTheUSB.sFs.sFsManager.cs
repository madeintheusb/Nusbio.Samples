/*
 
MadeInTheUSB.sFs - small Files system

A very simple file system that can be implemented in C# based on 
- 128k bytes SPI EEPROM  (Page size or sector size is 256 bytes)
    Read performance 28 Kbyte/S
    Write performance 12 Kbyte/S
- Later 16 or 32 Mb NOR FLASH and 16 GB SD Card.

Key ideas:
---------

1) The files stored on the disk with the MadeInTheUSB.sFs file system are
protected from the Windows OS and any process running on the PC including:
trojan, virus, malware, ransomware program. The .NET application contains its
own file system and HAL (Hardware abstraction layer) based on the SPI communication
protocol and the USB device Nusbio v1 or v2 (www.Nusbio.net).

2) The data is encrypted on the disk (EEPROM). Though the data may be decrypted in
memory. The encryption password is for now just a .NET string, later it should be changed into a SecureString.

3) In v1 only SPI EEPROM are supported using Nusbio v1 (see www.Nusbio.net).
In v2 support for 16Mb or more NOR FLASH memory with read performance up to 3 Mb/S using Nusbio v2. 
In v3 support for 16 GB SD Card with read performance up to 1 Mb/S Nusbio v2.

Architecture:
- The first 8 pages (8x256) 2k byte are reserved to store the FAT
    The first 5 bytes are reserved for: Magic 1byte + NumberOfFile 4bytes (see method SerializeFAT())
    The metadata for a file is 28 bytes
    The total number of file that can be stored is 42. (2048-5)/48.
- The rest of the pages are used for file storage.
- No folder only one root
- File name length is limited to 16 ascii characters. File name support 
  european or asian characters but the size may be less than 16
- File max size 2Gb
- Once a file is marked as deleted the used space is not recovered
- Data is always encrypted
- A storage compaction feature can get rid of the unused space
- The concept of page or sector is the same and is for now 256 bytes.
*/

/*
   Copyright (C) 2017 MadeInTheUSB LLC
   Written by FT for MadeInTheUSB

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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace MadeInTheUSB.sFs
{
    /// <summary>
    /// The MadeInTheUSB small File System class
    /// </summary>
    public class sFsManager 
    {
        public string VolumeName = "MadeInTheUSB.sfs based on USB device Nusbio and EEPROM";
        public List<sFsFileInfo> FileInfos;

        IFATWriterReader _fatWriterReader;

        private IFATWriterReader GetFATWriterReader()
        {
            return _fatWriterReader;
        }

        public long MaxKByte
        {
            get
            {
                return GetFATWriterReader().DiskMaxByte / 1024;
            }
        }

        /// <summary>
        /// Encryption password. This should be a SecureString or better.
        /// </summary>
        private SecureString _pw;

#if NUSBIO2
        // Implementation for Nusbio v2 is not implemented yet
        public EEPROM_25AA1024_FILESYSTEM() : base(1024)
        {
        }
#else
        // Implementation for USB device Nusbio v1
        public sFsManager(
            string volumeName,
            SecureString pw,
            Nusbio nusbio,
            NusbioGpio clockPin,
            NusbioGpio mosiPin,
            NusbioGpio misoPin,
            NusbioGpio selectPin,
            bool debug = false) 
        {
            this._fatWriterReader = new FATWriterReaderEEPROMImpl(nusbio, clockPin, mosiPin, misoPin, selectPin, debug);
            this._pw              = pw;
            this.VolumeName       = volumeName;
            this.FileInfos        = new List<sFsFileInfo>();
        }
#endif
        /// <summary>
        /// Page where the FAT start
        /// </summary>
        const int FAT_START_ADDR = 0;
        /// <summary>
        /// Size of the FAT
        /// </summary>
        const int FAT_END_ADDR = 256 * 8; // 2048 bytes reserved for the FAT
        /// <summary>
        /// Magic value used to determine if we read a valid FAT
        /// </summary>
        const int MAGIC_BYTE = 164;
        /// <summary>
        /// Where is the data section start
        /// </summary>
        const int DATA_START_ADDR = FAT_END_ADDR;
        /// <summary>
        /// Number of file that can be stored on the disk. Only one root no sub directory
        /// 41 file maximun
        /// ((256*8)-5)/(32+4+4+8+1) = 41.69387755102041
        /// </summary>
        public const int MAX_FILE_PER_FAT = 41;

        /// Error messages
        public const string SFS_COMMAND_ERR_MESSAGE = "The system cannot find the file specified.";
        
        /// <summary>
        /// Convert the FAT data into a byte array
        /// </summary>
        /// <param name="fileInfos"></param>
        /// <returns></returns>
        internal byte[] SerializeFAT()
        {
            byte[] buffer;

            if (FileInfos.Count > MAX_FILE_PER_FAT)
                throw new ArgumentException("Too many files");

            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    bw.Write((byte)MAGIC_BYTE);             // 1 byte
                    bw.Write((Int32)FileInfos.Count);       // 4 byte
                    foreach (var fi in FileInfos)
                    {
                        bw.Write(BinSerializer.SerializeStringInUTF8(fi.FileName, sFsFileInfo.MAX_FILE_NAME_SIZE)); // 32 bytes
                        bw.Write((Int32)fi.StartAddr); // 4 bytes
                        bw.Write((Int32)fi.Length);    // 4 bytes
                        bw.Write((byte)fi.Attribute);  // 1 bytes
                        bw.Write(fi.LastModificationDate.Ticks);    // 8 bytes
                    }
                }
                buffer = ms.ToArray();
                buffer = BinSerializer.PadBuffer(buffer, FAT_END_ADDR);
                return buffer;
            }
        }

        public sFsFileInfo this[string fileName]
        {
            get
            {
                var filename = fileName.ToLowerInvariant();
                foreach (var f in this.FileInfos)
                    if ((f.FileName.ToLowerInvariant() == filename) && (!f.Deleted))
                        return f;
                return null;
            }
        }

        internal List<sFsFileInfo> DeSerializeFAT(byte[] buffer)
        {
            var l = new List<sFsFileInfo>();
            try
            {
                using (var ms = new MemoryStream(buffer))
                {
                    using (var br = new BinaryReader(ms, Encoding.UTF8))
                    {
                        int magicByte = br.ReadByte();
                        if (magicByte != MAGIC_BYTE)
                            throw new ApplicationException("Magic byte not found");

                        var fileCount = br.ReadInt32(); // 4 bytes, number of file stored on the disk

                        for (var i = 0; i < fileCount; i++)
                        {
                            var fileName = BinSerializer.DeSerializeStringInUTF8(br.ReadBytes(sFsFileInfo.MAX_FILE_NAME_SIZE)); // 16 bytes
                            l.Add(new sFsFileInfo(fileName, false)
                            {
                                StartAddr            = br.ReadInt32(), // 4 bytes
                                Length               = br.ReadInt32(), // 4 bytes
                                Attribute            = (sFsFileAttribute)br.ReadByte(), // 1 bytes
                                LastModificationDate = new DateTime(br.ReadInt64()) // 8 bytes
                            });
                        }

                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
                return null;
            }
            return l;
        }

        private int GetNextPageAvailable()
        {
            if (this.FileInfos.Count > 0)
            {
                var lf = this.FileInfos[this.FileInfos.Count - 1];
                return lf.StartAddr + lf.ComputeFileSizeInPageMultiple();
            }
            else return DATA_START_ADDR;
        }

        public bool Format()
        {
            if (this.FileInfos == null)
                this.FileInfos = new List<sFsFileInfo>();
            if (this.RemoveAll())
                return this.UpdateFileSystem();
            return false;
        }

        public bool RemoveAll()
        {
            this.Clean();
            this.FileInfos.Clear();
            return true;
        }

        public sFsFileInfo AddFile(string fileName, byte [] buffer)
        {
            var f = new MadeInTheUSB.sFs.sFsFileInfo(fileName, false)
            {
                LastModificationDate = DateTime.UtcNow,
                StartAddr = GetNextPageAvailable()
            };
            this.FileInfos.Add(f);
            if (f.SetBuffer(buffer))
            {
                var cu = ComputeUsedSpace();
                if(cu.RemainingFree < 0)
                {
                    this.FileInfos.Remove(f); // not enough space left
                    return null;
                }
                return f;
            }
            else
                return null;
        }

        public sFsFileInfo AddFile(string fileName)
        {
            var f = new MadeInTheUSB.sFs.sFsFileInfo(fileName, true)
            {
                LastModificationDate = DateTime.UtcNow,
                StartAddr = GetNextPageAvailable()
            };
            this.FileInfos.Add(f);
            return f;
        }

        public bool UpdateFileSystem()
        {
            var encoder   = new UnicodeEncoding();
            var fatBuffer = SerializeFAT();
            var errorCount = 0;

            foreach (var f in FileInfos)
            {
                if (f.Dirty)
                {
                    if (GetFATWriterReader().WriteFile(f.StartAddr, PublicEncryptor.C(this._pw, f.GetBufferInMutipleOfPage().ToArray())))
                        f.Dirty = false;
                    else
                        errorCount++;
                }
            }
            if (errorCount == 0)
            {
                if ((GetFATWriterReader().WriteFAT(0, FAT_START_ADDR, PublicEncryptor.C(this._pw, fatBuffer)))) // Write the FAT 2 page = 512 byte
                    return true;
                else
                    return false;
            }
            return false;
        }

        public class DiskUsage
        {
            public long TotalUsed       = 0;
            public long TotalSectorUsed = 0;
            public long RemainingFree   = 0;
            public long TotalSize       = 0;
            public long FileCount       = 0;

            public override string ToString()
            {
                return string.Format("{0} File(s), {1} byte used, {2} bytes free, {3} pages used", 
                        FileCount, TotalUsed, RemainingFree, TotalSectorUsed);
            }
        }

        public DiskUsage ComputeUsedSpace()
        {
            var du = new DiskUsage() { TotalSize = GetFATWriterReader().DiskMaxByte };
            du.FileCount = this.FileInfos.Count;
            foreach (var f in this.FileInfos)
            {
                du.TotalUsed += f.Length;
            }
            du.TotalSectorUsed = (FAT_END_ADDR + du.TotalUsed) / GetFATWriterReader().SectorSize;
            du.RemainingFree = GetFATWriterReader().DiskMaxByte - du.TotalUsed;
            return du;
        }

        internal string RemoveDoubleQuoteFromFileName(string fileName)
        {
            if (fileName.StartsWith(@""""))
                fileName = fileName.Substring(1);
            if (fileName.EndsWith(@""""))
                fileName = fileName.Substring(0, fileName.Length-1);
            return fileName;
        }

        public bool Delete(string fileName)
        {
            fileName = RemoveDoubleQuoteFromFileName(fileName);
            var f = this[fileName];
            if (f == null)
                return false;
            f.Attribute = sFsFileAttribute.Deleted;
            return true;
        }

     

        public bool ReadFileSystem()
        {
            var encoder      = new UnicodeEncoding();
            var r = GetFATWriterReader().LoadFAT(0, FAT_START_ADDR, FAT_END_ADDR);
            if (r == null)
                return false;
            this.FileInfos = DeSerializeFAT(PublicEncryptor.DC(this._pw, r));
            return FileInfos != null;
        }

        public sFsFileInfo LoadFileContent(string fileName)
        {
            return this.LoadFileContent(this[fileName]);
        }

        public sFsFileInfo LoadFileContent(sFsFileInfo fi)
        {
            var rr = GetFATWriterReader().ReadFile(fi.StartAddr, (uint)BinSerializer.ComputeFileSizePerSector(fi.Length, (int)GetFATWriterReader().SectorSize));
            if (rr != null)
            {
                fi.Buffer = PublicEncryptor.DC(this._pw, rr).Take(fi.Length).ToArray();
                return fi;
            }
            else
                throw new ApplicationException(string.Format("Cannot read data file content '{0}'", fi.FileName));
        }

        /// <summary>
        /// Compact or re claim all space used by deleted files. Deleted files are gone.
        /// This method use the memory as temporary space to save the non deleted file.
        /// This may not be possible when sFs will support Giga byte of data.
        /// Only the method call WriteAsFileSystem() is transactional
        /// </summary>
        /// <returns></returns>
        public bool Compact()
        {
            var ok = false;
            var newFileInfos = new List<sFsFileInfo>();
            try
            {
                foreach (var f in this.FileInfos) // Get rid of the deleted file
                {
                    if (f.Attribute != sFsFileAttribute.Deleted)
                    {
                        this.LoadFileContent(f); // Load the content of the non delete file in memory
                        newFileInfos.Add(f);
                    }
                }
                // Reassign StartPage to each non deleted file
                var startPage = DATA_START_ADDR;
                foreach (var f in newFileInfos)
                {
                    f.StartAddr = startPage;
                    startPage  += f.ComputeFileSizeInPageMultiple();
                    f.Dirty     = true;
                }
                ok = true;
            }
            catch (System.Exception ex)
            {

            }
            if (ok)
            {
                var oldFileInfos = this.FileInfos;
                try
                {
                    // Transactional part
                    this.FileInfos = newFileInfos;
                    return this.UpdateFileSystem();
                }
                catch
                {
                    this.FileInfos = oldFileInfos; // Restore old FAT
                    this.UpdateFileSystem(); // Save old FAT, we may also fail
                    return false;
                }
            }
            return false;
        }

        public bool Clean()
        {
            var errorCounter = 0;
            foreach(var f in this.FileInfos)
            {
                if (!f.Clean())
                    errorCounter++;
            }
            System.GC.Collect();
            return errorCounter == 0;
        }
    }
}