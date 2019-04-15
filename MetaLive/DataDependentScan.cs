using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.Interfaces.InstrumentAccess_V1.Control.Scans;


namespace MetaLive
{
    class DataDependentScan
    {
        public static void PlaceMS2Scan(IScans m_scans, Parameters parameters, double mz)
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }

            double Range = parameters.MS1IonSelecting.IsolationWindow;
            string xl = (mz - Range).ToString("0.00");
            string xh = (mz + Range).ToString("0.00");            
            ICustomScan scan = m_scans.CreateCustomScan();
            scan.Values["Resolution"] = parameters.MS2ScanSetting.MS2Resolution.ToString();
            scan.Values["NCE"] = parameters.MS2ScanSetting.NCE.ToString();
            scan.Values["IsolationRangeLow"] = xl;
            scan.Values["IsolationRangeHigh"] = xh;
            scan.Values["Resolution"] = parameters.MS2ScanSetting.MS2Resolution.ToString();
            scan.Values["FirstMass"] = parameters.MS2ScanSetting.MS2MzRangeLowBound.ToString();
            scan.Values["LastMass"] = parameters.MS2ScanSetting.MS2MzRangeHighBound.ToString();
            scan.Values["AGC_Target"] = parameters.MS2ScanSetting.MS2AgcTarget.ToString();

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


    }
}
