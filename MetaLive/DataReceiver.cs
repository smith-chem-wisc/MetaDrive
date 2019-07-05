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
using System.Threading.Tasks;
using System.Collections.Generic;

using Thermo.Interfaces.ExactiveAccess_V1;
using Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer;
using IMsScan = Thermo.Interfaces.InstrumentAccess_V2.MsScanContainer.IMsScan;

using Thermo.Interfaces.InstrumentAccess_V1.Control.Scans;

using MassSpectrometry;
using MzLibUtil;
using Chemistry;

namespace MetaLive
{
	/// <summary>
	/// Show incoming data packets and signals of acquisition start, acquisition stop and each scan.
	/// </summary>
	class DataReceiver
	{
        IScans m_scans = null;
        public IExactiveInstrumentAccess InstrumentAccess { get; set; }
        public IMsScanContainer ScanContainer { get; set; }

        bool isTakeOver = false;
        bool dynamicExclude = true;
        bool placeUserDefinedScan = true;
        int BoxCarScanNum = 0;
        bool TimeIsOver = false;

        static object lockerExclude = new object();
        static object lockerScan = new object();

        internal DataReceiver(Parameters parameters)
        {
            Parameters = parameters;
            UserDefinedScans = new Queue<UserDefinedScan>();
            DynamicExclusionList = new DynamicExclusionList();
            BoxDynamic = new Queue<double>();

            if (Parameters.BoxCarScanSetting.BoxCarStatic && Parameters.MS2ScanSetting.DoMS2)
            {
                AddScanIntoQueueAction = AddScanIntoQueue_StaticBoxMS2FromFullScan;
                Console.WriteLine("AddScanIntoQueueAction = StaticBoxMS2FromFullScan.");

            }
            if (Parameters.BoxCarScanSetting.BoxCarStatic && !Parameters.MS2ScanSetting.DoMS2)
            {
                AddScanIntoQueueAction = AddScanIntoQueue_StaticBoxNoMS2;
                Console.WriteLine("AddScanIntoQueueAction = StaticBoxNoMS2.");
            }
        }

        Parameters Parameters { get; set; }
        Queue<UserDefinedScan> UserDefinedScans { get; set; }
        DynamicExclusionList DynamicExclusionList { get; set; }
        Queue<double> BoxDynamic { get; set; }

        public Action<IMsScan> AddScanIntoQueueAction { get; set; }

        internal void DetectStartSignal()
        {
            ScanContainer.MsScanArrived += Orbitrap_MsScanArrived_TakeOver;

            while(!isTakeOver)
            {
                Thread.Sleep(300);
                Console.WriteLine("Connected.");
            }
            Console.WriteLine("Detect Start Signal!");

            ScanContainer.MsScanArrived -= Orbitrap_MsScanArrived_TakeOver;
        }

        private void Orbitrap_MsScanArrived_TakeOver(object sender, MsScanEventArgs e)
        {
            using (IMsScan scan = (IMsScan)e.GetScan())
            {
                TakeOverInstrumentMessage(scan);
            }
        }

