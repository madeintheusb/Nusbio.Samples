/*
   Copyright (C) 2015, 2016 MadeInTheUSB LLC
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
*/

using MadeInTheUSB.WinUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MadeInTheUSB.EEPROM
{
    /// <summary>
    /// The EEPROM AT28C16 is a 2k eeprom with a paralel 10 bit address bus
    /// and a 8 bit data bus. This EEPROM is easy to control with projec with no
    /// MCU. This class is used to upload data into the EEPROM.
    /// For now we only set the address but to 4 bit and can only program the first 
    /// 16 byte
    /// https://www.youtube.com/watch?v=BA12Z7gQ4P0
    /// http://www.mouser.com/ds/2/268/doc0540-1065592.pdf
    /// </summary>
    public class EEPROM_AT28C16
    {

        List<NusbioGpio> _addressLines;

        /// <summary>
        /// Active low
        /// </summary>
        NusbioGpio _writeEnable;
        /// <summary>
        /// Active low
        /// </summary>
        NusbioGpio _outputEnable;
        Nusbio _nusbio;

        ShiftRegister74HC595 _sr = null;
        /// <summary>
        /// The gpio 8 from Shift Register is view as gpio0 for the 8 possible
        /// address bit. Note we only use 5 bit address for now 0..32
        /// </summary>
        const int ShiftRegisterAddressGpioStart = 8;

        public EEPROM_AT28C16(Nusbio nusbio, 
            ShiftRegister74HC595 sr, 
            NusbioGpio writeEnable,
            NusbioGpio outputEnable)
        {
            this._writeEnable  = writeEnable;
            this._outputEnable = outputEnable;
            this._nusbio       = nusbio;
            this._sr           = sr;

            // Just used as a place holder we will really use gpio 8,9,10,11,12
            this._addressLines = new List<NusbioGpio>() { NusbioGpio.Gpio0, NusbioGpio.Gpio1, NusbioGpio.Gpio2, NusbioGpio.Gpio3, NusbioGpio.Gpio4 };

            _nusbio[this._writeEnable].High(); // Disable
            _nusbio[this._outputEnable].Low(); // Enable            
        }

        public EEPROM_AT28C16(Nusbio nusbio,
            List<NusbioGpio> addressLines,
            NusbioGpio writeEnable,
            NusbioGpio outputEnable) 
        {
            this._addressLines = addressLines;
            this._writeEnable  = writeEnable;
            this._outputEnable = outputEnable;
            this._nusbio       = nusbio;

            _nusbio[this._outputEnable].Low(); // Enable
            _nusbio[this._writeEnable].High(); // Disable            
        }

        public void EnableOutput(bool state)
        {
            _nusbio[this._outputEnable].DigitalWrite(!state);            
        }

        public void Read(int addr)
        {
            SetAddress(addr);
            EnableOutput(true);
        }

        public void SetAddress(int addr)
        {
            if (this._sr == null)
                SetAddressWithNusbio(addr);
            else
                this._sr.SetDataLinesAndAddrLines(0, (byte)addr);
        }

        private void SetAddressWithShiftRegister(int addr)
        {
            for (var i = 0; i < this._addressLines.Count; i++)
            {
                var bit = 1 << i;
                var turnBitOnOrOff = (addr & bit) == bit;
                _sr.DigitalWrite(ShiftRegisterAddressGpioStart + i, turnBitOnOrOff ? GPIO.PinState.High : GPIO.PinState.Low);
            }
        }

        private void SetAddressWithNusbio(int addr)
        {
            //for (var i = 0; i < this._addressLines.Count; i++)
            //{
            //    var bit            = 1 << i;
            //    var turnBitOnOrOff = (addr & bit) == bit;
            //    _nusbio[this._addressLines[i]].DigitalWrite(turnBitOnOrOff);
            //}
            var mask = _nusbio.GetGpioMask();
            for (var i = 0; i < this._addressLines.Count; i++)
            {
                var bit = 1 << i;
                var turnBitOnOrOff = (addr & bit) == bit;
                if(turnBitOnOrOff)
                    mask = BitUtil.SetBit(mask, (_nusbio[this._addressLines[i]] as GPIO.Gpio).Bit);
                else
                    mask = BitUtil.UnsetBit(mask, (_nusbio[this._addressLines[i]] as GPIO.Gpio).Bit);
            }
            _nusbio.SetGpioMask(mask);
        }

        public void Write(int addr, int data)
        {
            if (this._sr != null)
                this._sr.SetDataLinesAndAddrLines((byte)data, (byte)addr);
            else
                this.SetAddress(addr);            
            Write();
        }

        /// <summary>
        /// Write the value of the data bus at the address set in the address bus.
        /// </summary>
        private void Write()
        {
            //EnableOutput(false);
            _nusbio[this._writeEnable].Low(); // Enable
            //Thread.Sleep(0);
            _nusbio[this._writeEnable].High(); // Disable

            //Thread.Sleep(10);
            //EnableOutput(true);
            Thread.Sleep(15);
        }
    }
}














//TimePeriod.__SleepMicro(300);