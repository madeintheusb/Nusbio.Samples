#include <fArduino.h>
///////////////////////////////////////////////////////////////////////////////
//  
// fArduino.cpp
// Reusable code for Arduino and Trinket programming, mostly to write clean and
// maintainable class. The fArduino library is only one cpp file with multiples
// classes, but remember that the linker will only use the methods and classes
// used.
//
// Torres Frederic 2014
//
// MIT License
//
// Remarks 
//
// - the SpeakerManager class whem compiled for a Trinket need to be compiled at 8Mhz
//
// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
// EEPROMEx.cpp - Extended EEPROM library
// Copyright(c) 2012 Thijs Elenbaas.All right reserved.
// http://michael.bouvy.net/blog/en/2013/06/17/eeprom-advanced-usage-arduino-uno-atmega328-int-char-string/
//
///////////////////////////////////////////////////////////////////////////////

#include <stdarg.h>
#include <stdio.h>
#include <fArduino.h>

////////////////////////////////////////////////////////////////////////////////////////////////////////////

String StringFormatClass::MakeString(char * padding, int max) {

    String r("");
    while (r.length() < max) {
        r.concat(padding);
    }
    return r;
}
String StringFormatClass::PadRight(String source, char * padding, int max) {

    String r(source);
    while (r.length() < max) {
        r.concat(padding);
    }
    return r;
}
String StringFormatClass::PadLeft(String source, char * padding, int max) {

    int neededPadding = max - source.length();
    if (neededPadding <= 0) {
        return source;
    }
    else {
        String r = this->MakeString(padding, neededPadding);
        r.concat(source);
        return r;
    }
}
String StringFormatClass::GetTime() {

    unsigned long secSinceStart = millis() / 1000;
    int hours                   = secSinceStart / 3600;
    secSinceStart              -= hours * 3600;
    int minutes                 = secSinceStart / 60;
    secSinceStart              -= minutes * 60;
    int seconds                 = secSinceStart;
    String formated             = this->Format("%02d:%02d:%2d", hours, minutes, seconds);

    return formated;
}
boolean StringFormatClass::IsDigit(char *format) {

    return (*format >= '0' && *format <= '9');
}
// http://www.tutorialspoint.com/c_standard_library/c_function_sprintf.htm
String StringFormatClass::Format(char *format, ...) {

    String formated = String("");
    char tmpBuf[16]; // temp buffer to format number
    va_list argptr;
    va_start(argptr, format);

    int formatSize = 0;

    while (*format != '\0') {

        if (*format == '%') {

            formatSize = 0;
            format++;

            // Check and reading format size if defined
            while (StringFormat.IsDigit(format)) {
                formatSize = *format - '0';
                format++;
            }
            
            if (*format == '%') { // string
                formated.concat("%");
            }
            else if (*format == 's') { // string
                char* s = va_arg(argptr, char *);
                formated.concat(String(s));
            }
            else if (*format == 'c') { // character
                char c = (char)va_arg(argptr, int);
                formated.concat(String(c));
            }
            else if (*format == 'd') { // decimal/integer
                int d = va_arg(argptr, int);
                formated.concat(StringFormat.PadLeft(String(d), "0", formatSize));
            }
            else if (*format == 'l') { // long
                long d = va_arg(argptr, long);
                formated.concat(String(d));
            }
            else if (*format == 'u') { // un signed integer or long - Not standard, format is %ui or %ul
                format++;
                if (*format == 'i') { // un signed integer
                    unsigned int ui = va_arg(argptr, unsigned int);
                    formated.concat(String(ui));
                }
                if (*format == 'l') { // un signed integer
                    unsigned long ui = va_arg(argptr, unsigned long);
                    formated.concat(String(ui));
                }
            }
            else if (*format == 'x') { // un signed integer hexa

                unsigned int ui = va_arg(argptr, unsigned int);
                snprintf(tmpBuf, sizeof(tmpBuf), "%x", ui);
                formated.concat(String(tmpBuf));
            }
            else if (*format == 'X') { // un signed integer hexa uppercase
                unsigned int ui = va_arg(argptr, unsigned int);
                snprintf(tmpBuf, sizeof(tmpBuf), "%X", ui);
                formated.concat(String(tmpBuf));
            }
            else if (*format == 'f') { // float
                
                double d = va_arg(argptr, double);
                #if defined(ARDUINO_UNO)
                    formated.concat(String(d));
                #endif
                #if defined(TRINKET) || defined(TRINKET_PRO)
                    //dtostrf(d, 10, 2)
                    sprintf(tmpBuf, "%f", d);
                    formated.concat(tmpBuf);
                #endif
            }
            else if (*format == 'b') { // boolean not standard

                // bool d = va_arg(argptr, bool);
                int d = va_arg(argptr, int);
                if (d)
                    strcpy(tmpBuf, "true");
                else
                    strcpy(tmpBuf, "false");
                formated.concat(String(tmpBuf));
            }
        }
        else {
            char c = format[0];
            formated.concat(String(format[0]));
        }
        format++;
    }
    va_end(argptr);
    return formated;
}
StringFormatClass StringFormat;

////////////////////////////////////////////////////////////////////////////////////////////////////////////

extern unsigned int       __bss_end;
extern unsigned int       __heap_start;
extern void *             __brkval;
extern struct __freelist *__flp; /* The head of the free list structure */

/*
* The free list structure as maintained by the
* avr-libc memory allocation routines.
*/
struct __freelist {
    size_t sz;
    struct __freelist *nx;
};

