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
    /// <summary>
    /// LCD HD44780 Datasheet: https://www.sparkfun.com/datasheets/LCD/HD44780.pdf
    /// </summary>
    public class LiquidCrystalBase
    {
        // commands
        protected const int LCD_CLEARDISPLAY = 0x01;
        protected const int LCD_RETURNHOME = 0x02;
        protected const int LCD_ENTRYMODESET = 0x04;
        protected const int LCD_DISPLAYCONTROL = 0x08;
        protected const int LCD_CURSORSHIFT = 0x10;
        protected const int LCD_FUNCTIONSET = 0x20;
        protected const int LCD_SETCGRAMADDR = 0x40;
        protected const int LCD_SETDDRAMADDR = 0x80;

        // flags for Display entry mode
        protected const int LCD_ENTRYRIGHT = 0x00;
        protected const int LCD_ENTRYLEFT = 0x02;
        protected const int LCD_ENTRYSHIFTINCREMENT = 0x01;
        protected const int LCD_ENTRYSHIFTDECREMENT = 0x00;

        // flags for Display on/off control
        protected const int LCD_DISPLAYON = 0x04;
        protected const int LCD_DISPLAYOFF = 0x00;
        protected const int LCD_CURSORON = 0x02;
        protected const int LCD_CURSOROFF = 0x00;
        protected const int LCD_BLINKON = 0x01;
        protected const int LCD_BLINKOFF = 0x00;

        // flags for Display/Cursor shift
        protected const int LCD_DISPLAYMOVE = 0x08;
        protected const int LCD_CURSORMOVE = 0x00;
        protected const int LCD_MOVERIGHT = 0x04;
        protected const int LCD_MOVELEFT = 0x00;

        // flags for function set
        protected const int LCD_8BITMODE = 0x10;
        protected const int LCD_4BITMODE = 0x00;
        protected const int LCD_2LINE = 0x08;
        protected const int LCD_1LINE = 0x00;
        protected const int LCD_5x10DOTS = 0x04;
        protected const int LCD_5x8DOTS = 0x00;

        // flags for Backlight control
        public const int LCD_BACKLIGHT = 0x8;
        public const int LCD_NOBACKLIGHT = 0x0;

        public int NumCols { get;set;}
        public int NumLines{ get;set;}

        //protected Nusbio _nusbio;

        public LiquidCrystalBase(IDigitalWriteRead digitalWriteRead)
        {
            _DigitalWriteRead = digitalWriteRead;
        }

        // Abstraction of the hardware
        protected IDigitalWriteRead _DigitalWriteRead;

        public void SetPinMode(int pin, PinMode mode)
        {
            this._DigitalWriteRead.SetPinMode(pin, mode);
        }

        public void DigitalWrite(int pin, PinState state)
        {
            this._DigitalWriteRead.DigitalWrite(pin, state);
        }

        public PinState DigitalRead(int pin)
        {
            return this._DigitalWriteRead.DigitalRead(pin);
        }

        protected void DigitalWrite(int pin, int val)
        {
            DigitalWrite(pin, (val == 0) ? PinState.Low : PinState.High);
        }

        protected void DelayMicroseconds(int delay)
        {
            TimePeriod.__SleepMicro(delay);
        }

        protected void Delay(int delay)
        {
            TimePeriod.Sleep(delay);
        }

        public virtual string Print(string format, params object[] args)
        {
            return string.Empty;
        }

        public void PrintRightPadded(string format, params object[] args)
        {

            var text = String.Format(format, args);
            text = text.PadRight(NumCols);
            this.Print(text);
        }

        public void Init(int cols, int lines, int dotsize = -1)
        {
            this.Begin(cols, lines, dotsize);
        }

        public bool Begin(int cols, int lines, int dotsize = -1)
        {
            return this.Begin((uint8_t)cols, (uint8_t)lines, (uint8_t)dotsize);
        }

        public virtual bool Begin(uint8_t cols, uint8_t lines, int16_t dotsize = -1)
        {
            return true;
        }

        
    }
}