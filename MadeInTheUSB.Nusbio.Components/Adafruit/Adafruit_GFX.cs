/*
    Ported to C# and Nusbio by FT for MadeInTheUSB
    Copyright (C) 2015 MadeInTheUSB LLC
 
    Adafruit 8x8 LED matrix with backpack
    This program control Adafruit 8x8 LED matrix with backpack
        https://learn.adafruit.com/adafruit-led-backpack/overview
            https://www.adafruit.com/product/872
            https://www.adafruit.com/product/1049 
    The C# files
        Components\Adafruit\Adafruit_GFX.cs
        Components\adafruit\ledbackpack.cs
    are a port from Adafruit library Adafruit-LED-Backpack-Library
    https://github.com/adafruit/Adafruit-LED-Backpack-Library
 
    MIT license, all text above must be included in any redistribution

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
 
*/

// Adafruit_LEDBackpack.cpp
// https://github.com/adafruit/Adafruit-LED-Backpack-Library
/*************************************************** 
  This is a library for our I2C LED Backpacks

  Designed specifically to work with the Adafruit LED Matrix backpacks 
  ----> http://www.adafruit.com/products/
  ----> http://www.adafruit.com/products/

  These displays use I2C to communicate, 2 pins are required to 
  interface. There are multiple selectable I2C addresses. For backpacks
  with 2 Address Select pins: 0x70, 0x71, 0x72 or 0x73. For backpacks
  with 3 Address Select pins: 0x70 thru 0x77

  Adafruit invests time and resources providing this open source code, 
  please support Adafruit and open-source hardware by purchasing 
  products from Adafruit!

  Written by Limor Fried/Ladyada for Adafruit Industries.  
  MIT license, all text above must be included in any redistribution
 ****************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NUSBIO2
using MadeInTheUSB.i2c;
#endif
using MadeInTheUSB.WinUtil;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
//using abs = System.Math..

/*
 About converting Arduino C to C#
 ================================
 Why do operations on "byte" result in "int"? - http://blogs.msdn.com/b/oldnewthing/archive/2004/03/10/87247.aspx
 */

namespace MadeInTheUSB.Adafruit
{
    public class Data
    {
    }

    public class Adafruit_GFX
    {
        private int16_t WIDTH = 8;   // This is the 'raw' Display w/h - never changes
        private int16_t HEIGHT = 8;   // This is the 'raw' Display w/h - never changes
        public int16_t Width, Height;
        internal uint8_t _rotation;
        UInt16 _textcolor, _textbgcolor;
        int16_t cursor_x, cursor_y;
        uint8_t textsize;
        Boolean wrap; // If set, 'wrap' text at right edge of Display

        public virtual void DrawPixel(int16_t x, int16_t y, uint16_t color)
        {

        }

        public Adafruit_GFX(Int16 w, Int16 h)
        {
            Width      = w;
            Height     = h;
            _rotation  = 0;
            cursor_y   = cursor_x = 0;
            textsize   = 1;
            _textcolor = _textbgcolor = 0xFFFF;
            wrap       = true;
        }

        public byte Rotation
        {
            get
            {

                return _rotation;
            }
            set
            {
                _rotation = (byte)(value & 3);
                switch (_rotation)
                {
                    case 0:
                    case 2:
                        this.Width = WIDTH;
                        this.Height = HEIGHT;
                        break;
                    case 1:
                    case 3:
                        this.Width = HEIGHT;
                        this.Height = WIDTH;
                        break;
                    default:
                        throw new ArgumentException(string.Format("Invalid rotation:{0}", _rotation));
                }
            }
        }

        protected int16_t abs(int v)
        {
            return (int16_t)System.Math.Abs(v);
        }

        protected int16_t abs(int16_t v)
        {
            return System.Math.Abs(v);
        }

        protected void Swap(ref int16_t v1, ref int16_t v2)
        {
            int16_t v = v1;
            v1 = v2;
            v2 = v;
        }

        void DrawCircleHelper(int x0, int y0, int r, int cornername, int color)
        {
            DrawCircleHelper((int16_t) x0, (int16_t) y0, (int16_t) r, (uint8_t) cornername, (uint16_t) color);
        }

