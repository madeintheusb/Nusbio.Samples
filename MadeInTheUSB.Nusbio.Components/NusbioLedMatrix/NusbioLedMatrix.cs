/*
    NusbioLedMatrix (MAX7219)
    Ported to C# and Nusbio by FT for MadeInTheUSB
    Copyright (C) 2015 MadeInTheUSB LLC
    
    Based on the library MAX7219
    A library for controling Leds with a MAX7219/MAX7222
    Copyright (c) 2007-2015 Eberhard Fahle
    https://github.com/wayoda/LedControl
  
    Also based on Adafruit 8x8 LED matrix with backpack
        Components\Adafruit\Adafruit_GFX.cs
 
    MIT license, all text above must be included in any redistribution

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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using MadeInTheUSB.Component;
using MadeInTheUSB.i2c;
using MadeInTheUSB.spi;
using MadeInTheUSB.WinUtil;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;

namespace MadeInTheUSB
{
    /// <summary>
    /// Dedicated class for the specific NusbioMatrix Device
    /// </summary>
    public class NusbioMatrix : MAX7219
    {
        /// <summary>
        /// The chip MAX7219 can be wired to an 8x8 LED matrix in 2 different ways.
        /// Depending on the wiring, the origin (0,0) is in the 
        /// - bottom right corner [DEFAULT]
        /// - upper left corner
        /// This will affect the programming
        /// </summary>
        public enum MAX7219_WIRING_TO_8x8_LED_MATRIX
        {
            OriginBottomRightCorner, // 1 8x8 LED matrix device sold by MadeInTheUSB
            OriginUpperLeftCorner,   // 4 8x8 LED matrix chained together device sold by MadeInTheUSB
        }

        public MAX7219_WIRING_TO_8x8_LED_MATRIX MAX7219Wiring = MAX7219_WIRING_TO_8x8_LED_MATRIX.OriginBottomRightCorner;

        public const int DEFAULT_BRIGTHNESS_DEMO = 5;

        public NusbioMatrix(
            Nusbio nusbio, 
            NusbioGpio selectGpio, 
            NusbioGpio mosiGpio, 
            NusbioGpio clockGpio, 
            NusbioGpio gndGpio, 
            MAX7219_WIRING_TO_8x8_LED_MATRIX max7219Wiring,
            int deviceCount = 1) :
            base(nusbio, selectGpio, mosiGpio, clockGpio, deviceCount)
        {
            this.MAX7219Wiring = max7219Wiring;
            if(gndGpio != NusbioGpio.None)
                nusbio.GPIOS[gndGpio].Low(); // Act as GND 
        }

        public static NusbioMatrix Initialize(
            Nusbio nusbio,
            NusbioGpio selectGpio,
            NusbioGpio mosiGpio,
            NusbioGpio clockGpio,
            NusbioGpio gndGpio,
            MAX7219_WIRING_TO_8x8_LED_MATRIX MAX7218Wiring,
            int deviceCount = 0)
        {
            // How to plug the 8x8 LED Matrix MAX7219 into Nusbio
            // --------------------------------------------------------------------------------
            // NUSBIO                          : GND VCC  7   6  5   4  3  2  1  0
            // 8x8 LED Matrix MAX7219 base     :     VCC GND DIN CS CLK
            // Gpio 7 act as ground so we can plug directly the 8x8 led matrix
            //
            // If you use a regular breadboard to connect the 8x8 LED matrix to Nusbio
            // Connect the LED Matrix's GND to the Nusbio's GND and set parameter gndGpio to None.
            var matrix = new NusbioMatrix(nusbio, selectGpio, mosiGpio,
                                                  clockGpio,  gndGpio,
                                                  MAX7218Wiring, deviceCount: deviceCount);
            matrix.Begin(DEFAULT_BRIGTHNESS_DEMO);
            return matrix;
        }

        public void WriteChar(int deviceIndex, char character, bool clear = true, int x = 2, int y = 0)
        {
            if (!CharDictionary.ContainsKey(character))
                throw new ArgumentException(string.Format("Character '{0}' is not defined in CharDictionary"));

            if(clear)
                this.Clear(deviceIndex);

            var charDef = CharDictionary[character];

            for (int i = 0; i < charDef.ColumnCount; i++)
            {
                int c = x + i;
                if (c >= 0 && c < 80)
                    SetColumn(deviceIndex, c, charDef.Columns[i]);
            }
            switch (this.MAX7219Wiring)
            {
                case MAX7219_WIRING_TO_8x8_LED_MATRIX.OriginUpperLeftCorner : this.RotateLeft(deviceIndex); break;
                case MAX7219_WIRING_TO_8x8_LED_MATRIX.OriginBottomRightCorner : this.RotateRight(deviceIndex); break;
            }
        }
    }

    public class NusbioGameMatrixObject
    {
        internal NusbioGameMatrix NusbioGameMatrix;
        public int _x, _y;
        protected double _angle;

        public virtual void Redraw()
        {
        }

        public virtual void Move(int sx = -1, int sy = -1, bool verify = true)
        {
        }

        public override string ToString()
        {
            return string.Format("({0:0.00}, {1:0.00})~ {2:000}   ", this._x, this._y, this._angle);
        }
    }

    public class NusbioGameMatrixBall : NusbioGameMatrixObject
    {
        private readonly NusbioGameMatrix _nusbioGameMatrix;
        internal MovementType Movement = MovementType.RightUp;

        public enum MovementType
        {
            RightUp, // x+y+
            RightDown, // x+y-
            LeftUp, // x-y+
            LeftDown // x-y-
        }

        public NusbioGameMatrixBall(int x, int y, NusbioGameMatrix nusbioGameMatrix)
        {
            this._x = x;
            this._y = y;
            this._angle = 45;
            this._nusbioGameMatrix = nusbioGameMatrix;
        }

        public override void Redraw()
        {
            //base.Redraw();
            _nusbioGameMatrix.NusbioMatrix.DrawPixel((int) this._x, (int) this._y, true);
        }

        private Random _seed = new Random(Environment.TickCount);

        private int NewDirectionRandomizer()
        {
            var r = _seed.Next(2);
            return r == 0 ? 1 : -1;
        }

        public override void Move(int sx = -1, int sy = -1, bool verify = true)
        {
            var px = this._x;
            var py = this._y;

            if (sx != -1) this._x = sx;
            if (sy != -1) this._y = sy;

            switch (Movement)
            {
                case MovementType.RightUp:
                    this._x++;
                    this._y++;
                    break;
                case MovementType.RightDown:
                    this._x++;
                    this._y--;
                    break;
                case MovementType.LeftUp:
                    this._x--;
                    this._y++;
                    break;
                case MovementType.LeftDown:
                    this._x--;
                    this._y--;
                    break;
            }
            if (verify)
            {
                if (this._x < 0 && this._y < 0)
                {
                    // LeftDown -> RightUp
                    this.Movement = MovementType.RightUp;
                    this.Move(px + NewDirectionRandomizer(), py + NewDirectionRandomizer(), false);
                }
                else if (this._x < 0)
                {
                    // LeftDown -> RightDown
                    this.Movement = MovementType.RightDown;
                    this.Move(px, py + NewDirectionRandomizer(), false);
                }
                else if (this._y < 0)
                {
                    // RightDown -> RightUp
                    this.Movement = MovementType.RightUp;
                    this.Move(px + NewDirectionRandomizer(), py, false);
                }
                else if (this._x >= this._nusbioGameMatrix.Width && this._y >= this._nusbioGameMatrix.Height)
                {
                    // RightUp -> LeftDown
                    this.Movement = MovementType.LeftDown;
                    this.Move(px + NewDirectionRandomizer(), py + NewDirectionRandomizer(), false);
                }
                else if (this._x >= this._nusbioGameMatrix.Width)
                {
                    // RightUp -> LeftUp
                    this.Movement = MovementType.LeftUp;
                    this.Move(px, py + NewDirectionRandomizer(), false);
                }
                else if (this._y >= this._nusbioGameMatrix.Height)
                {
                    // LeftUp -> LeftDown
                    this.Movement = MovementType.LeftDown;
                    this.Move(px + NewDirectionRandomizer(), py, false);
                }
            }


            /*
            if(sx != -1)
                this._x = sx;
            if(sy != -1)
                this._y = sy;

            var px = this._x;
            var py = this._y;

            this._x += Math.Cos(this._angle);
            this._y += Math.Sin(this._angle);

            if (verify)
            {
                if (this._x < 0 && this._y < 0)
                {
                    this._angle = (360 - this._angle)*-1;
                    this.Move(px, py, false); // request recomputing
                }
                else if (this._x < 0)
                {
                    this._angle = (360 - this._angle)*-1;
                    this.Move(px, py, false); // request recomputing
                }
                else if (this._y < 0)
                {
                    this._angle = (360 - this._angle)*-1;
                    this.Move(px, py, false); // request recomputing
                }
                else if (this._x > (this._nusbioGameMatrix.Width) && this._y > (this._nusbioGameMatrix.Height))
                {
                    this._angle = (360 - this._angle);
                    this.Move(px, py, false); // request recomputing
                }
                else if (this._x > (this._nusbioGameMatrix.Width))
                {
                    this._angle = (180 - this._angle);
                    this.Move(px, py, false); // request recomputing
                }
                else if (this._y > (this._nusbioGameMatrix.Height))
                {
                    this._angle = (360 - this._angle);
                    this.Move(px, py, false); // request recomputing
                }
            }
            else
            {
                if (this._x < 0 && this._y < 0)
                {
                    this._x = 0;
                    this._y = 0;
                }
                else if (this._x < 0)
                {
                    this._x = 0;
                }
                else if (this._y < 0)
                {
                    this._y = 0;
                }
                else if (this._x > (this._nusbioGameMatrix.Width) && this._y > (this._nusbioGameMatrix.Height))
                {
                    this._x = this._nusbioGameMatrix.Width;
                    this._y = this._nusbioGameMatrix.Height;
                }
                else if (this._x > (this._nusbioGameMatrix.Width))
                {
                    this._x = this._nusbioGameMatrix.Width;
                }
                else if (this._y > (this._nusbioGameMatrix.Height))
                {
                    this._y = this._nusbioGameMatrix.Height;
                }
            }*/
        }
    }


    public class NusbioMatrixGameRackette : NusbioGameMatrixObject
    {
        private readonly NusbioGameMatrix _nusbioGameMatrix;

        public NusbioMatrixGameRackette(int x, int y, NusbioGameMatrix nusbioGameMatrix)
        {
            this._x = x;
            this._y = y;
            this._nusbioGameMatrix = nusbioGameMatrix;
        }

        public override void Redraw()
        {
            _nusbioGameMatrix.NusbioMatrix.DrawLine(this._x, this._y + 1, this._x, this._y - 1, true);
        }

        public override void Move(int sx = -1, int sy = -1, bool verify = true)
        {

        }

        public void MoveUp()
        {
            if (this._y < this._nusbioGameMatrix.Height - 1)
                this._y++;
        }

        public void MoveDown()
        {
            if (this._y > 0 + 1)
                this._y--;
        }

        public bool InYArea(int vy)
        {
            return vy >= this._y - 1 && vy <= this._y + 1;
        }
    }


    public class NusbioGameMatrix
    {
        private NusbioGameMatrixBall _mainBall;

        public NusbioMatrix NusbioMatrix;
        public NusbioMatrixGameRackette Rackette;

        public int PointWon = 0;
        public int PointLost = 0;

        private List<NusbioGameMatrixObject> _objects = new List<NusbioGameMatrixObject>();

        public int Height
        {
            get { return NusbioMatrix.Height; }
        }

        public int Width
        {
            get { return NusbioMatrix.Width; }
        }

        public NusbioGameMatrix(NusbioMatrix nusbioMatrix)
        {
            NusbioMatrix = nusbioMatrix;
            _mainBall = new NusbioGameMatrixBall(2, 0, this);
            Rackette = new NusbioMatrixGameRackette(7, 3, this);

            _objects = new List<NusbioGameMatrixObject>();
            _objects.Add(_mainBall);
            _objects.Add(Rackette);
        }

        public void Redraw()
        {
            NusbioMatrix.Clear(0, false);
            foreach (var o in _objects)
            {
                o.Move();
                o.Redraw();
            }
            NusbioMatrix.WriteDisplay();

            // The ball reached the wall area
            if (this._mainBall._x == this.Width - 1)
            {
                if (this.Rackette.InYArea(this._mainBall._y))
                    this.PointWon++;
                else
                    this.PointLost++;
            }
        }

        public override string ToString()
        {
            var b = new StringBuilder();
            foreach (var o in _objects)
                b.Append(o.ToString()).AppendLine();

            b.AppendFormat("Win:{0}, Lost:{1}", this.PointWon, this.PointLost).AppendLine();
            return b.ToString();
        }
    }

    public class NusbioLandscapeMatrix
    {
        public NusbioMatrix _nusbioMatrix;
        public int CurrentYPosition = 0;
        public int CurrentXPosition = 0;
        private int _deviceIndex;
        
        public NusbioLandscapeMatrix(NusbioMatrix nusbioMatrix, int deviceIndex)
        {
            this._deviceIndex      = deviceIndex;
            this._nusbioMatrix     = nusbioMatrix;
            this.CurrentXPosition = this._nusbioMatrix.Width - 1;
            this.CurrentYPosition = this._nusbioMatrix.Height - 1;
            _nusbioMatrix.Clear(this._deviceIndex, true);
        }

        private Random _seed = new Random(Environment.TickCount);

        private int NewDirectionRandomizer()
        {
            var r = _seed.Next(2);
            return r == 0 ? 1 : -1;
        }

        public override string ToString()
        {
            return string.Format("x:{0}, y:{1}", this.CurrentXPosition, this.CurrentYPosition);
        }

        public void Redraw()
        {
            _nusbioMatrix.ScrollPixelLeftDevices(_nusbioMatrix.DeviceCount-1, 0);
            this._nusbioMatrix.CurrentDeviceIndex = this._deviceIndex;
            if (this._nusbioMatrix.MAX7219Wiring == NusbioMatrix.MAX7219_WIRING_TO_8x8_LED_MATRIX.OriginBottomRightCorner) 
                _nusbioMatrix.DrawPixel(CurrentYPosition, CurrentXPosition, true);
            else
                _nusbioMatrix.DrawPixel(CurrentXPosition, CurrentYPosition, true);
            _nusbioMatrix.WriteDisplay(all: true);
            CurrentXPosition += NewDirectionRandomizer();
            if (CurrentXPosition >= _nusbioMatrix.Width - 1)
                CurrentXPosition = _nusbioMatrix.Width - 1;
            if (CurrentXPosition < 0)
                CurrentXPosition = 0;
        }
    }
}
