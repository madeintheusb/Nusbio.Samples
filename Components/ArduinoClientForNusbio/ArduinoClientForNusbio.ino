/************************************


*/

#include "fArduino.h"

int comRxPinInterruptId = 0;
int comRxPin            = 2;
int comTxPin            = 3;   

bool comOn = false;

#define ON_BOARD_LED 13
Led _onBoardLed(ON_BOARD_LED);

void setup() {

    Board.Delay(1500); // Wait one second so after plug in the USB port, I can start the ArduinoWindowsConsole

    Board.InitializeComputerCommunication(9600, "Initializing...");
    Board.TraceHeader("GDevice Client Demo");
    Board.SetPinMode(ON_BOARD_LED, OUTPUT);
    Board.SetPinMode(comRxPin, INPUT);
    Board.SetPinMode(comTxPin, OUTPUT);
    
    _onBoardLed.SetBlinkMode(1000);

    attachInterrupt(comRxPinInterruptId, data_incoming, RISING);
}

void ShowUserData(int value) {

    Board.SendWindowsConsoleCommand(StringFormat.Format("value:%d", value), true);
}

void IncomingDataInterrupt() {

    int comStart = digitalRead(comRxPin);
    if (comStart == HIGH && comOn == false) {
        comOn = true;
        _onBoardLed.SetBlinkMode(80);
    }
    else if (comStart == LOW && comOn == true) {
        comOn = false;
        _onBoardLed.SetBlinkMode(1000);
    }
}
volatile int detectedHigh = 0;
int detectedLow = 0;
volatile int detectedInterrupt = 0;
int detectedReadyForFirstChar = 0;
volatile byte data_in = 0;



void data_incoming_BU(){

    int i, good = 0;

    for (i = 0; i < 100; i++) {

        delayMicroseconds(20);
        good = 1;
        if (digitalRead(comRxPin) == LOW){
            good = 0;
            break;
        }
    }

    if (good == 1) {

        data_in = 0;

        detectedInterrupt++;

        detachInterrupt(comRxPinInterruptId);

        while (digitalRead(comRxPin) == LOW) {
            delayMicroseconds(10);
        }

        detectedInterrupt++;

        delayMicroseconds(750);

        detectedReadyForFirstChar++;

        for (i = 0; i < 8; i++) {

            if (digitalRead(comRxPin) == HIGH) {
                bitWrite(data_in, i, 1);
                detectedHigh++;
            }
            else {
                bitWrite(data_in, i, 0);
                detectedLow++;
            }
            delayMicroseconds(1000);
        }
        attachInterrupt(comRxPinInterruptId, data_incoming, RISING);
    }
}

void data_incoming(){

    int i, good = 0;
    detachInterrupt(comRxPinInterruptId);
    delay(2100);
    if (digitalRead(comRxPin) == HIGH) {
        detectedInterrupt++;
    }
    attachInterrupt(comRxPinInterruptId, data_incoming, RISING);

    /*for (i = 0; i < 100; i++) {

        delay(20);
        good = 1;
        if (digitalRead(comRxPin) == LOW){
            good = 0;
            break;
        }
    }

    if (good == 1) {

        data_in = 0;

        detectedInterrupt++;
*/
        /*detachInterrupt(comRxPinInterruptId);

        while (digitalRead(comRxPin) == LOW) {
            delay(10);
        }

        delay(750);*/

       /* detectedReadyForFirstChar++;

        for (i = 0; i < 8; i++) {

            if (digitalRead(comRxPin) == HIGH) {
                bitWrite(data_in, i, 1);
                detectedHigh++;
            }
            else {
                bitWrite(data_in, i, 0);
                detectedLow++;
            }
            delay(1000);
        }*/
        //attachInterrupt(comRxPinInterruptId, data_incoming, RISING);
    //}
}

int linePreviousState = LOW;

void loop() {

  /*  int newState = digitalRead(comRxPin);
    if (newState != linePreviousState) {
        Board.Trace(StringFormat.Format("new state %d to %d", linePreviousState, newState));
    }
    linePreviousState = newState;*/

    _onBoardLed.Blink();

    boolean executed = false;
    WindowsCommand winCommand = Board.GetWindowsConsoleCommand();
    
    if (detectedInterrupt > 0) {
        Board.Trace(StringFormat.Format("detectedInterrupt %d", detectedInterrupt));
        Board.Delay(200);
    }
    if (detectedReadyForFirstChar > 0) {
        Board.Trace(StringFormat.Format("detectedReadyForFirstChar %d data in:%d, detectedLow:%d, detectedHigh:%d", detectedReadyForFirstChar, data_in, detectedLow, detectedHigh));
        Board.Delay(200);
    }
    

 /*   int comStart = digitalRead(comRxPin);
    if (comStart == HIGH && comOn == false) {
        Board.SendWindowsConsoleCommand("Com start detected", true);
        comOn = true;
        _onBoardLed.SetBlinkMode(80);
    }    
    else if (comStart == LOW && comOn == true) {
        Board.SendWindowsConsoleCommand("Com stop detected", true);
        comOn = false;
        _onBoardLed.SetBlinkMode(1000);
    }*/

    if (winCommand.Command != "") {

        if (winCommand.Command == "test") {

            if (comOn) {
                Board.SendWindowsConsoleCommand("Request Com to stop?", true);
                digitalWrite(comTxPin, LOW);
            }
            else {
                Board.SendWindowsConsoleCommand("Request Com to start?", true);
                digitalWrite(comTxPin, HIGH);
            }
            executed = true;
        }
        if (executed) {
            Board.Trace(StringFormat.Format("[%s executed]", winCommand.Command.c_str()));
        }
        else {
            Board.Trace(StringFormat.Format("[Invalid command:%s]", winCommand.Command.c_str()));
        }
    }
}
