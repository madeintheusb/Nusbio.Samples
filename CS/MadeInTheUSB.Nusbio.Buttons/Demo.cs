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

                // See web site page, for wiring
                // - http://www.madeintheusb.net/TutorialInput/Index/#InputButton
                // - http://www.madeintheusb.net/Images/Fritzing/Button%202%20Wires%20PullUp_bb.png

                var buttons = new List<Button>()
                {
                    new Button(nusbio, NusbioGpio.Gpio6, inverse: true),
                    new Button(nusbio, NusbioGpio.Gpio7, inverse: true)
                };

                while (nusbio.Loop())
                {
                    for (var x = 0; x < buttons.Count; x++)
                    {

                        ConsoleEx.Write(0, 4 + x, string.Format("[{0}] Button {1} State:{2,-4}", DateTime.Now, x, buttons[x].GetButtonState()), ConsoleColor.DarkCyan);
                        if (buttons[x].GetButtonUpState())
                        {
                            ConsoleEx.Write(0, 6 + x, string.Format("[{0}] Button {1} {2}  ", DateTime.Now, x, buttons[x].GetState()), ConsoleColor.DarkCyan);
                        }
                        else if (buttons[x].GetButtonDownState())
                        {
                            ConsoleEx.Write(0, 6 + x, string.Format("[{0}] Button {1} {2}  ", DateTime.Now, x, buttons[x].GetState()), ConsoleColor.DarkCyan);
                        }
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



