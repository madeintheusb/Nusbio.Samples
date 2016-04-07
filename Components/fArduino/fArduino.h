// fArduino.h
// Use the NewPing Library - v1.5 - 08/15/2012

#ifndef _FARDUINO_h

    #define _FARDUINO_h

    #if defined(ARDUINO) && ARDUINO >= 100
        #include "Arduino.h"
    #else
        #include "WProgram.h"
    #endif

    // --- fArduino LIBRARY COMPILATION MODE ---

    //#define TRINKET 1 // Pins information for Trinket https://learn.adafruit.com/introducing-trinket/pinouts
    //#define TRINKET_PRO 1
    #define ARDUINO_UNO 1

    #if defined(TRINKET)
        #define EEPROM_SIZE 512
    #endif
    #if defined(TRINKET_PRO)
        #define EEPROM_SIZE 1024
        #define SERIAL_AVAILABLE 1
        #define TRACE_DELAY 20
    #endif
    #if defined(ARDUINO_UNO)
        #define EEPROM_SIZE 1024
        #define SERIAL_AVAILABLE 1
        #define TRACE_DELAY 10
    #endif

//1024 bytes on the ATmega328
//512 bytes on the ATmega168 and ATmega8 
//4 KB(4096 bytes) on the ATmega1280 and ATmega2560.

    // #define LIGHTSENSORBUTTON_DEBUG 1
    // --- fArduino LIBRARY COMPILATION MODE ---

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// EEPROMClassEx
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #define ABOUT_TO_CALL_EEPROM_API while(!eeprom_is_ready());cli();
    #define DONE_CALLING_EEPROM_API sei();

    /*
        https://raw.githubusercontent.com/thijse/Arduino-Libraries/master/EEPROMEx/EEPROMex.h

        EEPROMEx.h - Extended EEPROM library
        Copyright (c) 2012 Thijs Elenbaas.  All right reserved.

        This library is free software; you can redistribute it and/or
        modify it under the terms of the GNU Lesser General Public
        License as published by the Free Software Foundation; either
        version 2.1 of the License, or (at your option) any later version.

        This library is distributed in the hope that it will be useful,
        but WITHOUT ANY WARRANTY; without even the implied warranty of
        MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
        Lesser General Public License for more details.

        You should have received a copy of the GNU Lesser General Public
        License along with this library; if not, write to the Free Software
        Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
    */
    #ifndef EEPROMEX_h
        #define EEPROMEX_h

        #if ARDUINO >= 100
            #include <Arduino.h> 
        #else
            #include <WProgram.h> 
        #endif
        #include <inttypes.h>
        #include <avr/eeprom.h>

        // Boards with ATmega328, Duemilanove, Uno, UnoSMD, Lilypad - 1024 bytes (1 kilobyte)
        // Boards with ATmega1280 or 2560, Arduino Mega series ? 4096 bytes (4 kilobytes)
        // Boards with ATmega168, Lilypad, old Nano, Diecimila  ? 512 bytes (1/2 kilobyte)

        #define EEPROMSizeATmega168   512     
        #define EEPROMSizeATmega328   1024     
        #define EEPROMSizeATmega1280  4096     
        #define EEPROMSizeATmega32u4  1024
        #define EEPROMSizeAT90USB1286 4096
        #define EEPROMSizeMK20DX128   2048

        #define EEPROMSizeUno         EEPROMSizeATmega328     
        #define EEPROMSizeUnoSMD      EEPROMSizeATmega328
        #define EEPROMSizeLilypad     EEPROMSizeATmega328
        #define EEPROMSizeDuemilanove EEPROMSizeATmega328
        #define EEPROMSizeMega        EEPROMSizeATmega1280
        #define EEPROMSizeDiecimila   EEPROMSizeATmega168
        #define EEPROMSizeNano        EEPROMSizeATmega168
        #define EEPROMSizeTeensy2     EEPROMSizeATmega32u4
        #define EEPROMSizeLeonardo    EEPROMSizeATmega32u4
        #define EEPROMSizeMicro       EEPROMSizeATmega32u4
        #define EEPROMSizeYun         EEPROMSizeATmega32u4
        #define EEPROMSizeTeensy2pp   EEPROMSizeAT90USB1286
        #define EEPROMSizeTeensy3     EEPROMSizeMK20DX128

        class EEPROMClassEx
        {
            public:
                EEPROMClassEx               (                     );
                bool     isReady            (                     );
                int      writtenBytes       (                     );
                void     setMemPool         (int base, int memSize);
                void     setMaxAllowedWrites(int allowedWrites    );
                int      getAddress         (int noOfBytes        );

                uint8_t  read               (int                  );
                bool     readBit            (int, byte            );
                uint8_t  readByte           (int                  );
                uint16_t readInt            (int                  );
                uint32_t readLong           (int                  );
                float    readFloat          (int                  );
                double   readDouble         (int                  );

                bool     write              (int, uint8_t         );
                bool     writeBit           (int, uint8_t, bool   );
                bool     writeByte          (int, uint8_t         );
                bool     writeInt           (int, uint16_t        );
                bool     writeLong          (int, uint32_t        );
                bool     writeFloat         (int, float           );
                bool     writeDouble        (int, double          );

                bool     update             (int, uint8_t         );
                bool     updateBit          (int, uint8_t, bool   );
                bool     updateByte         (int, uint8_t         );
                bool     updateInt          (int, uint16_t        );
                bool     updateLong         (int, uint32_t        );
                bool     updateFloat        (int, float           );
                bool     updateDouble       (int, double          );

                // Use template for other data formats

                template <class T> int readBlock(int address, const T value[], int items)
                {
                    if (!isWriteOk(address + items*sizeof(T))) return 0;
                    unsigned int i;
                    for (i = 0; i < (unsigned int)items; i++)
                        readBlock<T>(address + (i*sizeof(T)), value[i]);
                    return i;
                }

                template <class T> int readBlock(int address, const T& value)
                {
                    eeprom_read_block((void*)&value, (const void*)address, sizeof(value));
                    return sizeof(value);
                }

                template <class T> int writeBlock(int address, const T value[], int items)
                {
                    if (!isWriteOk(address + items*sizeof(T))) return 0;
                    unsigned int i;
                    for (i = 0; i < (unsigned int)items; i++)
                        writeBlock<T>(address + (i*sizeof(T)), value[i]);
                    return i;
                }

                template <class T> int writeBlock(int address, const T& value)
                {
                    if (!isWriteOk(address + sizeof(value))) return 0;
                    eeprom_write_block((void*)&value, (void*)address, sizeof(value));
                    return sizeof(value);
                }

                template <class T> int updateBlock(int address, const T value[], int items)
                {
                    int writeCount = 0;
                    if (!isWriteOk(address + items*sizeof(T))) return 0;
                    unsigned int i;
                    for (i = 0; i < (unsigned int)items; i++)
                        writeCount += updateBlock<T>(address + (i*sizeof(T)), value[i]);
                    return writeCount;
                }

                template <class T> int updateBlock(int address, const T& value)
                {
                    int writeCount = 0;
                    if (!isWriteOk(address + sizeof(value))) return 0;
                    const byte* bytePointer = (const byte*)(const void*)&value;
                    for (unsigned int i = 0; i < (unsigned int)sizeof(value); i++) {
                        if (read(address) != *bytePointer) {
                            write(address, *bytePointer);
                            writeCount++;
                        }
                        address++;
                        bytePointer++;
                    }
                    return writeCount;
                }

            private:
                //Private variables
                static int _base;
    
                static int _nextAvailableaddress;
                static int _writeCounts;
                int _allowedWrites;
                bool checkWrite(int base, int noOfBytes);
                bool isWriteOk(int address);
                bool isReadOk(int address);

            public:
                static int _memSize;
        };

        extern EEPROMClassEx EEPROM;

    #endif

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// MemDB
    /// - Manage byte array saved in EEPROM
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class MemDB {

        private:
            int _size;
            int _index;
            int _startAddress;
            int _lengthAddress;    
        protected:
        public:
            static void InitEEPROM(int maxEEPROMMemory, int maxAllowedWrites);
            MemDB();
            ~MemDB();

            int  CreateString(int size);
            void SetString(String s);
            String GetString();

            int  CreateByteArray(int size, boolean init);
            byte AddByteArray(byte b);
            byte GetByteArray(int index);
            int  ClearByteArray();
            int  GetLength();
            int  SetLength();

            String ToString();
            void ToSerial();
    };

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// StringFormatClass
    /// Create a sigleton object named StringFormat exposing method to format string
    /// The formating follow the sprinf syntax http://www.tutorialspoint.com/c_standard_library/c_function_sprintf.htm
    /// with some extended syntax for unsigned int and long and boolean
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class StringFormatClass {
        public:
            String Format(char *format, ...);
            String GetTime();
            String PadRight(String source, char * padding, int max);
            String PadLeft(String source, char * padding, int max);
            String MakeString(char * padding, int max);
            boolean IsDigit(char *format);
    };
    extern StringFormatClass StringFormat;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// WindowsCommand
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    struct WindowsCommand {

        String Command;
    };

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Board
    /// Represet the Trinket/Arduino board
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    #define ArraySize(x) sizeof(x) / sizeof(x[0])
    #define UNDEFINED_PIN -1
    #define MAX_FORMAT_SIZE 128
    #define TRACE_HEADER_CHAR "~"

 
    class BoardClass {
    private:
        boolean _serialCommunicationInitialized;
        unsigned long _startTime;
    protected:
    public:
        BoardClass();
        ~BoardClass();

        //char * GetTime();
        //String Format(char *format, ...);
        int GetFreeMemory();

        int GetEEPROMSize();

        int RoundDouble(double d);

        WindowsCommand GetWindowsConsoleCommand();
        void SendWindowsConsoleCommand(String command, boolean newLine = true, boolean asynchronous = false);

        boolean GetButtonStateDebounced(int pin, boolean lastState);
        void LedOn(int pin, boolean state);
        void LedOn(int pin, boolean state, int delay);
        void LedSet(int pin, int level);
        void SetPinMode(uint8_t, uint8_t);
        void Delay(unsigned long);

        void InitializeComputerCommunication(unsigned long speed, char * message);

        void TraceNoNewLine(char * msg);
        void TraceNoNewLine(const char * msg);
        void TraceNoNewLine(const String &);

        void Trace(char * msg, boolean printTime = true);
        void Trace(const char * msg, boolean printTime = true);
        void Trace(String msg, boolean printTime = true);

        void TraceHeader(char * msg);
        void TraceFormat(char * format, int d1);
        void TraceFormat(char * format, int d1, int d2);
        void TraceFormat(char * format, char *s);
        void TraceFormat(char * format, char *s1, char *s2);
        void TraceFormat(char * format, char *s1, char *s2, char * s3);
        void TraceFormat(char * format, char d1);
        void TraceFormat(char * format, float f1);
        void TraceFormat(char * format, double f1, double f2);

        void ClearKeyboard();

        bool InBetween(int newValue, int refValue, int plusOrMinuspercent);
        bool InBetween(double newValue, double refValue, double plusOrMinuspercent);

        //char * ToString(double d);
        //char * ToString(float f);
    };

    // Global Signleton
    extern BoardClass Board;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Led
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class Led {

        private:
            int _pin;
            unsigned long _rate;
            boolean _state;
            int _level;
            unsigned long _blinkStartTime;

        public:
            bool State;
            Led(int pin);
            void SetState(boolean on);
            void SetLevel(int level);
            ~Led();
            void SetBlinkMode(unsigned long rate);
            unsigned long GetBlinkDurationCycle();
            void SetBlinkModeOff();
            boolean IsBlinking();
            void Blink();
            void Blink(int blinkCount, int waitTime);
    };

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// MultiState Button
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class MultiStateButton {

        private:
            int         _pin;
            int         _previousPin;
            int         _maxState;
            const int  *_ledIntensityArray;  // Point to an array of int mapping the intensity of the led with the StateIndex
            bool        _statedChanged;

        public:
            Led     *LedInstance;
            int     StateIndex;

            boolean NextButtonLastStateInLoop;
            boolean PreviousButtonLastStateInLoop;

            MultiStateButton(int pin, Led * led, int maxState, const int * ledIntensityArray);
            ~MultiStateButton();

            boolean GetButtonStateDebounced();
            boolean GetPreviousButtonStateDebounced();
            void NextState();
            void PreviousState();
            void UpdateUI();
            void SetPreviousButton(int pin);

            boolean StateChangeFor(int state);
    };

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// TimeOut
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class TimeOut {

        private:
            unsigned long _time;
            unsigned long _duration;

        public:
            unsigned long Counter;

            TimeOut(unsigned long duration);
            ~TimeOut();
            void Reset();
            boolean IsTimeOut();
            boolean EveryCalls(unsigned long callCount);
            String ToString();
    };
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///  Temperature Manager
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class TemperatureManager {

        private:

        public:
            TemperatureManager();
            ~TemperatureManager();
            float CelsiusToFahrenheit(float celsius);
            void Add(float celsius);
    };

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    /// SpeakerManager
    ///
    /// based on http://www.instructables.com/id/Arduino-Basics-Making-Sound/step2/Playing-A-Melody/
    /// http://makezine.com/projects/make-35/advanced-arduino-sound-synthesis/
    /// 
    /// Remark from Jeremy Blum about using the default Arduino tone() method
    /// http://www.jeremyblum.com/2010/09/05/driving-5-speakers-simultaneously-with-an-arduino
    /// The built-in tone() function allows you to generate a squarewave with 50% duty cycle of your
    /// selected frequency on any pin on the arduino.
    /// It relies on one of the arduino?s 3 timers to work in the background.
    /// 
    /// SPECIFICALLY, IT USES TIMER2, WHICH IS ALSO RESPONSIBLE FOR
    /// CONTROLLING PWM ON PINS 3 AND 11.
    /// SO YOU NATURALLY LOOSE THAT ABILITY WHEN USING THE TONE() FUNCTION.
    ///
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Speaker Notes
    #define NOTE_B0  31
    #define NOTE_C1  33
    #define NOTE_CS1 35
    #define NOTE_D1  37
    #define NOTE_DS1 39
    #define NOTE_E1  41
    #define NOTE_F1  44
    #define NOTE_FS1 46
    #define NOTE_G1  49
    #define NOTE_GS1 52
    #define NOTE_A1  55
    #define NOTE_AS1 58
    #define NOTE_B1  62
    #define NOTE_C2  65
    #define NOTE_CS2 69
    #define NOTE_D2  73
    #define NOTE_DS2 78
    #define NOTE_E2  82
    #define NOTE_F2  87
    #define NOTE_FS2 93
    #define NOTE_G2  98
    #define NOTE_GS2 104
    #define NOTE_A2  110
    #define NOTE_AS2 117
    #define NOTE_B2  123
    #define NOTE_C3  131
    #define NOTE_CS3 139
    #define NOTE_D3  147
    #define NOTE_DS3 156
    #define NOTE_E3  165
    #define NOTE_F3  175
    #define NOTE_FS3 185
    #define NOTE_G3  196
    #define NOTE_GS3 208
    #define NOTE_A3  220
    #define NOTE_AS3 233
    #define NOTE_B3  247
    #define NOTE_C4  262
    #define NOTE_CS4 277
    #define NOTE_D4  294
    #define NOTE_DS4 311
    #define NOTE_E4  330
    #define NOTE_F4  349
    #define NOTE_FS4 370
    #define NOTE_G4  392
    #define NOTE_GS4 415
    #define NOTE_A4  440
    #define NOTE_AS4 466
    #define NOTE_B4  494
    #define NOTE_C5  523
    #define NOTE_CS5 554
    #define NOTE_D5  587
    #define NOTE_DS5 622
    #define NOTE_E5  659
    #define NOTE_F5  698
    #define NOTE_FS5 740
    #define NOTE_G5  784
    #define NOTE_GS5 831
    #define NOTE_A5  880
    #define NOTE_AS5 932
    #define NOTE_B5  988
    #define NOTE_C6  1047
    #define NOTE_CS6 1109
    #define NOTE_D6  1175
    #define NOTE_DS6 1245
    #define NOTE_E6  1319
    #define NOTE_F6  1397
    #define NOTE_FS6 1480
    #define NOTE_G6  1568
    #define NOTE_GS6 1661
    #define NOTE_A6  1760
    #define NOTE_AS6 1865
    #define NOTE_B6  1976
    #define NOTE_C7  2093
    #define NOTE_CS7 2217
    #define NOTE_D7  2349
    #define NOTE_DS7 2489
    #define NOTE_E7  2637
    #define NOTE_F7  2794
    #define NOTE_FS7 2960
    #define NOTE_G7  3136
    #define NOTE_GS7 3322
    #define NOTE_A7  3520
    #define NOTE_AS7 3729
    #define NOTE_B7  3951
    #define NOTE_C8  4186
    #define NOTE_CS8 4435
    #define NOTE_D8  4699
    #define NOTE_DS8 4978
    #define NOTE_SILENCE 0

    #define SPEAKERMANAGER_PLAY_SEQUENCE_NORMAL 1
    #define SPEAKERMANAGER_PLAY_SEQUENCE_SLOW   2
    #define SPEAKERMANAGER_PLAY_SEQUENCE_REVERSE true

    #define _1_NOTE 1
    #define _2_NOTE 2
    #define _4_NOTE 4
    #define _8_NOTE 8

    class SpeakerManager {

        private:
            byte _pin;

            int           _backGroundNoteDurationIndex;
            int *         _backGroundNoteDurationSequence;
            int           _backGroundNoteDurationSequenceSize;

        public:
            boolean       BackGroundOn;

            SpeakerManager(byte pin);
            ~SpeakerManager();
            void Play(int note, int duration);
            void Play(int note, int duration, int speed);
            void Play(int note, int duration, int speed, boolean stop);
            void Off();
            void PlaySequence(int size, int * noteDurationSequence);
            void PlaySequence(int size, int * noteDurationSequence, int speed);
            void PlaySequence(int size, int * noteDurationSequence, int speed, boolean reverse);
            void Tone(int note, int duration);

            void StartSequenceBackGround(int size, int * noteDurationSequence);
            boolean BackGroundUpdate();
            void StartBackgroundNote();
            void StopBackGroundSequence();
    };



    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// LightSensorButton
    /// 
    ///   - When used with a 1k resistor, simply passing the hand over the button 
    ///   will trigger the event.
    ///   - When used with a 10k resistor, the user will have to put the finger on
    ///   the light sensor
    ///   - Possiblity to implement : Put the finger on the button (FingerDown),
    ///   wait 3 seconds, remove finger
    /// 
    ///   https://learn.adafruit.com/photocells/using-a-photocell
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class LightSensorButton {

        private:
            byte          _pin;    
            unsigned long _lastReferenceTime;
            int           _lastDifference;
            double        _lastChangeInPercent;
            int           _lastValue;
            int           _referenceValue;

            boolean ChangeDetected();
            boolean BackToReferenceValue();

        public:
            byte UPDATE_REFERENCE_EVERY_X_SECONDS; // Update light reference every 15 seconds
            byte MAX_REFERENCES;                   // Capture the average of 3 light value to compute the lght reference
            byte REFERENCE_ACQUISITION_TIME;
            byte DETECTION_PERCENT;                // If the light change more than +-24% we detected a possible change
            byte DETECTION_PERCENT_BACK;           // if the light go back +-ReferenceValue, the button was activated by a human (Hopefully, not 100% guaranteed)
            byte DETECTION_BACK_WAIT_TIME;         // Wait time before we check if the light value went back to the reference value

            String Name;

            // Set to true to notify we need to update the light reference
            boolean NeedReference;

            LightSensorButton(byte pin, char * name);
            ~LightSensorButton();

            // Compute and return the new reference light value
            int UpdateReferences();

            // Return true if it is time to update the reference light
            boolean ReferenceTimeOut();

            // Return true if the button was activated
            boolean Activated();
    
            // Return internal information about the state of button
            String ToString();
            boolean FingerUp();
            boolean FingerDown();
    };




    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Piezo
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class Piezo {

        private:
            byte    _pin;
            int     _threshHold;
            int     _maxCalibratedValue;
            boolean _debug;
            boolean _ready;
        public:
            byte MaxMidiVelocity;
            Piezo(int pin, int threshHold, int maxCalibratedValue, int maxMidiVelocity);
            int GetValue();
            int GetTimeValue();
            void WaitForRebound();
    };

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Register74HC595_8Bit
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class Register74HC595_8Bit {

        private:
            int _latchPin;   // Pin connected to ST_CP of 74HC595
            int _clockPin;  // Pin connected to SH_CP of 74HC595
            int _dataPin;  // Pin connected to DS of 74HC595
        public:

        Register74HC595_8Bit(int latchPin, int clockPin, int dataPin);
        void Send8BitValue(int v);
        void DisplayNumberFrom0To8(int v);
        void DisplayNumberFrom0To8Reverse(int v);
        void FlashNumberFrom0To8(int v, int flashCount = 3, int waitTime = 400);
        void FlashValue(int v, int flashCount = 4, int waitTime = 250);
        void AnimateAllLeftToRight(int waitTime);
        void AnimateAllRightToLeft(int waitTime);
        //void AnimateAllLeftToRightAndRightToLeft(int waitTime);
        void AnimateOneLeftToRightAndRightToLeft(int waitTime);
        void AnimateEveryOther(int flashCount, int waitTime);
    };
   

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Register74HC595_16Bit
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class Register74HC595_16Bit {

        private:
            int _latchPin;   // Pin connected to ST_CP of 74HC595
            int _clockPin;  // Pin connected to SH_CP of 74HC595
            int _dataPin;  // Pin connected to DS of 74HC595
        public:

            Register74HC595_16Bit(int latchPin, int clockPin, int dataPin);
            void Send16BitValue(unsigned int v);
            void AnimateOneLeftToRightAndRightToLeft2Leds(int waitTime, int count);
            void AnimateOneLeftToRightAndRightToLeft1Leds(int waitTime, int count);
    };

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// RadioShackPIRSensor
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class RadioShackPIRSensor {

        private:
            int _pin;
        public:

            RadioShackPIRSensor(int pin);
            boolean MotionDetected();
    };

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// PullUpButton
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class PullUpButton {

        private:
            int         _pin;
            int         _previousInput;

        public:
            PullUpButton(int pin);
            boolean IsPressed();
    };

    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// UltrasonicDistanceSensor
    /// HcSr04
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class UltrasonicDistanceSensor {
        private:
            uint8_t _triggerPin, _echoPin;
            int MaxCmDistance;
    public:
        UltrasonicDistanceSensor(uint8_t triggerPin, uint8_t echoPin, int maxCmDistance = 400);
        int Ping();
    };
        
#endif