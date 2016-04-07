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

namespace MadeInTheUSB.Sensor
{
    public class AnalogTemperatureSensor : AnalogSensor
    {
        public const double CELCIUS_TO_KELVIN = 274.15;

        protected double _celsiusValue;

        public enum TemperatureType
        {
            Celsius,
            Fahrenheit,
            Kelvin
        }

        public AnalogTemperatureSensor(Nusbio nusbio) : base(nusbio)
        {
            
        }

        public virtual bool Begin()
        {
            return true;
        }

        public virtual double GetTemperature(TemperatureType type = TemperatureType.Celsius)
        {
            throw new NotImplementedException("Method GetTemperature() must be overridden");
        }

        public double CelsiusToFahrenheit(double v)
        {
            return (v * 9.0 / 5.0) + 32.0;
        }

        public double CelsiusToKelvin(double v)
        {
            return v * CELCIUS_TO_KELVIN;
        }
    }

}