/* Calculates the size of the free list */
int freeListSize() {

    struct __freelist* current;
    int total = 0;
    for (current = __flp; current; current = current->nx) {
        total += 2; /* Add two bytes for the memory block's header  */
        total += (int)current->sz;
    }
    return total;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////

BoardClass::BoardClass() {

    this->_startTime = millis();
    this->_serialCommunicationInitialized = false;
}
BoardClass::~BoardClass() {

}
void BoardClass::SendWindowsConsoleCommand(String command, boolean newLine/*=true*/, boolean asynchronous /*=false*/) {

    if (asynchronous) {
        #if defined(SERIAL_AVAILABLE)
            Serial.println(command);
        #endif
    }
    else {
        if (newLine)
            this->Trace(command, false);
        else
            this->TraceNoNewLine(command);
    }
}
WindowsCommand BoardClass::GetWindowsConsoleCommand() {

    WindowsCommand windowsCommand;
    windowsCommand.Command = String("");

    #if defined(SERIAL_AVAILABLE)
    if (Serial.available())
        windowsCommand.Command = Serial.readStringUntil('\n');
    #endif

    return windowsCommand;
}
int BoardClass::RoundDouble(double d) {
    d += 0.5;
    int i = (int)d;
    return i;
}
bool BoardClass::InBetween(int newValue, int refValue, int plusOrMinuspercent) {

    int p = refValue*plusOrMinuspercent / 100;
    return (newValue >= refValue - p) && (newValue <= refValue + p);
}
bool BoardClass::InBetween(double newValue, double refValue, double plusOrMinuspercent) {

    double p = refValue*plusOrMinuspercent / 100.0;
    return (newValue >= refValue - p) && (newValue <= refValue + p);
}
int BoardClass::GetEEPROMSize() {
    return EEPROM_SIZE;
}
int BoardClass::GetFreeMemory() {

    int free_memory;
    if ((int)__brkval == 0) {
        free_memory = ((int)&free_memory) - ((int)&__heap_start);
    }
    else {
        free_memory = ((int)&free_memory) - ((int)__brkval);
        free_memory += freeListSize();
    }
    return free_memory;
}
void BoardClass::ClearKeyboard() {

    #if defined(SERIAL_AVAILABLE)
    while (Serial.available())
        Serial.read();
    #endif
}
//
// http://www.baldengineer.com/when-do-you-use-the-arduinos-to-use-serial-flush.html
//
void BoardClass::Trace(char * msg, boolean printTime /*= true*/) {

    #if defined(SERIAL_AVAILABLE)
    if (this->_serialCommunicationInitialized) {

        if (printTime) {

            Serial.print("[");
            Serial.flush();
            Serial.print(StringFormat.GetTime());
            Serial.flush();
            Serial.print("]");
            Serial.flush();
        }
        Serial.println(msg);
        Serial.flush();
        Board.Delay(TRACE_DELAY);
    }
    #endif
}
void BoardClass::TraceNoNewLine(char * msg) {

    #if defined(SERIAL_AVAILABLE)
    if (this->_serialCommunicationInitialized) {

        Serial.print(msg);
        Serial.flush();
    }
    #endif
}
void BoardClass::TraceNoNewLine(const char * msg) {

    this->TraceNoNewLine((char *)msg);
}
void BoardClass::Trace(String msg, boolean printTime/* = true*/) {

    this->Trace(msg.c_str(), printTime);
}
void BoardClass::Trace(const char * msg, boolean printTime/* = true*/) {

    Trace((char*)msg, printTime);
}
void BoardClass::TraceNoNewLine(const String & msg) {

    this->TraceNoNewLine(msg.c_str());
}
char * PadRight(char * s, char * padding, int max) {

    while (strlen(s) < max) {
        s = strcat(s, padding);
    }
    return s;
}
void BoardClass::TraceHeader(char * msg) {

    #if defined(SERIAL_AVAILABLE)

    int maxPad = 64;

    String barString("");
    barString = StringFormat.PadRight(barString, TRACE_HEADER_CHAR, maxPad);

    String msg2("");
    msg2.concat(TRACE_HEADER_CHAR);
    msg2.concat(" ");
    msg2.concat(msg);
    msg2 = StringFormat.PadRight(msg2, " ", maxPad - 1);
    msg2.concat(TRACE_HEADER_CHAR);

    this->Trace(barString);
    this->Trace(msg2);
    this->Trace(barString);
    #endif
}
void BoardClass::TraceFormat(char * format, char *s) {

    char buffer[MAX_FORMAT_SIZE];
    snprintf(buffer, MAX_FORMAT_SIZE, format, s);
    this->Trace(buffer);
}
void BoardClass::TraceFormat(char * format, char *s1, char *s2) {

    char buffer[MAX_FORMAT_SIZE];
    snprintf(buffer, MAX_FORMAT_SIZE, format, s1, s2);
    this->Trace(buffer);
}
void BoardClass::TraceFormat(char * format, char *s1, char *s2, char * s3) {

    char buffer[MAX_FORMAT_SIZE];
    snprintf(buffer, MAX_FORMAT_SIZE, format, s1, s2, s3);
    this->Trace(buffer);
}
void BoardClass::TraceFormat(char * format, char d1) {

    char buffer[MAX_FORMAT_SIZE];
    snprintf(buffer, MAX_FORMAT_SIZE, format, d1);
    this->Trace(buffer);
}
void BoardClass::TraceFormat(char * format, int d1) {

#if defined(SERIAL_AVAILABLE)
    char buffer[MAX_FORMAT_SIZE];
    snprintf(buffer, MAX_FORMAT_SIZE, format, d1);
    this->Trace(buffer);
#endif
}
void BoardClass::TraceFormat(char * format, int d1, int d2) {

    char buffer[MAX_FORMAT_SIZE];
    snprintf(buffer, MAX_FORMAT_SIZE, format, d1, d2);
    this->Trace(buffer);
}
/// snprintf fr float does not work for the Arduino
void BoardClass::TraceFormat(char * format, float f1) {

    char buffer[MAX_FORMAT_SIZE];
    String f1s = dtostrf((double)f1, 2, 2, buffer);
    snprintf(buffer, MAX_FORMAT_SIZE, format, f1s.c_str());
    this->Trace(buffer);
}
void BoardClass::TraceFormat(char * format, double f1, double f2) {

    char buffer[MAX_FORMAT_SIZE];
    char bufferFloat1[MAX_FORMAT_SIZE];
    char bufferFloat2[MAX_FORMAT_SIZE];

    String f1s = dtostrf((double)f1, 2, 2, bufferFloat1);
    String f2s = dtostrf((double)f2, 2, 2, bufferFloat2);

    snprintf(buffer, MAX_FORMAT_SIZE, format, f1s.c_str(), f2s.c_str());
    this->Trace(buffer);
}
void BoardClass::InitializeComputerCommunication(unsigned long speed, char * message) {

#if defined(SERIAL_AVAILABLE)
    Serial.begin(speed);
    this->_serialCommunicationInitialized = true;
    if (message) {
        this->Trace(message);
    }
#endif
}
void BoardClass::LedOn(int pin, boolean state, int delay) {

    this->LedOn(pin, state);
    this->Delay(delay);
}
void BoardClass::LedOn(int pin, boolean state) {

    digitalWrite(pin, state ? HIGH : LOW);
}
void BoardClass::LedSet(int pin, int level) {

    analogWrite(pin, level);
}
void BoardClass::SetPinMode(uint8_t pin, uint8_t type) {

    pinMode(pin, type);
}
void BoardClass::Delay(unsigned long l) {

    delay(l);
}
///////////////////////////////////////////////////////////////////////////////
// If we detect a change of button state between the current loop()
// function execution and the previous one, we wait and re reread 
// the value
boolean BoardClass::GetButtonStateDebounced(int pin, boolean lastState) {

    boolean state = digitalRead(pin) == HIGH;
    if (state != lastState) {
        delay(5);
        state = digitalRead(pin) == HIGH;
    }
    return state;
}

// Global Signleton
BoardClass Board;

////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////
/// Led
/// Represent the concept of a led, mostly to manage a blinking LED
/// without blocking the controller loop
Led::Led(int pin) {

    this->_pin = pin;
    this->_state = false;
    this->_rate = 0;
}
Led::~Led() {

}
void Led::SetState(boolean on) {

    Board.LedOn(this->_pin, on);
    this->_state = on;
}
void Led::SetLevel(int level) {

    if (level != this->_level) {

        Board.LedSet(this->_pin, level);
        this->_state = level > 0;
        this->_level = level;
    }
}
void Led::SetBlinkMode(unsigned long rate) {

    this->_rate           = rate;
    this->_blinkStartTime = millis();
    this->_state          = true;
    this->SetState(this->_state);
}
unsigned long Led::GetBlinkDurationCycle() {

    return millis() - this->_blinkStartTime;
}
void Led::SetBlinkModeOff() {

    this->_rate  = 0;
    this->_state = false;
    this->SetLevel(0);
}
boolean Led::IsBlinking() {

    return this->_rate != 0;
}
void Led::Blink() {

    if (GetBlinkDurationCycle() > this->_rate)  { // We need to reverse the mode of the led

        this->_state = !this->_state;
        this->SetState(this->_state);
        this->_blinkStartTime = millis();
    }
}
void Led::Blink(int blinkCount, int waitTime) {
    
    for (int z = 0; z < blinkCount; z++) {

        this->SetState(true);
        Board.Delay(waitTime);
        this->SetState(false);
        Board.Delay(waitTime);
    }
    this->SetState(false);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// MultiState Button
///
MultiStateButton::MultiStateButton(int pin, Led * led, int maxState, const int * ledIntensityArray) {

    this->_pin                          = pin;
    this->_previousPin                  = UNDEFINED_PIN;
    this->_maxState                     = maxState;
    this->_ledIntensityArray            = ledIntensityArray;
    this->LedInstance                   = led;
    this->StateIndex                    = 0;
    this->NextButtonLastStateInLoop     = false;
    this->PreviousButtonLastStateInLoop = false;
}
MultiStateButton::~MultiStateButton() {

}
void MultiStateButton::SetPreviousButton(int pin) {

    this->_previousPin = pin;
}
// Return true if the user switch to state state and we never queried it before
boolean MultiStateButton::StateChangeFor(int state) {

    if (this->StateIndex == state) {

        boolean r = this->_statedChanged;
        if (r) {
            this->_statedChanged = false;
        }
        return r;
    }
    else return false;
}
void MultiStateButton::NextState() {

    this->StateIndex++;
    if (this->StateIndex >= this->_maxState)
        this->StateIndex = 0;
    this->_statedChanged = true;
}
void MultiStateButton::PreviousState() {

    this->StateIndex--;
    if (this->StateIndex < 0)
        this->StateIndex = this->_maxState - 1;
    this->_statedChanged = true;
}
boolean MultiStateButton::GetButtonStateDebounced() {

    boolean state = digitalRead(this->_pin) == HIGH;

    if (state != this->NextButtonLastStateInLoop) {

        delay(5);
        state = digitalRead(this->_pin) == HIGH;
    }
    return state;
}
boolean MultiStateButton::GetPreviousButtonStateDebounced() {

    if (this->_previousPin != UNDEFINED_PIN) {

        boolean state = digitalRead(this->_previousPin) == HIGH;

        if (state != this->PreviousButtonLastStateInLoop) {

            delay(5);
            state = digitalRead(this->_previousPin) == HIGH;
        }
        return state;
    }
    else return false;
}
void MultiStateButton::UpdateUI() {

    if (this->LedInstance->IsBlinking()) {

        this->LedInstance->Blink();
    }
    else{
        this->LedInstance->SetLevel(this->_ledIntensityArray[this->StateIndex]);
    }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// TimeOut
///
TimeOut::TimeOut(unsigned long duration) {

    this->Counter   = -1;
    this->_duration = duration;
    this->Reset();
}
TimeOut::~TimeOut() {
}
boolean TimeOut::IsTimeOut() {

    boolean b = (millis() - this->_time) > this->_duration;
    if (b) {
        this->Reset();
    }
    return b;
}
boolean TimeOut::EveryCalls(unsigned long callCount) {

    return this->Counter % callCount == 0;
}
void TimeOut::Reset() {

    this->_time = millis();
    this->Counter++;
}
String TimeOut::ToString() {

    return StringFormat.Format("TimeOut counter:%l, duration:%l, time:%l", this->Counter, this->_duration, this->_time);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////
///  Temperature Manager
///
TemperatureManager::TemperatureManager() {

}
TemperatureManager::~TemperatureManager() {

}
float TemperatureManager::CelsiusToFahrenheit(float celsius) {

    return (9.0 / 5.0 * celsius) + 32.0;
}
void TemperatureManager::Add(float celsius) {

}
////////////////////////////////////////////////////////////////////////////////////////////////////////////

SpeakerManager::SpeakerManager(byte pin) {

    this->_pin = pin;
    this->BackGroundOn = false;
}
SpeakerManager::~SpeakerManager() {

}
void SpeakerManager::PlaySequence(int size, int * noteDurationSequence) {

    this->PlaySequence(size, noteDurationSequence, SPEAKERMANAGER_PLAY_SEQUENCE_NORMAL, false);
}
void SpeakerManager::PlaySequence(int size, int * noteDurationSequence, int speed) {

    this->PlaySequence(size, noteDurationSequence, speed, false);
}
void SpeakerManager::StartSequenceBackGround(int size, int * noteDurationSequence) {

    this->_backGroundNoteDurationSequence = noteDurationSequence;
    this->_backGroundNoteDurationSequenceSize = size;
    this->_backGroundNoteDurationIndex = 0;
    this->BackGroundOn = true;
    this->StartBackgroundNote();
}
void SpeakerManager::StartBackgroundNote() {

    this->Play(this->_backGroundNoteDurationSequence[this->_backGroundNoteDurationIndex], this->_backGroundNoteDurationSequence[this->_backGroundNoteDurationIndex + 1], SPEAKERMANAGER_PLAY_SEQUENCE_NORMAL);
}
void SpeakerManager::StopBackGroundSequence() {

    this->BackGroundOn = false;    
}
boolean SpeakerManager::BackGroundUpdate() {

    if (this->BackGroundOn) {

        this->_backGroundNoteDurationIndex += 2;

        if (this->_backGroundNoteDurationIndex >= this->_backGroundNoteDurationSequenceSize) {

            this->BackGroundOn = false;
        }
        else this->StartBackgroundNote();
    }
    return this->BackGroundOn;
}
void SpeakerManager::PlaySequence(int size, int * noteDurationSequence, int speed, boolean reverse) {

    if (reverse) {

        for (int i = size - 1; i >= 0; i -= 2) {

            this->Play(noteDurationSequence[i - 1], noteDurationSequence[i], speed);
        }
    }
    else {
        for (int i = 0; i < size; i += 2) {

            this->Play(noteDurationSequence[i], noteDurationSequence[i + 1], speed);
        }
    }
}
void SpeakerManager::Play(int note, int duration) {

    this->Play(note, duration, SPEAKERMANAGER_PLAY_SEQUENCE_NORMAL);
}
void SpeakerManager::Play(int note, int duration, int speed) {

    this->Play(note, duration, speed, true);
}
void SpeakerManager::Play(int note, int duration, int speed, boolean stop) {

    // to calculate the note duration, take one second divided by the note type. e.g. quarter note = 1000 / 4, eighth note = 1000/8, etc.

    float speedAdjustment = 1.0;
#if defined(TRINKET)
    speedAdjustment = 1.8;
#endif

    int noteDuration = 1000 / ((duration / speed) * speedAdjustment);
    this->Tone(note, noteDuration);

    // To distinguish the notes, set a minimum time between them. 
    // the note's duration + 30% seems to work well:
    delay(noteDuration * 1.30);

    if (stop)
        this->Off();
}
void SpeakerManager::Off() {

#if defined(TRINKET)
#else
    noTone(this->_pin);
#endif
}
void Trinket_Tone(unsigned char speakerPin, int frequencyInHertz, long timeInMilliseconds) {

    long delayAmount = (long)(1000000 / frequencyInHertz);
    long loopTime = (long)((timeInMilliseconds * 1000) / (delayAmount * 2));

    for (int x = 0; x<loopTime; x++) {

        digitalWrite(speakerPin, HIGH);
        delayMicroseconds(delayAmount);
        digitalWrite(speakerPin, LOW);
        delayMicroseconds(delayAmount);
    }
}
void SpeakerManager::Tone(int note, int duration) {
#if defined(TRINKET)
    Trinket_Tone(this->_pin, note, duration);
#else
    tone(this->_pin, note, duration);
#endif
}



/*
EEPROMEx.cpp - Extended EEPROM library
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

/******************************************************************************
* Includes
******************************************************************************/
//#include "EEPROMex.h"

/******************************************************************************
* Definitions
******************************************************************************/

#define _EEPROMEX_VERSION 1 // software version of this library
#define _EEPROMEX_DEBUG     // Enables logging of maximum of writes and out-of-memory
/******************************************************************************
* Constructors
******************************************************************************/

// Boards with ATmega328, Duemilanove, Uno, Uno SMD, Lilypad - 1024 bytes (1 kilobyte)
// Boards with ATmega1280 or 2560, Arduino Mega series – 4096 bytes (4 kilobytes)
// Boards with ATmega168, Lilypad, old Nano, Diecimila  – 512 bytes
// By default we choose conservative settings
EEPROMClassEx::EEPROMClassEx()
    : _allowedWrites(100)
{
}

/******************************************************************************
* User API
******************************************************************************/

void EEPROMClassEx::setMemPool(int base, int memSize) {
    //Base can only be adjusted if no addresses have already been issued
    if (_nextAvailableaddress == _base)
        _base = base;
    _nextAvailableaddress = _base;

    //Ceiling can only be adjusted if not below issued addresses
    if (memSize >= _nextAvailableaddress)
        _memSize = memSize;

#ifdef _EEPROMEX_DEBUG    
    #if defined(SERIAL_AVAILABLE)
    if (_nextAvailableaddress != _base)
        Serial.println("Cannot change base, addresses have been issued");

    if (memSize < _nextAvailableaddress)
        Serial.println("Cannot change ceiling, below issued addresses");
    #endif
#endif	

}

void EEPROMClassEx::setMaxAllowedWrites(int allowedWrites) {
#ifdef _EEPROMEX_DEBUG
    _allowedWrites = allowedWrites;
#endif			
}

int EEPROMClassEx::getAddress(int noOfBytes) {

    int availableaddress = _nextAvailableaddress;
    _nextAvailableaddress += noOfBytes;

#ifdef _EEPROMEX_DEBUG    
    if (_nextAvailableaddress > _memSize) {

        #if defined(SERIAL_AVAILABLE)
        Serial.println("Attempt to write outside of EEPROM memory");
        #endif
        return -availableaddress;
    }
    else {
        return availableaddress;
    }
#endif
    return availableaddress;
}


bool EEPROMClassEx::isReady() {
    return eeprom_is_ready();
}

uint8_t EEPROMClassEx::read(int address)
{
    return readByte(address);
}

bool EEPROMClassEx::readBit(int address, byte bit) {
    if (bit> 7) return false;
    if (!isReadOk(address + sizeof(uint8_t))) return false;
    byte byteVal = eeprom_read_byte((unsigned char *)address);
    byte bytePos = (1 << bit);
    return (byteVal & bytePos);
}

uint8_t EEPROMClassEx::readByte(int address)
{
    if (!isReadOk(address + sizeof(uint8_t))) return 0;

    ABOUT_TO_CALL_EEPROM_API;
    uint8_t v = eeprom_read_byte((unsigned char *)address);
    DONE_CALLING_EEPROM_API;

    return v;
}

uint16_t EEPROMClassEx::readInt(int address)
{
    if (!isReadOk(address + sizeof(uint16_t))) return 0;

    ABOUT_TO_CALL_EEPROM_API;
    uint16_t v = eeprom_read_word((uint16_t *)address);
    DONE_CALLING_EEPROM_API;

    return v;
}

uint32_t EEPROMClassEx::readLong(int address)
{
    if (!isReadOk(address + sizeof(uint32_t))) return 0;
    return eeprom_read_dword((unsigned long *)address);
}

float EEPROMClassEx::readFloat(int address)
{
    if (!isReadOk(address + sizeof(float))) return 0;
    float _value;
    readBlock<float>(address, _value);
    return _value;
}

double EEPROMClassEx::readDouble(int address)
{
    if (!isReadOk(address + sizeof(double))) return 0;
    double _value;
    readBlock<double>(address, _value);
    return _value;
}

bool EEPROMClassEx::write(int address, uint8_t value)
{
    return writeByte(address, value);
}

bool EEPROMClassEx::writeBit(int address, uint8_t bit, bool value) {
    updateBit(address, bit, value);
    return true;
}


bool EEPROMClassEx::writeByte(int address, uint8_t value)
{
    if (!isWriteOk(address + sizeof(uint8_t))) return false;

    ABOUT_TO_CALL_EEPROM_API;
    eeprom_write_byte((unsigned char *)address, value);
    DONE_CALLING_EEPROM_API;

    return true;
}

bool EEPROMClassEx::writeInt(int address, uint16_t value)
{
    if (!isWriteOk(address + sizeof(uint16_t))) return false;

    ABOUT_TO_CALL_EEPROM_API;
    eeprom_write_word((uint16_t *)address, value);
    DONE_CALLING_EEPROM_API;

    return true;
}

bool EEPROMClassEx::writeLong(int address, uint32_t value)
{
    if (!isWriteOk(address + sizeof(uint32_t))) return false;
    eeprom_write_dword((unsigned long *)address, value);
    return true;
}

bool EEPROMClassEx::writeFloat(int address, float value)
{
    return (writeBlock<float>(address, value) != 0);
}

bool EEPROMClassEx::writeDouble(int address, double value)
{
    return (writeBlock<float>(address, value) != 0);
}

bool EEPROMClassEx::update(int address, uint8_t value)
{
    return (updateByte(address, value));
}

bool EEPROMClassEx::updateBit(int address, uint8_t bit, bool value)
{
    if (bit> 7) return false;

    byte byteValInput = readByte(address);
    byte byteValOutput = byteValInput;
    // Set bit
    if (value) {
        byteValOutput |= (1 << bit);  //Set bit to 1
    }
    else {
        byteValOutput &= ~(1 << bit); //Set bit to 0
    }
    // Store if different from input
    if (byteValOutput != byteValInput) {
        writeByte(address, byteValOutput);
    }
    return true;
}

bool EEPROMClassEx::updateByte(int address, uint8_t value)
{
    return (updateBlock<uint8_t>(address, value) != 0);
}

bool EEPROMClassEx::updateInt(int address, uint16_t value)
{
    return (updateBlock<uint16_t>(address, value) != 0);
}

bool EEPROMClassEx::updateLong(int address, uint32_t value)
{
    return (updateBlock<uint32_t>(address, value) != 0);
}

bool EEPROMClassEx::updateFloat(int address, float value)
{
    return (updateBlock<float>(address, value) != 0);
}

bool EEPROMClassEx::updateDouble(int address, double value)
{
    return (writeBlock<double>(address, value) != 0);
}

bool EEPROMClassEx::isWriteOk(int address)
{
#ifdef _EEPROMEX_DEBUG    
    _writeCounts++;
    if (_allowedWrites == 0 || _writeCounts > _allowedWrites) {
        #if defined(SERIAL_AVAILABLE)
        Serial.println("Exceeded maximum number of writes");
        #endif
        return false;
    }

    if (address > _memSize) {
        #if defined(SERIAL_AVAILABLE)
        Serial.println("Attempt to write outside of EEPROM memory");
        #endif
        return false;
    }
    else {
        return true;
    }
#endif		
    return true;
}

bool EEPROMClassEx::isReadOk(int address)
{
#ifdef _EEPROMEX_DEBUG    
    if (address > _memSize) {
        #if defined(SERIAL_AVAILABLE)
        Serial.println("Attempt to write outside of EEPROM memory");
        #endif
        return false;
    }
    else {
        return true;
    }
#endif
    return true;
}

int EEPROMClassEx::_base                 = 0;
int EEPROMClassEx::_memSize              = 512;
int EEPROMClassEx::_nextAvailableaddress = 0;
int EEPROMClassEx::_writeCounts          = 0;

EEPROMClassEx EEPROM;

//////////////////////////////////////////////////////
/// MemDB 
/// Object to store byte array in the EEPROM memory
///
void MemDB::InitEEPROM(int maxEEPROMMemory, int maxAllowedWrites) {

    EEPROM.setMaxAllowedWrites(maxEEPROMMemory);
    EEPROM.setMemPool(0, maxAllowedWrites);
}
MemDB::MemDB() {
}
MemDB::~MemDB() {
}
int MemDB::CreateString(int size) {

    return EEPROM.getAddress(size);
}
void MemDB::SetString(String s) {

}
String MemDB::GetString() {

}
int MemDB::CreateByteArray(int size, boolean init) {
    
    this->_size          = size;
    this->_lengthAddress = EEPROM.getAddress(sizeof(int)); // Location of the array len is first
    this->_startAddress  = EEPROM.getAddress(size);        // Location of the data

    if (init) {
        this->ClearByteArray();
    }
    else {
        this->_index = this->GetLength(); // Load length from EEPROM
    }
    return this->_startAddress;
}
int MemDB::ClearByteArray() {

    this->_index = 0;
    this->SetLength();
}
byte MemDB::AddByteArray(byte b) {

    if (this->_index >= 0 && this->_index < this->_size) {

        EEPROM.writeByte(this->_startAddress + this->_index, b);
        this->_index += 1;
        this->SetLength();
        return b;
    }    
    return 0;
}
byte MemDB::GetByteArray(int index) {

    if (index >= 0 && index < this->_size) {
        return EEPROM.readByte(this->_startAddress + index);
    }
    else return 0; // Which we could throw
}
int MemDB::GetLength() {

    return EEPROM.readInt(this->_lengthAddress);
}
int MemDB::SetLength() {

    return EEPROM.writeInt(this->_lengthAddress, this->_index);
}
String MemDB::ToString() {

    String values("");
    for (int t = 0; t < this->GetLength(); t++) {
        byte b = this->GetByteArray(t);
        //values += StringFormat.Format("%d:%d, ", t, b);
        values += StringFormat.Format("%d ", t, b);
    }
    return values;
}
// Send the data as csv to the serial port
void MemDB::ToSerial() {

    for (int t = 0; t < this->GetLength(); t++) {
        
        #if defined(SERIAL_AVAILABLE)
        Serial.println(StringFormat.Format("%3d, %d", t, this->GetByteArray(t)));
        Serial.flush();
        Board.Delay(5);
        #endif
    }
}



////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// Piezo
////////////////////////////////////////////////////////////////////////////////////////////////////////////

Piezo::Piezo(int pin, int threshHold, int maxCalibratedValue, int maxMidiVelocity) {

    this->_pin                = pin;
    this->_threshHold         = threshHold;
    this->_debug              = !false;
    this->_maxCalibratedValue = maxCalibratedValue;
    this->_ready              = true;
    this->MaxMidiVelocity     = maxMidiVelocity;
}
void Piezo::WaitForRebound() {

    while (true) {
        int val = analogRead(this->_pin);
        if (val == 0)
            break;
        delay(1);
    }
}
int Piezo::GetValue() {
    
    if (this->_ready) {

        int val = analogRead(this->_pin);

        if (val > this->_threshHold) {


            int threshHold2 = this->_threshHold * 2;
            this->_ready = false;
            int maxVal   = val;
            String buf("");
            buf.concat(val); buf.concat(" ");

            while (true) {

                int val2 = analogRead(this->_pin);
                if (val2 > maxVal) {
                    maxVal = val2;
                    if (this->_debug) {
                        buf.concat(val2); buf.concat(" ");
                    }
                    if (maxVal > threshHold2)
                        break;
                }
                else {
                    break;
                }
            }

            if (maxVal > this->_maxCalibratedValue)
                maxVal = this->_maxCalibratedValue;

            int _8bitVal = map(maxVal, 0, this->_maxCalibratedValue, 0, this->MaxMidiVelocity);
            if (this->_debug) {
                
                #if defined(SERIAL_AVAILABLE)
                Serial.println(StringFormat.Format("[%d] HeightByte:%d val:%d maxVal:%d buf:%s", this->_pin, _8bitVal, val, maxVal, buf.c_str()));
                Serial.flush();
                #endif
            }
            else {
                delay(6);
            }
            return _8bitVal;
        }
        else return -1;
    }
    else {
        // Let's wait until descending value of the shock reach 0 
        int val = analogRead(this->_pin);
        if (val == 0) {
            this->_ready = true;
        }
        return -1;
    }
}
int Piezo::GetTimeValue() {

    int _8bitVal;
    int val = analogRead(this->_pin);

    if (val > this->_threshHold) {

        String buf("");
        buf.concat(val); buf.concat(" ");

        int t = 0;
        while (true)  {
            int val2 = analogRead(this->_pin);
            if (val2 > this->_threshHold) {
                t++;
            }
            else {
                break;
            }
        }
        if (t < 100) {
            _8bitVal = -1;
        }
        else {
            _8bitVal = map(t, 0, this->_maxCalibratedValue, 0, 127);
        }
        if (this->_debug) {

            #if defined(SERIAL_AVAILABLE)
            Serial.println(StringFormat.Format("_8bitVal:%d, val:%d, t:%d", _8bitVal, val, t));
            //Serial.flush();
            #endif
        }

        return _8bitVal;
    }
    else return -1;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// Register74HC595_8Bit
////////////////////////////////////////////////////////////////////////////////////////////////////////////

Register74HC595_8Bit::Register74HC595_8Bit(int latchPin, int clockPin, int dataPin) {

    this->_latchPin = latchPin;
    this->_clockPin = clockPin;
    this->_dataPin  = dataPin;

    //set pins to output so you can control the shift register
    Board.SetPinMode(latchPin, OUTPUT);
    Board.SetPinMode(clockPin, OUTPUT);
    Board.SetPinMode(dataPin,  OUTPUT);
}
void Register74HC595_8Bit::Send8BitValue(int v) {

    // take the latchPin low so  the LEDs don't change while you're sending in bits:
    digitalWrite(this->_latchPin, LOW);
    shiftOut(this->_dataPin, this->_clockPin, MSBFIRST, v); // shift out the bits:
    digitalWrite(this->_latchPin, HIGH); //take the latch pin high so the LEDs will light up:
}
void Register74HC595_8Bit::FlashValue(int v, int flashCount /* = 4 */, int waitTime /* = 125 */) {

    for (int i = 0; i < flashCount; i++) {

        this->Send8BitValue(v);
        Board.Delay(waitTime);
        this->Send8BitValue(0);
        Board.Delay(waitTime);
    }
}
void Register74HC595_8Bit::AnimateOneLeftToRightAndRightToLeft(int waitTime){

    this->Send8BitValue(0);

    int p2[] = { 1, 2, 4, 8, 16, 32, 64, 128 };

    for (int i = 0; i < 8; i++) {

        this->Send8BitValue(p2[i]);
        Board.Delay(waitTime);
    }
    for (int i = 8 - 1; i >= 0; i--) {

        this->Send8BitValue(p2[i]);
        Board.Delay(waitTime);
    }
    this->Send8BitValue(0);
}
void Register74HC595_8Bit::FlashNumberFrom0To8(int v, int flashCount /* = 4 */, int waitTime /* = 125 */) {

    for (int i = 0; i < flashCount; i++) {

        this->DisplayNumberFrom0To8(v);
        Board.Delay(waitTime);
        this->DisplayNumberFrom0To8(0);
        Board.Delay(waitTime);
    }
}
void Register74HC595_8Bit::AnimateEveryOther(int flashCount, int waitTime) {

    this->Send8BitValue(0);

    for (int i = 0; i < flashCount; i++) {

        this->FlashValue(1 + 4 + 16 + 64, 1, waitTime);
        this->FlashValue(2 + 8 + 32 + 128, 1, waitTime);
    }
}
void Register74HC595_8Bit::AnimateAllLeftToRight(int waitTime) {

    this->Send8BitValue(0);

    for (int v = 0; v <= 8; v++) {

        this->DisplayNumberFrom0To8(v);
        Board.Delay(waitTime);
    }
    for (int v = 8; v > 0; v--) {

        this->DisplayNumberFrom0To8(v);
        Board.Delay(waitTime);
    }
    this->Send8BitValue(0);
}
void Register74HC595_8Bit::AnimateAllRightToLeft(int waitTime) {

    this->Send8BitValue(0);
    for (int v = 0; v <= 8; v++) {

        this->DisplayNumberFrom0To8Reverse(v);
        Board.Delay(waitTime);
    }
    for (int v = 8; v > 0; v--) {

        this->DisplayNumberFrom0To8Reverse(v);
        Board.Delay(waitTime);
    }
    this->Send8BitValue(0);
}
//  0   1   2   3   4   5   6   7
//  1   2   4   8   16  32  64  128
void Register74HC595_8Bit::DisplayNumberFrom0To8(int v) {

    int v2 = 0;

    switch (v) {

        case 0: v2 = 0;                                  break;
        case 1: v2 = 1;                                  break;
        case 2: v2 = 1 + 2;                              break;
        case 3: v2 = 1 + 2 + 4;                          break;
        case 4: v2 = 1 + 2 + 4 + 8;                      break;
        case 5: v2 = 1 + 2 + 4 + 8 + 16;                 break;
        case 6: v2 = 1 + 2 + 4 + 8 + 16 + 32;            break;
        case 7: v2 = 1 + 2 + 4 + 8 + 16 + 32 + 64;       break;
        case 8: v2 = 1 + 2 + 4 + 8 + 16 + 32 + 64 + 128; break;
    }
    this->Send8BitValue(v2);
}
void Register74HC595_8Bit::DisplayNumberFrom0To8Reverse(int v) {

    int v2 = 0;

    switch (v) {

        case 0: v2 = 0;                                 break;
        case 1: v2 = 128;                               break;
        case 2: v2 = 128 + 64;                          break;
        case 3: v2 = 128 + 64 + 32;                     break;
        case 4: v2 = 128 + 64 + 32 + 16;                break;
        case 5: v2 = 128 + 64 + 32 + 16 + 8;            break;
        case 6: v2 = 128 + 64 + 32 + 16 + 8 + 4;        break;
        case 7: v2 = 128 + 64 + 32 + 16 + 8 + 4 + 2;    break;
        case 8: v2 = 128 + 64 + 32 + 16 + 8 + 4 + 2 + 1;break;
    }
    this->Send8BitValue(v2);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// Register74HC595_16Bit
////////////////////////////////////////////////////////////////////////////////////////////////////////////

Register74HC595_16Bit::Register74HC595_16Bit(int latchPin, int clockPin, int dataPin) {

    this->_latchPin = latchPin;
    this->_clockPin = clockPin;
    this->_dataPin = dataPin;

    //set pins to output so you can control the shift register
    Board.SetPinMode(latchPin, OUTPUT);
    Board.SetPinMode(clockPin, OUTPUT);
    Board.SetPinMode(dataPin, OUTPUT);
}

void Register74HC595_16Bit::Send16BitValue(unsigned int v) {

    byte low  = (byte)(v & 0xff);
    byte high = (byte)((v >> 8) & 0xff);
    
    digitalWrite(this->_latchPin, LOW);
    shiftOut(this->_dataPin, this->_clockPin, MSBFIRST, high);
    shiftOut(this->_dataPin, this->_clockPin, MSBFIRST, low);
    digitalWrite(this->_latchPin, HIGH);
}

void Register74HC595_16Bit::AnimateOneLeftToRightAndRightToLeft2Leds(int waitTime, int count) {

    for (int counter = 0; counter < count; counter++) {

        unsigned int v = 256 + 1;

        for (int i = 0; i <= 7; i++) {

            //Board.Trace(StringFormat.Format("[%d]v:%ui", i, v));
            this->Send16BitValue(v);
            Board.Delay(i == 0 ? waitTime * 2 : waitTime); // Wait more for the first step
            v = v << 1;
        }

        v = 32768 + 128;

        for (int i = 0; i <= 7; i++) {

            //Board.Trace(StringFormat.Format("[%d]v:%ui", i, v));
            this->Send16BitValue(v);
            Board.Delay(i == 7 ? waitTime * 2 : waitTime); // Wait more for the first step
            v = v >> 1;
        }
        this->Send16BitValue(0);
    }
}

void Register74HC595_16Bit::AnimateOneLeftToRightAndRightToLeft1Leds(int waitTime, int count) {

    unsigned int _16Bits[] = {
    1,     // 1
    2,     // 2
    4,     // 3
    8,     // 4
    16,    // 5
    32,    // 6
    64,    // 7
    128,   // 8
    256,   // 9
    512,   // 10
    1024,  // 11
    2048,  // 12
    4096,  // 13
    8192,  // 14
    16384, // 15
    32768  // 16
    };

    for (int counter = 0; counter < count; counter++) {

        unsigned int v = 256 + 1;

        for (int i = 0; i < 16; i++) {

            //Board.Trace(StringFormat.Format("[%d]v:%ui", i, _16Bits[i]));
            this->Send16BitValue(_16Bits[i]);
            Board.Delay(waitTime);
        }
        for (int i = 15; i >=0; i--) {

            //Board.Trace(StringFormat.Format("[%d]v:%ui", i, _16Bits[i]));
            this->Send16BitValue(_16Bits[i]);
            Board.Delay(waitTime);
        }
        this->Send16BitValue(0);
    }
}


/**************************************************
LightSensorButton

- When used with a 1k resistor, simply passing the hand over the button
will trigger the event.
- When used with a 10k resistor, the user will have to put the finger on
the light sensor
- Possiblity to implement : Put the finger on the button (FingerDown),
wait 3 seconds, remove finger

https://learn.adafruit.com/photocells/using-a-photocell
*/
boolean LightSensorButton::Activated() {

    static byte changeDetectedCounter;

    if (this->ChangeDetected()) {

#if defined(LIGHTSENSORBUTTON_DEBUG)
        Board.Trace(StringFormat.Format("LightSensorButton(%d) Change detected %d", _pin, changeDetectedCounter));
#endif

        changeDetectedCounter++;
        if (changeDetectedCounter > 2) {
            Board.Delay(200);
        }
        if (changeDetectedCounter > 5) {
            // Most likely the light has changed quickly in the room and it is not cause by a human
            // passing the had in front of the light sensor.
            // We better give up and request new light reference
            Board.Trace(StringFormat.Format("LightSensorButton(%d) Permanent light change detected", _pin));
            changeDetectedCounter = 0;
            this->NeedReference = true;
            return false;
        }

        Board.Delay(this->DETECTION_BACK_WAIT_TIME);

        if (this->BackToReferenceValue()) {

#if defined(LIGHTSENSORBUTTON_DEBUG)
            Board.Trace("Returned to ref value -- Activated");
#endif
            changeDetectedCounter = 0;
            return true;
        }
    }
}
boolean LightSensorButton::BackToReferenceValue() {

    return Board.InBetween(analogRead(this->_pin), this->_referenceValue, this->DETECTION_PERCENT_BACK);
}
boolean LightSensorButton::ChangeDetected() {

    _lastValue = analogRead(this->_pin);
    _lastDifference = _referenceValue - _lastValue;
    int expectedChange = _referenceValue * DETECTION_PERCENT / 100;
    _lastChangeInPercent = ((1.0 * _lastValue / _referenceValue) - 1) * 100.0;
    boolean r = abs(_lastDifference) >= expectedChange;
    return r;
}
int LightSensorButton::UpdateReferences() {

    int ref = 0;

    for (int i = 0; i < MAX_REFERENCES; i++) {

        ref += analogRead(this->_pin);
        delay(REFERENCE_ACQUISITION_TIME);
    }
    _referenceValue = ref / MAX_REFERENCES;
    this->NeedReference = false;
    this->_lastReferenceTime = millis();
    this->ChangeDetected();
    return _referenceValue;
}
boolean LightSensorButton::ReferenceTimeOut() {

    return (millis() - this->_lastReferenceTime) > this->UPDATE_REFERENCE_EVERY_X_SECONDS * 1000;
}
LightSensorButton::LightSensorButton(byte pin, char * name) {

    this->Name = name;
    this->UPDATE_REFERENCE_EVERY_X_SECONDS = 15; // Update light reference every 15 seconds
    this->MAX_REFERENCES = 3;  // Capture the average of 3 light value to compute the lght reference
    this->REFERENCE_ACQUISITION_TIME = 250;
    this->DETECTION_PERCENT = 24; // If the light change more than +-24% we detected a possible change
    this->DETECTION_PERCENT_BACK = 8;  // if the light go back +-ReferenceValue, the button was activated by a human (Hopefully, not 100% guaranteed)
    this->DETECTION_BACK_WAIT_TIME = 250;// Wait time before we check if the light value went back to the reference value

    this->_pin = pin;
    this->NeedReference = true;
    this->_lastReferenceTime = 0;
    this->_lastDifference = 0;
    this->_lastChangeInPercent = 0;
    this->_referenceValue = 0;
}
LightSensorButton::~LightSensorButton() {

}
boolean LightSensorButton::FingerUp() {

    return this->_lastDifference < 0;
}
boolean LightSensorButton::FingerDown() {

    return this->_lastDifference > 0;
}
String LightSensorButton::ToString() {

    return StringFormat.Format("LightSensorButton(%s:%d) { value:%d, diff:%d, diffPercent:%f%% }",
        this->Name.c_str(),
        this->_pin,
        this->_lastValue,
        this->_lastDifference,
        this->_lastChangeInPercent
        );
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// RadioShackPIRSensor
////////////////////////////////////////////////////////////////////////////////////////////////////////////

RadioShackPIRSensor::RadioShackPIRSensor(int pin){

    this->_pin = pin;
    Board.SetPinMode(pin, INPUT);
}
boolean RadioShackPIRSensor::MotionDetected(){

    int val = digitalRead(this->_pin);
    return digitalRead(this->_pin) == HIGH;
}


////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// PullUpButton
////////////////////////////////////////////////////////////////////////////////////////////////////////////

PullUpButton::PullUpButton(int pin) {

    this->_pin           = pin;
    this->_previousInput = HIGH;
    Board.SetPinMode(this->_pin, INPUT_PULLUP);
}
boolean PullUpButton::IsPressed() {

    boolean r = false;
    int input = digitalRead(this->_pin);

    if (this->_previousInput == HIGH && input == LOW) {

        Board.Delay(50);
        input = digitalRead(this->_pin);
        r = input == LOW;
    }
    if (r) {
        this->_previousInput = HIGH;
    }
    else {
        this->_previousInput = input;
    }
    return r;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// UltrasonicDistanceSensor
/// HcSr04
////////////////////////////////////////////////////////////////////////////////////////////////////////////

UltrasonicDistanceSensor::UltrasonicDistanceSensor(uint8_t triggerPin, uint8_t echoPin, int maxCmDistance) {

    this->_triggerPin   = triggerPin;
    this->_echoPin      = echoPin;
    this->MaxCmDistance = maxCmDistance;
}

int UltrasonicDistanceSensor::Ping() {

    long duration, distance;

    digitalWrite(this->_triggerPin, LOW);
    delayMicroseconds(2);
    digitalWrite(this->_triggerPin, HIGH);

    delayMicroseconds(10);

    digitalWrite(this->_triggerPin, LOW);

    duration = pulseIn(this->_echoPin, HIGH);
    distance = (duration / 2) / 29.1;

    return distance;
}