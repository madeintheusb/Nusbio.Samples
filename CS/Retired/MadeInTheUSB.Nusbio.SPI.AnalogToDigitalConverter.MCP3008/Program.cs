using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MadeInTheUSB;
using MadeInTheUSB.GPIO;

namespace DigitalPotentiometerSample
{
    class Program
    {
        static void Main(string[] args)
        {
            MadeInTheUSB.Devices.Initialize();
            Demo.Run(args);
        }
    }
}
