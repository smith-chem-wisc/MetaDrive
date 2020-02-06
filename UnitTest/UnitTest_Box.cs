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
using IO.MzML;

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
            Assert.AreEqual(Parameters.BoxCarScanSetting.NumberOfBoxCarScans, 2);
        }

        [Test]
        public static void test_StaticBoxCar()
        {
            Parameters parameters = new Parameters();
            parameters.BoxCarScanSetting = new BoxCarScanSetting()
            {
                NumberOfBoxCarScans = 2,
                NumberOfBoxCarBoxes = 12,
                BoxCarMzRangeLowBound = 400.0,
                BoxCarMzRangeHighBound = 1600,
                BoxCarOverlap = 2
            };

            var gammaSep = BoxCarScan.GammaDistributionSeparation(parameters);
            var staticBoxCars = BoxCarScan.GenerateStaticBoxes(gammaSep, parameters.BoxCarScanSetting.NumberOfBoxCarScans);
            BoxCarScan.BuildStaticBoxString(parameters);

            var test = BoxCarScan.StaticBoxCarScanRanges;

            Assert.AreEqual(test[1], "[(434.64,473.20),(502.56,539.25),(567.67,604.10),(632.80,669.97),(699.83,738.60),(770.48,811.73),(846.62,891.47),(930.65,980.65),(1026.06,1083.64),(1138.43,1207.86),(1278.01,1368.09),(1467.44,1600.00)]");
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

        [Test]
        public static  void BU_dynamicBoxCarRange()
        {
            string FilepathMZML = Path.Combine(TestContext.CurrentContext.TestDirectory, @"Data/20170802_QEp1_FlMe_SA_BOX0_SILAC_BoxCar_SLICED.mzML");
            MsDataFile file = Mzml.LoadAllStaticData(FilepathMZML, null);
            var scans = file.GetAllScansList();

            var spectrum = new MzSpectrumXY(scans.First().MassSpectrum.XArray, scans.First().MassSpectrum.YArray, true);
            Parameters parameters = Program.AddParametersFromFile("");
            var isos = IsoDecon.MsDeconv_Deconvolute(spectrum, spectrum.Range, parameters.DeconvolutionParameter);

            List<List<Tuple<double, double, double>>> Boxes = new List<List<Tuple<double, double, double>>>();
            BoxCarScan.GenerateDynamicBoxes_BU(isos, parameters, Boxes);
        }

    }
}
