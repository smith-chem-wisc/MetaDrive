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
using IO.MzML;
using MzLibUtil;

namespace UnitTest
{
    [TestFixture]
    public static class UnitTest_FeatureFind
    {
        [SetUp]
        public static void SetUp()
        {
            //Initiate Element
            UsefulProteomicsDatabases.Loaders.LoadElements();
        }

        [Test]
        public static void Test_FeatureFind()
        {
            string FilepathMZML = Path.Combine(TestContext.CurrentContext.TestDirectory, @"Data/013119_JeKoHLAI_tryptic_NCE203040_45min.mzML");
            MsDataFile file = Mzml.LoadAllStaticData(FilepathMZML, null);
            var scans = file.GetAllScansList();


            var scan9860 = scans.Where(p => p.NativeId.Contains("9860")).First();

            var reduceBuildingTime = new MzSpectrumBU(new double[] { 1 }, new double[] { 1 }, true);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var test = new MzSpectrumBU(scan9860.MassSpectrum.XArray, scan9860.MassSpectrum.YArray, true);

            stopwatch.Stop();

            Stopwatch stopwatch1 = new Stopwatch();
            stopwatch1.Start();

            var isotopenvolops = test.Deconvolute(test.Range, new DeconvolutionParameter()).OrderBy(p=>p.monoisotopicMass).ToArray();
            stopwatch1.Stop();

            Stopwatch stopwatch2 = new Stopwatch();
            stopwatch2.Start();
            var x = FeatureFinder.ExtractGlycoMS1features(isotopenvolops);
            stopwatch2.Stop();

        }
    }
}
