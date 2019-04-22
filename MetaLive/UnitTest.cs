using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MetaLive
{
    [TestFixture]
    public static class UnitTest
    {
        [Test]
        public static void boxCarRangeTest()
        {
            var Parameters = Program.AddParametersFromFile("");
            var x = Parameters.BoxCarScanSetting.BoxCarMsxInjectRanges;
            Assert.AreEqual(Parameters.BoxCarScanSetting.BoxCarScans, 2);
        }

        [Test]
        public static void dynamicBoxCarRange()
        {
            var Parameters = Program.AddParametersFromFile("");
            List<double> masses = new List<double> { 1500 };
            var test = BoxCarScan.BuildDynamicBoxString(Parameters, masses);
            Assert.AreEqual(test, "[(400.00,496.01),(756.01,1200.00)]");
        }
    }
}
