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

Imports System.Reflection
Imports System.Drawing
Imports DynamicSugar
Imports MadeInTheUSB.GPIO
Imports MadeInTheUSB.spi
Imports MadeInTheUSB.Components.APA
Imports MadeInTheUSB.WinUtil
Imports System.Text
Imports System.Threading
' http://converter.telerik.com/

Module Demo



    Function GetAssemblyProduct() As String
        Return My.Application.Info.ProductName
    End Function
    

    Private Sub Cls(nusbio As Nusbio)

        Console.Clear()
        ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue)
        ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue)
        ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, String.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan)
        ConsoleEx.WriteMenu(-1, 2, "T)est Set values  L)ED Fade in/out")
        ConsoleEx.WriteMenu(-1, 6, "Q)uit")
    End Sub

    Public Sub LedFadeInFadeOut(ad5290 As AD5290_DigitalPotentiometer)

        Console.Clear()
        ConsoleEx.TitleBar(0, "AD5290 - LED Fade In Fade Out", ConsoleColor.Yellow, ConsoleColor.DarkBlue)
        ConsoleEx.WriteMenu(-1, 1, "Q)uit")
        ConsoleEx.Gotoxy(0, 2)
        Dim wait As Integer = 20

        Dim goOn As Boolean = True

        While (goOn)

            For i As Integer = 0 To ad5290.MaxDigitalValue Step 2

                ConsoleEx.WriteLine(0, 2, String.Format("Digital Potentiometer Step:{0}, Resistence:{1:00000}, Current:{2:0.000}", i, ad5290.Resistance, ad5290.Amps), ConsoleColor.DarkCyan)
                ad5290.SetValue(i)
                Thread.Sleep(wait)
                If (Console.KeyAvailable) Then
                    If (Console.ReadKey().Key = ConsoleKey.Q) Then
                        goOn = False
                    End If
                End If
            Next
            Thread.Sleep(wait * 50)
            For i As Integer = ad5290.MaxDigitalValue To 0 Step -2

                ConsoleEx.WriteLine(0, 2, String.Format("Digital Potentiometer Step:{0}, Resistence:{1:00000}, Current:{2:0.000}", i, ad5290.Resistance, ad5290.Amps), ConsoleColor.DarkCyan)
                ad5290.SetValue(i)
                Thread.Sleep(wait)
                If (Console.KeyAvailable) Then
                    If (Console.ReadKey().Key = ConsoleKey.Q) Then
                        goOn = False
                    End If
                End If
            Next
        End While
        Console.WriteLine("Hit any key to continue")
        Console.ReadKey()
    End Sub

    Public Sub DemoDigitalPot(ad5290 As AD5290_DigitalPotentiometer)

        Console.Clear()
        ConsoleEx.TitleBar(0, "AD5290 Digital Potentiometer Demo", ConsoleColor.Yellow, ConsoleColor.DarkBlue)
        ConsoleEx.WriteMenu(-1, 1, "Q)uit")
        ConsoleEx.Gotoxy(0, 2)

        For i = 0 To ad5290.MaxDigitalValue Step 4

            ad5290.SetValue(i)
            Console.WriteLine("Digital Potentiometer Step:{0}, Resistence:{1}, Current:{2}", ad5290.CurrentStep, ad5290.Resistance, ad5290.Amps)
            If (Console.KeyAvailable) Then
                If (Console.ReadKey().Key = ConsoleKey.Q) Then
                    Exit For
                End If
            End If
        Next
        Console.WriteLine("Hit any key to continue")
        Console.ReadKey()
    End Sub

    Public Sub Run()

        Console.WriteLine("Nusbio Initializing")
        Dim serialNumber = Nusbio.Detect()
        If serialNumber Is Nothing Then
            Console.WriteLine("Nusbio not detected")
            Return
        End If

        Using nusbioD = New Nusbio(serialNumber:=serialNumber)

            Dim referenceVoltage = 12.44 ' 9.26
            Dim resistance = 10000
            Dim ad5290_10k As AD5290_DigitalPotentiometer = New AD5290_DigitalPotentiometer(nusbioD, resistance, referenceVoltage)

            Cls(nusbioD)

            While nusbioD.Loop()

                If Console.KeyAvailable Then

                    Dim kk = Console.ReadKey(True)
                    Dim blinkMode = kk.Modifiers = ConsoleModifiers.Shift
                    Dim key = kk.Key

                    If key = ConsoleKey.Q Then
                        Exit While
                    End If
                    If key = ConsoleKey.T Then
                        DemoDigitalPot(ad5290_10k)
                    End If
                    If key = ConsoleKey.L Then
                        LedFadeInFadeOut(ad5290_10k)
                    End If
                    Cls(nusbioD)
                End If
            End While
        End Using
        Console.Clear()
    End Sub
End Module
