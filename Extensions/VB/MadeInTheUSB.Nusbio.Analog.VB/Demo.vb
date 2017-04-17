'
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

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports MadeInTheUSB
Imports MadeInTheUSB.Adafruit
Imports MadeInTheUSB.i2c
Imports MadeInTheUSB.GPIO
Imports MadeInTheUSB.Sensor
Imports MadeInTheUSB.WinUtil
Imports MadeInTheUSB.Components

Namespace DigitalPotentiometerSample
    Class Demo

        Private Shared ad As MCP3008
        Shared _waitTime As Integer = 100
        ' 20
        Shared _demoStep As Integer = 5

        Private Shared Function GetAssemblyProduct() As String
            Dim currentAssem As Assembly = GetType(Demo).Assembly
            Dim attribs As Object() = currentAssem.GetCustomAttributes(GetType(AssemblyProductAttribute), True)
            If attribs.Length > 0 Then
                Return DirectCast(attribs(0), AssemblyProductAttribute).Product
            End If
            Return Nothing
        End Function

        Private Shared Sub Cls(nusbio__1 As Nusbio)
            Console.Clear()

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue)
            'ConsoleEx.WriteMenu(-1, 2, "0) --- ");
            ConsoleEx.WriteMenu(-1, 13, "Q)uit")
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue)
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, String.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio__1.SerialNumber, nusbio__1.Description), ConsoleColor.Black, ConsoleColor.DarkCyan)
        End Sub

        Private Shared Function CalibrateLightSensor(lightSensor As AnalogLightSensor, type As AnalogLightSensor.LightSensorType) As AnalogLightSensor
            Select Case type
                Case AnalogLightSensor.LightSensorType.CdsPhotoCell_3mm_45k_140k
                    lightSensor.AddCalibarationValue("Dark", 0, 35)
                    lightSensor.AddCalibarationValue("Office Night", 35, 100)
                    lightSensor.AddCalibarationValue("Office Day", 101, 175)
                    lightSensor.AddCalibarationValue("Outdoor Sun Light", 151, 1024)
                    Exit Select
                Case AnalogLightSensor.LightSensorType.Unknown, AnalogLightSensor.LightSensorType.CdsPhotoCell_5mm_5k_200k
                    lightSensor.AddCalibarationValue("Dark", 0, 100)
                    lightSensor.AddCalibarationValue("Office Night", 101, 299)
                    lightSensor.AddCalibarationValue("Office Day", 300, 400)
                    lightSensor.AddCalibarationValue("Outdoor Sun Light", 401, 1024)
                    Exit Select
            End Select
            Return lightSensor
        End Function

        Public Shared Sub Run(args As String())
            Console.WriteLine("Nusbio initialization")
            Dim serialNumber = Nusbio.Detect()
            If serialNumber Is Nothing Then
                ' Detect the first Nusbio available
                Console.WriteLine("Nusbio not detected")
                Return
            End If

            Using nusbio__1 = New Nusbio(serialNumber)
                Cls(nusbio__1)

                Const motionSensorAnalogPort As Integer = 0
                Const temperatureSensorAnalogPort As Integer = 1
                Const multiButtonPort As Integer = 3
                Const lightSensorAnalogPort As Integer = 6
                ' Has a pull down resistor
                Dim halfSeconds = New TimeOut(200)
                '
                '                    Mcp300X - SPI Config
                '                    gpio 0 - CLOCK
                '                    gpio 1 - MOSI
                '                    gpio 2 - MISO
                '                    gpio 3 - SELECT 
                '                

                ad = New MCP3008(nusbio__1, selectGpio:=NusbioGpio.Gpio3, mosiGpio:=NusbioGpio.Gpio1, misoGpio:=NusbioGpio.Gpio2, clockGpio:=NusbioGpio.Gpio0)
                ad.Begin()

                Dim analogTempSensor = New Tmp36AnalogTemperatureSensor(nusbio__1, 5)
                analogTempSensor.Begin()

                Dim analogMotionSensor = New AnalogMotionSensor(nusbio__1, 4)
                analogMotionSensor.Begin()

                Dim lightSensor = CalibrateLightSensor(New AnalogLightSensor(nusbio__1), AnalogLightSensor.LightSensorType.CdsPhotoCell_3mm_45k_140k)
                lightSensor.Begin()

                Dim multiButton As AnalogSensor = Nothing
                'multiButton = new AnalogSensor(nusbio, multiButtonPort);
                'multiButton.Begin();

                Dim loopCounter As ULong = 0

                While nusbio__1.[Loop]()
                    If halfSeconds.IsTimeOut() Then
                        ConsoleEx.WriteLine(0, 2, String.Format("{0,-20}, {1}", DateTime.Now, System.Math.Max(System.Threading.Interlocked.Increment(loopCounter), loopCounter - 1)), ConsoleColor.Cyan)

                        lightSensor.SetAnalogValue(ad.Read(lightSensorAnalogPort))
                        ConsoleEx.WriteLine(0, 4, String.Format("Light Sensor       : {0} (ADValue:{1:000.000}, Volt:{2:000.000})    ", lightSensor.CalibratedValue.PadRight(18), lightSensor.AnalogValue, lightSensor.Voltage), ConsoleColor.Cyan)

                        ' There is an issue with the TMP36 and the ADC MCP3008
                        ' The correction is to add a 2k resistors between the out of the temperature sensor
                        ' and the AD port and increase the value by 14%
                        Const TMP36_AccuracyCorrection As Integer = 14
                        analogTempSensor.SetAnalogValue(ad.Read(temperatureSensorAnalogPort, TMP36_AccuracyCorrection))
                        ConsoleEx.WriteLine(0, 6, String.Format("Temperature Sensor : {0:00.00}C, {1:00.00}F     (ADValue:{2:0000}, Volt:{3:000.000})      ", analogTempSensor.GetTemperature(AnalogTemperatureSensor.TemperatureType.Celsius), analogTempSensor.GetTemperature(AnalogTemperatureSensor.TemperatureType.Fahrenheit), analogTempSensor.AnalogValue, analogTempSensor.Voltage), ConsoleColor.Cyan)

                        analogMotionSensor.SetAnalogValue(ad.Read(motionSensorAnalogPort))
                        Dim motionType = analogMotionSensor.MotionDetected()
                        If motionType = DigitalMotionSensorPIR.MotionDetectedType.MotionDetected OrElse motionType = DigitalMotionSensorPIR.MotionDetectedType.None OrElse motionType = DigitalMotionSensorPIR.MotionDetectedType.SameMotionDetected Then
                            ConsoleEx.Write(0, 8, String.Format("Motion Sensor      : {0,-18} (ADValue:{1:000.000}, Volt:{2:000.000})", motionType, analogMotionSensor.AnalogValue, analogMotionSensor.Voltage), ConsoleColor.Cyan)
                        End If

                        If multiButton IsNot Nothing Then
                            multiButton.SetAnalogValue(ad.Read(multiButtonPort))
                            ConsoleEx.Write(0, 10, String.Format("Multi Button       : {0,-18} (ADValue:{1:000.000}, Volt:{2:000.000})", If(multiButton.AnalogValue > 2, "Down", "Up  "), multiButton.AnalogValue, multiButton.Voltage), ConsoleColor.Cyan)
                        End If
                    End If

                    If Console.KeyAvailable Then
                        Dim k = Console.ReadKey(True).Key

                        If k = ConsoleKey.C Then
                            Cls(nusbio__1)
                        End If
                        If k = ConsoleKey.Q Then

                            Exit While
                        End If
                        Cls(nusbio__1)
                    End If
                End While
            End Using
            Console.Clear()
        End Sub
    End Class
End Namespace

