using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using MassSpectrometry;
using System.Diagnostics;
using System.Threading;
using System.IO;
using MetaLive;

namespace UnitTest
{
    [TestFixture]
    public static class UnitTest_Box
    {
        [SetUp]
        public static void SetUp()
        {
            //Initiate Element
            UsefulProteomicsDatabases.Loaders.LoadElements();
        }

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
            Assert.AreEqual(test, "[(400.000,496.007),(506.007,746.007),(756.007,1496.007),(1506.007,1600.000)]");
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
