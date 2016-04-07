/*
    NusbioLedMatrix (MAX7219)
    Ported to C# and Nusbio by FT for MadeInTheUSB
    Copyright (C) 2015 MadeInTheUSB LLC
    
    Based on the library MAX7219
    A library for controling Leds with a MAX7219/MAX7222
    Copyright (c) 2007-2015 Eberhard Fahle
    https://github.com/wayoda/LedControl
  
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using MadeInTheUSB.Component;
using MadeInTheUSB.i2c;
using MadeInTheUSB.spi;
using MadeInTheUSB.WinUtil;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;

namespace MadeInTheUSB
{
    /// <summary>
    /// Dedicated class for the specific 8 seven segment display managed by the chip MAX7219
    /// 
    ///  _
    /// | |
    ///  -
    /// | |
    ///  -
    /// 
    /// Bit
    ///   64
    ///   _
    /// 2| |32
    /// 1 -
    /// 4| |16
    ///   -
    ///   8 
    /// The dot is 128
    /// </summary>
    public class NusbioSevenSegmentDisplay : MAX7219
    {
        public const int DEFAULT_BRIGTHNESS_DEMO = 5;
        public int SevenSegmentCount;


        /// Bit
        ///   64
        ///   _
        /// 2| |32
        /// 1 -
        /// 4| |16
        ///   -
        ///   8 
        public static Dictionary<char, byte> Letters = new Dictionary<char, uint8_t>()
        {
            {'A', 64 + 2 + 1 + 4 + 32 + 16},
            {'B', 64 + 2 + 4 + 1 + 32 + 16 + 8},
            {'C', 64 + 2 + 4 + 8},
            {'D', 64 + 2 + 4 + 32 + 16 + 8},
            {'E', 64 + 2 + 4 + 8 + 1},
            {'F', 64 + 2 + 4 + 1},
            {'G', 64 + 2 + 4 + 8 + 16 + 1 },
            {'H', 2  + 1 + 4 + 32 + 16},
            {'I', 2 + 4 },
            {'J', 32 + 16 + 8 },
            {'K', 0 }, // Not defined
            {'L', 2 + 4 + 8 },
            {'M', 0 },
            {'N', 0 },
            {'O', 64 + 2 + 4 + 32 + 16 + 8},
            {'P', 64 + 2 + 1 +32 + 4 },
            {'Q', 0 },
            {'R', 0 },
            {'S',  64 + 2 + 1 + 16 + 8},
            {'T', 0 },
            {'X', 0 },
            {'Y', 0 },
            {'Z', 0 },
        };

        public void WriteLetter(int deviceIndex, int digit, string text)
        {
            text = text.ToUpperInvariant();
            for (var i = 0; i < text.Length; i++)
            {
                if(Letters.ContainsKey(text[i]))
                {
                    SetDigitDataByte(deviceIndex, digit - i, Letters[text[i]], false);    
                }
            }
        }

        public NusbioSevenSegmentDisplay(
            Nusbio nusbio, 
            int sevenSegmentCount,
            NusbioGpio selectGpio, 
            NusbioGpio mosiGpio, 
            NusbioGpio clockGpio, 
            int deviceCount = 1) :
            base(nusbio, selectGpio, mosiGpio, clockGpio, deviceCount)
        {
            this.SevenSegmentCount = sevenSegmentCount;
        }

        public static NusbioSevenSegmentDisplay Initialize(
            Nusbio nusbio,
            int sevenSegmentCount,
            NusbioGpio selectGpio,
            NusbioGpio mosiGpio,
            NusbioGpio clockGpio,
            int deviceCount = 0)
        {
            var sevenSegmentDisplay = new NusbioSevenSegmentDisplay(nusbio, sevenSegmentCount, selectGpio, mosiGpio, clockGpio, deviceCount: deviceCount);
            sevenSegmentDisplay.Begin(DEFAULT_BRIGTHNESS_DEMO);
            return sevenSegmentDisplay;
        }

    }
}
