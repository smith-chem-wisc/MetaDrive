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
        public static void Test_RealDataDeconv()
        {            
            string FilepathMZML = Path.Combine(TestContext.CurrentContext.TestDirectory, @"Data/20170802_QEp1_FlMe_SA_BOX0_SILAC_BoxCar_SLICED.mzML");
            MsDataFile file = Mzml.LoadAllStaticData(FilepathMZML, null);
            var scans = file.GetAllScansList();

            var test = new MzSpectrumBU(scans.First().MassSpectrum.XArray, scans.First().MassSpectrum.YArray, true);
            test.DeconvolutePeak(test.ExtractIndicesByY().First(), new DeconvolutionParameter());

            var ms1scans = scans.Where(p => p.MsnOrder == 1).ToList();
            DeconvolutionParameter deconvolutionParameter = new DeconvolutionParameter();
            List<NeuCodeIsotopicEnvelop>[] neuCodeIsotopicEnvelops = new List<NeuCodeIsotopicEnvelop>[ms1scans.Count];
            long[] watch = new long[ms1scans.Count];
            long[] watch1 = new long[ms1scans.Count];
            long[] watch2 = new long[ms1scans.Count];
            int i = 0;
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
                List<NeuCodeIsotopicEnvelop> Envelops = new List<NeuCodeIsotopicEnvelop>();
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
                    Envelops.Add(iso);
                    foreach (var seenPeak in iso.peaks.Select(b => b.mz))
                    {
                        seenPeaks.Add(seenPeak);
                    }
                    topN++;
                }
                stopwatch2.Stop();

                watch[i] = stopwatch.ElapsedMilliseconds;
                watch1[i] = stopwatch1.ElapsedMilliseconds;
                watch2[i] = stopwatch2.ElapsedMilliseconds; //TO THINK: why the first element of watch2 is larger than others?
                neuCodeIsotopicEnvelops[i] = Envelops;
                i++;
            }

            Assert.That(neuCodeIsotopicEnvelops.Count() == 380);
        }

        [Test]
        public static void Test_ChargeDeconv()
        {
            string FilepathMZML = Path.Combine(TestContext.CurrentContext.TestDirectory, @"Data/2076.mzML");
            MsDataFile file = Mzml.LoadAllStaticData(FilepathMZML, null);
            var scans = file.GetAllScansList();

            DeconvolutionParameter deconvolutionParameter = new DeconvolutionParameter();
            deconvolutionParameter.DeconvolutionMaxAssumedChargeState = 60;

            Stopwatch stopwatch0 = new Stopwatch();
            stopwatch0.Start();
            var spectrum = new MzSpectrumXY(scans.First().MassSpectrum.XArray, scans.First().MassSpectrum.YArray, true);
            stopwatch0.Stop();

            Stopwatch stopwatch1 = new Stopwatch();
            stopwatch1.Start();
            var x = ChargeDecon.FindChargesForScan(spectrum, deconvolutionParameter);
            stopwatch1.Stop();

            Stopwatch stopwatch2 = new Stopwatch();
            stopwatch2.Start();
            var x2 = ChargeDecon.QuickFindChargesForScan(spectrum, deconvolutionParameter);
            stopwatch2.Stop();

            Stopwatch stopwatch3 = new Stopwatch();
            stopwatch3.Start();
            int indUp = spectrum.ExtractIndicesByY().First();
            double mass_up = spectrum.XArray[indUp];
            var highest = ChargeDecon.FindChargesForPeak(spectrum, indUp, new DeconvolutionParameter());
            stopwatch3.Stop();

            var Parameters = Program.AddParametersFromFile("");
            List<double> masses = highest.Select(p => p.Value.Mz).ToList();
            string dynamicTargets;
            string dynamicMaxITs;
            var test = BoxCarScan.BuildDynamicBoxString(Parameters, masses, out dynamicTargets, out dynamicMaxITs);
            Assert.That(test == "[(400.0,522.8),(524.8,542.2),(544.2,563.1),(565.1,585.6),(587.6,610.0),(612.0,636.5),(638.5,665.4),(667.4,697.1),(699.1,732.0),(734.0,770.5),(772.5,813.3),(815.3,861.1),(863.1,915.0),(917.0,976.0),(978.0,1045.7),(1047.7,1126.1),(1128.1,1200.0)]");
        }
    }
}
