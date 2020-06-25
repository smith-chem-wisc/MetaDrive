using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.Interfaces.InstrumentAccess_V1.Control.Scans;
using Chemistry;
using MassSpectrometry;

namespace MetaDrive
{
    public class BoxCarScan
    {
        public static string[] StaticBoxCarScanRanges { get; set; }
        public static string StaticBoxCarScanTargets { get; set; }
        public static string StaticBoxCarScanMaxIts { get; set; }

        #region Static BoxCar 

        public static void PlaceStaticBoxCarScan(IScans m_scans, Parameters parameters)
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }

            ICustomScan scan = m_scans.CreateCustomScan();
            scan.Values["FirstMass"] = parameters.BoxCarScanSetting.BoxCarMzRangeLowBound.ToString();
            scan.Values["LastMass"] = parameters.BoxCarScanSetting.BoxCarMzRangeHighBound.ToString();
            scan.Values["IsolationRangeLow"] = (parameters.BoxCarScanSetting.BoxCarMzRangeLowBound).ToString();
            scan.Values["IsolationRangeHigh"] = (parameters.BoxCarScanSetting.BoxCarMzRangeHighBound).ToString();

            scan.Values["MaxIT"] = parameters.BoxCarScanSetting.BoxCarMaxInjectTimeInMillisecond.ToString();
            scan.Values["Resolution"] = parameters.BoxCarScanSetting.BoxCarResolution.ToString();
            scan.Values["Polarity"] = parameters.GeneralSetting.Polarity.ToString();
            scan.Values["NCE"] = "0.0";
            scan.Values["NCE_NormCharge"] = parameters.BoxCarScanSetting.BoxCarNormCharge.ToString();
            scan.Values["NCE_SteppedEnergy"] = "0";
            scan.Values["NCE_Factors"] = "[]";

            scan.Values["SourceCID"] = parameters.GeneralSetting.SourceCID.ToString("0.00");
            scan.Values["Microscans"] = parameters.BoxCarScanSetting.BoxCarMicroScans.ToString();
            scan.Values["AGC_Target"] = parameters.BoxCarScanSetting.BoxCarAgcTarget.ToString();
            scan.Values["AGC_Mode"] = parameters.GeneralSetting.AGC_Mode.ToString();

