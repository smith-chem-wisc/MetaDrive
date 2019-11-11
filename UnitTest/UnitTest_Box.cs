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
using MathNet;
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
            Assert.AreEqual(Parameters.BoxCarScanSetting.BoxCarScans, 2);
        }

        [Test]
        public static void gammaDistribution()
        {
            var test = MathNet.Numerics.Distributions.Gamma.PDF(1,1,1);
        }

        [Test]
        public static void dynamicBoxCarRange()
        {
            var Parameters = Program.AddParametersFromFile("");
            List<double> masses = new List<double> { 375.0, 698.16, 637.49, 666.43,  1237.5 };
            Tuple<double, double, double>[] tuples = new Tuple<double, double, double>[] 
            { new Tuple<double, double, double>(400.0, 636.5, 236.5), new Tuple<double, double, double>(638.5, 665.4, 26.9),
                new Tuple<double, double, double>(667.4, 697.2, 29.8), new Tuple<double, double, double>(699.2, 1200.0, 500.8) };
            string dynamicTargets;
            string dynamicMaxITs;
            var test = BoxCarScan.BuildDynamicBoxString(Parameters, tuples, out dynamicTargets, out dynamicMaxITs);
            Assert.AreEqual(test, "[(400.0,636.5),(638.5,665.4),(667.4,697.2),(699.2,1200.0)]");
            Assert.That(dynamicTargets == "[166666,166666,166666]");
            Assert.That(dynamicMaxITs == "[84,84,84]");

            var dynamicInclusion = BoxCarScan.BuildDynamicBoxInclusionString(Parameters, masses, out dynamicTargets, out dynamicMaxITs);

            Assert.AreEqual(dynamicInclusion, "[(697.660,698.660),(636.990,637.990),(665.930,666.930)]");
            Assert.That(dynamicTargets == "[166666,166666,166666]");
            Assert.That(dynamicMaxITs == "[84,84,84]");

            var dynamicInclusionForMS2 = DataDependentScan.BuildDynamicBoxInclusionString(Parameters, masses, out dynamicTargets, out dynamicMaxITs);
            Assert.AreEqual(dynamicInclusionForMS2, "[(374.300,375.700),(697.460,698.860),(636.790,638.190),(665.730,667.130),(1236.800,1238.200)]");
        }
    }
}
