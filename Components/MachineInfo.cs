/*
   Copyright (C) 2015 MadeInTheUSB LLC
   Written by FT for MadeInTheUSB

   The MIT License (MIT)

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in
        all copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
        THE SOFTWARE.
  
    MIT license, all text above must be included in any redistribution
*/

using System;
using MadeInTheUSB;
using MadeInTheUSB.GPIO;
using System.Diagnostics;

namespace MadeInTheUSB
{
    public class MachineInfo
    {
        public PerformanceCounter CpuPercent;
        public PerformanceCounter DiskReadBytePerSec; 
        public PerformanceCounter DiskWriteBytePerSec; 

        public MachineInfo()
        {
            InitPerformanceCounters();
        }
        
        private void InitPerformanceCounters()
        {
            CpuPercent                       = new PerformanceCounter();
            CpuPercent.CategoryName          = "Processor";
            CpuPercent.CounterName           = "% Processor Time";
            CpuPercent.InstanceName          = "_Total";

            DiskReadBytePerSec               = new PerformanceCounter(); 
            DiskReadBytePerSec.CategoryName  = "PhysicalDisk"; 
            DiskReadBytePerSec.CounterName   = "Disk Read Bytes/sec"; 
            DiskReadBytePerSec.InstanceName  = "_Total"; 
 
            DiskWriteBytePerSec              = new PerformanceCounter(); 
            DiskWriteBytePerSec.CategoryName = "PhysicalDisk"; 
            DiskWriteBytePerSec.CounterName  = "Disk Write Bytes/sec"; 
            DiskWriteBytePerSec.InstanceName = "_Total";
        }
    }
}
