'#Const DEMO_WITH_4_8x8_LED_MATRIX_CHAINED = True
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
' http://converter.telerik.com/

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Drawing
Imports MadeInTheUSB
Imports MadeInTheUSB.Adafruit
Imports MadeInTheUSB.Component
Imports MadeInTheUSB.i2c
Imports MadeInTheUSB.GPIO
Imports MadeInTheUSB.WinUtil
Imports MadeInTheUSB.Display

Namespace NusbioMatrixApp

    Class Demo
        Private Const DEFAULT_BRIGTHNESS_DEMO As Integer = 5
        Private Const ConsoleUserStatusRow As Integer = 10

        Private Class Coordinate
            Public X As Int16, Y As Int16

            Public Sub New(xx As Integer, yy As Integer)
                Me.X = xx
                Me.Y = yy
            End Sub
        End Class

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

            ConsoleEx.WriteMenu(-1, 4, "0) Animation demo  1) Images demo")
            ConsoleEx.WriteMenu(-1, 5, "P)erformance test  L)andscape demo  A)xis demo")
            ConsoleEx.WriteMenu(-1, 6, " T)ext demo  R)otate demo  B)rigthness demo")
            ConsoleEx.WriteMenu(-1, 7, " C)lear All  Q)uit I)nit Devices")

            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue)
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, String.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio__1.SerialNumber, nusbio__1.Description), ConsoleColor.Black, ConsoleColor.DarkCyan)
        End Sub

        Private Shared smileBmp As New List(Of String)() From { _
            "B00111100", _
            "B01000010", _
            "B10100101", _
            "B10000001", _
            "B10100101", _
            "B10011001", _
            "B01000010", _
            "B00111100" _
        }

        Private Shared neutralBmp As New List(Of String)() From { _
            "B00111100", _
            "B01000010", _
            "B10100101", _
            "B10000001", _
            "B10111101", _
            "B10000001", _
            "B01000010", _
            "B00111100" _
        }

        Private Shared frownbmp As New List(Of String)() From { _
            "B00111100", _
            "B01000010", _
            "B10100101", _
            "B10000001", _
            "B10011001", _
            "B10100101", _
            "B01000010", _
            "B00111100" _
        }

        Private Shared Square00Bmp As New List(Of String)() From { _
            "B00000000", _
            "B00000000", _
            "B00000000", _
            "B00000000", _
            "B00000000", _
            "B00000000", _
            "B00000000", _
            "B00000000" _
        }

        Private Shared Square01Bmp As New List(Of String)() From { _
            "B11111111", _
            "B10000001", _
            "B10000001", _
            "B10000001", _
            "B10000001", _
            "B10000001", _
            "B10000001", _
            "B11111111" _
        }

        Private Shared Square02Bmp As New List(Of String)() From { _
            "B11111111", _
            "B10000001", _
            "B10111101", _
            "B10100101", _
            "B10100101", _
            "B10111101", _
            "B10000001", _
            "B11111111" _
        }

        Private Shared Square03Bmp As New List(Of String)() From { _
            "B11111111", _
            "B10000001", _
            "B10111101", _
            "B10110101", _
            "B10101101", _
            "B10111101", _
            "B10000001", _
            "B11111111" _
        }

        Private Shared Square04Bmp As New List(Of String)() From { _
            "B11111111", _
            "B10000001", _
            "B10111101", _
            "B10101101", _
            "B10110101", _
            "B10111101", _
            "B10000001", _
            "B11111111" _
        }

        Private Shared Square05Bmp As New List(Of String)() From { _
            "B11111111", _
            "B10000001", _
            "B10111101", _
            "B10111101", _
            "B10111101", _
            "B10111101", _
            "B10000001", _
            "B11111111" _
        }

        Private Shared Square06Bmp As New List(Of String)() From { _
            "B11111111", _
            "B11111111", _
            "B11111111", _
            "B11111111", _
            "B11111111", _
            "B11111111", _
            "B11111111", _
            "B11111111" _
        }

        Private Shared Sub PerformanceTest(matrix As NusbioMatrix, deviceIndex As Integer)

            Console.Clear()
            ConsoleEx.TitleBar(0, "Performance Test")
            ConsoleEx.WriteMenu(0, 2, "Draw images and pixel as fast as possible Q)uit")

            Dim maxRepeat As Integer = 32
            matrix.CurrentDeviceIndex = deviceIndex

            ConsoleEx.Bar(0, ConsoleUserStatusRow, "DrawBitmap Demo", ConsoleColor.Yellow, ConsoleColor.Red)
            ConsoleEx.Gotoxy(0, ConsoleUserStatusRow + 1)

            Dim images = New List(Of List(Of String))() From { _
                Square00Bmp, _
                Square02Bmp _
            }

            For rpt As Byte = 0 To maxRepeat - 1

                For Each image As List(Of String) In images

                    matrix.Clear(deviceIndex, refresh:=False)
                    matrix.DrawBitmap(0, 0, image, 8, 8, True)
                    matrix.WriteDisplay(deviceIndex)
                    Console.Write(".")
                Next
            Next
            DrawAllMatrixOnePixelAtTheTimeDemo(matrix, deviceIndex, 0, 10)
        End Sub

        Private Shared Sub ScrollDemo(matrix As NusbioMatrix, deviceIndex As Integer)
            Console.Clear()
            ConsoleEx.TitleBar(0, "Scroll Demo")
            ConsoleEx.WriteMenu(0, 2, "Q)uit")

            matrix.Clear(all:=True, refresh:=True)

            For d As Integer = 0 To matrix.DeviceCount - 1

                For x As Integer = 0 To matrix.Width - 1

                    matrix.SetLed(d, x, 0, True)
                    matrix.SetLed(d, x, 7, True)
                    matrix.SetLed(d, 0, x, True)
                Next
            Next
            matrix.WriteDisplay(all:=True)
            Thread.Sleep(1000)

            For z As Integer = 0 To 8 * 3 - 1

                matrix.ScrollPixelLeftDevices(3, 0)
                matrix.WriteDisplay(all:=True)
            Next
        End Sub

        Private Shared Sub DisplaySquareImage(matrix As NusbioMatrix, deviceIndex As Integer)
            Dim maxRepeat As Integer = 2
            matrix.CurrentDeviceIndex = deviceIndex

            Console.Clear()
            ConsoleEx.TitleBar(0, "Display Images Demo")
            ConsoleEx.WriteMenu(0, 2, "Q)uit")

            For rpt As Byte = 0 To maxRepeat - 1
                Dim images = New List(Of List(Of String))() From { _
                    Square00Bmp, _
                    Square01Bmp, _
                    Square02Bmp, _
                    Square03Bmp, _
                    Square04Bmp, _
                    Square05Bmp, _
                    Square06Bmp, _
                    Square01Bmp, _
                    Square00Bmp, _
                    Square01Bmp, _
                    Square00Bmp, _
                    Square01Bmp _
                }
                For Each image As List(Of String) In images

                    matrix.Clear(deviceIndex, refresh:=False)
                    matrix.DrawBitmap(0, 0, image, 8, 8, 1)
                    matrix.CopyToAll(deviceIndex, refreshAll:=True)
                    TimePeriod.Sleep(140)

                    If Console.KeyAvailable Then
                        If Console.ReadKey().Key = ConsoleKey.Q Then
                            Return
                        End If
                    End If
                Next
            Next
            matrix.Clear(deviceIndex, refresh:=True)
        End Sub

        Private Shared Sub DisplayImage(matrix As NusbioMatrix)

            Dim MAX_REPEAT As Integer = 3
            Dim wait As Integer = 400

            ConsoleEx.Bar(0, ConsoleUserStatusRow, "DrawBitmap Demo", ConsoleColor.Yellow, ConsoleColor.Red)

            For rpt As Byte = 0 To MAX_REPEAT - 1

                Dim images = New List(Of List(Of String))() From { _
                    neutralBmp, _
                    smileBmp, _
                    neutralBmp, _
                    frownbmp _
                }
                For Each image As List(Of String) In images

                    matrix.Clear(refresh:=False)
                    matrix.DrawBitmap(0, 0, BitUtil.ParseBinary(image), 8, 8, 1)
                    matrix.WriteDisplay()
                    TimePeriod.Sleep(wait)
                Next
            Next
            matrix.Clear()
        End Sub

        Private Shared Sub DrawRoundRectDemo(matrix As NusbioMatrix, wait As Integer, maxRepeat As Integer, deviceIndex As Integer)
            Console.Clear()
            ConsoleEx.TitleBar(0, "Draw Round Rectangle Demo")

            matrix.CurrentDeviceIndex = deviceIndex

            For rpt As Byte = 0 To maxRepeat Step 2
                matrix.Clear(deviceIndex)
                Dim yy = 0
                While yy <= 3
                    matrix.DrawRoundRect(yy, yy, 8 - (yy * 2), 8 - (yy * 2), 2, 1)
                    matrix.CopyToAll(deviceIndex, True)
                    TimePeriod.Sleep(wait)
                    yy += 1
                End While
                TimePeriod.Sleep(wait)
                yy = 2
                While yy >= 0
                    matrix.DrawRoundRect(yy, yy, 8 - (yy * 2), 8 - (yy * 2), 2, 0)
                    matrix.CopyToAll(deviceIndex, True)
                    TimePeriod.Sleep(wait)
                    yy -= 1
                End While
                matrix.Clear(deviceIndex)
                matrix.CopyToAll(deviceIndex, True)
                TimePeriod.Sleep(wait)
            Next
        End Sub

        Private Shared Sub DrawAllMatrixOnePixelAtTheTimeDemo(matrix As NusbioMatrix, deviceIndex As Integer, Optional waitAfterClear As Integer = 350, Optional maxRepeat As Integer = 4)
            Console.Clear()
            ConsoleEx.TitleBar(0, "Draw one pixel at the time demo")
            ConsoleEx.WriteMenu(0, 2, "Q)uit")

            ConsoleEx.WriteLine(0, ConsoleUserStatusRow + 1, "".PadLeft(80), ConsoleColor.Black)
            ConsoleEx.Gotoxy(0, ConsoleUserStatusRow + 1)

            For rpt As Byte = 0 To maxRepeat - 1
                matrix.Clear(deviceIndex, refresh:=True)
                TimePeriod.Sleep(waitAfterClear)
                For r As Integer = 0 To matrix.Height - 1
                    For c As Integer = 0 To matrix.Width - 1
                        matrix.CurrentDeviceIndex = deviceIndex
                        matrix.DrawPixel(r, c, True)
                        ' Only refresh the row when we light up an led
                        ' This is 8 time faster than a full refresh
                        matrix.WriteRow(deviceIndex, r)
                        Console.Write("."c)
                    Next
                Next
            Next
        End Sub

        Private Shared Sub ScrollText(matrix As NusbioMatrix, Optional deviceIndex As Integer = 0)
            Dim quit = False
            Dim speed = 10
            Dim text = "Hello World!   "

            If matrix.DeviceCount = 1 AndAlso matrix.MAX7219Wiring = NusbioMatrix.MAX7219_WIRING_TO_8x8_LED_MATRIX.OriginBottomRightCorner Then
                speed = speed * 3
            End If

            While Not quit
                Console.Clear()
                ConsoleEx.TitleBar(0, "Scroll Text")
                ConsoleEx.WriteMenu(0, 2, String.Format("Q)uit  F)aster  S)lower   Speed:{0:000}", speed))

                matrix.Clear(all:=True)
                matrix.WriteDisplay(all:=True)

                For ci As Integer = 0 To text.Length - 1

                    Dim c = text(ci)

                    ConsoleEx.WriteMenu(ci, 4, c.ToString())

                    matrix.WriteChar(deviceIndex, c)
                    ' See property matrix.MAX7218Wiring for more info
                    matrix.WriteDisplay(all:=True)

                    If speed > 0 Then
                        Thread.Sleep(speed)
                        ' Provide a better animation
                        If matrix.DeviceCount = 1 AndAlso matrix.MAX7219Wiring = NusbioMatrix.MAX7219_WIRING_TO_8x8_LED_MATRIX.OriginBottomRightCorner Then
                            Thread.Sleep(speed * 3)
                        End If
                    End If

                    For i As Integer = 0 To MAX7219.MATRIX_ROW_SIZE - 1

                        matrix.ScrollPixelLeftDevices(matrix.DeviceCount - 1, 0, 1)
                        matrix.WriteDisplay(all:=True)

                        ' Do not wait when we scrolled the last pixel, we will wait when we display the new character
                        If i < MAX7219.MATRIX_ROW_SIZE - 1 Then
                            If speed > 0 Then
                                Thread.Sleep(speed)
                            End If
                        End If

                        If Console.KeyAvailable Then
                            Select Case Console.ReadKey().Key
                                Case ConsoleKey.Q
                                    quit = True
                                    i = 100
                                    ci = 10000
                                    Exit Select
                                Case ConsoleKey.S
                                    speed += 10
                                    Exit Select
                                Case ConsoleKey.F
                                    speed -= 10
                                    If speed < 0 Then
                                        speed = 0
                                    End If
                                    Exit Select
                            End Select
                            ConsoleEx.WriteMenu(0, 2, String.Format("Q)uit  F)aster  S)lower   Speed:{0:000}", speed))
                        End If
                    Next
                Next
            End While
        End Sub

        Private Shared Sub LandscapeDemo(matrix As NusbioMatrix, Optional deviceIndex As Integer = 0)

            Console.Clear()
            ConsoleEx.TitleBar(0, "Random Landscape Demo")
            ConsoleEx.WriteMenu(0, 2, "Q)uit  F)ull speed")
            Dim landscape = New NusbioLandscapeMatrix(matrix, 0)

            Dim speed = 200 - (matrix.DeviceCount * 25)
            ' slower speed if we have 1 device rather than 4
            matrix.Clear(all:=True)
            Dim quit = False
            Dim fullSpeed = False

            While Not quit

                landscape.Redraw()

                ConsoleEx.WriteLine(0, 4, landscape.ToString(), ConsoleColor.Cyan)
                If Not fullSpeed Then
                    Thread.Sleep(speed)
                End If

                If Console.KeyAvailable Then
                    Select Case Console.ReadKey(True).Key
                        Case ConsoleKey.Q
                            quit = True
                            Exit Select
                        Case ConsoleKey.F
                            fullSpeed = Not fullSpeed
                            Exit Select
                    End Select
                End If
            End While
        End Sub

        Private Shared Sub RotateMatrix(matrix As NusbioMatrix, deviceIndex As Integer)

            Console.Clear()
            ConsoleEx.TitleBar(0, "Rotate Demo")
            ConsoleEx.WriteMenu(0, 2, "Rotate:  L)eft  R)ight  Q)uit")

            matrix.Clear(deviceIndex)
            matrix.CurrentDeviceIndex = deviceIndex
            matrix.DrawLine(0, 0, 0, matrix.Height, True)
            matrix.DrawLine(7, 0, 7, matrix.Height, True)
            matrix.DrawLine(0, 2, matrix.Width, 2, True)
            matrix.WriteDisplay(deviceIndex)

            While True
                Dim k = Console.ReadKey(True).Key
                Select Case k
                    Case ConsoleKey.Q
                        Return
                        Exit Select
                    Case ConsoleKey.L
                        matrix.RotateLeft(deviceIndex)
                        Exit Select
                    Case ConsoleKey.R
                        matrix.RotateRight(deviceIndex)
                        Exit Select
                End Select
                matrix.WriteDisplay(deviceIndex)
            End While
        End Sub

        Private Shared Sub DrawAxis(matrix As NusbioMatrix, deviceIndex As Integer)

            ConsoleEx.Bar(0, ConsoleUserStatusRow, "Draw Axis Demo", ConsoleColor.Yellow, ConsoleColor.Red)

            Console.Clear()
            ConsoleEx.TitleBar(0, "Draw Axis Demo")
            ConsoleEx.WriteMenu(0, 2, "Q)uit")


            matrix.Clear(deviceIndex)
            matrix.CurrentDeviceIndex = deviceIndex

            matrix.Clear(deviceIndex)
            matrix.CurrentDeviceIndex = deviceIndex
            matrix.DrawLine(0, 0, matrix.Width, 0, True)
            matrix.DrawLine(0, 0, 0, matrix.Height, True)
            matrix.WriteDisplay(deviceIndex)

            For i As Integer = 0 To matrix.Width - 1
                matrix.SetLed(deviceIndex, i, i, True, True)
            Next
            Dim k = Console.ReadKey()
        End Sub

        Private Shared Sub DrawOnePixelAllOverTheMatrixDemo(matrix As NusbioMatrix, deviceIndex As Integer, Optional waitAfterClear As Integer = 350, Optional maxRepeat As Integer = 4)

            ConsoleEx.Bar(0, ConsoleUserStatusRow, "DrawPixel Demo", ConsoleColor.Yellow, ConsoleColor.Red)

            For rpt As Byte = 0 To maxRepeat - 1

                For r As Integer = 0 To matrix.Height - 1

                    For c As Integer = 0 To matrix.Width - 1

                        matrix.Clear(deviceIndex)
                        matrix.CurrentDeviceIndex = deviceIndex
                        matrix.DrawPixel(r, c, True)

                        ' Only refresh the row when we light up an led
                        ' This is 8 time faster than a full refresh
                        matrix.WriteRow(deviceIndex, r)
                        Thread.Sleep(32)
                    Next
                Next
            Next
            matrix.Clear(deviceIndex)
        End Sub

        Private Shared Sub BrightnessDemo(matrix As NusbioMatrix, maxRepeat As Integer, deviceIndex As Integer)

            Console.Clear()
            ConsoleEx.TitleBar(0, "Brightness Demo")

            matrix.Clear(deviceIndex)
            matrix.CurrentDeviceIndex = deviceIndex

            Dim y = 0
            For y = 0 To matrix.Height - 1

                matrix.DrawLine(0, y, matrix.Width, y, True)
                matrix.WriteDisplay(deviceIndex)
            Next
            matrix.AnimateSetBrightness(maxRepeat - 2, deviceIndex:=deviceIndex)
            matrix.Clear(deviceIndex)
        End Sub

        Private Shared Sub DrawRectDemo(matrix As NusbioMatrix, MAX_REPEAT As Integer, wait As Integer, deviceIndex As Integer)

            Console.Clear()
            ConsoleEx.TitleBar(0, "Draw Rectangle Demo")
            ConsoleEx.WriteMenu(0, 2, "Q)uit")

            matrix.Clear(deviceIndex)
            matrix.CopyToAll(deviceIndex, refreshAll:=True)
            matrix.CurrentDeviceIndex = deviceIndex

            For rpt As Byte = 0 To MAX_REPEAT - 1 Step 3

                matrix.Clear()
                Dim y = 0

                While y <= 4

                    matrix.DrawRect(y, y, 8 - (y * 2), 8 - (y * 2), True)
                    matrix.CopyToAll(deviceIndex, refreshAll:=True)
                    TimePeriod.Sleep(wait)
                    y += 1
                End While

                TimePeriod.Sleep(wait)
                y = 4
                While y >= 1

                    matrix.DrawRect(y, y, 8 - (y * 2), 8 - (y * 2), False)
                    matrix.CopyToAll(deviceIndex, refreshAll:=True)
                    TimePeriod.Sleep(wait)
                    y -= 1
                End While
            Next
            matrix.Clear(deviceIndex)
        End Sub

        Private Shared Sub DrawCircleDemo(matrix As NusbioMatrix, wait As Integer, deviceIndex As Integer)

            Console.Clear()
            ConsoleEx.TitleBar(0, "DrawCircle Demo")

            matrix.CurrentDeviceIndex = deviceIndex
            matrix.Clear(deviceIndex)
            matrix.CopyToAll(deviceIndex, refreshAll:=True)

            Dim circleLocations = New List(Of Coordinate)() From {
                New Coordinate(4, 4),
                New Coordinate(3, 3),
                New Coordinate(5, 5),
                New Coordinate(2, 2)
            }

            For Each circleLocation As Coordinate In circleLocations

                For ray As Byte = 0 To 4

                    matrix.Clear(deviceIndex)
                    matrix.DrawCircle(circleLocation.X, circleLocation.Y, ray, 1)
                    matrix.CopyToAll(deviceIndex, refreshAll:=True)
                    TimePeriod.Sleep(wait)
                Next
            Next
        End Sub

        '
        '        static void MultiMatrixDemo(NusbioMatrix matrix)
        '        {
        '            ConsoleEx.Bar(0, ConsoleUserStatusRow, "Multi Matrix Demo", ConsoleColor.Yellow, ConsoleColor.Red);
        '            matrix.Clear(0);
        '            matrix.Clear(1);
        '
        '            var pw = new List<byte>() {1, 2, 4, 8, 16, 32, 64, 128};
        '
        '            for (var r = 0; r < 8; r++) { 
        '                for (var c = 0; c < 8; c++)
        '                {
        '                    matrix.SpiTransferBuffer(new List<byte>(){
        '                        (byte)(r+1),
        '                        pw[c],
        '                        (byte)(r+1),
        '                        pw[8-c-1],
        '                    }, software: true);
        '                    Thread.Sleep(50);
        '                }
        '            }
        '        }


        Private Shared Sub Animate(matrix As NusbioMatrix, deviceIndex As Integer)

            Dim wait As Integer = 100
            Dim maxRepeat As Integer = 5

            matrix.CurrentDeviceIndex = deviceIndex

            DrawRoundRectDemo(matrix, wait, maxRepeat, deviceIndex)

            matrix.SetRotation(0)
            DrawAllMatrixOnePixelAtTheTimeDemo(matrix, deviceIndex)

            'matrix.SetRotation(1);
            'DrawAllMatrixOnePixelAtTheTimeDemo(matrix, maxRepeat);

            'matrix.SetRotation(2);
            'DrawAllMatrixOnePixelAtTheTimeDemo(matrix, maxRepeat);

            'matrix.SetRotation(3);
            'DrawAllMatrixOnePixelAtTheTimeDemo(matrix, maxRepeat);

            SetDefaultOrientations(matrix)
            BrightnessDemo(matrix, maxRepeat, deviceIndex)
            SetBrightnesses(matrix)

            DrawCircleDemo(matrix, wait, deviceIndex)
            DrawRectDemo(matrix, maxRepeat, wait, deviceIndex)

            matrix.CurrentDeviceIndex = 0
        End Sub

        Private Shared Sub SetDefaultOrientations(matrix As NusbioMatrix)

            matrix.SetRotation(0)
        End Sub

        Private Shared Sub test1(matrix As NusbioMatrix)

            matrix.Clear(0)
            Thread.Sleep(500)
            For r As Integer = 0 To 7
                For c As Integer = 0 To 7
                    matrix.SetLed(0, r, c, True, True)
                Next
            Next
            ' WriteDisplay for every pixel
            Thread.Sleep(500)

            matrix.Clear(0)
            Thread.Sleep(500)
            For r As Integer = 0 To 7
                For c As Integer = 0 To 7
                    matrix.SetLed(0, r, c, True, c = 7)
                Next
            Next
            ' WriteDisplay for every row
            Thread.Sleep(500)

            matrix.Clear(0)
            Thread.Sleep(500)
            For r As Integer = 0 To 7
                For c As Integer = 0 To 7
                    matrix.SetLed(0, r, c, True, False)
                Next
            Next
            matrix.WriteDisplay()
            ' WriteDisplay only once
            Thread.Sleep(500)
        End Sub

        Private Shared Sub SetBrightnesses(matrix As NusbioMatrix)

            For deviceIndex As Integer = 0 To matrix.DeviceCount - 1

                matrix.SetBrightness(DEFAULT_BRIGTHNESS_DEMO, deviceIndex)
            Next
        End Sub

        Private Shared Function InitializeMatrix(nusbio As Nusbio, origin As NusbioMatrix.MAX7219_WIRING_TO_8x8_LED_MATRIX, matrixChainedCount As Integer) As NusbioMatrix

            ' How to plug the 8x8 LED Matrix directly into Nusbio
            ' -----------------------------------------------------------------------
            ' NUSBIO                          : GND VCC  7   6  5   4  3  2  1  0
            ' 8x8 LED Matrix MAX7219 base     :     VCC GND DIN CS CLK
            ' Gpio 7 act as ground so we can plug directly the 8x8 led matrix
            Dim matrix = NusbioMatrix.Initialize(nusbio, selectGpio:=NusbioGpio.Gpio5, mosiGpio:=NusbioGpio.Gpio6, clockGpio:=NusbioGpio.Gpio4, gndGpio:=NusbioGpio.Gpio7, MAX7218Wiring:=origin, _
                deviceCount:=matrixChainedCount)
            ' If you have MAX7219 LED Matrix chained together increase the number
            Return matrix
        End Function

        Public Shared Sub Run()

            Console.WriteLine("Nusbio initialization")
            Dim serialNumber = Nusbio.Detect()
            If serialNumber Is Nothing Then
                ' Detect the first Nusbio available
                Console.WriteLine("Nusbio not detected")
                Return
            End If

