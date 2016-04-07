/*
   Copyright (C) 2015 MadeInTheUSB LLC
   Written by FT for MadeInTheUSB

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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MadeInTheUSB.i2c;
using MadeInTheUSB.WinUtil;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
//using abs = System.Math..

namespace MadeInTheUSB.Adafruit
{
    /// <summary>
    /// Allow to control multiple instance of LEDBackpack synchronized
    /// </summary>
    public class MultiLEDBackpackManager  
    {
        private List<LEDBackpack> _backpacks = new List<LEDBackpack>();

        public MultiLEDBackpackManager()
        {
            
        }

        public List<LEDBackpack> Backpacks
        {
            get { return this._backpacks; }
        }

        public LEDBackpack Add(int16_t width, int16_t height, Nusbio nusbio, NusbioGpio sdaOutPin, NusbioGpio sclPin, byte addr)
        {
            var b = new LEDBackpack(width, height, nusbio, sdaOutPin, sclPin);
            if (b.Detect(addr)) { 
                b.Begin(addr);
                this._backpacks.Add(b);
                return b;
            }
            else return null;
        }
        
        public void DrawRoundRect(int x, int y, int w, int h, int r, int color)
        {
            foreach (var b in this._backpacks)
                b.DrawRoundRect(x, y, w, h, r, color);
        }

        public void DrawRect(int x, int y, int w, int h, bool color)
        {
            foreach (var b in this._backpacks)
                b.DrawRect(x, y, w, h, color);
        }

        public void DrawLine(int16_t x0, int16_t y0, int16_t x1, int16_t y1, uint16_t color)
        {
            foreach (var b in this._backpacks)
                b.DrawLine(x0, y0, x1, y1, color);
        }

        public void DrawCircle(int16_t x0, int16_t y0, int16_t r, uint16_t color)
        {
            foreach (var b in this._backpacks)
                b.DrawCircle(x0, y0, r, color);
        }

        public void SetRotation(int rotation)
        {
            foreach (var b in this._backpacks)
                b.Rotation = (byte)rotation;
        }
        
        public void DrawBitmap(int16_t x, int16_t y, List<int> bitmap, int16_t w, int16_t h, uint16_t color)
        {
            foreach (var b in this._backpacks)
                b.DrawBitmap(x, y, bitmap, w, h, color);
        }

        public void DrawPixel(int x, int y, bool color)
        {
            foreach (var b in this._backpacks)
                b.DrawPixel(x, y, color);
        }

        public void AnimateSetBrightness(int maxRepeat, int onWaitTime = 20, int offWaitTime = 40, int maxBrigthness = 15) {

            for (byte rpt = 0; rpt < maxRepeat; rpt++)
            {
                for (var b = 0; b < maxBrigthness; b++)
                {
                    this.SetBrightness(b);
                    TimePeriod.Sleep(onWaitTime);
                }
                for (var b = maxBrigthness; b >= 0; b--)
                {
                    this.SetBrightness(b);
                    TimePeriod.Sleep(offWaitTime);
                }
                TimePeriod.Sleep(offWaitTime*10);
            }
        }

        public void SetBrightness(int b)
        {
            foreach (var bp in this._backpacks)
                bp.SetBrightness(b);
        }

        public void SetBlinkRate(byte b)
        {
            foreach (var bp in this._backpacks)
                bp.SetBlinkRate(b);
        }

        public void Clear(bool refresh = false)
        {
            foreach (var bp in this._backpacks)
                bp.Clear(refresh);
        }

        public bool WriteDisplay()
        {
            foreach (var bp in this._backpacks)
                bp.WriteDisplay();
            return true;
        }
    }
}

