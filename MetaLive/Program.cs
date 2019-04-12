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
using Nett;

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
            var test = new MzSpectrumBU(new double[]{ 1}, new double[] { 1 }, true);

            //string path = args[0];
            string defaultParameterPath = Path.Combine(DataDir, @"Data", @"Parameters.toml");
            Parameters parameters = AddParametersFromFile(defaultParameterPath);


            if (parameters.TestMod)
            {
                Console.WriteLine("----------------------------");
                new CustomScansTandemByArrival().DoJob(parameters.TotalTimeInMinute*60000);
            }
            else
            {
                Console.WriteLine("----------------------------");
                var dataReceiver = new DataReceiver(parameters);
                dataReceiver.DoJob(parameters.TotalTimeInMinute * 60000);
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static private Parameters AddParametersFromFile(string filePath)
        {
            Parameters parameters = new Parameters();

            if (filePath == "")
            {
                return parameters;
            }

            var filename = Path.GetFileName(filePath);
            var theExtension = Path.GetExtension(filename).ToLowerInvariant();

            
            if (theExtension != ".toml")
            {
                return parameters;
            }

            var tomlRead = Toml.ReadFile(filePath);

            return parameters;
        }
    }
}
