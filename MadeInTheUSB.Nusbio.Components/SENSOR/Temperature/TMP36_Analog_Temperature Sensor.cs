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
    /// <summary>
    /// Tmp36 temperature sensor handler.
    /// Tmp36 is very inaccurate, typical accuracies of 
    ///     ±1°C at +25°C and 
    ///     ±2°C over the −40°C to +125°C temperature range
    /// http://www.analog.com/en/products/analog-to-digital-converters/integrated-special-purpose-converters/integrated-temperature-sensors/tmp36.html#product-overview
    /// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// If you want an accurate temperature sensor use the I2C MCP9808
    /// See Adafruit breakout https://www.adafruit.com/products/1782
    /// </summary>
    public class Tmp36AnalogTemperatureSensor : AnalogTemperatureSensor
    {
        /// <summary>
        /// Sensor TMP36 is very inaccurate, implemenent an optional feature
        /// where the result is the average of the last x samples.
        /// This should give better result for TMP35
        /// </summary>
        public int AverageOnLastCountSamples = -1;
        private Queue<double> _averageOnLastCountSamples;

        public Tmp36AnalogTemperatureSensor(Nusbio nusbio) : base(nusbio)
        {
            
        }

        public virtual void SetAnalogValue(double value)
        {
            base.SetAnalogValue(value);
            base.Voltage      = value * base.ReferenceVoltage;
            base.Voltage     /= 1024.0;
            var celsiusValue  = (Voltage - 0.5) * 100;

            if (AverageOnLastCountSamples == -1)
            {
                this._celsiusValue = celsiusValue;
            }
            else
            {
                if (_averageOnLastCountSamples == null)
                    _averageOnLastCountSamples = new Queue<double>(AverageOnLastCountSamples);

                _averageOnLastCountSamples.Enqueue(celsiusValue);
                while (_averageOnLastCountSamples.Count() > AverageOnLastCountSamples)
                {
                    _averageOnLastCountSamples.Dequeue();
                }
                this._celsiusValue = _averageOnLastCountSamples.Average();
            }
        }

        public bool Begin()
        {
            return base.Begin();
        }

        public virtual double GetTemperature(TemperatureType type = TemperatureType.Celsius)
        {
            switch (type)
            {
                case TemperatureType.Celsius:    return this._celsiusValue;
                case TemperatureType.Fahrenheit: return CelsiusToFahrenheit(GetTemperature(TemperatureType.Celsius));
                case TemperatureType.Kelvin:     return CelsiusToKelvin(GetTemperature(TemperatureType.Celsius));
                default:
                    throw new ArgumentException();
            }
        }
    }
}

