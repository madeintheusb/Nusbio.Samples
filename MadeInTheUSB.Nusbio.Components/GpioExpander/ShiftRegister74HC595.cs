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
    /// <summary>
    /// Port of the wiring_shift.c - shiftOut() function
    /// Part of Arduino - http://www.arduino.cc/
    /// Copyright (c) 2005-2006 David A. Mellis
    /// </summary>
    public class ShiftRegister74HC595
    {
        private const int LSBFIRST = 0;
        private const int MSBFIRST = 1;

        private NusbioGpio _dataPin;
        private NusbioGpio _latchPin;
        private NusbioGpio _clockPin;
        private Nusbio     _nusbio;

        public ShiftRegister74HC595(Nusbio nusbio, NusbioGpio dataPin,NusbioGpio latchPin,NusbioGpio clockPin)
        {
            this._dataPin  = dataPin;
            this._latchPin = latchPin;
            this._clockPin = clockPin;
            this._nusbio  = nusbio;
        }
        
        public void AnimateOneLeftToRightAndRightToLeft(int waitTime){

            this.Send8BitValue(0);

            var p2 = new List<byte>() { 1, 2, 4, 8, 16, 32, 64, 128 };

            for (int i = 0; i < 8; i++) {

                this.Send8BitValue(p2[i]);
                Thread.Sleep(waitTime);
            }
            for (int i = 8 - 1; i >= 0; i--) {

                this.Send8BitValue(p2[i]);
                Thread.Sleep(waitTime);
            }
            this.Send8BitValue(0);
        }

        private static void ShiftOut(Nusbio nusbio, NusbioGpio dataPin , NusbioGpio clockPin , int bitOrder , int val)
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

        public static int ShiftIn(Nusbio nusbio,  NusbioGpio dataPin, NusbioGpio clockPin, int bitOrder) {

	        int value = 0;
	        int i;

	        for (i = 0; i < 8; ++i) {

                nusbio.GPIOS[clockPin].DigitalWrite(PinState.High);

		        if (bitOrder == LSBFIRST)
			        value |= Nusbio.ConvertTo1Or0(nusbio.GPIOS[dataPin].DigitalRead()) << i;
		        else
			        value |= Nusbio.ConvertTo1Or0(nusbio.GPIOS[dataPin].DigitalRead()) << (7 - i);

                nusbio.GPIOS[clockPin].DigitalWrite(PinState.Low);
	        }
	        return value;
        }

        public void Send8BitValue(byte v) {

            _nusbio.GPIOS[_latchPin].DigitalWrite(PinState.Low);            
            ShiftOut(_nusbio, _dataPin, _clockPin, MSBFIRST, v);
            _nusbio.GPIOS[_latchPin].DigitalWrite(PinState.High);
        }
    }
}
