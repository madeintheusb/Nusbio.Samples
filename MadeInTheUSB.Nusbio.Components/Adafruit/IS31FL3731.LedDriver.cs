/*
    Written by FT for MadeInTheUSB
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
 
    This code is based from 
  
    Adafruit_MCP9808 - library - https://github.com/adafruit/Adafruit_MCP9808_Library
  
    See Adafruit: 
        - MCP9808 High Accuracy I2C Temperature Sensor Breakout Board - https://www.adafruit.com/product/1782
        - Adafruit MCP9808 Precision I2C Temperature Sensor Guide - https://learn.adafruit.com/adafruit-mcp9808-precision-i2c-temperature-sensor-guide/overview
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MadeInTheUSB.i2c;
using MadeInTheUSB.WinUtil;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
using MadeInTheUSB.Components.Interface;

namespace MadeInTheUSB.Adafruit
{
    /// <summary>
    /// datasheet https://cdn-learn.adafruit.com/assets/assets/000/030/994/original/31FL3731.pdf?1457554773 
    /// 144 Led = 16 x 9
    /// </summary>
    public class IS31FL3731 : Adafruit_GFX, MadeInTheUSB.Components.Interface.Ii2cOut
    {
        /// <summary>
        /// The chip IS31FL3731 is I2C 400 000 Hz
        /// http://www.issi.com/WW/pdf/31FL3731.pdf
        /// Nusbio default is 912 600 baud which is 
        /// </summary>
        public const int MAX_BAUD_RATE                 = 921600 / 2;

        public const int MAX_WIDTH                     = 16;
        public const int MAX_HEIGHT                    = 9;

        public const int ISSI_ADDR_DEFAULT             = 0x74;

        public const int ISSI_REG_CONFIG               = 0x00;
        public const int ISSI_REG_CONFIG_PICTUREMODE   = 0x00;
        public const int ISSI_REG_CONFIG_AUTOPLAYMODE  = 0x08;
        public const int ISSI_REG_CONFIG_AUDIOPLAYMODE = 0x18;

        public const int ISSI_CONF_PICTUREMODE         = 0x00;
        public const int ISSI_CONF_AUTOFRAMEMODE       = 0x04;
        public const int ISSI_CONF_AUDIOMODE           = 0x08;

        public const int ISSI_REG_PICTUREFRAME         = 0x01;

        public const int ISSI_REG_SHUTDOWN             = 0x0A;
        public const int ISSI_REG_AUDIOSYNC            = 0x06;

        public const int ISSI_COMMANDREGISTER          = 0xFD; // 253
        public const int ISSI_BANK_FUNCTIONREG         = 0x0B; // 11 helpfully called 'page nine'

        public const int ISSI_PWM_REGISTER_LED0        = 0x24;

#if NUSBIO2
        
#else
        private Nusbio _nusbio;
        private I2CEngine _i2c;
#endif

        public int DeviceId;

        private int _frame;
        byte[,] _buffer = new byte[MAX_WIDTH, MAX_HEIGHT];

        public void ScrollPixelLeftDevices(int scrollCount = 1)
        {
            for (var i = 0; i < scrollCount; i++)
                __ScrollPixelLeftDevices();
        }

        private void __ScrollPixelLeftDevices()
        {
            for (var r = 0; r < this.Height; r++)
            {
                for (var c = 0; c < this.Width - 1; c++)
                {
                    this._buffer[c, r] = this._buffer[c + 1, r];
                }
                this._buffer[this.Width - 1, r] = 0;
            }
        }

        public IS31FL3731(Nusbio nusbio, NusbioGpio sdaOutPin, NusbioGpio sclPin, byte deviceId = ISSI_ADDR_DEFAULT, int width = 16, int height = 9) : base((Int16)width, (Int16)height)
        {
            this._i2c    = new I2CEngine(nusbio, sdaOutPin, sclPin, deviceId);
            this._nusbio = nusbio;
        }

        public bool Begin(byte deviceAddress = ISSI_ADDR_DEFAULT)
        {
            try
            {
                #if !NUSBIO2
                this._i2c.DeviceId = deviceAddress;
                #endif
                this.DeviceId = deviceAddress;

                this._frame = 0;

                if (!WriteRegister8(ISSI_BANK_FUNCTIONREG, ISSI_REG_SHUTDOWN, 0x00)) return false; // shutdown
                Thread.Sleep(10);
                //if (!WriteRegister8(ISSI_BANK_FUNCTIONREG, ISSI_REG_SHUTDOWN, 0x01)) return false; // out of shutdown

                // picture mode
                if (!WriteRegister8(ISSI_BANK_FUNCTIONREG, ISSI_REG_CONFIG, ISSI_REG_CONFIG_PICTUREMODE)) return false;

                if (!DisplayFrame(_frame)) return false;

                // all LEDs on & 0 PWM
                Clear(); // set each led to 0 PWM

                for (uint8_t f = 0; f < 8; f++)
                {
                    for (uint8_t i = 0; i <= 0x11; i++)
                        if (!WriteRegister8(f, i, 0xff)) return false;  // each 8 LEDs on
                }

                if (!WriteRegister8(ISSI_BANK_FUNCTIONREG, ISSI_REG_SHUTDOWN, 0x01)) return false; // out of shutdown

                return AudioSync(false);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
                return false;
            }
        }

        public void Clear(int frame = 0, bool refresh = true)
        {
            for (var r = 0; r < MAX_HEIGHT; r++)
                for (var c = 0; c < MAX_WIDTH; c++)
                    _buffer[c, r] = 0;

            if (refresh)
                UpdateDisplay(frame);
        }

        public double UpdateDisplay(int frame)
        {
            var sw = StopWatch.StartNew();
            int byteCount = 0;

            this.SelectFrame(frame);
            byteCount += 2;

            var flatBuffer = new List<byte>();
            for (var r = 0; r < MAX_HEIGHT; r++)
                for (var c = 0; c < MAX_WIDTH; c++)
                    flatBuffer.Add(_buffer[c, r]);

            var flatBufferIndex = 0;
            // 6x24=144
            for (uint8_t i = 0; i < 6; i++)
            {
                var command = (byte)(ISSI_PWM_REGISTER_LED0 + i * 24);
                var buffer = new List<byte>();

                for (uint8_t j = 0; j < 24; j++)
                    buffer.Add(flatBuffer[flatBufferIndex++]);

                this.SendBuffer(command, buffer);
                byteCount += buffer.Count;
            }
            this.DisplayFrame(frame);
            byteCount += 2;
            sw.Stop();

            // Return number of byte / second
            return byteCount / (sw.ElapsedMilliseconds / 1000.0);
        }

        private bool SendBuffer(byte command, List<byte> buffer)
        {
            var l = new List<byte>();
            l.Add(command);
            l.AddRange(buffer);
            return ((Ii2cOut)this).i2c_WriteBuffer(l.ToArray());
        }

        public void Fill(List<List<byte>> val)
        {
            SelectFrame(_frame);
            // 6x24=144
            for (uint8_t i = 0; i < 6; i++)
            {
                var command = (byte)(ISSI_PWM_REGISTER_LED0 + i * 24);
                var buffer  = new List<byte>();

                for (uint8_t j = 0; j < 24; j++)
                    buffer.Add(val[i][j]);
                
                this.SendBuffer(command, buffer);
            }
        }

        public void Fill(byte val, int frame = -1, bool refresh = true)
        {
            if (frame == -1)
                frame = _frame;

            this.SelectFrame(frame);

            // 6x24=144
            for (uint8_t i = 0; i < 6; i++)
            {
                var command = (byte)(ISSI_PWM_REGISTER_LED0 + i * 24);
                var buffer = new List<byte>();

                for (uint8_t j = 0; j < 24; j++)
                    buffer.Add((byte)val);

                this.SendBuffer(command, buffer);
            }
            if (refresh)
                this.DisplayFrame(frame);
        }

        private void SwapInt16(ref int16_t a, ref int16_t b)
        {
            int16_t t = a; a = b; b = t;
        }

        public void DrawPixel(int x, int y, int color)
        {
            __DrawPixel((int16_t)x, (int16_t)y, (uint16_t)color);
        }

        public override void DrawPixel(int16_t x, int16_t y, uint16_t color)
        {
            __DrawPixel(x, y, color);
        }

        private void __DrawPixel(int16_t x, int16_t y, uint16_t color)
        {
            // check rotation, move pixel around if necessary
            switch (this._rotation)
            {
                case 1:
                    SwapInt16(ref x, ref y);
                    x = (int16_t)(16 - x - 1);
                    break;
                case 2:
                    x = (int16_t)(16 - x - 1);
                    y = (int16_t)(9 - y - 1);
                    break;
                case 3:
                    SwapInt16(ref x, ref y);
                    y = (int16_t)(9 - y - 1);
                    break;
            }

            if ((x < 0) || (x >= 16)) return;
            if ((y < 0) || (y >= 9)) return;
            if (color > 255) color = 255; // PWM 8bit max

            //SetLedPwm((uint8_t)(x + y*16), (uint8_t)color, (uint8_t)_frame);
            SetLedPwm(x, y, (uint8_t)color, (uint8_t)_frame);
            return;
        }

        public bool DisplayFrame(int f)
        {
            if (f > 7) f = 0;
            return this.WriteRegister8(ISSI_BANK_FUNCTIONREG, ISSI_REG_PICTUREFRAME, (uint8_t)f);
        }

        public bool SelectFrame(int b)
        {
            if (_frame != b)
            {
                _frame = b;                
                return ((Ii2cOut)this).i2c_WriteBuffer(new byte[2] { (byte)ISSI_COMMANDREGISTER, (byte)b });
            }
            else
                return true;
        }

        public void SetLedPwm(int x, int y, int pwm, int frame = -1, bool buffered = true)
        {
            if (buffered)
                this._buffer[x, y] = (byte)pwm;
            else
                SetLedPwm((uint8_t)(x + y * 16), (uint8_t)pwm, frame);
        }

        public void SetLedPwm(uint8_t lednum, uint8_t pwm, int frame = -1)
        {
            if (frame == -1)
                frame = this._frame;
            if (lednum >= 144)
                return;
            WriteRegister8((uint8_t)frame, (uint8_t)(ISSI_PWM_REGISTER_LED0 + lednum), pwm);
        }

        private bool AudioSync(bool sync)
        {
            if (sync)
            {
                return this.WriteRegister8(ISSI_BANK_FUNCTIONREG, ISSI_REG_AUDIOSYNC, 0x1);
            }
            else
            {
                return this.WriteRegister8(ISSI_BANK_FUNCTIONREG, ISSI_REG_AUDIOSYNC, 0x0);
            }
        }

        //private uint __currentBaud = 0;
        //private bool __resetBaudRate = false;

        private bool WriteRegister8(uint8_t b, uint8_t reg, uint8_t data)
        {
            if (!this.SelectFrame(b)) return false;
            return ((Ii2cOut)this).i2c_WriteBuffer(new byte[2] { (byte)reg, (byte)data });
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

        bool Ii2cOut.i2c_WriteBuffer(byte[] buffer)
        {
            return this._i2c.WriteBuffer( buffer);//(byte)this.DeviceId,
        }

        bool Ii2cOut.i2c_WriteReadBuffer(byte[] writeBuffer, byte[] readBuffer)
        {
            throw new NotImplementedException();
        }
#endif        




    }

    public class LandscapeIS31FL3731
    {
        public IS31FL3731 _IS31FL3731;
        public int CurrentYPosition = 0;
        public int CurrentXPosition = 0;
        private int doubleBufferIndex = 0;

        public LandscapeIS31FL3731(IS31FL3731 is31FL3731)
        {
            this._IS31FL3731 = is31FL3731;
            this.CurrentXPosition = this._IS31FL3731.Width - 1;
            this.CurrentYPosition = this._IS31FL3731.Height - 1;
            this._IS31FL3731.Clear();
        }

        private Random _seed = new Random(Environment.TickCount);

        private int NewDirectionRandomizer()
        {
            var r = _seed.Next(2);
            return r == 0 ? 1 : -1;
        }

        public override string ToString()
        {
            return string.Format("x:{0}, y:{1}", this.CurrentXPosition, this.CurrentYPosition);
        }

        public double Redraw()
        {
            _IS31FL3731.ScrollPixelLeftDevices();
            _IS31FL3731.DrawPixel(CurrentXPosition, CurrentYPosition, 128);
            var bytePerSec = _IS31FL3731.UpdateDisplay(doubleBufferIndex);

            doubleBufferIndex = doubleBufferIndex == 1 ? 0 : 1;

            CurrentYPosition += NewDirectionRandomizer();

            if (CurrentYPosition >= _IS31FL3731.Height)
                CurrentYPosition = _IS31FL3731.Height - 1;
            if (CurrentYPosition < 0)
                CurrentYPosition = 0;
            return bytePerSec;
        }
    }

}

