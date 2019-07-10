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



    }
}
