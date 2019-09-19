using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.Interfaces.InstrumentAccess_V1.Control.Scans;
using Chemistry;


namespace MetaLive
{
    public class DataDependentScan
    {
        public static void PlaceMS2Scan(IScans m_scans, Parameters parameters, double mz)
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }

            double Range = parameters.MS1IonSelecting.IsolationWindow;
            string xl = (mz - Range).ToString("0.000");
            string xh = (mz + Range).ToString("0.000");
            if (mz-Range < 50.0)
            {
                Console.WriteLine("placing data dependent ms2 scan wrong, the Ms2MzRangeLowBound should larger than 50!!!");

                return;
            }
            ICustomScan scan = m_scans.CreateCustomScan();

            scan.Values["FirstMass"] = parameters.MS2ScanSetting.MS2MzRangeLowBound.ToString();
            scan.Values["LastMass"] = parameters.MS2ScanSetting.MS2MzRangeHighBound.ToString(); //TO THINK: Dynamic range as MqLive?
            scan.Values["IsolationRangeLow"] = xl;
            scan.Values["IsolationRangeHigh"] = xh;
            scan.Values["Resolution"] = parameters.MS2ScanSetting.MS2Resolution.ToString();

            scan.Values["MaxIT"] = parameters.MS2ScanSetting.MS2MaxInjectTimeInMillisecond.ToString();
            scan.Values["Resolution"] = parameters.MS2ScanSetting.MS2Resolution.ToString();
            scan.Values["Polarity"] = parameters.GeneralSetting.Polarity.ToString();
            scan.Values["NCE"] = parameters.MS2ScanSetting.NCE.ToString();
            scan.Values["NCE_NormCharge"] = parameters.MS1IonSelecting.NormCharge.ToString();
            scan.Values["NCE_SteppedEnergy"] = "0";
            if (parameters.MS2ScanSetting.NCE_factors != "null")
            {
                scan.Values["NCE_Factors"] = parameters.MS2ScanSetting.NCE_factors;
            }
            scan.Values["SourceCID"] = parameters.GeneralSetting.SourceCID.ToString("0.00");
            scan.Values["Microscans"] = parameters.MS2ScanSetting.MS2MicroScans.ToString();
            scan.Values["AGC_Target"] = parameters.MS2ScanSetting.MS2AgcTarget.ToString();
            scan.Values["AGC_Mode"] = parameters.GeneralSetting.AGC_Mode.ToString();


            scan.Values["MsxInjectRanges"] = "[]";
            scan.Values["MsxInjectTargets"] = "[]";
            scan.Values["MsxInjectMaxITs"] = "[]";
            scan.Values["MsxInjectNCEs"] = "[]";
            scan.Values["MsxInjectDirectCEs"] = "[]";

            //Console.WriteLine("++++++++++++++++++");
            //Console.WriteLine("Target Isolation Mass: {0}, {1}", xl, xh);
            //foreach (var v in scan.Values)
            //{
            //    Console.WriteLine(v);
            //}

            ////This is the code where we find all the scan settings.
            ////foreach (var item in m_scans.PossibleParameters)
            ////{
            ////    Console.WriteLine(item.Name + "----" + item.DefaultValue + "----" + item.Help + "----" + item.Selection);
            ////}

