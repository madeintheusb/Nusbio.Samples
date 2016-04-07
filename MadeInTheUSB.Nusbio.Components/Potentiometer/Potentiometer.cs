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
*/

using System;
using System.Collections.Generic;
using System.Threading;
using MadeInTheUSB;
using MadeInTheUSB.GPIO;

/// https://msdn.microsoft.com/en-us/library/windows/desktop/dn553408(v=vs.85).aspx
namespace MadeInTheUSB.Sensor
{
    /// </summary>
    /// 
    /// /// <summary>
    /// How to use potentiometer with the Nusbio.
    /// The potentiometer act as a resistor from 0 to x Ohms.
    /// For exemple a 10k potentiometer.
    /// 
    /// The Nusbio like the Raspberry PI does not offer Analog to digigal input port unlike the Arduino.
    /// There is no way to read the voltage coming out of the sensor.
    /// But what we can do is use a Capacitor (Reservoir) and count "how long it takes" to fill it.
    /// From that we can define a calibration and determine to position of the potentiometer in
    /// percent (0%..100%)
    /// 
    /// For a 10k Resistor, Capacitor 47mF, 1 4.7k Resistor feeding the capacitor (10k/2)
    /// 1 100 Ohms resistor going to ground
    /// 
    /// Wiring:
    ///                 [-------] <- Potentiometer
    ///                 |       |
    ///                 |^  ^  ^|
    ///                  |  |
    /// gpio0 -> 4.7kOhm[ ] |
    ///                     |--------> +[Capacitor]- -----> Ground
    /// gpio1 <--------------
    /// 
    /// 
    /// For more info see: http://razzpisampler.oreilly.com/ch08.html
    /// </summary>
    public class Potentiometer
    {
        
        public const int ErrorValue = -1;
 
        private readonly int               _inPin;
        private readonly int               _outPin;
        private readonly IDigitalWriteRead _digitalWriteRead;
        private readonly TimeOut           _timeOut;
        private readonly int               _minCalibratedValue;
        private int                        _maxCalibratedValue;
        private readonly double            _rangeCalibratedValue;
        private long                       _value = -1;
        private int                        _timeOutMaxValue = 2000;
        private double                     _lastPercentValue = -1;
        private bool                       _reversePercentageValue = false;

        public int AcquireResetTime = 20;

        /// <summary>
        /// Return the last value acquired, do not acquired the value except if it was never initialized
        /// </summary>
        public double LastPercentValue
        {
            get
            {
                if(_lastPercentValue == -1)
                    AcquireSample();
                return _lastPercentValue;
            }
        }

        /// <summary>
        /// Returns and break down the percentage value in 10 values from 0 to 9
        /// </summary>
        public int StepValue
        {
            get
            {
                var d = this.PercentValue;
                var v = (int)(d / 10);
                if(v >= 10)
                    v = 9;
                return v;
            }
        }
        public double PercentValue
        {
            get
            {
                double d = this.Value;
                this._lastPercentValue = ((d - this._minCalibratedValue) * 100 / this._rangeCalibratedValue);
                if(this._lastPercentValue < 0) 
                    this._lastPercentValue = 0;
                if(this._lastPercentValue > 100) 
                    this._lastPercentValue = 100;

                if (this._reversePercentageValue)
                {
                    this._lastPercentValue = 100 - this._lastPercentValue;
                }

                return this._lastPercentValue;
            }
        }               

        public long Value
        {
            get
            {
                if (this._timeOut.IsTimeOut() || this._value == -1)
                {
                    if (!AcquireSample())
                    {
                        _value = -1;
                    }
                }
                return _value;
            }
        }

        private void Discharge()
        {
            // << Force the capactor to discharge
            // In Input mode the gpio is HIGH (FT232 Gpio PullMode)
            // Switching it to LOW give a path for the capacitor to discharge
            // For the MCP30028 it is important to set the Pull up mode first
            // and the open the gpio in input mode
            this._digitalWriteRead.SetPullUp(_inPin, PinState.Low);
            this._digitalWriteRead.SetPinMode(_inPin, PinMode.Output);
            this._digitalWriteRead.DigitalWrite(_inPin, PinState.Low);

            Thread.Sleep(this.AcquireResetTime);
        }

        private bool AcquireSample()
        {
            this.Discharge();  

            var count = 0;
            var r     = true;
            
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Force the capactor to Charge
            // In Input mode the gpio is HIGH (FT232 Gpio PullMode)
            // Sending 1v to the capacitor

            this._digitalWriteRead.SetPullUp(_inPin, PinState.High);
            this._digitalWriteRead.SetPinMode(_inPin, PinMode.Input);
            
            // Send 5 volts to the capacitor which charge it even more
            this._digitalWriteRead.DigitalWrite(_outPin, PinState.High);

            // While the gpio _inPin (PullUp) has a connection to ground via
            // the capacitor charging the state of the gpio is low.
            // When the capacitor is charged, it does not let the current flow
            // and does not provide a connection to groud to gpio _inPin,
            // therefore the state of the gpio _inPin become High.
            // We could say that the one send from gpio _outPin, has been received
            // by gpio _inPin.
            while (this._digitalWriteRead.DigitalRead(_inPin) == PinState.Low)
            {
                count++;
                if (count > this._timeOutMaxValue)
                {
                    r = false;
                    break;
                }
            }
            this._value = sw.ElapsedMilliseconds;
            this._digitalWriteRead.DigitalWrite(_outPin, PinState.Low);

            Thread.Sleep(this.AcquireResetTime); // May not be needed

            return r;
        }

        public Potentiometer(IDigitalWriteRead digitalWriteRead, int inPin, int outPin, int minCalibratedValue, int maxCalibratedValue, int sampleFrequencySeconds = 500, bool reversePercentageValue = false, int timeOutMaxValue = 2000)
        {
            this._timeOut                = new TimeOut(sampleFrequencySeconds);
            this._inPin                  = inPin;
            this._outPin                 = outPin;
            this._digitalWriteRead       = digitalWriteRead;
            this._minCalibratedValue     = minCalibratedValue;
            this._maxCalibratedValue     = maxCalibratedValue;
            this._rangeCalibratedValue   = maxCalibratedValue - minCalibratedValue;
            this._timeOutMaxValue        = timeOutMaxValue;
            this._reversePercentageValue = reversePercentageValue;

            

            this._digitalWriteRead.SetPinMode(_outPin, PinMode.Output);
        }
    }
}
