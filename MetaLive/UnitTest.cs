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
            Assert.AreEqual(test, "[(400.000,498.507),(506.007,756.007),(1503.507,1600.000)]");
        }

        [Test]
        public static void dataDependentBox()
        {
            var Parameters = Program.AddParametersFromFile("");
            List<Tuple<double, int>> Mass_Charges = new List<Tuple<double, int>> { new Tuple<double, int>(750, 2), new Tuple<double, int>(800, 2) };
            var test = DataDependentScan.BuildDataDependentBoxString(Parameters, Mass_Charges);
            Assert.AreEqual(test, "[(374.757,377.257),(399.757,402.257)]");
        }
    }
}
