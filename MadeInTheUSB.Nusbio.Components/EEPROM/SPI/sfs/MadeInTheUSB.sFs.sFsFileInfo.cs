

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
        public string FileName;
        /// <summary>
        /// The file data when loaded in memory
        /// </summary>
        public byte[] Buffer;
        /// <summary>
        /// The start address of the disk
        /// </summary>
        public int StartAddr;
        /// <summary>
        /// The length of the file
        /// </summary>
        public int Length;
        /// <summary>
        /// The last time the file was modified
        /// </summary>
        public DateTime LastModificationDate;
        /// <summary>
        /// If the file is marked as deleted or not. We do not physically
        /// delete file or re-usage deleted space.
        /// The operation of Compactage reclaim un used space.
        /// </summary>
        public sFsFileAttribute Attribute = sFsFileAttribute.Normal;

        /// <summary>
        /// If true the file need to be save to the disk
        /// </summary>
        public bool Dirty = false;

        /// <summary>
        /// 
        /// </summary>
        const int PAGE_SIZE = 256;

        public List<byte> GetBufferInMutipleOfPage()
        {
            return BinSerializer.MakeBufferMultilpleOf(this.Buffer.ToList(), PAGE_SIZE);
        }

        public int GetStartPage()
        {
            return this.StartAddr / PAGE_SIZE;
        }

        public bool Deleted
        {
            get
            {
                return (this.Attribute & sFsFileAttribute.Deleted) == sFsFileAttribute.Deleted;
            }
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
        public sFsFileInfo(string fileName, bool tryToLoadFile)
        {
            this.FileName = Path.GetFileName(fileName);
            if (tryToLoadFile)
                this.LoadFileFromWindowsFS(fileName);
        }

        public bool SetBuffer(byte[] buffer)
        {
            this.Buffer = buffer;
            this.Dirty = true;
            this.Length = buffer.Length;
            return true;
        }

        public bool LoadFileFromWindowsFS(string fileName)
        {
            if (File.Exists(fileName))
            {
                var buffer = File.ReadAllBytes(fileName);
                return SetBuffer(buffer);
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

        public string GetBufferAsAsciiString()
        {
            var encoder = new System.Text.ASCIIEncoding();
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
            if (_tempFile != null)
            {
                if (File.Exists(_tempFile))
                {
                    BinSerializer.OverwriteFile(_tempFile);
                    File.Delete(_tempFile);
                }
                if (this.Buffer != null)
                {
                    BinSerializer.OverwriteBuffer(this.Buffer);
                    this.Buffer = null;
                }
            }
            return true;
        }
    }
}