        void DrawCircleHelper(int16_t x0, int16_t y0, int16_t r, uint8_t cornername, uint16_t color)
        {
            int16_t f = (int16_t)(1 - r);
            int16_t ddF_x = 1;
            int16_t ddF_y = (int16_t)(-2 * r);
            int16_t x = 0;
            int16_t y = r;

            while (x < y)
            {
                if (f >= 0)
                {
                    y--;
                    ddF_y += 2;
                    f += ddF_y;
                }
                x++;
                ddF_x += 2;
                f += ddF_x;
                if (BitUtil.IsSet(cornername, 0x4))
                {

                    DrawPixel((int16_t)(x0 + x), (int16_t)(y0 + y), color);
                    DrawPixel((int16_t)(x0 + y), (int16_t)(y0 + x), color);
                }
                if (BitUtil.IsSet(cornername, 0x2))
                {
                    DrawPixel((int16_t)(x0 + x), (int16_t)(y0 - y), color);
                    DrawPixel((int16_t)(x0 + y), (int16_t)(y0 - x), color);
                }
                if (BitUtil.IsSet(cornername, 0x8))
                {
                    DrawPixel((int16_t)(x0 - y), (int16_t)(y0 + x), color);
                    DrawPixel((int16_t)(x0 - x), (int16_t)(y0 + y), color);
                }
                if (BitUtil.IsSet(cornername, 0x1))
                {
                    DrawPixel((int16_t)(x0 - y), (int16_t)(y0 - x), color);
                    DrawPixel((int16_t)(x0 - x), (int16_t)(y0 - y), color);
                }
            }
        }

