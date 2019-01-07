#region legal notice
// Copyright(c) 2016 - 2018 Thermo Fisher Scientific - LSMS
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion legal notice
using System;
using System.Linq;
using System.Threading;

using Thermo.Interfaces.ExactiveAccess_V1;
using Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer;
using IMsScan = Thermo.Interfaces.InstrumentAccess_V2.MsScanContainer.IMsScan;

using Thermo.Interfaces.InstrumentAccess_V1.Control.Scans;

using MassSpectrometry;
using MzLibUtil;


namespace MetaLive
{
	/// <summary>
	/// Show incoming data packets and signals of acquisition start, acquisition stop and each scan.
	/// </summary>
	class DataReceiver
	{
        int m_scanId = 1;   // must be != 0
        IScans m_scans = null;

        internal DataReceiver() { }

		internal void DoJob()
		{
			using (IExactiveInstrumentAccess instrument = Connection.GetFirstInstrument())
			{
                using (m_scans = instrument.Control.GetScans(false))
                {
                    IMsScanContainer orbitrap = instrument.GetMsScanContainer(0);
                    Console.WriteLine("Waiting 60 seconds for scans on detector " + orbitrap.DetectorClass + "...");

                    orbitrap.AcquisitionStreamOpening += Orbitrap_AcquisitionStreamOpening;
                    orbitrap.AcquisitionStreamClosing += Orbitrap_AcquisitionStreamClosing;
                    orbitrap.MsScanArrived += Orbitrap_MsScanArrived;
                    Thread.CurrentThread.Join(30000);
                    orbitrap.MsScanArrived -= Orbitrap_MsScanArrived;
                    orbitrap.AcquisitionStreamClosing -= Orbitrap_AcquisitionStreamClosing;
                    orbitrap.AcquisitionStreamOpening -= Orbitrap_AcquisitionStreamOpening;
                }
            }
		}

		private void Orbitrap_MsScanArrived(object sender, MsScanEventArgs e)
		{
			// If examining code takes longer, in particular for some scans, it is wise
			// to use a processing queue in order to get the system as responsive as possible.

			using (IMsScan scan = (IMsScan) e.GetScan())	// caution! You must dispose this, or you block shared memory!
			{
				Console.WriteLine("\n{0:HH:mm:ss,fff} scan with {1} centroids arrived", DateTime.Now, scan.CentroidCount);

                // The common part is shared by all Thermo Fisher instruments, these settings mainly form the so called filter string
                // which also appears on top of each spectrum in many visualizers.
                Console.WriteLine("----------------Common--------------");
                Dump("Common", scan.CommonInformation);

                // The specific part is individual for each instrument type. Many values are shared by different Exactive Series models.
                Console.WriteLine("----------------Specific--------------");
                Dump("Specific", scan.SpecificInformation);

                Console.WriteLine("---------------------------------------");

                TakeOverInstrumentMessage(scan, "MSOrder", "MS");

                TakeOverInstrumentMessage(scan);

                PlaceScan();

                //if (scan.HasCentroidInformation && IsMS1Scan(scan))
                //{
                //    var spectrum = TurnScan2Spectrum(scan);

                //    var IsotopicEnvelopes = spectrum.Deconvolute(GetMzRange(scan), 2, 8, 5.0, 3);

                //    Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute {1}", DateTime.Now, IsotopicEnvelopes.ToList().Count);

                //    if (IsotopicEnvelopes.Count() > 0)
                //    {
                //        PlaceScan();
                //    }                  
                //}

            }
		}

		private void Orbitrap_AcquisitionStreamClosing(object sender, EventArgs e)
		{
			Console.WriteLine("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream closed (end of method)");            
        }

		private void Orbitrap_AcquisitionStreamOpening(object sender, MsAcquisitionOpeningEventArgs e)
		{
			Console.WriteLine("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream opens (start of method)");
        }

        private void Dump(string title, IInfoContainer container)
        {
            Console.WriteLine(title);
            foreach (string key in container.Names)
            {
                string value;
                // the source has to be "Unknown" to match all sources. Otherwise, the source has to match.
                // Sources can address values appearing in Tune files, general settings, but also values from
                // status log or additional values to a scan.
                MsScanInformationSource source = MsScanInformationSource.Unknown;   // show everything
                try
                {
                    if (container.TryGetValue(key, out value, ref source))
                    {
                        string descr = source.ToString();
                        descr = descr.Substring(0, Math.Min(11, descr.Length));
                        Console.WriteLine("   {0,-11} {1,-35} = {2}", descr, key, value);
                    }
                }
                catch { /* up to and including 2.8SP1 there is a bug displaying items which are null and if Foundation 3.1SP4 is used with CommonCore */ }
            }
        }

        private void TakeOverInstrumentMessage(IMsScan scan, string key, string expectedValue)
        {
            string value;
            try
            {
                if (scan.CommonInformation.TryGetValue(key, out value))
                {
                    if (value != expectedValue)
                    {
                        return;
                    }
                    Console.WriteLine("   {0,-35} = {1}", key, value);
                    Console.WriteLine("Instrument take over Scan by IAPI is dectected.");
                }
            }
            catch {  }
        }

        private void TakeOverInstrumentMessage(IMsScan scan)
        {
            object massRanges;
            ThermoFisher.Foundation.IO.Range[] x = new ThermoFisher.Foundation.IO.Range[] { };
            try
            {
                if (scan.CommonInformation.TryGetRawValue("MassRanges", out massRanges))
                {
                    x = (ThermoFisher.Foundation.IO.Range[])massRanges;                  
                    Console.WriteLine("{0}, {1}", x.First().Low, x.First().High);
                }
            }
            catch { }
        }

        private bool IsMS1Scan(IMsScan scan)
        {
            string value;
            if (scan.CommonInformation.TryGetValue("MSOrder", out value))
            {
                if (value == "MS")
                {
                    return true;
                }
            }
            return false;
        }

        private MzSpectrum TurnScan2Spectrum(IMsScan scan)
        {
            return new MzSpectrum(scan.Centroids.Select(p=>p.Mz).ToArray(), scan.Centroids.Select(p => p.Intensity).ToArray(), true);
        }

        private MzRange GetMzRange(IMsScan scan)
        {
            return new MzRange(scan.Centroids.Min(p =>p.Mz), scan.Centroids.Max(p => p.Mz));
        }



        private void PlaceScan()
        {
            // If no information about possible settings are available yet or if we finished our job, we bail out.
            if ((m_scanId > 10) || (m_scans.PossibleParameters.Length == 0))
            {
                return;
            }

            foreach (var item in m_scans.PossibleParameters)
            {
                Console.WriteLine(item.Name + "----" + item.DefaultValue + "----" + item.Help + "----" + item.Selection);
            }
            ICustomScan scan = m_scans.CreateCustomScan();
            scan.RunningNumber = m_scanId++;
            scan.Values["Resolution"] = "15000.0";
            scan.Values["IsolationRangeLow"] = "350";
            scan.Values["IsolationRangeLow"] = "400";
            scan.Values["FirstMass"] = "300";
            scan.Values["LastMass"] = "1700";
            scan.Values["NCE"] = "20";
            foreach (var v in scan.Values)
            {
                Console.WriteLine(v);
            }       
            Console.WriteLine("{0:HH:mm:ss,fff} placing scan {1}", DateTime.Now, scan.RunningNumber);
            m_scans.SetCustomScan(scan);
        }
    }
}
