/*
   Copyright (C) 2015 MadeInTheUSB LLC
   Written by FT for MadeInTheUSB
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
  
    Written with the help of
        https://rheingoldheavy.com/mcp3008-tutorial-02-sampling-dc-voltage/
  
    Mcp300X 10bit ADC Breakout Board from RheinGoldHeavy.com supported
    https://rheingoldheavy.com/product/breakout-board-mcp3008/
    
    Datasheet http://www.adafruit.com/datasheets/Mcp300X.pdf
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
using MadeInTheUSB.spi;

namespace MadeInTheUSB
{
    /// <summary>
    /// Base class to support analog to digital converters:
    /// MCP3008 : 8 AD
    /// MCP3004 : 4 AD
    /// </summary>
    public class MCP300X_Base
    {
        public SPIEngine _spiEngine;

        public int MaxAdConverterConverter = 8;

        /// <summary>
        /// The 8 analog to digital (AD) channels/ports configured in single mode.
        /// Differential mode is not implemented.
        /// </summary>
        private List<int> _channelInSingleMode = new List<int>() {
            0x08,
            0x09,
            0x0A,
            0x0B,
            0x0C,
            0x0D,
            0x0E,
            0x0F
        };
        
        public MCP300X_Base(int maxADConverter, Nusbio nusbio, NusbioGpio selectGpio, NusbioGpio mosiGpio, 
                                                NusbioGpio misoGpio, NusbioGpio clockGpio) 
        {
            this._spiEngine              = new SPIEngine(nusbio, selectGpio, mosiGpio, misoGpio, clockGpio);
            this.MaxAdConverterConverter = maxADConverter;
        }

        public void Begin()
        {
            _spiEngine.Begin();
        }

        /// <summary>
        /// Read the value of one analog port using software bit banging.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public int ReadSpiSoftware(int port)
        {
            this._spiEngine.Nusbio.SetPinMode(this._spiEngine.MisoGpio, PinMode.Input);
            int adcValue = 0;
            int command  = WinUtil.BitUtil.ParseBinary("B11000000");
            command |= (port) << 3;
            var nusbio   = this._spiEngine.Nusbio;

            this._spiEngine.Select();
            
            for (int i = 7; i >= 4; i--)
            {
                var r = command & (1 << i);
                nusbio[this._spiEngine.MosiGpio].DigitalWrite(r != 0);  
    
                nusbio[this._spiEngine.ClockGpio].DigitalWrite(true);
                nusbio[this._spiEngine.ClockGpio].DigitalWrite(false);    
            }
             
            nusbio[this._spiEngine.ClockGpio].DigitalWrite(true); // ignores 2 null bits
            nusbio[this._spiEngine.ClockGpio].DigitalWrite(false);    

            nusbio[this._spiEngine.ClockGpio].DigitalWrite(true);
            nusbio[this._spiEngine.ClockGpio].DigitalWrite(false);    
            
            for(var i = 10; i > 0; i--) { // Read bits from adc since it is ADC is 10 bits
                
                adcValue +=  Nusbio.ConvertTo1Or0(nusbio[this._spiEngine.MisoGpio].DigitalRead()) << i;
    
                nusbio[this._spiEngine.ClockGpio].DigitalWrite(true);
                nusbio[this._spiEngine.ClockGpio].DigitalWrite(false);    
            }
            this._spiEngine.Unselect();
            return adcValue;
        }

        /// <summary>
        /// Read the value of one analog port using Nusbio spi/hardware acceleration.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public int Read(int port)
        {
            if ((port > 7) || (port < 0))
                throw new ArgumentException(string.Format("Invalid analog port {0}", port));
            
            const byte junk = (byte)0;
            var port2       = (byte)((_channelInSingleMode[port] << 4));
            var r1          = this._spiEngine.Transfer(new List<Byte>() { 0x1, port2, junk });

            return ValidateOperation(r1);
        }

        private int ValidateOperation(SPIResult result)
        {
            if (result.Succeeded && result.Buffer.Count == 3)
            {
                //ConsoleEx.WriteLine(0, 15, string.Format("B0:{0:000}, B1:{1:000}, B2:{2:000}",
                //    result.Buffer[0], result.Buffer[1], result.Buffer[2]), ConsoleColor.Yellow);
                int r = 0;
                if (WinUtil.BitUtil.IsSet(result.Buffer[1], 1))
                    r += 256;
                if (WinUtil.BitUtil.IsSet(result.Buffer[1], 2))
                    r += 512;
                r += result.Buffer[2];

                var rr = (result.Buffer[1] & 0x3) << 8 | result.Buffer[2];
                return r;
            }
            else return -1;
        }
    }
    
    public class MCP3008 : MCP300X_Base
    {
        public MCP3008(Nusbio nusbio, NusbioGpio selectGpio, NusbioGpio mosiGpio, NusbioGpio misoGpio, NusbioGpio clockGpio)
        : base(8, nusbio, selectGpio, mosiGpio, misoGpio,clockGpio)
        {
        }
    }

    public class MCP3004 : MCP300X_Base
    {
        public MCP3004(Nusbio nusbio, NusbioGpio selectGpio, NusbioGpio mosiGpio, NusbioGpio misoGpio, NusbioGpio clockGpio)
        : base(4, nusbio, selectGpio, mosiGpio, misoGpio,clockGpio)
        {
        }
    }
}

