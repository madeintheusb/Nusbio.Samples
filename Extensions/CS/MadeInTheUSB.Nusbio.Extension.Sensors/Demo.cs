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
        static int                      _waitTime = 100; // 20
        static int                      _demoStep = 5;
        
        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Demo).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if(attribs.Length > 0)
                return  ((AssemblyProductAttribute) attribs[0]).Product;
            return null;
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            //ConsoleEx.WriteMenu(-1, 2, "0) --- ");
            ConsoleEx.WriteMenu(-1, 12, "Q)uit");
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        static AnalogLightSensor CalibrateLightSensor(AnalogLightSensor lightSensor, AnalogLightSensor.LightSensorType type)
        {
            switch (type)
            {
                case AnalogLightSensor.LightSensorType.CdsPhotoCell_3mm_45k_140k:
                    lightSensor.AddCalibarationValue("Dark"             , 000, 065);
                    lightSensor.AddCalibarationValue("Office Night"     , 066, 120);
                    lightSensor.AddCalibarationValue("Office Day"       , 121, 200);
                    lightSensor.AddCalibarationValue("Outdoor Sun Light", 201, 1024);
                break;
                case AnalogLightSensor.LightSensorType.Unknown:
                case AnalogLightSensor.LightSensorType.CdsPhotoCell_5mm_5k_200k :
                    lightSensor.AddCalibarationValue("Dark"             , 0, 100);
                    lightSensor.AddCalibarationValue("Office Night"     , 101, 299);
                    lightSensor.AddCalibarationValue("Office Day"       , 300, 400);
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

            var lightSensorAnalogPort       = 2;
            var motionSensorAnalogPort      = 0;
            var buttonSensorAnalogPort      = 1;
            var ledGpio                     = NusbioGpio.Gpio5;

            using (var nusbio = new Nusbio(serialNumber))
            {
                Cls(nusbio);

                var halfSeconds = new TimeOut(333);

                // Mcp300X Analog To Digital - SPI Config
                ad = new MCP3008(nusbio, 
                    selectGpio: NusbioGpio.Gpio3, 
                    mosiGpio:   NusbioGpio.Gpio1, 
                    misoGpio:   NusbioGpio.Gpio2, 
                    clockGpio:  NusbioGpio.Gpio0);
                ad.Begin();

                var analogMotionSensor = new AnalogMotionSensor(nusbio, 4);
                analogMotionSensor.Begin();

                var button = new AnalogButton(nusbio);

                var lightSensor = CalibrateLightSensor(new AnalogLightSensor(nusbio), AnalogLightSensor.LightSensorType.CdsPhotoCell_3mm_45k_140k);
                lightSensor.Begin();

                // Analog Port 5, 6, 7 are only available in
                // Analog Extension PCBv2
                const int multiButtonPort = 5;
                AnalogSensor multiButton = null;
                //multiButton = new AnalogSensor(nusbio, multiButtonPort);
                //multiButton.Begin();

                // TC77 Temperature Sensor SPI
                var tc77 = new TC77(nusbio,
                    clockGpio:  NusbioGpio.Gpio0,
                    mosiGpio:   NusbioGpio.Gpio1,
                    misoGpio:   NusbioGpio.Gpio2,
                    selectGpio: NusbioGpio.Gpio4
                    );
                tc77.Begin();
                
                if(nusbio.Type == NusbioType.NusbioType1_Light)
                {
                    tc77._spi.SoftwareBitBangingMode = true;
                    ad._spiEngine.SoftwareBitBangingMode = true;
                }

                while(nusbio.Loop())
                {                    
                    if (halfSeconds.IsTimeOut())
                    {
                        nusbio[ledGpio].AsLed.ReverseSet();
                        
                        ConsoleEx.WriteLine(0, 2, string.Format("{0,-15}", DateTime.Now), ConsoleColor.Cyan);
                        
                        lightSensor.SetAnalogValue(ad.Read(lightSensorAnalogPort));
                        ConsoleEx.WriteLine(0, 4, string.Format("Light Sensor       : {0,-18} (ADValue:{1:000.000}, Volt:{2:0.00})       ", 
                            lightSensor.CalibratedValue.PadRight(18), lightSensor.AnalogValue, lightSensor.Voltage), ConsoleColor.Cyan);

                        analogMotionSensor.SetAnalogValue(ad.Read(motionSensorAnalogPort));
                        var motionType = analogMotionSensor.MotionDetected();
                        if (motionType == DigitalMotionSensorPIR.MotionDetectedType.MotionDetected || motionType == DigitalMotionSensorPIR.MotionDetectedType.None)
                        {
                            ConsoleEx.Write(0, 6, string.Format("Motion Sensor      : {0,-18} (ADValue:{1:000.000}, Volt:{2:0.00})    ", 
                                motionType, analogMotionSensor.AnalogValue, analogMotionSensor.Voltage), ConsoleColor.Cyan);
                        }

                        ConsoleEx.WriteLine(0, 8, string.Format("Temperature Sensor : {0:0.00}C {1:0.00}F    ",tc77.GetTemperature(), 
                            tc77.GetTemperature(AnalogTemperatureSensor.TemperatureType.Fahrenheit)), ConsoleColor.Cyan);

                        button.SetAnalogValue(ad.Read(buttonSensorAnalogPort));
                        ConsoleEx.WriteLine(0, 10, string.Format("Button             : {0,-18} [{1:0000}, {2:0.00}V]   ",  
                            button.Down ? "Down" : "Up", button.AnalogValue, button.Voltage), ConsoleColor.Cyan);

                        if (multiButton != null)
                        {
                            multiButton.SetAnalogValue(ad.Read(multiButtonPort));
                            ConsoleEx.Write(0, 12, string.Format("Multi Button       : {0,-18} (ADValue:{1:000.000}, Volt:{2:000.000})",
                                multiButton.AnalogValue > 2 ? "Down" : "Up  ",
                                multiButton.AnalogValue,
                                multiButton.Voltage), ConsoleColor.Cyan);
                        }
                    }

                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;

                        if (k == ConsoleKey.C)
                        {
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.Q) {
                            
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

