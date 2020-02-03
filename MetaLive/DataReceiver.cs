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
        bool placeUserDefinedScan = true;
        int BoxCarScanNum = 0;
        bool TimeIsOver = false;

        static object lockerExclude = new object();
        static object lockerScan = new object();

        #endregion

        internal DataReceiver(Parameters parameters)
        {
            Parameters = parameters;
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
                case MethodTypes.DynamicBoxCar_BU:
                    AddScanIntoQueueAction = AddScanIntoQueue_BynamicBoxCar_BU;
                    Console.WriteLine("AddScanIntoQueueAction = DynamicBoxCar_BU.");
                    break;
                case MethodTypes.DynamicBoxCar_TD:
                    DynamicDBCExclusionList = new DynamicDBCExclusionList();
                    AddScanIntoQueueAction = AddScanIntoQueue_DynamicBoxCar_TD;
                    Console.WriteLine("AddScanIntoQueueAction = DynamicBoxCar_TD.");
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
        public Action<IMsScan> AddScanIntoQueueAction { get; set; }

        DynamicExclusionList DynamicExclusionList { get; set; }
        DynamicDBCExclusionList DynamicDBCExclusionList { get; set; }

        List<Tuple<double, double, double>>[] Boxes { get; set;  }


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

            if (Parameters.GeneralSetting.MethodType == MethodTypes.DynamicBoxCar_TD)
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
            placeUserDefinedScan = false;

            Console.WriteLine("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream closed (end of method)");            
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

                if (value == Parameters.BoxCarScanSetting.BoxCarMzRangeLowBound.ToString() || valueHigh == Parameters.BoxCarScanSetting.BoxCarMzRangeHighBound.ToString())
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
                        FullScan.PlaceFullScan(m_scans, Parameters);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("AddScanIntoQueue_BottomUp Exception!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }

        //It is possible to deconvolution only deconvolute TopN peaks to generate MS2 scans. Deconvolute one peak, add one MS2 scan. 
        //There is no need to wait to deconvolute all peaks and then add all MS2 scans. 
        private void DeconvoluteMS1ScanAddMS2Scan_TopN(IMsScan scan)
        {
            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute Start", DateTime.Now);

            List<IsoEnvelop> isoEnvelops = Deconvolute_BU(scan);

            PlaceBU_MS2Scan(scan, isoEnvelops);
        }

        private List<IsoEnvelop> Deconvolute_BU(IMsScan scan)
        {
            var spectrum = new MzSpectrumXY(scan.Centroids.Select(p => p.Mz).ToArray(), scan.Centroids.Select(p => p.Intensity).ToArray(), false);
            List<IsoEnvelop> isoEnvelops = IsoDecon.MsDeconv_Deconvolute(spectrum, spectrum.Range, Parameters.DeconvolutionParameter);
            return isoEnvelops;
        }

        private void PlaceBU_MS2Scan(IMsScan scan, List<IsoEnvelop> isoEnvelops)
        {
            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute Dynamic BoxCar Start", DateTime.Now);

            int placeScanCount = 0;

            foreach (var iso in isoEnvelops.OrderByDescending(p=>p.TotalIntensity))
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
                        Console.WriteLine("2 ExclusionList Enqueue: {0}", iso.ExperimentIsoEnvelop.First().Mz);
                    }

                    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, iso.ExperimentIsoEnvelop.First().Mz);
                    placeScanCount++;
                }

            }
        }


        #endregion

        #region StaticBox WorkFlow

        //In StaticBox, the MS1 scan contains a lot of features. There is no need to extract features from BoxCar Scans for placing MS2 scans.
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
                            FullScan.PlaceFullScan(m_scans, Parameters);
                            BoxCarScan.PlaceBoxCarScan(m_scans, Parameters);
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

        #region DynamicBox_BU WorkFlow

        private void AddScanIntoQueue_BynamicBoxCar_BU(IMsScan scan)
        {
            try
            {
                //Is MS1 Scan
                if (scan.HasCentroidInformation && IsMS1Scan(scan))
                {
                    bool isBoxCarScan = IsBoxCarScan(scan);

                    //string scanNumber;
                    //scan.CommonInformation.TryGetValue("ScanNumber", out scanNumber);
                    Console.WriteLine("In DynamicBoxCar_BU method, MS1 Scan arrived. Is BoxCar Scan: {0}.", isBoxCarScan);

                    if (!isBoxCarScan && Parameters.MS2ScanSetting.DoMS2)
                    {
                        List<IsoEnvelop> isoEnvelops = Deconvolute_BU(scan);

                        Boxes = GenerateBoxes_BU(isoEnvelops, Parameters);

                        PlaceBU_MS2Scan(scan, isoEnvelops);

                    }

                    if (isBoxCarScan)
                    {
                        BoxCarScanNum--;
                    }

                    if (BoxCarScanNum == 0)
                    {
                        lock (lockerScan)
                        {
                            FullScan.PlaceFullScan(m_scans, Parameters);

                            //The nearby Full mass scans in the current DDA method are very similar, so it is possible to use the previous full scan to generate the current boxes.
                            if (Boxes.Length > 0)
                            {
                                BoxCarScan.PlaceBoxCarScan_BU(m_scans, Parameters, Boxes);
                            }
                        }
                        BoxCarScanNum = Parameters.BoxCarScanSetting.BoxCarScans;
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("AddScanIntoQueue_DynamicBoxCar_Bu MS2FromFullScan Exception!");
                Console.WriteLine(e.ToString() + " " + e.Source);
            }
        }

        //return Tuple<double, double, double> for each box start m/z, end m/z, m/z length
        public static List<Tuple<double, double, double>>[] GenerateBoxes_BU(List<IsoEnvelop> isoEnvelops, Parameters parameters)
        {
            var thred = isoEnvelops.OrderByDescending(p => p.IntensityRatio).First().IntensityRatio / 20;
            var mzs = isoEnvelops.Where(p => p.IntensityRatio > thred).Select(p => p.ExperimentIsoEnvelop.First().Mz).OrderBy(p => p).ToList();

            Tuple<double, double, double>[] ranges = new Tuple<double, double, double>[mzs.Count];

            for (int i = 1; i < mzs.Count; i++)
            {
                ranges[i - 1] = new Tuple<double, double, double>(mzs[i - 1], mzs[i], mzs[i] - mzs[i - 1]);
            }

            ranges[mzs.Count - 1] = new Tuple<double, double, double>(mzs.Last(), parameters.BoxCarScanSetting.BoxCarMzRangeHighBound, parameters.BoxCarScanSetting.BoxCarMzRangeHighBound - mzs.Last());

            //return ranges.OrderByDescending(p => p.Item3).Where(p => p.Item3 > 15).Take(12).OrderBy(p => p.Item1).ToArray();

            List<Tuple<double, double, double>>[] boxes = new List<Tuple<double, double, double>>[parameters.BoxCarScanSetting.BoxCarBoxes];

            for (int i = 0; i < parameters.BoxCarScanSetting.BoxCarBoxes; i++)
            {
                boxes[i] = new List<Tuple<double, double, double>>();
            }

            int j = 0;
            foreach (var r in ranges)
            {
                //Make sure the range is longer than 10. 
                if (r.Item3 > 10)
                {
                    if (j <= parameters.BoxCarScanSetting.BoxCarBoxes-1)
                    {
                        boxes[j].Add(r);
                        j++;
                    }
                    else
                    {
                        j = 0;
                    }
                }
            }

            return boxes;
        }

        #endregion

        #region DynamicBox_TD WorkFlow

        private void DynamicDBCExclusionListDeque()
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

        private void AddScanIntoQueue_DynamicBoxCar_TD(IMsScan scan)
        {
            try
            {
                //Is MS1 Scan
                if (scan.HasCentroidInformation)
                {           
                    List<ChargeEnvelop> chargeEnvelops;

                    var isoEnvelops = Deconvolute_TD(scan, out chargeEnvelops);

                    Console.WriteLine("chargeEnvelops.Count: {0}", isoEnvelops.Count);

                    if (Parameters.BoxCarScanSetting.DoDbcForMS1)
                    {
                        if (IsBoxCarScan(scan))
                        {
                            PlaceDynamicBoxCarMS2Scan(scan, chargeEnvelops, isoEnvelops);
                            FullScan.PlaceFullScan(m_scans, Parameters);
                        }
                        else
                        {
                            if (chargeEnvelops.Count >= 1 || isoEnvelops.Count >= 5)
                            {
                                lock (lockerExclude)
                                {
                                    var thred = isoEnvelops.OrderByDescending(p => p.IntensityRatio).First().IntensityRatio / 20;
                                    var isos = isoEnvelops.Where(p => p.IntensityRatio > thred);
                                    foreach (var x in isos)
                                    {
                                        DynamicExclusionList.exclusionList.Enqueue(new Tuple<double, int, DateTime>(x.ExperimentIsoEnvelop.First().Mz, x.Charge, DateTime.Now));
                                    }

                                }

                                var boxes = GenerateBoxes_TD(isoEnvelops);

                                BoxCarScan.PlaceBoxCarScan(m_scans, Parameters, boxes);
                            }
                            else
                            {
                                FullScan.PlaceFullScan(m_scans, Parameters);
                            }
                        }
                    }
                    else
                    {
                        if (Parameters.BoxCarScanSetting.PrecursorSkipScan)
                        {
                            FullScan.PlaceFullScan(m_scans, Parameters);

                            PlaceDynamicBoxCarMS2Scan(scan, chargeEnvelops, isoEnvelops);
                        }
                        else
                        {
                            PlaceDynamicBoxCarMS2Scan(scan, chargeEnvelops, isoEnvelops);

                            FullScan.PlaceFullScan(m_scans, Parameters);
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

        private List<IsoEnvelop> Deconvolute_TD(IMsScan scan, out List<ChargeEnvelop> chargeEnvelops)
        {
            var spectrum = new MzSpectrumXY(scan.Centroids.Select(p => p.Mz).ToArray(), scan.Centroids.Select(p => p.Intensity).ToArray(), false);
            List<IsoEnvelop> isoEnvelops;
            chargeEnvelops = ChargeDecon.ChargeDeconIsoForScan(spectrum, Parameters.DeconvolutionParameter, out isoEnvelops).OrderByDescending(p=>p.ChargeDeconScore).Where(p => p.distributions_withIso.Count >= 2).ToList();
            return isoEnvelops;
        }

        private void PlaceDynamicBoxCarMS2Scan(IMsScan scan, List<ChargeEnvelop> chargeEnvelops, List<IsoEnvelop> isoEnvelops)
        {
            Console.WriteLine("\n{0:HH:mm:ss,fff} Deconvolute Dynamic BoxCar Start", DateTime.Now);

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
                    Console.WriteLine("DynamicDBCExclusionList didn't match.");
                    if (Parameters.MS2ScanSetting.DoDbcMS2)
                    {
                        DataDependentScan.PlaceMS2Scan(m_scans, Parameters, ce.mzs_box);
                    }
                    else
                    {
                        var mz = ce.distributions_withIso.OrderByDescending(p => p.intensity).First().isoEnvelop.ExperimentIsoEnvelop.First().Mz;
                        if (DynamicExclusionList.isNotInExclusionList(mz, Parameters.MS1IonSelecting.ExclusionTolerance))
                        {
                            DataDependentScan.PlaceMS2Scan(m_scans, Parameters, mz);
                        }
                    }
                    
                    placeScanCount++;

                    lock (lockerExclude)
                    {
                        DynamicDBCExclusionList.DBCExclusionList.Enqueue(new DynamicDBCValue(mzs, 0, DateTime.Now));
                        foreach (var x in ce.distributions_withIso)
                        {
                            var mz = x.isoEnvelop.ExperimentIsoEnvelop.First().Mz;
                            if (DynamicExclusionList.isNotInExclusionList(mz, Parameters.MS1IonSelecting.ExclusionTolerance))
                            {
                                DynamicExclusionList.exclusionList.Enqueue(new Tuple<double, int, DateTime>(mz, x.charge, DateTime.Now));
                                Console.WriteLine("1 ExclusionList Enqueue: {0}", mz);
                            }
                        }
                        
                    }
                }
                else
                {
                    Console.WriteLine("DynamicDBCExclusionList did match.");
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
                        Console.WriteLine("2 ExclusionList Enqueue: {0}", iso.ExperimentIsoEnvelop.First().Mz);
                    }

                    DataDependentScan.PlaceMS2Scan(m_scans, Parameters, iso.ExperimentIsoEnvelop.First().Mz);
                    placeScanCount++;
                }

            }
        }

        //return Tuple<double, double, double> for each box start m/z, end m/z, m/z length
        public static Tuple<double, double, double>[] GenerateBoxes_TD(List<IsoEnvelop> isoEnvelops)
        {
            var thred = isoEnvelops.OrderByDescending(p => p.IntensityRatio).First().IntensityRatio / 20;
            var mzs = isoEnvelops.Where(p => p.IntensityRatio > thred).Select(p => p.ExperimentIsoEnvelop.First().Mz).OrderBy(p => p).ToList();

            Tuple<double, double, double>[] ranges = new Tuple<double, double, double>[mzs.Count];

            for (int i = 1; i < mzs.Count; i++)
            {
                ranges[i - 1] = new Tuple<double, double, double>(mzs[i - 1], mzs[i], mzs[i] - mzs[i - 1]);
            }
            ranges[mzs.Count - 1] = new Tuple<double, double, double>(mzs.Last(), 2000, 2000 - mzs.Last());

            return ranges.OrderByDescending(p => p.Item3).Where(p => p.Item3 > 15).Take(12).OrderBy(p => p.Item1).ToArray();

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
