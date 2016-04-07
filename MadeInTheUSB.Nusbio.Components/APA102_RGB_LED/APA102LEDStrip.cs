/*
   The class APA102LEDStrip was written with the help of the following:
   
       Adafruit_DotStar
       https://github.com/adafruit/Adafruit_DotStar/blob/master/Adafruit_DotStar.cpp
  
       apa102-arduino
       https://github.com/pololu/apa102-arduino
  
       The Wheel() function comes from the Adafruit code 
       https://github.com/adafruit/Adafruit_NeoPixel

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
  
   MIT license, all text above must be included in any redistribution* 
  
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
 
   About data transfert speed:
   ===========================
   The APA 102 RGB LED strip protocol work this way:
        Move at the begining of the strip -- StartFrame()
        For each LED we send 4 bytes
            B1 - A header + Intensity
            B2 - Blue value 0..255
            B3 - Green value 0..255
            B4 - Red value 0..255
        Send EndFrame()
 
   Nusbio using the SPI like protocol sedning data to an APA 102 LED strip, can transfer out 
   between 7k and 10k byte/seconds dependent on the performance of the Windows machine.
 
   10434 / 4 BytePerLed = 2608 => This means it would take 1 seconds to light up one strip with 2608 LED
 
   60 Led * 4 byte = 240 byte / 10434 byte/sec = 0.023 s
   
   See file Nusbio.Samples\MadeInTheUSB.Nusbio.NusbioRGB\Demo.cs, method SpeedTest(),
   with a 60 LED strip.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
#if COLOR_MINE
using ColorMine.ColorSpaces;
#endif
using MadeInTheUSB.i2c;
using MadeInTheUSB.WinUtil;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
//using MadeInTheUSB.Component;
using System.Drawing;
using MadeInTheUSB.GPIO;

namespace MadeInTheUSB.Components.APA
{
    [Flags]
    public enum APA102CommandFrame : byte
    {
        None        = 1,
        Start       = 2,
        End         = 4,
        StartAndEnd = 4 + 2,
    }

    /// <summary>
    /// 
    /// *** ATTENTION ***
    /// 
    /// WHEN CONTROLLING AN APA LED STRIP WITH NUSBIO YOU MUST KNOW THE AMP CONSUMPTION.
    /// 
    /// USB DEVICE ARE LIMITED TO 500 MILLI AMP.
    /// 
    /// AN LED IN GENERAL CONSUMES FROM 20 TO 25 MILLI AMP. AN RGB LED CONSUMES 3 TIMES 
    /// MORE IF THE RED, GREEN AND BLUE ARE SET TO THE 255, 255, 255 WHICH IS WHITE
    /// AT THE MAXIMUN INTENSISTY WHICH IS 31.
    /// 
    /// YOU MUST KNOW WHAT IS THE MAXIMUN CONSUMPTION OF YOUR APA 102 RGB LEB STRIP WHEN THE 
    /// RGB IS SET TO WHITE, WHITE, WHITE AND THE BRIGTHNESS IS AT THE MAXIMUM.
    /// 
    ///    -------------------------------------------------------------------------------
    ///    --- NEVER GO OVER 300 MILLI AMP IF THE LED STRIP IS POWERED FROM THE NUSBIO ---
    ///    -------------------------------------------------------------------------------
    /// 
    ///         POWER ONLY A LED STRIP OF 5 LED WHEN DIRECTLY PLUGGED INTO NUSBIO.
    /// 
    /// THE FUNCTION AmpTest() WILL LIGHT UP THE FIRST LED OF THE STRIP AT MAXIMUM BRIGHTNESS.
    /// USE A MULTI METER TO WATCH THE AMP COMSUMPTION.
    /// 
    /// IF YOU WANT TO POWER MORE THAN 5 LEDS, THERE ARE 2 SOLUTIONS:
    /// 
    /// (1) ONLY FOR 6 to 10 LEDs. ADD BETWEEN NUSBIO VCC AND THE STRIP 5V PIN A 47 OHM RESISTORS.
    /// YOU WILL LOOSE SOME BRIGTHNESS, BUT IT IS SIMPLER. THE RESISTOR LIMIT THE CURRENT THAT
    /// CAN BE USED FROM THE USB.
    /// 
    /// (2) USE A SECOND SOURCE OF POWER LIKE:
    /// 
    ///  - A 5 VOLTS 1 AMPS ADAPTERS TO POWER A 30 LED STRIP
    ///  - A 5 VOLTS 2 AMPS ADAPTERS TO POWER A 60 LED STRIP
    ///  
    /// ~~~ ATTENTION ~~~
    /// 
    ///     WHEN USING A SECOND SOURCE OF POWER IN THE SAME BREADBOARD OR PCB, ~ NEVER ~ 
    ///     CONNECT THE POSISTIVE OF THE SECOND SOURCE OF POWER WITH THE NUSBIO VCC.
    /// 
    /// SEE OUR WEB SITE 'LED STRIP TUTORIAL' FOR MORE INFO.
    /// 
    /// </summary>
    public class APA102LEDStrip
    {
        public const int MAX_BRIGHTNESS      = 31;
        public const int MIN_BRIGHTNESS      = 1;
        public const int FIRST_BYTE_SEQUENCE = 224; // B11100000

        /// <summary>
        /// This is a dangerous option
        /// </summary>
        public static bool CombineSPIDataAndClockOnSameCycle = false;
        public static bool OptimizeDataLine = false;

        private Nusbio _nusbio;
        private int    _clockPin;
        private int    _dataPin;
        private int    _brightness;
        private bool   _shiftBrightNessInc = true;

        public int MaxLed;
        public List<Color> LedColors;
        
        /// <summary>
        /// http://colormine.org/color-converter
        /// </summary>
        public void TestC()
        {
            int v1 = 0, v2 = 0; // the new vertex and the previous one

            for (var hue = 0; hue < 360 ; hue += 2)
            {
                var light = 40;
                for (var sat = 40; sat <= 80; sat += 10)
                {
                    var color = RGBHelper.HsvToRgb(hue, sat, light);

                    Console.WriteLine(" hue:{0:000} sat:{1:000}, light:{2:000}, color:{3:000}", hue, sat, light, color);
                    this.AddRGBSequence(true, 6, this.MaxLed, color);
                    this.Show();
                    Thread.Sleep(2);
                }
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Q)
                        break;
                }
            }
        }

        public APA102LEDStrip SetColor(int brightness, Color color)
        {
            this.Reset();
            for (var l = 0; l < this.MaxLed; l++)
                this.AddRGB(color, brightness);
            return this;
        }

        public void ShiftBrightness(int value)
        {
            if (_shiftBrightNessInc)
            {
                this._brightness += value;
                if (this._brightness > MAX_BRIGHTNESS)
                {
                    this._brightness = MAX_BRIGHTNESS;
                    _shiftBrightNessInc = false;
                }
            }
            else
            {
                this._brightness -= value;
                if (this._brightness < 1)
                {
                    this._brightness = MIN_BRIGHTNESS;
                    _shiftBrightNessInc = true;
                }
            }
        }

        public int Brightness
        {
            get { return this._brightness; }
            set
            {
                this._brightness = value;
                if (this._brightness < 0) this._brightness = 1;
                if (this._brightness > MAX_BRIGHTNESS) this._brightness = MAX_BRIGHTNESS;
            }
        }

        public APA102LEDStrip(Nusbio nusbio, int ledCount, int clockPin, int dataPin)
        {
            this._brightness = MAX_BRIGHTNESS / 3;
            this.MaxLed      = ledCount;
            this._nusbio     = nusbio;
            this._clockPin   = clockPin;
            this._dataPin    = dataPin;
            nusbio.SetPinMode(dataPin, GPIO.PinMode.Output);
            nusbio.SetPinMode(clockPin, GPIO.PinMode.Output);
            this.Init();
            this.AllOff();
        }

        public APA102LEDStrip Reset()
        {
            this.LedColors = new List<Color>();
            return this;
        }

        public bool IsFull
        {
            get { return this.LedColors.Count >= this.MaxLed; }
        }

        public APA102LEDStrip ShowAndShiftRightAllSequence(int wait)
        {
            for (var i = 0; i < this.MaxLed; i++)
            {
                this.Show().ShiftRightSequence().Wait(wait);
            }
            return this;
        }

        public APA102LEDStrip ShiftRightSequence()
        {
            var cl = this.LedColors.Last();
            this.LedColors.RemoveAt(this.LedColors.Count - 1);
            this.LedColors.Insert(0, cl);

            return this;
        }

        public APA102LEDStrip InsertRGBSequence(int index, int brightness, params Color[] colors)
        {
            foreach (var c in colors)
            {
                if (this.IsFull)
                    break;
                else
                    this.InsertRGB(c, index, brightness);
            }
            return this;
        }

        public APA102LEDStrip AddRGBSequence(bool reset, int brightness, int times, Color color)
        {
            if (reset)
                this.Reset();

            for (var i = 0; i < times; i++)
            {
                AddRGBSequence(false, brightness, color);
            }
            return this;
        }

        public APA102LEDStrip AddRGBSequence(bool reset, int brightness, params Color[] colors)
        {
            if (reset)
                this.Reset();

            foreach (var c in colors)
            {
                if (this.IsFull)
                    break;
                else
                    this.AddRGB(c, brightness);
            }
            return this;
        }

        public APA102LEDStrip AddRGBSequence(bool reset, params Color[] colors)
        {
            return this.AddRGBSequence(reset, this.Brightness, colors);
        }

        public APA102LEDStrip ReverseSequence()
        {
            this.LedColors.Reverse();
            return this;
        }

        public APA102LEDStrip AddHSV(double hue, double saturation, double lightness, int brightness = -1)
        {
            return this.AddRGB(RGBHelper.HsvToRgb(hue, saturation, lightness), brightness);
        }

        public APA102LEDStrip AddRGB(Color c, int brightness = -1)
        {
            Color c2 = c;

            if (brightness != -1)
            {
                c2 = Color.FromArgb(brightness, c.R, c.G, c.B);
            }
            this.LedColors.Add(c2);
            return this;
        }

        public APA102LEDStrip InsertRGB(Color c, int index, int brightness = -1)
        {
            Color c2 = c;

            if (brightness != -1)
            {
                c2 = Color.FromArgb(brightness, c.R, c.G, c.B);
            }
            this.LedColors.Insert(index, c2);
            return this;
        }

        public APA102LEDStrip AddRGB(int r, int g, int b)
        {
            this.AddRGB(Color.FromArgb(r, g, b));
            return this;
        }

        public APA102LEDStrip AddHSV(double h, double S, double V)
        {
            this.AddRGB(RGBHelper.HsvToRgb(h, S, V));
            return this;
        }

        public APA102LEDStrip AllOff()
        {
            return AllToOneColor(0, 0, 0);
        }

        public APA102LEDStrip AllToOneColor(int r, int g, int b, int brightness = -1)
        {
            this.Reset();
            for (var i = 0; i < this.MaxLed; i++)
                this.AddRGB(Color.FromArgb(r, g, b), brightness);
            this.Show();
            return this;
        }

        public APA102LEDStrip AllToOneColor(Color c, int brightness = -1)
        {
            this.Reset();
            for (var i = 0; i < this.MaxLed; i++)
                this.AddRGB(c, brightness);
            this.Show();
            return this;
        }

        public APA102LEDStrip SetColors(List<Color> colors)
        {
            this.LedColors = colors;
            this.Show();
            return this;
        }

        public APA102LEDStrip Wait(int duration = 10)
        {
            if (duration > 0)
                TimePeriod.Sleep(duration);
            return this;
        }

        public APA102LEDStrip Show(int brightness = 32)
        {
            if (this.LedColors.Count != this.MaxLed)
                throw new ArgumentException(string.Format("The APA102 Led strip has {0} leds, {1} led color definitions provided", this.MaxLed, this.LedColors.Count));

            // Send color in buffer mode.
            // To set an led we must send 4 bytes:
            // - (1) Header + Brightness
            // - (2) R value 0..255
            // - (3) G value 0..255
            // - (4) B value 0..255
            // Because of the way the APA 102 work if we send the 4 bytes for LED x, it does not mean
            // it will turn on until we send some other byte for the next LED or send the end of frame
            var intBuffer = new List<int>() {0, 0, 0, 0}; // Start frame
            foreach (Color c in this.LedColors)
            {
                int brightN = c.A; // By default we use the Alpha value for the brightness
                if (brightN < 0 || brightN > MAX_BRIGHTNESS)
                    brightN = this._brightness; // use global brightness setting if not defined in the alpha
                intBuffer.AddRange(new List<int>()
                {
                    APA102LEDStrip.FIRST_BYTE_SEQUENCE | brightN, // First byte + Brightness
                    c.B, c.G, c.R
                });
            }
            intBuffer.AddRange(new List<int>() { 0xFF,0xFF,0xFF,0xFF}); // End frame
            this.Transfer(intBuffer.ToArray());
            return this;
        }

        public void Init()
        {
            _nusbio.DigitalWrite(this._dataPin,  GPIO.PinState.Low);
            _nusbio.DigitalWrite(this._clockPin, GPIO.PinState.Low);
        }

        public void StartFrame()
        {
            this.Init();
            this.Transfer(0, 0, 0, 0);
        }

        public void EndFrame()
        {
            this.Transfer(0xFF, 0xFF, 0xFF, 0xFF);
        }

        //private void Transfer(int b)
        //{
        //    var gs = new GpioSequence((this._DigitalWriteRead as Nusbio).GetGpioMask());
        //    gs.ShiftOut(this._DigitalWriteRead as Nusbio, this._dataPin, this._clockPin, b);
        //    gs.Send(this._DigitalWriteRead as Nusbio);
        //}

        //public void Transfer(int b0, int b1, int b2, int b3)
        // {
        //     var gs = new GpioSequence((this._DigitalWriteRead as Nusbio).GetGpioMask());
        //     gs = Shift.ShiftOutHardWare(this._DigitalWriteRead as Nusbio, this._dataPin, this._clockPin, b0, gs, send: false);
        //     gs = Shift.ShiftOutHardWare(this._DigitalWriteRead as Nusbio, this._dataPin, this._clockPin, b1, gs, send: false);
        //     gs = Shift.ShiftOutHardWare(this._DigitalWriteRead as Nusbio, this._dataPin, this._clockPin, b2, gs, send: false);
        //     gs = Shift.ShiftOutHardWare(this._DigitalWriteRead as Nusbio, this._dataPin, this._clockPin, b3, gs, send: true);
        // }

        private void Transfer(params int[] buffer)
        {
            var s = this._nusbio.GetTransferBufferSize();
            var gs = new GpioSequence((this._nusbio as Nusbio).GetGpioMask(), s);
            var i = 0;
            while (i < buffer.Length)
            {
                if (gs.EmptySpace >= GpioSequence.BIT_PER_BYTE)
                {                    
                    // Add one byte to the gpio sequence
                    gs.ShiftOut(this._nusbio as Nusbio, this._dataPin, this._clockPin, buffer[i], dataAndClockSameCycleOptimized :CombineSPIDataAndClockOnSameCycle);
                    i += 1;
                }
                else
                {
                    gs.Send(this._nusbio as Nusbio, OptimizeDataLine);
                    var lastMaskValue = gs[gs.Count - 1];
                    //gs = new GpioSequence((this._DigitalWriteRead as Nusbio).GetGpioMask());
                    gs = new GpioSequence(lastMaskValue, this._nusbio.GetTransferBufferSize());
                }
            }
            if (gs.Count > 0)
                gs.Send(this._nusbio as Nusbio, OptimizeDataLine);
        }

        public static string ToHexValue(Color color)
        {
            return "#" + color.R.ToString("X2") +
                   color.G.ToString("X2") +
                   color.B.ToString("X2");
        }

        public static Color ToBrighterRgb(Color color, int percent = 10)
        {
            double r = color.R*(1 + (percent/100.0));
            double g = color.G*(1 + (percent/100.0));
            double b = color.B*(1 + (percent/100.0));
            
            Color c = Color.FromArgb((byte) r, (byte) g, (byte) b);

            return c;
        }

        public static Color ToBrighter(Color color, int percent = 10)
        {
            double hue;
            double saturation;
            double value;
            RGBHelper.ColorToHSV(color, out hue, out saturation, out value);

            saturation = saturation * (1 + (percent/100.0));
            value = value * (1 + (percent/100.0));

            return RGBHelper.HsvToRgb(hue, saturation, value);
        }

        public static string ToDecValue(Color color)
        {
            return color.R.ToString("000") + "-" +
                   color.G.ToString("000") + "-" +
                   color.B.ToString("000");
        }

        public static Dictionary<string, Color> DrawingColors
        {
            get
            {
                var d = new Dictionary<string, Color>();
                foreach (var c in _drawingColorList)
                    d[c.Name] = c;
                return d;
            }
        }

        private static readonly List<Color> _drawingColorList = new List<Color>()
        {

            Color.AliceBlue,
            Color.Azure,
            Color.Beige,
            Color.Bisque,
            Color.Blue,
            Color.BlueViolet,
            Color.Brown,
            Color.BurlyWood,
            Color.CadetBlue,
            Color.Chartreuse,
            Color.Chocolate,
            Color.Coral,
            Color.CornflowerBlue,
            Color.Cornsilk,
            Color.Crimson,
            Color.Cyan,
            Color.DarkOrange,
            Color.DarkOrchid,
            Color.DarkRed,

            Color.DarkTurquoise,
            Color.DarkViolet,

            Color.DarkBlue,
            Color.DarkCyan,
            Color.DarkGoldenrod,
            Color.DarkGreen,
            Color.DarkMagenta,

            Color.DeepPink,
            Color.DeepSkyBlue,
            Color.DimGray,
            Color.DodgerBlue,
            Color.Firebrick,
            Color.FloralWhite,
            Color.ForestGreen,
            Color.Fuchsia,
            Color.Gainsboro,
            Color.GhostWhite,
            Color.Gold,
            Color.Goldenrod,
            Color.Gray,
            Color.Green,
            Color.GreenYellow,
            Color.Honeydew,
            Color.HotPink,
            Color.IndianRed,
            Color.Indigo,
            Color.Ivory,
            Color.Khaki,
            Color.Lavender,
            Color.LavenderBlush,
            Color.LawnGreen,
            Color.LemonChiffon,
            Color.LightBlue,
            Color.LightCoral,
            Color.LightCyan,
            Color.LightGoldenrodYellow,
            Color.LightGray,
            Color.LightGreen,
            Color.LightPink,
            Color.LightSalmon,
            Color.LightSeaGreen,
            Color.LightSkyBlue,
            Color.LightSlateGray,
            Color.LightSteelBlue,
            Color.LightYellow,
            Color.Lime,
            Color.LimeGreen,
            Color.Linen,
            Color.Magenta,
            Color.Maroon,
            Color.MediumAquamarine,
            Color.MediumBlue,
            Color.MediumOrchid,
            Color.MediumPurple,
            Color.MediumSeaGreen,
            Color.MediumSlateBlue,
            Color.MediumSpringGreen,
            Color.MediumTurquoise,
            Color.MediumVioletRed,
            Color.MidnightBlue,
            Color.MintCream,
            Color.MistyRose,
            Color.Moccasin,
            Color.NavajoWhite,
            Color.Navy,
            Color.OldLace,
            Color.Olive,
            Color.OliveDrab,
            Color.Orange,
            Color.OrangeRed,
            Color.Orchid,
            Color.PaleGoldenrod,
            Color.PaleGreen,
            Color.PaleTurquoise,
            Color.PaleVioletRed,
            Color.PapayaWhip,
            Color.PeachPuff,
            Color.Peru,
            Color.Pink,
            Color.Plum,
            Color.PowderBlue,
            Color.Purple,
            Color.Red,
            Color.RosyBrown,
            Color.RoyalBlue,
            Color.SaddleBrown,
            Color.Salmon,
            Color.SandyBrown,
            Color.SeaGreen,
            Color.SeaShell,
            Color.Sienna,
            Color.Silver,
            Color.SkyBlue,
            Color.SlateBlue,
            Color.SlateGray,
            Color.Snow,
            Color.SpringGreen,
            Color.SteelBlue,
            Color.Tan,
            Color.Teal,
            Color.Thistle,
            Color.Tomato,
            Color.Transparent,
            Color.Turquoise,
            Color.Violet,
            Color.Wheat,
            Color.White,
            Color.WhiteSmoke,
            Color.Yellow,
            Color.YellowGreen,
        };

        public static class Extensions 
        {
            public enum LedPerMeter
            {
                _30LedPerMeter,
                _60LedPerMeter,
                _1Led,
                _2LedTest,
            }
            public enum StripIndex
            {
                _0,
                _1,                
            }
            public static class TwoStripAdapter
            {
                public static APA102LEDStrip Init(Nusbio nusbio, LedPerMeter ledPerMeter, StripIndex stripIndex, int ledOnStrip)
                {
                    switch (stripIndex)
                    {
                        case StripIndex._0:
                            switch (ledPerMeter)
                            {
                                case LedPerMeter._1Led: // Extension 2 RGB LED Panel 
                                    return new APA102LEDStrip(nusbio, 1, 4, 5).AllOff();
                                case LedPerMeter._2LedTest:
                                    return new APA102LEDStrip(nusbio, ledOnStrip, 3, 2).AllOff();
                                case LedPerMeter._30LedPerMeter:
                                    return new APA102LEDStrip(nusbio, ledOnStrip, 2, 3).AllOff();

                                case LedPerMeter._60LedPerMeter:
                                    return new APA102LEDStrip(nusbio, ledOnStrip, 3, 2).AllOff();
                            }
                        break;
                        case StripIndex._1:
                            switch (ledPerMeter)
                            {
                                case LedPerMeter._1Led: // Extension 2 RGB LED Panel 
                                    return new APA102LEDStrip(nusbio, 1, 6, 7).AllOff();
                                case LedPerMeter._30LedPerMeter:
                                    return new APA102LEDStrip(nusbio, ledOnStrip, 4, 5).AllOff();
                                case LedPerMeter._60LedPerMeter:
                                    return new APA102LEDStrip(nusbio, ledOnStrip, 5, 4).AllOff();
                            }
                        break;
                    }
                    throw new ArgumentException("Cannot create instance of APA102LEDStrip");
                }
            }
        }

    }
}

