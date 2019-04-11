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
        public static void PlaceMS1Scan(IScans m_scans)
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }
            ICustomScan scan = m_scans.CreateCustomScan();
            scan.Values["Resolution"] = "120000.0";
            scan.Values["FirstMass"] = "375";
            scan.Values["LastMass"] = "1500";
            scan.Values["MaxIT"] = "100";
            scan.Values["NCE_NormCharge"] = "2";
            scan.Values["AGC_Target"] = "1000000";   

            Console.WriteLine("{0:HH:mm:ss,fff} placing MS1 scan", DateTime.Now);
            m_scans.SetCustomScan(scan);
        }

        public static void PlaceMxmScan(IScans m_scans)
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }
            ICustomScan scan = m_scans.CreateCustomScan();
            scan.Values["Resolution"] = "120000.0";
            scan.Values["FirstMass"] = "375";
            scan.Values["LastMass"] = "1500";
            scan.Values["MaxIT"] = "100";
            scan.Values["NCE_NormCharge"] = "2";
            scan.Values["AGC_Target"] = "1000000";

            scan.Values["MsxInjectRanges"] = "[(375,425),(475,525),(575,625),(675,725),(775,825)]";
            scan.Values["MsxInjectMaxITs"] = "[50,50,50,50,50]";

            Console.WriteLine("{0:HH:mm:ss,fff} placing MS1 scan", DateTime.Now);
            m_scans.SetCustomScan(scan);
        }
    }
}
 