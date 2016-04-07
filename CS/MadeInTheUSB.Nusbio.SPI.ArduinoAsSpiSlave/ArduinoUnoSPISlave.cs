using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.spi;

namespace MadeInTheUSB
{
    
    public class ArduinoUnoSPISlave
    {
        private const int API_SUCCESS_CODE = 1;

        public const int MAX_BUFFER_TRANSFERT_SIZE = 128;

        public const byte END_OF_COMMAND = 10;

        [Flags]
        public enum ArduinoType : byte  
        {
            ArduinoUno  = 1,
            ATtiny85    = 2,
            Unknown     = 128
        }

        public ArduinoType Type;

        public SPIEngine _spiEngine;

        /// <summary>
        /// Arduino Uno 0 to 13 for now
        /// </summary>
        public int MaxGpioIndex
        {
            get
            {
                switch (this.Type)
                {
                    case ArduinoType.ArduinoUno :
                        return 9; // Gpio 10, 11, 12, 13 are reserved for the SPI communication
                }
                throw new AggregateException("Invalid type");
            }
        }

        public int MaxAnalogIndex
        {
            get
            {
                switch (this.Type)
                {
                    case ArduinoType.ArduinoUno : return 5; // Analog 0,1,2,3,4,5
                }
                throw new AggregateException("Invalid type");
            }
        }

        
        public int MinAnalogIndex
        {
            get
            {
                switch (this.Type)
                {
                    case ArduinoType.ArduinoUno : return 0; // Analog 0,1,2,3,4,5
                }
                throw new AggregateException("Invalid type");
            }
        }

        public int MinGpioIndex
        {
            get
            {
                switch (this.Type)
                {
                    case ArduinoType.ArduinoUno :
                        return 2; // Gpio 0, 1 and reserved for UART tx,rx
                }
                throw new AggregateException("Invalid type");
            }
        }

        /// <summary>
        /// Arduino Uno SPI Pin
        ///  CS      10
        ///  MOSI    11
        ///  MISO    12
        ///  CLOCK   13  
        /// </summary>
        /// <param name="nusbio"></param>
        /// <param name="selectGpio"></param>
        /// <param name="mosiGpio"></param>
        /// <param name="misoGpio"></param>
        /// <param name="clockGpio"></param>
        public ArduinoUnoSPISlave(ArduinoType type, Nusbio nusbio, 
            NusbioGpio selectGpio, 
            NusbioGpio mosiGpio, 
            NusbioGpio misoGpio, 
            NusbioGpio clockGpio 
            )
        {
            this.Type = type;
                this._spiEngine = new SPIEngine(nusbio,
                    selectGpio,
                    mosiGpio,
                    misoGpio,
                    clockGpio
                    );
        }
        
        public  int ArduinoApi(string cmd, List<byte> buffer2 = null)
        {
            int v1 = -1, v2 = -1;

            List<byte> buffer = null;
            if (cmd == null)
            {
                buffer = buffer2;
                if(buffer[buffer.Count-1] != 13  && buffer[buffer.Count-1] != 10)
                    buffer.Add(13); // \r  means end of buffer
            }
            else
            {
                buffer = ASCIIEncoding.ASCII.GetBytes(cmd + "\n").ToList();
            }


            //var r = _spiEngine.Transfer(buffer); // Not optimized at all
            //_spiEngine.TransferNoMiso(false, buffer); // Compact data line, but Select/UnSelect use gpio 2ms latency
            var packagedBuffers = new List<SPIEngine.PackagedBuffer>() {
                new SPIEngine.PackagedBuffer() { Buffer = buffer }
            };
            _spiEngine.TransferNoMiso(0, false, packagedBuffers); // Compact data line, and Select/UnSelect are part of bitbanging buffer

            // Send dummy byte 0 to read answer byte 0
            // Getting the response slowdown the speed from 10k to 7k per seconds
            // But Nusbio.BaudRate = 230400 we do not lost chars and 7 to 10k
            var r2 = _spiEngine.Transfer(new List<byte>() { 0 }); // dummy
            if (r2.Succeeded) 
                v1 = r2.ReadBuffer[0];
            return v1; 
        }

        public bool SetPinMode(int pin, PinMode pinMode)
        {
            return ArduinoApi(string.Format("pm{0}{1}", pin.ToString("X"), (byte) pinMode)) == API_SUCCESS_CODE;
        }

        public int DigitalRead(int pin)
        {
            return ArduinoApi(string.Format("dr{0}", pin.ToString("X")));
        }

        public int AnalogWrite(int pin, byte val)
        {
            char c = (char)val;
            return ArduinoApi(string.Format("aw{0}{1}", pin.ToString("X"), c));
        }

        public int DigitalWrite(int pin, PinState pinState)
        {
            // DigitalRead 23456789ABCD
            return ArduinoApi(string.Format("dw{0}{1}", pin.ToString("X"), pinState == PinState.High ? 1:0));
        }

        public int AnalogRead(int pin)
        {
            return ArduinoApi(string.Format("ar{0}", pin.ToString("X")));
        }

        public bool SendBuffer(List<byte> buffer)
        {
            return ArduinoApi(null, buffer) == API_SUCCESS_CODE;
        }

        public bool NeoPixelInit(int stripIndex, int pin, int maxLed)
        {
            var buffer = new List<byte>();
            buffer.Add(110); // n
            buffer.Add(105); // i
            buffer.Add((byte)stripIndex);
            buffer.Add((byte)pin);
            buffer.Add((byte)maxLed);
            buffer.Add(END_OF_COMMAND);
            return ArduinoApi(null, buffer) == API_SUCCESS_CODE;
        }

        public bool NeoPixelSet(int stripIndex, int brigthness, int startIndex, int ledCount, params byte [] rgbs)
        {
            var buffer = new List<byte>();
            buffer.Add(110); // n
            buffer.Add(115); // s
            buffer.Add((byte)brigthness);
            buffer.Add((byte)startIndex);
            buffer.Add((byte)ledCount);

            if (startIndex >= 255)
                throw new ArgumentException(string.Format("startIndex cannot be greater than 255" , startIndex, ledCount));

            var maxLedSetInOneOperation = ((MAX_BUFFER_TRANSFERT_SIZE - 1) / 3); // -1 because of the \n and 3 because for each led we have rgb
            if (ledCount >= maxLedSetInOneOperation)
                throw new ArgumentException(string.Format("ledCount cannot be greater than {0}" , maxLedSetInOneOperation));
            
            foreach(var c in rgbs)
                buffer.Add(c);

            buffer.Add(END_OF_COMMAND);

            if (buffer.Count > MAX_BUFFER_TRANSFERT_SIZE)
            {
                // Fix it by adding an index
                throw new ArgumentException("Buffer too big");
            }

            return ArduinoApi(null, buffer) == API_SUCCESS_CODE;
        }
    }
}