        internal void DoJob()
		{
            Thread childThreadCheckTime = new Thread(CheckTime);
            childThreadCheckTime.IsBackground = true;
            childThreadCheckTime.Start();
            Console.WriteLine("Start Thread for checking time!");

            Thread childThreadExclusionList = new Thread(DynamicExclusionListDeqeue);
            childThreadExclusionList.IsBackground = true;
            childThreadExclusionList.Start();
            Console.WriteLine("Start Thread for exclusion list!");


            Thread childThreadPlaceScan = new Thread(PlaceScan);
            childThreadPlaceScan.IsBackground = true;
            childThreadPlaceScan.Start();
            Console.WriteLine("Start Thread for Place Scan!");



            using (IExactiveInstrumentAccess instrument = Connection.GetFirstInstrument())
			{
                using (m_scans = instrument.Control.GetScans(false))
                {                    
                    IMsScanContainer orbitrap = instrument.GetMsScanContainer(0);
                    Console.WriteLine("Waiting for scans on detector " + orbitrap.DetectorClass + "...");

                    orbitrap.AcquisitionStreamOpening += Orbitrap_AcquisitionStreamOpening;
                    orbitrap.AcquisitionStreamClosing += Orbitrap_AcquisitionStreamClosing;
                    orbitrap.MsScanArrived += Orbitrap_MsScanArrived;

                    Thread.CurrentThread.Join(Parameters.GeneralSetting.TotalTimeInMinute*60000);

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

                //TO THINK: If the coming scan is MS2 scan, start the timing of the scan precursor into exclusion list. Currently, start when add the scan precursor.
                if (!IsMS1Scan(scan))
                {
                    Console.WriteLine("MS2 Scan arrived.");
                }
                else
                {
                    Console.WriteLine("MS1 Scan arrived.");
                    if (!TimeIsOver)
                    {
                        AddScanIntoQueueAction(scan);
                    }
                }

            }
        }

        private void Orbitrap_AcquisitionStreamOpening(object sender, MsAcquisitionOpeningEventArgs e)
        {
            Console.WriteLine("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream opens (start of method)");
        }

        private void Orbitrap_AcquisitionStreamClosing(object sender, EventArgs e)
		{
            dynamicExclude = false;
            placeUserDefinedScan = false;

            Console.WriteLine("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream closed (end of method)");            
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
            try
            {
                //TO DO: should I use spining or blocking
                while (dynamicExclude)
                {
                    Thread.Sleep(300);

                    DateTime dateTime = DateTime.Now;

                    //Console.WriteLine("Check the dynamic exclusionList.");

                    lock (lockerExclude)
                    {
                        for (int i = 0; i < DynamicExclusionList.exclusionList.Count; i++)
                        {
                            if ((dateTime - DynamicExclusionList.exclusionList.Peek().Item3).Seconds < Parameters.MS1IonSelecting.ExclusionDuration * 1000)
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
            catch (Exception)
            {
                Console.WriteLine("DynamicExclusionListDeqeue Exception!");
            }
        }

        private void PlaceScan()
        {
            try
            {
                //TO DO: should I use spining or blocking
                while (placeUserDefinedScan)
                {
                    Thread.Sleep(30); //TO DO: How to control the Thread

                    //Console.WriteLine("Check the UserDefinedScans.");

                    lock (lockerScan)
                    {
                        if (UserDefinedScans.Count > 0)
                        {
                            var x = UserDefinedScans.Dequeue();
                            {
                                switch (x.UserDefinedScanType)
                                {
                                    case UserDefinedScanType.FullScan:
                                        FullScan.PlaceFullScan(m_scans, Parameters);
                                        break;
                                    case UserDefinedScanType.DataDependentScan:
                                        DataDependentScan.PlaceMS2Scan(m_scans, Parameters, x.Mass_Charges.First());
                                        break;
                                    case UserDefinedScanType.BoxCarScan:
                                        if (Parameters.BoxCarScanSetting.BoxCarDynamic)
                                        {
                                            BoxCarScan.PlaceBoxCarScan(m_scans, Parameters, x.dynamicBox);
                                        }
                                        else
                                        {
                                            BoxCarScan.PlaceBoxCarScan(m_scans, Parameters);
                                        }
                                        break;
                                    default:
                                        break;
                                }

                            }
                        } 
                       
                    }
                }
            }

            catch (Exception)
            {
                Console.WriteLine("PlaceScan Exception!");
            }
        }

        private void CheckTime()
        {
            Thread.CurrentThread.Join(Parameters.GeneralSetting.TotalTimeInMinute*60000);
            TimeIsOver = true;
        }

        private void AddScanIntoQueue(IMsScan scan)
        {
            try
            {   
                //Is MS1 Scan
                if (scan.HasCentroidInformation && IsMS1Scan(scan))
                {
                    if (IsBoxCarScan(scan))
                    {
                        BoxCarScanNum--;
                    }
                    
                    string scanNumber;
                    scan.CommonInformation.TryGetValue("ScanNumber", out scanNumber);
                    Console.WriteLine("MS1 Scan arrived. Is BoxCar Scan: {0}. Deconvolute.", IsBoxCarScan(scan));

                    var addMS2Scans = DeconvoluteAndAddIn(scan);

                    if (!Parameters.BoxCarScanSetting.BoxCarDynamic && !Parameters.BoxCarScanSetting.BoxCarStatic)
                    {
                        lock (lockerScan)
                        {
                            UserDefinedScans.Enqueue(new UserDefinedScan(UserDefinedScanType.FullScan));
                        }
                    }
           
                    if (Parameters.BoxCarScanSetting.BoxCarDynamic)
                    {
                        if (addMS2Scans)
                        {
                            if (IsBoxCarScan(scan))
                            {
                                lock (lockerScan)
                                {
                                    UserDefinedScans.Enqueue(new UserDefinedScan(UserDefinedScanType.FullScan));
                                }
                            }
                            else
                            {
                                var dynamicBoxCarScan = new UserDefinedScan(UserDefinedScanType.BoxCarScan);
                                dynamicBoxCarScan.dynamicBox.Add(BoxDynamic.Dequeue());  //Only add one mass for dynamic box currently   
                                lock (lockerScan)
                                {
                                    UserDefinedScans.Enqueue(dynamicBoxCarScan);
                                }
                            }
                        }
                        else
                        {
                            lock (lockerScan)
                            {
                                UserDefinedScans.Enqueue(new UserDefinedScan(UserDefinedScanType.FullScan));
                            }
                        }
                    }

                    if (Parameters.BoxCarScanSetting.BoxCarStatic)
                    {
                        if (BoxCarScanNum == 0)
                        {
                            lock (lockerScan)
                            {
                                UserDefinedScans.Enqueue(new UserDefinedScan(UserDefinedScanType.FullScan));
                                UserDefinedScans.Enqueue(new UserDefinedScan(UserDefinedScanType.BoxCarScan));
                            }
                            BoxCarScanNum = Parameters.BoxCarScanSetting.BoxCarScans;
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("AddScanIntoQueue Exception!");
            }
        }

        private void AddScanIntoQueue_StaticBoxNoMS2(IMsScan scan)
        {
            try
            {
                //Is MS1 Scan
                if (scan.HasCentroidInformation && IsMS1Scan(scan))
                {
                    if (IsBoxCarScan(scan))
                    {
                        BoxCarScanNum--;
                    }

                    string scanNumber;
                    scan.CommonInformation.TryGetValue("ScanNumber", out scanNumber);
                    Console.WriteLine("MS1 Scan arrived. Is BoxCar Scan: {0}.", IsBoxCarScan(scan));

                    if (BoxCarScanNum == 0)
                    {
                        lock (lockerScan)
                        {
                            UserDefinedScans.Enqueue(new UserDefinedScan(UserDefinedScanType.FullScan));
                            UserDefinedScans.Enqueue(new UserDefinedScan(UserDefinedScanType.BoxCarScan));
                        }
                        BoxCarScanNum = Parameters.BoxCarScanSetting.BoxCarScans;
                    }

                }
            }
            catch (Exception)
            {
                Console.WriteLine("AddScanIntoQueue Exception!");
            }
        }

        //In StaticBox, the MS1 scan contains a lot of features. There is no need to extract features from BoxCar Scans.
        private void AddScanIntoQueue_StaticBoxMS2FromFullScan(IMsScan scan)
        {
            try
            {
                //Is MS1 Scan
                if (scan.HasCentroidInformation && IsMS1Scan(scan))
                {
                    string scanNumber;
                    scan.CommonInformation.TryGetValue("ScanNumber", out scanNumber);
                    Console.WriteLine("MS1 Scan arrived. Is BoxCar Scan: {0}.", IsBoxCarScan(scan));

                    if (IsBoxCarScan(scan))
                    {
                        BoxCarScanNum--;
                    }
                    else
                    {
                        DeconvoluteAndAddIn(scan);
                    }

                    if (BoxCarScanNum == 0)
                    {
                        lock (lockerScan)
                        {
                            UserDefinedScans.Enqueue(new UserDefinedScan(UserDefinedScanType.FullScan));
                            UserDefinedScans.Enqueue(new UserDefinedScan(UserDefinedScanType.BoxCarScan));
                        }
                        BoxCarScanNum = Parameters.BoxCarScanSetting.BoxCarScans;
                    }

                }
            }
            catch (Exception)
            {
                Console.WriteLine("AddScanIntoQueue Exception!");
            }
        }

        private bool DeconvoluteAndAddIn(IMsScan scan)
        {
            var spectrum = TurnScan2Spectrum(scan);

            DeconvolutionParameter deconvolutionParameter = new DeconvolutionParameter();
            var IsotopicEnvelopes = spectrum.Deconvolute(GetMzRange(scan), deconvolutionParameter).OrderByDescending(p => p.totalIntensity).ToArray(); //Ordered by intensity
            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute {1}", DateTime.Now, IsotopicEnvelopes.Count());

            if (IsotopicEnvelopes.Count() > 0)
            {
                if (!IsBoxCarScan(scan) && Parameters.BoxCarScanSetting.BoxCarDynamic)
                {
                    BoxDynamic.Enqueue(IsotopicEnvelopes.First().monoisotopicMass);
                }
                
                int topN = 0;
                foreach (var iso in IsotopicEnvelopes)
                {
                    if (topN >= Parameters.MS1IonSelecting.TopN)
                    {
                        break;
                    }
                    lock (lockerExclude)
                    {
                        //TO DO: DynamicExclusion for Top-down has different logic.
                        if (DynamicExclusionList.isNotInExclusionList(iso.monoisotopicMass, 1.25))
                        {
                            var dataTime = DateTime.Now;
                            DynamicExclusionList.exclusionList.Enqueue(new Tuple<double, int, DateTime>(iso.monoisotopicMass, iso.charge, dataTime));
                            Console.WriteLine("ExclusionList Enqueue: {0}", DynamicExclusionList.exclusionList.Count);

                            var theScan = new UserDefinedScan(UserDefinedScanType.DataDependentScan);
                            //TO DO: get the best Mz.
                            theScan.Mass_Charges.Add(new Tuple<double, int>(iso.monoisotopicMass, iso.charge));
                            lock (lockerScan)
                            {
                                UserDefinedScans.Enqueue(theScan);
                                Console.WriteLine("dataDependentScans increased.");
                            }
                            topN++;
                        }
                    }
                }
                if (topN > 0)
                {
                    return true;
                }
                return false;
            }
            return false;
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

        private bool IsBoxCarScan(IMsScan scan)
        {
            //TO DO: better way to check if is boxcar scan.
            string value;
            if (scan.CommonInformation.TryGetValue("LowMass", out value))
            {   
                Console.WriteLine("IsBoxCarScan: " + value + "," + Parameters.BoxCarScanSetting.BoxCarMzRangeLowBound.ToString());
                if (value == Parameters.BoxCarScanSetting.BoxCarMzRangeLowBound.ToString())
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
