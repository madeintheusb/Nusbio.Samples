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
using MadeInTheUSB;
using MadeInTheUSB.GPIO;

namespace MadeInTheUSB.Sensor
{
    /// <summary>
    /// PIR motion sensor have a reset time of 4 seconds ie, when they detect a motion the gpio is high
    /// for 4 seconds. We use the datetime _motionDetectedTimeStamp, to count the 4 seconds.
    /// The reset time of 4 seconds can be adjusted on the PIR with one of the 2 potentiometers.
    /// </summary>
    public class DigitalMotionSensorPIR
    {
        public enum MotionDetectedType
        {
            MotionDetected,
            SameMotionDetected,
            None,
        };

        private readonly int               _gpio;
        private readonly IDigitalWriteRead _digitalWriteRead;
        private readonly int               _resetTimeInSecond;
        private DateTime?                  _motionDetectedTimeStamp;

        private DateTime _idleStartTime;

        public DigitalMotionSensorPIR(IDigitalWriteRead digitalWriteRead, NusbioGpio gpio, int resetTimeInSecond = 4) : 
            this(digitalWriteRead, int.Parse(gpio.ToString().Replace("Gpio","")), resetTimeInSecond )
        {
            
        }

        public DigitalMotionSensorPIR(IDigitalWriteRead digitalWriteRead, int gpio, int resetTimeInSecond = 4)
        {
            this._idleStartTime     = DateTime.UtcNow;
            this._gpio              = gpio;
            this._resetTimeInSecond = resetTimeInSecond;
            this._digitalWriteRead  = digitalWriteRead;
            //_digitalWriteRead.SetPullUp(gpio, PinState.High);
            _digitalWriteRead.SetPinMode(gpio, PinMode.Input);
        }

        public void Begin()
        {

        }

        private bool IsResetTimeOver()
        {
            if (this._motionDetectedTimeStamp.HasValue)
            {
                TimeSpan span = DateTime.Now - this._motionDetectedTimeStamp.Value;
                return span.TotalSeconds > this._resetTimeInSecond;
            }
            else return false;
        }

        public MotionDetectedType MotionDetected()
        {
            if (this._digitalWriteRead.DigitalRead(this._gpio) == PinState.High)
            {
                if (!_motionDetectedTimeStamp.HasValue)
                {
                    // Just detected a motion, start counting for reset and return true
                    this._motionDetectedTimeStamp = DateTime.Now;
                    return MotionDetectedType.MotionDetected;
                }
                else
                {
                    if (this.IsResetTimeOver())
                    {
                        // Just detected a new motion, start counting for reset and return true
                        Console.WriteLine("RE DETECTTION = "+DateTime.Now );
                        this._motionDetectedTimeStamp = DateTime.Now;
                        return MotionDetectedType.MotionDetected;
                    }
                    else
                    {
                        // We are in the 4 seconds reset period, let's not say we detected a motion
                        //Console.WriteLine("SameMotionDetected ");
                        return MotionDetectedType.SameMotionDetected;
                    }
                }
            }
            else
            {
                if (this.IsResetTimeOver())
                {
                    this._motionDetectedTimeStamp = null;
                }
                return MotionDetectedType.None;
            }
        }
    }
}
