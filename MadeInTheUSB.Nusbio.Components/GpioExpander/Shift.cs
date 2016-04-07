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
using System.Drawing;

namespace MadeInTheUSB.Component
{
    /// <summary>
    /// Port of the wiring_shift.c - shiftOut() function
    /// Part of Arduino - http://www.arduino.cc/
    /// Copyright (c) 2005-2006 David A. Mellis
    /// </summary>
    public class Shift
    {
        public static void ClockIt(IDigitalWriteRead dwr,  int clockPin)
        {
            dwr.DigitalWrite(clockPin, PinState.High);
            dwr.DigitalWrite(clockPin, PinState.Low);
        }
        
        public static GpioSequence ShiftOutHardWare(Nusbio nusbio, int dataPin , int clockPin, int val, GpioSequence gs = null, bool send = true, BitOrder bitOrder = BitOrder.MSBFIRST)
        {
            if(gs == null)
                gs = new GpioSequence(nusbio.GetGpioMask());

	        for (var i = 0; i < 8; i++)
	        {
                int a = 0;
                
	            if (bitOrder == BitOrder.LSBFIRST)
	                a = (val & (1 << i));
	            else
	                a = (val & (1 << (7 - i)));
                gs.ClockBit(nusbio[clockPin], nusbio[dataPin], a > 0);
	        }
            if(send)
                nusbio.SetGpioMask(gs.ToArray());
            return gs;
        }
       
        public static void ShiftOutDataClockOptimized(IDigitalWriteRead dwr, int dataPin , int clockPin, int val, BitOrder bitOrder = BitOrder.MSBFIRST)
        {
            int i;
            
	        for (i = 0; i < 8; i++)
	        {
	            if (bitOrder == BitOrder.LSBFIRST)
	            {
	                var a = (val & (1 << i));
	                dwr.DigitalWrite(dataPin, Nusbio.ConvertToPinState(a));
	            }
	            else
	            {
	                var b = (val & (1 << (7 - i)));
	                dwr.DigitalWrite(dataPin, Nusbio.ConvertToPinState(b));
	            }
	            ClockIt(dwr, clockPin);
	        }   
        }

        public static void ShiftOut(IDigitalWriteRead dwr, int dataPin, int clockPin, List<byte> vals, BitOrder bitOrder = BitOrder.MSBFIRST)
        {
            foreach (var val in vals)
                ShiftOut(dwr, dataPin, clockPin, val, bitOrder);
        }

        public static void ShiftOut(
            IDigitalWriteRead dwr, 
            int dataPin, 
            int clockPin, 
            int val, 
            BitOrder bitOrder = BitOrder.MSBFIRST)
        {
            int i;

            System.Diagnostics.Debug.WriteLine("Shift {0}", val);

	        for (i = 0; i < 8; i++)
	        {
	            if (bitOrder == BitOrder.LSBFIRST)
	            {
	                var a = (val & (1 << i));
	                dwr.DigitalWrite(dataPin, Nusbio.ConvertToPinState(a));
	            }
	            else
	            {
	                var b = (val & (1 << (7 - i)));
	                dwr.DigitalWrite(dataPin, Nusbio.ConvertToPinState(b));
	            }
	            ClockIt(dwr, clockPin);
	        }   
        }

        public static int ShiftIn(IDigitalWriteRead dwr,  int dataPin, int clockPin, BitOrder bitOrder) {

	        int value = 0;
	        int i;

	        for (i = 0; i < 8; ++i) {

                dwr.DigitalWrite(clockPin, PinState.High);

		        if (bitOrder == BitOrder.LSBFIRST)
			        value |= Nusbio.ConvertTo1Or0(dwr.DigitalRead(dataPin)) << i;
		        else
			        value |= Nusbio.ConvertTo1Or0(dwr.DigitalRead(dataPin)) << (7 - i);

                dwr.DigitalWrite(dataPin, PinState.Low);
	        }
	        return value;
        }

    }
}
