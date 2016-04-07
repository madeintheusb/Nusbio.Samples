using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MadeInTheUSB.UnitTestsIndicator
{
    [TestClass]
    public class UnitTestsBaseClass
    {
        public TestContext TestContext { get; set; }

        [TestInitialize()]
        public void Initialize()
        {
        }

        [TestCleanup()]
        public void TestCleanup()
        {
            NusbioForMSTest.Notify(TestContext.TestName, TestContext.CurrentTestOutcome == UnitTestOutcome.Passed);
        }

        [ClassCleanup] 
        public void ClassCleanup()
        {
        }
    }

    [TestClass]
    public class UnitTests : UnitTestsBaseClass
    {
        private const int wait = 1*00;

        [TestMethod]
        public void TestMethod1()
        {
            Thread.Sleep(1*1000);
        }
        [TestMethod]
        public void TestMethod2()
        {
            Thread.Sleep(1*1000);
        }
        [TestMethod]
        public void TestMethod3()
        {
            Thread.Sleep(1*1000);
        }
        [TestMethod]
        public void TestMethod4()
        {
            //throw new ArgumentException("Something bad happen");
            Thread.Sleep(1*100);
        }
        [TestMethod]public void TestMethodG_0()  { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_1()  { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_2()  { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_3()  { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_4()  { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_5()  { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_6()  { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_7()  { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_8()  { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_9()  { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_10() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_11() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_12() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_13() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_14() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_15() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_16() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_17() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_18() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_19() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_20() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_21() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_22() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_23() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_24() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_25() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_26() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_27() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_28() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_29() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_30() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_31() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_32() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_33() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_34() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_35() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_36() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_37() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_38() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_39() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_40() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_41() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_42() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_43() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_44() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_45() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_46() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_47() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_48() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_49() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_50() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_51() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_52() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_53() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_54() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_55() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_56() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_57() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_58() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_59() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_60() { Thread.Sleep(wait); }

        [TestMethod]
        public void TestMethodG_61()
        {
            Thread.Sleep(wait);
            //throw new NotImplementedException();
        }

        [TestMethod]public void TestMethodG_62() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_63() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_64() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_65() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_66() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_67() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_68() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_69() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_70() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_71() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_72() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_73() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_74() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_75() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_76() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_77() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_78() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_79() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_80() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_81() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_82() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_83() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_84() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_85() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_86() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_87() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_88() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_89() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_90() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_91() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_92() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_93() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_94() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_95() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_96() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_97() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_98() { Thread.Sleep(wait); }
        [TestMethod]public void TestMethodG_99() { Thread.Sleep(wait); }
    }
}
