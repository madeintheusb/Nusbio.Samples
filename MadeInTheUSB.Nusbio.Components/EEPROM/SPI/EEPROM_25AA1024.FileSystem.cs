/*
   Copyright (C) 2015, 2016 MadeInTheUSB LLC
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

namespace MadeInTheUSB.EEPROM
{
    public class FileInfo
    {
        public const int MAX_FILE_NAME_SIZE = 16;
        public const int MAX_FILE           = 15;

        public string       FileName;
        public byte []      Buffer;
        public int          StartPage;
        public int          Length;
        public DateTime     Created;



        const int PAGE_SIZE = 256;

        public List<byte>GetBufferInMutipleOfPage()
        {
            return BinSerializer.MakeBufferMultilpleOf(this.Buffer.ToList(), PAGE_SIZE);
        }

        public int GetBlockPage()
        {
            return BinSerializer.ComputeFileSizePerSector(this.Length, PAGE_SIZE);
        }

        public FileInfo(string fileName)
        {
            this.FileName = Path.GetFileName(fileName);
            if(File.Exists(fileName)) // The file may not exist
                this.Buffer = File.ReadAllBytes(fileName);
        }

        public int GetFileNameSerialized()
        {
            return BinSerializer.SerializeStringInUTF8(this.FileName, MAX_FILE_NAME_SIZE).Length;
        }

        public void SetBufferAsString(string s)
        {
            var encoder = new UnicodeEncoding();
            this.Buffer = encoder.GetBytes(s);
            this.Length = this.Buffer.Length;
        }

        public string GetBufferAsUnicodeString()
        {
            var encoder = new UnicodeEncoding();
            return encoder.GetString(this.Buffer);
        }
    }

    public class EEPROM_25AA1024_FILESYSTEM : EEPROM_25AA1024
    {

        public const string NusbioEEPROMDiskTitle = "Nusbio EEPROM Disk";
#if NUSBIO2
        public EEPROM_25AA1024_FILESYSTEM() : base(1024)
        {
        }
#else
        public EEPROM_25AA1024_FILESYSTEM(Nusbio nusbio,
            NusbioGpio clockPin,
            NusbioGpio mosiPin,
            NusbioGpio misoPin,
            NusbioGpio selectPin,
            bool debug = false) : base(nusbio, clockPin, mosiPin, misoPin, selectPin, debug)
        {
            var b = this.MaxByte;
            var p = this.MaxPage;
            DATA_START_ADDR = this.PAGE_SIZE * 2;
        }
#endif

        const int MAGIC_BYTE = 164;
        // we can store 62 file sizes per FAT, the fat is 256 byte
        // 1 byte magic number, 4 byte
        int DATA_START_ADDR = -1; // Computed
        int FAT_START_ADDR = 0;
        int FAT_END_ADDR = 256 * 2;

        internal byte[] SerializeMetadata(List<FileInfo> fileInfos)
        {
            byte[] buffer;

            if (fileInfos.Count > FileInfo.MAX_FILE)
                throw new ArgumentException("Too many files");

            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    bw.Write((byte)MAGIC_BYTE);             // 1 byte
                    bw.Write((Int32)fileInfos.Count);       // 2 byte
                    foreach (var fi in fileInfos)
                    {
                        bw.Write(BinSerializer.SerializeStringInUTF8(fi.FileName, FileInfo.MAX_FILE_NAME_SIZE)); // 16
                        bw.Write((Int32)fi.StartPage); // 4 byte
                        bw.Write((Int32)fi.Length); // 4 byte
                        bw.Write(fi.Created.Ticks);         // 8 bytes
                    }
                }
                buffer = ms.ToArray();
                buffer = BinSerializer.PadBuffer(buffer, FAT_END_ADDR);
                return buffer;
            }
        }

        internal List<FileInfo> DeSerializeMetadata(byte[] buffer)
        {
            var l = new List<FileInfo>();
            try
            {
                using (var ms = new MemoryStream(buffer))
                {
                    using (var br = new BinaryReader(ms, Encoding.UTF8))
                    {
                        int magicByte = br.ReadByte();
                        if (magicByte != MAGIC_BYTE)
                            throw new ApplicationException("Magic byte not found");
                        var fileCount = br.ReadInt32(); // 4

                        for (var i = 0; i < fileCount; i++)
                        {
                            var fileName = BinSerializer.DeSerializeStringInUTF8(br.ReadBytes(FileInfo.MAX_FILE_NAME_SIZE));
                            l.Add(new FileInfo(fileName) {
                                StartPage = br.ReadInt32(),
                                Length = br.ReadInt32(),
                                Created = new DateTime(br.ReadInt64()) // 8
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


        public bool WriteAsFileSystem(string pw, List<FileInfo> files)
        {
            var encoder = new UnicodeEncoding();
            var fileInfos = new List<FileInfo>();

            var startPage = DATA_START_ADDR;
            foreach (var f in files)
            {
                f.StartPage = startPage;
                fileInfos.Add(f);
                startPage += f.GetBlockPage();
            }

            var fatBuffer = SerializeMetadata(fileInfos);
            
            if (WriteAll(FAT_START_ADDR, PublicEncryptor.C(pw, fatBuffer).ToList())) // Write the FAT 2 page = 512 byte
            {
                foreach (var f in files)
                {
                    if (WriteAll(f.StartPage, PublicEncryptor.C(pw, f.GetBufferInMutipleOfPage().ToArray()).ToList()))
                    {
                    }
                }
            }
            return true;
        }


        public string ReadFile(int addr, int size)
        {
            var r = base.ReadPage(addr, BinSerializer.ComputeFileSizePerSector(size, PAGE_SIZE));
            var encoder = new UnicodeEncoding();
            var s = encoder.GetString(r.Buffer.Take(size).ToArray());
            return s;
        }

        public class DiskUsage
        {
            public long TotalUsed = 0;
            public long TotalSectorUsed = 0;
            public long RemainingFree = 0;
            public long TotalSize;

            public override string ToString()
            {
                return string.Format("TotalUsed {0}, TotalSectorUsed {1}, RemainingFree {2}", 
                        TotalUsed, TotalSectorUsed, RemainingFree);
            }
        }

        public DiskUsage ComputeUsedSpace(Dictionary<string,FileInfo> files)
        {
            var du = new DiskUsage() { TotalSize = base.MaxByte };
            foreach (var f in files)
            {
                du.TotalUsed += f.Value.Length;
            }
            du.TotalSectorUsed = (FAT_END_ADDR + du.TotalUsed) / PAGE_SIZE;
            du.RemainingFree = base.MaxByte - du.TotalSectorUsed;
            return du;
        }

        public void Dir(Dictionary<string, FileInfo> files)
        {
            Console.WriteLine(string.Format("Volume: {0}, {1} KByte", NusbioEEPROMDiskTitle, this.MaxKByte));
            foreach (var f in files)
                Console.WriteLine("{0}, {1} bytes, {2}",
                    f.Value.Created,
                    f.Value.Length.ToString().PadLeft(6),
                    f.Value.FileName);

            var cu = ComputeUsedSpace(files);
            Console.WriteLine(Environment.NewLine+cu.ToString());
        }

        public Dictionary<string, FileInfo> ReadFileSystem(string pw, bool loadFiles = true, params string [] fileToLoads)
        {
            var fileToLoads2 = fileToLoads.ToList().ConvertAll(d => d.ToLower());
            var files = new Dictionary<string, FileInfo>();
            var encoder      = new UnicodeEncoding();
            var r            = base.ReadPage(0, FAT_END_ADDR); // Read FAT 2 pages
            var fileInfos    = DeSerializeMetadata(PublicEncryptor.DC(pw, r.Buffer));

            if (fileInfos == null)
                return null;

            var addr         = this.DATA_START_ADDR;

            for (int i = 0; i < fileInfos.Count; i++)
            {
                var fi = fileInfos[i];
                if (loadFiles || fileToLoads2.Contains(fi.FileName.ToLowerInvariant()))
                {
                    var rr = base.ReadPage(fi.StartPage, BinSerializer.ComputeFileSizePerSector(fi.Length, PAGE_SIZE));
                    if (rr.Succeeded)
                    {
                        var b = PublicEncryptor.DC(pw, rr.Buffer).ToList();
                        fi.Buffer = b.Take(fi.Length).ToArray();
                    }
                    else
                        throw new ApplicationException(string.Format("Cannot read data file content '{0}'", fi.FileName));
                }
                files.Add(fi.FileName, fi);
            }
            return files;
        }
    }
}