        public void DrawBitmap(int x, int y, List<string> bitmapDefinedAsString, int w, int h, bool color)
        {
            this.DrawBitmap((int16_t)x, (int16_t)y, bitmapDefinedAsString, (int16_t)w, (int16_t)h, (uint16_t)(color ? 1 : 0));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="bitmapDefinedAsString">
        /// Accept bitmap defined as a list of byte defined like this "B11111111", "B10000001"
        /// </param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="color"></param>
        public void DrawBitmap(int16_t x, int16_t y, List<string> bitmapDefinedAsString, int16_t w, int16_t h, uint16_t color)
        {
            DrawBitmap(0, 0, BitUtil.ParseBinary(bitmapDefinedAsString), 8, 8, 1);
        }

        public void DrawBitmap(int16_t x, int16_t y, List<int> bitmap, int16_t w, int16_t h, uint16_t color) {

            int16_t i, j, byteWidth = (int16_t)((w + 7) / 8);

            for(j=0; j<h; j++) {

                for(i=0; i<w; i++ )
                {
                    var bitmapValue = bitmap[j * byteWidth + i / 8];
                    var expected = (128 >> (i & 7));
                    if((bitmapValue & expected) == expected) {

                        this.DrawPixel((int16_t)(x+i), (int16_t)(y+j), (uint16_t)color);
                    }
                }
            }
        }
        public void DrawCircle(int x0, int y0, int r, bool color)
        {
            DrawCircle((int16_t) x0, (int16_t) y0, (int16_t) r, (uint16_t)(color ? 1 : 0));
        }
        // Draw a circle outline
        public void DrawCircle(int16_t x0, int16_t y0, int16_t r, uint16_t color)
        {
            int16_t f = (int16_t)(1 - r);
            int16_t ddF_x = 1;
            int16_t ddF_y = (int16_t)(-2 * r);
            int16_t x = 0;
            int16_t y = r;

            DrawPixel(x0, (int16_t)(y0 + r), color);
            DrawPixel(x0, (int16_t)(y0 - r), color);
            DrawPixel((int16_t)(x0 + r), y0, color);
            DrawPixel((int16_t)(x0 - r), y0, color);

            while (x < y)
            {
                if (f >= 0)
                {
                    y--;
                    ddF_y += 2;
                    f += ddF_y;
                }
                x++;
                ddF_x += 2;
                f += ddF_x;

                DrawPixel((int16_t)(x0 + x), (int16_t)(y0 + y), color);
                DrawPixel((int16_t)(x0 - x), (int16_t)(y0 + y), color);
                DrawPixel((int16_t)(x0 + x), (int16_t)(y0 - y), color);
                DrawPixel((int16_t)(x0 - x), (int16_t)(y0 - y), color);
                DrawPixel((int16_t)(x0 + y), (int16_t)(y0 + x), color);
                DrawPixel((int16_t)(x0 - y), (int16_t)(y0 + x), color);
                DrawPixel((int16_t)(x0 + y), (int16_t)(y0 - x), color);
                DrawPixel((int16_t)(x0 - y), (int16_t)(y0 - x), color);
            }
        }

        public void DrawRect(int x, int y, int w, int h, bool color)
        {
            this.DrawRect((int16_t)x, (int16_t)y, (int16_t)w, (int16_t)h, (uint16_t)(color ? 1 : 0));
        }

        public void DrawRect(int16_t x, int16_t y, int16_t w, int16_t h, uint16_t color)
        {
            DrawFastHLine(x, y, w, color);
            DrawFastHLine(x, (int16_t)(y + h - 1), w, color);
            DrawFastVLine(x, y, h, color);
            DrawFastVLine((int16_t)(x + w - 1), y, h, color);
        }
        /// <summary>
        /// C# helper
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="h"></param>
        /// <param name="color"></param>
        void DrawFastVLine(int x, int y, int h, int color)
        {
            DrawFastVLine((int16_t)x, (int16_t)y, (int16_t)h, (uint16_t)color);
        }

        void DrawFastVLine(int16_t x, int16_t y, int16_t h, uint16_t color)
        {
            DrawLine(x, y, x, (int16_t)(y + h - 1), color);
        }

        /// <summary>
        /// C# Helper
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="color"></param>
        void DrawFastHLine(int x, int y, int w, int color)
        {
            DrawFastHLine((int16_t)x, (int16_t)y, (int16_t)w, (uint16_t)color);
        }

        void DrawFastHLine(int16_t x, int16_t y, int16_t w, uint16_t color)
        {
            // Update in subclasses if desired!
            DrawLine(x, y, (int16_t)(x + w - 1), y, color);
        }

        public void DrawRoundRect(int x, int y, int w, int h, int r, int color)
        {
            DrawRoundRect((int16_t) x, (int16_t) y, (int16_t) w, (int16_t) h, (int16_t) r, (uint16_t) color);
        }

        public void DrawRoundRect(int16_t x, int16_t y, int16_t w, int16_t h, int16_t r, uint16_t color) {
          // smarter version
          DrawFastHLine(x+r  , y    , w-2*r, color); // Top
          DrawFastHLine(x+r  , y+h-1, w-2*r, color); // Bottom
          DrawFastVLine(x    , y+r  , h-2*r, color); // Left
          DrawFastVLine(x+w-1, y+r  , h-2*r, color); // Right
          // draw four corners
          DrawCircleHelper(x+r    , y+r    , r, 1, color);
          DrawCircleHelper(x+w-r-1, y+r    , r, 2, color);
          DrawCircleHelper(x+w-r-1, y+h-r-1, r, 4, color);
          DrawCircleHelper(x+r    , y+h-r-1, r, 8, color);
        }

        public void DrawLine(int x0, int y0, int  x1, int  y1, bool color)
        {
            this.DrawLine((int16_t)(x0), (int16_t)(y0), (int16_t)(x1), (int16_t)(y1), (uint16_t)(color ? 1 : 0));
        }

        // Bresenham's algorithm - thx wikpedia
        public void DrawLine(int16_t x0, int16_t y0, int16_t x1, int16_t y1, uint16_t color)
        {
            bool steep = abs(y1 - y0) > abs(x1 - x0);
            if (steep)
            {
                Swap(ref x0, ref y0);
                Swap(ref x1, ref y1);
            }

            if (x0 > x1)
            {
                Swap(ref x0, ref x1);
                Swap(ref y0, ref y1);
            }

            int16_t dx, dy;
            dx = (int16_t)(x1 - x0);
            dy = (int16_t)(abs(y1 - y0));

            int16_t err = (int16_t)(dx / 2);
            int16_t ystep;

            if (y0 < y1)
            {
                ystep = 1;
            }
            else
            {
                ystep = -1;
            }

            for (; x0 <= x1; x0++)
            {
                if (steep)
                {
                    DrawPixel(y0, x0, color);
                }
                else
                {
                    DrawPixel(x0, y0, color);
                }
                err -= dy;
                if (err < 0)
                {
                    y0 += ystep;
                    err += dx;
                }
            }
        }
    }
}

