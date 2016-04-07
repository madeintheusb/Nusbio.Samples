/*
   The class APA102LEDStrip was written with the help of the following:
   
       Adafruit_DotStar
       https://github.com/adafruit/Adafruit_DotStar/blob/master/Adafruit_DotStar.cpp
  
       apa102-arduino
       https://github.com/pololu/apa102-arduino
  
       The Wheel() function comes from the Adafruit code 
       https://github.com/adafruit/Adafruit_NeoPixel

   Copyright (C) 2015 MadeInTheUSB LLC
   Ported to C# and Nusbio by FT for MadeInTheUSB

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
 
 ACKNOWLEDGMENT 
 ==============
  
Base on the work "RGB LED - Automatic Smooth Color Cycling" from Marco Colli 2012 for Arduino
RGB LED - Automatic Smooth Color Cycling
Marco Colli
April 2012

Uses the properties of the RGB Colour Cube
The RGB colour space can be viewed as a cube of colour. If we assume a cube of dimension 1, then the 
coordinates of the vertices for the cubve will range from (0,0,0) to (1,1,1) (all black to all white).
The transitions between each vertex will be a smooth colour flow and we can exploit this by using the 
path coordinates as the LED transition effect. 
 

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MadeInTheUSB.i2c;
using MadeInTheUSB.WinUtil;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
using System.Drawing;
using MadeInTheUSB.GPIO;

namespace MadeInTheUSB.Components
{
    public class RgbLedAutomaticSmoothColorCycler
    {
        // Constants for readability are better than magic numbers
        // Used to adjust the limits for the LED, especially if it has a lower ON threshold
        public const int MIN_RGB_VALUE = 0;   // no smaller than 0. 
        public const int MAX_RGB_VALUE = 255; // no bigger than 255.

        //// Slowing things down we need ...
        //public const int TRANSITION_DELAY = 70;   // in milliseconds, between individual light changes
        //public const int WAIT_DELAY = 500;  // in milliseconds, at the end of each Traverse
        //
        // Total traversal time is ((MAX_RGB_VALUE - MIN_RGB_VALUE) * TRANSITION_DELAY) + WAIT_DELAY
        // eg, ((255-0)*70)+500 = 18350ms = 18.35s

        // Structure to contain a 3D coordinate
        public class coord
        {
            public int  x, y, z;

            public coord(int X, int Y, int Z)
            {
                this.x = X;
                this.y = Y;
                this.z = Z;
            }
        } 

        public static coord  v = new coord(0, 0, 0); // the current rgb coordinates (colour) being displayed
        
        /*
        Vertices of a cube
      
            C+----------+G
            /|        / |
          B+---------+F |
           | |       |  |    y   
           |D+-------|--+H   ^  7 z
           |/        | /     | /
          A+---------+E      +--->x

        */
        public static List<coord> vertex = new List<coord>()
        {
        //x  y  z      name
          new coord(0, 0, 0) , // A or 0
          new coord(0, 1, 0), // B or 1
          new coord(0, 1, 1), // C or 2
          new coord(0, 0, 1), // D or 3
          new coord(1, 0, 0), // E or 4
          new coord(1, 1, 0), // F or 5
          new coord(1, 1, 1), // G or 6
          new coord(1, 0, 1)  // H or 7
        };

        /*
            A list of vertex numbers encoded 2 per byte.
            Hex digits are used as vertices 0-7 fit nicely (3 bits 000-111) and have the same visual
            representation as decimal, so bytes 0x12, 0x34 ... should be interpreted as vertex 1 to 
            v2 to v3 to v4 (ie, one continuous path B to C to D to E).
        */
        public static List<int> path = new List<int>()
        {
          0x01, 0x23, 0x76, 0x54, 0x03, 0x21, 0x56, 0x74,  // trace the edges
          0x13, 0x64, 0x16, 0x02, 0x75, 0x24, 0x35, 0x17, 0x25, 0x70,  // do the diagonals
        };

        public static int MAX_PATH_SIZE
        {
            get { return path.Count; }
        }
        
        // Move along the colour line from where we are to the next vertex of the cube.
        // The transition is achieved by applying the 'delta' value to the coordinate.
        // By definition all the coordinates will complete the transition at the same 
        // time as we only have one loop index.
        public static List<Color> Traverse(int dx, int dy, int dz)
        {
            var l = new List<Color>();
            if ((dx == 0) && (dy == 0) && (dz == 0))   // no point looping if we are staying in the same spot!
                return l;
    
            for (var i = 0;  i < MAX_RGB_VALUE - MIN_RGB_VALUE; 
                    i++, 
                    v.x += dx, 
                    v.y += dy, 
                    v.z += dz
                    )
            {
                l.Add(Color.FromArgb(v.x, v.y, v.z));
            }
            return l;
        }
    }
}
        