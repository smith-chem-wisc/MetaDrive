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
            scan.Values["Resolution"] = parameters.FullScanSetting.Resolution.ToString();
            scan.Values["FirstMass"] = parameters.FullScanSetting.MzRangeLowBound.ToString();
            scan.Values["LastMass"] = parameters.FullScanSetting.MzRangeHighBound.ToString() ;
            scan.Values["MaxIT"] = parameters.FullScanSetting.MaxInjectTimeInMillisecond.ToString();
            scan.Values["NCE_NormCharge"] = parameters.MS1IonSelecting.NormCharge.ToString();
            scan.Values["AGC_Target"] = parameters.FullScanSetting.AgcTarget.ToString();   

            Console.WriteLine("{0:HH:mm:ss,fff} placing MS1 scan", DateTime.Now);
            m_scans.SetCustomScan(scan);
        }
    }
}
 