            scan.Values["MsxInjectTargets"] = StaticBoxCarScanTargets;
            scan.Values["MsxInjectMaxITs"] = StaticBoxCarScanMaxIts;
            scan.Values["MsxInjectNCEs"] = "[]";
            scan.Values["MsxInjectDirectCEs"] = "[]";
            for (int i = 0; i < parameters.BoxCarScanSetting.NumberOfBoxCarScans; i++)
            {
                scan.Values["MsxInjectRanges"] = StaticBoxCarScanRanges[i];

                Console.WriteLine("{0:HH:mm:ss,fff} placing BoxCar MS1 scan", DateTime.Now);
                m_scans.SetCustomScan(scan);
            }
        }

        //gamma distribution is used to mimic the distribution of intensities.
        public static List<double> GammaDistributionSeparation(Parameters parameters)
        {
            var alpha = 2;
            var beta = 3;

            var start = 0.1;
            var end = 0.9;

            List<double> sep = new List<double>();
            int totalSep = parameters.BoxCarScanSetting.NumberOfBoxCarBoxes * parameters.BoxCarScanSetting.NumberOfBoxCarScans + 1;
            for (int i = 0; i < totalSep; i++)
            {
                var p = start + (end - start) / (totalSep - 1) * i;
                var sepx = MathNet.Numerics.Distributions.Gamma.InvCDF(alpha, beta, p);
                sep.Add(sepx);
            }

            var firstMass = parameters.BoxCarScanSetting.BoxCarMzRangeLowBound;
            var lastMass = parameters.BoxCarScanSetting.BoxCarMzRangeHighBound;
            var scale = lastMass - firstMass;

            List<double> scale_sep = new List<double>();
            for (int i = 0; i < sep.Count; i++)
            {
                var x = scale * (sep[i] - sep.First()) / (sep.Last() - sep.First()) + firstMass;
                scale_sep.Add(x);
            }

            return scale_sep;
        }

        public static Tuple<double, double, double>[][] GenerateStaticBoxes(List<double> mzs, int NumOfBoxScan)
        {
            Tuple<double, double, double>[] ranges = new Tuple<double, double, double>[mzs.Count - 1];

            for (int i = 1; i < mzs.Count; i++)
            {
                ranges[i-1] = new Tuple<double, double, double>(mzs[i-1], mzs[i], mzs[i] - mzs[i-1]);
            }
            
            Tuple<double, double, double>[][] rangeArray = new Tuple<double, double, double>[NumOfBoxScan][];

            for (int i = 0; i < NumOfBoxScan; i++)
            {
                rangeArray[i] = new Tuple<double, double, double>[(mzs.Count - 1)/NumOfBoxScan];
                for (int j = 0; j < (mzs.Count - 1) / NumOfBoxScan; j++)
                {
                    rangeArray[i][j] = ranges[j * NumOfBoxScan + i];
                }
            }

            return rangeArray;
        }

        public static string GenerateStaticBoxString(Parameters parameters, Tuple<double, double, double>[] dynamicBox, out string dynamicBoxTargets, out string dynamicBoxMaxITs)
        {
            string dynamicBoxRanges = "[";

            var overlap = parameters.BoxCarScanSetting.BoxCarOverlap;

            foreach (var box in dynamicBox)
            {
                dynamicBoxRanges += "(";
                dynamicBoxRanges += (box.Item1 - overlap).ToString("0.00");
                dynamicBoxRanges += ",";
                dynamicBoxRanges += (box.Item2 + overlap).ToString("0.00");
                dynamicBoxRanges += "),";
            }

            dynamicBoxRanges = dynamicBoxRanges.Remove(dynamicBoxRanges.Count() - 1);
            dynamicBoxRanges += "]";


            //Boxtargets and BoxMaxITs
            dynamicBoxTargets = "[";
            for (int i = 0; i < dynamicBox.Length; i++)
            {
                dynamicBoxTargets += parameters.BoxCarScanSetting.BoxCarAgcTarget / dynamicBox.Length;
                if (i != dynamicBox.Length - 1)
                {
                    dynamicBoxTargets += ",";
                }
            }
            dynamicBoxTargets += "]";

            dynamicBoxMaxITs = "[";
            for (int i = 0; i < dynamicBox.Length; i++)
            {
                dynamicBoxMaxITs += parameters.BoxCarScanSetting.BoxCarMaxInjectTimeInMillisecond / dynamicBox.Length;
                if (i != dynamicBox.Length - 1)
                {
                    dynamicBoxMaxITs += ",";
                }
            }
            dynamicBoxMaxITs += "]";

            return dynamicBoxRanges;
        }

        public static void BuildStaticBoxString(Parameters parameters)
        {
            string dynamicTargets;
            string dynamicMaxIts;

            var mzs = GammaDistributionSeparation(parameters);
            //To correct the range shift by GenerateStaticBoxString
            mzs[0] = mzs.First() + parameters.BoxCarScanSetting.BoxCarOverlap;
            mzs[mzs.Count-1] = mzs.Last() - parameters.BoxCarScanSetting.BoxCarOverlap;

            var staticBoxes = GenerateStaticBoxes(mzs, parameters.BoxCarScanSetting.NumberOfBoxCarScans);

            StaticBoxCarScanRanges = new string[parameters.BoxCarScanSetting.NumberOfBoxCarScans];
            for (int i = 0; i < parameters.BoxCarScanSetting.NumberOfBoxCarScans; i++)
            {
                StaticBoxCarScanRanges[i] = GenerateStaticBoxString(parameters, staticBoxes[i], out dynamicTargets, out dynamicMaxIts);
                StaticBoxCarScanTargets = dynamicTargets;
                StaticBoxCarScanMaxIts = dynamicMaxIts;
            }
        }

        #endregion

        #region DynamicBoxCar Bottom-Up

        //return Tuple<double, double, double> for each box start m/z, end m/z, m/z length
        public static void GenerateDynamicBoxes_BU(List<IsoEnvelop> isoEnvelops, Parameters parameters, List<List<Tuple<double, double, double>>> boxes)
        {
            //var thred = isoEnvelops.OrderByDescending(p => p.IntensityRatio).First().IntensityRatio / 20;
            var mzs = isoEnvelops.Where(p => p.Mz > parameters.BoxCarScanSetting.BoxCarMzRangeLowBound
                && p.Mz < parameters.BoxCarScanSetting.BoxCarMzRangeHighBound).OrderBy(p=>p.TotalIntensity).Take(30).Select(p => p.ExperimentIsoEnvelop.First().Mz).OrderBy(p => p).ToArray();

            Tuple<double, double, double>[] ranges = new Tuple<double, double, double>[mzs.Length + 1];

            ranges[0] = new Tuple<double, double, double>(parameters.BoxCarScanSetting.BoxCarMzRangeLowBound, mzs[0], mzs[0] - parameters.BoxCarScanSetting.BoxCarMzRangeLowBound);
            for (int i = 1; i < mzs.Length; i++)
            {
                ranges[i] = new Tuple<double, double, double>(mzs[i - 1], mzs[i], mzs[i] - mzs[i - 1]);
            }

            ranges[mzs.Length] = new Tuple<double, double, double>(mzs.Last(), parameters.BoxCarScanSetting.BoxCarMzRangeHighBound, parameters.BoxCarScanSetting.BoxCarMzRangeHighBound - mzs.Last());

            for (int i = 0; i < parameters.BoxCarScanSetting.NumberOfBoxCarScans; i++)
            {
                var tuples = new List<Tuple<double, double, double>>();
                boxes.Add(tuples);
            }


            int j = 0;
            foreach (var r in ranges.Where(p=>p.Item3 > 3)) //If the range is too small, say 5, there is no need to place a box between. 
            {
                if (j <= parameters.BoxCarScanSetting.NumberOfBoxCarScans - 1)
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

        public static void PlaceDynamicBoxCarScan_BU(IScans m_scans, Parameters parameters, List<List<Tuple<double, double, double>>> dynamicBoxs)
        {
            foreach (var dynamicBox in dynamicBoxs)
            {
                if (dynamicBox.Count > 0)
                {
                    PlaceDynamicBoxCarScan(m_scans, parameters, dynamicBox.ToArray());
                }
            }
        }

        #endregion

        #region DynamicBoxCar TopDown

        //return Tuple<double, double, double> for each box start m/z, end m/z, m/z length
        public static Tuple<double, double, double>[] GenerateDynamicBoxes_TD(List<IsoEnvelop> isoEnvelops)
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
        //parameter Tuple<double, double, double> for each box start m/z, end m/z, m/z length
        public static void PlaceDynamicBoxCarScan(IScans m_scans, Parameters parameters, Tuple<double, double, double>[] dynamicBox)
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }

            ICustomScan scan = m_scans.CreateCustomScan();
            scan.Values["FirstMass"] = parameters.BoxCarScanSetting.BoxCarMzRangeLowBound.ToString();
            scan.Values["LastMass"] = parameters.BoxCarScanSetting.BoxCarMzRangeHighBound.ToString();
            scan.Values["IsolationRangeLow"] = (parameters.BoxCarScanSetting.BoxCarMzRangeLowBound - 200).ToString();
            scan.Values["IsolationRangeHigh"] = (parameters.BoxCarScanSetting.BoxCarMzRangeHighBound + 200).ToString();

            scan.Values["MaxIT"] = parameters.BoxCarScanSetting.BoxCarMaxInjectTimeInMillisecond.ToString();
            scan.Values["Resolution"] = parameters.BoxCarScanSetting.BoxCarResolution.ToString();
            scan.Values["Polarity"] = parameters.GeneralSetting.Polarity.ToString();
            scan.Values["NCE"] = "0.0";
            scan.Values["NCE_NormCharge"] = parameters.BoxCarScanSetting.BoxCarNormCharge.ToString();
            scan.Values["NCE_SteppedEnergy"] = "0";
            scan.Values["NCE_Factors"] = "[]";

            scan.Values["SourceCID"] = parameters.GeneralSetting.SourceCID.ToString("0.00");
            scan.Values["Microscans"] = parameters.BoxCarScanSetting.BoxCarMicroScans.ToString();
            scan.Values["AGC_Target"] = parameters.BoxCarScanSetting.BoxCarAgcTarget.ToString();
            scan.Values["AGC_Mode"] = parameters.GeneralSetting.AGC_Mode.ToString();

            

            string dynamicTargets;
            string dynamicMaxIts;
            var dynamicBoxString = BuildDynamicBoxString(parameters, dynamicBox, out dynamicTargets, out dynamicMaxIts);
            scan.Values["MsxInjectRanges"] = dynamicBoxString;
            scan.Values["MsxInjectTargets"] = dynamicTargets;
            scan.Values["MsxInjectMaxITs"] = dynamicMaxIts;

            scan.Values["MsxInjectNCEs"] = "[]";
            scan.Values["MsxInjectDirectCEs"] = "[]";

            Console.WriteLine("{0:HH:mm:ss,fff} placing Dynamic BoxCar MS1 scan {1}", DateTime.Now, dynamicBoxString);
            m_scans.SetCustomScan(scan);

        }

        public static string BuildDynamicBoxString(Parameters parameters, Tuple<double, double, double>[] dynamicBox, out string dynamicBoxTargets, out string dynamicBoxMaxITs)
        {
            string dynamicBoxRanges = "[";

            foreach (var box in dynamicBox)
            {
                dynamicBoxRanges += "(";
                dynamicBoxRanges += (box.Item1+2.5).ToString("0.00");
                dynamicBoxRanges += ",";
                dynamicBoxRanges += (box.Item2-2.5).ToString("0.00");
                dynamicBoxRanges += "),";
            }

            dynamicBoxRanges = dynamicBoxRanges.Remove(dynamicBoxRanges.Count()-1);
            dynamicBoxRanges += "]";


            //Boxtargets and BoxMaxITs
            dynamicBoxTargets = "[";
            for (int i = 0; i < dynamicBox.Length; i++)
            {
                dynamicBoxTargets += parameters.BoxCarScanSetting.BoxCarAgcTarget / dynamicBox.Length;
                if (i != dynamicBox.Length-1)
                {
                    dynamicBoxTargets += ",";
                }
            }
            dynamicBoxTargets += "]";

            dynamicBoxMaxITs = "[";
            for (int i = 0; i < dynamicBox.Length; i++)
            {
                dynamicBoxMaxITs += parameters.BoxCarScanSetting.BoxCarMaxInjectTimeInMillisecond / dynamicBox.Length;
                if (i != dynamicBox.Length - 1)
                {
                    dynamicBoxMaxITs += ",";
                }
            }
            dynamicBoxMaxITs += "]";

            return dynamicBoxRanges;
        }

        public static string BuildDynamicBoxInclusionString(Parameters parameters, List<double> dynamicBoxBeforeOrder, out string dynamicBoxTargets, out string dynamicBoxMaxITs)
        {
            //The dynamicBox list should be ordered?
            var dynamicBox = dynamicBoxBeforeOrder.Where(p => p <= parameters.BoxCarScanSetting.BoxCarMzRangeHighBound && p >= parameters.BoxCarScanSetting.BoxCarMzRangeLowBound).ToList();

            string dynamicBoxRanges = "[";

            dynamicBoxRanges += "(";
            
            var firstMass = (dynamicBox[0] - parameters.BoxCarScanSetting.DynamicBlockSize < parameters.BoxCarScanSetting.BoxCarMzRangeLowBound) ? 
                parameters.BoxCarScanSetting.BoxCarMzRangeLowBound : dynamicBox[0] - parameters.BoxCarScanSetting.DynamicBlockSize;
            dynamicBoxRanges += firstMass.ToString("0.000");
            dynamicBoxRanges += ",";
            dynamicBoxRanges += (dynamicBox[0] + parameters.BoxCarScanSetting.DynamicBlockSize).ToString("0.000");
            dynamicBoxRanges += "),";

            for (int i = 1; i < dynamicBox.Count-1; i++)
            {
                dynamicBoxRanges += "(";
                dynamicBoxRanges += (dynamicBox[i] - parameters.BoxCarScanSetting.DynamicBlockSize).ToString("0.000");
                dynamicBoxRanges += ",";
                dynamicBoxRanges += (dynamicBox[i] + parameters.BoxCarScanSetting.DynamicBlockSize).ToString("0.000");
                dynamicBoxRanges += "),";
            }

            dynamicBoxRanges += "(";
            dynamicBoxRanges += (dynamicBox.Last() - parameters.BoxCarScanSetting.DynamicBlockSize).ToString("0.000");
            dynamicBoxRanges += ",";

            var lastMass = (dynamicBox.Last() + parameters.BoxCarScanSetting.DynamicBlockSize > parameters.BoxCarScanSetting.BoxCarMzRangeHighBound) ? 
                parameters.BoxCarScanSetting.BoxCarMzRangeHighBound : dynamicBox.Last() + parameters.BoxCarScanSetting.DynamicBlockSize;
            dynamicBoxRanges += lastMass.ToString("0.000");
            dynamicBoxRanges += ")";

            dynamicBoxRanges += "]";


            //Boxtargets and BoxMaxITs
            dynamicBoxTargets = "[";
            for (int i = 0; i < dynamicBox.Count; i++)
            {
                dynamicBoxTargets += parameters.BoxCarScanSetting.BoxCarAgcTarget / dynamicBox.Count;
                if (i != dynamicBox.Count - 1)
                {
                    dynamicBoxTargets += ",";
                }
            }
            dynamicBoxTargets += "]";

            dynamicBoxMaxITs = "[";
            for (int i = 0; i < dynamicBox.Count; i++)
            {
                dynamicBoxMaxITs += parameters.BoxCarScanSetting.BoxCarMaxInjectTimeInMillisecond / dynamicBox.Count;
                if (i != dynamicBox.Count - 1)
                {
                    dynamicBoxMaxITs += ",";
                }
            }
            dynamicBoxMaxITs += "]";

            return dynamicBoxRanges;
        }

    }
}
