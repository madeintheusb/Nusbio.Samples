/*
    Copyright (C) 2015 MadeInTheUSB LLC

    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
    associated documentation files (the "Software"), to deal in the Software without restriction, 
    including without limitation the rights to use, copy, modify, merge, publish, distribute, 
    sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all copies or substantial 
    portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
    LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
    WHETHER IN AN ACTION OF CONTRACT, TO    RT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using MadeInTheUSB;
using MadeInTheUSB.Components;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.EEPROM;
using MadeInTheUSB.Security;
using System.Security;

namespace MadeInTheUSB
{
    class Demo
    {
        private static sFs.sFsManager _sfsManager;
        private static sFs.sFsREPLCommand _sFsREPLCommand;

        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Program).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if (attribs.Length > 0)
                return ((AssemblyProductAttribute)attribs[0]).Product;
            return null;
        }


        const string ReadmeDotText = @"
NusbioDisk is a external USB disk including HAL (Hardare 
Abstraction Layer) and file system (MadeInTheUSB.sFs) 
written in C#, using the USB Nusbio.net device.

The first implementation use an 128 Kbyte SPI EEPROM.
";

        static bool FormatAndInitDisk()
        {
            var ok = false;
            Console.Clear();
            ConsoleEx.TitleBar(0, "Format and initialization", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            var keyInfo = ConsoleEx.Question(3, "Format and initialize disk Y)es N)o", new List<char> { 'Y', 'N' });
            if (keyInfo == 'N') return ok;

            ConsoleEx.WriteLine(0, 4, "Formatting...", ConsoleColor.Yellow);
            if(!_sfsManager.Format())
            {
                ConsoleEx.WriteLine(0, 3, "Format failed", ConsoleColor.Red);
                return false;
            }

            ConsoleEx.WriteLine(0, 5, "Loading and saving files...", ConsoleColor.Yellow);
            _sfsManager.AddFile(@".\files\Readme.txt");
            _sfsManager.AddFile(@".\files\Shadow.jpg");
            var rr = _sfsManager.UpdateFileSystem();

            if (rr)
            {
                Console.WriteLine(string.Format("Keys were generated"), ConsoleColor.Cyan);
                ok = ReloadFileSystem();
            }
            else
                Console.WriteLine(string.Format("Error generating the keys"), ConsoleColor.Red);

            Pause();
            return ok;
        }

        private static void Pause()
        {
            Console.WriteLine("Hit any key to continue");
            Console.ReadKey();
        }

        static void Cls(Nusbio nusbio, bool justTitle = false)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.Gotoxy(0, 1);
        }

        private static SecureString AskForPW(Nusbio nusbio, string volumeName) {

            Cls(nusbio, true);
            ConsoleEx.Gotoxy(0, 1);
            Console.Write(string.Format("{0} - Password>", volumeName));
            var c = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Black;
            SecureString ss = PublicEncryptor.ConvertToSecureString(Console.ReadLine());
            Console.ForegroundColor = c;
            return ss;
        }

        const string VolumeName = "NusbioDisk.sFs";

        private static bool ReloadFileSystem()
        {
            Console.WriteLine("Reading FAT and loading files");
            return _sfsManager.ReadFileSystem();
        }

        static bool AddNewLocalFile(string fileName = null)
        {
            string text = null;
            var ok   = false;
            Console.Clear();
            ConsoleEx.TitleBar(0, "Add Nex Text File", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.Write(0, 4, "File name>", ConsoleColor.Cyan);
            if (fileName == null)
            {
                fileName = Console.ReadLine();
                ConsoleEx.Write(0, 5, "Text file content>", ConsoleColor.Cyan);
                text = Console.ReadLine();
            }

            var newFile = _sfsManager.AddFile(fileName);
            if(text!=null)
                newFile.SetContentAsString(text);
            var rr = _sfsManager.UpdateFileSystem();

            if (rr)
            {
                Console.WriteLine("File saved", ConsoleColor.Cyan);
            }
            else
                Console.WriteLine("Error saving file", ConsoleColor.Red);

            Pause();
            return ok;
        }

        static void ShowFile(int fileIndex)
        {
            var fi = _sfsManager.FileInfos[fileIndex];
            Console.Clear();
            ConsoleEx.TitleBar(0, "Loading:" + fi.FileName, ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            fi = _sfsManager.LoadFileContent(fi);

            Console.Clear();
            ConsoleEx.TitleBar(0, "Viewing:" + fi.FileName, ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.Gotoxy(0, 1);

            if (fi.IsImage)
            {
                int exitCode = -1;
                var rr = ExecuteProgram.ExecProgram("mspaint.exe", string.Format(@"""{0}""", fi.GetAsLocalTempFile()), true, ref exitCode, true, false);
                if(rr && exitCode == 0)
                {

                }
                else
                {
                    ConsoleEx.WriteLine(0, 3, "Cannot open the file", ConsoleColor.Red);
                    Pause();
                }
            }
            else
            {
                var text = fi.GetBufferAsUnicodeString();
                Console.WriteLine(text);
                Pause();
            }
            _sfsManager.Clean();
        }

        public const string SFS_COMMAND_ERR_CANNOT_FIND_FILE    = "The system cannot find the file specified";
        public const string SFS_COMMAND_ERR_SAVING_FAT          = "Error saving FAT";
        public const string SFS_COMMAND_ERR_COPYING_FILE        = "Error copying file, check free space";
        public const string SFS_COMMAND_ERR_ZERO_FILE_COPIED    = "0 file(s) copied";
        public const string SFS_COMMAND_ERR_COMPACTAGE_FAILED = "Compactage failed";

        public const string SFS_COMMAND_MESSAGE_ONE_FILE_COPIED = "1 file(s) copied";
        public const string SFS_COMMAND_MESSAGE_FILE_DELETED    = "File deleted";
        public const string SFS_COMMAND_MESSAGE_COMPACTAGE_DONE = "Compactage done";

        const string COMMAND_HELP = @"
MadeInTheUSB.sfs - simple File system - command
  cls: clear screen
  dir: List all files
  dirAll: List all files including deleted
  type: Displays the contents of a text file 
  open: Copy the file on the Windows file system and open it
  exit: Quit the sFs console
  copy: Copies one file to another location on the sFs disk or a Windows disk
  del: Mark one file as deleted
  format: Format the sFs disk
  compact: reclaim used space from deleted files.
";


     

        

        public static void Run(string[] args)
        {
            SecureString pw; 

            Console.WriteLine("Nusbio initialization");
            Nusbio.ActivateFastMode();
            var serialNumber = Nusbio.Detect();

            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("nusbio not detected");
                return;
            }

            using (var nusbio = new Nusbio(serialNumber))
            {
                pw = AskForPW(nusbio, VolumeName);
                _sfsManager     = new sFs.sFsManager(VolumeName, pw, nusbio: nusbio, clockPin: NusbioGpio.Gpio0, mosiPin: NusbioGpio.Gpio1, misoPin: NusbioGpio.Gpio2, selectPin: NusbioGpio.Gpio3);
                _sFsREPLCommand = new sFs.sFsREPLCommand(_sfsManager);

                Cls(nusbio, true);
                Console.WriteLine(Environment.NewLine + Environment.NewLine);

                var ok = ReloadFileSystem();
                if (!ok)
                {
                    Cls(nusbio, true);
                    if (ConsoleEx.Question(3, string.Format("Cannot access {0} with this password, retry password Y)es N)o",
                        _sfsManager.VolumeName), new List<char> { 'Y', 'N' }) == 'Y')
                        Environment.Exit(1);

                    Cls(nusbio, true); // Re format - re init key
                    var keyInfo = ConsoleEx.Question(4, string.Format("Would you like to format {0} Y)es N)o", _sfsManager.VolumeName), new List<char> { 'Y', 'N' });
                    if (keyInfo == 'N') return;
                    FormatAndInitDisk();
                }

                Cls(nusbio);

                while (nusbio.Loop())
                {
                    Console.Write(string.Format(@"{0}:\>", _sfsManager.VolumeName));
                    var cmd = Console.ReadLine();
                    var cmdLC = cmd.ToLowerInvariant();
                    var tokens = cmdLC.Split(' ');
                    if (tokens.Length > 0)
                    {
                        if (tokens[0] == "exit") nusbio.ExitLoop();
                        if (tokens[0] == "format") FormatAndInitDisk();
                        if (tokens[0] == "cls") Console.Clear();
                        if (tokens[0] == "help") Console.WriteLine(COMMAND_HELP);
                        if (tokens[0] == "del")
                        {
                            if (_sfsManager.Delete(cmd.Substring(4)))
                            {
                                if(_sfsManager.UpdateFileSystem())
                                    Console.WriteLine(SFS_COMMAND_MESSAGE_FILE_DELETED);
                                else
                                    Console.WriteLine(SFS_COMMAND_ERR_SAVING_FAT);
                            }
                            else
                                Console.WriteLine(SFS_COMMAND_ERR_CANNOT_FIND_FILE);
                        }
                        if (tokens[0] == "compact")
                        {
                            if(_sfsManager.Compact())
                                Console.WriteLine(SFS_COMMAND_MESSAGE_COMPACTAGE_DONE);
                            else
                                Console.WriteLine(SFS_COMMAND_ERR_COMPACTAGE_FAILED);
                        }
                        if (tokens[0] == "type")   Console.WriteLine(_sFsREPLCommand.TypeCommand(cmd.Substring(5)));
                        if (tokens[0] == "open")   Console.WriteLine(_sFsREPLCommand.OpenCommand(cmd.Substring(5)));
                        if (tokens[0] == "dir")    Console.WriteLine(_sFsREPLCommand.DirCommand());
                        if (tokens[0] == "dirall") Console.WriteLine(_sFsREPLCommand.DirCommand(true));
                        if (tokens[0] == "copy")   Console.WriteLine(_sFsREPLCommand.CopyFileCommand(cmd.Substring(5)));
                    }
                }
            }
            Console.Clear();
        }
    }
}




