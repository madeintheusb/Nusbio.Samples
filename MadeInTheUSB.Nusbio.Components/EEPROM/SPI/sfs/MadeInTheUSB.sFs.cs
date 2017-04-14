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
using System.Text;

namespace MadeInTheUSB.sFs
{
    public class sFsFileInfo
    {
        public const int MAX_FILE_NAME_SIZE = 32;

        /// <summary>
        /// The file name. This variable is not stored on the disk
        /// </summary>
        public string       FileName;
        /// <summary>
        /// The file data when loaded in memory
        /// </summary>
        public byte []      Buffer;
        /// <summary>
        /// The start page of the disk
        /// </summary>
        public int          StartPage;
        /// <summary>
        /// The length of the file
        /// </summary>
        public int          Length;
        /// <summary>
        /// The last time the file was modified
        /// </summary>
        public DateTime     LastModificationDate;

        /// <summary>
        /// If true the file need to be save to the disk
        /// </summary>
        public bool Dirty = false;

        const int PAGE_SIZE = 256;

        public List<byte>GetBufferInMutipleOfPage()
        {
            return BinSerializer.MakeBufferMultilpleOf(this.Buffer.ToList(), PAGE_SIZE);
        }

        /// <summary>
        /// Return the size of the file in multiple of the page size. 
        /// ((File.Length / PAGE_SIZE) + 1) * PAGE_SIZE;
        /// </summary>
        /// <returns></returns>
        public int ComputeFileSizeInPageMultiple()
        {
            return BinSerializer.ComputeFileSizePerSector(this.Length, PAGE_SIZE);
        }

        /// <summary>
        /// Constructor. Load a file from the PC into memory. this sFsFileInfo instance
        /// can later be added to the file system and save to the disk
        /// </summary>
        /// <param name="fileName"></param>
        public sFsFileInfo(string fileName)
        {
            this.FileName = Path.GetFileName(fileName);
            this.LoadFileFromWindowsFS(fileName);
        }

        public bool LoadFileFromWindowsFS(string fileName)
        {
            if (File.Exists(fileName))
            {
                this.Buffer = File.ReadAllBytes(fileName);
                this.Length = this.Buffer.Length;
                this.Dirty  = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the length of the file name once serialized for storage (AKA converted as UTF8)
        /// </summary>
        /// <returns></returns>
        public int GetFileNameSerialized()
        {
            return BinSerializer.SerializeStringInUTF8(this.FileName, MAX_FILE_NAME_SIZE).Length;
        }

        /// <summary>
        /// Initialize the data of the file from a string
        /// </summary>
        /// <param name="s"></param>
        public void SetContentAsString(string s)
        {
            var encoder = new UnicodeEncoding();
            this.Buffer = encoder.GetBytes(s);
            this.Length = this.Buffer.Length;
            this.Dirty = true;
        }

        /// <summary>
        /// Get the data as a unicode string
        /// </summary>
        /// <returns></returns>
        public string GetBufferAsUnicodeString()
        {
            var encoder = new UnicodeEncoding();
            return encoder.GetString(this.Buffer);
        }

        static List<string> extensionImages = new List<string>() {
            ".bmp", ".jpg", ".png", ".tif", ".gif"
        };

        public bool IsImage
        {
            get
            {
                var ext = Path.GetExtension(this.FileName).ToLowerInvariant();
                return extensionImages.Contains(ext);
            }
        }

        private string _tempFile;

        public string GetAsLocalTempFile()
        {
            _tempFile = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), this.FileName);
            File.WriteAllBytes(_tempFile, this.Buffer);
            return _tempFile;
        }

