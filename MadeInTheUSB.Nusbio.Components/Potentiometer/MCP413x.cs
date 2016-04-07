/*
   Copyright (C) 2015 MadeInTheUSB LLC
   Written by FT for MadeInTheUSB
  
   Written with the help of :
  
       crossExerciseBench/Libraries/MCP4131/ 
       https://github.com/threldor/crossExerciseBench/tree/master/Libraries/MCP4131
  
       jmalloc/arduino-mcp4xxx
       https://github.com/jmalloc/arduino-mcp4xxx/blob/master/mcp4xxx.cpp

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
using MadeInTheUSB.spi;

namespace MadeInTheUSB
{
    /// <summary>
    /// Microship datasheet http://ww1.microchip.com/downloads/en/DeviceDoc/22060b.pdf
    /// arduino-mcp4xxx https://github.com/jmalloc/arduino-mcp4xxx
    /// </summary>
    public class MCP41X1_Base
    {
        SPIEngine _spiEngine;

        const byte ADDRESS_MASK         = 240; // 11110000
        const byte COMMAND_MASK         = 12;  // 00001100
        const byte CMDERR_MASK          = 2;   // 00000010
        const byte DATA_MASK            = 1;   // 00000001
        const ushort DATA_MASK_WORD     = 511; // 0x01FF a & with this mask only keep bit 0..10

        const byte TCON_SHUTDOWN_MASK   = 8;   // 1000
        const byte TCON_TERM_A_MASK     = 4;   // 0100
        const byte TCON_WIPER_MASK      = 2;   // 0010
        const byte TCON_TERM_B_MASK     = 1;   // 0001

        const byte BLANK_TRANSFER_BYTE  = 0;

        const ushort STATUS_SHUTDOWN_MASK = 2; // 10

        public enum ADDRESS : int
        {
            POT_0      = 0, // B0000
            POT_1      = 1, // B0001
            TCON       = 4, // B0100
            STATUS     = 5, // B0101
            UNDEFINED  = -1
        };

        public enum COMMAND : int
        {
           WRITE      = 0, // B00
           READ       = 3, // B11
           INCREMENT  = 1, // B01
           DECREMENT  = 2, // B10
        };
        
        public int MaxDigitalValue;
        public int MinDigitalValue;
        public int MaxResistance;
        public int DigitalValue;
        public double Voltage { get; private set;}

        public MCP41X1_Base(Nusbio nusbio, NusbioGpio selectGpio, NusbioGpio mosiGpio, NusbioGpio misoGpio, NusbioGpio clockGpio, NusbioGpio resetGpio = NusbioGpio.None, bool debug = false) {

            this._spiEngine      = new SPIEngine(nusbio, selectGpio, mosiGpio, misoGpio, clockGpio, resetGpio, debug);
            this.Voltage         = Nusbio.Voltage; // Nusbio is a 5 volt device
            this.MaxDigitalValue = 128;
            this.MinDigitalValue = 0;
        }

        public void Begin()
        {
            _spiEngine.Begin();
            this.Set(0, ADDRESS.POT_0);
            this.Set(0, ADDRESS.POT_1);
            //this.Increment(1, ADDRESS.POT_1);
            //this.Increment(1, ADDRESS.POT_0);
            //this.Decrement(1, ADDRESS.POT_1);
            //this.Decrement(1, ADDRESS.POT_0);
        }

        private bool CheckErrorCode(byte data)
        {
            // Bit CMDERR_MASK must be 0 for ok
            return ! WinUtil.BitUtil.IsSet(data, CMDERR_MASK);
        }

        public MadeInTheUSB.spi.SPIEngine.SPIResult Increment(int times = 1, ADDRESS pot = ADDRESS.POT_0)
        {
            var r = new MadeInTheUSB.spi.SPIEngine.SPIResult();
            if (times > 1) {

                for (var i = 0; i < times; i++)
                {
                    r = this.Increment(1);
                }
            }
            else { 
                if(this.DigitalValue < this.MaxDigitalValue + 1) 
                {
                    this.DigitalValue++;
                    this.Transfer(pot, COMMAND.INCREMENT);
                }
            }
            return r;
        }

        public MadeInTheUSB.spi.SPIEngine.SPIResult Decrement(int times = 1, ADDRESS pot = ADDRESS.POT_0)
        {
            var r = new MadeInTheUSB.spi.SPIEngine.SPIResult();
            if (times > 1) {

                for (var i = 0; i < times; i++)
                {
                    r = this.Decrement(1);
                }
            }
            else { 
                if(this.DigitalValue > this.MinDigitalValue )
                {
                    this.DigitalValue--;
                    this.Transfer(pot, COMMAND.DECREMENT);
                }
            }
            return r;
        }

        public MadeInTheUSB.spi.SPIEngine.SPIResult Set(int digitalValue, ADDRESS pot = ADDRESS.POT_0)
        {
            try { 
                this._spiEngine.Select();

                var r = new MadeInTheUSB.spi.SPIEngine.SPIResult();
                if((digitalValue < this.MinDigitalValue)||(digitalValue > this.MaxDigitalValue))
                    return r;
                this.DigitalValue = digitalValue;
                r = Transfer(pot, COMMAND.WRITE, digitalValue);
            
                //// Verify that the command error flag is off
                //var b0         = r.ReadBuffer[0];
                //bool valid     = !WinUtil.BitUtil.IsSet(b0, CMDERR_MASK);
                //r.Succeeded = r.Succeeded && valid;

                return r;
            }
            finally { 
                this._spiEngine.Unselect();
            }
        }

        //public int Get(ADDRESS pot = ADDRESS.POT_0)
        //{
        //    try
        //    {
        //        this._spiEngine.Select();

        //        int result     = -1;
        //        var cmd        = build_command(pot, COMMAND.READ);
        //        var r          = _spiEngine.Transfer(cmd);
        //        var b0         = r.ReadBuffer[0];
        //        bool valid     = WinUtil.BitUtil.IsSet(b0, CMDERR_MASK);
            
        //        if (valid)
        //        {
        //            var dataByte = build_data(0);
        //            var r2  = _spiEngine.Transfer(dataByte);
        //            result  = r2.ReadBuffer[0];
        //        }
        //        return result;
        //    }
        //    finally { 
        //        this._spiEngine.Unselect();
        //    }
        //}

        public int Get(ADDRESS pot = ADDRESS.POT_0)
        {
            try { 
                this._spiEngine.Select();
                var r = new MadeInTheUSB.spi.SPIEngine.SPIResult();
                r = Transfer(pot, COMMAND.READ, 0);
                return r.Succeeded ? r.Value : -1;
            }
            finally { 
                this._spiEngine.Unselect();
            }
        }

        public bool ValidateOperation(MadeInTheUSB.spi.SPIEngine.SPIResult result, ADDRESS address, COMMAND command, int? value = null)
        {
            // We cannot read an answer from the MCP4131. MISO and MOSI pins are combined
            // and we cannot make it work. So always return true
            if(this.GetType().Name == "MCP4131")
                return true;

            var r = false;
            // See datasheet http://exploringarduino.com/wp-content/uploads/2013/06/MCP4231-datasheet.pdf
            // Page 48
            //   Value Function MOSI (SDI pin) MISO (SDO pin) (2)
            //   00h Volatile Wiper 0 Write Data nn nnnn nnnn 0000 00nn nnnn nnnn 1111 1111 1111 1111
            //   Read Data nn nnnn nnnn 0000 11nn nnnn nnnn 1111 111n nnnn nnnn
            //   Increment Wiper — 0000 0100 1111 1111
            //   Decrement Wiper — 0000 1000 1111 1111
            //   01h Volatile Wiper 1 Write Data nn nnnn nnnn 0001 00nn nnnn nnnn 1111 1111 1111 1111
            //   Read Data nn nnnn nnnn 0001 11nn nnnn nnnn 1111 111n nnnn nnnn
            //   Increment Wiper — 0001 0100 1111 1111
            //   Decrement Wiper — 0001 1000 1111 1111
            //   02h Reserved — — — —
            //   03h Reserved — — — —
            //   04h Volatile
            //   TCON Register
            //   Write Data nn nnnn nnnn 0100 00nn nnnn nnnn 1111 1111 1111 1111
            //   Read Data nn nnnn nnnn 0100 11nn nnnn nnnn 1111 111n nnnn nnnn
            //   05h Status Register Read Data nn nnnn nnnn 0101 11nn nnnn nnnn 1111 111n nnnn nnnn
            //   06h-0Fh Reserved — — — —
            //   Note 1: The Data Memory is only 9-bits wide, so the MSb is ignored by the device.
            //   2: All these Address/Command combinations are valid, so the CMDERR bit is s
            switch (command)
            {
                case COMMAND.WRITE     : r = (result.ReadBuffer.Count == 2) && (result.ReadBuffer[0] == 255) && result.ReadBuffer[1] == 255; break;
                case COMMAND.DECREMENT :
                case COMMAND.INCREMENT : r = (result.ReadBuffer.Count == 1) && (result.ReadBuffer[0] == 255); break;
                case COMMAND.READ      : 
                    r = (result.ReadBuffer.Count == 2) && (result.ReadBuffer[0] >= 254); 
                    if(r)
                        result.Value = result.ReadBuffer[1];
                    break;
            }
            return r;
        }

        private MadeInTheUSB.spi.SPIEngine.SPIResult Transfer(ADDRESS address, COMMAND command, int? value = null)
        {
            var l = new List<byte>();
            if(value.HasValue) {
                l.Add(build_command(address, command));
                l.Add(((byte)(value & DATA_MASK_WORD)));  
            }
            else {
                l.Add(build_command(address, command));
            }
            var r = _spiEngine.Transfer(l);
            if(r.Succeeded)
                r.Succeeded = this.ValidateOperation(r, address, command, value);
            return r;
        }

        byte build_command(ADDRESS address, COMMAND command) 
        {
            var r = ((((int)address) << 4) & ADDRESS_MASK) | ((((int)command) << 2) & COMMAND_MASK) | CMDERR_MASK;
            return (byte)r;
        }

        byte build_data(byte data)
        {
            int i = data & DATA_MASK_WORD;
            return (byte)i;
        }

        /// <summary>
        /// Return a 2 byte (16 bits int)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="command"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        ushort build_command(ADDRESS address, COMMAND command, ushort data)
        {
            int i = (build_command(address, command) << 8) | (data & DATA_MASK_WORD);
            return (ushort)i;
        }

        public int OhmPerStep
        {
            get
            {
                return this.MaxResistance / this.MaxDigitalValue;
            }
        }

        public double Resistance
        {
            get
            {
                return this.MaxResistance - (this.MaxResistance / this.MaxDigitalValue * this.DigitalValue);
            }
        }

        public double Amp
        {
            get
            {
                return this.Voltage * 1000.0 / this.Resistance;
            }
        }

        public double Power
        {
            get
            {
                return this.Voltage * this.Amp;
            }
        }

        public override string ToString()
        {
            return string.Format("Val:{0:000}, Ohm/Step:{1}, Res:{2:00000}, Volt:{3:0.00}, mAmp:{4:000.00}, mWatt:{5:0000.00}",
                                this.DigitalValue, this.OhmPerStep, this.Resistance, this.Voltage, this.Amp, this.Power
                                );
        }
    }
    /*
     
    MCP4131
      
    CS(SELECT)    [    ] VDD(POWER)
    SCK(CLOCK)    [    ] SDO(POTENTIOMETER-GND)
    SDI/SDO(MOSI) [    ] P0B(POTENTIOMETER-OUTPUT)
    VSS(GND)      [    ] P0W(POTENTIOMETER-POWER)
     
    */
    /// <summary>
    /// 10k digital potentiometer, 7 bits (there is also 8 bits version)
    /// </summary>
    public class MCP4131 : MCP41X1_Base
    {
        public MCP4131(Nusbio nusbio, NusbioGpio selectGpio, NusbioGpio mosiGpio, NusbioGpio misoGpio, NusbioGpio clockGpio, bool debug = false)
        : base(nusbio, selectGpio, mosiGpio, 

            // Per datasheet
            // On the MCP41X1 devices, pin-out limitations do not allow for individual SDI and SDO pins. 
            // On these devices, the SDI and SDO pins are multiplexed. 
            // Therefore MISO = MOSI
            misoGpio,

            clockGpio, NusbioGpio.None, debug )
        {
            base.MaxResistance   = 10000; // 10k Ohm
        }
    }
    /// <summary>
    /// 100k digital potentiometer, 7 Bits
    /// </summary>
    public class MCP4132 : MCP41X1_Base
    {
        public MCP4132(Nusbio nusbio, NusbioGpio selectGpio, NusbioGpio mosiGpio, NusbioGpio misoGpio, NusbioGpio clockGpio, bool debug = false)
        : base(nusbio, selectGpio, mosiGpio, 
            misoGpio,
            clockGpio, NusbioGpio.None, debug )
        {
            base.MaxResistance   = 100000; // 100k Ohm
        }
    }

    /// <summary>
    /// http://exploringarduino.com/wp-content/uploads/2013/06/MCP4231-datasheet.pdf
    /// http://www.learningaboutelectronics.com/Articles/MCP4231-dual-digital-potentiometer-circuit.php
    /// </summary>
    public class MCP4231 : MCP41X1_Base
    {
        public MCP4231(Nusbio nusbio, NusbioGpio selectGpio, NusbioGpio mosiGpio, NusbioGpio misoGpio, NusbioGpio clockGpio, bool debug = false)
        : base(nusbio, selectGpio, mosiGpio, 

            // Per datasheet
            // On the MCP41X1 devices, pin-out limitations do not allow for individual SDI and SDO pins. 
            // On these devices, the SDI and SDO pins are multiplexed. 
            // Therefore MISO = MOSI
            misoGpio,

            clockGpio, NusbioGpio.None, debug )
        {
            base.MaxResistance   = 10000; // 10k Ohm
        }

        public MadeInTheUSB.spi.SPIEngine.SPIResult SetAll(int digitalValue)
        {
            var r0 = base.Set(digitalValue, ADDRESS.POT_0);
            var r1 = base.Set(digitalValue, ADDRESS.POT_1);
            return r1;
        }
        public MadeInTheUSB.spi.SPIEngine.SPIResult Set(int digitalValue0, int digitalValue1)
        {
            var r0 = base.Set(digitalValue0, ADDRESS.POT_0);
            var r1 = base.Set(digitalValue1, ADDRESS.POT_1);
            return r1;
        }
    }
}

