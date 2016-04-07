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
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Components;
using System.Drawing;
using MadeInTheUSB.Components.APA;

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
            ConsoleEx.WriteMenu(-1, 5, "R)ainbow Demo  B)rightness Demo   S)equence Demo   RainboW) Demo [insync]" );
            ConsoleEx.WriteMenu(-1, 9, "Q)uit");
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        
        private static string TargetColors = @"Blue
BlueViolet
Brown
Chartreuse
Chocolate
CornflowerBlue
Crimson
Cyan
DarkOrange
DarkOrchid
DarkRed
DarkTurquoise
DarkViolet
DarkBlue
DarkCyan
DarkGoldenrod
DarkGreen
DarkMagenta
DeepPink
DeepSkyBlue
DodgerBlue
Firebrick
ForestGreen
Fuchsia
Gold
Green
Indigo
LawnGreen
LightSeaGreen
Lime
Maroon
MediumBlue
MediumSpringGreen
MediumVioletRed
MidnightBlue
Navy
Olive
Orange
OrangeRed
Purple
Red
RoyalBlue
SeaGreen
SpringGreen
Teal
Turquoise
Yellow
";

        public static void ColorsSequence(APA102LEDStrip ledStripe0, APA102LEDStrip ledStripe1)
        {
            var wait              = 300;
            var quit              = false;
            ledStripe0.Brightness = 7;
            ledStripe0.AllOff();

            Console.Clear();
            ConsoleEx.TitleBar(0, "Color Sequence Demo", ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 4, "Q)uit");

            var bkColors = TargetColors.Replace(Environment.NewLine, ",").Split(',').ToList();
            
            while (!quit)
            {
                foreach (var sBColor in bkColors) 
                {
                    if(string.IsNullOrEmpty(sBColor.Trim()))
                        continue;

                    var bkColor = APA102LEDStrip.DrawingColors[sBColor];
                    ConsoleEx.Gotoxy(1, 2);
                    ConsoleEx.WriteLine(string.Format("Background Color:{0}, Html:{1}, Dec:{2}", bkColor.Name.PadRight(16), APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor)), ConsoleColor.DarkCyan);

                    ledStripe0.Reset().AddRGBSequence(true, ledStripe0.Brightness, ledStripe0.MaxLed, bkColor).Show();
                    ledStripe1.Reset().AddRGBSequence(true, ledStripe1.Brightness, ledStripe0.MaxLed, bkColor).Show().Wait(wait);

                    if (Console.KeyAvailable)
                    {
                        quit = true;
                        break;
                    }
                }
            }
            ledStripe0.AllOff();
            var k = Console.ReadKey(true).Key;
        }


        public static void ComboWith6LedPanel(Nusbio nusbio, APA102LEDStrip ledStrip0, APA102LEDStrip ledStrip1 = null)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Rainbow Demo", ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 6, "Q)uit");

            int wait        = 50;
            var quit        = false;
            var maxStep     = 256;           
            var brightness = 5;
            int jStep = 8;
            ledStrip0.AllOff();
            ledStrip1.AllOff();

            var ledIndex = -1;
            var maxLedIndex = 4;
            var ledIndexTimeOut = new TimeOut(wait*50);

            while (!quit) 
            {                
                var j2 = maxStep;
                for(var j=0; j < maxStep; j += jStep) 
                {
                    ledStrip0.Reset();
                    ledStrip1.Reset();

                    for (var i = 0; i < ledStrip0.MaxLed; i++)
                        ledStrip0.AddRGBSequence(false, brightness, RGBHelper.Wheel(((i * maxStep / ledStrip0.MaxLed) + j)));

                    for (var i = 0; i < ledStrip1.MaxLed; i++)
                        ledStrip1.AddRGBSequence(false, brightness, RGBHelper.Wheel(((i * maxStep / ledStrip0.MaxLed) + j2)));

                    foreach (var bkColor in ledStrip0.LedColors)
                    {
                        ConsoleEx.WriteLine(1, 2,String.Format("Strip 0 - Color:{0}, Html:{1}, Dec:{2}", bkColor.Name, APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor)), ConsoleColor.DarkCyan);
                    }
                    foreach (var bkColor in ledStrip1.LedColors)
                    {
                        ConsoleEx.WriteLine(1, 3,String.Format("Strip 1 - Color:{0}, Html:{1}, Dec:{2}", bkColor.Name, APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor)), ConsoleColor.DarkCyan);
                    }
                    j2 -= jStep;
                    if (j2 < 0)
                        j2 = maxStep;

                    ledStrip0.Show();
                    ledStrip1.Show().Wait(wait);
                    /*
                    // Turn on and off led connected to gpio 0,1,2,3
                    if (ledIndexTimeOut.IsTimeOut())
                    {
                        if (ledIndex != -1)
                            nusbio[ledIndex].Low();
                        ledIndex++;
                        if (ledIndex == maxLedIndex)
                            ledIndex = 0;
                        nusbio[ledIndex].High();
                    }
                    */

                    if(Console.KeyAvailable) 
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.Q) 
                        { 
                            quit = true;
                            break;
                        }
                    }
                }
            }
            ledStrip0.AllOff();
            ledStrip1.AllOff();
        }

        public static bool PlayTrigonometrieVariation(APA102LEDStrip ledStrip, int wait, List<System.Drawing.Color> colorVariations, string varName, int loopCount = 1)
        {
            Console.WriteLine("Variation:{0}", varName);
            
            for (var i = 0; i < loopCount; i++)
            {
                foreach (var c in colorVariations)
                {
                    ledStrip.Reset();
                    ledStrip.AddRGBSequence(false, 10 , c);
                    ledStrip.Show().Wait(wait);
                    if (Console.KeyAvailable)
                        if (Console.ReadKey().Key == ConsoleKey.Q)
                            return false;
                }

            }
            return true;
        }

        public static void TrigonometrieOneColorDemo(APA102LEDStrip ledStrip)
        {
            var wait = 6;

            var maxColorValues   = 1024;
            var quit             = false;
            ledStrip.Brightness  = 11;
            ledStrip.AllOff();

            Console.Clear();
            ConsoleEx.TitleBar(0, "Trigonometric Demo");
            ConsoleEx.WriteMenu(-1, 2, "Q)uit");
            ConsoleEx.WriteMenu(-1, 3, "");
            
            // -- Still in reseach mode --

            var _4Sin_div_PI   = RGBHelper.GenerateTrigonometricValues(maxColorValues, Math.PI, radianV => 4 * Math.Sin(radianV)   / (1*Math.PI), colorMinValue: 64);
            var _4Sin3_div_3PI = RGBHelper.GenerateTrigonometricValues(maxColorValues, Math.PI, radianV => 4 * Math.Sin(3*radianV) / (3*Math.PI), colorMinValue: 64);
            var _4Sin5_div_5PI = RGBHelper.GenerateTrigonometricValues(maxColorValues, Math.PI, radianV => 4 * Math.Sin(5*radianV) / (5*Math.PI), colorMinValue: 64);
            var _4Sin7_div_7PI = RGBHelper.GenerateTrigonometricValues(maxColorValues, Math.PI, radianV => 4 * Math.Sin(7*radianV) / (7*Math.PI), colorMinValue: 64);

            // Green
            var greenColorVariation0 = RGBHelper.GenerateListOfColor(maxColorValues, null, _4Sin_div_PI,   null);
            var greenColorVariation1 = RGBHelper.GenerateListOfColor(maxColorValues, null, _4Sin3_div_3PI, null);
            var greenColorVariation2 = RGBHelper.GenerateListOfColor(maxColorValues, null, _4Sin5_div_5PI, null);
            var greenColorVariation3 = RGBHelper.GenerateListOfColor(maxColorValues, null, _4Sin7_div_7PI, null);
            
            while (!quit)
            {
                //if(!PlayTrigonometrieVariation(ledStrip, wait, greenColorVariation0, "4*Math.Sin(radianV)  /(1*Math.PI)")) break;
                //if(!PlayTrigonometrieVariation(ledStrip, wait, greenColorVariation1, "4*Math.Sin(3*radianV)/(3*Math.PI)"))  break;
                if(!PlayTrigonometrieVariation(ledStrip, wait, greenColorVariation2, "4*Math.Sin(5*radianV)/(5*Math.PI)"))  break;
                //if(!PlayTrigonometrieVariation(ledStrip, wait, greenColorVariation3, "4*Math.Sin(7*radianV)/(7*Math.PI)"))  break;

                if (Console.KeyAvailable)
                    if (Console.ReadKey().Key == ConsoleKey.Q)
                        break;    
            }
            ledStrip.AllOff();
        }

       
       

        public static void RainbowDemo(APA102LEDStrip ledStrip0, int jStep, APA102LEDStrip ledStrip1, bool inSync)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Rainbow Demo", ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 6, "Q)uit");

            int wait        = 13;
            var quit        = false;
            var maxStep     = 256;           
            var brightness = 5;
            ledStrip0.AllOff();
            ledStrip1.AllOff();

            while (!quit) 
            {                
                var j2 = maxStep;
                for(var j=0; j < maxStep; j += jStep) 
                {
                    if (inSync) // If in sync both led show the same color else show opposite color 
                    {
                        j2 = j;    
                    }

                    ledStrip0.Reset();
                    ledStrip1.Reset();

                    for (var i = 0; i < ledStrip0.MaxLed; i++)
                        ledStrip0.AddRGBSequence(false, brightness, RGBHelper.Wheel(((i * maxStep / ledStrip0.MaxLed) + j)));

                    for (var i = 0; i < ledStrip1.MaxLed; i++)
                        ledStrip1.AddRGBSequence(false, brightness, RGBHelper.Wheel(((i * maxStep / ledStrip0.MaxLed) + j2)));

                    foreach (var bkColor in ledStrip0.LedColors)
                    {
                        ConsoleEx.WriteLine(1, 2,String.Format("Strip 0 - Color:{0}, Html:{1}, Dec:{2}, WheelIndex:{3}      ", bkColor.Name, APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor), j), ConsoleColor.DarkCyan);
                    }
                    foreach (var bkColor in ledStrip1.LedColors)
                    {
                        ConsoleEx.WriteLine(1, 3,String.Format("Strip 1 - Color:{0}, Html:{1}, Dec:{2}, WheelIndex:{3}"     , bkColor.Name, APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor), j2), ConsoleColor.DarkCyan);
                    }

                    ledStrip0.Show();
                    ledStrip1.Show().Wait(wait);

                    if(Console.KeyAvailable) 
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.Q) 
                        { 
                            quit = true;
                            break;
                        }
                    }
                    j2--;
                }
            }
            ledStrip0.AllOff();
            ledStrip1.AllOff();
        }

        public static void LineDemo(APA102LEDStrip ledStripe)
        {
            int wait             = 20;
            var quit             = false;
            ledStripe.AllOff();
            Console.Clear();
            ConsoleEx.WriteMenu(-1, 4, "Q)uit");

            while (!quit) 
            {
                var j = 0;

                for (var i = 0; i < ledStripe.MaxLed; i++)
                {
                    var bkColor = RGBHelper.Wheel(((i * 256 / ledStripe.MaxLed) + j));
                    ledStripe.AddRGBSequence(true, 2, i+1, bkColor);
                    if(++j >= 256) j = 0;
                    while(!ledStripe.IsFull) ledStripe.AddRGBSequence(false, 2, Color.Black); 

                    ledStripe.Show().Wait(wait);

                    Console.WriteLine(String.Format("Color:{0}, Html:{1}, Dec:{2}",  bkColor.Name.PadRight(16), APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor)));

                    if(Console.KeyAvailable) {

                        while (Console.KeyAvailable) { 

                            var k = Console.ReadKey(true).Key;
                            if (k == ConsoleKey.Q) { 
                                quit = true;
                                break;
                            }
                        }
                    }
                }
                ledStripe.Wait(wait*3).AllOff();
            }
            ledStripe.AllOff();
        }

        public static void BrigthnessDemo(APA102LEDStrip ledStrip0, APA102LEDStrip ledStrip1)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Brightness Demo", ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 3, "Q)uit");

            ledStrip0.AllOff();
            ledStrip1.AllOff();
            var bkColors = TargetColors.Replace(Environment.NewLine, ",").Split(',').ToList();
            var wait     = 15;

            while (!Console.KeyAvailable)
            {
                foreach (var sBColor in bkColors)
                {
                    var bkColor = APA102LEDStrip.DrawingColors[sBColor];

                    for (var b = 1; b <= APA102LEDStrip.MAX_BRIGHTNESS; b += 2)
                    {
                        ConsoleEx.Write(1, 2, string.Format("Brightness {0:00}", b), ConsoleColor.DarkCyan);
                        ledStrip0.SetColor(b, bkColor).Show();
                        ledStrip1.SetColor(b, bkColor).Show().Wait(wait);
                    }

                    if (Console.KeyAvailable) break;
                    ledStrip0.Wait(wait * 10); // Wait when the fade in is done

                    for (var b = APA102LEDStrip.MAX_BRIGHTNESS; b >= 0; b -= 2)
                    {
                        ConsoleEx.Write(1, 2, string.Format("Brightness {0:00}", b), ConsoleColor.DarkCyan);
                        ledStrip0.SetColor(b, bkColor).Show();
                        ledStrip1.SetColor(b, bkColor).Show().Wait(wait);
                    }

                    if (Console.KeyAvailable) break;
                    ledStrip0.Wait(wait * 10); // Wait when the fade out is deon
                }
            }
            ledStrip0.AllOff();
            ledStrip1.AllOff();
            var k = Console.ReadKey(true).Key;
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

            //
            // Nusbio Extension - 2 RGB LED Panel
            //
            // For more information about the Nusbio APA102 2 Strip Adapter to control up to 2 strips 
            // with 10 RGB LED on each strip powered from Nusbio. See following url
            // http://www.madeintheusb.net/TutorialExtension/Index#Apa102RgbLedStrip

            using (var nusbio = new Nusbio(serialNumber))
            {
                Cls(nusbio);
                APA102LEDStrip ledStrip0 = APA102LEDStrip.Extensions.TwoStripAdapter.Init(nusbio, APA102LEDStrip.Extensions.LedPerMeter._1Led, APA102LEDStrip.Extensions.StripIndex._0, 1);
                APA102LEDStrip ledStrip1 = APA102LEDStrip.Extensions.TwoStripAdapter.Init(nusbio, APA102LEDStrip.Extensions.LedPerMeter._1Led, APA102LEDStrip.Extensions.StripIndex._1, 1);

                while(nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.Q)
                            break;

                        if (k == ConsoleKey.B)
                            BrigthnessDemo(ledStrip0, ledStrip1);

                        if (k == ConsoleKey.C)
                            ComboWith6LedPanel(nusbio, ledStrip0, ledStrip1);

                        if (k == ConsoleKey.R)
                            RainbowDemo(ledStrip0, 1, ledStrip1, false);

                        if (k == ConsoleKey.W)
                            RainbowDemo(ledStrip0, 1, ledStrip1, true);

                        if (k == ConsoleKey.F)
                            RainbowDemo(ledStrip0, 8, ledStrip1, true);

                        if (k == ConsoleKey.S)
                            ColorsSequence(ledStrip0, ledStrip1);

                        if (k == ConsoleKey.T)
                            TrigonometrieOneColorDemo(ledStrip1);

                        if (k == ConsoleKey.L)
                            LineDemo(ledStrip0);

                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }
    }
}


