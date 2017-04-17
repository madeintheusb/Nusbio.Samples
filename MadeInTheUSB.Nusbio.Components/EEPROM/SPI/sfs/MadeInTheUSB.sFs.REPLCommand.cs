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
    /// <summary>
    /// The MadeInTheUSB small File System class
    /// </summary>
    public class sFsREPLCommand 
    {
        public const string SFS_COMMAND_ERR_CANNOT_FIND_FILE    = "The system cannot find the file specified";
        public const string SFS_COMMAND_ERR_SAVING_FAT          = "Error saving FAT";
        public const string SFS_COMMAND_ERR_COPYING_FILE        = "Error copying file, check free space";
        public const string SFS_COMMAND_ERR_ZERO_FILE_COPIED    = "0 file(s) copied";
        public const string SFS_COMMAND_ERR_COMPACTAGE_FAILED   = "Compactage failed";
        public const string SFS_COMMAND_ERR_INVALID_COMMAND     = "Invalid command, syntax error";

        public const string SFS_COMMAND_MESSAGE_ONE_FILE_COPIED = "1 file(s) copied";
        public const string SFS_COMMAND_MESSAGE_FILE_DELETED    = "File deleted";
        public const string SFS_COMMAND_MESSAGE_COMPACTAGE_DONE = "Compactage done";

        sFsManager _sFsManager;

        public sFsREPLCommand(sFsManager sFsManager)
        {
            _sFsManager = sFsManager;
        }

        private static bool IsFileFromWindowsFileSystem(string f)
        {
            return f.Length >= 2 && f[1] == ':';
        }

        private byte[] LoadFileInMemory(string f)
        {
            if (IsFileFromWindowsFileSystem(f))
                return File.ReadAllBytes(f);
            else
            {
                var fi = _sFsManager[f];
                if (fi == null)
                    return null;
                else
                {
                    _sFsManager.LoadFileContent(fi);
                    return fi.Buffer;
                }
            }
        }

        enum ParseCopyCommandMode
        {
            Begin,
            ParseSourceFile,
            DoneParsingSourceFile,
            ParseDestinationFile,
            DoneParsingDestinationFile,
        }

        public CopyCommandInfo ParseCopyCommand(string cmd)
        {
            var r = new CopyCommandInfo();
            var mode = ParseCopyCommandMode.Begin;
            cmd = cmd.Trim();

            for (var i = 0; i < cmd.Length; i++)
            {
                if (cmd[i] == '"')
                {
                    switch (mode)
                    {
                        case ParseCopyCommandMode.Begin: mode = ParseCopyCommandMode.ParseSourceFile; break;
                        case ParseCopyCommandMode.ParseSourceFile: mode = ParseCopyCommandMode.DoneParsingSourceFile; break;
                        case ParseCopyCommandMode.ParseDestinationFile: mode = ParseCopyCommandMode.DoneParsingDestinationFile; break;
                        case ParseCopyCommandMode.DoneParsingSourceFile: mode = ParseCopyCommandMode.ParseDestinationFile; break;
                        case ParseCopyCommandMode.DoneParsingDestinationFile:
                            r.Succeeded = true;
                            break;
                    }
                }
                else
                {
                    switch (mode)
                    {
                        case ParseCopyCommandMode.Begin: break;
                        case ParseCopyCommandMode.DoneParsingSourceFile: break;
                        case ParseCopyCommandMode.ParseSourceFile: r.SourceFile += cmd[i].ToString(); break;
                        case ParseCopyCommandMode.ParseDestinationFile: r.DestinationFile += cmd[i].ToString(); break;
                    }
                }
            }
            return r;
        }


        public class CopyCommandInfo
        {
            public string SourceFile, DestinationFile;
            public bool Succeeded;
        }
        
        public string CopyFileCommand(string cmd)
        {
            var c = this.ParseCopyCommand(cmd);
            if (!c.Succeeded)
                return SFS_COMMAND_ERR_INVALID_COMMAND;

            return CopyFileCommand(c);
        }

        public string CopyFileCommand(CopyCommandInfo c)
        {
            try
            {
                var srcByte = LoadFileInMemory(c.SourceFile);
                if (IsFileFromWindowsFileSystem(c.DestinationFile))
                {
                    File.WriteAllBytes(c.DestinationFile, srcByte);
                    return SFS_COMMAND_MESSAGE_ONE_FILE_COPIED;
                }
                else
                {
                    var fi = _sFsManager.AddFile(c.DestinationFile, srcByte);
                    if (fi == null)
                    {
                        Console.WriteLine(SFS_COMMAND_ERR_COPYING_FILE);
                    }
                    else
                    {
                        if (_sFsManager.UpdateFileSystem())
                            return SFS_COMMAND_MESSAGE_ONE_FILE_COPIED;
                    }
                }
            }
            catch (System.Exception ex)
            {

            }
            finally
            {
                _sFsManager.Clean();
            }
            return SFS_COMMAND_ERR_ZERO_FILE_COPIED;
        }

        public string TypeCommand(string fileName)
        {
            fileName = _sFsManager.RemoveDoubleQuoteFromFileName(fileName);

            var s = new System.Text.StringBuilder();

            var f = _sFsManager[fileName];

            if (f == null)
                return sFsManager.SFS_COMMAND_ERR_MESSAGE;

            _sFsManager.LoadFileContent(f);
            var t = f.GetBufferAsAsciiString();
            s.Append(t).AppendLine();

            return s.ToString();
        }

        public string OpenCommand(string fileName)
        {
            try
            {
                fileName = _sFsManager.RemoveDoubleQuoteFromFileName(fileName);
                var fi = _sFsManager[fileName];
                if (fi == null)
                    return sFsManager.SFS_COMMAND_ERR_MESSAGE;

                _sFsManager.LoadFileContent(fi);
                var localFile = fi.GetAsLocalTempFile();

                int exitCode = -1;
                if (fi.IsImage)
                {
                    var rr = ExecuteProgram.ExecProgram("mspaint.exe", string.Format(@"""{0}""", localFile), true, ref exitCode, true, false);
                    if (rr && exitCode == 0)
                        return "";
                    else
                        return "Cannot open the file";
                }
                else
                {
                    var rr = ExecuteProgram.OpenFile(string.Format(@"""{0}""", localFile), ref exitCode);
                    if (rr && exitCode == 0)
                        return "";
                    else
                        return "Cannot open the file";
                }
            }
            finally
            {
                _sFsManager.Clean();
            }
        }

        public string DirCommand(bool displayDeleted = false)
        {
            var s = new System.Text.StringBuilder();
            s.AppendLine();
            s.AppendFormat("Volume: {0}, {1} KByte", _sFsManager.VolumeName, _sFsManager.MaxKByte).AppendLine().AppendLine();
            foreach (var f in _sFsManager.FileInfos)
            {
                if ((displayDeleted) ||((f.Attribute & sFsFileAttribute.Deleted) != sFsFileAttribute.Deleted))
                {
                    s.AppendFormat(
                        //"{0}, {1} bytes, {2}, {3}:{4}, {5}",
                        "{0}, {1} bytes, {2}, {3}",
                        f.LastModificationDate.ToString("s"),
                        f.Length.ToString().PadLeft(6),
                        f.Attribute.ToString().PadLeft(7),
                        //f.GetStartPage().ToString().PadLeft(4),
                        //(f.ComputeFileSizeInPageMultiple() / PAGE_SIZE).ToString().PadRight(4),
                        f.FileName).AppendLine();
                }
            }
            s.AppendLine();
            var cu = _sFsManager.ComputeUsedSpace();
            s.Append(cu.ToString());
            s.AppendLine();
            return s.ToString();
        }
    }
}