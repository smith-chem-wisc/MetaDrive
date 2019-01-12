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
        IScans m_scans = null;
        

        public DataDependentScan(DateTime startTime, int timeInSecond, IScans M_scans, double mz, double range)
        {
            FinishTime = startTime.AddSeconds(timeInSecond);
            m_scans = M_scans;
            MZ = mz;
            Range = range;
        }

        public DateTime FinishTime { get; }
        public double MZ { get; }
        public double Range { get; }

        public void PlaceMS2Scan()
        {
            if (m_scans.PossibleParameters.Length == 0)
            {
                return;
            }
            string xl = (MZ - Range).ToString("0.00");
            string xh = (MZ + Range).ToString("0.00");            
            ICustomScan scan = m_scans.CreateCustomScan();
            scan.Values["Resolution"] = "15000.0";
            scan.Values["NCE"] = "30";
            scan.Values["IsolationRangeLow"] = xl;
            scan.Values["IsolationRangeHigh"] = xh;
            scan.Values["Resolution"] = "15000.0";
            scan.Values["FirstMass"] = "100";
            scan.Values["LastMass"] = "2000";
            scan.Values["AGC_Target"] = "100000";

            //Console.WriteLine("++++++++++++++++++");
            //Console.WriteLine("Target Isolation Mass: {0}, {1}", xl, xh);
            ////foreach (var item in m_scans.PossibleParameters)
            ////{
            ////    Console.WriteLine(item.Name + "----" + item.DefaultValue + "----" + item.Help + "----" + item.Selection);
            ////}

            //foreach (var v in scan.Values)
            //{
            //    Console.WriteLine(v);
            //}
     
            Console.WriteLine("{0:HH:mm:ss,fff} placing data dependent ms2 scan {1}", DateTime.Now, xl);
            m_scans.SetCustomScan(scan);
        }


    }
}
