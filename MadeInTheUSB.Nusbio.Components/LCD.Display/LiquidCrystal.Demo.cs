/*
   This class is based on the Arduino LiquidCrystal Library.
   Thanks to whoever wrote this library. There is no copyright mentioned in the source code. 
    
   Copyright (C) 2015 MadeInTheUSB LLC
   Ported to C# and Nusbio by FT for MadeInTheUSB

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
  
    MIT license, all text above must be included in any redistribution
*/

using System;
using System.Globalization;
using MadeInTheUSB;
using MadeInTheUSB.GPIO;
using System.Linq;
using int16_t  = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t  = System.Byte;
using size_t   = System.Int16;
using MadeInTheUSB.WinUtil;
using System.Collections.Generic;

namespace MadeInTheUSB.Display
{
    public class LiquidCrystalDemo
    {
        public static void ApiDemoDisplay(ILiquidCrystal lc, int x, int y, string text, bool clear = true, int waitTime = 1000) {

            if(clear)
                lc.Clear();

            if(text.Length > lc.NumCols) {

                var p = lc.NumCols-1;
                while (p > 0 & text[p] != ' ')
                {
                    p--;
                }

                if(p==0)
                    p = lc.NumCols-1;

                lc.Print(x, y, text.Substring(0, p));
                lc.Print(x, y+1, text.Substring(p).TrimStart());
            }
            else {
                lc.Print(x, y, text);
            }

            if(waitTime> 0)
                TimePeriod.Sleep(waitTime);
        }
        
        public static void NusbioRocks(ILiquidCrystal lc, int wait = 180)
        {
            var autoScrollDemoText1 = "Nusbio for .NET          rocks or what?";
            lc.Clear();
            lc.Autoscroll();
            lc.SetCursor(lc.NumCols, 0);
            try { 
                foreach (var c in autoScrollDemoText1)
                {
                    lc.Print(c.ToString());
                    TimePeriod.Sleep(wait);
                    if (Console.KeyAvailable)
                        return;
                }
                TimePeriod.Sleep(1000);
            }
            finally{
                lc.NoAutoscroll();
                lc.Clear();
            }
        }

        public static void ProgressBarDemo(ILiquidCrystal lc)
        {
            lc.Clear();
            lc.Print(0, 0, "Working hard...");
            for (var p = 0; p <= 100; p += 10)
            {
                lc.ProgressBar(0, 1, 10, p, string.Format("{0}% ", p.ToString("000")));
                TimePeriod.Sleep(150);
                if (Console.KeyAvailable)
                {
                    lc.Clear();
                    return;
                }
            }
            TimePeriod.Sleep(1000);
            lc.Clear();
        }

        public static void NusbioRocksOrWhat(ILiquidCrystal lc)
        {
            lc.Clear();
            lc.Print(-1, 0, "Nusbio for .NET");
            lc.Print(-1, 1, "rocks or what!");
        }
        
        public static void ApiDemo(ILiquidCrystal lc) {

            ApiDemoDisplay(lc, 0, 0, " -- Api Demo --");

            // Display text
            ApiDemoDisplay(lc, 0, 0, "Display text on line 0 and 1");
            ApiDemoDisplay(lc, 0, 0, DateTime.Now.ToString("d"), waitTime:0);
            ApiDemoDisplay(lc, 0, 1, DateTime.Now.ToString("T"), clear:false);
            if (lc.NumLines > 2)
                ApiDemoDisplay(lc, 0, 2, DateTime.Now.ToString("dddd"), clear:false);    
            if (lc.NumLines > 3)
                ApiDemoDisplay(lc, 0, 3, CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month), clear:false);    
            
            // Turn  display on / off
            ApiDemoDisplay(lc, 0, 0, "About to turn the display off", waitTime:2000);
            lc.NoDisplay();
            TimePeriod.Sleep(1000);
            lc.Display();
            ApiDemoDisplay(lc, 0, 0, "Display turned on");

            // Flash Screen
            ApiDemoDisplay(lc, 0, 0, "About to flash the screen ...", waitTime:2000);
            ApiDemoDisplay(lc, 0, 0, "Flashing the screen ...", waitTime:0);
            lc.Flash(10);
            TimePeriod.Sleep(1000);
            
            // Cursor Blink Demo
            ApiDemoDisplay(lc, 0, 0, "Cursor blink mode on");
            lc.Blink();
            TimePeriod.Sleep(1000*4);
            ApiDemoDisplay(lc, 0, 0, "Cursor blink mode off");
            lc.NoBlink();
            
            // Cursor Demo
            ApiDemoDisplay(lc, 0, 0, "Display Cursor");
            lc.Cursor();
            TimePeriod.Sleep(1000);
            for (var i = 0; i < 15; i++) { 
                lc.SetCursor(i, 0); TimePeriod.Sleep(300);
            }
            for (var i = 15; i >= 0; i--) { 
                lc.SetCursor(i, 0); TimePeriod.Sleep(300);
            }
            TimePeriod.Sleep(1000*1);
            lc.NoCursor();
            ApiDemoDisplay(lc, 0, 0, "Cursor off");

            // Autoscroll demo
            ApiDemoDisplay(lc, 0, 0, "Autoscroll Demo", waitTime:2000);
            NusbioRocks(lc);

            // Progress Bar Demo
            ApiDemoDisplay(lc, 0, 0, "Progress Bar Demo");
            ProgressBarDemo(lc);

            ApiDemoDisplay(lc, 0, 0, "-- Demo Done --");
        }

        public static void CustomCharDemo(ILiquidCrystal lc) {

            var smiley = new List<string>() {
              "B00000",
              "B10001",
              "B00000",
              "B00000",
              "B10001",
              "B01110",
              "B00000",
              "B00000",
            };

            lc.CreateChar(0, BitUtil.ParseBinary(smiley).ToArray());
            lc.Clear();
            lc.Write(0);
            lc.Write(0xF4);
            lc.Write(0xFF);
            lc.Write(0xFF);
        }
    }
}