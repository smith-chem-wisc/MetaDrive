using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.Interfaces.InstrumentAccess_V1.Control.Scans;
using Chemistry;


namespace MetaLive
{
    class DataDependentScan
    {
        public static void PlaceMS2Scan(IScans m_scans, Parameters parameters, Tuple<double, int> mass_charge)
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }

            double Range = parameters.MS1IonSelecting.IsolationWindow;
            string xl = (mass_charge.Item1.ToMz(mass_charge.Item2) - Range).ToString("0.000");
            string xh = (mass_charge.Item1.ToMz(mass_charge.Item2) + Range).ToString("0.000");            
            ICustomScan scan = m_scans.CreateCustomScan();

            scan.Values["FirstMass"] = parameters.MS2ScanSetting.MS2MzRangeLowBound.ToString();
            scan.Values["LastMass"] = (mass_charge.Item1 * 1.2).ToString("0.00");
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
            scan.Values["Microscans"] = "1";
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

            Console.WriteLine("{0:HH:mm:ss,fff} placing data dependent ms2 scan {1}", DateTime.Now, xl);
            m_scans.SetCustomScan(scan);
        }

        public static void PlaceMS2Scan(IScans m_scans, Parameters parameters, List<Tuple<double, int>> Mass_Charges)
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }

            double Range = parameters.MS1IonSelecting.IsolationWindow;
            //string xl = (mz - Range).ToString("0.00");
            //string xh = (mz + Range).ToString("0.00");
            ICustomScan scan = m_scans.CreateCustomScan();
            scan.Values["Resolution"] = parameters.MS2ScanSetting.MS2Resolution.ToString();
            scan.Values["NCE"] = parameters.MS2ScanSetting.NCE.ToString();
            //scan.Values["IsolationRangeLow"] = xl;
            //scan.Values["IsolationRangeHigh"] = xh;
            scan.Values["MsxInjectRanges"] = BuildDataDependentBoxString(parameters, Mass_Charges);
            scan.Values["Resolution"] = parameters.MS2ScanSetting.MS2Resolution.ToString();
            scan.Values["FirstMass"] = parameters.MS2ScanSetting.MS2MzRangeLowBound.ToString();
            scan.Values["LastMass"] = parameters.MS2ScanSetting.MS2MzRangeHighBound.ToString();
            scan.Values["AGC_Target"] = parameters.MS2ScanSetting.MS2AgcTarget.ToString();

            //Console.WriteLine("{0:HH:mm:ss,fff} placing data dependent ms2 scan {1}", DateTime.Now, xl);
            m_scans.SetCustomScan(scan);
        }

        public static string BuildDataDependentBoxString(Parameters parameters, List<Tuple<double, int>> Mass_Charges)
        {
            double Range = parameters.MS1IonSelecting.IsolationWindow;
            string dynamicBoxRanges = "[";
            List<double> mzs = new List<double>();

            for (int i = 0; i < Mass_Charges.Count; i++)
            {
                mzs.Add(Mass_Charges[i].Item1.ToMz(Mass_Charges[i].Item2));
            }

            for (int i = 0; i < mzs.Count; i++)
            {
                var mz = mzs[i];
                if (i == mzs.Count -1)
                {
                    dynamicBoxRanges += "(";
                    dynamicBoxRanges += (mz - Range).ToString("0.000");
                    dynamicBoxRanges += ",";
                    dynamicBoxRanges += (mz + Range).ToString("0.000");
                    dynamicBoxRanges += ")";
                }
                else
                {
                    dynamicBoxRanges += "(";
                    dynamicBoxRanges += (mz - Range).ToString("0.000");
                    dynamicBoxRanges += ",";
                    dynamicBoxRanges += (mz + Range).ToString("0.000");
                    dynamicBoxRanges += "),";
                }
            }
            dynamicBoxRanges += "]";

            return dynamicBoxRanges;
        }

    }
}
