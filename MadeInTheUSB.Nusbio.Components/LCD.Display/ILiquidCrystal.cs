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
    public interface ILiquidCrystal
    {
        void Begin(uint8_t cols, uint8_t lines, int16_t dotsize = -1);
        void Begin(int cols, int lines, int dotsize = -1);
        void Clear();
        void Home();
        void SetCursor(int col, int row);
        void NoDisplay();
        void Display();
        void NoCursor();
        void Cursor();
        void NoBlink();
        void Blink();
        void ScrollDisplayLeft();
        void ScrollDisplayRight();
        void LeftToRight();
        void RightToLeft();
        void Autoscroll();
        void NoAutoscroll();
        void CreateChar(uint8_t location, int[] charmap);
        void CreateChar(uint8_t location, uint8_t[] charmap);
        string Print(string format, params object[] args);
        string Print(int x, int y, string format, params object[] args);
        void Flash(int count, int waitTime = 230);
        void ProgressBar(int x, int y, int maxChar, int percent, string text = null);
        size_t Write(uint8_t value);
        int NumCols { get;set;}
        int NumLines{ get;set;}
    }
}
