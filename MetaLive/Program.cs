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

            //Initiate Element
            var DataDir = AppDomain.CurrentDomain.BaseDirectory;
            var ElementsLocation = Path.Combine(DataDir, @"Data", @"elements.dat");
            Loaders.LoadElements(ElementsLocation);
            //Loading avagine model for Deconvolution
            var test = new MzSpectrumBU(new double[]{ 1}, new double[] { 1 }, true);

            //Load parameters
            string path="";
            if (args.Count() > 0)
            {
                path = args[0];
            }
            Parameters parameters = AddParametersFromFile(path);

            //Start the task
            if (parameters.GeneralSetting.TestMod)
            {
                Console.WriteLine("----------------------------");
                new CustomScansTandemByArrival().DoJob(parameters.GeneralSetting.TotalTimeInMinute*60000);
            }
            else
            {
                Console.WriteLine("----------------------------");
                var dataReceiver = new DataReceiver(parameters);
                dataReceiver.DoJob(parameters.GeneralSetting.TotalTimeInMinute * 60000);
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static private Parameters AddParametersFromFile(string filePath)
        {
            Parameters parameters = new Parameters();

            var DataDir = AppDomain.CurrentDomain.BaseDirectory;
            string defaultParameterPath = Path.Combine(DataDir, @"Data", @"Parameters.toml");           
            var filename = Path.GetFileName(filePath);
            var theExtension = Path.GetExtension(filename).ToLowerInvariant();

            if (filePath == "" || theExtension != ".toml")
            {
                filePath = defaultParameterPath;
            }

            parameters = Toml.ReadFile<Parameters>(filePath);

            var path = Path.GetDirectoryName(filePath);

            //TO DO: If we want to write it in the future
            //Toml.WriteFile(parameters, Path.Combine(path, @"test.toml"));

            return parameters;
        }

    }
}
