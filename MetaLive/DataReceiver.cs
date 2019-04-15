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
using System.Collections.Generic;

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
        IScans m_scans = null;
        bool isTakeOver = false;
        bool dynamicExclude = true;

        static object locker = new object();

        internal DataReceiver(Parameters parameters)
        {
            Parameters = parameters;
            UserDefinedScans = new Queue<UserDefinedScan>();
            DynamicExclusionList = new DynamicExclusionList();
        }

        Parameters Parameters { get; set; }
        Queue<UserDefinedScan> UserDefinedScans { get; set; }
        DynamicExclusionList DynamicExclusionList { get; set; }

        internal void DoJob(int timeInMicrosecond)
		{       
            using (IExactiveInstrumentAccess instrument = Connection.GetFirstInstrument())
			{
                using (m_scans = instrument.Control.GetScans(false))
                {                    
                    IMsScanContainer orbitrap = instrument.GetMsScanContainer(0);
                    Console.WriteLine("Waiting for scans on detector " + orbitrap.DetectorClass + "...");

                    orbitrap.AcquisitionStreamOpening += Orbitrap_AcquisitionStreamOpening;
                    orbitrap.AcquisitionStreamClosing += Orbitrap_AcquisitionStreamClosing;
                    orbitrap.MsScanArrived += Orbitrap_MsScanArrived;

                    Thread.CurrentThread.Join(timeInMicrosecond);

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
                Console.WriteLine("==================================================");
                Console.WriteLine("\n{0:HH:mm:ss,fff} scan with {1} centroids arrived", DateTime.Now, scan.CentroidCount);

                if (isTakeOver)
                {
                    //TO DO: If the coming scan is MS2 scan, start the timing of the scan precursor into exclusion list. Currently, start when add the scan precursor.
                    if (!IsMS1Scan(scan))
                    {
                        Console.WriteLine("MS2 Scan arrived.");
                    }


                    if (scan.HasCentroidInformation && IsMS1Scan(scan))
                    {                     
                        Console.WriteLine("MS1 Scan arrived. Deconvolute:");

                        var spectrum = TurnScan2Spectrum(scan);

                        var IsotopicEnvelopes = spectrum.DeconvoluteBU(GetMzRange(scan), 2, 8, 5.0, 3);
                        Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute {1}", DateTime.Now, IsotopicEnvelopes.Count());

                        List<double> topNMzs = new List<double>();
                        foreach (var iso in IsotopicEnvelopes)
                        {
                            if (topNMzs.Count > Parameters.MS1IonSelecting.TopN)
                            {
                                continue;
                            }
                            if (DynamicExclusionList.isNotInExclusionList(iso.peaks.First().mz, 1.25))
                            {
                                topNMzs.Add(iso.peaks.First().mz);
                            }
                        }

                        Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute After Exclude {1}", DateTime.Now, topNMzs.Count);

                        if (topNMzs.Count() > 0)
                        {
                            foreach (var mz in topNMzs)
                            {
                                var theScan = new UserDefinedScan(UserDefinedScanType.DataDependentScan);
                                theScan.MZ = mz;
                                UserDefinedScans.Enqueue(theScan);
                                Console.WriteLine("dataDependentScans increased.");

                                lock (locker)
                                {
                                    var dataTime = DateTime.Now;
                                    DynamicExclusionList.exclusionList.Enqueue(new Tuple<double, DateTime>(mz, dataTime));
                                    Console.WriteLine("ExclusionList Enqueue: {0}", DynamicExclusionList.exclusionList.Count);
                                }
                            }                        
                        }
                     
                        while (UserDefinedScans.Count > 0)
                        {                           
                            var x = UserDefinedScans.Dequeue();
                            {
                                DataDependentScan.PlaceMS2Scan(m_scans, Parameters, x.MZ);     
                            }
                        }

                        FullScan.PlaceFullScan(m_scans, Parameters);
                        BoxCarScan.PlaceBoxCarScan(m_scans, Parameters);
                        BoxCarScan.PlaceBoxCarScan(m_scans, Parameters);
                    } 
                }
                else
                {
                    TakeOverInstrumentMessage(scan);
                }
            }
        }

		private void Orbitrap_AcquisitionStreamClosing(object sender, EventArgs e)
		{
            dynamicExclude = false;
			Console.WriteLine("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream closed (end of method)");            
        }

		private void Orbitrap_AcquisitionStreamOpening(object sender, MsAcquisitionOpeningEventArgs e)
		{
            try
            {
                Thread childThreadExclusionList = new Thread(DynamicExclusionListDeqeue);
                childThreadExclusionList.IsBackground = true;
                childThreadExclusionList.Start();
                Console.WriteLine("Start Thread for exclusion list!");
            }
            catch (Exception)
            {
                Console.WriteLine("DynamicExclusionListDeqeue Exception!");
            }

            Console.WriteLine("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream opens (start of method)");
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

                    if (x.First().Low == 374.0 && x.First().High == 1751.0)
                    {
                        Console.WriteLine("Instrument take over Scan by IAPI is dectected.");
                        isTakeOver = true;
                        FullScan.PlaceFullScan(m_scans, Parameters);
                    }
                }
            }
            catch { }
        }

        private void DynamicExclusionListDeqeue()
        {
            while (dynamicExclude)
            {
                Thread.Sleep(Parameters.MS1IonSelecting.ExclusionDuration);

                DateTime dateTime = DateTime.Now;

                Console.WriteLine("Check the dynamic exclusionList.");

                lock (locker)
                {
                    for (int i = 0; i < DynamicExclusionList.exclusionList.Count; i++)
                    {
                        if ((dateTime - DynamicExclusionList.exclusionList.Peek().Item2).TotalMilliseconds < 15000)
                        {
                            Console.WriteLine("The dynamic exclusionList is OK. Now: {0:HH:mm:ss,fff}, Peek: {1:HH:mm:ss,fff}", dateTime, DynamicExclusionList.exclusionList.Peek().Item2);
                            break;
                        }
                        else
                        {

                            DynamicExclusionList.exclusionList.Dequeue();
                            Console.WriteLine("{0:HH:mm:ss,fff} ExclusionList Dequeue: {1}", dateTime, DynamicExclusionList.exclusionList.Count);

                        }
                    }

                }
            }
        }

        private void PlaceScan()
        {

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

        private MzSpectrumBU TurnScan2Spectrum(IMsScan scan)
        {
            return new MzSpectrumBU(scan.Centroids.Select(p=>p.Mz).ToArray(), scan.Centroids.Select(p => p.Intensity).ToArray(), true);
        }

        private MzRange GetMzRange(IMsScan scan)
        {
            return new MzRange(scan.Centroids.First().Mz, scan.Centroids.Last().Mz);
        }

        private double getPrecusorMass(IMsScan scan)
        {
            object a;
            if (scan.CommonInformation.TryGetRawValue("Masses", out a))
            {
                var b = (double[])a;
                Console.WriteLine("Current ms2 scan mass: {0}", b);
                return b.First();
            }
            return 0;
        }
    }
}
