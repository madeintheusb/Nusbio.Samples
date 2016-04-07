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
using MadeInTheUSB.Sensor;

namespace LightSensorConsole
{
    class Demo
    {
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
        }

        private void Experiment(Nusbio nusbio, NusbioGpio inPin, NusbioGpio outPin) {

            nusbio.SetPinMode(inPin, PinMode.Input); // Raise Voltage to 1v at the + of the capacitor
            nusbio[0].High(); // Raise Voltage to 5v at the + of the capacitor
            Thread.Sleep(1000);
            nusbio[0].Low();
            nusbio.SetPinMode(inPin, PinMode.Output); // Raise Voltage to 1v at the + of the capacitor
            Thread.Sleep(500);
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("Nusbio initialization");
            var serialNumber = Nusbio.Detect();
            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("nusbio not detected");
                return;
            }
            
            var outPin = 0;
            var inPin  = 1;

            using (var nusbio = new Nusbio(serialNumber)) // , 
            {
                var pot = new Potentiometer(nusbio, inPin, outPin, 23, 290, 500); // 50k Pot
                var ddd = pot.PercentValue;

                Cls(nusbio);
                while(nusbio.Loop())
                {
            
                    ConsoleEx.Write(0, 10, string.Format("[{0}] Value:{1,-4}, Percent:{2}% ",
                        DateTime.Now, pot.Value, (int)pot.PercentValue),
                        ConsoleColor.DarkCyan);

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

//int count = 0;
                    
//if(nusbio.GPIOS[outPin].Mode == PinMode.Output)
//    nusbio.ResetGpios(inputGpios: new List<NusbioGpio>() {outPin});

//nusbio.GPIOS[inPin].DigitalWrite(PinState.Low);
//Thread.Sleep(5);
//nusbio.ResetGpios(inputGpios: new List<NusbioGpio>() {inPin});
//var tick = Environment.TickCount;

//var sw = System.Diagnostics.Stopwatch.StartNew();

//nusbio.GPIOS[outPin].DigitalWrite(PinState.High);
//while (nusbio.GPIOS[inPin].DigitalRead() == PinState.Low)
//{
//    count++;
//}
//tick = Environment.TickCount - tick;

//sw.Stop();
//var eElapsedMilliseconds = sw.ElapsedMilliseconds;

//Thread.Sleep(5);
//nusbio.ResetGpios(inputGpios: new List<NusbioGpio>() {outPin});

//ConsoleEx.Write(0, 9, string.Format("[{0}] Count:{1,-4}, Tick:{2,-5} eElapsedMilliseconds:{3,-5}",
//    DateTime.Now, count, tick, eElapsedMilliseconds), 
//    ConsoleColor.DarkCyan);
//Thread.Sleep(1000);   
