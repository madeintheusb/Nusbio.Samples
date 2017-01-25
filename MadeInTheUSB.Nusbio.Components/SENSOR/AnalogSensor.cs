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
using MadeInTheUSB.i2c;

using int16_t  = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t  = System.Byte;

namespace MadeInTheUSB.Sensor
{
    public class AnalogSensor
    {
        protected Nusbio _nusbio;
        public double AnalogValue;
        public double Voltage;

        public int ADPort;
        /// <summary>
        /// 5 Volt reference voltage -- Nusbio is a 5 volt device
        /// </summary>
        public double ReferenceVoltage = 3.258;//5.09 3.258

        public AnalogSensor(Nusbio nusbio, int adPort = -1)
        {
            this._nusbio = nusbio;
            this.ADPort  = adPort;
        }

        public void SetAnalogValue(int value)
        {
            this.SetAnalogValue((double) value);
        }

        public virtual void SetAnalogValue(double value)
        {
            this.AnalogValue = value;
            this.Voltage      = value * this.ReferenceVoltage;
            this.Voltage     /= 1024.0;
        }

        public void Begin()
        {
            
        }
    }
}