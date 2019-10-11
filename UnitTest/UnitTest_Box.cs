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
        public static void test_generateMsxInjectRanges()
        {
            List<double> mz = new List<double>();
            List<double> intensity = new List<double>();
            string filePath=Path.Combine(TestContext.CurrentContext.TestDirectory, @"Data\topdown.csv");
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                int lineCount = 0;
                while (streamReader.Peek() >= 0)
                {
                    string line = streamReader.ReadLine();

                    lineCount++;
                    if (lineCount ==1)
                    {
                        continue;
                    }

                    var split = line.Split(',');
                    mz.Add(double.Parse(split[0]));
                    intensity.Add(double.Parse(split[1]));
                }
            }

            //mz = (500, 1600)
            BoxCarScanSetting boxCarScanSetting = new BoxCarScanSetting();
            boxCarScanSetting.BoxCarScans = 2;
            boxCarScanSetting.BoxCarBoxes = 12;
            boxCarScanSetting.BoxCarMzRangeLowBound = 500;
            boxCarScanSetting.BoxCarMzRangeHighBound = 1600;
            var tuples = boxCarScanSetting.SelectRanges(mz.ToArray(), intensity.ToArray());
            var ranges = boxCarScanSetting.CalculateMsxInjectRanges(tuples);
            var boxRanges = boxCarScanSetting.GenerateMsxInjectRanges(ranges);
            Assert.That(boxRanges[0] == "[(500.0,542.6),(572.0,596.1),(617.4,637.0),(655.5,673.2),(690.3,707.2),(723.8,740.3),(757.0,773.8),(791.3,809.9),(830.2,852.2),(876.8,905.8),(940.4,984.6),(1045.3,1146.1)]");

            //mz = (550, 1500)
            BoxCarScanSetting boxCarScanSetting2 = new BoxCarScanSetting();
            boxCarScanSetting2.BoxCarScans = 2;
            boxCarScanSetting2.BoxCarBoxes = 12;
            boxCarScanSetting2.BoxCarMzRangeLowBound = 550;
            boxCarScanSetting2.BoxCarMzRangeHighBound = 1550;
            var tuples2 = boxCarScanSetting2.SelectRanges(mz.ToArray(), intensity.ToArray());
            var ranges2 = boxCarScanSetting2.CalculateMsxInjectRanges(tuples2);
            var boxRanges2 = boxCarScanSetting2.GenerateMsxInjectRanges(ranges2);
            Assert.That(boxRanges2[0] == "[(550.0,572.3),(595.3,615.8),(634.6,652.4),(669.4,685.9),(702.1,718.0),(733.8,749.6),(765.6,781.9),(799.0,817.4),(837.4,859.0),(883.5,912.2),(946.5,990.4),(1050.2,1149.1)]");

        }

        [Test]
        public static void test_generateMsxInjectRanges2()
        {
            List<double> mz = new List<double>();
            List<double> intensity = new List<double>();
            string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"Data\smooth_bt.csv");
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                int lineCount = 0;
                while (streamReader.Peek() >= 0)
                {
                    string line = streamReader.ReadLine();

                    lineCount++;
                    if (lineCount == 1)
                    {
                        continue;
                    }

                    var split = line.Split(',');
                    mz.Add(double.Parse(split[0]));
                    intensity.Add(double.Parse(split[1]));
                }
            }

            BoxCarScanSetting boxCarScanSetting = new BoxCarScanSetting();
            boxCarScanSetting.BoxCarScans = 2;
            boxCarScanSetting.BoxCarBoxes = 12;
            boxCarScanSetting.BoxCarMzRangeLowBound = 400;
            boxCarScanSetting.BoxCarMzRangeHighBound = 1200;
            var tuples = boxCarScanSetting.SelectRanges(mz.ToArray(), intensity.ToArray());
            var ranges = boxCarScanSetting.CalculateMsxInjectRanges(tuples);
            var boxRanges = boxCarScanSetting.GenerateMsxInjectRanges(ranges);
            Assert.That(boxRanges[0] == "[(400.0,403.5),(415.2,427.0),(439.0,451.4),(464.3,477.8),(491.9,506.7),(522.3,538.7),(555.8,573.8),(592.6,612.5),(634.0,657.9),(685.5,718.4),(758.4,807.6),(868.0,956.6)]");
        }
    }
}