            Console.WriteLine("{0:HH:mm:ss,fff} placing data dependent ms2 scan {1}", DateTime.Now, mz);
            m_scans.SetCustomScan(scan);
        }

        public static void PlaceMS2Scan(IScans m_scans, Parameters parameters, List<double> dynamicBox)
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }

            double Range = parameters.MS1IonSelecting.IsolationWindow;
            ICustomScan scan = m_scans.CreateCustomScan();

            scan.Values["FirstMass"] = parameters.MS2ScanSetting.MS2MzRangeLowBound.ToString();
            scan.Values["LastMass"] = parameters.MS2ScanSetting.MS2MzRangeHighBound.ToString(); //TO THINK: Dynamic range as MqLive?
            scan.Values["IsolationRangeLow"] = parameters.MS2ScanSetting.MS2MzRangeLowBound.ToString();
            scan.Values["IsolationRangeHigh"] = parameters.MS2ScanSetting.MS2MzRangeLowBound.ToString();
            scan.Values["Resolution"] = parameters.MS2ScanSetting.MS2Resolution.ToString();

            scan.Values["MaxIT"] = parameters.MS2ScanSetting.MS2MaxInjectTimeInMillisecond.ToString();
            scan.Values["Resolution"] = parameters.MS2ScanSetting.MS2Resolution.ToString();
            scan.Values["Polarity"] = parameters.GeneralSetting.Polarity.ToString();
            scan.Values["NCE"] = parameters.MS2ScanSetting.NCE.ToString();
            scan.Values["NCE_NormCharge"] = parameters.MS1IonSelecting.NormCharge.ToString();
            scan.Values["NCE_SteppedEnergy"] = "0";
            if (parameters.MS2ScanSetting.NCE_factors != "null")
            {
                scan.Values["NCE_Factors"] = parameters.MS2ScanSetting.NCE_factors;
            }
            scan.Values["SourceCID"] = parameters.GeneralSetting.SourceCID.ToString("0.00");
            scan.Values["Microscans"] = parameters.MS2ScanSetting.MS2MicroScans.ToString();
            scan.Values["AGC_Target"] = parameters.MS2ScanSetting.MS2AgcTarget.ToString();
            scan.Values["AGC_Mode"] = parameters.GeneralSetting.AGC_Mode.ToString();


            string dynamicTargets;
            string dynamicMaxIts;
            var dynamicBoxString = BuildDynamicBoxInclusionString(parameters, dynamicBox, out dynamicTargets, out dynamicMaxIts);
            scan.Values["MsxInjectRanges"] = dynamicBoxString;
            scan.Values["MsxInjectTargets"] = dynamicTargets;
            scan.Values["MsxInjectMaxITs"] = dynamicMaxIts;

            scan.Values["MsxInjectNCEs"] = "[]";
            scan.Values["MsxInjectDirectCEs"] = "[]";

            Console.WriteLine("{0:HH:mm:ss,fff} placing data dependent ms2 scan {1}", DateTime.Now, dynamicBoxString);
            m_scans.SetCustomScan(scan);
        }

        public static string BuildDynamicBoxInclusionString(Parameters parameters, List<double> dynamicBoxBeforeOrder, out string dynamicBoxTargets, out string dynamicBoxMaxITs)
        {
            //The dynamicBox list should be ordered?
            var dynamicBox = dynamicBoxBeforeOrder.Where(p => p <= parameters.MS2ScanSetting.MS2MzRangeHighBound && p >= parameters.MS2ScanSetting.MS2MzRangeLowBound).ToList();

            string dynamicBoxRanges = "[";

            dynamicBoxRanges += "(";

            var firstMass = (dynamicBox[0] - parameters.MS1IonSelecting.IsolationWindow < parameters.MS2ScanSetting.MS2MzRangeLowBound) ?
                parameters.MS2ScanSetting.MS2MzRangeLowBound : dynamicBox[0] - parameters.MS1IonSelecting.IsolationWindow;
            dynamicBoxRanges += firstMass.ToString("0.000");
            dynamicBoxRanges += ",";
            dynamicBoxRanges += (dynamicBox[0] + parameters.MS1IonSelecting.IsolationWindow).ToString("0.000");
            dynamicBoxRanges += "),";

            for (int i = 1; i < dynamicBox.Count - 1; i++)
            {
                dynamicBoxRanges += "(";
                dynamicBoxRanges += (dynamicBox[i] - parameters.MS1IonSelecting.IsolationWindow).ToString("0.000");
                dynamicBoxRanges += ",";
                dynamicBoxRanges += (dynamicBox[i] + parameters.MS1IonSelecting.IsolationWindow).ToString("0.000");
                dynamicBoxRanges += "),";
            }

            dynamicBoxRanges += "(";
            dynamicBoxRanges += (dynamicBox.Last() - parameters.MS1IonSelecting.IsolationWindow).ToString("0.000");
            dynamicBoxRanges += ",";

            var lastMass = (dynamicBox.Last() + parameters.MS1IonSelecting.IsolationWindow > parameters.MS2ScanSetting.MS2MzRangeHighBound) ?
                parameters.MS2ScanSetting.MS2MzRangeHighBound : dynamicBox.Last() + parameters.MS1IonSelecting.IsolationWindow;
            dynamicBoxRanges += lastMass.ToString("0.000");
            dynamicBoxRanges += ")";

            dynamicBoxRanges += "]";


            //Boxtargets and BoxMaxITs
            dynamicBoxTargets = "[";
            for (int i = 0; i < dynamicBox.Count; i++)
            {
                dynamicBoxTargets += parameters.MS2ScanSetting.MS2AgcTarget / dynamicBox.Count;
                if (i != dynamicBox.Count - 1)
                {
                    dynamicBoxTargets += ",";
                }
            }
            dynamicBoxTargets += "]";

            dynamicBoxMaxITs = "[";
            for (int i = 0; i < dynamicBox.Count; i++)
            {
                dynamicBoxMaxITs += parameters.MS2ScanSetting.MS2MaxInjectTimeInMillisecond / dynamicBox.Count;
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
