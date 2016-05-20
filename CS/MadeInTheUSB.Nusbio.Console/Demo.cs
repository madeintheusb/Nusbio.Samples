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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MadeInTheUSB;
using MadeInTheUSB.GPIO;
using DynamicSugar;
using MadeInTheUSB.WinUtil;

namespace NusbioConsole
{
    class Demo
    {
        private static TimePeriod _timePeriod;

        private static void Configuration(Nusbio nusbio)
        {
            var title = "Nusbio Device EEPROM Configuration";
            Console.Clear();
            ConsoleEx.TitleBar(0, title);
            ConsoleEx.WriteLine(0, 1, "Loading configuration...", ConsoleColor.Gray);
            var config = nusbio.GetEEPROMConfiguration();
            foreach (var k in config)
            {
                Console.WriteLine("{0} = {1}", k.Key, k.Value);
            }
            Console.WriteLine(Environment.NewLine + "Hit enter to continue");
            Console.ReadKey();
            
            if(config["ADDriveCurrent"].ToString() == "4")
            {
                var mA = 16; // We are driving 16 mA
                Console.Clear();
                if (ConsoleEx.Question(23, "Increase GPIO current drive/sink to 16 mA Y)es N)o", DS.List('Y', 'N')) == 'Y')
                {
                    Console.Clear();
                    if (nusbio.UpdateADDriveCurrent(mA))
                    {
                        Console.WriteLine("UpdateADDriveCurrent() succeeded. Un plug Nusbio device");
                    }
                    else
                    {
                        Console.WriteLine("UpdateADDriveCurrent() failed. Un plug Nusbio device");
                    }
                    Console.WriteLine("Hit enter to continue");
                    Console.ReadKey();
                }
            }
            else if(config["ADDriveCurrent"].ToString() == "16")
            {
                var mA = 4; // We are driving 16 mA
                Console.Clear();
                if (ConsoleEx.Question(23, "Increase GPIO current drive/sink to 4 mA Y)es N)o", DS.List('Y', 'N')) == 'Y')
                {
                    Console.Clear();
                    if (nusbio.UpdateADDriveCurrent(mA))
                    {
                        Console.WriteLine("UpdateADDriveCurrent() succeeded. Un plug Nusbio device");
                    }
                    else
                    {
                        Console.WriteLine("UpdateADDriveCurrent() failed. Un plug Nusbio device");
                    }
                    Console.WriteLine("Hit enter to continue");
                    Console.ReadKey();
                }
            }

        }

        private static void AnimateBlocking3(Nusbio nusbio)
        {
            var maxGpio = 8;

            var gpiosSequence = DS.List(

                NusbioGpio.Gpio0,
                NusbioGpio.Gpio1,

                NusbioGpio.Gpio3,
                NusbioGpio.Gpio2,
                
                NusbioGpio.Gpio4,
                NusbioGpio.Gpio5,
                                                
                NusbioGpio.Gpio7,
                NusbioGpio.Gpio6
                
                );

            int max   = 36;
            int min   = 4;
            int step  = 2; 
            int delay = min;
            bool on   = false;

            while(true) {

                if (Console.KeyAvailable && Console.ReadKey(true).Key!=  ConsoleKey.Attention)
                    break;

                for(var i=0; i<maxGpio; i++ ){
                
                    nusbio[gpiosSequence[i]].DigitalWrite(PinState.High);
                    TimePeriod.Sleep(delay);
                    nusbio[gpiosSequence[i]].DigitalWrite(PinState.Low);
                    TimePeriod.Sleep(delay);
                    delay += on ? (step) : (-step);
                    if (delay > max) on = false;
                    if (delay < min) on = true;
                    if (delay < 0) delay = 0;

                }
            }
        }

        private static bool AnimateNonBlocking2(Nusbio nusbio)
        {
            if (nusbio.IsAsynchronousSequencerOn)
            {
                nusbio.CancelAsynchronousSequencer();
                return false;
            }
            else
            {
                nusbio.StartAsynchronousSequencer(100, seq: DS.List(

                    NusbioGpio.Gpio0,
                    NusbioGpio.Gpio1,
                    NusbioGpio.Gpio2,
                    NusbioGpio.Gpio3,
                    NusbioGpio.Gpio4,
                    NusbioGpio.Gpio5,
                    NusbioGpio.Gpio6,

                    NusbioGpio.Gpio7,

                    NusbioGpio.Gpio6,
                    NusbioGpio.Gpio5,
                    NusbioGpio.Gpio4,
                    NusbioGpio.Gpio3,
                    NusbioGpio.Gpio2,
                    NusbioGpio.Gpio1
                    
                    ));
                return true;
            }
        }

