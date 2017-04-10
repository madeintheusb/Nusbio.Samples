/*
    Written by FT for MadeInTheUSB
    Copyright (C) 2015 MadeInTheUSB LLC

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
 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MadeInTheUSB.GPIO;

namespace MadeInTheUSB.Sensor
{
    /// <summary>
    /// PIR motion sensor handled with a Analog to Digital converter.
    /// PIR motion sensor can be dealt with a simple gpio. But for demo purpose and for a specific board
    /// it was easier to control the PIR motion sensor with an AD converter
    /// </summary>
    public class AnalogMotionSensor : AnalogSensor
    {
        private readonly int _resetTimeInSecond;

        TimeOut _timeOut;

        public AnalogMotionSensor(Nusbio nusbio, int resetTimeInSecond = 4) : base(nusbio)
        {
            this._resetTimeInSecond = resetTimeInSecond;
            this._timeOut = null;
            
        }

        public virtual bool Begin()
        {
            return true;
        }

        public DigitalMotionSensorPIR.MotionDetectedType MotionDetected()
        {
            if (this.AnalogValue > 1)
            {
                if(this._timeOut == null)
                {
                    // Start a timeout and return MotionDetected
                    this._timeOut = new TimeOut(this._resetTimeInSecond);
                    return DigitalMotionSensorPIR.MotionDetectedType.MotionDetected;
                }
                else
                {
                    if (this._timeOut.IsTimeOut())
                    {
                        // this is a new timeout
                        this._timeOut = new TimeOut(this._resetTimeInSecond);
                        return DigitalMotionSensorPIR.MotionDetectedType.MotionDetected;
                    }
                    else
                        return DigitalMotionSensorPIR.MotionDetectedType.SameMotionDetected;
                }
            }
            else
            {
                this._timeOut = null;
                return DigitalMotionSensorPIR.MotionDetectedType.None;
            }
        }
    }
}

