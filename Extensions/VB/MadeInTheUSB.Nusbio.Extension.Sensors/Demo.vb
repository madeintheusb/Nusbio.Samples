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

Namespace ExtensionSensors

    Class Demo

        'Private Shared ad As MCP3008
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

        Private Shared Sub Cls(nusbioDevice As Nusbio)

            Console.Clear()

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue)
            'ConsoleEx.WriteMenu(-1, 2, "0) --- ");
            ConsoleEx.WriteMenu(-1, 12, "Q)uit")
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue)
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, String.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbioDevice.SerialNumber, nusbioDevice.Description), ConsoleColor.Black, ConsoleColor.DarkCyan)
        End Sub

        Private Shared Function CalibrateLightSensor(lightSensor As AnalogLightSensor, type As AnalogLightSensor.LightSensorType) As AnalogLightSensor
            Select Case type
                Case AnalogLightSensor.LightSensorType.CdsPhotoCell_3mm_45k_140k
                    lightSensor.AddCalibarationValue("Dark", 0, 119)
                    lightSensor.AddCalibarationValue("Office Night", 120, 160)
                    lightSensor.AddCalibarationValue("Office Day", 161, 200)
                    lightSensor.AddCalibarationValue("Outdoor Sun Light", 201, 1024)
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

            Dim lightSensorAnalogPort = 2
            Dim motionSensorAnalogPort = 0
            Dim buttonSensorAnalogPort = 1
            Dim ledGpio = NusbioGpio.Gpio5

            Dim halfSeconds = New TimeOut(500)

            Using nusbioDevice = New Nusbio(serialNumber)

                Cls(nusbioDevice)
                nusbioDevice(ledGpio).AsLed.ReverseSet()

                ' Mcp300X Analog To Digital - SPI Config
                Dim ad = New MCP3008(nusbioDevice, selectGpio:=NusbioGpio.Gpio3, mosiGpio:=NusbioGpio.Gpio1, misoGpio:=NusbioGpio.Gpio2, clockGpio:=NusbioGpio.Gpio0)
                ad.Begin()

                Dim analogMotionSensor = New AnalogMotionSensor(nusbioDevice, 4)
                analogMotionSensor.Begin()

                Dim button = New AnalogButton(nusbioDevice)
                button.Begin()

                Dim lightSensor = CalibrateLightSensor(New AnalogLightSensor(nusbioDevice), AnalogLightSensor.LightSensorType.CdsPhotoCell_3mm_45k_140k)
                lightSensor.Begin()

                ' TC77 Temperature Sensor SPI
                Dim tc77 = New TC77(nusbioDevice, clockGpio:=NusbioGpio.Gpio0, mosiGpio:=NusbioGpio.Gpio1, misoGpio:=NusbioGpio.Gpio2, selectGpio:=NusbioGpio.Gpio4)

                While nusbioDevice.[Loop]()

                    If halfSeconds.IsTimeOut() Then

                        nusbioDevice(ledGpio).AsLed.ReverseSet()

                        ConsoleEx.WriteLine(0, 2, String.Format("{0,-15}", DateTime.Now, lightSensor.AnalogValue), ConsoleColor.Cyan)

                        lightSensor.SetAnalogValue(ad.Read(lightSensorAnalogPort))
                        ConsoleEx.WriteLine(0, 4, String.Format("Light Sensor       : {0,-18} (ADValue:{1:000.000}, Volt:{2:0.00})       ", lightSensor.CalibratedValue.PadRight(18), lightSensor.AnalogValue, lightSensor.Voltage), ConsoleColor.Cyan)

                        analogMotionSensor.SetAnalogValue(ad.Read(motionSensorAnalogPort))
                        Dim motionType = analogMotionSensor.MotionDetected()
                        If motionType = DigitalMotionSensorPIR.MotionDetectedType.MotionDetected OrElse motionType = DigitalMotionSensorPIR.MotionDetectedType.None Then
                            ConsoleEx.Write(0, 6, String.Format("Motion Sensor      : {0,-18} (ADValue:{1:000.000}, Volt:{2:0.00})    ", motionType, analogMotionSensor.AnalogValue, analogMotionSensor.Voltage), ConsoleColor.Cyan)
                        End If

                        ConsoleEx.WriteLine(0, 8, String.Format("Temperature Sensor : {0:0.00}C {1:0.00}F    ", tc77.GetTemperature(), tc77.GetTemperature(AnalogTemperatureSensor.TemperatureType.Fahrenheit)), ConsoleColor.Cyan)

                        button.SetAnalogValue(ad.Read(buttonSensorAnalogPort))
                        ConsoleEx.WriteLine(0, 10, String.Format("Button             : {0,-18} [{1:0000}, {2:0.00}V]   ", If(button.Down, "Down", "Up"), button.AnalogValue, button.Voltage), ConsoleColor.Cyan)
                    End If

                    If Console.KeyAvailable Then
                        Dim k = Console.ReadKey(True).Key

                        If k = ConsoleKey.C Then
                            Cls(nusbioDevice)
                        End If
                        If k = ConsoleKey.Q Then

                            Exit While
                        End If
                        Cls(nusbioDevice)
                    End If
                End While
            End Using
            Console.Clear()
        End Sub
    End Class
End Namespace

'=======================================================
'Service provided by Telerik (www.telerik.com)
'Conversion powered by NRefactory.
'Twitter: @telerik
'Facebook: facebook.com/telerik
'=======================================================