#If DEMO_WITH_4_8x8_LED_MATRIX_CHAINED Then
			Dim matrixChainedCount = 4
			Dim origin = NusbioMatrix.MAX7219_WIRING_TO_8x8_LED_MATRIX.OrigineUpperLeftCorner
			' Different Wiring for 4 8x8 LED Matrix sold by MadeInTheUSB
#Else
            Dim matrixChainedCount = 1
            Dim origin = NusbioMatrix.MAX7219_WIRING_TO_8x8_LED_MATRIX.OriginBottomRightCorner
#End If

            Using nusbio__1 = New Nusbio(serialNumber)
                Dim matrix = InitializeMatrix(nusbio__1, origin, matrixChainedCount)

                SetBrightnesses(matrix)
                Cls(nusbio__1)

                While nusbio__1.[Loop]()
                    If Console.KeyAvailable Then
                        Dim k = Console.ReadKey(True).Key

                        If k = ConsoleKey.C Then
                            matrix.Clear(all:=True, refresh:=True)
                        End If

                        If k = ConsoleKey.L Then
                            LandscapeDemo(matrix)
                        End If

                        If k = ConsoleKey.D0 Then
                            Animate(matrix, 0)
                        End If

                        If k = ConsoleKey.D1 Then
                            DisplaySquareImage(matrix, 0)
                        End If

                        If k = ConsoleKey.A Then
                            DrawAxis(matrix, 0)
                        End If

                        If k = ConsoleKey.R Then
                            RotateMatrix(matrix, 0)
                        End If

                        If k = ConsoleKey.S Then
                            ScrollDemo(matrix, 0)
                        End If

                        If k = ConsoleKey.B Then
                            BrightnessDemo(matrix, 5, 0)
                        End If

                        If k = ConsoleKey.P Then
                            PerformanceTest(matrix, 0)
                        End If
                        ' Speed test
                        If k = ConsoleKey.T Then
                            ScrollText(matrix)
                        End If

                        If k = ConsoleKey.I Then
                            matrix = InitializeMatrix(nusbio__1, origin, matrixChainedCount)
                        End If

                        If k = ConsoleKey.Q Then
                            Exit While
                        End If

                        Cls(nusbio__1)
                        matrix.Clear(all:=True, refresh:=True)
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
