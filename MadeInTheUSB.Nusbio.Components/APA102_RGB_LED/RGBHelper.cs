//#define COLOR_MINE
/*
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
 
 ACKNOWLEDGMENT 
 ==============
  
 Based on work from 
 - https://en.wikipedia.org/wiki/HSL_and_HSV 
 - http://stackoverflow.com/questions/1335426/is-there-a-built-in-c-net-system-api-for-hsv-to-rgb
  

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if COLOR_MINE
using ColorMine.ColorSpaces;
#endif
using MadeInTheUSB.i2c;
using MadeInTheUSB.WinUtil;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
using System.Drawing;
using MadeInTheUSB.GPIO;

namespace MadeInTheUSB.Components
{
    public class RGBHelper
    {

    public static Color Adjust(Color c, double rFactor, double gFactor = -1, double bFactor = -1)
    {
        if (gFactor == -1) gFactor = rFactor;
        if (bFactor == -1) bFactor = rFactor;
        return Color.FromArgb((byte)(c.R*rFactor), (byte)(c.G*gFactor), (byte)(c.B*bFactor));
    }

    public static List<System.Drawing.Color> GenerateListOfColor(
            int size, 
            List<byte> r, 
            List<byte> g, 
            List<byte> b,
            int minValExpected = 1)
        {
            if(r == null) r = RGBHelper.MakeList(0, size);
            if(g == null) g = RGBHelper.MakeList(0, size);
            if(b == null) b = RGBHelper.MakeList(0, size);

            r = RGBHelper.Minimize(r, minValExpected);
            g = RGBHelper.Minimize(g, minValExpected);
            b = RGBHelper.Minimize(b, minValExpected);

            var l = new List<System.Drawing.Color>();
            for (var i = 0; i < r.Count; i++)
            {
                l.Add(Color.FromArgb(r[i], g[i], b[i]));
            }
            return l;
        }

        public static List<byte> MakeList(byte val, int count)
        {
            var l = new List<byte>();
            for (var i = 0; i < count; i++)
                l.Add(val);
            return l;
        }

        public static List<byte> GenerateTrigonometricColor2(
            int trigoCircleStepCount, 
            double trigoCircleCircleCountInRadian, // PI or 2 PI
            Func<double, double> f,
            int colorMaxValue = 255,
            int colorMinValue = 0,
            double multipleFactor = 10000,
            bool diplayData = false
            )
        {
            var doubleFormat = "+0.000000;-0.000000";
            var intFormat    = "+000.000;-#000.000";
            var l            = new List<byte>();
            var dl           = new List<double>();
            var stepInc      = trigoCircleCircleCountInRadian / trigoCircleStepCount;
            double radianV   = 0;

            Console.WriteLine("colorMaxValue:{0}, colorMinValue:{1}", colorMaxValue, colorMinValue);

            for (; radianV < trigoCircleCircleCountInRadian; radianV += stepInc) {

                var fValue = f(radianV) * multipleFactor;
                dl.Add(fValue);

                if (diplayData)
                {
                    Console.WriteLine("Rad:{0} fVal:{1} ",
                        radianV.ToString(doubleFormat), fValue.ToString(doubleFormat));
                }
                
            }
            while (dl.Count > trigoCircleStepCount) dl.RemoveAt(dl.Count - 1);
            return ConvertProportianalyToByte(dl);
        }

        public static List<byte> ConvertProportianalyToByte(List<double> values, int maxByteVal = 255)
        {
            var l = new List<byte>();
            var max = values.Max();
            var z = max / maxByteVal;

            foreach (var v in values)
            {
                var d = v / z;
                var i = (int)d;
                l.Add((byte)i);
            }
            return l;
        }

        public static List<byte> Minimize(List<byte> values, int minValExpected = 1)
        {
            var l = new List<byte>();
            while (true)
            {
                var min = values.Min();
                if(min <= minValExpected)
                    break;
                foreach (var v in values)
                {
                    l.Add((byte)(v - min + 1));
                }
                values = l;
            }
            return values;
        }

        public static List<byte> GenerateTrigonometricValues(
            int trigoCircleStepCount, 
            double trigoCircleCircleCountInRadian, // PI or 2 PI
            Func<double, double> f,
            int colorFloorValue = 128,
            int colorMinValue = 0,
            bool diplayData = false
            )
        {
            var doubleFormat = "+0.000000;-0.000000";
            var intFormat    = "+000.000;-#000.000";
            var l            = new List<byte>();
            var stepInc      = trigoCircleCircleCountInRadian / trigoCircleStepCount;
            double radianV   = 0;

            Console.WriteLine("colorMaxValue:{0}, colorMinValue:{1}", colorFloorValue, colorMinValue);

            for (; radianV < trigoCircleCircleCountInRadian; radianV += stepInc) {

                var fValue       = f(radianV);
                var percentColor = colorFloorValue * fValue;
                var color        = colorMinValue + (int)(percentColor);
                var bColor       = (byte) color;

                if (color < 0)
                    throw new ArgumentException("Value smaller than 0 generated");

                if (color > 255)
                    throw new ArgumentException("Value greater than 255 generated");

                if (diplayData)
                {
                    Console.WriteLine("Rad:{0} fVal:{1} %Color:{2}, color:{3}, 8bColor:{4}",
                        radianV.ToString(doubleFormat), fValue.ToString(doubleFormat),
                        percentColor.ToString(intFormat),
                        color.ToString(intFormat),
                        bColor.ToString(intFormat));
                }
                l.Add(bColor);
            }
            while (l.Count > trigoCircleStepCount) l.RemoveAt(l.Count - 1);
            return l;
        }
        
        public static Color Wheel(int wheelPos)
        {
            byte b = (byte) wheelPos;
            return WheelByte((byte)(b & 255));
        }

        // Based on ADAFRUIT strandtes.ino for NeoPixel
        // Input a value 0 to 255 to get a color value.
        // The colours are a transition r - g - b - back to r.
        private static Color WheelByte(byte wheelPos)
        {
            if (wheelPos < 85)
            {
                return Color.FromArgb(0, wheelPos*3, 255 - wheelPos*3, 0);
            }
            else if (wheelPos < 170)
            {
                wheelPos -= 85;
                return Color.FromArgb(0, 255 - wheelPos*3, 0, wheelPos*3);
            }
            else
            {
                wheelPos -= 170;
                return Color.FromArgb(0, 0, wheelPos*3, 255 - wheelPos*3);
            }
        }


        public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        #if COLOR_MINE
        public static Color HslToRgb(double h, double s, double l)
        {
            var hsl = new Hsl { H = h, S = s, L = l };
            var r = hsl.To<Rgb>();
            var d = Color.FromArgb((int) r.R, (int) r.G, (int) r.B);
            return d;
        }
        #endif

        public static Color HsvToRgb(double h, double S, double V)
        {
            if (S > 1.0) S = S / 100.0;
            if (V > 1.0) V = V / 100.0;

            int r, g, b;
            double H = h;
            while (H < 0)
            {
                H += 360;
            }
            while (H >= 360)
            {
                H -= 360;
            }
            double R, G, B;
            if (V <= 0)
            {
                R = G = B = 0;
            }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H/60.0;
                int i = (int) Math.Floor(hf);
                double f = hf - i;
                double pv = V*(1 - S);
                double qv = V*(1 - S*f);
                double tv = V*(1 - S*(1 - f));
                switch (i)
                {
                        // Red is the dominant color
                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                        // Green is the dominant color
                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                        // Blue is the dominant color
                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                        // Red is the dominant color
                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                        // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.
                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                        // The color is not defined, we should throw an error.
                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int) (R*255.0));
            g = Clamp((int) (G*255.0));
            b = Clamp((int) (B*255.0));
            var c = Color.FromArgb(r, g, b);
            return c;
        }


        private static int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }

        public static Color HsvToRgb2(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }
    }
}
        