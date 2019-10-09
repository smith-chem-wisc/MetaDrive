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

        #region Control object

        bool isTakeOver = false;
        bool firstFullScanPlaced = false;

        bool dynamicExclude = true;
        bool glycoInclude = true;
        bool placeUserDefinedScan = true;
        int BoxCarScanNum = 0;
        bool TimeIsOver = false;

        static object lockerExclude = new object();
        static object lockerScan = new object();
        static object lockerGlyco = new object();

        #endregion

        internal DataReceiver(Parameters parameters)
        {
            Parameters = parameters;
            UserDefinedScans = new Queue<UserDefinedScan>();
            DynamicExclusionList = new DynamicExclusionList();

            switch (Parameters.GeneralSetting.MethodType)
            {
                case MethodTypes.ShotGun:
                    AddScanIntoQueueAction = AddScanIntoQueue_ShotGun;
                    Console.WriteLine("AddScanIntoQueueAction = ShotGun.");
                    break;
                case MethodTypes.StaticBoxCar:
                    AddScanIntoQueueAction = AddScanIntoQueue_StaticBox;
                    Console.WriteLine("AddScanIntoQueueAction = StaticBox.");
                    break;
                case MethodTypes.DynamicBoxCar:
                    DynamicDBCExclusionList = new DynamicDBCExclusionList();
                    AddScanIntoQueueAction = AddScanIntoQueue_DynamicBox;
                    Console.WriteLine("AddScanIntoQueueAction = DynamicBox.");
                    break;
                case MethodTypes.GlycoFeature:
                    IsotopesForGlycoFeature = new IsotopesForGlycoFeature();
                    AddScanIntoQueueAction = AddScanIntoQueue_Glyco;
                    Console.WriteLine("AddScanIntoQueueAction = Glyco.");
                    break;
                case MethodTypes.Partner:
                    AddScanIntoQueueAction = AddScanIntoQueue_Partner;
                    Console.WriteLine("AddScanIntoQueueAction = NeuCode.");
                    break;
                case MethodTypes.UserDefined:
                    AddScanIntoQueueAction = AddScanIntoQueue_UserDefined;
                    Console.WriteLine("AddScanIntoQueueAction = UserDefined.");
                    break;
                default:
                    break;
            }
        }

        Parameters Parameters { get; set; }
        Queue<UserDefinedScan> UserDefinedScans { get; set; }
        public Action<IMsScan> AddScanIntoQueueAction { get; set; }

        DynamicExclusionList DynamicExclusionList { get; set; }
        DynamicDBCExclusionList DynamicDBCExclusionList { get; set; }
        IsotopesForGlycoFeature IsotopesForGlycoFeature { get; set; } 


        #region TakeOver

        internal void DetectStartSignal()
        {
            ScanContainer.MsScanArrived += Orbitrap_MsScanArrived_TakeOver;

            while(!isTakeOver)
            {
                Thread.Sleep(100);
                Console.WriteLine("Connected. Listening...");
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

        private void TakeOverInstrumentMessage(IMsScan scan)
        {
            try
            {
                if (IsTakeOverScan(scan))
                {
                    Console.WriteLine("Instrument take over Scan by IAPI is dectected.");
                    isTakeOver = true;
                    Console.WriteLine("Instrument take over duration time: {0} min", Parameters.GeneralSetting.TotalTimeInMinute);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("TakeOver Execption!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }

        #endregion

        #region DoJob Main Functions

        internal void DoJob()
		{
            Thread childThreadCheckTime = new Thread(CheckTime);
            childThreadCheckTime.IsBackground = true;
            childThreadCheckTime.Start();
            Console.WriteLine("Start Thread for checking time!");

            if (Parameters.GeneralSetting.MethodType == MethodTypes.DynamicBoxCar)
            {
                Thread childThreadDBCExclusionList = new Thread(DynamicDBCExclusionListDeque);
                childThreadDBCExclusionList.IsBackground = true;
                childThreadDBCExclusionList.Start();
                Console.WriteLine("Start Thread for DynamicBoxCar (DBC) exclusion list!");
            }


            Thread childThreadExclusionList = new Thread(DynamicExclusionListDeqeue);
            childThreadExclusionList.IsBackground = true;
            childThreadExclusionList.Start();
            Console.WriteLine("Start Thread for exclusion list!");


            //Thread childThreadPlaceScan = new Thread(PlaceScan);
            //childThreadPlaceScan.IsBackground = true;
            //childThreadPlaceScan.Start();
            //Console.WriteLine("Start Thread for Place Scan!");

            if (Parameters.GeneralSetting.MethodType == MethodTypes.GlycoFeature)
            {
                Thread childThreadGlycoInclusionList = new Thread(IsotopesForGlycoFeatureDeque);
                childThreadGlycoInclusionList.IsBackground = true;
                childThreadGlycoInclusionList.Start();
                Console.WriteLine("Start Thread for glyco Inclusion list!");
            }

            using (IExactiveInstrumentAccess instrument = Connection.GetFirstInstrument())
			{
                using (m_scans = instrument.Control.GetScans(false))
                {                    
                    IMsScanContainer orbitrap = instrument.GetMsScanContainer(0);
                    Console.WriteLine("Waiting for scans on detector " + orbitrap.DetectorClass + "...");                   

                    orbitrap.AcquisitionStreamOpening += Orbitrap_AcquisitionStreamOpening;
                    orbitrap.AcquisitionStreamClosing += Orbitrap_AcquisitionStreamClosing;
                    orbitrap.MsScanArrived += Orbitrap_MsScanArrived;

                    Thread.CurrentThread.Join(Parameters.GeneralSetting.TotalTimeInMinute * 60000);

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

                if (!IsTakeOverScan(scan))
                {
                    if (IsMS1Scan(scan))
                    {
                        Console.WriteLine("MS1 Scan arrived.");
                        if (!TimeIsOver)
                        {
                            AddScanIntoQueueAction(scan);
                        }
                    }
                    else
                    {

                        Console.WriteLine("MS2 Scan arrived.");
                    }
                }
                else if (!firstFullScanPlaced)
                {
                    FullScan.PlaceFullScan(m_scans, Parameters);
                    Console.WriteLine("Place First User defined Full Scan.");
                    firstFullScanPlaced = true;
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
            glycoInclude = false;
            placeUserDefinedScan = false;

            Console.WriteLine("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream closed (end of method)");            
        }	   

        private void PlaceScan()
        {
            try
            {
                //TO DO: should I use spining or blocking
                while (placeUserDefinedScan)
                {
                    Thread.Sleep(10); //TO DO: How to control the Thread

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
                                        if (x.dynamicBox.Count!=0)
                                        {
                                            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, x.dynamicBox);
                                        }
                                        else
                                        {
                                            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, x.Mz);
                                        }
                                        break;
                                    case UserDefinedScanType.BoxCarScan:
                                        if (Parameters.GeneralSetting.MethodType == MethodTypes.StaticBoxCar)
                                        {
                                            BoxCarScan.PlaceBoxCarScan(m_scans, Parameters);
                                        }
                                        else
                                        {
                                            BoxCarScan.PlaceBoxCarScan(m_scans, Parameters, x.dynamicBox);
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

            catch (Exception e)
            {
                Console.WriteLine("PlaceScan Exception!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }

        private void CheckTime()
        {
            Thread.CurrentThread.Join(Parameters.GeneralSetting.TotalTimeInMinute * 60000);
            TimeIsOver = true;
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

                    Console.WriteLine("Check the dynamic exclusionList.");

                    lock (lockerExclude)
                    {
                        bool toDeque = true;

                        while (toDeque && DynamicDBCExclusionList.DBCExclusionList.Count > 0)
                        {
                            if (dateTime.Subtract(DynamicDBCExclusionList.DBCExclusionList.Peek().DateTime).TotalMilliseconds < Parameters.MS1IonSelecting.ExclusionDuration * 1000)
                            {
                                Console.WriteLine("The dynamic exclusionList is OK. Now: {0:HH:mm:ss,fff}, Peek: {1:HH:mm:ss,fff}.", dateTime, DynamicDBCExclusionList.DBCExclusionList.Peek().DateTime);
                                toDeque = false;
                            }
                            else
                            {

                                DynamicDBCExclusionList.DBCExclusionList.Dequeue();
                                Console.WriteLine("{0:HH:mm:ss,fff} ExclusionList Dequeue: {1}", dateTime, DynamicDBCExclusionList.DBCExclusionList.Count);

                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("DynamicExclusionListDeqeue Exception!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }

        #endregion

        #region ScanInfo Function

        private bool IsTakeOverScan(IMsScan scan)
        {
            object massRanges;
            ThermoFisher.Foundation.IO.Range[] x = new ThermoFisher.Foundation.IO.Range[] { };

            if (scan.CommonInformation.TryGetRawValue("MassRanges", out massRanges))
            {
                x = (ThermoFisher.Foundation.IO.Range[])massRanges;
                Console.WriteLine("Take Over scan: {0}, {1}", x.First().Low, x.First().High);

                if (x.First().Low == 374.0 && x.First().High == 1751.0)
                {
                    return true;
                }
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
            string valueHigh;
            if (scan.CommonInformation.TryGetValue("LowMass", out value) && scan.CommonInformation.TryGetValue("HighMass", out valueHigh))
            {
                Console.WriteLine("IsBoxCarScan: " + value + "," + Parameters.BoxCarScanSetting.BoxCarMzRangeLowBound.ToString());

                if (value == Parameters.BoxCarScanSetting.BoxCarMzRangeLowBound.ToString() && valueHigh == Parameters.BoxCarScanSetting.BoxCarMzRangeHighBound.ToString())
                {
                    return true;
                }
            }
            return false;

        }

        #endregion

        #region ShotGun WorkFlow

        private void AddScanIntoQueue_ShotGun(IMsScan scan)
        {
            try
            {   
                //Is MS1 Scan
                if (scan.HasCentroidInformation)
                {
                    Console.WriteLine("MS1 Scan arrived. Is BoxCar Scan: {0}. Deconvolute.", IsBoxCarScan(scan));

                    DeconvoluteMS1ScanAddMS2Scan_TopN(scan);

                    lock (lockerScan)
                    {
                        UserDefinedScans.Enqueue(new UserDefinedScan(UserDefinedScanType.FullScan));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("AddScanIntoQueue_BottomUp Exception!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }


        private void AddIsotopicEnvelope2UserDefinedScans(NeuCodeIsotopicEnvelop iso, ref int theTopN)
        {
            lock (lockerExclude)
            {
                if (DynamicExclusionList.isNotInExclusionList(iso.peaks.OrderBy(p => p.intensity).Last().mz, Parameters.MS1IonSelecting.ExclusionTolerance))
                {
                    var dataTime = DateTime.Now;
                    DynamicExclusionList.exclusionList.Enqueue(new Tuple<double, int, DateTime>(iso.peaks.OrderBy(p => p.intensity).Last().mz, iso.charge, dataTime));
                    Console.WriteLine("ExclusionList Enqueue: {0}", DynamicExclusionList.exclusionList.Count);

                    var theScan = new UserDefinedScan(UserDefinedScanType.DataDependentScan);

                    //TO DO: get the best Mz.
                    theScan.Mz = iso.peaks.OrderBy(p => p.intensity).Last().mz;
                    lock (lockerScan)
                    {
                        UserDefinedScans.Enqueue(theScan);
                        Console.WriteLine("dataDependentScans increased.");
                    }
                    theTopN++;
                }
            }
        }

        private void DeconvoluteMS1ScanAddMS2Scan(IMsScan scan)
        {
            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute Start", DateTime.Now);

            var spectrum = new MzSpectrumBU(scan.Centroids.Select(p => p.Mz).ToArray(), scan.Centroids.Select(p => p.Intensity).ToArray(), true);

            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute Creat spectrum", DateTime.Now);

            var IsotopicEnvelopes = spectrum.Deconvolute(spectrum.Range, Parameters.DeconvolutionParameter);

            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute Finished", DateTime.Now, IsotopicEnvelopes.Count());

            IsotopicEnvelopes = IsotopicEnvelopes.OrderByDescending(p => p.totalIntensity).ToArray();

            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute order by intensity", DateTime.Now, IsotopicEnvelopes.Count());

            if (IsotopicEnvelopes.Count() > 0)
            {
                int topN = 0;
                foreach (var iso in IsotopicEnvelopes)
                {
                    if (topN >= Parameters.MS1IonSelecting.TopN)
                    {
                        break;
                    }
                    AddIsotopicEnvelope2UserDefinedScans(iso, ref topN);
                }
            }

        }

        private int DeconvolutePeakByIntensity(MzSpectrumBU spectrum, IEnumerable<int> indexByY, HashSet<double> seenPeaks, int topN)
        {
            int theTopN = 0;
            foreach (var peakIndex in indexByY)
            {
                if (theTopN >= topN)
                {
                    break;
                }

                if (seenPeaks.Contains(spectrum.XArray[peakIndex]))
                {
                    continue;
                }
                var iso = spectrum.DeconvolutePeak(peakIndex, Parameters.DeconvolutionParameter);
                if (iso == null)
                {
                    continue;
                }
                foreach (var seenPeak in iso.peaks.Select(b => b.mz))
                {
                    seenPeaks.Add(seenPeak);
                }

                AddIsotopicEnvelope2UserDefinedScans(iso, ref theTopN);
            }
            return theTopN;
        }

        //This deconvolution only deconvolute TopN peaks to generate MS2 scans. Deconvolute one peak, add one MS2 scan. 
        //There is no need to wait to deconvolute all peaks and then add all MS2 scans. 
        //In theory, this should reduce the time for deconvolute all peaks.
        private void DeconvoluteMS1ScanAddMS2Scan_TopN(IMsScan scan)
        {
            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute Start", DateTime.Now);

            var spectrum = new MzSpectrumBU(scan.Centroids.Select(p => p.Mz).ToArray(), scan.Centroids.Select(p => p.Intensity).ToArray(), false);
            HashSet<double> seenPeaks = new HashSet<double>();
            var indexByY = spectrum.ExtractIndicesByY();

            DeconvolutePeakByIntensity(spectrum, indexByY, seenPeaks, Parameters.MS1IonSelecting.TopN);
        }


        #endregion

        #region StaticBox WorkFlow

        //In StaticBox, the MS1 scan contains a lot of features. There is no need to extract features from BoxCar Scans.
        private void AddScanIntoQueue_StaticBox(IMsScan scan)
        {
            try
            {
                //Is MS1 Scan
                if (scan.HasCentroidInformation && IsMS1Scan(scan))
                {
                    bool isBoxCarScan = IsBoxCarScan(scan);

                    string scanNumber;
                    scan.CommonInformation.TryGetValue("ScanNumber", out scanNumber);
                    Console.WriteLine("In StaticBox method, MS1 Scan arrived. Is BoxCar Scan: {0}.", isBoxCarScan);

                    if (!isBoxCarScan && Parameters.MS2ScanSetting.DoMS2)
                    {
                        DeconvoluteMS1ScanAddMS2Scan_TopN(scan);
                    }

                    if (isBoxCarScan)
                    {
                        BoxCarScanNum--;
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
            catch (Exception e)
            {
                Console.WriteLine("AddScanIntoQueue_StaticBoxMS2FromFullScan Exception!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }

        #endregion

        #region DynamicBox WorkFlow

        private void DynamicDBCExclusionListDeque()
        {
            try
            {
                //TO DO: should I use spining or blocking
                while (dynamicExclude)
                {
                    Thread.Sleep(300);

                    DateTime dateTime = DateTime.Now;

                    Console.WriteLine("Check the dynamic DBC exclusionList.");

                    lock (lockerExclude)
                    {
                        bool toDeque = true;

                        while (toDeque && DynamicExclusionList.exclusionList.Count > 0)
                        {
                            if (dateTime.Subtract(DynamicExclusionList.exclusionList.Peek().Item3).TotalMilliseconds < Parameters.MS1IonSelecting.ExclusionDuration * 1000)
                            {
                                Console.WriteLine("The dynamic exclusionList is OK. Now: {0:HH:mm:ss,fff}, Peek: {1:HH:mm:ss,fff}.", dateTime, DynamicExclusionList.exclusionList.Peek().Item3);
                                toDeque = false;
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
            catch (Exception e)
            {
                Console.WriteLine("DynamicExclusionListDeqeue Exception!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }

        private void AddScanIntoQueue_DynamicBox(IMsScan scan)
        {
            try
            {
                //Is MS1 Scan
                if (scan.HasCentroidInformation)
                {
                    bool isBoxCarScan = IsBoxCarScan(scan);
                    //Console.WriteLine("MS1 Scan arrived. Is BoxCar Scan: {0}.", isBoxCarScan);

                    //lock (lockerScan)
                    if (!isBoxCarScan)
                    {
                        FullScan.PlaceFullScan(m_scans, Parameters);
                    }

                    var chargeEnvelops = DeconvoluateDynamicBoxRange(scan);

                    if (!isBoxCarScan && chargeEnvelops.Count > 0)
                    {
                        //lock (lockerScan)
                        {
                            Console.WriteLine("chargeEnvelops.Count: {0}", chargeEnvelops.Count);

                            if (!Parameters.BoxCarScanSetting.DynamicBoxCarOnlyForMS2)
                            {
                                if (chargeEnvelops.Count== 1)
                                {
                                    BoxCarScan.PlaceBoxCarScan(m_scans, Parameters, chargeEnvelops.First().distributions.Select(q => q.mz).ToList());
                                }
                                else if (chargeEnvelops.Count > 1 && chargeEnvelops[0].TotalIsoIntensity/chargeEnvelops[1].TotalIsoIntensity > 4)
                                {
                                    BoxCarScan.PlaceBoxCarScan(m_scans, Parameters, chargeEnvelops.First().distributions.Select(q => q.mz).ToList());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("AddScanIntoQueue_DynamicBox Exception!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }

        private List<ChargeEnvelop> DeconvoluateDynamicBoxRange(IMsScan scan)
        {
            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute Dynamic BoxCar Start", DateTime.Now);

            var spectrum = new MzSpectrumXY(scan.Centroids.Select(p => p.Mz).ToArray(), scan.Centroids.Select(p => p.Intensity).ToArray(), false);
            List<IsoEnvelop> isoEnvelops;
            var chargeEnvelops = ChargeDecon.ChargeDeconIsoForScan(spectrum, Parameters.DeconvolutionParameter, out isoEnvelops).Where(p=>p.distributions_withIso.Count >= 3);

            int placeScanCount = 0;

            foreach (var ce in chargeEnvelops)
            {
                if (placeScanCount >= Parameters.MS1IonSelecting.TopN)
                {
                    break;
                }

                var mzs = ce.distributions.Select(p => p.mz).OrderBy(p => p).ToArray();

                int matchedCount = DynamicDBCExclusionList.MatchExclusionList(mzs, 0.1);

                if (matchedCount == 0)
                {
                    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, ce.mzs_box);

                    placeScanCount++;

                    lock (lockerExclude)
                    {
                        DynamicDBCExclusionList.DBCExclusionList.Enqueue(new DynamicDBCValue(mzs, 0, DateTime.Now));
                    }
                }
            }


            foreach (var iso in isoEnvelops)
            {
                if (placeScanCount >= Parameters.MS1IonSelecting.TopN)
                {
                    break;
                }

                if (DynamicExclusionList.isNotInExclusionList(iso.ExperimentIsoEnvelop.First().Mz, Parameters.MS1IonSelecting.ExclusionTolerance))
                {
                    var dataTime = DateTime.Now;
                    lock (lockerExclude)
                    {
                        DynamicExclusionList.exclusionList.Enqueue(new Tuple<double, int, DateTime>(iso.ExperimentIsoEnvelop.First().Mz, iso.Charge, dataTime));
                        Console.WriteLine("ExclusionList Enqueue: {0}", DynamicExclusionList.exclusionList.Count);
                    }

                    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, iso.ExperimentIsoEnvelop.First().Mz);
                    placeScanCount++;
                }

            }


            return chargeEnvelops.ToList();
        }

        #endregion

        #region Glyco WorkFlow

        private void IsotopesForGlycoFeatureDeque()
        {
            try
            {
                //TO DO: should I use spining or blocking
                while (glycoInclude)
                {
                    Thread.Sleep(300);

                    DateTime dateTime = DateTime.Now;

                    lock (lockerGlyco)
                    {
                        bool toDeque = true;

                        while (toDeque && IsotopesForGlycoFeature.isotopeList.Count > 0)
                        {
                            if (dateTime.Subtract(IsotopesForGlycoFeature.isotopeList.Peek().Item2).TotalMilliseconds < Parameters.MS1IonSelecting.ExclusionDuration * 1000)
                            {
                                Console.WriteLine("The glyco isotopeList is OK. Now: {0:HH:mm:ss,fff}, Peek: {1:HH:mm:ss,fff}.", dateTime, IsotopesForGlycoFeature.isotopeList.Peek().Item2);
                                toDeque = false;
                            }
                            else
                            {

                                IsotopesForGlycoFeature.isotopeList.Dequeue();
                                Console.WriteLine("{0:HH:mm:ss,fff} Glyco isotopeList Dequeue: {1}", dateTime, IsotopesForGlycoFeature.isotopeList.Count);

                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Glyco isotopeList Exception!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }

        private void AddScanIntoQueue_Glyco(IMsScan scan)
        {
            try
            {
                //Is MS1 Scan
                if (scan.HasCentroidInformation && IsMS1Scan(scan))
                {
                    string scanNumber;
                    scan.CommonInformation.TryGetValue("ScanNumber", out scanNumber);
                    Console.WriteLine("MS1 Scan {0} arrived.", scanNumber);

                    DeconvoluteFindGlycoFeatures(scan);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("AddScanIntoQueue_BottomUp Exception!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }

        private List<NeuCodeIsotopicEnvelop> DeconvolutePeakConstructGlycoFamily(MzSpectrumBU spectrum)
        {
            //TO THINK: improve deconvolution is the key for everything!
            var IsotopicEnvelopes = spectrum.Deconvolute(spectrum.Range, Parameters.DeconvolutionParameter).OrderByDescending(p => p.totalIntensity).Take(100);

            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute Finished, get {1} isotopenvelops", DateTime.Now, IsotopicEnvelopes.Count());

            NeuCodeIsotopicEnvelop[] allIsotops;

            lock (lockerGlyco)
            {
                var dateTime = DateTime.Now;

                IsotopesForGlycoFeature.AddIsotopeIntoList(IsotopicEnvelopes, dateTime);

                allIsotops = IsotopesForGlycoFeature.isotopeList.Where(p => !p.Item1.AlreadyExist).Select(p => p.Item1).OrderBy(p => p.monoisotopicMass).ToArray();
            }

            var allFeatures = FeatureFinder.ExtractGlycoMS1features(allIsotops).OrderByDescending(P => P.MatchedFamilyCount);

            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute Finished, get {1} features.", DateTime.Now, allFeatures.Count());

            List<NeuCodeIsotopicEnvelop> glycoIsotope = new List<NeuCodeIsotopicEnvelop>();

            int addedGlycoFeature = 0;
            if (allFeatures.Count() > 0)
            {
                foreach (var iso in allFeatures)
                {
                    if (addedGlycoFeature >= Parameters.GlycoSetting.TopN)
                    {
                        break;
                    }

                    lock (lockerExclude)
                    {
                        if (DynamicExclusionList.isNotInExclusionList(iso.peaks.OrderBy(p => p.intensity).Last().mz, Parameters.MS1IonSelecting.ExclusionTolerance))
                        {
                            var dataTime = DateTime.Now;
                            DynamicExclusionList.exclusionList.Enqueue(new Tuple<double, int, DateTime>(iso.peaks.OrderBy(p => p.intensity).Last().mz, iso.charge, dataTime));
                            Console.WriteLine("ExclusionList Enqueue: {0}", DynamicExclusionList.exclusionList.Count);
                            iso.SelectedMz = iso.peaks.OrderBy(p => p.intensity).Last().mz;
                            glycoIsotope.Add(iso);
                            addedGlycoFeature++;
                        }
                    }
                }
            }

            if (addedGlycoFeature < 5)
            {
                foreach (var iso in IsotopicEnvelopes)
                {
                    if (addedGlycoFeature - 5 >= 0)
                    {
                        break;
                    }
                    lock (lockerExclude)
                    {
                        if (DynamicExclusionList.isNotInExclusionList(iso.peaks.OrderBy(p => p.intensity).Last().mz, Parameters.MS1IonSelecting.ExclusionTolerance))
                        {
                            var dataTime = DateTime.Now;
                            DynamicExclusionList.exclusionList.Enqueue(new Tuple<double, int, DateTime>(iso.peaks.OrderBy(p => p.intensity).Last().mz, iso.charge, dataTime));
                            Console.WriteLine("ExclusionList Enqueue: {0}", DynamicExclusionList.exclusionList.Count);
                            iso.SelectedMz = iso.peaks.OrderBy(p => p.intensity).Last().mz;
                            glycoIsotope.Add(iso);
                            addedGlycoFeature++;
                        }
                    }
                }
            }

            return glycoIsotope;
        }

        private void DeconvoluteFindGlycoFeatures(IMsScan scan)
        {
            var spectrum = new MzSpectrumBU(scan.Centroids.Select(p => p.Mz).ToArray(), scan.Centroids.Select(p => p.Intensity).ToArray(), true);

            //Deconvolute whole scan and contrust glycofamily
            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute Creat spectrum for glycofamily", DateTime.Now);
            var candidates = DeconvolutePeakConstructGlycoFamily(spectrum);
            Console.WriteLine("\n{0:HH:mm:ss,fff} Placed {1} Glycofamily scans.", DateTime.Now, candidates);

            int j = 0;
            while (j <= candidates.Count() - 6)
            {
                var theScan = new UserDefinedScan(UserDefinedScanType.DataDependentScan);

                //TO DO: get the best Mz.
                theScan.Mz = candidates[j].SelectedMz;
                lock (lockerScan)
                {
                    UserDefinedScans.Enqueue(theScan);
                    Console.WriteLine("dataDependentScans increased.");
                }
                j++;
            }
            lock (lockerScan)
            {
                UserDefinedScans.Enqueue(new UserDefinedScan(UserDefinedScanType.FullScan));
            }
            while (j <= candidates.Count() - 1)
            {
                var theScan = new UserDefinedScan(UserDefinedScanType.DataDependentScan);

                //TO DO: get the best Mz.
                theScan.Mz = candidates[j].SelectedMz;
                lock (lockerScan)
                {
                    UserDefinedScans.Enqueue(theScan);
                    Console.WriteLine("dataDependentScans increased.");
                }
                j++;
            }
        }

        #endregion

        #region NeuCode WorkFlow

        private void AddScanIntoQueue_Partner(IMsScan scan)
        {
            try
            {
                //Is MS1 Scan
                if (scan.HasCentroidInformation && IsMS1Scan(scan))
                {
                    Console.WriteLine("MS1 Scan arrived Partner.");

                    DeconvoluteFindPartners(scan);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("AddScanIntoQueue_Partner Exception!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }

        private int DeconvolutePeakByIntensity_NeuCode(MzSpectrumBU spectrum, IEnumerable<int> indexByY, HashSet<double> seenPeaks, int topN)
        {
            int theTopN = 0;
            foreach (var peakIndex in indexByY)
            {
                if (theTopN >= topN)
                {
                    break;
                }

                if (seenPeaks.Contains(spectrum.XArray[peakIndex]))
                {
                    continue;
                }
                var iso = spectrum.DeconvolutePeak_NeuCode(peakIndex, Parameters.DeconvolutionParameter);

                if (iso == null)
                {
                    continue;
                }

                foreach (var seenPeak in iso.peaks.Select(b => b.mz))
                {
                    seenPeaks.Add(seenPeak);
                }

                if (!iso.IsNeuCode)
                {
                    continue;
                }

                Console.WriteLine("NeuCode pair come.");

                iso.SelectedMz = spectrum.XArray[peakIndex];

                foreach (var seenPeak in iso.Partner.peaks.Select(b => b.mz))
                {
                    seenPeaks.Add(seenPeak);
                }

                lock (lockerExclude)
                {
                    if (DynamicExclusionList.isNotInExclusionList(iso.peaks.OrderBy(p => p.intensity).Last().mz, Parameters.MS1IonSelecting.ExclusionTolerance))
                    {
                        var dataTime = DateTime.Now;
                        DynamicExclusionList.exclusionList.Enqueue(new Tuple<double, int, DateTime>(iso.peaks.OrderBy(p => p.intensity).Last().mz, iso.charge, dataTime));
                        Console.WriteLine("ExclusionList Enqueue: {0}", DynamicExclusionList.exclusionList.Count);

                        var theScan = new UserDefinedScan(UserDefinedScanType.DataDependentScan);

                        //TO DO: get the best Mz.
                        theScan.Mz = iso.SelectedMz;
                        lock (lockerScan)
                        {
                            UserDefinedScans.Enqueue(theScan);
                            Console.WriteLine("dataDependentScans increased.");
                        }
                        theTopN++;
                    }
                }
            }
            return theTopN;
        }

        private List<NeuCodeIsotopicEnvelop> DeconvolutePeakByIntensity_NeuCode_NoPlacing(MzSpectrumBU spectrum, IEnumerable<int> indexByY, HashSet<double> seenPeaks, int topN, ref List<NeuCodeIsotopicEnvelop> normalIsotopicEnvelops)
        {
            List<NeuCodeIsotopicEnvelop> neuCodeIsotopicEnvelops = new List<NeuCodeIsotopicEnvelop>();
            int theTopN = 0;
            foreach (var peakIndex in indexByY)
            {
                if (theTopN >= topN)
                {
                    break;
                }

                if (seenPeaks.Contains(spectrum.XArray[peakIndex]))
                {
                    continue;
                }
                var iso = spectrum.DeconvolutePeak_NeuCode(peakIndex, Parameters.DeconvolutionParameter);

                if (iso == null)
                {
                    continue;
                }

                iso.SelectedMz = spectrum.XArray[peakIndex];

                foreach (var seenPeak in iso.peaks.Select(b => b.mz))
                {
                    seenPeaks.Add(seenPeak);
                }

                if (!iso.IsNeuCode)
                {
                    normalIsotopicEnvelops.Add(iso);
                    continue;
                }

                Console.WriteLine("NeuCode pair come.");

                foreach (var seenPeak in iso.Partner.peaks.Select(b => b.mz))
                {
                    seenPeaks.Add(seenPeak);
                }

                lock (lockerExclude)
                {
                    if (DynamicExclusionList.isNotInExclusionList(iso.peaks.OrderBy(p => p.intensity).Last().mz, Parameters.MS1IonSelecting.ExclusionTolerance))
                    {
                        var dataTime = DateTime.Now;
                        DynamicExclusionList.exclusionList.Enqueue(new Tuple<double, int, DateTime>(iso.peaks.OrderBy(p => p.intensity).Last().mz, iso.charge, dataTime));
                        Console.WriteLine("ExclusionList Enqueue: {0}", DynamicExclusionList.exclusionList.Count);
                        neuCodeIsotopicEnvelops.Add(iso);

                        theTopN++;
                    }
                }
            }
            return neuCodeIsotopicEnvelops;
        }

        private void DeconvoluteFindNeuCodeFeatures(IMsScan scan)
        {
            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute NeuCode Start", DateTime.Now);

            var spectrum = new MzSpectrumBU(scan.Centroids.Select(p => p.Mz).ToArray(), scan.Centroids.Select(p => p.Intensity).ToArray(), false);
            HashSet<double> seenPeaks = new HashSet<double>();
            var indexByY = spectrum.ExtractIndicesByY();

            List<NeuCodeIsotopicEnvelop> normalIsotopicEnvelops = new List<NeuCodeIsotopicEnvelop>();
            var candidates = DeconvolutePeakByIntensity_NeuCode_NoPlacing(spectrum, indexByY, seenPeaks, Parameters.MS1IonSelecting.TopN, ref normalIsotopicEnvelops).OrderBy(p => p.totalIntensity).ToList();

            if (Parameters.MS1IonSelecting.TopN - candidates.Count() > 0)
            {
                int normalAdd = Parameters.MS1IonSelecting.TopN - candidates.Count();

                foreach (var niso in normalIsotopicEnvelops)
                {
                    if (normalAdd > 0)
                    {
                        lock (lockerExclude)
                        {
                            if (DynamicExclusionList.isNotInExclusionList(niso.peaks.OrderBy(p => p.intensity).Last().mz, Parameters.MS1IonSelecting.ExclusionTolerance))
                            {
                                var dataTime = DateTime.Now;
                                DynamicExclusionList.exclusionList.Enqueue(new Tuple<double, int, DateTime>(niso.peaks.OrderBy(p => p.intensity).Last().mz, niso.charge, dataTime));
                                Console.WriteLine("ExclusionList Enqueue: {0}", DynamicExclusionList.exclusionList.Count);

                                candidates.Add(niso);
                                normalAdd--;
                            }
                        }
                    }
                }
            }

            int j = 0;
            while (j <= candidates.Count() - 6)
            {
                var theScan = new UserDefinedScan(UserDefinedScanType.DataDependentScan);

                //TO DO: get the best Mz.
                theScan.Mz = candidates[j].SelectedMz;
                lock (lockerScan)
                {
                    UserDefinedScans.Enqueue(theScan);
                    Console.WriteLine("dataDependentScans increased.");
                }
                j++;
            }
            lock (lockerScan)
            {
                UserDefinedScans.Enqueue(new UserDefinedScan(UserDefinedScanType.FullScan));
            }
            while (j <= candidates.Count() - 1)
            {
                var theScan = new UserDefinedScan(UserDefinedScanType.DataDependentScan);

                //TO DO: get the best Mz.
                theScan.Mz = candidates[j].SelectedMz;
                lock (lockerScan)
                {
                    UserDefinedScans.Enqueue(theScan);
                    Console.WriteLine("dataDependentScans increased.");
                }
                j++;
            }
        }

        private void DeconvoluteFindPartners(IMsScan scan)
        {
            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute Partner Start", DateTime.Now);

            var mzSpectrumXY = new MzSpectrumXY(scan.Centroids.Select(p => p.Mz).ToArray(), scan.Centroids.Select(p => p.Intensity).ToArray(), false);

            var isoEnvelops = IsoDecon.MsDeconv_Deconvolute(mzSpectrumXY, mzSpectrumXY.Range, Parameters.DeconvolutionParameter).Where(p => p.IsLight && p.Charge >= 5 && p.MsDeconvScore >= 50 && p.MsDeconvSignificance > 0.1).OrderByDescending(p => p.MsDeconvScore).ToList();

            var placeScanCount = 0;

            foreach (var iso in isoEnvelops)
            {
                if (placeScanCount >= Parameters.MS1IonSelecting.TopN)
                {
                    break;
                }

                if (DynamicExclusionList.isNotInExclusionList(iso.ExperimentIsoEnvelop.First().Mz, Parameters.MS1IonSelecting.ExclusionTolerance))
                {
                    var dataTime = DateTime.Now;
                    lock (lockerExclude)
                    {
                        DynamicExclusionList.exclusionList.Enqueue(new Tuple<double, int, DateTime>(iso.ExperimentIsoEnvelop.First().Mz, iso.Charge, dataTime));
                        Console.WriteLine("ExclusionList Enqueue: {0}", DynamicExclusionList.exclusionList.Count);
                    }

                    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, iso.ExperimentIsoEnvelop.First().Mz);
                    placeScanCount++;
                }
            }

            FullScan.PlaceFullScan(m_scans, Parameters);

        }

        #endregion

        #region UserDefinedWorkFlow

        private void AddScanIntoQueue_UserDefined(IMsScan scan)
        {
            try
            {
                //Is MS1 Scan
                if (scan.HasCentroidInformation)
                {
                    PlaceUserDefined(scan);               
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("AddScanIntoQueue_UserDefined Exception!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }

        private void PlaceUserDefined(IMsScan scan)
        {
            Console.WriteLine("\n{0:HH:mm:ss,fff} UserDefined Start", DateTime.Now);

            var monomass = 16950.88339;

            Dictionary<int, double> mz_z = new Dictionary<int, double>();

            for (int i = 15; i <= 27; i++)
            {
                mz_z.Add(i, monomass.ToMz(i));

            }

            var mzs = mz_z.Values;

            //foreach (var mz in mzs)
            //{
            //    Parameters.MS2ScanSetting.NCE = 10;
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //    Parameters.MS2ScanSetting.NCE = 15;
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //    Parameters.MS2ScanSetting.NCE = 20;
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //    Parameters.MS2ScanSetting.NCE = 25;
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //    Parameters.MS2ScanSetting.NCE = 30;
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //    Parameters.MS2ScanSetting.NCE = 35;
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //    Parameters.MS2ScanSetting.NCE = 40;
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //    Parameters.MS2ScanSetting.NCE = 45;
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //    Parameters.MS2ScanSetting.NCE = 50;
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //}
            //Parameters.MS2ScanSetting.NCE = 25;

            //foreach (var mz in mzs)
            //{
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //    Parameters.MS2ScanSetting.NCE_factors = "[0.9, 1, 1.1]";
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //    Parameters.MS2ScanSetting.NCE_factors = "[0.8, 1, 1.2]";
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //    Parameters.MS2ScanSetting.NCE_factors = "[0.6, 1, 1.4]";
            //    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
            //    Parameters.MS2ScanSetting.NCE_factors = "null";
            //}

            int j = 15;
            while (j <= 27)
            {
                DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz_z[j]);
                Parameters.MS2ScanSetting.NCE_factors = "[0.9, 1, 1.1]";
                DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz_z[j]);
                Parameters.MS2ScanSetting.NCE_factors = "[0.8, 1, 1.2]";
                DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz_z[j]);
                Parameters.MS2ScanSetting.NCE_factors = "[0.6, 1, 1.4]";
                DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz_z[j]);
                Parameters.MS2ScanSetting.NCE_factors = "null";
                j += 1;
            }

            List<double> comb = new List<double>();
            comb.Add(mz_z[20]);
            comb.Add(mz_z[21]);
            comb.Add(mz_z[22]);
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "[0.9, 1, 1.1]";
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "[0.8, 1, 1.2]";
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "[0.6, 1, 1.4]";
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "null";

            comb.Clear();
            comb.Add(mz_z[19]);
            comb.Add(mz_z[21]);
            comb.Add(mz_z[23]);
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "[0.9, 1, 1.1]";
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "[0.8, 1, 1.2]";
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "[0.6, 1, 1.4]";
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "null";

            comb.Clear();
            comb.Add(mz_z[17]);
            comb.Add(mz_z[21]);
            comb.Add(mz_z[25]);
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "[0.9, 1, 1.1]";
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "[0.8, 1, 1.2]";
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "[0.6, 1, 1.4]";
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "null";

            comb.Clear();
            comb.Add(mz_z[17]);
            comb.Add(mz_z[19]);
            comb.Add(mz_z[21]);
            comb.Add(mz_z[23]);
            comb.Add(mz_z[25]);
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "[0.9, 1, 1.1]";
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "[0.8, 1, 1.2]";
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "[0.6, 1, 1.4]";
            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, comb);
            Parameters.MS2ScanSetting.NCE_factors = "null";

            FullScan.PlaceFullScan(m_scans, Parameters);
        }

        #endregion

    }
}
