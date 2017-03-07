#define GPIO_15_AS_INPUT
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MadeInTheUSB;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.EEPROM;
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Sensor;

namespace MadeInTheUSB
{
    class Demo
    {
        static GY_9960LLC gy_9960LLC;

        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Program).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if(attribs.Length > 0)
                return  ((AssemblyProductAttribute) attribs[0]).Product;
            return null;
        }

        static void GestureMode(Nusbio nusbio, GY_9960LLC gy_9960LLC)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Gesture Mode", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 5, "Q)uit");
            while(true)
            {
                Thread.Sleep(100);
                var isGestureAvailable = gy_9960LLC.isGestureAvailable();
                //ConsoleEx.WriteMenu(-1, 2, string.Format("InterruptOn:{0}, isGestureAvailable:{1}",
                Console.WriteLine(string.Format("InterruptOn:{0}, isGestureAvailable:{1}", 
                    gy_9960LLC.InterruptOn(), isGestureAvailable
                    ));

                if(isGestureAvailable)
                {
                    var g = gy_9960LLC.readGesture();
                    if(g != GY_9960LLC.Direction.DIR_NONE)
                        Console.WriteLine(string.Format("Gest:{0}", g));
                }

                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey();
                    if (k.Key == ConsoleKey.Q)
                        break;
                }

            }
        }

        
        const int LIGHT_INT_HIGH = 1000;// High light level for interrupt
        const int LIGHT_INT_LOW   = 10;// Low light level for interrupt

        static bool Error(string m)
        {
            Console.WriteLine(m);
            Console.WriteLine("Press any key to continue");
            var k = Console.ReadKey();
            return false;
        }

        static bool LightSensorMode(Nusbio nusbio, GY_9960LLC apds)
        {
            //Console.Clear();
            //ConsoleEx.TitleBar(0, "Light Sensor Mode", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            //ConsoleEx.WriteMenu(-1, 5, "Q)uit");

            //// Set high and low interrupt thresholds
            //if (!apds.setLightIntLowThreshold(LIGHT_INT_LOW))
            //    return Error("Error writing low threshold");

            //if (!apds.setLightIntHighThreshold(LIGHT_INT_HIGH))
            //    return Error("Error writing high threshold");

            //// Start running the APDS-9960 light sensor (no interrupts)
            //if (apds.enableLightSensor(false))
            //{
            //    return Error("Light sensor is now running");
            //}
            //else
            //{
            //    Serial.println(F("Something went wrong during light sensor init!"));
            //}

            //// Read high and low interrupt thresholds
            //if (!apds.getLightIntLowThreshold(threshold))
            //{
            //    Serial.println(F("Error reading low threshold"));
            //}
            //else
            //{
            //    Serial.print(F("Low Threshold: "));
            //    Serial.println(threshold);
            //}
            //if (!apds.getLightIntHighThreshold(threshold))
            //{
            //    Serial.println(F("Error reading high threshold"));
            //}
            //else
            //{
            //    Serial.print(F("High Threshold: "));
            //    Serial.println(threshold);
            //}

            //// Enable interrupts
            //if (!apds.setAmbientLightIntEnable(1))
            //{
            //    Serial.println(F("Error enabling interrupts"));
            //}

            //// Wait for initialization and calibration to finish
            //delay(500);

            //while (true)
            //{
            //    Thread.Sleep(100);

            //    if (Console.KeyAvailable)
            //    {
            //        var k = Console.ReadKey();
            //        if (k.Key == ConsoleKey.Q)
            //            break;
            //    }

            //}
            return true;
        }


        static void Cls(Nusbio nusbio)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 5, "G)esture mode L)ight Sensor  Q)uit");
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
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
            
            var sclPin = NusbioGpio.Gpio0;
            var sdaPin = NusbioGpio.Gpio1;
            var interruptPin = NusbioGpio.Gpio2;

            using (var nusbio = new Nusbio(serialNumber)) // , 
            {
                gy_9960LLC = new GY_9960LLC(nusbio, sdaPin, sclPin, interruptPin, GY_9960LLC.APDS9960_I2C_ADDR);

                Cls(nusbio);
                
                while (nusbio.Loop())
                {
                    TimePeriod.Sleep(100);

                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.A)
                            Cls(nusbio);
                        if (k == ConsoleKey.G)
                            LightSensor(nusbio, gy_9960LLC);
                        if (k == ConsoleKey.G)
                            GestureMode(nusbio, gy_9960LLC);
                        if (k == ConsoleKey.Q)
                        {
                            break;
                        }
                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }
    }
}

