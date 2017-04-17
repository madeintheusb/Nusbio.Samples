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

namespace MadeInTheUSB
{
    class Demo
    {
        private static sFs.sFsManager _sfsManager;
        
        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Program).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if (attribs.Length > 0)
                return ((AssemblyProductAttribute)attribs[0]).Product;
            return null;
        }

        const string ENCRYPTED_EXTENSION = ".x";

        private static void ExportKey(string keyXml, string keyName)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Export Key:"+keyName, ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            
            ConsoleEx.TitleBar(0, "Export " + keyName, ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            var tmpFile = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), keyName + ".txt");
            File.WriteAllText(tmpFile, keyXml);
            int exitCode = -1;
            var rr = ExecuteProgram.ExecProgram("notepad.exe", string.Format(@"""{0}""", tmpFile), true, ref exitCode, true, false);

            BinSerializer.OverwriteFile(tmpFile);
            File.Delete(tmpFile);

            ConsoleEx.WriteLine(0, 2, string.Format("The {0} file, has been overwritten & deleted from the disk", keyName), ConsoleColor.Cyan);
            Pause();
        }

        private static bool EncrypteFile(PrivatePublicKeyHelper ppk, string key, string keyType)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Encrypte File", ConsoleColor.Yellow, ConsoleColor.DarkBlue);

            Console.Clear();
            ConsoleEx.TitleBar(0, string.Format("Encrypte File With {0}", keyType), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteLine(0, 2, "Filename:", ConsoleColor.Cyan);
            var fileName = Console.ReadLine();

            ConsoleEx.WriteLine(0, 4, "", ConsoleColor.Cyan);

            if (!File.Exists(fileName))
            {
                ConsoleEx.WriteLine(0, 3, "Cannot find the file", ConsoleColor.Red);
                return false;
            }

            var dataFile = File.ReadAllBytes(fileName);
            var dataEncrypted = ppk.EncryptBuffer(dataFile, key);
            var tmpFile = fileName + ENCRYPTED_EXTENSION;

            if (File.Exists(tmpFile))
                File.Delete(tmpFile);

            File.WriteAllBytes(tmpFile, dataEncrypted);

            Console.WriteLine(string.Format("Encyrpted file is located at {0}", tmpFile), ConsoleColor.Cyan);
            Pause();
            return true;
        }

        const string LoremIpsum = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit.Integer nec odio.Praesent libero.Sed cursus ante dapibus diam. Sed nisi. Nulla quis sem at nibh elementum imperdiet.Duis sagittis ipsum.Praesent mauris. Fusce nec tellus sed augue semper porta.Mauris massa. Vestibulum lacinia arcu eget nulla.
Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.Curabitur sodales ligula in libero.Sed dignissim lacinia nunc. Curabitur tortor. Pellentesque nibh. Aenean quam. In scelerisque sem at dolor.Maecenas mattis. Sed convallis tristique sem. Proin ut ligula vel nunc egestas porttitor.Morbi lectus risus, iaculis vel, suscipit quis, luctus non, massa.Fusce ac turpis quis ligula lacinia aliquet.";

        static bool GeneratePrivateKeyPublicKey(string pw)
        {
            var ok = false;
            Console.Clear();
            ConsoleEx.TitleBar(0, "Generate new private/public key", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            Console.Clear();
            ConsoleEx.TitleBar(0, "Generate Private/Public Key", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            var keyInfo = ConsoleEx.Question(3, "Re generate the private key and publick key Y)es N)o", new List<char> { 'Y', 'N' });
            if (keyInfo == 'N') return ok;

            keyInfo = ConsoleEx.Question(3, "Are you sure you want to generate Private/Public Key Y)es N)o", new List<char> { 'Y', 'N' });
            if (keyInfo == 'N') return ok;

            ConsoleEx.WriteLine(0, 4, "Formatting...", ConsoleColor.Yellow);
            var ppk = new PrivatePublicKeyHelper(true);
            if(!_sfsManager.Format())
            {
                ConsoleEx.WriteLine(0, 3, "Format failed", ConsoleColor.Red);
            }

            ConsoleEx.WriteLine(0, 5, "Loading and saving files...", ConsoleColor.Yellow);
            var privatek = _sfsManager.AddFile("PrivateKey");
            privatek.SetContentAsString(ppk.PrivateKey);
            var publicK = _sfsManager.AddFile("PublicKey");
            publicK.SetContentAsString(ppk.PublicKey);
            var readmeTxt = _sfsManager.AddFile("ReadMe.txt");
            readmeTxt.SetContentAsString(LoremIpsum);
            var rr = _sfsManager.UpdateFileSystem();

            if (rr)
            {
                Console.WriteLine(string.Format("Keys were generated"), ConsoleColor.Cyan);
                ok = ReloadFileSystem(pw, ppk);
            }
            else
                Console.WriteLine(string.Format("Error generating the keys"), ConsoleColor.Red);

            Pause();
            return ok;
        }

        private static bool DecrypteFile(PrivatePublicKeyHelper ppk, string key, string keyType)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, string.Format("Decrypte file with {0}", keyType), ConsoleColor.Yellow, ConsoleColor.DarkBlue);

            ConsoleEx.WriteLine(0, 2, "Filename:", ConsoleColor.Cyan);
            var fileName = Console.ReadLine();
            ConsoleEx.Gotoxy(0, 4);

            if (!File.Exists(fileName))
            {
                ConsoleEx.WriteLine(0, 3, "Cannot find the file", ConsoleColor.Red);
                Pause();
                return false;
            }
            var sourceExtension = Path.GetExtension(fileName.Replace(ENCRYPTED_EXTENSION, ""));
            var dataFile = File.ReadAllBytes(fileName);
            var dataEncrypted = ppk.DecryptBuffer(dataFile, key);
            var tmpFile = fileName + sourceExtension;
            File.WriteAllBytes(tmpFile, dataEncrypted);

            Console.WriteLine(string.Format("Encyrpted file is located at {0}", tmpFile), ConsoleColor.Cyan);
            Pause();
            return true;
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

            if (!justTitle)
            {
                ConsoleEx.WriteMenu(1, 1, "1-Encrypte with public key");
                ConsoleEx.WriteMenu(1, 2, "2-Encrypte with private key");
                ConsoleEx.WriteMenu(1, 3, "3-Decrypte with private key");
                ConsoleEx.WriteMenu(1, 4, "4-Export public Key");
                ConsoleEx.WriteMenu(1, 5, "5-Export private Key");
                ConsoleEx.WriteMenu(1, 6, "6-Generate Private/Publick key");
                ConsoleEx.WriteMenu(1, 7, "Q)uit");
                ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
                ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
            }
            if (_sfsManager != null && _sfsManager.FileInfos != null)
            {
                ConsoleEx.Gotoxy(0, 9);
                Console.WriteLine(string.Format(@"{0}:\>Dir", _sfsManager.VolumeName) + Environment.NewLine);
                Console.WriteLine(_sfsManager.DirCommand());
            }
        }

        private static string AskForPW(Nusbio nusbio, string volumeName) {

            Cls(nusbio, true);
            ConsoleEx.Gotoxy(0, 1);
            Console.Write(string.Format("{0} - Password>", volumeName));
            var c = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Black;
            var pw = Console.ReadLine();
            Console.ForegroundColor = c;
            return pw;
        }

        const string VolumeName = "NusbioPrivateKeyDisk";

        private static bool ReloadFileSystem(string pw, PrivatePublicKeyHelper ppk)
        {
            Console.WriteLine("Reading FAT and loading files");
            var ok = _sfsManager.ReadFileSystem();
            if (ok)
            {
                ppk.PrivateKey = _sfsManager.LoadFileContent("PrivateKey").GetBufferAsUnicodeString();
                ppk.PublicKey  = _sfsManager.LoadFileContent("PublicKey").GetBufferAsUnicodeString();
            }
            return true;
        }

        private static void UnitTests(PrivatePublicKeyHelper ppk, sFs.sFsManager sFsManager)
        {
            Console.Clear();
            var sw = Stopwatch.StartNew();

            var source          = @"C:\@USB_STICK_Data\Img_4950.jpg";
            var sourceExtension = Path.GetExtension(source);
            ppk.PrivateKey      = sFsManager["PrivateKey"].GetBufferAsUnicodeString();
            ppk.PublicKey       = sFsManager["PublicKey"].GetBufferAsUnicodeString();
            var dataFile        = File.ReadAllBytes(source);
            var dataEncrypted   = ppk.EncryptBuffer(dataFile, ppk.PublicKey);
            var tmpFile         = source + ENCRYPTED_EXTENSION;
            File.WriteAllBytes(tmpFile, dataEncrypted);

            sw.Stop();
            Console.WriteLine("Encryption time: {0}", sw.ElapsedMilliseconds / 1000.0);

            sw            = Stopwatch.StartNew();
            var dataFile2 = File.ReadAllBytes(tmpFile);
            dataEncrypted = ppk.DecryptBuffer(dataFile2, ppk.PrivateKey);
            var newFile   = tmpFile + sourceExtension;
            File.WriteAllBytes(newFile, dataEncrypted);
            sw.Stop();
            Console.WriteLine("Dencryption time: {0}", sw.ElapsedMilliseconds / 1000.0);
            Pause();
        }

        static bool AddNewTextFile(string pw, string fileName = null)
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

        static List<ConsoleKey> DigitKeys = new List<ConsoleKey>()
        {
            ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.D3, ConsoleKey.D4, ConsoleKey.D5,
            ConsoleKey.D6, ConsoleKey.D7, ConsoleKey.D8, ConsoleKey.D9, ConsoleKey.D0,

        };

        public static void Run(string[] args)
        {
            var pw = string.Empty;

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
                _sfsManager = new sFs.sFsManager( VolumeName, pw, nusbio: nusbio, clockPin: NusbioGpio.Gpio0, mosiPin: NusbioGpio.Gpio1, misoPin: NusbioGpio.Gpio2, selectPin: NusbioGpio.Gpio3);
                _sfsManager.Begin();

                Cls(nusbio, true);
                Console.WriteLine(Environment.NewLine + Environment.NewLine);

                Console.WriteLine("Initializing private public key module");
                var ppk = new PrivatePublicKeyHelper(true);

                //GeneratePrivateKeyPublicKey(pw);

                var ok = ReloadFileSystem(pw, ppk);
                if (!ok)
                {
                    Cls(nusbio, true);

                    if (ConsoleEx.Question(3, string.Format("Cannot access {0} with this password, retry Y)es N)o",
                        _sfsManager.VolumeName), new List<char> { 'Y', 'N' }) == 'Y')
                        Environment.Exit(1);

                    Cls(nusbio, true); // Re format - re init key

                    var keyInfo = ConsoleEx.Question(4, string.Format("Would you like to initialize {0} Y)es N)o",
                        _sfsManager.VolumeName), new List<char> { 'Y', 'N' });
                    if (keyInfo == 'N') return;
                    GeneratePrivateKeyPublicKey(pw);
                }

                Cls(nusbio);
                

                while (nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var kk = Console.ReadKey(true);
                        var k = kk.Key;
                        
                        if (kk.Modifiers == ConsoleModifiers.Control)
                        {
                            if(DigitKeys.Contains(k))
                            {
                                var a = (int)k-48;
                                if (a == 0) a = 10;
                                a--;
                                ShowFile(a);
                            }
                        }
                        else
                        {

                            if (k == ConsoleKey.A)
                                AddNewTextFile(pw);
                            if (k == ConsoleKey.B)
                                AddNewTextFile(pw, @"Shadow.jpg");
                            
                            if (k == ConsoleKey.D1)
                                EncrypteFile(ppk, ppk.PublicKey, "PublicKey");
                            if (k == ConsoleKey.D2)
                                EncrypteFile(ppk, ppk.PrivateKey, "PrivateKey");
                            if (k == ConsoleKey.D3)
                                DecrypteFile(ppk, ppk.PrivateKey, "PrivateKey");
                            if (k == ConsoleKey.D4)
                                ExportKey(ppk.PublicKey, "PublicKey");
                            if (k == ConsoleKey.D5)
                                ExportKey(ppk.PrivateKey, "PrivateKey");
                            if (k == ConsoleKey.D6)
                                GeneratePrivateKeyPublicKey(pw);
                            if (k == ConsoleKey.U)
                                UnitTests(ppk, _sfsManager);
                            if (k == ConsoleKey.Q) break;
                        }
                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }

    }
}




