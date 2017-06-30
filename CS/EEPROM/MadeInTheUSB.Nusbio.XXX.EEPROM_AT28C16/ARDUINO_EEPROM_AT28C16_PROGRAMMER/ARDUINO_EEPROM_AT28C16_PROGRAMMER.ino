/*
 * https://raw.githubusercontent.com/beneater/eeprom-programmer/master/eeprom-programmer.ino
*/
#define SHIFT_DATA 2
#define SHIFT_CLK 3
#define SHIFT_LATCH 4
#define EEPROM_D0 5
#define EEPROM_D7 12
#define WRITE_EN 13 // Active Low

#define OUT_EN_BIT 0x80 // 16 gpio extender 8..15 16..23 -> connect to gpio 23
#define READ_EN 12 // Active Low

#define DEFAULT_ERASE_VALUE 0x56

#define ADDR_OFFSET 0

void setOutEnable(bool state) {
  if(state)
    digitalWrite(READ_EN, LOW);
  else
    digitalWrite(READ_EN, true);
}

/*
 * Output the address bits and outputEnable signal using shift registers.
 */
void setAddress(int address, bool outputEnable) {
  
  shiftOut(SHIFT_DATA, SHIFT_CLK, MSBFIRST, (address >> 8) | (outputEnable ? 0x00 : OUT_EN_BIT));
  shiftOut(SHIFT_DATA, SHIFT_CLK, MSBFIRST, address);

  digitalWrite(SHIFT_LATCH, LOW);
  delay(1);
  digitalWrite(SHIFT_LATCH, HIGH);
  delay(1);
  digitalWrite(SHIFT_LATCH, LOW);
  delay(20);
}

void SetDataPinInInputMode() 
{ 
  for (int pin = EEPROM_D0; pin <=EEPROM_D7; pin += 1) 
  {
    pinMode(pin, INPUT);
  }
  delay(25);
}
void SetDataPinInOutputMode() {
  for (int pin = EEPROM_D0; pin <=EEPROM_D7; pin += 1) {
    pinMode(pin, OUTPUT);
    digitalWrite(pin, LOW);
  }
  delay(25);
}
/*
 * Read a byte from the EEPROM at the specified address.
 */
byte __readEEPROM(int address) {

  //Serial.print("__readEEPROM [");Serial.print(address);Serial.println("]");delay(20);
  byte data = 0;
  for (int pin = EEPROM_D7; pin >= EEPROM_D0; pin -= 1) 
  {
    int pinVal = digitalRead(pin);    
    data = (data << 1) + pinVal;    
    Serial.print("pin:");Serial.print(pin);
    Serial.print(" pinVal:");Serial.print(pinVal);
    Serial.print(" data:");Serial.println(data);
    delay(20);
    delay(20);
  }
  return data;
}

byte readEEPROM(int address) {

  SetDataPinInInputMode();
  
  setAddress(address, /*outputEnable*/ true);  
  byte v0 =  __readEEPROM(address);  
  return v0;
  
//////////////////////////////////
  
  byte v1 =  __readEEPROM(address);
  if(v0 == v1) 
  {
      return v0;
  }
  else
  {    
      //Serial.print("readEEPROM issue1 [");Serial.print(address);Serial.println("]");  delay(110);
      v0 =  __readEEPROM(address);
      delay(2);
      v1 =  __readEEPROM(address);
      if(v0 == v1) {
        Serial.print("readEEPROM pass1 [");Serial.print(address);Serial.println("]"); delay(110);
        return v0;
      }
      else {
        Serial.print("readEEPROM issue2 [");Serial.print(address);Serial.println("]");  
        delay(110);
        v0 =  __readEEPROM(address);
        return v0;
      }
  }
}

/*
 * Write a byte to the EEPROM at the specified address.
 */
void writeEEPROM(int address, byte data) 
{
  Serial.print("writeEEPROM ");Serial.print(address);
  Serial.print(" data:");Serial.print(data);
  Serial.println("");
  delay(20);
  
  setAddress(address, /*outputEnable*/ false);
  SetDataPinInOutputMode();
  
  for (int pin = EEPROM_D0; pin <= EEPROM_D7; pin += 1) 
  {
    byte bit = data & 1;
    digitalWrite(pin, bit);    
    
    Serial.print("pin:");Serial.print(pin);
    Serial.print(" bit:");Serial.print(bit);
    Serial.print(" data:");Serial.println(data);
    delay(100);    
    data = data >> 1;
  }
  delay(50);

  //PORTB = PORTB | B00100000;   // set pin 13 high
  //asm("nop\n nop\n nop\n nop\n nop\n nop\n");
 //PORTB = PORTB & B11011111;  // set pin 13 low
 
  //cli(); // disable global interrupts
  digitalWrite(WRITE_EN, LOW);
  delayMicroseconds(1);
  //_delay_us(300);
  //asm("nop;nop;nop;nop;nop;nop;nop;nop;nop;nop;");   
  digitalWrite(WRITE_EN, HIGH);
  //sei(); // re-enable global interrupts:  
  delay(100);
}


/*
 * Read the contents of the EEPROM and print them to the serial monitor.
 */
void printContents() {

  byte data[16];
  
  for (int base = 0; base <=16; base += 16) {
        
    for (int offset = 0; offset < 16; offset += 1) {
        data[offset] = readEEPROM(base + offset);
    }

    char buf[80];
    sprintf(buf, "%03x:  %02x %02x %02x %02x %02x %02x %02x %02x   %02x %02x %02x %02x %02x %02x %02x %02x",
            base, data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7],
            data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15]);

    Serial.println(buf);
  }
}

