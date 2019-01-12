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
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var DataDir = AppDomain.CurrentDomain.BaseDirectory;
            var ElementsLocation = Path.Combine(DataDir, @"Data", @"elements.dat");
            Loaders.LoadElements(ElementsLocation);

            //For Deconvolution, generate avagine model first.      
            MzSpectrumBU.DoNeucodeModel = false;
            var test = new MzSpectrumBU(new double[]{ 1}, new double[] { 1 }, true);


            //Console.WriteLine("----------------------------");
            //new CustomScansTandemByArrival().DoJob(300000);

            //Thread.CurrentThread.Join(60000);

            Console.WriteLine("----------------------------");
            var dataReceiver = new DataReceiver();
            dataReceiver.DoJob(3600000);

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
