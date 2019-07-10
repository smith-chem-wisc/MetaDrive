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

namespace UnitTest
{
    [TestFixture]
    public static class UnitTest_Deconv
    {
        [SetUp]
        public static void SetUp()
        {
            //Initiate Element
            UsefulProteomicsDatabases.Loaders.LoadElements();
        }

        [Test]
        public static void Test_ExtractIndicesByY()
        {
            var test = new MzSpectrumBU(new double[] { 1 }, new double[] { 1 }, true);

            int size = 3000;
            Random random = new Random(1);
            double[] X = new double[size];
            double[] Y = new double[size];
            for (int i = 0; i < size; i++)
            {
                X[i] = random.NextDouble() + random.Next();
                Y[i] = random.NextDouble() + random.Next();
            }
            Array.Sort(X);

            int circle = 10;

            long[] watch = new long[circle];
            long[] watch1 = new long[circle];

            //TO THINK: Why only the first circle consume longer time.
            for (int i = 0; i < circle; i++)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                var ol = MzSpectrumBU.ExtractIndicesByY_old(Y);
                stopwatch.Stop();
                watch[i] = stopwatch.ElapsedMilliseconds;

                Stopwatch stopwatch1 = new Stopwatch();
                stopwatch1.Start();
                var ne = MzSpectrumBU.ExtractIndicesByY_new(Y);
                stopwatch1.Stop();
                watch1[i] = stopwatch1.ElapsedMilliseconds;


                for (int j = 0; j < circle; j++)
                {
                    Assert.AreEqual(ol.ElementAt(j), ne.ElementAt(j));
                }
            }
        }

        [Test]
        public static void Test_RealData()
        {
            var test = new MzSpectrumBU(new double[] { 1 }, new double[] { 1 }, true);

            string FilepathMZML = Path.Combine(TestContext.CurrentContext.TestDirectory, "20170802_QEp1_FlMe_SA_BOX0_SILAC_BoxCar_SLICED.mzML");
            MsDataFile file = Mzml.LoadAllStaticData(FilepathMZML, null);
            var scans = file.GetAllScansList();

            var ms1scans = scans.Where(p => p.MsnOrder == 1).ToList();
            DeconvolutionParameter deconvolutionParameter = new DeconvolutionParameter();
            foreach (var s in ms1scans)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                var spectrum = new MzSpectrumBU(s.MassSpectrum.XArray, s.MassSpectrum.YArray, true);
                stopwatch.Stop();

                Stopwatch stopwatch1 = new Stopwatch();
                stopwatch1.Start();         
                var yind = spectrum.ExtractIndicesByY();
                stopwatch1.Stop();

                var first = spectrum.XArray[yind.ElementAt(0)];
                var second = spectrum.XArray[yind.ElementAt(1)];
                //Assert.That((int)first == 560);
                //Assert.That((int)second == 356);

                Stopwatch stopwatch2 = new Stopwatch();
                stopwatch2.Start();
                List<NeuCodeIsotopicEnvelop> neuCodeIsotopicEnvelops = new List<NeuCodeIsotopicEnvelop>();
                HashSet<double> seenPeaks = new HashSet<double>();
                int topN = 0;

                foreach (var peakIndex in yind)
                {
                    if (topN >= 10)
                    {
                        break;
                    }

                    if (seenPeaks.Contains(spectrum.XArray[peakIndex]))
                    {
                        continue;
                    }
                    var iso = spectrum.DeconvolutePeak(peakIndex, deconvolutionParameter);
                    if (iso == null)
                    {
                        continue;
                    }
                    neuCodeIsotopicEnvelops.Add(iso);
                    foreach (var seenPeak in iso.peaks.Select(b => b.mz))
                    {
                        seenPeaks.Add(seenPeak);
                    }
                    topN++;
                }
                stopwatch2.Stop();
            }
        }

    }
}
