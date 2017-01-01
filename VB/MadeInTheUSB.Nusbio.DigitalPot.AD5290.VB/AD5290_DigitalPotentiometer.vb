'   Copyright (C) 2015 MadeInTheUSB LLC
'
'   The MIT License (MIT)
'
'        Permission is hereby granted, free of charge, to any person obtaining a copy
'        of this software and associated documentation files (the "Software"), to deal
'        in the Software without restriction, including without limitation the rights
'        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
'        copies of the Software, and to permit persons to whom the Software is
'        furnished to do so, subject to the following conditions:
'
'        The above copyright notice and this permission notice shall be included in
'        all copies or substantial portions of the Software.
'
'        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
'        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
'        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
'        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
'        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
'        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
'        THE SOFTWARE.
' 
'    Written by FT for MadeInTheUSB
'    MIT license, all text above must be included in any redistribution
'
Imports MadeInTheUSB.spi

''' <summary>
''' The AD5290 does not send data back to the SPI master. There is no need to wire MISO
''' from Nusbio to the AD5290.
''' Please read the datasheet for verification
''' http://www.analog.com/media/en/technical-documentation/data-sheets/AD5290.pdf
''' The AD5290 SDO port is used for chaining and not for MISO.
''' 
''' SMD Adapter
''' ===========
'''  CLK   6  5 CS
'''  MOSI  7  4 GND
'''  SDO   8  3 VSS - Supply -> GND 
'''  VDD5V 9  2 B -> GND
'''  W    10  1 A <- 5V
''' 
''' </summary>
''' <remarks></remarks>
Public Class AD5290_DigitalPotentiometer

    Public MaxDigitalValue As Integer = 255

    Public MaxStep As Integer = 256
    Public CurrentStep As Integer = 0
    Public ReferenceVoltage As Double = 5

    Public _ohms As Integer

    Dim _spi As SPIEngine

    Public Sub New(nusbioDevice As Nusbio, ohms As Integer, refVoltage As Double)

        ' As a standard configure the SPI Bus as follow:
        ' clockGpio := NusbioGpio.Gpio0
        ' mosiGpio  := NusbioGpio.Gpio1
        ' misoGpio  := NusbioGpio.AD5290 -- No data is coming back from the AD5290 to Nusbio
        ' selectGpio:= NusbioGpio.Gpio2
        Me._spi = New MadeInTheUSB.spi.SPIEngine(nusbioDevice,
                                             selectGpio:=NusbioGpio.Gpio2,
                                             mosiGpio:=NusbioGpio.Gpio1,
                                             misoGpio:=NusbioGpio.None,
                                             clockGpio:=NusbioGpio.Gpio0,
                                             debug:=False)

        Me.ReferenceVoltage = refVoltage
        Me._ohms = ohms
    End Sub

    ReadOnly Property Resistance() As Double
        Get
            Return Me._ohms - (Me._ohms / Me.MaxStep * Me.CurrentStep)
        End Get
    End Property

    ReadOnly Property Amps() As Double
        Get
            Return ReferenceVoltage / Resistance
        End Get
    End Property

    Public Function SetValue(value As Integer) As Boolean

        Me.CurrentStep = value

        If (value < 0 Or value > MaxDigitalValue) Then
            Throw New ArgumentException(String.Format("value {0} is invalid", value))
        End If

        Dim r As SPIResult
        r = Me._spi.Transfer(value)
        Return r.Succeeded
    End Function
End Class
