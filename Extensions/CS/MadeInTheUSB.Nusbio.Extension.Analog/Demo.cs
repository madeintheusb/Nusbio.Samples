/*
   Copyright (C) 2015 MadeInTheUSB LLC

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
 
    Written by FT for MadeInTheUSB
    MIT license, all text above must be included in any redistribution
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
using MadeInTheUSB.Adafruit;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.Sensor;
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Components;

namespace DigitalPotentiometerSample
{
    class Demo
    {

        private static MCP3008 ad;
        static int _waitTime = 100; // 20
        static int _demoStep = 5;

        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Demo).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if (attribs.Length > 0)
                return ((AssemblyProductAttribute)attribs[0]).Product;
            return null;
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            //ConsoleEx.WriteMenu(-1, 2, "0) --- ");
            ConsoleEx.WriteMenu(-1, 12, "Q)uit");
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        static AnalogLightSensor CalibrateLightSensor(AnalogLightSensor lightSensor, AnalogLightSensor.LightSensorType type)
        {
            switch (type)
            {
                case AnalogLightSensor.LightSensorType.CdsPhotoCell_3mm_45k_140k:
                    lightSensor.AddCalibarationValue("Dark", 0, 20);
                    lightSensor.AddCalibarationValue("Office Night", 21, 65);
                    lightSensor.AddCalibarationValue("Office Day", 65, 100);
                    lightSensor.AddCalibarationValue("Outdoor Sun Light", 101, 1024);
                    break;
                case AnalogLightSensor.LightSensorType.Unknown:
                case AnalogLightSensor.LightSensorType.CdsPhotoCell_5mm_5k_200k:
                    lightSensor.AddCalibarationValue("Dark", 0, 100);
                    lightSensor.AddCalibarationValue("Office Night", 101, 299);
                    lightSensor.AddCalibarationValue("Office Day", 300, 400);
                    lightSensor.AddCalibarationValue("Outdoor Sun Light", 401, 1024);
                    break;
            }
            return lightSensor;
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("Nusbio initialization");
            var serialNumber = Nusbio.Detect();
            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("Nusbio not detected");
                return;
            }

            using (var nusbio = new Nusbio(serialNumber))
            {
                Cls(nusbio);
                var halfSeconds = new TimeOut(1000);
                /*
                    Mcp300X - SPI Config
                    gpio 0 - CLOCK
                    gpio 1 - MOSI
                    gpio 2 - MISO
                    gpio 3 - SELECT 
                */
                ad = new MCP3008(nusbio,
                    selectGpio: NusbioGpio.Gpio3,
                    mosiGpio: NusbioGpio.Gpio1,
                    misoGpio: NusbioGpio.Gpio2,
                    clockGpio: NusbioGpio.Gpio0);
                ad.Begin();

                var analogTempSensor = new Tmp36AnalogTemperatureSensor(nusbio);
                analogTempSensor.Begin();
                analogTempSensor.ReferenceVoltage = 5.05; // If you can measure your voltage out of the Nusbio VCC

                var analogMotionSensor = new AnalogMotionSensor(nusbio, 4);
                analogMotionSensor.Begin();

                var lightSensor = CalibrateLightSensor(new AnalogLightSensor(nusbio), AnalogLightSensor.LightSensorType.CdsPhotoCell_3mm_45k_140k);

                lightSensor.Begin();

                while (nusbio.Loop())
                {
                    if (halfSeconds.IsTimeOut())
                    {
                        const int lightSensorAnalogPort = 6;
                        const int motionSensorAnalogPort = 2;
                        const int temperatureSensorAnalogPort = 0;

                        ConsoleEx.WriteLine(0, 2, string.Format("{0,-20}", DateTime.Now, lightSensor.AnalogValue), ConsoleColor.Cyan);

                        lightSensor.SetAnalogValue(ad.Read(lightSensorAnalogPort));
                        ConsoleEx.WriteLine(0, 4, string.Format("Light Sensor       : {0} (ADValue:{1:000.000}, Volt:{2:000.000})    ",
                            lightSensor.CalibratedValue.PadRight(18),
                            lightSensor.AnalogValue,
                            lightSensor.Voltage), ConsoleColor.Cyan);

                        analogTempSensor.SetAnalogValue(ad.Read(temperatureSensorAnalogPort));
                        ConsoleEx.WriteLine(0, 6, string.Format("Temperature Sensor : {0:00.00}C, {1:00.00}F     (ADValue:{2:0000}, Volt:{3:000.000})      ",
                            analogTempSensor.GetTemperature(AnalogTemperatureSensor.TemperatureType.Celsius),
                            analogTempSensor.GetTemperature(AnalogTemperatureSensor.TemperatureType.Fahrenheit),
                            analogTempSensor.AnalogValue,
                            analogTempSensor.Voltage), ConsoleColor.Cyan);

                        analogMotionSensor.SetAnalogValue(ad.Read(motionSensorAnalogPort));
                        var motionType = analogMotionSensor.MotionDetected();
                        if (motionType == DigitalMotionSensorPIR.MotionDetectedType.MotionDetected || motionType == DigitalMotionSensorPIR.MotionDetectedType.None)
                        {
                            ConsoleEx.Write(0, 8, string.Format("Motion Sensor     : {0,-20} (ADValue:{1:000.000}, Volt:{2:000.000})", motionType, analogMotionSensor.AnalogValue, analogMotionSensor.Voltage), ConsoleColor.Cyan);
                        }
                    }

                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;

                        if (k == ConsoleKey.C)
                        {
                            Cls(nusbio);
                        }
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
