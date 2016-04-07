/*
    This code is based from
 
    Adafruit 0.56" 4-Digit 7-Segment Display w/I2C Backpack - Green
    For the Nusbio
    https://www.adafruit.com/products/880
 
    Ported to C# and Nusbio by FT for MadeInTheUSB
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
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MadeInTheUSB.i2c;


using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
//using abs = System.Math

namespace MadeInTheUSB.Adafruit
{
    public class _4Digits7Segments : LEDBackpack
    {
        public const int DEC = 10;
        public const int HEX = 16;
        public const int OCT = 8;
        public const int BIN = 2;
        public const int BYTE = 0;
        public const int SEVENSEG_DIGITS = 5;

        public const byte DRAW_COLOR_CMD = 0x04;
        public const byte DRAW_COLOR_VALUE = 0x02;

        public bool ColonOn = false;

        public const int MaxDigit = 4;
        public int MaxPosition = MaxDigit + 1;

        private static List<uint8_t> numbertable = new List<uint8_t> {
	        0x3F, /* 0 */
	        0x06, /* 1 */
	        0x5B, /* 2 */
	        0x4F, /* 3 */
	        0x66, /* 4 */
	        0x6D, /* 5 */
	        0x7D, /* 6 */
	        0x07, /* 7 */
	        0x7F, /* 8 */
	        0x6F, /* 9 */
	        0x77, /* a */
	        0x7C, /* b */
	        0x39, /* C */
	        0x5E, /* d */
	        0x79, /* E */
	        0x71, /* F */
        };

        uint8_t _position;

        public _4Digits7Segments(Nusbio nusbio, NusbioGpio sdaOutPin, NusbioGpio sclPin) :
            base(0, 0, nusbio, sdaOutPin, sclPin)
        {
            this._position = 0;
        }

        public void printError()
        {

            for (uint8_t i = 0; i < SEVENSEG_DIGITS; ++i)
            {
                WriteDigitRaw(i, (uint8_t)(i == 2 ? 0x00 : 0x40));
            }
            base.WriteDisplay();
        }

        public override void Clear(bool refresh = false)
        {
            base.Clear(refresh);
            this.GotoX(0);
        }

        private List<byte> _internalPositions = new List<byte>() {
            0, 1, 3, 4
        };

        public void GotoX(int x)
        {
            if (x < 0 || x >= _internalPositions.Count)
                x = 0;
            this._position = _internalPositions[x];
        }

        public void WriteDigit(char c, bool refresh = false)
        {
            Write((byte)c, refresh);
        }

        public void Write(double val, string format = "0.00", bool refresh = false, bool rightJustified = true, bool dot = false)
        {
            var s = val.ToString(format);

            if (rightJustified)
                this.GotoX(MaxPosition - s.Length); // Right Justify

            var i = 0;
            while (i < s.Length && i < MaxDigit)
            {
                this.Write((byte)s[i], false, dot);
                i += 1;
            }
            if (refresh)
                this.WriteDisplay();
        }

        public void Write(int val, bool refresh = false, bool rightJustified = true, bool dot = false)
        {
            this.Write(val.ToString(), refresh, rightJustified, dot);
        }

        public void Write(string s, bool refresh = false, bool rightJustified = true, bool dot = false)
        {
            if (rightJustified)
                this.GotoX(MaxPosition - s.Length - 1); // Right Justify

            var i = 0;
            while (i < s.Length && i < 4)
            {
                this.Write((byte)s[i], false, dot);
                i += 1;
            }
            if (refresh)
                this.WriteDisplay();
        }

        public void Write(uint8_t c, bool refresh = false, bool dot = false)
        {
            if (c == '\n') _position = 0;
            if (c == '\r') _position = 0;

            if ((c >= '0') && (c <= '9'))
            {
                uint8_t cb = (uint8_t)(c - '0');
                WriteDigitNum(_position, cb, dot);
                _position++;
                if (_position == 2) // Skip the colon
                    _position++;
                if (refresh)
                    this.WriteDisplay();
            }
            else throw new ArgumentException(string.Format("Char:{0} is not supported for {1}", c, this.GetType().FullName));
        }

        void WriteDigitRaw(uint8_t d, uint8_t bitmask)
        {
            if (d > 4) return;
            _displayBuffer[d] = bitmask;
        }

        public void DrawColon(bool state)
        {
            ColonOn = state;
            if (state)
                _displayBuffer[2] = DRAW_COLOR_VALUE;
            else
                _displayBuffer[2] = 0;

            SendDrawColonCommand(); // WriteDisplay the colon right wayt
        }

        void SendDrawColonCommand()
        {
            base._i2c.Send3BytesCommand(
                (uint8_t)DRAW_COLOR_CMD,
                (uint8_t)(_displayBuffer[2] & 0xFF),
                (uint8_t)(_displayBuffer[2] >> 8)
            );
        }

        void WriteDigitNum(uint8_t position, uint8_t num, bool dot = false)
        {
            if (position > 4) return;
            int dotNum = dot ? 1 : 0;
            WriteDigitRaw(position, (uint8_t)(numbertable[num] | (dotNum << 7)));
        }
    }
}

