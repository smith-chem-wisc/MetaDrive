using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.Interfaces.InstrumentAccess_V1.Control.Scans;


namespace MetaLive
{
    class FullMS1Scan
    {
        public static void PlaceFullScan(IScans m_scans, Parameters parameters)
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }
            ICustomScan scan = m_scans.CreateCustomScan();
            scan.Values["Resolution"] = parameters.FullScanSetting.Resolution.ToString();
            scan.Values["FirstMass"] = parameters.FullScanSetting.MzRangeLowBound.ToString();
            scan.Values["LastMass"] = parameters.FullScanSetting.MzRangeHighBound.ToString() ;
            scan.Values["MaxIT"] = parameters.FullScanSetting.MaxInjectTimeInMillisecond.ToString();
            scan.Values["NCE_NormCharge"] = parameters.MS1IonSelecting.NormCharge.ToString();
            scan.Values["AGC_Target"] = parameters.FullScanSetting.AgcTarget.ToString();   

            Console.WriteLine("{0:HH:mm:ss,fff} placing MS1 scan", DateTime.Now);
            m_scans.SetCustomScan(scan);
        }

        public static void PlaceBoxCarScan(IScans m_scans, Parameters parameters)
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }
            ICustomScan scan = m_scans.CreateCustomScan();
            scan.Values["Resolution"] = parameters.BoxCarScanSetting.BoxCarResolution.ToString();
            scan.Values["FirstMass"] = parameters.BoxCarScanSetting.BoxCarMzRangeLowBound.ToString();
            scan.Values["LastMass"] = parameters.BoxCarScanSetting.BoxCarMzRangeHighBound.ToString();
            scan.Values["MaxIT"] = parameters.BoxCarScanSetting.BoxCarMaxInjectTimeInMillisecond.ToString();
            scan.Values["NCE_NormCharge"] = parameters.BoxCarScanSetting.BoxCarNormCharge.ToString();
            scan.Values["AGC_Target"] = parameters.BoxCarScanSetting.BoxCarAgcTarget.ToString();
           
            scan.Values["MsxInjectRanges"] = "[(375,425),(475,525),(575,625),(675,725),(775,825)]";
            scan.Values["MsxInjectMaxITs"] = "[50,50,50,50,50]";

            Console.WriteLine("{0:HH:mm:ss,fff} placing MS1 scan", DateTime.Now);
            m_scans.SetCustomScan(scan);
        }
    }
}
 