        private static void ReverseGpioLed3State(NusbioGpio led, Nusbio nusbio)
        {
            if (nusbio.GPIOS[led].AsLed.ExecutionMode == ExecutionModeEnum.Blinking)
                nusbio.GPIOS[led].AsLed.SetBlinkModeOff();
            else
                nusbio.GPIOS[led].AsLed.ReverseSet();
        }

private static void AnimateBlocking5(Nusbio nusbio)
{
    var waitTime0     = 500;
    var quit           = false;
    var lampGpio       = 0;
    var fiberOpticGpio = 1;
    var fanGpio        = 2;
    while (!quit) {
        nusbio[lampGpio].DigitalWrite(PinState.High);
        for (var g = 0; g < 10; g++)
        {
            for (var gg = 0; gg < 10; gg++)
            {
                nusbio[fanGpio].DigitalWrite((gg % 3) == 0);
                nusbio[fiberOpticGpio].DigitalWrite(PinState.High);
                Thread.Sleep(waitTime0);
                nusbio[fiberOpticGpio].DigitalWrite(PinState.Low);
                Thread.Sleep(waitTime0);
                if (Console.KeyAvailable && Console.ReadKey(true).Key != ConsoleKey.Attention) { quit = true; break; }
            }
        }
    }
    nusbio.SetAllGpioOutputState(PinState.Low);
}

        private static void AnimateBlocking1(Nusbio nusbio)
        {
            var maxRepeat = 3;
            var maxGpio   = 8;

            while (true)
            {
                for (var i = 0; i < maxRepeat; i++)
                {
                    for (var g = 0; g < maxGpio; g++)
                        nusbio[g].DigitalWrite(PinState.High);
                    Thread.Sleep(200);
                    for (var g = 0; g < maxGpio; g++)
                        nusbio[g].DigitalWrite(PinState.Low);
                    Thread.Sleep(200);
                }

                if (Console.KeyAvailable && Console.ReadKey(true).Key!=  ConsoleKey.Attention)
                    break;

                for (var i = 0; i < maxRepeat; i++)
                {
                    for (var g = 0; g < maxGpio; g++)
                        nusbio[g].AsLed.Set(true);
                    Thread.Sleep(200);
                    for (var g = 0; g < maxGpio; g++)
                        nusbio[g].AsLed.Set(false);
                    Thread.Sleep(200);
                }

                if (Console.KeyAvailable && Console.ReadKey(true).Key!=  ConsoleKey.Attention)
                    break;
            }
        }

        private static void ReverseLed3State(NusbioGpio led, Nusbio nusbio)
        {
            if (nusbio.GPIOS[led].AsLed.ExecutionMode == ExecutionModeEnum.Blinking)
                nusbio.GPIOS[led].AsLed.SetBlinkModeOff();
            else
                nusbio.GPIOS[led].AsLed.ReverseSet();
        }

        private static void ReverseGpio(NusbioGpio gpio, Nusbio nusbio)
        {
            if (nusbio.GPIOS[gpio].Mode == PinMode.Output)
            {
                nusbio.GPIOS[gpio].State = !nusbio.GPIOS[gpio].State;
                nusbio.GPIOS[gpio].DigitalWrite(nusbio.GPIOS[gpio].State ? PinState.High : PinState.Low);
            }
        }

        static string GetAssemblyProduct()
        {
            System.Reflection.Assembly currentAssem = typeof(Program).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if (attribs.Length > 0)
                return ((AssemblyProductAttribute)attribs[0]).Product;
            return null;
        }

        static void NusbioUrlEvent(string message)
        {
            ConsoleEx.Bar(0, 12, "Http Request:{0}".FormatString(message), ConsoleColor.Cyan, ConsoleColor.DarkCyan);
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);

            ConsoleEx.TitleBar(   ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar     (0, ConsoleEx.WindowHeight - 3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
            ConsoleEx.Bar     (0, ConsoleEx.WindowHeight - 4, "Web Server listening at {0}".FormatString(nusbio.GetWebServerUrl()), ConsoleColor.Black, ConsoleColor.DarkCyan);
            ConsoleEx.Bar     (0, ConsoleEx.WindowHeight - 5, "Machine Time Capabilities {0}".FormatString(TimePeriod.GetTimeCapabilityInfo()), ConsoleColor.Black, ConsoleColor.DarkCyan);

            ShowNusbioState(nusbio);
            NusbioUrlEvent("");
            
            ConsoleEx.WriteMenu(-1, 2, "Gpios: 0) 1) 2) 3) 4) 5) 6) 7) [Shift:Blink Mode]");
            ConsoleEx.WriteMenu(-1, 4, "F1) Animation  F2) Non Blocking Animation  F3) Animation 3");
            ConsoleEx.WriteMenu(-1, 6, "Q)uit  A)ll off  W)eb UI  C)onfiguration");
        }

        static void ShowNusbioState(Nusbio nusbio)
        {
            var b = new StringBuilder(100);

            b.AppendFormat("Gpios ");

            foreach (var g in nusbio.GPIOS)
            {
                if (g.Value.AsLed.ExecutionMode == ExecutionModeEnum.Blinking)
                    b.AppendFormat("{0}:Blinking, ", g.Value.Name);
                else
                    b.AppendFormat("{0}:{1}, ", g.Value.Name.Substring(4), g.Value.State ? "High" : "Low ");
            }
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 7, b.ToString().RemoveLastChar().RemoveLastChar(), ConsoleColor.Cyan, ConsoleColor.DarkCyan);

            var maskString = "Gpios Mask:{0} - {1}".FormatString(nusbio.GetGpioMask().ToString("000"), nusbio.GetGpioMaskAsBinary());
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 8, maskString, ConsoleColor.Cyan, ConsoleColor.DarkCyan);

