using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MadeInTheUSB.GPIO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MadeInTheUSB.UnitTestsIndicator
{
    [TestClass]
    public class NusbioForMSTest
    {
        public const int RedLedGpioIndex    = 0;
        public const int YellowLedGpioIndex = 2;
        public const int GreenLedGpioIndex  = 3;
        public const int BlueLedGpioIndex   = 1;

        public static MadeInTheUSB.Nusbio nusbio;

        public static TestContext _context;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            _context = context;
            if (nusbio == null)
            {
                MadeInTheUSB.Devices.Initialize();
                var serialNumber = Nusbio.Detect();
                if (serialNumber != null) // Detect the first Nusbio available
                {
                    nusbio = new Nusbio(serialNumber);
                    // Turn yellow led on while executing unit tests
                    nusbio[YellowLedGpioIndex].High();
                }
            }
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            if (nusbio != null)
            {
                nusbio[YellowLedGpioIndex].Low();

                if (nusbio[RedLedGpioIndex].State == false) // If no error turn the green led
                    nusbio[GreenLedGpioIndex].High();

                var outcome = _context.CurrentTestOutcome;
            }
        }

        public static void Notify(string testName, bool passed)
        {
            if (nusbio != null)
            {
                if (!passed)
                    nusbio[RedLedGpioIndex].High();
            }
        }

        public static void SetLed(int gpioIndex, bool on)
        {
            if (nusbio != null)
            {
                nusbio[gpioIndex].DigitalWrite(on);
            }
        }
    }
}
