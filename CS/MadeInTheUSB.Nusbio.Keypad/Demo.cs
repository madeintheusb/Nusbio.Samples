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
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MadeInTheUSB;
using MadeInTheUSB.Buttons;
using MadeInTheUSB.GPIO;

namespace ButtonConsole
{
    class Demo
    {
        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Program).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if (attribs.Length > 0)
                return ((AssemblyProductAttribute)attribs[0]).Product;
            return null;
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("Nusbio Initializing");
            var serialNumber = Nusbio.Detect();
            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("Nusbio not detected");
                return;
            }

            using (var nusbio = new Nusbio(serialNumber))
            {
                Cls(nusbio);
                
                var keys = new List<List<char>>();
                keys.Add(new List<char>() { '1', '2', '3' });
                keys.Add(new List<char>() { '4', '5', '6' });
                keys.Add(new List<char>() { '7', '8', '9' });
                keys.Add(new List<char>() { '*', '0', '#' });

                var keypad = new Keypad(nusbio, 
                    gpioRow: new List<NusbioGpio>() { NusbioGpio.Gpio3, NusbioGpio.Gpio4, NusbioGpio.Gpio5, NusbioGpio.Gpio6 },
                    gpioCol: new List<NusbioGpio>() { NusbioGpio.Gpio0, NusbioGpio.Gpio1, NusbioGpio.Gpio2 },
                    keys   : keys
                );

                while (nusbio.Loop())
                {
                    var keyInfo = keypad.Check();
                    if(keyInfo!=null)
                    {
                        ConsoleEx.WriteLine(0, 4, string.Format("[{0:0}, {1:0}]Key:{2}", keyInfo.Row, keyInfo.Col, keyInfo.Key), ConsoleColor.DarkCyan);
                    }

                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.Q) break;
                    }
                }
            }
            Console.Clear();
        }
    }
}



