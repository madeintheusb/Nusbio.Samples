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
using MadeInTheUSB.GPIO;

namespace LedConsole
{
    class Demo
    {
        const NusbioGpio dataPin  = NusbioGpio.Gpio0;  // BLUE Pin connected to DS of 74HC595
        const NusbioGpio latchPin = NusbioGpio.Gpio1;  // GREEN Pin connected to ST_CP of 74HC595
        const NusbioGpio clockPin = NusbioGpio.Gpio2;  // YELLOW Pin connected to SH_CP of 74HC595

        //private const int LSBFIRST = 0;
        //private const int MSBFIRST = 1;

        //static void shiftOut(Nusbio Nusbio, NusbioGpio dataPin , NusbioGpio clockPin , int bitOrder , int val)
        //{
        //    int i;
            
        //    for (i = 0; i < 8; i++)
        //    {
        //        if (bitOrder == LSBFIRST)
        //        {
        //            var a = (val & (1 << i));
        //            Nusbio.GPIOS[dataPin].DigitalWrite(Nusbio.ConvertToPinState(a));
        //        }
        //        else
        //        {
        //            var b = (val & (1 << (7 - i)));
        //            Nusbio.GPIOS[dataPin].DigitalWrite(Nusbio.ConvertToPinState(b));
        //        }
        //        Nusbio.GPIOS[clockPin].DigitalWrite(PinState.High);
        //        Nusbio.GPIOS[clockPin].DigitalWrite(PinState.Low);
        //    }   
        //}

        //static void Register74HC595_8Bit_Send8BitValue(Nusbio Nusbio, byte v) {

        //    Nusbio.GPIOS[latchPin].DigitalWrite(PinState.Low);            
        //    shiftOut(Nusbio, dataPin, clockPin, MSBFIRST, v);
        //    Nusbio.GPIOS[latchPin].DigitalWrite(PinState.High);
        //}
        

        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Program).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if(attribs.Length > 0)
                return  ((AssemblyProductAttribute) attribs[0]).Product;
            return null;
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
            ConsoleEx.WriteMenu(-1, 2, "A)nimation");
            ConsoleEx.WriteMenu(-1, 4, "Q)uit");
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
                var sr = new ShiftRegister74HC595(nusbio, dataPin, latchPin, clockPin);
                sr.Send8BitValue(0);
                while (nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.Q) break;
                        if (k == ConsoleKey.A) 
                            sr.AnimateOneLeftToRightAndRightToLeft(15);
                    }
                }
            }
            Console.Clear();
        }
    }
}