        public bool Clean()
        {
            if(_tempFile!=null)
            {
                if(File.Exists(_tempFile))
                {
                    BinSerializer.OverwriteFile(_tempFile);
                    File.Delete(_tempFile);
                }
                if(this.Buffer!=null)
                {
                    BinSerializer.OverwriteBuffer(this.Buffer);
                    this.Buffer = null;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// The MadeInTheUSB small File System class
    /// </summary>
    public class sFsManager : EEPROM.EEPROM_25AA1024
    {
        public string VolumeName = "MadeInTheUSB.sfs based on USB device Nusbio and EEPROM";
        public List<sFsFileInfo> FileInfos;

        /// <summary>
        /// Encryption password. This should be a SecureString or better.
        /// </summary>
        private string _pw;

#if NUSBIO2
        // Implementation for Nusbio v2 is not implemented yet
        public EEPROM_25AA1024_FILESYSTEM() : base(1024)
        {
        }
#else
        // Implementation for USB device Nusbio v1
        public sFsManager(
            string volumeName,
            string pw,
            Nusbio nusbio,
            NusbioGpio clockPin,
            NusbioGpio mosiPin,
            NusbioGpio misoPin,
            NusbioGpio selectPin,
            bool debug = false) : base(nusbio, clockPin, mosiPin, misoPin, selectPin, debug)
        {
            this._pw        = pw;
            this.VolumeName = volumeName;
            var b           = this.MaxByte;
            var p           = this.MaxPage;
            this.FileInfos = new List<sFsFileInfo>();
        }
#endif
        /// <summary>
        /// Page where the FAT start
        /// </summary>
        const int FAT_START_ADDR = 0;
        /// <summary>
        /// Size of the FAT
        /// </summary>
        const int FAT_END_ADDR = 256 * 8; // 2048 byte is the FAT size
        /// <summary>
        /// Magic value used to determine we read a valid FAT
        /// </summary>
        const int MAGIC_BYTE = 164;
        /// <summary>
        /// Where is the data section start
        /// </summary>
        const int DATA_START_ADDR = FAT_END_ADDR;
        /// <summary>
        /// 
        /// </summary>
        public const int MAX_FILE_PER_FAT = 42;

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
                        bw.Write((Int32)fi.StartPage); // 4 bytes
                        bw.Write((Int32)fi.Length);    // 4 bytes
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
                foreach(var f in this.FileInfos)
                    if (f.FileName.ToLowerInvariant() == fileName.ToLowerInvariant())
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
                            l.Add(new sFsFileInfo(fileName)
                            {
                                StartPage            = br.ReadInt32(), // 4 bytes
                                Length               = br.ReadInt32(), // 4 bytes
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
                return lf.StartPage + lf.ComputeFileSizeInPageMultiple();
            }
            else return DATA_START_ADDR;
        }

        public sFsFileInfo AddFile(string fileName)
        {
            var f = new MadeInTheUSB.sFs.sFsFileInfo(fileName)
            {
                LastModificationDate = DateTime.UtcNow,
                StartPage = GetNextPageAvailable()
            };
            this.FileInfos.Add(f);
            return f;
        }

        public bool WriteAsFileSystem()
        {
            var encoder   = new UnicodeEncoding();
            var fatBuffer = SerializeFAT();
            var errorCount = 0;

            foreach (var f in FileInfos)
            {
                if (f.Dirty)
                {
                    if (WriteAll(f.StartPage, PublicEncryptor.C(this._pw, f.GetBufferInMutipleOfPage().ToArray()).ToList()))
                        f.Dirty = false;
                    else
                        errorCount++;
                }
            }
            if (errorCount == 0)
            {
                if (WriteAll(FAT_START_ADDR, PublicEncryptor.C(this._pw, fatBuffer).ToList())) // Write the FAT 2 page = 512 byte
                    return true;
                else
                    return false;
            }
            return false;
        }

        private string ReadFile(int addr, int size)
        {
            var r = base.ReadPage(addr, BinSerializer.ComputeFileSizePerSector(size, PAGE_SIZE));
            var encoder = new UnicodeEncoding();
            var s = encoder.GetString(r.Buffer.Take(size).ToArray());
            return s;
        }

        public class DiskUsage
        {
            public long TotalUsed       = 0;
            public long TotalSectorUsed = 0;
            public long RemainingFree   = 0;
            public long TotalSize       = 0;
            public long FileCount = 0;

            public override string ToString()
            {
                return string.Format("{0} File(s), {1} byte used, {2} bytes free, {3} pages used", 
                        FileCount, TotalUsed, RemainingFree, TotalSectorUsed);
            }
        }

        public DiskUsage ComputeUsedSpace()
        {
            var du = new DiskUsage() { TotalSize = base.MaxByte };
            du.FileCount = this.FileInfos.Count;
            foreach (var f in this.FileInfos)
            {
                du.TotalUsed += f.Length;
            }
            du.TotalSectorUsed = (FAT_END_ADDR + du.TotalUsed) / PAGE_SIZE;
            du.RemainingFree = base.MaxByte - du.TotalUsed;
            return du;
        }

        public string Dir()
        {
            var s = new System.Text.StringBuilder();

            s.AppendFormat("Volume: {0}, {1} KByte", VolumeName, this.MaxKByte).AppendLine();
            foreach (var f in this.FileInfos)
            {
                s.AppendFormat("{0}, {1} bytes, {2}",
                    f.LastModificationDate,
                    f.Length.ToString().PadLeft(6),
                    f.FileName).AppendLine();
            }
            s.AppendLine();
            var cu = ComputeUsedSpace();
            s.Append(cu.ToString());

            return s.ToString();
        }

        public bool ReadFileSystem()
        {
            var encoder      = new UnicodeEncoding();

            // Read the FAT
            var r            = base.ReadPage(0, FAT_END_ADDR);
            FileInfos        = DeSerializeFAT(PublicEncryptor.DC(this._pw, r.Buffer));
            return FileInfos != null;

        }

        public sFsFileInfo LoadFileContent(string fileName)
        {
            return this.LoadFileContent(this[fileName]);
        }

        public sFsFileInfo LoadFileContent(sFsFileInfo fi)
        {
            var rr = base.ReadPage(fi.StartPage, BinSerializer.ComputeFileSizePerSector(fi.Length, PAGE_SIZE));
            if (rr.Succeeded)
            {
                fi.Buffer = PublicEncryptor.DC(this._pw, rr.Buffer);
                return fi;
            }
            else
                throw new ApplicationException(string.Format("Cannot read data file content '{0}'", fi.FileName));
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