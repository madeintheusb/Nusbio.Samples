#define DEBUGY
/*
   Copyright (C) 2017 MadeInTheUSB LLC
   Ported from C to C# by FT for MadeInTheUSB

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

    This code is based on the C library 
    SparkFun_APDS-9960_Sensor_Arduino_Library
    https://github.com/sparkfun/SparkFun_APDS-9960_Sensor_Arduino_Library
*/

using System;
using System.Collections.Generic;
using MadeInTheUSB;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.i2c;

using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Components.Interface;

namespace MadeInTheUSB.Sensor
{
    /// <summary>
    /// https://raw.githubusercontent.com/sparkfun/SparkFun_APDS-9960_Sensor_Arduino_Library/master/src/SparkFun_APDS9960.cpp
    /// https://raw.githubusercontent.com/sparkfun/SparkFun_APDS-9960_Sensor_Arduino_Library/master/src/SparkFun_APDS9960.h
    /// </summary>
    public class GY_9960LLC : MadeInTheUSB.Components.Interface.Ii2cOut
    {
        /* APDS-9960 I2C address */
        public const int APDS9960_I2C_ADDR = 0x39;

        /* Gesture parameters */
        protected const int GESTURE_THRESHOLD_OUT = 10;
        protected const int GESTURE_SENSITIVITY_1 = 50;
        protected const int GESTURE_SENSITIVITY_2 = 20;

        /* Error code for returned values */
        protected const int ERROR = 0xFF;

        /* Acceptable device IDs */
        protected const int APDS9960_ID_1 = 0xAB;
        protected const int APDS9960_ID_2 = 0x9C;

        /* Misc parameters */
        protected const int FIFO_PAUSE_TIME = 30;      // Wait period (ms) between FIFO reads

        /* APDS-9960 register addresses */
        protected const int APDS9960_ENABLE     = 0x80;
        protected const int APDS9960_ATIME      = 0x81;
        protected const int APDS9960_WTIME      = 0x83;
        protected const int APDS9960_AILTL      = 0x84;
        protected const int APDS9960_AILTH      = 0x85;
        protected const int APDS9960_AIHTL      = 0x86;
        protected const int APDS9960_AIHTH      = 0x87;
        protected const int APDS9960_PILT       = 0x89;
        protected const int APDS9960_PIHT       = 0x8B;
        protected const int APDS9960_PERS       = 0x8C;
        protected const int APDS9960_CONFIG1    = 0x8D;
        protected const int APDS9960_PPULSE     = 0x8E;
        protected const int APDS9960_CONTROL    = 0x8F;
        protected const int APDS9960_CONFIG2    = 0x90;
        protected const int APDS9960_ID         = 0x92;
        protected const int APDS9960_STATUS     = 0x93;
        protected const int APDS9960_CDATAL     = 0x94;
        protected const int APDS9960_CDATAH     = 0x95;
        protected const int APDS9960_RDATAL     = 0x96;
        protected const int APDS9960_RDATAH     = 0x97;
        protected const int APDS9960_GDATAL     = 0x98;
        protected const int APDS9960_GDATAH     = 0x99;
        protected const int APDS9960_BDATAL     = 0x9A;
        protected const int APDS9960_BDATAH     = 0x9B;
        protected const int APDS9960_PDATA      = 0x9C;
        protected const int APDS9960_POFFSET_UR = 0x9D;
        protected const int APDS9960_POFFSET_DL = 0x9E;
        protected const int APDS9960_CONFIG3    = 0x9F;
        protected const int APDS9960_GPENTH     = 0xA0;
        protected const int APDS9960_GEXTH      = 0xA1;
        protected const int APDS9960_GCONF1     = 0xA2;
        protected const int APDS9960_GCONF2     = 0xA3;
        protected const int APDS9960_GOFFSET_U  = 0xA4;
        protected const int APDS9960_GOFFSET_D  = 0xA5;
        protected const int APDS9960_GOFFSET_L  = 0xA7;
        protected const int APDS9960_GOFFSET_R  = 0xA9;
        protected const int APDS9960_GPULSE     = 0xA6;
        protected const int APDS9960_GCONF3     = 0xAA;
        protected const int APDS9960_GCONF4     = 0xAB;
        protected const int APDS9960_GFLVL      = 0xAE;
        protected const int APDS9960_GSTATUS    = 0xAF;
        protected const int APDS9960_IFORCE     = 0xE4;
        protected const int APDS9960_PICLEAR    = 0xE5;
        protected const int APDS9960_CICLEAR    = 0xE6;
        protected const int APDS9960_AICLEAR    = 0xE7;
        protected const int APDS9960_GFIFO_U    = 0xFC;
        protected const int APDS9960_GFIFO_D    = 0xFD;
        protected const int APDS9960_GFIFO_L    = 0xFE;
        protected const int APDS9960_GFIFO_R    = 0xFF;

        /* Bit fields */
        protected const int APDS9960_PON    = 1;  // 0b00000001
        protected const int APDS9960_AEN    = 2;  // 0b00000010
        protected const int APDS9960_PEN    = 4;  // 0b00000100
        protected const int APDS9960_WEN    = 8;  // 0b00001000
        protected const int APSD9960_AIEN   = 16; // 0b00010000
        protected const int APDS9960_PIEN   = 32; // 0b00100000
        protected const int APDS9960_GEN    = 64; // 0b01000000
        protected const int APDS9960_GVALID = 1;  // 0b00000001

        /* On/Off definitions */
        protected const int OFF = 0;
        protected const int ON  = 1;

        /* Acceptable parameters for setMode */
        protected const int POWER             = 0;
        protected const int AMBIENT_LIGHT     = 1;
        protected const int PROXIMITY         = 2;
        protected const int WAIT              = 3;
        protected const int AMBIENT_LIGHT_INT = 4;
        protected const int PROXIMITY_INT     = 5;
        protected const int GESTURE           = 6;
        protected const int ALL               = 7;

        /* LED Drive values */
        protected const int LED_DRIVE_100MA  = 0;
        protected const int LED_DRIVE_50MA   = 1;
        protected const int LED_DRIVE_25MA   = 2;
        protected const int LED_DRIVE_12_5MA = 3;

        /* Proximity Gain (PGAIN) values */
        protected const int PGAIN_1X = 0;
        protected const int PGAIN_2X = 1;
        protected const int PGAIN_4X = 2;
        protected const int PGAIN_8X = 3;

