using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using MassSpectrometry;
using UsefulProteomicsDatabases;
using System.IO;

namespace MetaLive
{
    class Program
    {
        static void Main(string[] args)
        {
            //Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var DataDir = AppDomain.CurrentDomain.BaseDirectory;
            var ElementsLocation = Path.Combine(DataDir, @"Data", @"elements.dat");
            UsefulProteomicsDatabases.Loaders.LoadElements(ElementsLocation);
            var test = new MzSpectrum(new double[]{ 1}, new double[] { 1 }, true);


            new DataReceiver().DoJob();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
