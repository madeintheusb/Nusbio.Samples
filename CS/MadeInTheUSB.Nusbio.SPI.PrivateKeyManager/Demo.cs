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
        private static EEPROM_25AA1024_FILESYSTEM _eeprom;
        private static Dictionary<string, EEPROM.FileInfo> _files;

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


        static void GeneratePrivateKeyPublicKey(string pw)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Generate new private/public key", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            Console.Clear();
            ConsoleEx.TitleBar(0, "Generate Private/Public Key", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            var keyInfo = ConsoleEx.Question(3, "Re generate the private key and publick key Y)es N)o", new List<char> { 'Y', 'N' });
            if (keyInfo == 'N') return;

            keyInfo = ConsoleEx.Question(3, "Are you sure you want to generate Private/Public Key Y)es N)o", new List<char> { 'Y', 'N' });
            if (keyInfo == 'N') return;

            ConsoleEx.Gotoxy(0, 4);

            var ppk = new PrivatePublicKeyHelper(true);
            var privatek = new MadeInTheUSB.EEPROM.FileInfo("PrivateKey") { Created = DateTime.UtcNow };
            privatek.SetBufferAsString(ppk.PrivateKey);

            var publicK = new MadeInTheUSB.EEPROM.FileInfo("PublicKey") { Created = DateTime.UtcNow };
            publicK.SetBufferAsString(ppk.PublicKey);

            var rr = _eeprom.WriteAsFileSystem(pw, new List<MadeInTheUSB.EEPROM.FileInfo>() {
                privatek, publicK
            });

            if (rr)
            {
                Console.WriteLine(string.Format("Keys were generated"), ConsoleColor.Cyan);
                _files = ReloadFileSystem(pw, ppk);
            }
            else
                Console.WriteLine(string.Format("Error generating the keys"), ConsoleColor.Red);
            Pause();
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
                ConsoleEx.WriteMenu(1, 2, "1-Encrypte with public key");
                ConsoleEx.WriteMenu(1, 3, "2-Encrypte with private key");
                ConsoleEx.WriteMenu(1, 4, "3-Decrypte with private key");
                ConsoleEx.WriteMenu(1, 5, "4-Export public Key");
                ConsoleEx.WriteMenu(1, 6, "5-Export private Key");
                ConsoleEx.WriteMenu(1, 7, "6-Generate Private/Publick key");
                ConsoleEx.WriteMenu(1, 8, "Q)uit");
                ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
                ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
            }
            
        }

        private static string AskForPW(Nusbio nusbio) {

            Cls(nusbio, true);
            ConsoleEx.Gotoxy(1, 1);
            Console.Write(string.Format("{0} - Password:", EEPROM_25AA1024_FILESYSTEM.NusbioEEPROMDiskTitle));
            var c = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Black;
            var pw = Console.ReadLine();
            Console.ForegroundColor = c;
            return pw;
        }

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
                pw = AskForPW(nusbio);

                _eeprom = new EEPROM_25AA1024_FILESYSTEM(
                    nusbio: nusbio,
                    clockPin: NusbioGpio.Gpio0,
                    mosiPin: NusbioGpio.Gpio1,
                    misoPin: NusbioGpio.Gpio2,
                    selectPin: NusbioGpio.Gpio3
                    );

                if (nusbio.Type == NusbioType.NusbioType1_Light)
                    _eeprom._spi.SoftwareBitBangingMode = true;

                _eeprom.Begin();

                Cls(nusbio, true);
                Console.WriteLine(Environment.NewLine + Environment.NewLine);

                Console.WriteLine("Initializing private public key module");
                var ppk = new PrivatePublicKeyHelper(true);

                _files = ReloadFileSystem(pw, ppk);

                if(_files == null)
                {
                    Cls(nusbio, true);
                    
                    if (ConsoleEx.Question(3, string.Format("Cannot access {0} with this password, retry Y)es N)o", EEPROM_25AA1024_FILESYSTEM.NusbioEEPROMDiskTitle), new List<char> { 'Y', 'N' }) == 'Y')
                        Environment.Exit(1);

                    Cls(nusbio, true); // Re format - re init key
                    
                    var keyInfo = ConsoleEx.Question(4, string.Format("Would you like to initialize {0} Y)es N)o", EEPROM_25AA1024_FILESYSTEM.NusbioEEPROMDiskTitle), new List<char> { 'Y', 'N' });
                    if (keyInfo == 'N') return;
                    GeneratePrivateKeyPublicKey(pw);
                }

                Cls(nusbio);
                ConsoleEx.Gotoxy(0, 11);
                Console.WriteLine(@"Nusbio:\>Dir" + Environment.NewLine);
                _eeprom.Dir(_files);

                while (nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;

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
                            UnitTests(ppk, _eeprom);
                        if (k == ConsoleKey.Q) break;
                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }

        private static Dictionary<string, EEPROM.FileInfo> ReloadFileSystem(string pw, PrivatePublicKeyHelper ppk)
        {
            Console.WriteLine("Reading FAT and loading files");
            _files = _eeprom.ReadFileSystem(pw, loadFiles: true);
            if (_files == null)
                return null;
            ppk.PrivateKey = _files["PrivateKey"].GetBufferAsUnicodeString();
            ppk.PublicKey  = _files["PublicKey"].GetBufferAsUnicodeString();
            return _files;
        }

        private static void UnitTests(PrivatePublicKeyHelper ppk, EEPROM_25AA1024_FILESYSTEM eeprom)
        {
            Console.Clear();
            var sw = Stopwatch.StartNew();

            var source          = @"C:\@USB_STICK_Data\Img_4950.jpg";
            var sourceExtension = Path.GetExtension(source);
            ppk.PrivateKey      = _files["PrivateKey"].GetBufferAsUnicodeString();
            ppk.PublicKey       = _files["PublicKey"].GetBufferAsUnicodeString();
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
    }
}


