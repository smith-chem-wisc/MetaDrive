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
    public class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            //Initiate Element
            Loaders.LoadElements();
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
                Console.WriteLine("--------------In Test Mod--------------");
                new CustomScansTandemByArrival(parameters).DoJob(parameters.GeneralSetting.TotalTimeInMinute*60000);
            }
            else
            {
                Console.WriteLine("--------------In Gather Mod--------------");
                var dataReceiver = new DataReceiver(parameters);
                dataReceiver.InstrumentAccess = Connection.GetFirstInstrument();
                dataReceiver.ScanContainer = dataReceiver.InstrumentAccess.GetMsScanContainer(0);

                dataReceiver.DetectStartSignal();
                dataReceiver.DoJob();

                dataReceiver.ScanContainer = null;
                dataReceiver.InstrumentAccess = null;
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static public Parameters AddParametersFromFile(string filePath)
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

            WriteCurrentParameterFile(filePath, parameters);

            var path = Path.GetDirectoryName(filePath);

            return parameters;
        }

        private static void WriteCurrentParameterFile(string TrueFileName, Parameters parameters)
        {
            var apath = System.IO.Path.GetDirectoryName(TrueFileName);
            var name = System.IO.Path.GetFileNameWithoutExtension(TrueFileName);
            var time = DateTime.Now.ToString("yyyy-MM-dd-HH-mm");
            Nett.Toml.WriteFile(parameters, System.IO.Path.Combine(apath, name + "_" + time + "_running.toml"));
        }

    }
}
