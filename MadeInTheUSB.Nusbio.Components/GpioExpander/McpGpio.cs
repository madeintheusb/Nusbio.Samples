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
            this._mcp = mcp;
            this.Mode = PinMode.Output;
            this.Name = name;
            this.Index = McpGpio.ExtractGpioIndex(name);
        }

        public string Name { get; set; }
        public bool State { get; set; }
        public byte Index { get; set; }
        public PinMode Mode { get; set; }
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
}