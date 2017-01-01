/*
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
  
    Written by FT for MadeInTheUSB
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

using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Components.Interface;
using MadeInTheUSB.i2c;
//using abs = System.Math..

namespace MadeInTheUSB.Adafruit
{
    public class LEDBackpack : Adafruit_GFX, MadeInTheUSB.Components.Interface.Ii2cOut
    {
        private const string DEFAULT_I2C_ERROR_MESSAGE = "I2C command failed, check your connections";

        private const byte HT16K33_BLINK_CMD = 0x80;
        private const byte HT16K33_BLINK_DISPLAYON = 0x01;
        private const byte HT16K33_BLINK_OFF = 0;
        private const byte HT16K33_BLINK_2HZ = 1;
        private const byte HT16K33_BLINK_1HZ = 2;
        private const byte HT16K33_BLINK_HALFHZ = 3;
        private const byte HT16K33_CMD_BRIGHTNESS = 0xE0;
        private const byte HT16K33_CMD_TURN_OSCILLATOR_ON = 0x21;

        public const int _displayBufferRowCount = 8;
        public byte[] _displayBuffer = new byte[_displayBufferRowCount];

        public int DeviceId;
#if !NUSBIO2
        protected I2CEngine _i2c;
        private Nusbio _nusbio;
#endif
#if NUSBIO2
            public LEDBackpack(int16_t width, int16_t height)
            : base(width, height)
            {
            }

#else

        public LEDBackpack(int16_t width, int16_t height, Nusbio nusbio, NusbioGpio sdaOutPin, NusbioGpio sclPin)
        : base(width, height)
        {
            this._nusbio = nusbio;
            this._i2c = new I2CEngine(nusbio, sdaOutPin, sclPin, 0);
        }
#endif

        public void DrawPixel(int x, int y, bool color)
        {
            this.DrawPixel((int16_t)x, (int16_t)y, (uint16_t)(color ? 1 : 0));
        }

        public override void DrawPixel(int16_t x, int16_t y, uint16_t color)
        {
            if ((y < 0) || (y >= 8)) return;
            if ((x < 0) || (x >= 8)) return;

            // check _rotation, move pixel around if necessary
            switch (this._rotation)
            {
                case 1:
                    Swap(ref x, ref y);
                    x = (int16_t)(8 - x - 1);
                    break;
                case 2:
                    x = (int16_t)(8 - x - 1);
                    y = (int16_t)(8 - y - 1);
                    break;
                case 3:
                    Swap(ref x, ref y);
                    y = (int16_t)(8 - y - 1);
                    break;
            }

            // wrap around the x
            x += 7;
            x %= 8;

            if (color > 0)
            {
                _displayBuffer[y] |= (byte)(1 << x); //   [y] is the row x bit for the column
            }
            else
            {
                _displayBuffer[y] &= (byte)(~(1 << x));
            }
        }


        public bool Detect(byte addr = 0x70)
        {
            try
            {
                this.Begin(addr);
                return true;
            }
            catch (I2CCommunicationException)
            {
                return false;
            }
            catch (System.Exception ex)
            {
                return false;
            }
        }

        public void SetRotation(int v)
        {
            base.Rotation = (byte)v;
        }

        public virtual void Begin(int addr = 0x70)
        {
            this._begin((byte)addr);
        }

        private void _begin(byte addr = 0x70)
        {
            this.DeviceId = addr;
#if NUSBIO2
            
#else
            if (this._i2c.DeviceId == 0)
                this._i2c.DeviceId = (byte)(addr);
#endif

            this._ii2cOut.i2c_Send1ByteCommand(HT16K33_CMD_TURN_OSCILLATOR_ON);
            this.SetBlinkRate(HT16K33_BLINK_OFF);
            this.SetBrightness(5); // 0 to 15
            this.Clear(true);
        }


        public void AnimateSetBrightness(int MAX_REPEAT, int onWaitTime = 20, int offWaitTime = 30)
        {
            for (byte rpt = 0; rpt < MAX_REPEAT; rpt++)
            {
                for (byte b = 0; b < 15; b++)
                {
                    this.SetBrightness(b);
                    TimePeriod.Sleep(onWaitTime);
                }
                for (byte b = 15; b > 0; b--)
                {
                    this.SetBrightness(b);
                    TimePeriod.Sleep(offWaitTime);
                }
            }
        }

        public void SetBrightness(int b)
        {
            SetBrightness((byte)b);
        }

        private byte _brightness;

        public byte GetBrightness()
        {
            return this._brightness;
        }

        public void SetBrightness(byte b)
        {
            if (b > 15) b = 15;
            this._brightness = b;
            if (!_ii2cOut.i2c_Send1ByteCommand((byte)(HT16K33_CMD_BRIGHTNESS | b)))
                throw new I2CCommunicationException(DEFAULT_I2C_ERROR_MESSAGE);
        }

        public void SetBlinkRate(byte b)
        {
            if (b > 3) b = 0; // turn off if not sure  
            if (!_ii2cOut.i2c_Send1ByteCommand((byte)(HT16K33_BLINK_CMD | HT16K33_BLINK_DISPLAYON | (b << 1))))
                throw new I2CCommunicationException(DEFAULT_I2C_ERROR_MESSAGE);
        }

        public virtual void Clear(bool refresh = false)
        {
            for (var i = 0; i < 8; i++)
            {
                _displayBuffer[i] = 0;
            }
            if (refresh)
                this.WriteDisplay();
        }

        public bool WriteDisplay()
        {
            var buf = new List<uint8_t>();
            byte addr = 0; // Start of screen
            buf.Add(addr);
            for (var i = 0; i < 8; i++)
            {
                buf.Add((uint8_t)(_displayBuffer[i] & 0xFF));    // 8 bit of columns
                buf.Add((uint8_t)(_displayBuffer[i] >> 8));
            }
            return _ii2cOut.i2c_WriteBuffer(buf.ToArray());
        }

        Ii2cOut _ii2cOut
        {
            get
            {
                return ((Ii2cOut)(this));
            }
        }

#if NUSBIO2
         //MadeInTheUSB.Components.Interface.Ii2cOut
        bool Ii2cOut.i2c_Send1ByteCommand(byte c)
        {
            return Nusbio2NAL.I2C_Helper_Write1Byte(this.DeviceId, c) == 1;
        }

        bool Ii2cOut.i2c_Send2ByteCommand(byte c0, byte c1)
        {
            throw new NotImplementedException();
        }

        bool Ii2cOut.i2c_WriteBuffer(byte[] buffer)
        {
            var b = new List<byte>() {(byte)this.DeviceId};
            b.AddRange(buffer);
            return Nusbio2NAL.I2C_Helper_Write(this.DeviceId, b.ToArray()) == 1;
        }
        bool Ii2cOut.i2c_WriteReadBuffer(byte[] writeBuffer, byte[] readBuffer)
        {
            throw new NotImplementedException();
        }
#else

        //MadeInTheUSB.Components.Interface.Ii2cOut
        bool Ii2cOut.i2c_Send1ByteCommand(byte c)
        {
            return this._i2c.Send1ByteCommand(c);
        }

        bool Ii2cOut.i2c_Send2ByteCommand(byte c0, byte c1)
        {
            throw new NotImplementedException();
        }

        bool Ii2cOut.i2c_WriteBuffer( byte[] buffer)
        {
            return this._i2c.WriteBuffer(buffer);
        }

        bool Ii2cOut.i2c_WriteReadBuffer(byte[] writeBuffer, byte[] readBuffer)
        {
            throw new NotImplementedException();
        }
#endif
    }
}