        /* ALS Gain (AGAIN) values */
        protected const int AGAIN_1X      = 0;
        protected const int AGAIN_4X      = 1;
        protected const int AGAIN_16X     = 2;
        protected const int AGAIN_64X     = 3;

        /* Gesture Gain (GGAIN) values */
        protected const int GGAIN_1X      = 0;
        protected const int GGAIN_2X      = 1;
        protected const int GGAIN_4X      = 2;
        protected const int GGAIN_8X      = 3;

        /* LED Boost values */
        protected const int LED_BOOST_100 = 0;
        protected const int LED_BOOST_150 = 1;
        protected const int LED_BOOST_200 = 2;
        protected const int LED_BOOST_300 = 3;

        /* Gesture wait time values */
        protected const int GWTIME_0MS    = 0;
        protected const int GWTIME_2_8MS  = 1;
        protected const int GWTIME_5_6MS  = 2;
        protected const int GWTIME_8_4MS  = 3;
        protected const int GWTIME_14_0MS = 4;
        protected const int GWTIME_22_4MS = 5;
        protected const int GWTIME_30_8MS = 6;
        protected const int GWTIME_39_2MS = 7;

        /* Default values */
        protected const int DEFAULT_ATIME          = 219; // 103ms
        protected const int DEFAULT_WTIME          = 246; // 27ms
        protected const int DEFAULT_PROX_PPULSE    = 0x87; // 16us, 8 pulses
        protected const int DEFAULT_GESTURE_PPULSE = 0x89; // 16us, 10 pulses
        protected const int DEFAULT_POFFSET_UR     = 0; // 0 offset
        protected const int DEFAULT_POFFSET_DL     = 0; // 0 offset      
        protected const int DEFAULT_CONFIG1        = 0x60; // No 12x wait (WTIME) factor
        protected const int DEFAULT_LDRIVE         = LED_DRIVE_100MA;
        protected const int DEFAULT_PGAIN          = PGAIN_4X;
        protected const int DEFAULT_AGAIN          = AGAIN_4X;
        protected const int DEFAULT_PILT           = 0; // Low proximity threshold
        protected const int DEFAULT_PIHT           = 50; // High proximity threshold
        protected const int DEFAULT_AILT           = 0xFFFF; // Force interrupt for calibration
        protected const int DEFAULT_AIHT           = 0;
        protected const int DEFAULT_PERS           = 0x11; // 2 consecutive prox or ALS for int.
        protected const int DEFAULT_CONFIG2        = 0x01; // No saturation interrupts or LED boost  
        protected const int DEFAULT_CONFIG3        = 0; // Enable all photodiodes, no SAI
        protected const int DEFAULT_GPENTH         = 40; // Threshold for entering gesture mode
        protected const int DEFAULT_GEXTH          = 30; // Threshold for exiting gesture mode    
        protected const int DEFAULT_GCONF1         = 0x40; // 4 gesture events for int., 1 for exit
        protected const int DEFAULT_GGAIN          = GGAIN_4X;
        protected const int DEFAULT_GLDRIVE        = LED_DRIVE_100MA;
        protected const int DEFAULT_GWTIME         = GWTIME_2_8MS;
        protected const int DEFAULT_GOFFSET        = 0; // No offset scaling for gesture mode
        protected const int DEFAULT_GPULSE         = 0xC9; // 32us, 10 pulses
        protected const int DEFAULT_GCONF3         = 0; // All photodiodes active during gesture
        protected const int DEFAULT_GIEN           = 0; // Disable gesture interrupts


       
        /* Direction definitions */
        public enum Direction
        {
            DIR_NONE,
            DIR_LEFT,
            DIR_RIGHT,
            DIR_UP,
            DIR_DOWN,
            DIR_NEAR,
            DIR_FAR,
            DIR_ALL,
            ERROR = 255
        };

        /* State definitions */
        public enum State
        {
            NA_STATE,
            NEAR_STATE,
            FAR_STATE,
            ALL_STATE,

            UNDEFINED = 255
        };
        
        public class gesture_data_type
        {
            public uint8_t [] u_data;
            public uint8_t [] d_data;
            public uint8_t [] l_data;
            public uint8_t [] r_data;
            public uint8_t index;
            public uint8_t total_gestures;
            public uint8_t in_threshold;
            public uint8_t out_threshold;

            public gesture_data_type()
            {
                u_data = new uint8_t[32];
                d_data = new uint8_t[32];
                l_data = new uint8_t[32];
                r_data = new uint8_t[32];
            }
        };

        gesture_data_type gesture_data_ = new gesture_data_type();

        /* Members */
        //gesture_data_type gesture_data_;
        int gesture_ud_delta_;
        int gesture_lr_delta_;
        int gesture_ud_count_;
        int gesture_lr_count_;
        int gesture_near_count_;
        int gesture_far_count_;
        State gesture_state_ = State.UNDEFINED;
        Direction gesture_motion_ = Direction.DIR_ALL;
        
        public int DeviceId;
#if !NUSBIO2
        protected I2CEngine _i2c;
        private Nusbio _nusbio;
#endif
        NusbioGpio _interruptPin;

        public GY_9960LLC(Nusbio nusbio, NusbioGpio sdaOutPin, NusbioGpio sclPin, NusbioGpio interruptPin, int deviceID)
        {
            this._interruptPin = interruptPin;
            this._nusbio       = nusbio;
            this._i2c          = new I2CEngine(nusbio, sdaOutPin, sclPin, (Byte)deviceID);

            nusbio.SetPinMode(_interruptPin, PinMode.Input);

            gesture_ud_delta_ = 0;
            gesture_lr_delta_ = 0;
            gesture_ud_count_ = 0;
            gesture_lr_count_ = 0;
            gesture_near_count_ = 0;
            gesture_far_count_ = 0;

            gesture_state_ = 0;
            gesture_motion_ = Direction.DIR_NONE;

            resetGestureParameters();

            if (!init())
            {
                throw new ApplicationException("Cannot find sensor");
            }
        }

        public PinState InterruptOn()
        {
            return _nusbio.GPIOS[_interruptPin].PinState;
        }

        void delay(int t)
        {
            System.Threading.Thread.Sleep(t);
        }

