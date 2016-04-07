///*
//    Copyright (C) 2015 MadeInTheUSB LLC

//    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
//    associated documentation files (the "Software"), to deal in the Software without restriction, 
//    including without limitation the rights to use, copy, modify, merge, publish, distribute, 
//    sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
//    furnished to do so, subject to the following conditions:

//    The above copyright notice and this permission notice shall be included in all copies or substantial 
//    portions of the Software.

//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
//    LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
//    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
//    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
//    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//*/
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using nusbio.Lib;
//using MadeInTheUSB;
///*
// * Pyroelectric Infrared PIR Motion Sensor Detector Module
// * http://www.amazon.com/gp/product/B008AESDSY/ref=oh_aui_detailpage_o01_s00?ie=UTF8&psc=1
// */
//using MadeInTheUSB.GPIO;

//namespace LightSensorConsole
//{
//    class Demo
//    {
//        static string GetAssemblyProduct()
//        {
//            Assembly currentAssem = typeof(Program).Assembly;
//            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
//            if(attribs.Length > 0)
//                return  ((AssemblyProductAttribute) attribs[0]).Product;
//            return null;
//        }

//        static void Cls(LDevice nusbio)
//        {
//            Console.Clear();

//            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
//            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
//            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
//        }

//        public static void Run(string[] args)
//        {
//            Console.WriteLine("Nusbio initialization");
//            var serialNumber = LDevice.Detect();
//            if (serialNumber == null) // Detect the first Nusbio available
//            {
//                Console.WriteLine("nusbio not detected");
//                return;
//            }

//            var trigger     = NusbioGpio.Gpio0; // output
//            var lightSensor = NusbioGpio.Gpio1; // Input

//            using (var nusbio = new Nusbio(serialNumber, inputGpios: new List<NusbioGpio>() {lightSensor}))
//            {
//                var v2 = nusbio.GPIOS[lightSensor].DigitalRead();
//                var lightSensorGpio = nusbio.GPIOS[lightSensor];
//                Cls(nusbio);
//                while (true)
//                {
//                    int reading = 0;
//                    nusbio.GPIOS[trigger].DigitalWrite(true); // Send 5v in light sensor + capacitor
//                    //nusbio.ResetGpios(inputGpios: new List<NusbioGpio>() {lightSensor});

//                    while (lightSensorGpio.DigitalRead() == PinState.Low)
//                    {
//                        reading++;
//                        if (reading >= 30000)
//                            break;
//                    }
//                    nusbio.GPIOS[trigger].DigitalWrite(false);
//                    //Thread.Sleep(5);
//                    //while (lightSensorGpio.DigitalRead() == PinState.High)
//                    //{
//                    //    Thread.Sleep(5);
//                    //}

//                    ConsoleEx.Write(0, 8, string.Format("[{0}] Reading:{1,-10}", DateTime.Now, reading), ConsoleColor.DarkCyan);
//                    //var c = ConsoleEx.Question(24, "Continue?", new List<char>() {'Y', 'N'});
//                    //if(c == 'N')break;
                    
//                    if (Console.KeyAvailable)
//                    {
//                        var k = Console.ReadKey(true).Key;
//                        if (k == ConsoleKey.Q) break;
//                    }

//                    nusbio.BackgroundTask(2000);
//                    //nusbio.ResetGpios();
//                }
//            }
//            Console.Clear();
//        }
//    }
//}

