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

namespace UnitTest
{
    [TestFixture]
    public class UnitTest_Exclusion
    {
        [SetUp]
        public static void SetUp()
        {
            //Initiate Element
            UsefulProteomicsDatabases.Loaders.LoadElements();
        }

        [Test]
        public static void DbcExclusionTest()
        {
            List<double> mzs1 = new List<double> { 515.45477, 545.71698, 579.76239, 618.23534, 662.44122, 713.32074, 772.67920, 842.83148, 1029.90552, 1158.51563 };
            List<double> mzs2 = new List<double> { 515.51178, 545.77563, 579.82434, 618.41199, 662.51282, 713.39734, 772.76312, 842.92328, 927.11493, 1030.01489, 1158.63989, 1324.016846};

            var DynamicDBCExclusionList = new DynamicDBCExclusionList();
            DynamicDBCExclusionList.DBCExclusionList.Enqueue(new DynamicDBCValue(mzs1.ToArray(), 0, DateTime.Now));

            var x = DynamicDBCExclusionList.MatchExclusionList(mzs2.ToArray(), 0.1);
        }
    }
}


