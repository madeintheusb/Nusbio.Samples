/*
 
   MCP23008 and MCP230XX I2C Support for the Nusbio
   Copyright (C) 2015 MadeInTheUSB LLC
   Written by FT for MadeInTheUSB
  
   MIT License (MIT)
  
   Datasheet: http://ww1.microchip.com/downloads/en/DeviceDoc/21919b.pdf
 
        MCP23008 - I2C
        MCP23S08 - SPI
  
  For more info in the MCP23008 see
     - Expanding the number of I/O lines using Microchip MCP23008 - http://embedded-lab.com/blog/?p=2834
 
   Copyright (C) 2015 MadeInTheUSB LLC
    
   This code is based from Adafruit-MCP23008-library
   https://www.adafruit.com/product/593
   https://github.com/adafruit/Adafruit-MCP23008-library

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;

using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;

namespace MadeInTheUSB
{

    public class MCP230XX_Base
    {
        protected const byte MCP230XX_IODIR   = 0x00;
        protected const byte MCP230XX_IPOL    = 0x01;
        protected const byte MCP230XX_GPINTEN = 0x02;
        protected const byte MCP230XX_DEFVAL  = 0x03;
        protected const byte MCP230XX_INTCON  = 0x04;
        protected const byte MCP230XX_IOCON   = 0x05;
        protected const byte MCP230XX_GPPU    = 0x06;
        protected const byte MCP230XX_INTF    = 0x07;
        protected const byte MCP230XX_INTCAP  = 0x08;
        protected const byte MCP230XX_GPIO    = 0x09;
        protected const byte MCP230XX_OLAT    = 0x0A;

        private const byte MCP23XXX_DEFAULT_I2C_ADDRESS = 0x20;

        protected I2CEngine _i2c;
        protected Nusbio _nusbio;

        protected byte _gpioStartIndex = 0;

        public byte GpioStartIndex
        {
            get
            {
                return _gpioStartIndex;
            }
        }

        public Dictionary<string, GpioPublicApiBase> GPIOS;

        public MCP230XX_Base(Nusbio nusbio, NusbioGpio sdaOutPin, NusbioGpio sclPin)
        {
            this._nusbio = nusbio;
            this._i2c = new I2CEngine(nusbio, sdaOutPin, sclPin, 0);
            this.GPIOS = new Dictionary<string, GpioPublicApiBase>();
        }

        public virtual void Begin(byte addr = MCP23XXX_DEFAULT_I2C_ADDRESS)
        {
            if (this._i2c.DeviceId == 0)
                this._i2c.DeviceId = (byte)(addr);

            if (this.Reset())
            {
                for (var i = 0; i < this.GetMaxGPIO(); i++)
                {
                    this.SetPinMode(this.GpioStartIndex + i, PinMode.Output);
                    this.DigitalWrite(this.GpioStartIndex + i, PinState.Low);
                }
            }
            else
                throw new ArgumentException(string.Format("Cannot reset {0}", this.GetType().FullName));
        }

        public static string BuildGpioName(int index)
        {
            return "Gpio" + (index);
        }

        protected virtual int GetMaxGPIO()
        {
            return 0;
        }

        public bool Reset()
        {
            this.SetPinsMode(0x00); // All pin in output mode
            return true;
        }

        public void SetPinMode(string gpio, MadeInTheUSB.GPIO.PinMode mode)
        {
            SetPinMode(McpGpio.ExtractGpioIndex(gpio), mode);
        }

        public void SetPinMode(int p, MadeInTheUSB.GPIO.PinMode mode)
        {
            SetPinMode((byte)p, mode);
        }

        public void SetPinMode(byte gpioIndex, MadeInTheUSB.GPIO.PinMode mode)
        {
            var k = MCP230XX_Base.BuildGpioName(gpioIndex);
            if (this.GPIOS.ContainsKey(k))
                this.GPIOS[k].Mode = mode;
            else
                throw new ArgumentException(string.Format("Gpio {0} not available", gpioIndex));

            var mcpIndex = gpioIndex - this.GpioStartIndex;

            if (mcpIndex > (this.GetMaxGPIO() - 1))
                throw new ArgumentException(string.Format("Gpio {0} not available", gpioIndex));

            int iodir = this._i2c.Send1ByteRead1Byte(MCP230XX_IODIR);
            if (iodir == -1)
                throw new ArgumentException(string.Format("Cannot read state of MCP230XX"));

            if (mode == GPIO.PinMode.Input || mode == GPIO.PinMode.InputPullUp)
                iodir |= (byte)(1 << mcpIndex);
            else
                iodir &= (byte)(~(1 << mcpIndex));

            if (!this._i2c.Send2BytesCommand(MCP230XX_IODIR, (byte)iodir))
                throw new ArgumentException(string.Format("Cannot set state of MCP230XX"));

            if (mode == GPIO.PinMode.InputPullUp)
                this.SetPullUp(gpioIndex, PinState.High);
            if (mode == GPIO.PinMode.Input)
                this.SetPullUp(gpioIndex, PinState.Low);
        }

        public int GetPinsMode()
        {
            int iodir = this._i2c.Send1ByteRead1Byte(MCP230XX_IODIR);
            return iodir;
        }

        public int GetPullUpMode()
        {
            int m = this._i2c.Send1ByteRead1Byte(MCP230XX_GPPU);
            return m;
        }

        internal void SetPinsMode(byte mask)
        {
            if (!this._i2c.Send2BytesCommand(MCP230XX_IODIR, mask))
                throw new ArgumentException(string.Format("Cannot set state of MCP230XX"));
        }

        public string GetGpioMaskAsBinary()
        {
            return WinUtil.BitUtil.BitRpr(GetGpioMask());
        }

        public byte GetGpioMask(bool forceToRead = false)
        {
            // read the current status of the GPIO pins
            return (byte)this._i2c.Send1ByteRead1Byte(MCP230XX_GPIO);
        }

        public void SetGpioMask(byte mask)
        {
            if (!this._i2c.Send2BytesCommand(MCP230XX_GPIO, mask))
                throw new InvalidGpioOperationException(string.Format("Cannot set gpio mask:{0}", mask));
        }

        public void AddGPIO(string gpio, PinMode mode, bool removeIfExist = false)
        {
            if (removeIfExist && this.GPIOS.ContainsKey(gpio))
                this.GPIOS.Remove(gpio);

            this.GPIOS.Add(gpio, new McpGpio(this, gpio));
        }

        public void AllOff()
        {
            foreach (var g in this.GPIOS.Values)
            {
                if (g.Mode == GPIO.PinMode.Output)
                {
                    var index = int.Parse(g.Name.Replace("Gpio", "")); // TODO: Improve
                    this.DigitalWrite((byte)index, GPIO.PinState.Low);
                }
            }
        }

        public virtual void DigitalWrite(int p, MadeInTheUSB.GPIO.PinState d)
        {

        }

        public virtual void SetPullUp(int p, MadeInTheUSB.GPIO.PinState d)
        {

        }

        public virtual PinState DigitalRead(int p)
        {
            return PinState.Unknown;
        }
    }
}