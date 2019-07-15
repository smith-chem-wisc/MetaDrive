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

        public static Parameters AddParametersFromFile(string filePath)
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

            parameters.BoxCarScanSetting.BoxCarMsxInjectRanges = GenerateBoxCarRanges(parameters);

            WriteCurrentParameterFile(filePath, parameters);

            return parameters;
        }

        private static string[] GenerateBoxCarRanges(Parameters parameters)
        {
            List<double> mz = new List<double>();
            List<double> intensity = new List<double>();

            var DataDir = AppDomain.CurrentDomain.BaseDirectory;
            string filePath_Top = Path.Combine(DataDir, @"Data", @"topdown.csv");
            using (StreamReader streamReader = new StreamReader(filePath_Top))
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

            var tuples = parameters.BoxCarScanSetting.SelectRanges(mz.ToArray(), intensity.ToArray());

            var ranges = parameters.BoxCarScanSetting.CalculateMsxInjectRanges(tuples);

            var boxRanges = parameters.BoxCarScanSetting.GenerateMsxInjectRanges(ranges);

            return boxRanges;
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
