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
    public class McpGpio : GpioPublicApiBase
    {
        MCP230XX_Base _mcp;

        public McpGpio(MCP230XX_Base mcp, string name)
        {
            this._mcp  = mcp;
            this.Mode  = PinMode.Output;
            this.Name  = name;
            this.Index = McpGpio.ExtractGpioIndex(name);
        }

        public string Name       { get; set; }
        public bool State        { get; set; }
        public byte Index        { get; set; }
        public PinMode Mode      { get; set; }
        public PinState PinState { get { return this.State ? PinState.High : PinState.Low; } set { this.State = value == PinState.High; } }

        PinState GpioPublicApiBase.DigitalRead()
        {
            return this._mcp.DigitalRead(this.Index);
        }

        void GpioPublicApiBase.DigitalWrite(PinState on)
        {
            this._mcp.DigitalWrite(this.Index, on);
        }

        void GpioPublicApiBase.DigitalWrite(bool high)
        {
            this._mcp.DigitalWrite(this.Index, high ? PinState.High : PinState.Low);
        }

        public static byte ExtractGpioIndex(string gpioName)
        {
            return byte.Parse(gpioName.Replace("Gpio", ""));
        }

        public void High()
        {
            ((GpioPublicApiBase)this).DigitalWrite(PinState.High);
        }

        public void Low()
        {
            ((GpioPublicApiBase)this).DigitalWrite(PinState.Low);
        }
    }

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
        protected Nusbio   _nusbio;

        protected byte _gpioStartIndex = 0;

        public byte GpioStartIndex
        {
            get { 
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
            this.SetPinsMode(0xFF);
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
            
            if (mcpIndex > (this.GetMaxGPIO()-1))
                throw new ArgumentException(string.Format("Gpio {0} not available", gpioIndex));

            int iodir = this._i2c.Send1ByteRead1Byte(MCP230XX_IODIR);
            if (iodir == -1)
                throw new ArgumentException(string.Format("Cannot read state of MCP230XX"));
            
            if (mode == GPIO.PinMode.Input)
                iodir |= (byte)(1 << mcpIndex);
            else
                iodir &= (byte)(~(1 << mcpIndex));

            if (!this._i2c.Send2BytesCommand(MCP230XX_IODIR, (byte)iodir))
                throw new ArgumentException(string.Format("Cannot set state of MCP230XX"));
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
            if(!this._i2c.Send2BytesCommand(MCP230XX_IODIR, mask))
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
            if(!this._i2c.Send2BytesCommand(MCP230XX_GPIO, mask))
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

    /// <summary>
    /// MCP23008 implementation
    /// http://www.microchip.com/wwwproducts/Devices.aspx?product=MCP23008
    /// http://ww1.microchip.com/downloads/en/DeviceDoc/21919e.pdf
    /// http://www.mouser.com/ProductDetail/Microchip-Technology/MCP23008-E-P/?qs=sGAEpiMZZMvcAs5GUBtMdVgPLCS8TUZv
    /// </summary>
    public class MCP23008 : MCP230XX_Base,  IDigitalWriteRead
    {
        protected override int GetMaxGPIO()
        {
            return 8;
        }

        public MCP23008(Nusbio nusbio, NusbioGpio sdaPin, NusbioGpio sclPin, int gpioStartIndex = 0)
            : base(nusbio, sdaPin, sclPin)
        {
            this._gpioStartIndex = (byte)gpioStartIndex;

            for (var i = 0; i < this.GetMaxGPIO(); i++)
            {
                base.AddGPIO(MCP230XX_Base.BuildGpioName(this.GpioStartIndex + i), PinMode.Output, true);
            }
        }

        //public void SetGpioMask(byte mask)
        //{
        //    SetGPIOMask(mask);
        //}

        public byte ComputeMask(byte mask, int gpioIndex, MadeInTheUSB.GPIO.PinState d)
        {
            var mcpIndex = gpioIndex - this.GpioStartIndex;

            // only 8 bits!
            if (mcpIndex > 7) 
                return mask;
            
            int gpio = mask;

            // set the pin and direction
            if (d == GPIO.PinState.High)
            {
                gpio |= (byte)(1 << mcpIndex);
            }
            else
            {
                gpio &= (byte)(~(1 << mcpIndex));
            }
            return (byte)gpio;
        }

        public override void DigitalWrite(int gpioIndex, MadeInTheUSB.GPIO.PinState d)
        {
            this.SetGpioMask(ComputeMask(GetGpioMask(), gpioIndex, d));
        }

        public override void SetPullUp(int gpioIndex, MadeInTheUSB.GPIO.PinState d)
        {
            var mcpGpioIndex = gpioIndex - this.GpioStartIndex;

            // only 8 bits!
            if (mcpGpioIndex > 7)
                throw new InvalidGpioOperationException(string.Format("Invalid gpio index:{0}", gpioIndex));

            int gppu = this._i2c.Send1ByteRead1Byte(MCP230XX_GPPU);
            if (gppu == -1)
                throw new InvalidGpioOperationException(string.Format("Command MCP230XX_GPPU({0}) failed gpio index:{1}", MCP230XX_GPPU, gpioIndex));

            if (d == GPIO.PinState.High)
            { // set the pin and direction
                gppu |= (byte)(1 << mcpGpioIndex);
            }
            else
            {
                gppu &= (byte)(~(1 << mcpGpioIndex));
            }
            // write the new GPIO
            this._i2c.Send2BytesCommand(MCP230XX_GPPU, (byte)gppu);
        }

        public override PinState DigitalRead(int gpioIndex)
        {
            var mcpIndex = gpioIndex - this.GpioStartIndex;

            // only 8 bits!
            if (mcpIndex > 7)
                return PinState.Unknown;

            var mask = GetGpioMask();
            var maskB = WinUtil.BitUtil.BitRpr(mask);
            // read the current GPIO
            var v = (byte)((mask >> mcpIndex) & 0x1);

            return v == 0 ? MadeInTheUSB.GPIO.PinState.Low : MadeInTheUSB.GPIO.PinState.High;
        }
    }
}