// 4-bit hex decoder for common cathode 7-segment display
// byte data[] = { 0x7e, 0x30, 0x6d, 0x79, 0x33, 0x5b, 0x5f, 0x70, 0x7f, 0x7b, 0x77, 0x1f, 0x4e, 0x3d, 0x4f, 0x47 };

// 4-bit hex decoder for common anode 7-segment display
  byte data[] = { 0x81, 0xcf, 0x92, 0x86, 0xcc, 0xa4, 0xa0, 0x8f, 0x80, 0x84, 0x88, 0xe0, 0xb1, 0xc2, 0xb0, 0xb8 };



void EraseEEPROM() {
  
  Serial.print("Erasing EEPROM");

  // We using a 7 bit address bus, so we can only set
  for (int address = 0; address < 128; address += 1) {
    
    writeEEPROM(address, DEFAULT_ERASE_VALUE);
    if (address % 10 == 0) Serial.print(".");
  }
  Serial.println(" done");
}

#define _7bit 16 // _7SegmentDisplayNumber2_Bit

byte _2_7SegmentDisplayTheFredWay[] = {     
    // Trigger via the first 4 bit a 74LS47N digit 0..9 on the first 7-segment display
    // and we use the bit 5(16)  to turn off the 2 segment to make a 1 on the second 7-SegmentDisplay
    // Active low
    0+_7bit, 1+_7bit, 2+_7bit, 3+_7bit, 4+_7bit, 5+_7bit, 6+_7bit, 7+_7bit, 8+_7bit, 9+_7bit,
    // Trigger via the first 4 bit a 74LS47N digit 0..5 on the first 7-segment display
    // and we use the bit 5(16) to turn on the 2 segment to make a 1 on the second 7-SegmentDisplay
    // Active low
    0,
    1,
    2,
    3,
    4,
    5,
    };

void InitEEPROMFor2SevenSegmentDisplayTheFredWay() {
  
  Serial.println("Init EEPROM For 2 7-SegmentDisplay the fred way");
  int len = sizeof(_2_7SegmentDisplayTheFredWay)/sizeof(_2_7SegmentDisplayTheFredWay[0]);
  Serial.print(len); Serial.println(" items");
  
  for (int a = 0; a < len; a++)
  {
    int v = a;
    
    if(a <= 9)
      v += _7bit;
    else
      v -= 10;
          
    Serial.print("Writing addr:"); Serial.print(a);
    Serial.print(" val:"); Serial.print(v);
    Serial.println(""); delay(100);
    
    //writeEEPROM(a, _2_7SegmentDisplayTheFredWay[a]);    
   
    writeEEPROM(ADDR_OFFSET + a, v);   
  }
  Serial.println("Done");
}


// We will the first 16 byte of the EEPROM
// The byte from 0 to 9 will contains on the first 4 bit:0..9
// The byte from 0 to 9 will contains on bit 5 : 1
// The byte from 10 to 15 will contains on the first 4 bit:0..5
// The byte from 10 to 15 will contains on bit 5 : 0

// The EEPROM bit:0..3 will plugged into a 7 Segment driver 74LS47 to drive 7-segment 1
// The EEPROM bit:5 will be plugged into segment b & c of 7 segment 2. When bit 5 is low
// this will turn on segment b & c and display 1 for ten th

byte ReadOneByte(int addr) {
    
  int val2 = readEEPROM(addr);  
  return val2;
}

void WriteReadOneByte(int addr, byte val) {
  
  Serial.print("Write[");Serial.print(addr); Serial.print("] = ");Serial.println(val);
  writeEEPROM(addr, val);
  delay(1000*1);
  int val2 = readEEPROM(addr);
  Serial.print("Value Read:");Serial.println(val2);
  delay(1000*1);
}
  
void DisplayEEPROMState() {
  
  Serial.println("Read EEPROM");
  //int i = 8;
  //Serial.print(">ReadOneByte:");Serial.println(i);delay(20);
  //int v = ReadOneByte(i);
  //Serial.print(">Value:");Serial.println(v);delay(20);    
    
  for(int i=ADDR_OFFSET; i<ADDR_OFFSET+16; i++) 
  {
     Serial.print(">ReadOneByte:");Serial.println(i);delay(20);
     int v = ReadOneByte(i);
     Serial.print(">Value:");Serial.println(v);delay(20);    
     delay(1000*2);
  }
  
  //Serial.println("Output disable - LED ON");  
  //setAddress(0, /*outputEnable*/ false);
  //delay(1000);
  //Serial.println("Output enable - LED OFF");  
  //setAddress(0, /*outputEnable*/ true);
  //delay(1000);
  
  //Serial.println("Reading all EEPROM");
  //printContents();
  //delay(1000*10);
}


void setup() {
  // put your setup code here, to run once:
  digitalWrite(WRITE_EN, HIGH); // Already in INPUT mode, so activate pullup 
  
  pinMode(SHIFT_DATA,   OUTPUT);
  pinMode(SHIFT_CLK,    OUTPUT);
  pinMode(SHIFT_LATCH,  OUTPUT);  
  pinMode(WRITE_EN,     OUTPUT);

  SetDataPinInOutputMode();
  SetDataPinInInputMode();
  
  Serial.begin(115200);
  Serial.println("Waiting 2 second before execution");
  delay(1000*2);
  Serial.println("Running");

  InitEEPROMFor2SevenSegmentDisplayTheFredWay();
  //EraseEEPROM(); 
  //Serial.println("Reading all EEPROM");
  //printContents();
  delay(1000);
  
  DisplayEEPROMState();
}

void loop() {


}

