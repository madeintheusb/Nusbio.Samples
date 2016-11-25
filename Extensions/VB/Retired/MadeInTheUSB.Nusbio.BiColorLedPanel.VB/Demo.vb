'
'    Copyright (C) 2015 MadeInTheUSB LLC
'
'    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
'    associated documentation files (the "Software"), to deal in the Software without restriction, 
'    including without limitation the rights to use, copy, modify, merge, publish, distribute, 
'    sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
'    furnished to do so, subject to the following conditions:
'
'    The above copyright notice and this permission notice shall be included in all copies or substantial 
'    portions of the Software.
'
'    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
'    LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
'    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
'    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
'    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
'

Imports System.Collections.Generic
Imports System.Linq
Imports System.Reflection
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports MadeInTheUSB
Imports MadeInTheUSB.GPIO
Imports MadeInTheUSB.WinUtil
Imports MadeInTheUSB.Components

Namespace LedConsole

    Class Demo

        Private Shared Sub ReverseLed(led As Led)
            led.ReverseSet()
        End Sub

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
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue)
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, String.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio__1.SerialNumber, nusbio__1.Description), ConsoleColor.Black, ConsoleColor.DarkCyan)

            ConsoleEx.WriteMenu(-1, 2, "0) 1) 2) 3) Animations   S)croll Animation")
            ConsoleEx.WriteMenu(-1, 3, "Q)uit")
        End Sub

        Public Shared Sub SimpleAnimation(biColorLeds As BiColorLedStrip, initialStateIndex As List(Of Integer))
            Dim wait = 450
            Console.Clear()
            ConsoleEx.WriteMenu(-1, 2, "Q)uit")
            biColorLeds.AllOff()
            Dim biColorLedStateIndex = New List(Of Integer)() From {0, 0, 0, 0}
            biColorLeds(0).StateIndex = initialStateIndex(0)
            biColorLeds(1).StateIndex = initialStateIndex(1)
            biColorLeds(2).StateIndex = initialStateIndex(2)
            biColorLeds(3).StateIndex = initialStateIndex(3)
            While True
                biColorLeds.Set(incState:=True, firstStateIndex:=1)
                MadeInTheUSB.WinUtil.TimePeriod.Sleep(wait * 2)
                If Console.KeyAvailable Then
                    Dim k = Console.ReadKey(True).Key
                    If k = ConsoleKey.Q Then
                        Exit While
                    End If
                End If
            End While
            biColorLeds.AllOff()
        End Sub

        Private Class Pattern

            Public BackgroundColor As BiColorLed.BiColorLedState
            Public ForegroundColor As BiColorLed.BiColorLedState
        End Class

        Public Shared Sub ScrollAnimation(biColorLeds As BiColorLedStrip)

            Console.Clear()
            ConsoleEx.WriteMenu(-1, 2, "Q)uit")
            Dim waitTime As Integer = 240
            biColorLeds.AllOff()
            Dim currentLed = 0
            Dim patternIndex = 0

            'Key.BackgroundColor = BiColorLed.BiColorLedState.Green, _
            'Key .ForegroundColor = BiColorLed.BiColorLedState.Red _

            Dim patterns = New Dictionary(Of Integer, Pattern)() From { _
                {0, New Pattern With {.BackgroundColor = BiColorLed.BiColorLedState.Green, .ForegroundColor = BiColorLed.BiColorLedState.Red}
                }, _
                {1, New Pattern With { _
                    .BackgroundColor = BiColorLed.BiColorLedState.Green, _
                    .ForegroundColor = BiColorLed.BiColorLedState.Yellow _
                }}, _
                {2, New Pattern() With { _
                    .BackgroundColor = BiColorLed.BiColorLedState.Red, _
                    .ForegroundColor = BiColorLed.BiColorLedState.Green _
                }}, _
                {3, New Pattern() With { _
                    .BackgroundColor = BiColorLed.BiColorLedState.Red, _
                    .ForegroundColor = BiColorLed.BiColorLedState.Yellow _
                }}, _
                {4, New Pattern() With { _
                    .BackgroundColor = BiColorLed.BiColorLedState.Yellow, _
                    .ForegroundColor = BiColorLed.BiColorLedState.Red _
                }}, _
                {5, New Pattern() With { _
                    .BackgroundColor = BiColorLed.BiColorLedState.Yellow, _
                    .ForegroundColor = BiColorLed.BiColorLedState.Green _
                }} _
            }

            Dim i As Integer

            While True

                Dim p = patterns(patternIndex)

                If currentLed = 0 Then
                    ' When we start a scroll line sequence first let set all the 4 leds with the background color
                    For i = 0 To biColorLeds.Count - 1

                        biColorLeds(i).[Set](p.BackgroundColor)
                    Next
                    MadeInTheUSB.WinUtil.TimePeriod.Sleep(waitTime)
                End If

                For i = 0 To biColorLeds.Count - 1

                    If i = currentLed Then
                        biColorLeds(currentLed).[Set](p.ForegroundColor)
                    Else
                        biColorLeds(i).[Set](p.BackgroundColor)
                    End If
                Next

                currentLed += 1
                If currentLed >= biColorLeds.Count Then
                    currentLed = 0
                    patternIndex += 1
                    If patternIndex >= patterns.Count Then
                        patternIndex = 0
                    End If
                End If

                MadeInTheUSB.WinUtil.TimePeriod.Sleep(waitTime)
                If Console.KeyAvailable Then
                    Dim k = Console.ReadKey(True).Key
                    If k = ConsoleKey.Q Then
                        Exit While
                    End If
                End If
            End While
            biColorLeds.AllOff()
        End Sub

        Public Shared Sub Run()

            Console.WriteLine("Nusbio Initializing")
            Dim serialNumber = Nusbio.Detect()
            If serialNumber Is Nothing Then
                ' Detect the first Nusbio available
                Console.WriteLine("Nusbio not detected")
                Return
            End If

            Using nusbioDevice = New Nusbio(serialNumber)

                Dim biColorLedStrip = New BiColorLedStrip(New List(Of BiColorLed)() From { _
                    New BiColorLed(nusbioDevice, NusbioGpio.Gpio0, NusbioGpio.Gpio1), _
                    New BiColorLed(nusbioDevice, NusbioGpio.Gpio2, NusbioGpio.Gpio3), _
                    New BiColorLed(nusbioDevice, NusbioGpio.Gpio4, NusbioGpio.Gpio5), _
                    New BiColorLed(nusbioDevice, NusbioGpio.Gpio6, NusbioGpio.Gpio7) _
                })

                Cls(nusbioDevice)
                Dim stateIndex0 = 0

                While nusbioDevice.[Loop]()

                    If Console.KeyAvailable Then
                        Dim k = Console.ReadKey(True).Key
                        If k = ConsoleKey.Q Then
                            Exit While
                        End If
                        If k = ConsoleKey.D0 Then
                            SimpleAnimation(biColorLedStrip, New List(Of Integer)() From {0, 0, 0, 0})
                        End If
                        If k = ConsoleKey.D1 Then
                            SimpleAnimation(biColorLedStrip, New List(Of Integer)() From {0, 1, 2, 3})
                        End If
                        If k = ConsoleKey.D2 Then
                            SimpleAnimation(biColorLedStrip, New List(Of Integer)() From {0, 1, 0, 1})
                        End If
                        If k = ConsoleKey.D3 Then
                            SimpleAnimation(biColorLedStrip, New List(Of Integer)() From {0, 3, 0, 3})
                        End If

                        If k = ConsoleKey.S Then
                            ScrollAnimation(biColorLedStrip)
                        End If

                        If k = ConsoleKey.O Then
                            nusbioDevice.SetAllGpioOutputState(PinState.Low)
                        End If
                        Cls(nusbioDevice)
                    End If
                End While
            End Using
            Console.Clear()
        End Sub
    End Class
End Namespace



