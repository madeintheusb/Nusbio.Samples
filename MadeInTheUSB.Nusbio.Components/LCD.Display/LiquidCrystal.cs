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
    public class LiquidCrystal : LiquidCrystalBase, ILiquidCrystal 
    {
        int _rs_pin; // LOW: command.  HIGH: character.
        int _rw_pin; // LOW: write to LCD.  HIGH: read from LCD.
        int _enable_pin; // activated by a HIGH pulse.
        uint8_t[] _data_pins = new uint8_t[8];

        int _displayfunction;
        int _displaycontrol;
        int _displaymode;
        int _initialized;
        
        int _currline;
        
        /// <summary>
        /// 
        /// If set use Nusbio hardware acceleration. If using a gpio extender added to Nubio
        /// then we cannot use hardware acceleration
        /// </summary>
        public Nusbio Nusbio;

        public LiquidCrystal(IDigitalWriteRead digitalWriteRead, uint8_t rs, uint8_t rw, uint8_t enable,
                         uint8_t d0, uint8_t d1, uint8_t d2, uint8_t d3,
                         uint8_t d4, uint8_t d5, uint8_t d6, uint8_t d7) : base(digitalWriteRead)
        {
            Init(digitalWriteRead, 0, rs, rw, enable, d0, d1, d2, d3, d4, d5, d6, d7);
        }

        public LiquidCrystal(IDigitalWriteRead digitalWriteRead, uint8_t rs, uint8_t enable,
                         uint8_t d0, uint8_t d1, uint8_t d2, uint8_t d3,
                         uint8_t d4, uint8_t d5, uint8_t d6, uint8_t d7): base(digitalWriteRead)
        {
            Init(digitalWriteRead, 0, rs, 255, enable, d0, d1, d2, d3, d4, d5, d6, d7);
        }

        public LiquidCrystal(IDigitalWriteRead digitalWriteRead, uint8_t rs, uint8_t rw, uint8_t enable,
                         uint8_t d0, uint8_t d1, uint8_t d2, uint8_t d3): base(digitalWriteRead)
        {
            Init(digitalWriteRead, 1, rs, rw, enable, d0, d1, d2, d3, 0, 0, 0, 0);
        }

        public LiquidCrystal(Nusbio digitalWriteRead,  uint8_t rs, uint8_t enable,
                         uint8_t d0, uint8_t d1, uint8_t d2, uint8_t d3): base(digitalWriteRead)
        {
            Init(digitalWriteRead, 1, rs, 255, enable, d0, d1, d2, d3, 0, 0, 0, 0);
        }

        void Init(IDigitalWriteRead digitalWriteRead, uint8_t fourbitmode, uint8_t rs, uint8_t rw, uint8_t enable, uint8_t d0, uint8_t d1, uint8_t d2, uint8_t d3, uint8_t d4, uint8_t d5, uint8_t d6, uint8_t d7)
        {
            // Detect if the digitalWriteRead implementation is a real Nusbio rather than an gpio extender for Nusbio.
            // With Nusbio we can use some of the hardware acceleration to send the data
            if(digitalWriteRead is Nusbio) {
                this.Nusbio = digitalWriteRead as Nusbio;
            }
            
            _rs_pin       = rs;
            _rw_pin       = rw;
            _enable_pin   = enable;

            _data_pins[0] = d0;
            _data_pins[1] = d1;
            _data_pins[2] = d2;
            _data_pins[3] = d3;
            _data_pins[4] = d4;
            _data_pins[5] = d5;
            _data_pins[6] = d6;
            _data_pins[7] = d7;
                        
            this.SetPinMode(_rs_pin, MadeInTheUSB.GPIO.PinMode.Output);
            // we can save 1 pin by not using RW. Indicate by passing 255 instead of pin#
            if (_rw_pin != 255)
            {
                SetPinMode(_rw_pin, MadeInTheUSB.GPIO.PinMode.Output);
            }
            SetPinMode(_enable_pin, MadeInTheUSB.GPIO.PinMode.Output);

            if (fourbitmode > 0)
                _displayfunction = LCD_4BITMODE | LCD_1LINE | LCD_5x8DOTS;
            else
                _displayfunction = LCD_8BITMODE | LCD_1LINE | LCD_5x8DOTS;
            Begin(16, 1);
        }

        public override void Begin(uint8_t cols, uint8_t lines, int16_t dotsize = -1)
        {
            if (dotsize == -1)
                dotsize = LCD_5x8DOTS;

            if (lines > 1)
            {
                _displayfunction |= LCD_2LINE;
            }
            NumLines = lines;
            _currline = 0;
            NumCols  = cols;

            // for some 1 line displays you can select a 10 pixel high font
            if ((dotsize != 0) && (lines == 1))
            {
                _displayfunction |= LCD_5x10DOTS;
            }

            // SEE PAGE 45/46 FOR INITIALIZATION SPECIFICATION!
            // according to datasheet, we need at least 40ms after power rises above 2.7V
            // before sending commands. Arduino can turn on way before 4.5V so we'll wait 50
            DelayMicroseconds(50000);
            // Now we pull both RS and R/W low to Begin commands
            DigitalWrite(_rs_pin, PinState.Low);
            DigitalWrite(_enable_pin, PinState.Low); // MUST BE LOW BY DEFAULT
            if (_rw_pin != 255)
            {
                DigitalWrite(_rw_pin, PinState.Low);
            }

            //put the LCD into 4 bit or 8 bit mode
            if (!((_displayfunction & LCD_8BITMODE) == LCD_8BITMODE))
            {
                // this is according to the hitachi HD44780 datasheet
                // figure 24, pg 46

                // we start in 8bit mode, try to set 4 bit mode
                Write4bits(0x03);
                DelayMicroseconds(4500); // wait min 4.1ms

                // second try
                Write4bits(0x03);
                DelayMicroseconds(4500); // wait min 4.1ms

                // third go!
                Write4bits(0x03);
                DelayMicroseconds(150);

                // finally, set to 4-bit interface
                Write4bits(0x02);
            }
            else
            {
                // this is according to the hitachi HD44780 datasheet
                // page 45 figure 23

                // Send function set command sequence
                Command(LCD_FUNCTIONSET | _displayfunction);
                DelayMicroseconds(4500);  // wait more than 4.1ms

                // second try
                Command(LCD_FUNCTIONSET | _displayfunction);
                DelayMicroseconds(150);

                // third go
                Command(LCD_FUNCTIONSET | _displayfunction);
            }

            // finally, set # lines, font size, etc.
            Command(LCD_FUNCTIONSET | _displayfunction);

            // turn the Display on with no Cursor or blinking default
            _displaycontrol = LCD_DISPLAYON | LCD_CURSOROFF | LCD_BLINKOFF;
            Display();

            // Clear it off
            Clear();

            // Initialize to default text direction (for romance languages)
            _displaymode = LCD_ENTRYLEFT | LCD_ENTRYSHIFTDECREMENT;
            // set the entry mode
            Command(LCD_ENTRYMODESET | _displaymode);
        }


        /********** high level commands, for the user! */
        public void Clear()
        {
            Command(LCD_CLEARDISPLAY);  // Clear Display, set Cursor position to zero
            DelayMicroseconds(2000);  // this command takes a long time!
        }

        public void Home()
        {
            Command(LCD_RETURNHOME);  // set Cursor position to zero
            DelayMicroseconds(2000);  // this command takes a long time!
        }

        public void SetCursor(int col, int row)
        {
            int[] row_offsets = new int[4] { 0x00, 0x40, 0x14, 0x54 };
            if (row >= NumLines)
            {
                row = NumLines - 1;    // we count rows starting w/0
            }

            Command(LCD_SETDDRAMADDR | (col + row_offsets[row]));
        }

        // Turn the Display on/off (quickly)
        public void NoDisplay()
        {
            _displaycontrol &= ~LCD_DISPLAYON;
            Command(LCD_DISPLAYCONTROL | _displaycontrol);
        }

        public void Display()
        {
            _displaycontrol |= LCD_DISPLAYON;
            Command(LCD_DISPLAYCONTROL | _displaycontrol);
        }

        // Turns the underline Cursor on/off
        public void NoCursor()
        {
            _displaycontrol &= ~LCD_CURSORON;
            Command(LCD_DISPLAYCONTROL | _displaycontrol);
        }

        public void Cursor()
        {
            _displaycontrol |= LCD_CURSORON;
            Command(LCD_DISPLAYCONTROL | _displaycontrol);
        }

        // Turn on and off the blinking Cursor
        public void NoBlink()
        {
            _displaycontrol &= ~LCD_BLINKON;
            Command(LCD_DISPLAYCONTROL | _displaycontrol);
        }

        public void Blink()
        {
            _displaycontrol |= LCD_BLINKON;
            Command(LCD_DISPLAYCONTROL | _displaycontrol);
        }

        // These commands scroll the Display without changing the RAM
        public void ScrollDisplayLeft()
        {
            Command(LCD_CURSORSHIFT | LCD_DISPLAYMOVE | LCD_MOVELEFT);
        }

        public void ScrollDisplayRight()
        {
            Command(LCD_CURSORSHIFT | LCD_DISPLAYMOVE | LCD_MOVERIGHT);
        }

        // This is for text that flows Left to Right
        public void LeftToRight()
        {
            _displaymode |= LCD_ENTRYLEFT;
            Command(LCD_ENTRYMODESET | _displaymode);
        }

        // This is for text that flows Right to Left
        public void RightToLeft()
        {
            _displaymode &= ~LCD_ENTRYRIGHT;
            Command(LCD_ENTRYMODESET | _displaymode);
        }

        // This will 'right justify' text from the Cursor
        public void Autoscroll()
        {
            _displaymode |= LCD_ENTRYSHIFTINCREMENT;
            Command(LCD_ENTRYMODESET | _displaymode);
        }

        // This will 'left justify' text from the Cursor
        public void NoAutoscroll()
        {
            _displaymode &= ~LCD_ENTRYSHIFTINCREMENT;
            Command(LCD_ENTRYMODESET | _displaymode);
        }

        public void CreateChar(uint8_t location, int [] charmap)
        {
            var l = new List<byte>();
            foreach(var v in charmap)
                l.Add((byte)v);
            this.CreateChar(location, l.ToArray());
        }
        // Allows us to fill the first 8 CGRAM locations
        // with custom characters
        public void CreateChar(uint8_t location, uint8_t[] charmap)
        {
            location &= 0x7; // we only have 8 locations 0-7
            Command(LCD_SETCGRAMADDR | (location << 3));
            for (int i = 0; i < 8; i++)
            {
                this.Write(charmap[i]);
            }
        }

        /*********** mid level commands, for sending data/cmds */

        void Command(int value)
        {
            Command((uint8_t)value);
        }

        void Command(uint8_t value)
        {
            Send(value, SendType.Command);
        }

        public size_t Write(int value)
        {
            return Write((byte)value);
        }

        public size_t Write(uint8_t value)
        {
            Send(value, SendType.Data);
            return 1; // assume sucess
        }

        /************ low level data pushing commands **********/
        // write either command or data, with automatic 4/8-bit selection

        internal enum SendType
        {
            Undefined,
            Command,
            Data
        };

        private SendType _sendType = SendType.Undefined;

        void Send(uint8_t value, SendType type)
        {
            if (type != _sendType) { 

                _sendType = type;
                DigitalWrite(_rs_pin, type == SendType.Command ? PinState.Low : PinState.High);
            }

            // if there is a RW pin indicated, set it low to Write
            if (_rw_pin != 255)
            {
                DigitalWrite(_rw_pin, PinState.Low);
            }

            if ((_displayfunction & LCD_8BITMODE) == LCD_8BITMODE)
            {
                Write8bits(value);
            }
            else
            {
                Write4bits(value >> 4);
                Write4bits(value);
            }
        }

        private bool OptimizeForNusbio()
        {
            return this.Nusbio != null;
        }

        void PulseEnable()
        {
            if (OptimizeForNusbio()) { 

                // Use Nusbio hardware acceleration to send the pulse
                var gs = new GpioSequence(this.Nusbio.GetGpioMask());
                // Already low by default -- gs.DigitalWrite(this.Nusbio[_enable_pin], PinState.Low);
                gs.DigitalWrite(this.Nusbio[_enable_pin], PinState.High);
                gs.DigitalWrite(this.Nusbio[_enable_pin], PinState.Low);
                this.Nusbio.SetGpioMask(gs.ToArray());
            }
            else { 
                // Use regular bit banging which is compatible with a gpio extender added to Nusbio
                // but this is slower, because there is not hardware acceleration
                // Already low by default -- DigitalWrite(_enable_pin, PinState.Low);
                DigitalWrite(_enable_pin, PinState.High);
                DigitalWrite(_enable_pin, PinState.Low);
            }
        } 

        void Write4bits(int value)
        {
            //// Software bit banging -- slow
            //// Bit bang by software the state of the 4 wires
            //for (int i = 0; i < 4; i++)
            //{
            //    var b = (value >> i) & 0x01;
            //    DigitalWrite(_data_pins[i], b);
            //}
            //var mask0 = base._DigitalWriteRead.GetGpioMask();
            //var aamaskB1 = WinUtil.BitUtil.BitRpr(mask0);
            //TimePeriod.__SleepMicro(1);
            //PulseEnable();
            //return;
            
            //byte bit  = 0;
            byte mask = 0;

            if (this.OptimizeForNusbio()) { 

                // This optimized code below commented does not work, so I revert
                // to use the slow bit banging to set the data.
                // Bizarely, sending the pulse the Nusbio hardware acceleration works
                // and make a different in the communication speed

                // Software bit banging -- slow
                // Bit bang by software the state of the 4 wires
                for (int i = 0; i < 4; i++)
                {
                    DigitalWrite(_data_pins[i], ((value >> i) & 0x01));
                }
                TimePeriod.__SleepMicro(1);                         
                /*
                // Compute the mask for the state of the 4 bits and make a call to the _DigitalWriteRead 
                mask = base._DigitalWriteRead.GetGpioMask();
                var maskB1 = WinUtil.BitUtil.BitRpr(mask);
                var maskB2 = WinUtil.BitUtil.BitRpr(mask);

                for (int i = 0; i < 4; i++)
                {
                    var gpioIndex = (byte)(_data_pins[i] - base._DigitalWriteRead.GpioStartIndex);
                    bit           = this.Nusbio.GetGpioBitFromIndex(gpioIndex);
                    var b         = (value >> i) & 0x01;
                    if (b == 1)
                    {
                        mask = WinUtil.BitUtil.SetBit(mask, bit);
                    }
                    else
                    {
                        mask = WinUtil.BitUtil.UnsetBit(mask, bit);
                    }
                    maskB2 = WinUtil.BitUtil.BitRpr(mask);
                }
                base._DigitalWriteRead.SetGpioMask(mask);
                */
            }
            else { 

                // Compute the mask for the state of the 4 bits and make a call to the _DigitalWriteRead 
                mask = base._DigitalWriteRead.GetGpioMask();
                for (int i = 0; i < 4; i++)
                {
                    var b = (value >> i) & 0x01;
                    if (b == 1)
                    {
                        mask = WinUtil.BitUtil.SetBitIndex(mask, (byte)(_data_pins[i] - base._DigitalWriteRead.GpioStartIndex));
                    }
                    else
                    {
                        mask = WinUtil.BitUtil.UnsetBitByIndex(mask, (byte)(_data_pins[i] - base._DigitalWriteRead.GpioStartIndex));
                    }
                }
                base._DigitalWriteRead.SetGpioMask(mask);
            }
            TimePeriod.__SleepMicro(1);
            PulseEnable();
        }

        void Write8bits(int value)
        {
            for (int i = 0; i < 8; i++)
            {
                DigitalWrite(_data_pins[i], (value >> i) & 0x01);
            }
            PulseEnable();
        }


        public override string Print(string format, params object[] args)
        {
            var text = String.Format(format, args);
            foreach (var c in text)
            {
                this.Write((uint8_t)c);
            }
            return text;
        }
         
        public string Print(int x, int y, string format, params object[] args)
        {
            var text = string.Format(format, args);
            if (x == -1)
                x = (this.NumCols - text.Length)/2;
            this.SetCursor(x, y);
            return this.Print(format, args);
        }

        public void Flash(int count, int waitTime = 230)
        {
            for (var i = 0; i < count; i++)
            {
                this.NoDisplay();
                TimePeriod.Sleep(waitTime);
                this.Display();
                TimePeriod.Sleep(waitTime);
            }
        }

        public void ProgressBar(int x, int y, int maxChar, int percent, string text = null)
        {
            this.SetCursor(x, y);
            if(text != null)
                this.Print(text);

            for (var i = 0; i < maxChar; i++)
            {
                if(i < (maxChar * percent / 100))
                    this.Write(0xFF);
                else
                    this.Write(32);
            }
        }
    }
}
