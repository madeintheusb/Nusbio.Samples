/*
    MAX7219, NusbioMatrix
    Written in C# for Nusbio by FT for MadeInTheUSB
    Copyright (C) 2015 MadeInTheUSB LLC
    
    Based on the library MAX7219
    A library for controling Leds with a MAX7219/MAX7221
    Copyright (c) 2007-2015 Eberhard Fahle
    https://github.com/wayoda/LedControl
  
    Based on the library MaxMatrix
    https://code.google.com/p/arudino-maxmatrix-library

    MAX7219
    https://datasheets.maximintegrated.com/en/ds/MAX7219-MAX7221.pdf
    http://www.mouser.com/ds/2/256/MAX7219-MAX7221-92855.pdf
  
    PART        TEMP RANGE          PIN-PACKAGE
    -------------------------------------------------------------------------- 
    MAX7219CNG  0°C to +70°C        24 Narrow Plastic DIP -- THRU HOLE
    MAX7219CWG  0°C to +70°C        24 Wide SO -- SMD SOIC-Wide-24 (12x12)
    MAX7219C/D  0°C to +70°C        Dice* *Dice are specified at TA = +25°C.
    MAX7219ENG  -40°C to +85°C      24 Narrow Plastic DIP
    MAX7219EWG  -40°C to +85°C      24 Wide SO
    MAX7219ERG  -40°C to +85°C      24 Narrow CERDIP
    -------------------------------------------------------------------------- 
  
    Also based on Adafruit 8x8 LED matrix with backpack for the graphic methods
        Components\Adafruit\Adafruit_GFX.cs
 
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
using MadeInTheUSB.Component;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.i2c;
using MadeInTheUSB.spi;
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Adafruit;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;

namespace MadeInTheUSB
{
    /// <summary>
    /// 
    /// Remark with multiple 8x8 LED matrix chained.
    /// 
    /// The methods inherited from class Adafruit_GFX have no parameter devIndex.
    /// It is therefore necessary to set the global member CurrentDeviceIndex before calling
    /// any of the inherited methods DrawBitmap, DrawRoundRect, DrawPixel, DrawRect, DrawCircle.
    /// 
    ///  Remark:
    ///     7-Segment Displays support is in progress.
    /// 
    /// </summary>
    public class MAX7219 : Adafruit_GFX {

        static List<object> __CHAR_TABLE = new List<object>() { 

             " " ,  3, 8, "B00000000", "B00000000", "B00000000", "B00000000", "B00000000", // space
             "!" ,1, 8, "B01011111", "B00000000", "B00000000", "B00000000", "B00000000", // !
             "\"",3, 8, "B00000011", "B00000000", "B00000011", "B00000000", "B00000000", // "
             "#" ,5, 8, "B00010100", "B00111110", "B00010100", "B00111110", "B00010100", // #
             "$" ,4, 8, "B00100100", "B01101010", "B00101011", "B00010010", "B00000000", // $
             "%" ,5, 8, "B01100011", "B00010011", "B00001000", "B01100100", "B01100011", // %
             "&" ,5, 8, "B00110110", "B01001001", "B01010110", "B00100000", "B01010000", // &
             "'" ,1, 8, "B00000011", "B00000000", "B00000000", "B00000000", "B00000000", // '
             "(" ,3, 8, "B00011100", "B00100010", "B01000001", "B00000000", "B00000000", // (
             ")" ,3, 8, "B01000001", "B00100010", "B00011100", "B00000000", "B00000000", // )
             "*" ,5, 8, "B00101000", "B00011000", "B00001110", "B00011000", "B00101000", // *
             "+" ,5, 8, "B00001000", "B00001000", "B00111110", "B00001000", "B00001000", // +
             ",",2, 8, "B10110000", "B01110000", "B00000000", "B00000000", "B00000000", // ,
             "-" ,4, 8, "B00001000", "B00001000", "B00001000", "B00001000", "B00000000", // -
             "." ,2, 8, "B01100000", "B01100000", "B00000000", "B00000000", "B00000000", // .
             "/" ,4, 8, "B01100000", "B00011000", "B00000110", "B00000001", "B00000000", // /
             "0" ,4, 8, "B00111110", "B01000001", "B01000001", "B00111110", "B00000000", // 0
             "1" ,3, 8, "B01000010", "B01111111", "B01000000", "B00000000", "B00000000", // 1
             "2" ,4, 8, "B01100010", "B01010001", "B01001001", "B01000110", "B00000000", // 2
             "3" ,4, 8, "B00100010", "B01000001", "B01001001", "B00110110", "B00000000", // 3
             "4" ,4, 8, "B00011000", "B00010100", "B00010010", "B01111111", "B00000000", // 4
             "5" ,4, 8, "B00100111", "B01000101", "B01000101", "B00111001", "B00000000", // 5
             "6" ,4, 8, "B00111110", "B01001001", "B01001001", "B00110000", "B00000000", // 6
             "7" ,4, 8, "B01100001", "B00010001", "B00001001", "B00000111", "B00000000", // 7
             "8" ,4, 8, "B00110110", "B01001001", "B01001001", "B00110110", "B00000000", // 8
             "9" ,4, 8, "B00000110", "B01001001", "B01001001", "B00111110", "B00000000", // 9
             ":" ,2, 8, "B01010000", "B00000000", "B00000000", "B00000000", "B00000000", // :
             ";" ,2, 8, "B10000000", "B01010000", "B00000000", "B00000000", "B00000000", // ;
             "<" ,3, 8, "B00010000", "B00101000", "B01000100", "B00000000", "B00000000", // <
             "=" ,3, 8, "B00010100", "B00010100", "B00010100", "B00000000", "B00000000", // =
             ">" ,3, 8, "B01000100", "B00101000", "B00010000", "B00000000", "B00000000", // >
             "?" ,4, 8, "B00000010", "B01011001", "B00001001", "B00000110", "B00000000", // ?
             "@" ,5, 8, "B00111110", "B01001001", "B01010101", "B01011101", "B00001110", // @
             "A" ,4, 8, "B01111110", "B00010001", "B00010001", "B01111110", "B00000000", // A
             "B" ,4, 8, "B01111111", "B01001001", "B01001001", "B00110110", "B00000000", // B
             "C" ,4, 8, "B00111110", "B01000001", "B01000001", "B00100010", "B00000000", // C
             "D" ,4, 8, "B01111111", "B01000001", "B01000001", "B00111110", "B00000000", // D
             "E" ,4, 8, "B01111111", "B01001001", "B01001001", "B01000001", "B00000000", // E
             "F" ,4, 8, "B01111111", "B00001001", "B00001001", "B00000001", "B00000000", // F
             "G" ,4, 8, "B00111110", "B01000001", "B01001001", "B01111010", "B00000000", // G
             "H" ,4, 8, "B01111111", "B00001000", "B00001000", "B01111111", "B00000000", // H
             "I" ,3, 8, "B01000001", "B01111111", "B01000001", "B00000000", "B00000000", // I
             "J" ,4, 8, "B00110000", "B01000000", "B01000001", "B00111111", "B00000000", // J
             "K" ,4, 8, "B01111111", "B00001000", "B00010100", "B01100011", "B00000000", // K
             "L" ,4, 8, "B01111111", "B01000000", "B01000000", "B01000000", "B00000000", // L
             "M" ,5, 8, "B01111111", "B00000010", "B00001100", "B00000010", "B01111111", // M
             "N" ,5, 8, "B01111111", "B00000100", "B00001000", "B00010000", "B01111111", // N
             "O" ,4, 8, "B00111110", "B01000001", "B01000001", "B00111110", "B00000000", // O
             "P" ,4, 8, "B01111111", "B00001001", "B00001001", "B00000110", "B00000000", // P
             "Q" ,4, 8, "B00111110", "B01000001", "B01000001", "B10111110", "B00000000", // Q
             "R" ,4, 8, "B01111111", "B00001001", "B00001001", "B01110110", "B00000000", // R
             "S" ,4, 8, "B01000110", "B01001001", "B01001001", "B00110010", "B00000000", // S
             "T" ,5, 8, "B00000001", "B00000001", "B01111111", "B00000001", "B00000001", // T
             "U" ,4, 8, "B00111111", "B01000000", "B01000000", "B00111111", "B00000000", // U
             "V" ,5, 8, "B00001111", "B00110000", "B01000000", "B00110000", "B00001111", // V
             "W" ,5, 8, "B00111111", "B01000000", "B00111000", "B01000000", "B00111111", // W
             "X" ,5, 8, "B01100011", "B00010100", "B00001000", "B00010100", "B01100011", // X
             "Y" ,5, 8, "B00000111", "B00001000", "B01110000", "B00001000", "B00000111", // Y
             "Z" ,4, 8, "B01100001", "B01010001", "B01001001", "B01000111", "B00000000", // Z
             "[" ,2, 8, "B01111111", "B01000001", "B00000000", "B00000000", "B00000000", // [
             "\\",4, 8, "B00000001", "B00000110", "B00011000", "B01100000", "B00000000", // \ "Backslash
             "]" ,2, 8, "B01000001", "B01111111", "B00000000", "B00000000", "B00000000", // ]
             "^" ,3, 8, "B00000010", "B00000001", "B00000010", "B00000000", "B00000000", // hat
             "_" ,4, 8, "B01000000", "B01000000", "B01000000", "B01000000", "B00000000", // _
             "`" ,2, 8, "B00000001", "B00000010", "B00000000", "B00000000", "B00000000", // `
             "a" ,4, 8, "B00100000", "B01010100", "B01010100", "B01111000", "B00000000", // a
             "b" ,4, 8, "B01111111", "B01000100", "B01000100", "B00111000", "B00000000", // b
             "c" ,4, 8, "B00111000", "B01000100", "B01000100", "B00101000", "B00000000", // c
             "d" ,4, 8, "B00111000", "B01000100", "B01000100", "B01111111", "B00000000", // d
             "e" ,4, 8, "B00111000", "B01010100", "B01010100", "B00011000", "B00000000", // e
             "f" ,3, 8, "B00000100", "B01111110", "B00000101", "B00000000", "B00000000", // f
             "g" ,4, 8, "B10011000", "B10100100", "B10100100", "B01111000", "B00000000", // g
             "h" ,4, 8, "B01111111", "B00000100", "B00000100", "B01111000", "B00000000", // h
             "i" ,3, 8, "B01000100", "B01111101", "B01000000", "B00000000", "B00000000", // i
             "j" ,4, 8, "B01000000", "B10000000", "B10000100", "B01111101", "B00000000", // j
             "k" ,4, 8, "B01111111", "B00010000", "B00101000", "B01000100", "B00000000", // k
             "l" ,3, 8, "B01000001", "B01111111", "B01000000", "B00000000", "B00000000", // l
             "m" ,5, 8, "B01111100", "B00000100", "B01111100", "B00000100", "B01111000", // m
             "n" ,4, 8, "B01111100", "B00000100", "B00000100", "B01111000", "B00000000", // n
             "o" ,4, 8, "B00111000", "B01000100", "B01000100", "B00111000", "B00000000", // o
             "p" ,4, 8, "B11111100", "B00100100", "B00100100", "B00011000", "B00000000", // p
             "q" ,4, 8, "B00011000", "B00100100", "B00100100", "B11111100", "B00000000", // q
             "r" ,4, 8, "B01111100", "B00001000", "B00000100", "B00000100", "B00000000", // r
             "s" ,4, 8, "B01001000", "B01010100", "B01010100", "B00100100", "B00000000", // s
             "t" ,3, 8, "B00000100", "B00111111", "B01000100", "B00000000", "B00000000", // t
             "u" ,4, 8, "B00111100", "B01000000", "B01000000", "B01111100", "B00000000", // u
             "v" ,5, 8, "B00011100", "B00100000", "B01000000", "B00100000", "B00011100", // v
             "w" ,5, 8, "B00111100", "B01000000", "B00111100", "B01000000", "B00111100", // w
             "x" ,5, 8, "B01000100", "B00101000", "B00010000", "B00101000", "B01000100", // x
             "y" ,4, 8, "B10011100", "B10100000", "B10100000", "B01111100", "B00000000", // y
             "z" ,3, 8, "B01100100", "B01010100", "B01001100", "B00000000", "B00000000", // z
             "{" ,3, 8, "B00001000", "B00110110", "B01000001", "B00000000", "B00000000", // {
             "|" ,1, 8, "B01111111", "B00000000", "B00000000", "B00000000", "B00000000", // |
             "}" ,3, 8, "B01000001", "B00110110", "B00001000", "B00000000", "B00000000", // }
             "~" ,4, 8, "B00001000", "B00000100", "B00001000", "B00000100", "B00000000", // ~
            };

        public class CharDef
        {
            public char Character;
            public int ColumnCount;
            public int Height;
            public List<byte> Columns = new List<uint8_t>();

            public CharDef(char character, int columnCount, int height, params string[] columnAsBit)
            {
                this.Character   = character;
                this.ColumnCount = columnCount;
                this.Height      = height;
                foreach (var b in columnAsBit)
                    this.Columns.Add((byte)BitUtil.ParseBinary(b));
            }
        }

        private static Dictionary<char, CharDef> _CHAR_DICTIONARY = null;

        public static Dictionary<char, CharDef> CharDictionary
        {
            get
            {
                if (_CHAR_DICTIONARY != null)
                    return _CHAR_DICTIONARY;

                _CHAR_DICTIONARY = new Dictionary<char, CharDef>();
                var i = 0;
                while (i < __CHAR_TABLE.Count)
                {
                    char character = __CHAR_TABLE[i + 0].ToString()[0];
                    var charDef = new CharDef(
                        character,
                        (int) __CHAR_TABLE[i + 1],
                        (int) __CHAR_TABLE[i + 2],
                        __CHAR_TABLE[i + 3].ToString(),
                        __CHAR_TABLE[i + 4].ToString(),
                        __CHAR_TABLE[i + 5].ToString(),
                        __CHAR_TABLE[i + 6].ToString(),
                        __CHAR_TABLE[i + 7].ToString()
                        );
                    _CHAR_DICTIONARY.Add(character, charDef);
                    i += 8;
                }
                return _CHAR_DICTIONARY;
            }
        }

        /*
         * Segments to be switched on for characters and digits on
         * 7-Segment Displays
         */
         static List<string> charTable = new List<string>() {

            "B01111110","B00110000","B01101101","B01111001","B00110011","B01011011","B01011111","B01110000",
            "B01111111","B01111011","B01110111","B00011111","B00001101","B00111101","B01001111","B01000111",
            "B00000000","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000",
            "B00000000","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000",
            "B00000000","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000",
            "B00000000","B00000000","B00000000","B00000000","B10000000","B00000001","B10000000","B00000000",
            "B01111110","B00110000","B01101101","B01111001","B00110011","B01011011","B01011111","B01110000",
            "B01111111","B01111011","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000",
            "B00000000","B01110111","B00011111","B00001101","B00111101","B01001111","B01000111","B00000000",
            "B00110111","B00000000","B00000000","B00000000","B00001110","B00000000","B00000000","B00000000",
            "B01100111","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000",
            "B00000000","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000","B00001000",
            "B00000000","B01110111","B00011111","B00001101","B00111101","B01001111","B01000111","B00000000",
            "B00110111","B00000000","B00000000","B00000000","B00001110","B00000000","B00010101","B00011101",
            "B01100111","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000",
            "B00000000","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000","B00000000"
        };

        // the opcodes for the MAX7221 and MAX7219
        private const int OP_NOOP        = 0;
        private const int OP_DIGIT0      = 1;
        private const int OP_DIGIT1      = 2;
        private const int OP_DIGIT2      = 3;
        private const int OP_DIGIT3      = 4;
        private const int OP_DIGIT4      = 5;
        private const int OP_DIGIT5      = 6;
        private const int OP_DIGIT6      = 7;
        private const int OP_DIGIT7      = 8;
        private const int OP_DECODEMODE  = 9;
        private const int OP_INTENSITY   = 10;
        private const int OP_SCANLIMIT   = 11;
        private const int OP_SHUTDOWN    = 12;
        private const int OP_DISPLAYTEST = 15;

        public const int MAX_BRITGHNESS        = 15;
        public const int MAX_MAX7219_CHAINABLE = 8;
        public const int MATRIX_ROW_SIZE       = 8; // 8 x 8 
        public const int MATRIX_COL_SIZE       = 8; // 8 x 8 

        /// <summary>
        /// The scan-limit register sets how many digits are displayed, from 1 to 8. T
        /// Originally the MAX7219 is for display 7-Segment display from 1 to 8.
        /// The scan limit defines how many 7-Segment are contolled.
        /// Each 7-Segments require 8 LEDs time 8, it is 8 x 8 = 64. 
        /// Since we want to handle most of the time 8x8 LEDs we need to set it to
        /// the max. 
        /// </summary>
        public static int MAX_SCAN_LIMIT = 8;

        /// <summary>
        /// The array for shifting the data to the devices 
        /// </summary>
        private byte[] _spiData = new byte[16];

        /// <summary>
        /// We keep track of the led-_pixels for all 8 devices in this array 
        /// </summary>
        private byte [] _pixels = new byte[64];

        /// <summary>
        /// Data is shifted out of this pin 
        /// </summary>
        private int _spiMosi;

        /// <summary>
        /// The clock is signaled on this pin
        /// </summary>
        private int _spiClk;

        /// <summary>
        /// This one is driven LOW for chip selectzion 
        /// </summary>
        private int _spiCs;

        /// <summary>
        /// The maximum number of devices we use 
        /// </summary>
        private int _deviceCount;

        /// <summary>
        /// 
        /// </summary>
        private SPIEngine   _spiEngine;

        /// <summary>
        /// 
        /// </summary>
        private Nusbio      _nusbio;

        /// <summary>
        /// The inherited Adafruit object Adafruit_GFX does not have the concept
        /// of multi device, this is a work around for now before calling any Adafruit_GFX method
        /// </summary>
        public int CurrentDeviceIndex = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="defaultBrightness"></param>
        public bool Begin(int defaultBrightness = 8)
        {
            for (var devIndex = 0; devIndex < this._deviceCount; devIndex++)
            {
                if (!this.Shutdown(false, devIndex)) return false; // The MAX72XX is in power-saving mode on startup, we have to do a wakeup call
                if (!this.SetBrightness(defaultBrightness, devIndex)) return false; // Set the brightness to a medium values
                this.Clear(devIndex); // and clear the display
            }
            return true; 
        }

        /* 
        * Create a new controler 
        * Params :
        * dataPin		pin on the Arduino where data gets shifted out
        * clockPin		pin for the clock
        * csPin		pin for selecting the device 
        * deviceCount	maximum number of devices that can be controled
        */
        public MAX7219(Nusbio nusbio, NusbioGpio selectGpio, NusbioGpio mosiGpio, NusbioGpio clockGpio, int deviceCount = 1, Int16 width = 8, Int16 height = 8) : base(width, height)
        {
           this._spiEngine = new SPIEngine(nusbio, selectGpio, mosiGpio, NusbioGpio.None, clockGpio);
           this._nusbio    = nusbio;

            if (deviceCount <= 0 || deviceCount > MAX_MAX7219_CHAINABLE)
                deviceCount = MAX_MAX7219_CHAINABLE;

            _deviceCount = deviceCount;

            for(var i = 0; i < 64; i++)
                _pixels[i] = 0x00;

            for(var i = 0; i < _deviceCount; i++)
            {
                var r1 = SpiTransfer(i, OP_DISPLAYTEST, 0);

                //scanlimit is set to max on startup
                // The scan-limit register sets how many digits are displayed, from 1 to 8. T
                // Originally the MAX7219 is for display 7-Segment display from 1 to 8.
                // The scan limit defines how many 7-Segment are contolled.
                // Each 7-Segments require 8 LEDs time 8, it is 8 x 8 = 64. 
                // Since we want to handle most of the time 8x8 LEDs we need to set it to
                // the max. 
                var r2 = SetScanLimit(i, MAX_SCAN_LIMIT-1);

                // decode is done in source
                var r3 = SpiTransfer(i, OP_DECODEMODE, 0); // No decode mode, see datasheet page 7

                Clear(i, refresh: true);
                //we go into Shutdown-mode on startup
                Shutdown(true, i);
            }
       }

        /*
        * Gets the number of devices attached to this MAX7219.
        * Returns :
        * int	the number of devices on this MAX7219
        */
        public int DeviceCount
        {
            get { return _deviceCount; }
        }

        /* 
         * Set the Shutdown (power saving) mode for the device
         * Params :
         * devIndex	The address of the display to control
         * _pixels	If true the device goes into power-down mode. Set to false
         *		for normal operation.
         */
        public bool Shutdown(bool status, int devIndex = 0)
        {
            var r = new SPIEngine.SPIResult();

            if (devIndex < 0 || devIndex >= _deviceCount)
                return r.Succeeded;

            if(status)
                r = SpiTransfer(devIndex, OP_SHUTDOWN,0);
            else
                r = SpiTransfer(devIndex, OP_SHUTDOWN,1);

            return r.Succeeded;
        }

        /* 
         * Set the number of digits (or rows) to be displayed.
         * See datasheet for sideeffects of the scanlimit on the brightness
         * of the display.
         * Params :
         * devIndex	address of the display to control
         * limit	number of digits to be displayed (1..8)
         */
        public bool SetScanLimit(int devIndex, int limit)
        {
            var r = new SPIEngine.SPIResult();
            if(devIndex<0 || devIndex>=_deviceCount)
                return r.Succeeded;

            if(limit >= 0 && limit < 8)
                r = SpiTransfer(devIndex, OP_SCANLIMIT,(byte)limit);

            return r.Succeeded;
        }

        public void AnimateSetBrightness(int maxRepeat, int onWaitTime = 20, int offWaitTime = 40, int maxBrigthness = MAX_BRITGHNESS, int deviceIndex = 0) {

            for (byte rpt = 0; rpt < maxRepeat; rpt++)
            {
                for (var b = 0; b < maxBrigthness; b++)
                {
                    this.SetBrightness(b, deviceIndex);
                    TimePeriod.Sleep(onWaitTime);
                }
                for (var b = maxBrigthness; b >= 0; b--)
                {
                    this.SetBrightness(b, deviceIndex);
                    TimePeriod.Sleep(offWaitTime);
                }
                TimePeriod.Sleep(offWaitTime*10);
            }
        }
        
        /// <summary>
        /// Set the brightness of the display.
        /// Params:
        /// devIndex		the address of the display to control
        /// intensity	the brightness of the display. (0..15)
        /// </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="intensity"></param>
        public bool SetBrightness(int intensity, int deviceIndex = 0)
        {
            if(deviceIndex<0 || deviceIndex>=_deviceCount)
                return false;

            if (intensity >= 0 && intensity <= MAX_BRITGHNESS)
            {
                var r = SpiTransfer(deviceIndex, OP_INTENSITY, (byte) intensity);
                return r.Succeeded;
            }
            else return false;
        }

        /// <summary>
        /// Switch all Leds on the display off. 
        /// Params:
        /// devIndex	address of the display to control
        /// </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="refresh"></param>
        public void Clear(int deviceIndex = 0, bool refresh = false, bool all = false)
        {
            if (all)
            {
                for (var i = 0; i < this.DeviceCount; i++)
                    this.Clear(i, refresh:false);
                if (refresh)
                    WriteDisplay(all: true);
            }
            else
            {
                if (deviceIndex < 0 || deviceIndex >= _deviceCount)
                    return;
                int offset = deviceIndex*MATRIX_ROW_SIZE;
                for (int i = 0; i < MATRIX_ROW_SIZE; i++)
                {
                    _pixels[offset + i] = 0;
                }
                if (refresh)
                    WriteDisplay(deviceIndex);
            }
        }

        public void Copy(int deviceIndexSrc, int deviceIndexDest)
        {
            int offsetSrc  = deviceIndexSrc  * MATRIX_ROW_SIZE;
            int offsetDest = deviceIndexDest * MATRIX_ROW_SIZE;

            for (var i = 0; i < MATRIX_ROW_SIZE; i++)
            {
                if(deviceIndexSrc < 0  || deviceIndexSrc  >= _deviceCount) return;
                if(deviceIndexDest < 0 || deviceIndexDest >= _deviceCount) return;
                _pixels[offsetDest + i] = (byte)(_pixels[offsetSrc + i]);
            }
        }

        public void ScrollLeftDevices__BU(int deviceIndexSrc, int deviceIndexDest)
        {
            int offsetSrc  = deviceIndexSrc  * MATRIX_ROW_SIZE;
            int offsetDest = deviceIndexDest * MATRIX_ROW_SIZE;

            // Scroll the first matrix on the left, we lose a column of pixel
            for (var r = 0; r < MATRIX_ROW_SIZE; r++)
            {
                ScrollLeftColumn(deviceIndexSrc, r);
            }

            // Save the first column from the matrix on the right
            var tmpBit = new bool[MATRIX_COL_SIZE];
            for (var r = 0; r < MATRIX_ROW_SIZE; r++)
            {
                tmpBit[r] = BitUtil.IsSet(_pixels[deviceIndexDest * 8 + r], 128);
            }

            // Scroll the second matrix on the right, we lose a column of pixel
            for (var r = 0; r < MATRIX_ROW_SIZE; r++)
            {
                ScrollLeftColumn(deviceIndexDest, r);
            }

            // Copy the first column of second matrix on the right to matrix on the left
            for (var r = 0; r < MATRIX_ROW_SIZE; r++)
            {
                this.SetLed(deviceIndexSrc, this.Width - 1, r, tmpBit[r]);
            }
        }

        public void ScrollPixelLeftDevices(int deviceIndexSrc, int deviceIndexDest, int scrollCount = 1)
        {
            if (scrollCount > 1)
            {
                for (var i = 0; i < scrollCount; i++)
                    ScrollPixelLeftDevices(deviceIndexSrc, deviceIndexDest);
            }
            else
            {
                if (this._deviceCount == 1)
                {
                    this.ScrollLeft(0);
                    return;
                }

                var index = deviceIndexSrc;
                bool scrollFirstMaxtrix = true;
                while (true)
                {
                    __ScrollLeftDevices(index, index - 1, scrollFirstMaxtrix);
                    if (index - 1 == deviceIndexDest)
                        break;
                    index--;
                    scrollFirstMaxtrix = false;
                }
            }
        }

        public void __ScrollLeftDevices(int deviceIndexSrc, int deviceIndexDest, bool scrollFirstMaxtrix)
        {
            int offsetSrc  = deviceIndexSrc  * MATRIX_ROW_SIZE;
            int offsetDest = deviceIndexDest * MATRIX_ROW_SIZE;

            if (scrollFirstMaxtrix)
            {
                // Scroll the first matrix on the left, we lose a column of pixel
                for (var r = 0; r < MATRIX_ROW_SIZE; r++)
                {
                    ScrollLeftColumn(deviceIndexSrc, r);
                }
            }

            // Save the first column from the matrix on the right
            var tmpBit = new bool[MATRIX_COL_SIZE];
            for (var r = 0; r < MATRIX_ROW_SIZE; r++)
            {
                tmpBit[r] = BitUtil.IsSet(_pixels[offsetDest + r], 128);
            }

            // Scroll the second matrix on the right, we lose a column of pixel
            for (var r = 0; r < MATRIX_ROW_SIZE; r++)
            {
                ScrollLeftColumn(deviceIndexDest, r);
            }

            // Copy the first column of second matrix on the right to matrix on the left
            for (var r = 0; r < MATRIX_ROW_SIZE; r++)
            {
                this.SetLed(deviceIndexSrc, this.Width - 1, r, tmpBit[r]);
            }
        }

        public void ShiftRight(bool rotate, bool fillZero)
        {
            int last = this._deviceCount * 8 - 1;
            byte old = this._pixels[last];
            for (var i = this._pixels.Length-1; i > 0; i--)
	            this._pixels[i] = this._pixels[i-1];
            if (rotate) 
                this._pixels[0] = old;
            else 
                if (fillZero) 
                    this._pixels[0] = 0;
        }

        public void ScrollLeft(int deviceIndex = 0)
        {
            int offset = deviceIndex * MATRIX_ROW_SIZE;
            for (var i = 0; i < MATRIX_ROW_SIZE-1; i++)
            {
                if(deviceIndex < 0 || deviceIndex >= _deviceCount)
                    return;
                _pixels[offset + i] = (byte)(_pixels[offset + i + 1]);
            }
            _pixels[offset + MATRIX_ROW_SIZE - 1] = 0;
        }
        
        

        public void WriteSprite(int deviceIndex, int x, int y, int spriteIndex, List<byte> sprite)
        {
            spriteIndex = spriteIndex * 7; // 7 size of sprint data

	        int w = sprite[spriteIndex];
	        int h = sprite[spriteIndex+1];

            if (h == 8 && y == 0)
            {
                for (int i = 0; i < w; i++)
                {
                    int c = x + i;
                    if (c >= 0 && c < 80)
                        SetColumn(deviceIndex, c, sprite[spriteIndex + i + 2]);
                }
            }
            else
            {
                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        int c = x + i;
                        int r = y + j;
                        if (c >= 0 && c < 80 && r >= 0 && r < 8)
                            SetLed(deviceIndex, c, r, BitUtil.IsSet((int) sprite[spriteIndex + i + 2], (byte) j));
                    }
                }
            }
        }

        public void ScrollDown(int deviceIndex = 0)
        {
            for(var i = 0; i < MATRIX_ROW_SIZE; i++)
                ScrollRightColumn(deviceIndex, i);
        }

        public void ScrollUp(int deviceIndex = 0)
        {
            for(var i = 0; i < MATRIX_ROW_SIZE; i++)
                ScrollLeftColumn(deviceIndex, i);
        }

        public void ScrollLeftColumn(int deviceIndex, int row)
        {
            if(deviceIndex<0 || deviceIndex>=_deviceCount)
                return;
            int offset = deviceIndex * MATRIX_ROW_SIZE;
            _pixels[offset + row] = (byte)(_pixels[offset + row] << 1);
        }

        public void ScrollRightColumn(int deviceIndex, int row)
        {
            if(deviceIndex<0 || deviceIndex>=_deviceCount)
                return;
            int offset = deviceIndex * MATRIX_ROW_SIZE;
            _pixels[offset + row] = (byte)(_pixels[offset + row] >> 1);
        }
        
        public SPIEngine.SPIResult WriteRowForAllDevices(int row)
        {
            var l = new List<byte>();

            for (var deviceIndex = 0; deviceIndex < this.DeviceCount; deviceIndex++)
            {
                int offset = deviceIndex * MATRIX_ROW_SIZE;
                l.Add(_pixels[offset + row]); // 8 bit defined the 8 pixels in a row -- order will be reversed
                l.Add((byte)(row + 1));      // Api to set the row 1 -- order will be reversed
            }
            l.Reverse();
            var r = SpiTransferBuffer(l, false);
            return r;
        }

        public void WriteDisplay(int deviceIndex = 0, bool all = false)
        {
            if (all)
            {
                this.SelectDevice(true); // Turn the SPI select pin once for the all operation, make a big difference in speed

                // Write each row from 0 to 7 with one USB/SPI buffer command
                for (var i = 0; i < MATRIX_ROW_SIZE; i++)
                    WriteRowForAllDevices(i);

                this.SelectDevice(false);
            }
            else
            {
                var shouldSelectUnSelect = _alreadySelected == false;

                if(shouldSelectUnSelect)                    
                    this.SelectDevice(true); // Turn the SPI select pin once for the all operation, make a big difference in speed

                for (var i = 0; i < MATRIX_ROW_SIZE; i++)
                    WriteRow(deviceIndex, i);

                if(shouldSelectUnSelect)                    
                    this.SelectDevice(false);
            }
        }

        public SPIEngine.SPIResult WriteRow(int deviceIndex, int row, bool computeBufferOnly = false)
        {
            var r = new SPIEngine.SPIResult();
            if(deviceIndex<0 || deviceIndex>=_deviceCount)
                return r;

            int offset = deviceIndex * MATRIX_ROW_SIZE;

            r = SpiTransfer(deviceIndex, (byte)(row+1),_pixels[offset+row], computeBufferOnly: computeBufferOnly);
            return r;
        }

        public void DrawPixel(int deviceIndex, int x, int y, bool ledOn)
        {
            this.SetLed(deviceIndex, y, x, ledOn, false);
        }

        public void DrawPixel(int x, int y, bool ledOn)
        {
            this.DrawPixel(this.CurrentDeviceIndex, x, y, ledOn);
        }

        public override void DrawPixel(int16_t x, int16_t y, uint16_t color)
        {
            this.SetLed(this.CurrentDeviceIndex, y, x, color != 0, false);
        }

        public void SetRotation(int rotation)
        {
            this.Rotation = (byte)rotation;
        }

        /// <summary>
        /// Set the _pixels of a single Led.
        /// Params :
        /// devIndex	address of the display 
        /// row	the row of the Led (0..7)
        /// col	the column of the Led (0..7)
        /// state	If true the led is switched on, 
        /// 	if false it is switched off
        /// </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="state"></param>
        /// <param name="refresh"></param>
        public void SetLed(int deviceIndex, int column, int row, Boolean state, bool refresh = false)
        {
            byte val = 0x00;

            if (deviceIndex == -1)
                deviceIndex = this.CurrentDeviceIndex;

            if(deviceIndex < 0 || deviceIndex >= _deviceCount)
                return;
            
            if(row < 0 || row > MATRIX_COL_SIZE-1 || column < 0 || column > MATRIX_COL_SIZE-1)
                return;

            int offset = deviceIndex * MATRIX_ROW_SIZE;

            val = (byte)(128 >> column);
            if(state)
                _pixels[offset+row] = (byte)(_pixels[offset+row] | val);
            else {
                val=(byte)(~val);
                _pixels[offset+row] = (byte)(_pixels[offset+row] & val);
            }
            if(refresh)
                this.WriteRow(deviceIndex, row);
        }

        /// <summary>
        /// Set all 8 Led's in a row to a new state
        /// Params:
        /// devIndex	address of the display
        /// row	row which is to be set (0..7)
        /// value	each bit set to 1 will light up the
        /// 	corresponding Led.
        /// </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="row"></param>
        /// <param name="value"></param>
        public void SetRow(int deviceIndex, int row, byte value)
        {
            if(deviceIndex<0 || deviceIndex>=_deviceCount)
                return;
            if(row < 0 || row > MATRIX_ROW_SIZE-1)
                return;
            int offset = deviceIndex * MATRIX_ROW_SIZE;
            _pixels[offset+row]=value;
            SpiTransfer(deviceIndex, (byte)(row+1),_pixels[offset+row]);
        }

        /// <summary>
        /// Set all 8 Led's in a column to a new state
        /// Params:
        /// devIndex	address of the display
        /// col	column which is to be set (0..7)
        /// value	each bit set to 1 will light up the
        /// 	corresponding Led.
        /// </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="col"></param>
        /// <param name="value"></param>
        public void SetColumn(int deviceIndex, int col, byte value)
        {
            if(deviceIndex < 0 || deviceIndex >= _deviceCount)
                return;
            if(col < 0 || col > MATRIX_COL_SIZE-1) 
                return;

            for(int row = 0; row < MATRIX_ROW_SIZE; row++) 
            {
                byte val = (byte)(value >> (MATRIX_ROW_SIZE - 1 - row));
                val      = (byte)(val & 0x01);
                SetLed(deviceIndex,row,col,val == 0 ? false : true);
            }            
        }

        public List<bool> GetColumn(int deviceIndex, int colIndex)
        {
            var l = new List<bool>();
            int offset = deviceIndex * MATRIX_ROW_SIZE;
            var colIndexPower = (byte)Math.Pow(2, colIndex);
            for (int row = 0; row < MATRIX_ROW_SIZE; row++)
            {
                l.Add(BitUtil.IsSet((int)_pixels[offset + row], colIndexPower));
            }
            return l;
        }

        public List<bool> GetRow(int deviceIndex, int rowIndex)
        {
            var l = new List<bool>();
            int offset = deviceIndex * MATRIX_ROW_SIZE + rowIndex;
            for (int col = 0; col < MATRIX_COL_SIZE; col++)
            {
                var colIndexPower = (byte)Math.Pow(2, col);
                l.Add(BitUtil.IsSet((int)_pixels[offset], colIndexPower));
            }
            return l;
        }

        public void RotateRight(int deviceIndex)
        {
            this.RotateLeft(deviceIndex);
            this.RotateLeft(deviceIndex);
            this.RotateLeft(deviceIndex);
        }

        public void RotateLeft(int deviceIndex)
        {
            int offset = deviceIndex * MATRIX_ROW_SIZE;

            var valuesRow = new List<List<bool>>();
            for (var row = 0; row < MATRIX_ROW_SIZE; row++)
            {
                valuesRow.Add(GetRow(deviceIndex, row));
            }

            this.Clear(deviceIndex, refresh: false);
            for (var col = 0; col < MATRIX_COL_SIZE; col++)
            {
                for (var row = 0; row < MATRIX_ROW_SIZE; row++)
                {
                    if (valuesRow[col][row])
                        _pixels[offset + row] = BitUtil.SetBit(_pixels[offset + row], (byte)Math.Pow(2, MATRIX_COL_SIZE - col - 1));
                }
            }
        }

        /// <summary>
        /// Display a hexadecimal digit on a 7-Segment Display
        /// Params:
        /// devIndex	address of the display
        /// digit	the position of the digit on the display (0..7)
        /// value	the value to be displayed. (0x00..0x0F)
        /// dp	sets the decimal point.
        /// </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="digit"></param>
        /// <param name="value"></param>
        /// <param name="dp"></param>
        public void SetDigit(int deviceIndex, int digit, byte value, Boolean dp)
        {
            int offset;
            byte v;

            if(deviceIndex < 0 || deviceIndex >= _deviceCount)
                return;

            if(digit < 0 || digit > 7 || value > 15)
                return;

            offset = deviceIndex * MATRIX_ROW_SIZE;

            v = (byte)WinUtil.BitUtil.ParseBinary(charTable[value]); 

            if(dp)
                v|=128;

            _pixels[offset+digit] = v;

            SpiTransfer(deviceIndex, (byte)(digit+1), v);
        }

        /// <summary>
        /// Display a character on a 7-Segment display.
        /// There are only a few characters that make sense here :
        /// '0','1','2','3','4','5','6','7','8','9','0',
        /// 'A','b','c','d','E','F','H','L','P',
        /// '.','-','_',' ' 
        /// Params:
        /// devIndex	address of the display
        /// digit	the position of the character on the display (0..7)
        /// value	the character to be displayed. 
        /// dp	sets the decimal point.
        /// </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="digit"></param>
        /// <param name="value"></param>
        /// <param name="dp"></param>
        public void SetChar(int deviceIndex, int digit, char value, Boolean dp)
        {
            int offset;
            byte index, v;

            if(deviceIndex < 0 || deviceIndex >= _deviceCount)
                return;

            if(digit < 0 || digit > MATRIX_ROW_SIZE-1)
                return;

            offset = deviceIndex * MATRIX_ROW_SIZE;
            index  = (byte)value;

            if(index >127) {
                //no defined beyond index 127, so we use the space char
                index = 32;
            }
            v = (byte)WinUtil.BitUtil.ParseBinary(charTable[index]); 
            if(dp)
                v|=128;
            _pixels[offset+digit] = v;
            SpiTransfer(deviceIndex, (byte)(digit+1), v);
        }

        private bool  _alreadySelected = false;

        private void SelectDevice(bool state)
        {
            if(state) {
                this._spiEngine.Select();
                _alreadySelected = true;
            }
            else
            {
                this._spiEngine.Unselect();
                _alreadySelected = false;
            }
        }
        /*
        private SPIEngine.SPIResult SpiTransferBuffer__(List<byte> buffer, bool software = false)
        {
            var r = new SPIEngine.SPIResult();
            var shouldSelectUnSelect = _alreadySelected == false;
            try
            {
                if(shouldSelectUnSelect)
                    this.SelectDevice(true);

                if (software)
                {
                    for (var i = 0; i < buffer.Count; i++)
                    {
                        Shift.ShiftOut(this._nusbio, Nusbio.GetGpioIndex(this._spiEngine.MosiGpio), Nusbio.GetGpioIndex(this._spiEngine.ClockGpio), buffer[i]);
                    }
                }
                else
                {
                    var gs = new GpioSequence((this._nusbio as Nusbio).GetGpioMask(), this._nusbio.GetTransferBufferSize());
                    var i  = 0;
                    while (i < buffer.Count)
                    {
                        if (gs.EmptySpace >= GpioSequence.BIT_PER_BYTE)
                        {
                            // Add one byte to the gpio sequence
                            gs.ShiftOut(this._nusbio as Nusbio, this._spiEngine.MosiGpio, this._spiEngine.ClockGpio, buffer[i], dataAndClockOptimized:false);
                            i += 1;
                        }
                        else
                        {
                            if (gs.Send(this._nusbio as Nusbio))
                            {
                                var lastMaskValue = gs[gs.Count - 1];
                                gs = new GpioSequence(lastMaskValue, this._nusbio.GetTransferBufferSize());
                            }
                            else
                            {
                                r.Fail("SPI buffer was not sent");
                                break;
                            }
                        }
                    }
                    if (gs.Count > 0)
                    {
                        if (gs.Send(this._nusbio as Nusbio))
                            r.Succeeded = true;
                        else 
                            r.Fail("SPI buffer was not sent");
                    }
                }
            }
            finally
            {
                if(shouldSelectUnSelect)
                    this.SelectDevice(false);
            }
            return r;
        }
        */
        private SPIEngine.SPIResult SpiTransferBuffer(List<byte> buffer, bool software = false)
        {
            var r = new SPIEngine.SPIResult();
            var shouldSelectUnSelect = _alreadySelected == false;
            try
            {
                if(shouldSelectUnSelect)
                    this.SelectDevice(true);

                if (software)
                {
                    for (var i = 0; i < buffer.Count; i++)
                    {
                        Shift.ShiftOut(this._nusbio, Nusbio.GetGpioIndex(this._spiEngine.MosiGpio), Nusbio.GetGpioIndex(this._spiEngine.ClockGpio), buffer[i]);
                    }
                }
                else
                {
                    r = this._spiEngine.Transfer(buffer, select:false);    
                }
            }
            finally
            {
                if(shouldSelectUnSelect)
                    this.SelectDevice(false);
            }
            return r;
        }

        public static string LogDataToFile = null;

        public static uint LogBufferIndex = 0;

        private void LogBufferToLogFile(List<byte> buffer)
        {
            LogBufferIndex++;

            var b = new StringBuilder(1000);
            b.AppendFormat("Buffer {0}, size:{1}", LogBufferIndex, buffer.Count).AppendLine();
            for (var i = 0; i < buffer.Count; i+=4)
            {
                b.AppendFormat("[{0}]", i.ToString("000"));
                b.AppendFormat("{0} ", buffer[i+0].ToString("000"));
                if(i<buffer.Count-1)
                    b.AppendFormat("{0} ", buffer[i+1].ToString("000"));
                if(i<buffer.Count-2)
                    b.AppendFormat("{0} ", buffer[i+2].ToString("000"));
                if(i<buffer.Count-3)
                    b.AppendFormat("{0} ", buffer[i+3].ToString("000"));
                b.AppendLine();
            }
            System.IO.File.AppendAllText(LogDataToFile, b.ToString());
        }

        private SPIEngine.SPIResult SpiTransfer(int devIndex, byte opCode, byte data, bool software = false, bool computeBufferOnly = false)
        {
            SPIEngine.SPIResult r = null;
            var buffer            = new List<byte>(10);
            var deviceToSkipFirst = this.DeviceCount - devIndex - 1;

            for (var d = 0; d < deviceToSkipFirst; d++)
            {
                buffer.Add(0); // OpCode
                buffer.Add(0); // Data
            }

            buffer.Add(opCode); // OpCode
            buffer.Add(data);  // Data

            var deviceToSkipAfter = this.DeviceCount - (this.DeviceCount - devIndex);
            for (var d = 0; d < deviceToSkipAfter; d++)
            {
                buffer.Add(0); // OpCode
                buffer.Add(0); // Data
            }

            if (computeBufferOnly)
            {
                r = new SPIEngine.SPIResult() { Succeeded = true };
                r.ReadBuffer = buffer;
            }
            else
            {
                if (LogDataToFile != null)
                    LogBufferToLogFile(buffer);

                r = SpiTransferBuffer(buffer, software);
            }
            return r;
        }
    }
}

