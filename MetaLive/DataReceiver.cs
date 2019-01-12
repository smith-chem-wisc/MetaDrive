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
        bool scanHasbeenPlaced = false;

        internal DataReceiver()
        {
            dataDependentScans = new Stack<DataDependentScan>();
            DynamicExclusionList = new DynamicExclusionList();
        }
        Stack<DataDependentScan> dataDependentScans { get; set; }
        DynamicExclusionList DynamicExclusionList { get; set; }

        internal void DoJob(int timeInMicrosecond)
		{
            //TO DO: Maybe dangerous because the DynamicExclusionList.ExclusionList can be attended by two function at same time. Not sure about he thread here. 
            System.Timers.Timer Timer = new System.Timers.Timer(interval: 15000);
            Timer.AutoReset = true;
            Timer.Enabled = true;
            using (IExactiveInstrumentAccess instrument = Connection.GetFirstInstrument())
			{
                using (m_scans = instrument.Control.GetScans(false))
                {                    
                    IMsScanContainer orbitrap = instrument.GetMsScanContainer(0);
                    Console.WriteLine("Waiting for scans on detector " + orbitrap.DetectorClass + "...");

                    orbitrap.AcquisitionStreamOpening += Orbitrap_AcquisitionStreamOpening;
                    orbitrap.AcquisitionStreamClosing += Orbitrap_AcquisitionStreamClosing;
                    orbitrap.MsScanArrived += Orbitrap_MsScanArrived;

                    Timer.Elapsed += DynamicExclusionList.exclustionListDynamicChange;

                    Thread.CurrentThread.Join(timeInMicrosecond);

                    Timer.Elapsed -= DynamicExclusionList.exclustionListDynamicChange;

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
                    //scanHasbeenPlaced = false;
                    //If the coming scan is MS2 scan, add the scan precusor into exclusion list.
                    if (!IsMS1Scan(scan))
                    {
                        Console.WriteLine("MS2 Scan arrived.");
                        //DynamicExclusionList.exclusionList.Enqueue(getPrecusorMass(scan));
                        //Console.WriteLine("ExclusionList Enqueue: {0}", DynamicExclusionList.exclusionList.Count);
                    }


                    if (scan.HasCentroidInformation && IsMS1Scan(scan))
                    {                     
                        Console.WriteLine("MS1 Scan arrived. Deconvolute:");

                        var spectrum = TurnScan2Spectrum(scan);

                        //TO DO: add function to validate isotopicenvelopes is not in exclusion list.
                        var IsotopicEnvelopes = spectrum.DeconvoluteBU(GetMzRange(scan), 2, 8, 5.0, 3);
                        Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute {1}", DateTime.Now, IsotopicEnvelopes.Count());

                        List<double> topNMasses = new List<double>();
                        foreach (var iso in IsotopicEnvelopes)
                        {
                            if (topNMasses.Count > 10) //Select top 15 except those in exclusion list.
                            {
                                break;
                            }
                            if (DynamicExclusionList.isNotInExclusionList(iso.peaks.First().mz, 1.25))
                            {
                                topNMasses.Add(iso.peaks.First().mz);
                            }
                        }

                        Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute After Exclude {1}", DateTime.Now, topNMasses.Count);

                        if (topNMasses.Count() > 0)
                        {
                            foreach (var mass in topNMasses)
                            {
                                dataDependentScans.Push(new DataDependentScan(DateTime.Now, 15, m_scans, mass, 1.25));
                                Console.WriteLine("dataDependentScans increased.");

                                DynamicExclusionList.exclusionList.Enqueue(mass);
                                Console.WriteLine("ExclusionList Enqueue: {0}", DynamicExclusionList.exclusionList.Count);
                            }                        
                        }

                        
                        //while (dataDependentScans.Count > 0 && !scanHasbeenPlaced)
                        while (dataDependentScans.Count > 0)
                        {                           
                            var x = dataDependentScans.Pop();
                            //if (x.FinishTime > DateTime.Now)
                            {
                                x.PlaceMS2Scan();
                                //scanHasbeenPlaced = true;
                            }
                        }

                        FullMS1Scan.PlaceMS1Scan(m_scans);
                    }

                    //if (!scanHasbeenPlaced)
                    //{
                    //    FullMS1Scan.PlaceMS1Scan(m_scans);
                    //}
                }
                else
                {
                    TakeOverInstrumentMessage(scan);
                }
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
                        FullMS1Scan.PlaceMS1Scan(m_scans);
                    }
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
