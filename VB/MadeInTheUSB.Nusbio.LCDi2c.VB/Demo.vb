#Const INCLUDE_TemperatureSensorMCP0908_InDemo = False
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
Imports MadeInTheUSB.i2c
Imports MadeInTheUSB.GPIO
Imports MadeInTheUSB.WinUtil
Imports MadeInTheUSB.Display

Namespace LCDConsole

    Class Demo

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
            ConsoleEx.WriteMenu(-1, 4, "A)PI demo  Custom cH)ar demo  Nusbio R)ocks  P)erformance Test  Q)uit")
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue)
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, String.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio__1.SerialNumber, nusbio__1.Description), ConsoleColor.Black, ConsoleColor.DarkCyan)
        End Sub

        Public Shared Sub PerformanceTest(lc As LiquidCrystal_I2C_PCF8574)

            Console.Clear()
            ConsoleEx.TitleBar(0, "Performance Test")

            ConsoleEx.WriteLine(0, 3, "Running test...", ConsoleColor.Gray)

            Dim testCount = 4

            Dim sw = System.Diagnostics.Stopwatch.StartNew()
            For i As Integer = 0 To testCount - 1
                lc.Clear()
                lc.Print(0, 0, "01234567890123456789")
                lc.Print(0, 1, "01234567890123456789")
                If lc.NumLines > 2 Then
                    lc.Print(0, 2, "01234567890123456789")
                    lc.Print(0, 3, "01234567890123456789")
                End If
            Next
            sw.[Stop]()
            ConsoleEx.WriteLine(0, 4, String.Format("Total time:{0} ms, Time to write one 16 chars line:{1} ms", sw.ElapsedMilliseconds, sw.ElapsedMilliseconds / lc.NumLines / testCount), ConsoleColor.Gray)

            ConsoleEx.WriteMenu(-1, 1, "Q)uit")
            Dim k = Console.ReadKey()
        End Sub


        Private Shared Function InitializeI2CAdafruitTemperatureSensorMCP0908_ClockGpio1_DataGpio0(nusbio As Nusbio) As MCP9808_TemperatureSensor

            Dim clockPin = NusbioGpio.None
            Dim dataOutPin = NusbioGpio.None
            Dim useAdafruitI2CAdapterForNusbio = True
            If useAdafruitI2CAdapterForNusbio Then
                clockPin = NusbioGpio.Gpio1
                ' Clock should be zero, but on the Adafruit MCP9808 SCL and SDA are inversed compared to the Adafruit LED matrix
                dataOutPin = NusbioGpio.Gpio0
            Else
                clockPin = NusbioGpio.Gpio6
                ' White, Arduino A5
                ' Green, Arduino A4
                dataOutPin = NusbioGpio.Gpio5
            End If
            Dim mcp9808TemperatureSensor = New MCP9808_TemperatureSensor(nusbio, dataOutPin, clockPin)
            If Not mcp9808TemperatureSensor.Begin() Then
                Console.WriteLine("MCP9808 not detected on I2C bus. Hit any key to retry")
                Dim kk = Console.ReadKey()
                If Not mcp9808TemperatureSensor.Begin() Then
                    Return Nothing
                End If
            End If
            Return mcp9808TemperatureSensor
        End Function

        Public Shared Sub Run(args As String())

            Console.WriteLine("Nusbio initialization")
            Dim serialNumber = Nusbio.Detect()
            If serialNumber Is Nothing Then
                ' Detect the first Nusbio available
                Console.WriteLine("Nusbio not detected")
                Return
            End If

            ' The PCF8574 has limited speed
            'Nusbio.BaudRate = LiquidCrystal_I2C_PCF8574.MAX_BAUD_RATE;

            Using nusbioDevice = New Nusbio(serialNumber)
                Console.WriteLine("LCD i2c Initialization")

                Dim lcdI2C As LiquidCrystal_I2C_PCF8574

                Dim sda = NusbioGpio.Gpio1
                Dim scl = NusbioGpio.Gpio0
                sda = NusbioGpio.Gpio7 ' Directly connected into Nusbio
                scl = NusbioGpio.Gpio6
                Dim maxColumn = 16
                Dim maxRow = 2
                Dim lcdI2cId = &H27
                lcdI2C = New LiquidCrystal_I2C_PCF8574(nusbioDevice, sda, scl, maxColumn, maxRow, deviceId:=lcdI2cId)

                lcdI2C.Begin(maxColumn, maxRow)
                lcdI2C.Backlight()
                lcdI2C.Print(0, 0, "Hi!")

                ' This temperature sensor is used to also demo how to connect
                ' multiple devices directly into Nusbio by using the Nsubio Expander Extension
                ' https://squareup.com/market/madeintheusb-dot-net/ex-expander
                Dim _MCP9808_TemperatureSensor As MCP9808_TemperatureSensor = Nothing

#If INCLUDE_TemperatureSensorMCP0908_InDemo Then
                _MCP9808_TemperatureSensor = InitializeI2CAdafruitTemperatureSensorMCP0908_ClockGpio1_DataGpio0(nusbioDevice)
#End If
                Dim timeOut = New TimeOut(1000)

                Cls(nusbioDevice)

                While nusbioDevice.[Loop]()

                    If _MCP9808_TemperatureSensor IsNot Nothing AndAlso timeOut.IsTimeOut() Then
                        lcdI2C.Print(0, 0, DateTime.Now.ToString("T"))
                        lcdI2C.Print(0, 1, "Temp {0:00}C {1:00}F", _MCP9808_TemperatureSensor.GetTemperature(TemperatureType.Celsius), _MCP9808_TemperatureSensor.GetTemperature(TemperatureType.Fahrenheit))
                    End If

                    If Console.KeyAvailable Then

                        Dim k = Console.ReadKey(True).Key
                        If k = ConsoleKey.Q Then
                            nusbioDevice.ExitLoop()
                        End If
                        If k = ConsoleKey.P Then
                            PerformanceTest(lcdI2C)
                            lcdI2C.Clear()
                        End If
                        If k = ConsoleKey.C Then
                            Cls(nusbioDevice)
                            lcdI2C.Clear()
                        End If
                        If k = ConsoleKey.A Then
                            LiquidCrystalDemo.ApiDemo(lcdI2C)
                            lcdI2C.Clear()
                        End If
                        If k = ConsoleKey.R Then
                            LiquidCrystalDemo.NusbioRocks(lcdI2C, 333)
                            lcdI2C.Clear()
                        End If
                        If k = ConsoleKey.H Then
                            LiquidCrystalDemo.CustomCharDemo(lcdI2C)
                        End If
                        If k = ConsoleKey.T Then
                            LiquidCrystalDemo.ProgressBarDemo(lcdI2C)
                            LiquidCrystalDemo.NusbioRocksOrWhat(lcdI2C)
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
