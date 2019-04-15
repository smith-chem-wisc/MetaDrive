using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MetaLive
{
    [TestFixture]
    public static class UnitTest
    {
        [Test]
        public static void boxCarRangeTest()
        {
            var Parameters = Program.AddParametersFromFile("");
            var x = Parameters.BoxCarScanSetting.BoxCarMsxInjectRanges;
            Assert.AreEqual(Parameters.BoxCarScanSetting.BoxCarScans, 2);
        }
    }
}
