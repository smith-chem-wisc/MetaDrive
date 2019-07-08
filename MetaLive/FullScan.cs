using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.Interfaces.InstrumentAccess_V1.Control.Scans;


namespace MetaLive
{
    class FullScan
    {
        public static void PlaceFullScan(IScans m_scans, Parameters parameters)
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }
            ICustomScan scan = m_scans.CreateCustomScan();
            scan.Values["FirstMass"] = parameters.FullScanSetting.MzRangeLowBound.ToString();
            scan.Values["LastMass"] = parameters.FullScanSetting.MzRangeHighBound.ToString();
            scan.Values["IsolationRangeLow"] = parameters.FullScanSetting.MzRangeLowBound.ToString();
            scan.Values["IsolationRangeHigh"] = parameters.FullScanSetting.MzRangeHighBound.ToString();
            scan.Values["MaxIT"] = parameters.FullScanSetting.MaxInjectTimeInMillisecond.ToString();
            scan.Values["Resolution"] = parameters.FullScanSetting.Resolution.ToString();
            scan.Values["Polarity"] = parameters.GeneralSetting.Polarity.ToString();
            scan.Values["NCE"] = "0.0";
            scan.Values["NCE_NormCharge"] = parameters.MS1IonSelecting.NormCharge.ToString();
            scan.Values["NCE_SteppedEnergy"] = "0";
            scan.Values["NCE_Factors"] = "[]";
            scan.Values["SourceCID"] = parameters.GeneralSetting.SourceCID.ToString("0.00");
            scan.Values["Microscans"] = parameters.FullScanSetting.Microscans.ToString();
            scan.Values["AGC_Target"] = parameters.FullScanSetting.AgcTarget.ToString();
            scan.Values["AGC_Mode"] = parameters.GeneralSetting.AGC_Mode.ToString();

            scan.Values["MsxInjectRanges"] = "[]";
            scan.Values["MsxInjectTargets"] = "[]";
            scan.Values["MsxInjectMaxITs"] = "[]";
            scan.Values["MsxInjectNCEs"] = "[]";
            scan.Values["MsxInjectDirectCEs"] = "[]";

            Console.WriteLine("{0:HH:mm:ss,fff} placing Full MS1 scan", DateTime.Now);
            m_scans.SetCustomScan(scan);
        }
    }
}
 