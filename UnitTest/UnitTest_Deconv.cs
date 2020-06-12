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
using MetaDrive;
using IO.MzML;
using MzLibUtil;
using Chemistry;

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
        public static void Test_RealDataDeconv()
        {            
            string FilepathMZML = Path.Combine(TestContext.CurrentContext.TestDirectory, @"Data/20170802_QEp1_FlMe_SA_BOX0_SILAC_BoxCar_SLICED.mzML");
            MsDataFile file = Mzml.LoadAllStaticData(FilepathMZML, null);
            var scans = file.GetAllScansList();

            var test = new MzSpectrumXY(scans.First().MassSpectrum.XArray, scans.First().MassSpectrum.YArray, true);


            var ms1scans = scans.Where(p => p.MsnOrder == 1).ToList();
            DeconvolutionParameter deconvolutionParameter = new DeconvolutionParameter();
           
        }

        [Test]
        public static void Test_ChargeDeconv()
        {
            string FilepathMZML = Path.Combine(TestContext.CurrentContext.TestDirectory, @"Data/2076.mzML");
            MsDataFile file = Mzml.LoadAllStaticData(FilepathMZML, null);
            var scans = file.GetAllScansList();

            DeconvolutionParameter deconvolutionParameter = new DeconvolutionParameter();

            Stopwatch stopwatch0 = new Stopwatch();
            stopwatch0.Start();
            var spectrum = new MzSpectrumXY(scans.First().MassSpectrum.XArray, scans.First().MassSpectrum.YArray, true);
            stopwatch0.Stop();

            Stopwatch stopwatch_iso = new Stopwatch();
            stopwatch_iso.Start();
            var iso = IsoDecon.MsDeconv_Deconvolute(spectrum, spectrum.Range, deconvolutionParameter);
            stopwatch_iso.Stop();

            Stopwatch stopwatch1 = new Stopwatch();
            stopwatch1.Start();
            var x = ChargeDecon.FindChargesForScan(spectrum, deconvolutionParameter);
            stopwatch1.Stop();

            //Stopwatch stopwatch2 = new Stopwatch();
            //stopwatch2.Start();
            var stopwatch2 = Stopwatch.StartNew();
            var x2 = ChargeDecon.QuickFindChargesForScan(spectrum, deconvolutionParameter);
            stopwatch2.Stop();

        //    Stopwatch stopwatch3 = new Stopwatch();
        //    stopwatch3.Start();
        //    int indUp = spectrum.ExtractIndicesByY().First();
        //    double mass_up = spectrum.XArray[indUp];
        //    var highest = ChargeDecon.FindChargesForPeak(spectrum, indUp, new DeconvolutionParameter());
        //    stopwatch3.Stop();

        //    var Parameters = Program.AddParametersFromFile("");
        //    List<double> masses = highest.Select(p => p.Value.Mz).ToList();
        //    string dynamicTargets;
        //    string dynamicMaxITs;

        //    Stopwatch stopwatch4 = new Stopwatch();
        //    stopwatch4.Start();
        //    var test = BoxCarScan.BuildDynamicBoxString(Parameters, masses, out dynamicTargets, out dynamicMaxITs);
        //    stopwatch4.Stop();
        //    Assert.That(test == "[(400.0,522.8),(524.8,542.2),(544.2,563.1),(565.1,585.6),(587.6,610.0),(612.0,636.5),(638.5,665.4),(667.4,697.1),(699.1,732.0),(734.0,770.5),(772.5,813.3),(815.3,861.1),(863.1,915.0),(917.0,976.0),(978.0,1045.7),(1047.7,1126.1),(1128.1,1200.0)]");
        }

        [Test]
        public static void Test_Charge()
        {
            //double mz = 854.64246;
            //var mass = mz.ToMass(34);
            //var monomass = 29023.596;
            //Dictionary<int, double> mz_z = new Dictionary<int, double>();
            //for (int i = 25; i <= 40; i++)
            //{
            //    mz_z.Add(i, monomass.ToMz(i));
            //}

            //double mz = 779.60669;
            //var mass = mz.ToMass(11);
            //var monomass = 8564.59355;
            //Dictionary<int, double> mz_z = new Dictionary<int, double>();
            //for (int i = 6; i <= 14; i++)
            //{
            //    mz_z.Add(i, monomass.ToMz(i));
            //}

            //double mz = 824.89209;
            //var mass = mz.ToMass(15);
            //var monomass = 12358.2722;
            //Dictionary<int, double> mz_z = new Dictionary<int, double>();
            //for (int i = 8; i <= 20; i++)
            //{
            //    mz_z.Add(i, monomass.ToMz(i));
            //}

            //double mz = 1848.41797;
            //var mass = mz.ToMass(13);
            //var monomass = 24016.33902;
            //Dictionary<int, double> mz_z = new Dictionary<int, double>();
            //for (int i = 8; i <= 20; i++)
            //{
            //    mz_z.Add(i, monomass.ToMz(i));
            //}

            double mz = 808.19220;
            var mass = mz.ToMass(21);
            var monomass = 16950.88339;
            Dictionary<int, double> mz_z = new Dictionary<int, double>();
            for (int i = 15; i <= 27; i++)
            {
                mz_z.Add(i, monomass.ToMz(i));
            }

        }

        [Test]
        public static void Test_ChargeDeconvFile()
        {
            //string FilepathMZML = "E:\\MassData\\20190912_TD_yeast_DBC\\20190912_Yeast7_DBC_FullScanFirst_T3_TopDown.mzML";
            string FilepathMZML = "E:\\MassData\\20191009_TD_F4\\20191009_J1-F4_DDA4.mzML";
            MsDataFile file = Mzml.LoadAllStaticData(FilepathMZML, null);
            var scans = file.GetAllScansList().Where(p=>p.MsnOrder == 1).ToArray();         

            DeconvolutionParameter deconvolutionParameter = new DeconvolutionParameter();

            //Stopwatch stopwatch0 = new Stopwatch();
            //stopwatch0.Start();
            //var spectrum = new MzSpectrumXY(scans[2167].MassSpectrum.XArray, scans[2167].MassSpectrum.YArray, true);
            //stopwatch0.Stop();

            //Stopwatch stopwatch_iso = new Stopwatch();
            //stopwatch_iso.Start();
            //var iso = IsoDecon.MsDeconv_Deconvolute(spectrum, spectrum.Range, deconvolutionParameter);
            //stopwatch_iso.Stop();

            //Stopwatch stopwatch1 = new Stopwatch();
            //stopwatch1.Start();
            //var x = ChargeDecon.FindChargesForScan(spectrum, deconvolutionParameter);
            //stopwatch1.Stop();

            ////Stopwatch stopwatch2 = new Stopwatch();
            ////stopwatch2.Start();
            //var stopwatch2 = Stopwatch.StartNew();
            //var x2 = ChargeDecon.QuickFindChargesForScan(spectrum, deconvolutionParameter);
            //stopwatch2.Stop();


            Tuple<int, double, long, long, long, long>[] watches = new Tuple<int, double, long, long, long, long>[scans.Length];

            for (int i = 0; i < scans.Length; i++)
            {
                Stopwatch stopwatch0 = new Stopwatch();
                stopwatch0.Start();
                var spectrum = new MzSpectrumXY(scans[i].MassSpectrum.XArray, scans[i].MassSpectrum.YArray, true);
                stopwatch0.Stop();

                Stopwatch stopwatch_iso = new Stopwatch();
                stopwatch_iso.Start();
                var iso = IsoDecon.MsDeconv_Deconvolute(spectrum, spectrum.Range, deconvolutionParameter);
                var test1 = iso.ToList();
                stopwatch_iso.Stop();

                Stopwatch stopwatch1 = new Stopwatch();
                stopwatch1.Start();
                //var x = ChargeDecon.FindChargesForScan(spectrum, deconvolutionParameter);
                stopwatch1.Stop();

                //Stopwatch stopwatch2 = new Stopwatch();
                //stopwatch2.Start();
                var stopwatch2 = Stopwatch.StartNew();
                var isoEnvelops = new List<IsoEnvelop>();
                var x2 = ChargeDecon.ChargeDeconIsoForScan(spectrum, deconvolutionParameter, out isoEnvelops);
                stopwatch2.Stop();

                watches[i] = new Tuple<int, double, long, long, long, long>(scans[i].OneBasedScanNumber, scans[i].RetentionTime, stopwatch0.ElapsedMilliseconds, stopwatch_iso.ElapsedMilliseconds, stopwatch1.ElapsedMilliseconds, stopwatch2.ElapsedMilliseconds);
            }

            var writtenFile = Path.Combine(Path.GetDirectoryName(FilepathMZML), "watches.mytsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("ScanNum\tRT\tConstruct\tIsoTime\tChargeDeconTime\tQuickChargeDeconTime");
                foreach (var theEvaluation in watches.OrderBy(p => p.Item1))
                {
                    output.WriteLine(theEvaluation.Item1.ToString() + "\t" + theEvaluation.Item2 + "\t" + theEvaluation.Item3.ToString() + "\t" + theEvaluation.Item4.ToString() + "\t" + theEvaluation.Item5 + "\t" + +theEvaluation.Item6);
                }
            }

        }

        [Test]
        public static void Test_PartnerDeconvFile()
        {
            string FilepathMZML = "E:\\MassData\\20191107\\20191107_StdMix_DSSd0d4_postmix1to1.mzML";
            MsDataFile file = Mzml.LoadAllStaticData(FilepathMZML, null);
            var scans = file.GetAllScansList().Where(p => p.MsnOrder == 1).ToArray();

            DeconvolutionParameter deconvolutionParameter = new DeconvolutionParameter
            {
                 DeconvolutionMinAssumedChargeState = 2,
                 DeconvolutionMaxAssumedChargeState = 8,             
            };

            var spectrum_test = new MzSpectrumXY(scans.Where(p=>p.OneBasedScanNumber == 19467).First().MassSpectrum.XArray, scans.Where(p => p.OneBasedScanNumber == 19467).First().MassSpectrum.YArray, true);
            var iso_test = IsoDecon.MsDeconv_Deconvolute(spectrum_test, spectrum_test.Range, deconvolutionParameter).ToList();

            Tuple<int, double, long, long, long, long>[] watches = new Tuple<int, double, long, long, long, long>[scans.Length];

            for (int i = 0; i < scans.Length; i++)
            {
                Stopwatch stopwatch0 = new Stopwatch();
                stopwatch0.Start();
                var spectrum = new MzSpectrumXY(scans[i].MassSpectrum.XArray, scans[i].MassSpectrum.YArray, true);
                stopwatch0.Stop();

                Stopwatch stopwatch_iso = new Stopwatch();
                stopwatch_iso.Start();
                var iso = IsoDecon.MsDeconv_Deconvolute(spectrum, spectrum.Range, deconvolutionParameter);

                var test1 = iso.ToList();
                stopwatch_iso.Stop();

                Stopwatch stopwatch1 = new Stopwatch();
                stopwatch1.Start();

                stopwatch1.Stop();


                var stopwatch2 = Stopwatch.StartNew();
                var isoEnvelops = new List<IsoEnvelop>();

                stopwatch2.Stop();

                watches[i] = new Tuple<int, double, long, long, long, long>(scans[i].OneBasedScanNumber, scans[i].RetentionTime, stopwatch0.ElapsedMilliseconds, stopwatch_iso.ElapsedMilliseconds, stopwatch1.ElapsedMilliseconds, stopwatch2.ElapsedMilliseconds);
            }

            var writtenFile = Path.Combine(Path.GetDirectoryName(FilepathMZML), "watches.mytsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("ScanNum\tRT\tConstruct\tIsoTime\tChargeDeconTime\tQuickChargeDeconTime");
                foreach (var theEvaluation in watches.OrderBy(p => p.Item1))
                {
                    output.WriteLine(theEvaluation.Item1.ToString() + "\t" + theEvaluation.Item2 + "\t" + theEvaluation.Item3.ToString() + "\t" + theEvaluation.Item4.ToString() + "\t" + theEvaluation.Item5 + "\t" + +theEvaluation.Item6);
                }
            }
        }


        [Test]
        public static void TestIntervals()
        {
            string FilepathMZML = "E:\\MassData\\20191107_QXL\\20191107_StdMix_DSSd0d4_postmix1to1.mzML";
            MsDataFile file = Mzml.LoadAllStaticData(FilepathMZML, null);
            var scans = file.GetAllScansList().Where(p => p.MsnOrder == 1).ToArray();


            DeconvolutionParameter deconvolutionParameter = new DeconvolutionParameter
            {
                DeconvolutionMinAssumedChargeState = 2,
                DeconvolutionMaxAssumedChargeState = 8,
    
            };


            var spectrum_test = new MzSpectrumXY(scans.Where(p => p.OneBasedScanNumber == 14995).First().MassSpectrum.XArray, scans.Where(p => p.OneBasedScanNumber == 14995).First().MassSpectrum.YArray, true);
            var iso_test = IsoDecon.MsDeconv_Deconvolute(spectrum_test, spectrum_test.Range, deconvolutionParameter).ToList();
   
            var tuples = IsoDecon.GenerateIntervals(iso_test);
            Assert.AreEqual(tuples.Count(), 5);

        }

    }
}
