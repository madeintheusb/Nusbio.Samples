@echo off
cls
echo.
echo Demo running unit tests with Nusbio Red, Green, Yellow LED
echo.
set MSTEST=D:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\MSTest.exe
set TEST_DLL=D:\DVT\MadeInTheUSB\Nusbio.Samples.TRUNK\CS\MadeInTheUSB.Nusbio.UnitTestsIndicator\bin\Debug\MadeInTheUSB.Nusbio.UnitTestsIndicator.dll

"%MSTEST%"  /testcontainer:%TEST_DLL%