            ConsoleEx.Gotoxy(0, 24);
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

            var halfSecondTimeOut = new TimeOut(20);

            using (var nusbio = new Nusbio(serialNumber: serialNumber, webServerPort: 1964))
            {
                nusbio.UrlEvent += NusbioUrlEvent;
                Cls(nusbio);
                while (nusbio.Loop(20))
                {
                    if (Console.KeyAvailable)
                    {
                        var kk        = Console.ReadKey(true);
                        var blinkMode = kk.Modifiers == ConsoleModifiers.Shift;
                        var key       = kk.Key;

                        if (key == ConsoleKey.Q) break;
                        if (key == ConsoleKey.C) Cls(nusbio);

                        if (blinkMode)
                        {
                            if (key == ConsoleKey.D0) nusbio.GPIOS[NusbioGpio.Gpio0].AsLed.SetBlinkMode(1000, 80);
                            if (key == ConsoleKey.D1) nusbio.GPIOS[NusbioGpio.Gpio1].AsLed.SetBlinkMode(1000, 80);
                            if (key == ConsoleKey.D2) nusbio.GPIOS[NusbioGpio.Gpio2].AsLed.SetBlinkMode(1000, 80);
                            if (key == ConsoleKey.D3) nusbio.GPIOS[NusbioGpio.Gpio3].AsLed.SetBlinkMode(1000, 80);
                            if (key == ConsoleKey.D4) nusbio.GPIOS[NusbioGpio.Gpio4].AsLed.SetBlinkMode(1000, 80);
                            if (key == ConsoleKey.D5) nusbio.GPIOS[NusbioGpio.Gpio5].AsLed.SetBlinkMode(1000, 80);
                            if (key == ConsoleKey.D6) nusbio.GPIOS[NusbioGpio.Gpio6].AsLed.SetBlinkMode(1000, 80);
                            if (key == ConsoleKey.D7) nusbio.GPIOS[NusbioGpio.Gpio7].AsLed.SetBlinkMode(1000, 80);
                        }
                        else
                        {
                            if (key == ConsoleKey.F5) AnimateBlocking5(nusbio);
                            if (key == ConsoleKey.F1) AnimateBlocking1(nusbio);
                            if (key == ConsoleKey.F2)
                            {
                                if (nusbio.IsAsynchronousSequencerOn) // If background sequencer for animation is on then turn it off if we receive any key
                                    nusbio.CancelAsynchronousSequencer();
                                else 
                                    AnimateNonBlocking2(nusbio);
                            }

                            if (key == ConsoleKey.F3) AnimateBlocking3(nusbio);
                            if (key == ConsoleKey.C) Configuration(nusbio);

                            if (key == ConsoleKey.D0) ReverseGpio(NusbioGpio.Gpio0, nusbio);
                            if (key == ConsoleKey.D1) ReverseGpio(NusbioGpio.Gpio1, nusbio);
                            if (key == ConsoleKey.D2) ReverseGpio(NusbioGpio.Gpio2, nusbio);
                            if (key == ConsoleKey.D3) ReverseGpio(NusbioGpio.Gpio3, nusbio);
                            if (key == ConsoleKey.D4) ReverseGpio(NusbioGpio.Gpio4, nusbio);
                            if (key == ConsoleKey.D5) ReverseGpio(NusbioGpio.Gpio5, nusbio);
                            if (key == ConsoleKey.D6) ReverseGpio(NusbioGpio.Gpio6, nusbio);
                            if (key == ConsoleKey.D7) ReverseGpio(NusbioGpio.Gpio7, nusbio);

                            if (key == ConsoleKey.A) nusbio.SetAllGpioOutputState(PinState.Low);
                            if (key == ConsoleKey.W) System.Diagnostics.Process.Start(nusbio.GetWebServerUrl());
                        }
                        ShowNusbioState(nusbio);
                        Cls(nusbio);
                    }
                    else { 
                        if(halfSecondTimeOut.IsTimeOut()) ShowNusbioState(nusbio);
                    }
                }
            }            
            Console.Clear();
        }
    }
}
