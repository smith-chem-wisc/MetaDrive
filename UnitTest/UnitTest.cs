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

namespace Test
{
    [TestFixture]
    public static class UnitTest
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
        public static void Test_ExtractIndicesByY()
        {
            int size = 2000;
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


                for (int j = 0; j < size; j++)
                {
                    Assert.AreEqual(ol.ElementAt(j), ne.ElementAt(j));
                }
            }
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
