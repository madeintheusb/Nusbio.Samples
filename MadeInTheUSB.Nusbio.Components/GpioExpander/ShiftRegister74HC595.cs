/*
   Copyright (C) 2015 MadeInTheUSB LLC
   Written by FT for MadeInTheUSB

   Port of the wiring_shift.c - shiftOut() function
   Part of Arduino - http://www.arduino.cc/
   Copyright (c) 2005-2006 David A. Mellis

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
using System.Threading;
using System.Threading.Tasks;
using MadeInTheUSB.GPIO;

namespace MadeInTheUSB
{
    public class ShiftRegister74HC595 : IDigitalWriteRead
    {
        private const int LSBFIRST = 0;
        private const int MSBFIRST = 1;

        [Flags]
        public enum ExGpio : ushort
        {
            None   = 0,
            Gpio8  = 1,
            Gpio9  = 2,
            Gpio10 = 4,
            Gpio11 = 8,
            Gpio12 = 16,
            Gpio13 = 32,
            Gpio14 = 64,
            Gpio15 = 128,
            Gpio16 = 256,
            Gpio17 = 512,
            Gpio18 = 1024,
            Gpio19 = 2048,
            Gpio20 = 4096,
            Gpio21 = 8192,
            Gpio22 = 16384,
            Gpio23 = 32768
        };

        public ExGpio GpioStates = ExGpio.None;

        /// <summary>
        /// Since this class can only works with Nusbio v 1 first 8 Gpios.
        /// We consider that the first extended gpio will be 8
        /// </summary>
        public int MinGpioIndex = 8;
        public int MaxGpioIndex = 23;
        /// <summary>
        /// Return 16 by default
        /// </summary>
        public int MaxGpio
        {
            get
            {
                return MaxGpioIndex - MinGpioIndex + 1;
            }
        }
                
        public List<ExGpio> ExGpios = new List<ExGpio>()
        {
            ExGpio.Gpio8, ExGpio.Gpio9,ExGpio.Gpio10,ExGpio.Gpio11,ExGpio.Gpio12,ExGpio.Gpio13,ExGpio.Gpio14,ExGpio.Gpio15,
            ExGpio.Gpio16,ExGpio.Gpio17,ExGpio.Gpio18,ExGpio.Gpio19, ExGpio.Gpio20,ExGpio.Gpio21,ExGpio.Gpio22,ExGpio.Gpio23
        };

        public ExGpio GetGpioFromIndex(int gpioIndex)
        {
            if (gpioIndex >= MinGpioIndex && gpioIndex <= MaxGpioIndex)
            {
                return ExGpios[gpioIndex - MinGpioIndex];
            }
            else throw new ArgumentException(string.Format("Invalid gpio index:{0}", gpioIndex));
        }

        public List<ExGpio> GetGpioEnums()
        {
            return ExGpios;
        }

        /// <summary>
        /// Use Nusbio hardware accelerated bit banging technology
        /// </summary>
        private GpioSequence _gs = new GpioSequence(0);

        private NusbioGpio _dataPin;
        private NusbioGpio _latchPin;
        private NusbioGpio _clockPin;
        private Nusbio     _nusbio;


        public ShiftRegister74HC595(Nusbio nusbio, int maxGpio, NusbioGpio dataPin, NusbioGpio latchPin, NusbioGpio clockPin)
        {
            this._dataPin  = dataPin;
            this._latchPin = latchPin;
            this._clockPin = clockPin;
            this._nusbio   = nusbio;

            this.SetPinDefaultState();

            // To handle 8bit shift registers remove the last gpio
            // from 15 to 23
            while (ExGpios.Count > maxGpio)
            {
                ExGpios.RemoveAt(ExGpios.Count - 1);
            }
        }

        private void ShiftOutFast(Nusbio nusbio, NusbioGpio dataPin, NusbioGpio clockPin, byte val, MadeInTheUSB.GPIO.BitOrder bitOrder = MadeInTheUSB.GPIO.BitOrder.MSBFIRST)
        {
            _gs.ShiftOut(nusbio, dataPin, clockPin, val, bitOrder, false, false);            
        }

        private void ShiftOutSoftware(Nusbio nusbio, NusbioGpio dataPin , NusbioGpio clockPin , int bitOrder, byte val)
        {
            int i;
            
	        for (i = 0; i < 8; i++)
	        {
	            if (bitOrder == LSBFIRST)
	            {
	                var a = (val & (1 << i));
	                nusbio.GPIOS[dataPin].DigitalWrite(Nusbio.ConvertToPinState(a));
	            }
	            else
	            {
	                var b = (val & (1 << (7 - i)));
	                nusbio.GPIOS[dataPin].DigitalWrite(Nusbio.ConvertToPinState(b));
	            }
	            nusbio.GPIOS[clockPin].DigitalWrite(PinState.High);
                nusbio.GPIOS[clockPin].DigitalWrite(PinState.Low);
	        }   
        }

        /// <summary>
        /// Should be tested with 74CH165
        /// </summary>
        /// <param name="nusbio"></param>
        /// <param name="dataPin"></param>
        /// <param name="clockPin"></param>
        /// <param name="bitOrder"></param>
        /// <returns></returns>
        public static int ShiftIn(Nusbio nusbio,  NusbioGpio dataPin, NusbioGpio clockPin, int bitOrder) {

	        int value = 0;
	        int i;

	        for (i = 0; i < 8; ++i)
            {
                nusbio.GPIOS[clockPin].DigitalWrite(PinState.High);
		        if (bitOrder == LSBFIRST)
			        value |= Nusbio.ConvertTo1Or0(nusbio.GPIOS[dataPin].DigitalRead()) << i;
		        else
			        value |= Nusbio.ConvertTo1Or0(nusbio.GPIOS[dataPin].DigitalRead()) << (7 - i);
                nusbio.GPIOS[clockPin].DigitalWrite(PinState.Low);
	        }
	        return value;
        }

        public void SetGpioMask(ExGpio v)
        {
            SetGpioMask((int)v);
        }

        public void SetGpioMask(byte mask)
        {
            this.SetGpioMask((int)mask);
        }

        public void SetDataLinesAndAddrLines(byte dataBus8Bit, byte addresseBus8Bit)
        {
            SetGpioMask(dataBus8Bit, addresseBus8Bit);
        }

        public void SetGpioMask(byte b0, byte b1)
        {
            int v = (b0 << 8) + b1;
            SetGpioMask(v);
        }

        public void Reset(int val = 0, int times = 10)
        {
            for (var i = 0; i < times; i++)
                SetGpioMask(val);
        }

        public void TestPins()
        {
            this._nusbio.GPIOS[_latchPin].DigitalWrite(PinState.Low);
            this._nusbio.GPIOS[_clockPin].DigitalWrite(PinState.Low);
            this._nusbio.GPIOS[_dataPin].DigitalWrite(PinState.Low);

            this._nusbio.GPIOS[_latchPin].DigitalWrite(PinState.High);
            this._nusbio.GPIOS[_clockPin].DigitalWrite(PinState.High);
            this._nusbio.GPIOS[_dataPin].DigitalWrite(PinState.High);

            SetPinDefaultState();
        }

        private void SetPinDefaultState()
        {
            this._nusbio.GPIOS[_dataPin].DigitalWrite(PinState.Low);
            this._nusbio.GPIOS[_clockPin].DigitalWrite(PinState.Low);
            this._nusbio.GPIOS[_latchPin].DigitalWrite(PinState.Low);
        }

        public void SetGpioMaskV1(int v)
        {
            this._gs = new GpioSequence(this._nusbio.GetGpioMask(), this._nusbio.GetTransferBufferSize());
            this._nusbio.GPIOS[_latchPin].DigitalWrite(PinState.Low);
            if (this.MaxGpio == 16)
            {
                // 16 bit - 2 Shift Register
                ShiftOutFast(_nusbio, _dataPin, _clockPin, (byte)(v >> 8));
                ShiftOutFast(_nusbio, _dataPin, _clockPin, (byte)(v & 0xFF));
            }
            else
            {   // 8 bit - 1 Shift Register
                ShiftOutFast(_nusbio, _dataPin, _clockPin, (byte)(v & 0xFF));
            }
            _gs.Send(_nusbio);
            _nusbio.GPIOS[_latchPin].DigitalWrite(PinState.High);
            if (this.MaxGpio == 16)
            {
                _nusbio.GPIOS[_latchPin].DigitalWrite(PinState.Low);
                _nusbio.GPIOS[_latchPin].DigitalWrite(PinState.High);
            }
            if (this.MaxGpio == 24)
            {
                // todo
            }
        }

        public void SetGpioMask(int v)
        {
            this._gs = new GpioSequence(this._nusbio.GetGpioMask(), this._nusbio.GetTransferBufferSize());
            //this._nusbio.GPIOS[_latchPin].DigitalWrite(PinState.Low);
            if (this.MaxGpio == 16)
            {
                // 16 bit - 2 Shift Register
                ShiftOutFast(_nusbio, _dataPin, _clockPin, (byte)(v >> 8));
                ShiftOutFast(_nusbio, _dataPin, _clockPin, (byte)(v & 0xFF));
            }
            else
            {   // 8 bit - 1 Shift Register
                ShiftOutFast(_nusbio, _dataPin, _clockPin, (byte)(v & 0xFF));
            }
            _gs.Send(_nusbio);
            _nusbio.GPIOS[_latchPin].DigitalWrite(PinState.Low);
            _nusbio.GPIOS[_latchPin].DigitalWrite(PinState.High);
            _nusbio.GPIOS[_latchPin].DigitalWrite(PinState.Low);
            //if (this.MaxGpio == 16)
            //{
            //    _nusbio.GPIOS[_latchPin].DigitalWrite(PinState.Low);
            //    _nusbio.GPIOS[_latchPin].DigitalWrite(PinState.High);
            //}
            //if (this.MaxGpio == 24)
            //{
            //    // todo
            //}
        }

        /// <summary>
        /// ShiftRegister 74HC595 only support output
        /// </summary>
        /// <param name="pin"></param>
        /// <param name="mode"></param>
        public void SetPinMode(int pin, PinMode mode)
        {
            if(mode != PinMode.Output)
                throw new NotImplementedException();
        }

        public void DigitalWrite(int pin, PinState state)
        {
            var gpio = GetGpioFromIndex(pin);

            if (((IDigitalWriteRead)this).DigitalRead(pin) == PinState.High)
            {
                if(state == PinState.Low)
                    GpioStates &= ~gpio;
            }
            else
            {
                if (state == PinState.High)
                    GpioStates |= gpio;
            }
            this.SetGpioMask((int)GpioStates);
        }

        public PinState DigitalRead(int pin)
        {
            var gpio = GetGpioFromIndex(pin);
            if (((int)GpioStates & (int)gpio) == (int)gpio)
                return PinState.High;
            else
                return PinState.Low;
        }

       

        /// <summary>
        /// Cannot be implemented because we have 16 bits
        /// and this interface was designed for Nusbio v1
        /// </summary>
        /// <param name="forceRead"></param>
        /// <returns></returns>
        public byte GetGpioMask(bool forceRead)
        {
            throw new NotImplementedException();
        }

        public void SetPullUp(int p, PinState d)
        {
            throw new NotImplementedException();
        }

        public byte GpioStartIndex
        {
            get
            {
                return (byte)MinGpioIndex;
            }
        }
    }
}