        /**
         * @brief Determines swipe direction or near/far state
         *
         * @return True if near/far event. False otherwise.
         */
        bool decodeGesture()
        {
            /* Return if near or far event is detected */
            if (gesture_state_ == State.NEAR_STATE)
            {
                gesture_motion_ = Direction.DIR_NEAR;
                return true;
            }
            else if (gesture_state_ == State.FAR_STATE)
            {
                gesture_motion_ = Direction.DIR_FAR;
                return true;
            }

            /* Determine swipe direction */
            if ((gesture_ud_count_ == -1) && (gesture_lr_count_ == 0))
            {
                gesture_motion_ = Direction.DIR_UP;
            }
            else if ((gesture_ud_count_ == 1) && (gesture_lr_count_ == 0))
            {
                gesture_motion_ = Direction.DIR_DOWN;
            }
            else if ((gesture_ud_count_ == 0) && (gesture_lr_count_ == 1))
            {
                gesture_motion_ = Direction.DIR_RIGHT;
            }
            else if ((gesture_ud_count_ == 0) && (gesture_lr_count_ == -1))
            {
                gesture_motion_ = Direction.DIR_LEFT;
            }
            else if ((gesture_ud_count_ == -1) && (gesture_lr_count_ == 1))
            {
                if (Math.Abs(gesture_ud_delta_) > Math.Abs(gesture_lr_delta_))
                {
                    gesture_motion_ = Direction.DIR_UP;
                }
                else
                {
                    gesture_motion_ = Direction.DIR_RIGHT;
                }
            }
            else if ((gesture_ud_count_ == 1) && (gesture_lr_count_ == -1))
            {
                if (Math.Abs(gesture_ud_delta_) > Math.Abs(gesture_lr_delta_))
                {
                    gesture_motion_ = Direction.DIR_DOWN;
                }
                else
                {
                    gesture_motion_ = Direction.DIR_LEFT;
                }
            }
            else if ((gesture_ud_count_ == -1) && (gesture_lr_count_ == -1))
            {
                if (Math.Abs(gesture_ud_delta_) > Math.Abs(gesture_lr_delta_))
                {
                    gesture_motion_ = Direction.DIR_UP;
                }
                else
                {
                    gesture_motion_ = Direction.DIR_LEFT;
                }
            }
            else if ((gesture_ud_count_ == 1) && (gesture_lr_count_ == 1))
            {
                if (Math.Abs(gesture_ud_delta_) > Math.Abs(gesture_lr_delta_))
                {
                    gesture_motion_ = Direction.DIR_DOWN;
                }
                else
                {
                    gesture_motion_ = Direction.DIR_RIGHT;
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        /**
         * @brief Processes a gesture event and returns best guessed gesture
         *
         * @return Number corresponding to gesture. -1 on error.
         */
        public Direction readGesture()
        {
            uint8_t fifo_level   = 0;
            int bytes_read       = 0;
            uint8_t [] fifo_data = new byte[128];
            uint8_t gstatus      = 0;
            Direction motion     = Direction.ERROR;
            int i;

            /* Make sure that power and gesture is on and data is valid */
            if (!isGestureAvailable() || !((getMode() & 65) == 65)) // 0b01000001
                return Direction.ERROR;

            /* Keep looping as long as gesture data is valid */
            {
                delay(FIFO_PAUSE_TIME); /* Wait some time to collect next batch of FIFO data */
                if (!wireReadDataByte(APDS9960_GSTATUS, ref gstatus)) /* Get the contents of the STATUS register. Is data still valid? */
                    return Direction.DIR_UP;
                
                if ((gstatus & APDS9960_GVALID) == APDS9960_GVALID) /* If we have valid data, read in FIFO */
                {
                    if (!wireReadDataByte(APDS9960_GFLVL, ref fifo_level)) /* Read the current FIFO level */
                        return Direction.ERROR;
#if DEBUGY
                    Console.WriteLine ("FIFO Level:{0}", fifo_level);
#endif
                    if (fifo_level > 0) /* If there's stuff in the FIFO, read it into our data block */
                    {
                        bytes_read = wireReadDataBlock(APDS9960_GFIFO_U, fifo_data, (fifo_level * 4));
                        if (bytes_read == -1)
                            return Direction.ERROR;
#if DEBUGY
                        Console.Write("FIFO Dump:");
                        for (i = 0; i < bytes_read; i++)
                            Console.Write("{0} ", fifo_data[i]);
                        Console.WriteLine();
#endif
                        if (bytes_read >= 4) /* If at least 1 set of data, sort the data into U/D/L/R */
                        {
                            for (i = 0; i < bytes_read; i += 4)
                            {
                                gesture_data_.u_data[gesture_data_.index] = fifo_data[i + 0];
                                gesture_data_.d_data[gesture_data_.index] = fifo_data[i + 1];
                                gesture_data_.l_data[gesture_data_.index] = fifo_data[i + 2];
                                gesture_data_.r_data[gesture_data_.index] = fifo_data[i + 3];
                                gesture_data_.index++;
                                gesture_data_.total_gestures++;
                            }
#if DEBUGY
                            Console.Write("Up Data: ");
                            for (i = 0; i < gesture_data_.total_gestures; i++)
                                Console.Write("{0} ", gesture_data_.u_data[i]);
                            Console.WriteLine("");
#endif
                            try
                            {
                                /* Filter and process gesture data. Decode near/far state */
                                if (processGestureData())
                                {
                                    if (decodeGesture())
                                    {
#if DEBUGY                              //***TODO: U-Turn Gestures
                                        //Console.WriteLine(gesture_motion_);
                                        return gesture_motion_;
#endif
                                    }
                                }
                            }
                            finally
                            {
                                /* Reset data */
                                gesture_data_.index = 0;
                                gesture_data_.total_gestures = 0;
                            }
                        }
                    }
                }
                else
                {
                    /* Determine best guessed gesture and clean up */
                    delay(FIFO_PAUSE_TIME);
                    decodeGesture();
                    motion = gesture_motion_;
#if DEBUG
                    Console.Write("END:{0}", gesture_motion_);
#endif
                    resetGestureParameters();
                    return motion;
                }
            }
            return Direction.DIR_NONE;
        }

        /**
         * @brief Processes the raw gesture data to determine swipe direction
         *
         * @return True if near or far state seen. False otherwise.
         */
        bool processGestureData()
        {
            uint8_t u_first = 0;
            uint8_t d_first = 0;
            uint8_t l_first = 0;
            uint8_t r_first = 0;
            uint8_t u_last = 0;
            uint8_t d_last = 0;
            uint8_t l_last = 0;
            uint8_t r_last = 0;
            int ud_ratio_first;
            int lr_ratio_first;
            int ud_ratio_last;
            int lr_ratio_last;
            int ud_delta;
            int lr_delta;
            int i;

            /* If we have less than 4 total gestures, that's not enough */
            if (gesture_data_.total_gestures <= 4)
            {
                return false;
            }

            /* Check to make sure our data isn't out of bounds */
            if ((gesture_data_.total_gestures <= 32) && (gesture_data_.total_gestures > 0) )
            {
                /* Find the first value in U/D/L/R above the threshold */
                for (i = 0; i < gesture_data_.total_gestures; i++)
                {
                    if ((gesture_data_.u_data[i] > GESTURE_THRESHOLD_OUT) && (gesture_data_.d_data[i] > GESTURE_THRESHOLD_OUT) && (gesture_data_.l_data[i] > GESTURE_THRESHOLD_OUT) && (gesture_data_.r_data[i] > GESTURE_THRESHOLD_OUT))
                    {
                        u_first = gesture_data_.u_data[i];
                        d_first = gesture_data_.d_data[i];
                        l_first = gesture_data_.l_data[i];
                        r_first = gesture_data_.r_data[i];
                        break;
                    }
                }

                /* If one of the _first values is 0, then there is no good data */
                if ((u_first == 0) || (d_first == 0) || (l_first == 0) || (r_first == 0) )
                {
                    return false;
                }
                /* Find the last value in U/D/L/R above the threshold */
                for (i = gesture_data_.total_gestures - 1; i >= 0; i--)
                {
//#if DEBUG
//                    Serial.print(F("Finding last: "));
//                    Serial.print(F("U:"));
//                    Serial.print(gesture_data_.u_data[i]);
//                    Serial.print(F(" D:"));
//                    Serial.print(gesture_data_.d_data[i]);
//                    Serial.print(F(" L:"));
//                    Serial.print(gesture_data_.l_data[i]);
//                    Serial.print(F(" R:"));
//                    Serial.println(gesture_data_.r_data[i]);
//#endif
                    if ((gesture_data_.u_data[i] > GESTURE_THRESHOLD_OUT) && (gesture_data_.d_data[i] > GESTURE_THRESHOLD_OUT) && (gesture_data_.l_data[i] > GESTURE_THRESHOLD_OUT) && (gesture_data_.r_data[i] > GESTURE_THRESHOLD_OUT))
                    {
                        u_last = gesture_data_.u_data[i];
                        d_last = gesture_data_.d_data[i];
                        l_last = gesture_data_.l_data[i];
                        r_last = gesture_data_.r_data[i];
                        break;
                    }
                }
            }

            /* Calculate the first vs. last ratio of up/down and left/right */
            ud_ratio_first = ((u_first - d_first) * 100) / (u_first + d_first);
            lr_ratio_first = ((l_first - r_first) * 100) / (l_first + r_first);
            ud_ratio_last = ((u_last - d_last) * 100) / (u_last + d_last);
            lr_ratio_last = ((l_last - r_last) * 100) / (l_last + r_last);

//#if DEBUG
//            Serial.print(F("Last Values: "));
//            Serial.print(F("U:"));
//            Serial.print(u_last);
//            Serial.print(F(" D:"));
//            Serial.print(d_last);
//            Serial.print(F(" L:"));
//            Serial.print(l_last);
//            Serial.print(F(" R:"));
//            Serial.println(r_last);

//            Serial.print(F("Ratios: "));
//            Serial.print(F("UD Fi: "));
//            Serial.print(ud_ratio_first);
//            Serial.print(F(" UD La: "));
//            Serial.print(ud_ratio_last);
//            Serial.print(F(" LR Fi: "));
//            Serial.print(lr_ratio_first);
//            Serial.print(F(" LR La: "));
//            Serial.println(lr_ratio_last);
//#endif

            /* Determine the difference between the first and last ratios */
            ud_delta = ud_ratio_last - ud_ratio_first;
            lr_delta = lr_ratio_last - lr_ratio_first;

//#if DEBUG
//            Serial.print("Deltas: ");
//            Serial.print("UD: ");
//            Serial.print(ud_delta);
//            Serial.print(" LR: ");
//            Serial.println(lr_delta);
//#endif

            /* Accumulate the UD and LR delta values */
            gesture_ud_delta_ += ud_delta;
            gesture_lr_delta_ += lr_delta;

//#if DEBUG
//            Serial.print("Accumulations: ");
//            Serial.print("UD: ");
//            Serial.print(gesture_ud_delta_);
//            Serial.print(" LR: ");
//            Serial.println(gesture_lr_delta_);
//#endif

            /* Determine U/D gesture */
            if (gesture_ud_delta_ >= GESTURE_SENSITIVITY_1)
            {
                gesture_ud_count_ = 1;
            }
            else if (gesture_ud_delta_ <= -GESTURE_SENSITIVITY_1)
            {
                gesture_ud_count_ = -1;
            }
            else
            {
                gesture_ud_count_ = 0;
            }

            /* Determine L/R gesture */
            if (gesture_lr_delta_ >= GESTURE_SENSITIVITY_1)
            {
                gesture_lr_count_ = 1;
            }
            else if (gesture_lr_delta_ <= -GESTURE_SENSITIVITY_1)
            {
                gesture_lr_count_ = -1;
            }
            else
            {
                gesture_lr_count_ = 0;
            }

            /* Determine Near/Far gesture */
            if ((gesture_ud_count_ == 0) && (gesture_lr_count_ == 0))
            {
                if ((Math.Abs(ud_delta) < GESTURE_SENSITIVITY_2) && (Math.Abs(lr_delta) < GESTURE_SENSITIVITY_2) )
                {
                    if ((ud_delta == 0) && (lr_delta == 0))
                    {
                        gesture_near_count_++;
                    }
                    else if ((ud_delta != 0) || (lr_delta != 0))
                    {
                        gesture_far_count_++;
                    }

                    if ((gesture_near_count_ >= 10) && (gesture_far_count_ >= 2))
                    {
                        if ((ud_delta == 0) && (lr_delta == 0))
                        {
                            gesture_state_ = State.NEAR_STATE;
                        }
                        else if ((ud_delta != 0) && (lr_delta != 0))
                        {
                            gesture_state_ = State.FAR_STATE;
                        }
                        return true;
                    }
                }
            }
            else
            {
                if ((Math.Abs(ud_delta) < GESTURE_SENSITIVITY_2) && (Math.Abs(lr_delta) < GESTURE_SENSITIVITY_2) )
                {
                    if ((ud_delta == 0) && (lr_delta == 0))
                    {
                        gesture_near_count_++;
                    }

                    if (gesture_near_count_ >= 10)
                    {
                        gesture_ud_count_ = 0;
                        gesture_lr_count_ = 0;
                        gesture_ud_delta_ = 0;
                        gesture_lr_delta_ = 0;
                    }
                }
            }

//#if DEBUG
//            Serial.print("UD_CT: ");
//            Serial.print(gesture_ud_count_);
//            Serial.print(" LR_CT: ");
//            Serial.print(gesture_lr_count_);
//            Serial.print(" NEAR_CT: ");
//            Serial.print(gesture_near_count_);
//            Serial.print(" FAR_CT: ");
//            Serial.println(gesture_far_count_);
//            Serial.println("----------");
//#endif

            return false;
        }


        /**
        * @brief Determines if there is a gesture available for reading
        *
        * @return True if gesture available. False otherwise.
        */
        public bool isGestureAvailable()
        {
            uint8_t val = 0;

            /* Read value from GSTATUS register */
            if (!wireReadDataByte(APDS9960_GSTATUS, ref val))
            {
                return false;
            }

            /* Shift and mask out GVALID bit */
            val &= APDS9960_GVALID;

            /* Return true/false based on GVALID bit */
            if (val == 1)
                return true;
            else
                return false;
        }


        /**
         * @brief Configures I2C communications and initializes registers to defaults
         *
         * @return True if initialized successfully. False otherwise.
         */
        bool init()
        {
            uint8_t id = 0;

            /* Initialize I2C */

            /* Read ID register and check against known values for APDS-9960 */
            if (!wireReadDataByte(APDS9960_ID, ref id))
                return false;

            if (!(id == APDS9960_ID_1 || id == APDS9960_ID_2))
                return false;

            /* Set ENABLE register to 0 (disable all features) */
            if (!setMode(ALL, OFF))
                return false;
            /* Set default values for ambient light and proximity registers */
            if (!wireWriteDataByte(APDS9960_ATIME, DEFAULT_ATIME))
                return false;
            if (!wireWriteDataByte(APDS9960_WTIME, DEFAULT_WTIME))
                return false;
            if (!wireWriteDataByte(APDS9960_PPULSE, DEFAULT_PROX_PPULSE))
                return false;
            if (!wireWriteDataByte(APDS9960_POFFSET_UR, DEFAULT_POFFSET_UR))
                return false;
            if (!wireWriteDataByte(APDS9960_POFFSET_DL, DEFAULT_POFFSET_DL))
                return false;
            if (!wireWriteDataByte(APDS9960_CONFIG1, DEFAULT_CONFIG1))
                return false;
            if (!setLEDDrive(DEFAULT_LDRIVE))
                return false;
            if (!setProximityGain(DEFAULT_PGAIN))
                return false;
            if (!setAmbientLightGain(DEFAULT_AGAIN))
                return false;
            if (!setProxIntLowThresh(DEFAULT_PILT))
                return false;
            if (!setProxIntHighThresh(DEFAULT_PIHT))
                return false;
            if (!setLightIntLowThreshold(DEFAULT_AILT))
                return false;
            if (!setLightIntHighThreshold(DEFAULT_AIHT))
                return false;
            if (!wireWriteDataByte(APDS9960_PERS, DEFAULT_PERS))
                return false;
            if (!wireWriteDataByte(APDS9960_CONFIG2, DEFAULT_CONFIG2))
                return false;
            if (!wireWriteDataByte(APDS9960_CONFIG3, DEFAULT_CONFIG3))
                return false;
            /* Set default values for gesture sense registers */
            if (!setGestureEnterThresh(DEFAULT_GPENTH))
                return false;
            if (!setGestureExitThresh(DEFAULT_GEXTH))
                return false;
            if (!wireWriteDataByte(APDS9960_GCONF1, DEFAULT_GCONF1))
            {
                return false;
            }
            if (!setGestureGain(DEFAULT_GGAIN))
            {
                return false;
            }
            if (!setGestureLEDDrive(DEFAULT_GLDRIVE))
            {
                return false;
            }
            if (!setGestureWaitTime(DEFAULT_GWTIME))
            {
                return false;
            }
            if (!wireWriteDataByte(APDS9960_GOFFSET_U, DEFAULT_GOFFSET))
            {
                return false;
            }
            if (!wireWriteDataByte(APDS9960_GOFFSET_D, DEFAULT_GOFFSET))
            {
                return false;
            }
            if (!wireWriteDataByte(APDS9960_GOFFSET_L, DEFAULT_GOFFSET))
            {
                return false;
            }
            if (!wireWriteDataByte(APDS9960_GOFFSET_R, DEFAULT_GOFFSET))
            {
                return false;
            }
            if (!wireWriteDataByte(APDS9960_GPULSE, DEFAULT_GPULSE))
            {
                return false;
            }
            if (!wireWriteDataByte(APDS9960_GCONF3, DEFAULT_GCONF3))
            {
                return false;
            }
            if (!setGestureIntEnable(DEFAULT_GIEN))
            {
                return false;
            }
            return true;
        }

        /**
        * @brief Turns gesture-related interrupts on or off
        *
        * @param[in] enable 1 to enable interrupts, 0 to turn them off
        * @return True if operation successful. False otherwise.
        */
        bool setGestureIntEnable(uint8_t enable)
        {
            uint8_t val = 0;

            /* Read value from GCONF4 register */
            if (!wireReadDataByte(APDS9960_GCONF4, ref val))
                return false;

            /* Set bits in register to given value */
            enable &= (1); // 0b00000001;
            enable = (byte)(enable << 1);
            val &= 253;// 0b11111101;
            val |= enable;

            /* Write register value back into GCONF4 register */
            if (!wireWriteDataByte(APDS9960_GCONF4, val))
                return false;
            return true;
        }

        /**
        * @brief Reads and returns the contents of the ENABLE register
        *
        * @return Contents of the ENABLE register. 0xFF if error.
        */
        uint8_t getMode()
        {
            uint8_t enable_value = 0;

            if (!wireReadDataByte(APDS9960_ENABLE, ref enable_value))
            {
                return ERROR;
            }
            return enable_value;
        }

        /**
         * @brief Enables or disables a feature in the APDS-9960
         *
         * @param[in] mode which feature to enable
         * @param[in] enable ON (1) or OFF (0)
         * @return True if operation success. False otherwise.
         */
        bool setMode(uint8_t mode, uint8_t enable)
        {
            uint8_t reg_val;
            reg_val = getMode(); /* Read current ENABLE register */
            if (reg_val == ERROR)
                return false;

            enable = BitUtil.SetBit(enable, 1);
            /* Change bit(s) in ENABLE register */
            if (mode >= 0 && mode <= 6)
            {
                if (enable == 1)
                {
                    reg_val |= (byte)(1 << mode);
                }
                else
                {
                    reg_val &= (byte)~(1 << mode);
                }
            }
            else if (mode == ALL)
            {
                if (enable == 1)
                {
                    reg_val = 0x7F;
                }
                else
                {
                    reg_val = 0x00;
                }
            }

            /* Write value back to ENABLE register */
            if (!wireWriteDataByte(APDS9960_ENABLE, reg_val))
            {
                return false;
            }

            return true;
        }

        /**
        * @brief Sets the exit proximity threshold for gesture sensing
        *
        * @param[in] threshold proximity value needed to end gesture mode
        * @return True if operation successful. False otherwise.
        */
        bool setGestureExitThresh(uint8_t threshold)
        {
            if (!wireWriteDataByte(APDS9960_GEXTH, threshold))
                return false;
            return true;
        }

        /**
        * @brief Sets the entry proximity threshold for gesture sensing
        *
        * @param[in] threshold proximity value needed to start gesture mode
        * @return True if operation successful. False otherwise.
        */
        bool setGestureEnterThresh(uint8_t threshold)
        {
            if (!wireWriteDataByte(APDS9960_GPENTH, threshold))
                return false;
            return true;
        }

        /**
         * @brief Sets the time in low power mode between gesture detections
         *
         * Value    Wait time
         *   0          0 ms
         *   1          2.8 ms
         *   2          5.6 ms
         *   3          8.4 ms
         *   4         14.0 ms
         *   5         22.4 ms
         *   6         30.8 ms
         *   7         39.2 ms
         *
         * @param[in] the value for the wait time
         * @return True if operation successful. False otherwise.
         */
        bool setGestureWaitTime(uint8_t time)
        {
            uint8_t val = 0;
            /* Read value from GCONF2 register */
            if (!wireReadDataByte(APDS9960_GCONF2, ref val))
                return false;

            /* Set bits in register to given value */
            time &= (7);// 0b00000111;
            val &= (248);// 0b11111000;
            val |= time;

            /* Write register value back into GCONF2 register */
            if (!wireWriteDataByte(APDS9960_GCONF2, val))
                return false;
            return true;
        }

        /**
         * @brief Sets the LED drive current during gesture mode
         *
         * Value    LED Current
         *   0        100 mA
         *   1         50 mA
         *   2         25 mA
         *   3         12.5 mA
         *
         * @param[in] drive the value for the LED drive current
         * @return True if operation successful. False otherwise.
         */
        bool setGestureLEDDrive(uint8_t drive)
        {
            uint8_t val = 0;

            /* Read value from GCONF2 register */
            if (!wireReadDataByte(APDS9960_GCONF2, ref val))
                return false;

            /* Set bits in register to given value */
            drive &= (1+2);// 0b00000011;
            drive = (byte)(drive << 3);
            val &= 231;// 0b11100111;
            val |= drive;

            /* Write register value back into GCONF2 register */
            if (!wireWriteDataByte(APDS9960_GCONF2, val))
                return false;

            return true;
        }



        /**
        * @brief Sets the gain of the photodiode during gesture mode
        *
        * Value    Gain
        *   0       1x
        *   1       2x
        *   2       4x
        *   3       8x
        *
        * @param[in] gain the value for the photodiode gain
        * @return True if operation successful. False otherwise.
        */
        bool setGestureGain(uint8_t gain)
        {
            uint8_t val = 0;

            /* Read value from GCONF2 register */
            if (!wireReadDataByte(APDS9960_GCONF2, ref val))
            {
                return false;
            }

            /* Set bits in register to given value */
            gain &= (1+2); // 0b00000011;
            gain = (byte)(gain << 5);
            val &= 158;// 0b10011111;
            val |= gain;

            /* Write register value back into GCONF2 register */
            if (!wireWriteDataByte(APDS9960_GCONF2, val))
                return false;

            return true;
        }

        /**
        * @brief Writes a single byte to the I2C device and specified register
        *
        * @param[in] reg the register in the I2C device to write to
        * @param[in] val the 1-byte value to write to the I2C device
        * @return True if successful write operation. False otherwise.
        */
        bool wireWriteDataByte(uint8_t reg, uint8_t val)
        {
            return _Ii2cOut.i2c_Send2ByteCommand(reg, val);
            //Wire.beginTransmission(APDS9960_I2C_ADDR);
            //Wire.write(reg);
            //Wire.write(val);
            //if (Wire.endTransmission() != 0)
            //{
            //    return false;
            //}

            //return true;
        }


        /**
        * @brief Writes a single byte to the I2C device (no register)
        *
        * @param[in] val the 1-byte value to write to the I2C device
        * @return True if successful write operation. False otherwise.
        */
        bool wireWriteByte(uint8_t val)
        {
            return _Ii2cOut.i2c_Send1ByteCommand(val);
            //Wire.beginTransmission(APDS9960_I2C_ADDR);
            //Wire.write(val);
            //if (Wire.endTransmission() != 0)
            //{
            //    return false;
            //}
            //return true;
        }

        /**
        * @brief Reads a single byte from the I2C device and specified register
        *
        * @param[in] reg the register to read from
        * @param[out] the value returned from the register
        * @return True if successful read operation. False otherwise.
        */
        bool wireReadDataByte(uint8_t reg, ref uint8_t val)
        {
            byte[] writeBuffer = new uint8_t[1] { reg };
            byte[] readBuffer = new uint8_t[1] { 0 };
            if(_Ii2cOut.i2c_WriteReadBuffer(writeBuffer, readBuffer))
            {
                val = readBuffer[0];
                return true;
            }
            return false;

            ///* Indicate which register we want to read from */
            //if (!wireWriteByte(reg))
            //{
            //    return false;
            //}

            ///* Read from register */
            //Wire.requestFrom(APDS9960_I2C_ADDR, 1);
            //while (Wire.available())
            //{
            //    val = Wire.read();
            //}

            //return true;
        }


        /**
        * @brief Sets the LED drive strength for proximity and ALS
        *
        * Value    LED Current
        *   0        100 mA
        *   1         50 mA
        *   2         25 mA
        *   3         12.5 mA
        *
        * @param[in] drive the value (0-3) for the LED drive strength
        * @return True if operation successful. False otherwise.
        */
        bool setLEDDrive(uint8_t drive)
        {
            uint8_t val = 0;

            /* Read value from CONTROL register */
            if (!wireReadDataByte(APDS9960_CONTROL, ref val))
                return false;

            /* Set bits in register to given value */
            drive &= (2+1);// 0b00000011;
            drive = (byte)(drive << 6);
            val &= 63;// 0b00111111;
            val |= drive;

            /* Write register value back into CONTROL register */
            if (!wireWriteDataByte(APDS9960_CONTROL, val))
                return false;
            return true;
        }


        /**
        * @brief Sets the high threshold for proximity detection
        *
        * @param[in] threshold the high proximity threshold
        * @return True if operation successful. False otherwise.
        */
        bool setProxIntHighThresh(uint8_t threshold)
        {
            if (!wireWriteDataByte(APDS9960_PIHT, threshold))
                return false;
            return true;
        }


        /**
         * @brief Sets the high threshold for ambient light interrupts
         *
         * @param[in] threshold high threshold value for interrupt to trigger
         * @return True if operation successful. False otherwise.
         */
        public bool setLightIntHighThreshold(uint16_t threshold)
        {
            uint8_t val_low;
            uint8_t val_high;

            /* Break 16-bit threshold into 2 8-bit values */
            val_low = (byte)(threshold & 0x00FF);
            val_high = (byte)((threshold & 0xFF00) >> 8);

            /* Write low byte */
            if (!wireWriteDataByte(APDS9960_AIHTL, val_low))
                return false;

            /* Write high byte */
            if (!wireWriteDataByte(APDS9960_AIHTH, val_high))
                return false;
            return true;
        }

        /**
         * @brief Sets the low threshold for ambient light interrupts
         *
         * @param[in] threshold low threshold value for interrupt to trigger
         * @return True if operation successful. False otherwise.
         */
        public bool setLightIntLowThreshold(uint16_t threshold)
        {
            uint8_t val_low;
            uint8_t val_high;

            /* Break 16-bit threshold into 2 8-bit values */
            val_low  = (byte)(threshold & 0x00FF);
            val_high = (byte)((threshold & 0xFF00) >> 8);

            /* Write low byte */
            if (!wireWriteDataByte(APDS9960_AILTL, val_low))
                return false;

            /* Write high byte */
            if (!wireWriteDataByte(APDS9960_AILTH, val_high))
                return false;
            return true;
        }

        /**
        * @brief Sets the lower threshold for proximity detection
        *
        * @param[in] threshold the lower proximity threshold
        * @return True if operation successful. False otherwise.
        */
        bool setProxIntLowThresh(uint8_t threshold)
        {
            if (!wireWriteDataByte(APDS9960_PILT, threshold))
                return false;
            return true;
        }

        /**
         * @brief Resets all the parameters in the gesture data member
         */
        void resetGestureParameters()
        {
            gesture_data_.index          = 0;
            gesture_data_.total_gestures = 0;
            gesture_ud_delta_            = 0;
            gesture_lr_delta_            = 0;
            gesture_ud_count_            = 0;
            gesture_lr_count_            = 0;
            gesture_near_count_          = 0;
            gesture_far_count_           = 0;
            gesture_state_               = 0;
            gesture_motion_              = Direction.DIR_NONE;
        }

        /**
         * @brief Starts the gesture recognition engine on the APDS-9960
         *
         * @param[in] interrupts true to enable hardware external interrupt on gesture
         * @return True if engine enabled correctly. False on error.
         */
        bool enableGestureSensor(bool interrupts)
        {
            /* Enable gesture mode
               Set ENABLE to 0 (power off)
               Set WTIME to 0xFF
               Set AUX to LED_BOOST_300
               Enable PON, WEN, PEN, GEN in ENABLE 
            */
            resetGestureParameters();
            if (!wireWriteDataByte(APDS9960_WTIME, 0xFF))
            {
                return false;
            }
            if (!wireWriteDataByte(APDS9960_PPULSE, DEFAULT_GESTURE_PPULSE))
            {
                return false;
            }
            if (!setLEDBoost(LED_BOOST_300))
            {
                return false;
            }
            if (interrupts)
            {
                if (!setGestureIntEnable(1))
                {
                    return false;
                }
            }
            else
            {
                if (!setGestureIntEnable(0))
                {
                    return false;
                }
            }
            if (!setGestureMode(1))
            {
                return false;
            }
            if (!enablePower())
            {
                return false;
            }
            if (!setMode(WAIT, 1))
            {
                return false;
            }
            if (!setMode(PROXIMITY, 1))
            {
                return false;
            }
            if (!setMode(GESTURE, 1))
            {
                return false;
            }

            return true;
        }

        /**
         * @brief Tells the state machine to either enter or exit gesture state machine
         *
         * @param[in] mode 1 to enter gesture state machine, 0 to exit.
         * @return True if operation successful. False otherwise.
         */
        bool setGestureMode(uint8_t mode)
        {
            uint8_t val = 0;

            /* Read value from GCONF4 register */
            if (!wireReadDataByte(APDS9960_GCONF4, ref val))
                return false;

            /* Set bits in register to given value */
            mode &= (1);// 0b00000001;
            val &= 254;// 0b11111110;
            val |= mode;

            /* Write register value back into GCONF4 register */
            if (!wireWriteDataByte(APDS9960_GCONF4, val))
                return false;

            return true;
        }

        /**
        * Turn the APDS-9960 on
        *
        * @return True if operation successful. False otherwise.
        */
        bool enablePower()
        {
            if (!setMode(POWER, 1))
                return false;
            return true;
        }

        /**
         * Turn the APDS-9960 off
         *
         * @return True if operation successful. False otherwise.
         */
        bool disablePower()
        {
            if (!setMode(POWER, 0))
                return false;
            return true;
        }
        /**
        * @brief Sets the LED current boost value
        *
        * Value  Boost Current
        *   0        100%
        *   1        150%
        *   2        200%
        *   3        300%
        *
        * @param[in] drive the value (0-3) for current boost (100-300%)
        * @return True if operation successful. False otherwise.
        */
        bool setLEDBoost(uint8_t boost)
        {
            uint8_t val = 0;

            /* Read value from CONFIG2 register */
            if (!wireReadDataByte(APDS9960_CONFIG2, ref val))
                return false;

            /* Set bits in register to given value */
            boost &= (1+2);// 0b00000011;
            boost = (byte)(boost << 4);
            val &= 207;// 0b11001111;
            val |= boost;

            /* Write register value back into CONFIG2 register */
            if (!wireWriteDataByte(APDS9960_CONFIG2, val))
                return false;
            return true;
        }

        /**
         * @brief Reads a block (array) of bytes from the I2C device and register
         *
         * @param[in] reg the register to read from
         * @param[out] val pointer to the beginning of the data
         * @param[in] len number of bytes to read
         * @return Number of bytes read. -1 on read error.
         */
        int wireReadDataBlock(int reg, byte [] val, int len)
        {
            if (_Ii2cOut.i2c_WriteReadBuffer(
                (new List<byte>() { (byte)reg }).ToArray(),
                val
                ))
            {
                return len;
            }
            else return -1;

            //int i = 0;

            ///* Indicate which register we want to read from */
            //if (!wireWriteByte(reg))
            //{
            //    return -1;
            //}

            ///* Read block data */
            //Wire.requestFrom(APDS9960_I2C_ADDR, len);
            //while (Wire.available())
            //{
            //    if (i >= len)
            //    {
            //        return -1;
            //    }
            //    val[i] = Wire.read();
            //    i++;
            //}

            //return i;
        }

        bool setAmbientLightIntEnable(bool enable)
        {
            return setAmbientLightIntEnable((byte)(enable ? 1 : 0));
        }
        /**
         * @brief Turns ambient light interrupts on or off
         *
         * @param[in] enable 1 to enable interrupts, 0 to turn them off
         * @return True if operation successful. False otherwise.
         */
        bool setAmbientLightIntEnable(uint8_t enable)
        {
            uint8_t val = 0;

            /* Read value from ENABLE register */
            if (!wireReadDataByte(APDS9960_ENABLE, ref val))
            {
                return false;
            }

            /* Set bits in register to given value */
            enable &= 1;// 0b00000001;
            enable = (byte)(enable << 4);
            val &= 239;// 0b11101111;
            val |= enable;

            /* Write register value back into ENABLE register */
            if (!wireWriteDataByte(APDS9960_ENABLE, val))
                return false;
            return true;
        }



        /**
        * @brief Starts the light (R/G/B/Ambient) sensor on the APDS-9960
        *
        * @param[in] interrupts true to enable hardware interrupt on high or low light
        * @return True if sensor enabled correctly. False on error.
        */
        public bool enableLightSensor(bool interrupts)
        {

            /* Set default gain, interrupts, enable power, and enable sensor */
            if (!setAmbientLightGain(DEFAULT_AGAIN))
            {
                return false;
            }
            if (interrupts)
            {
                if (!setAmbientLightIntEnable(1))
                {
                    return false;
                }
            }
            else
            {
                if (!setAmbientLightIntEnable(0))
                {
                    return false;
                }
            }
            if (!enablePower())
            {
                return false;
            }
            if (!setMode(AMBIENT_LIGHT, 1))
            {
                return false;
            }

            return true;

        }

        /**
        * @brief Sets the receiver gain for the ambient light sensor (ALS)
        *
        * Value    Gain
        *   0        1x
        *   1        4x
        *   2       16x
        *   3       64x
        *
        * @param[in] drive the value (0-3) for the gain
        * @return True if operation successful. False otherwise.
        */
        bool setAmbientLightGain(uint8_t drive)
        {
            uint8_t val = 0;
            if (!wireReadDataByte(APDS9960_CONTROL, ref val)) /* Read value from CONTROL register */
                return false;

            /* Set bits in register to given value */
            drive &= (2+1);// 0b00000011;
            val &= 252;// 0b11111100;
            val |= drive;
            
            if (!wireWriteDataByte(APDS9960_CONTROL, val)) /* Write register value back into CONTROL register */
                return false;
            return true;
        }

        /**
         * @brief Gets the low threshold for ambient light interrupts
         *
         * @param[out] threshold current low threshold stored on the APDS-9960
         * @return True if operation successful. False otherwise.
         */
        bool getLightIntLowThreshold(ref uint16_t threshold)
        {
            uint8_t val_byte = 0;
            threshold        = 0;

            /* Read value from ambient light low threshold, low byte register */
            if (!wireReadDataByte(APDS9960_AILTL, ref val_byte))
                return false;
            threshold = val_byte;

            /* Read value from ambient light low threshold, high byte register */
            if (!wireReadDataByte(APDS9960_AILTH, ref val_byte))
                return false;
            threshold = (byte)(threshold + ((uint16_t)val_byte << 8));
            return true;
        }

        /**
 * @brief Sets the receiver gain for proximity detection
 *
 * Value    Gain
 *   0       1x
 *   1       2x
 *   2       4x
 *   3       8x
 *
 * @param[in] drive the value (0-3) for the gain
 * @return True if operation successful. False otherwise.
 */
        bool setProximityGain(uint8_t drive)
        {
            uint8_t val = 0;
            if (!wireReadDataByte(APDS9960_CONTROL, ref val)) /* Read value from CONTROL register */
                return false;

            /* Set bits in register to given value */
            drive &= (2+1);// 0b00000011;
            drive = (byte)(drive << 2);
            val &= 243;// 0b11110011;
            val |= drive;

            /* Write register value back into CONTROL register */
            if (!wireWriteDataByte(APDS9960_CONTROL, val))
                return false;
            return true;
        }
        
        Ii2cOut _Ii2cOut
        {
            get
            {
                return (Ii2cOut)this;
            }
        }

        //MadeInTheUSB.Components.Interface.Ii2cOut
        bool Ii2cOut.i2c_Send1ByteCommand(byte c)
        {
            return this._i2c.Send1ByteCommand(c);
        }

        bool Ii2cOut.i2c_Send2ByteCommand(byte c0, byte c1)
        {
            return this._i2c.Send2BytesCommand(c0, c1);
        }

        bool Ii2cOut.i2c_WriteBuffer(byte[] buffer)
        {
            return this._i2c.WriteBuffer(buffer);
        }

        bool Ii2cOut.i2c_WriteReadBuffer(byte[] writeBuffer, byte[] readBuffer)
        {
            if (writeBuffer.Length == 1 && readBuffer.Length == 1)
            {
                readBuffer[0] =(byte) this._i2c.Send1ByteRead1Byte(writeBuffer[0]);
                return true;
            }
            else if (writeBuffer.Length == 1 && readBuffer.Length > 1)
            {
                if(this._i2c.Send1ByteCommand(writeBuffer[0]))
                    return this._i2c.ReadBuffer(readBuffer.Length, readBuffer);
                return false;
            }
            return false;
        }
    }
}