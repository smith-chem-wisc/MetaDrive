using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.Interfaces.InstrumentAccess_V1.Control.Scans;
using Chemistry;

namespace MetaLive
{
    class BoxCarScan
    {
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
            for (int i = 0; i < parameters.BoxCarScanSetting.BoxCarScans; i++)
            {
                scan.Values["MsxInjectRanges"] = parameters.BoxCarScanSetting.BoxCarMsxInjectRanges[i];

                Console.WriteLine("{0:HH:mm:ss,fff} placing BoxCar MS1 scan", DateTime.Now);
                m_scans.SetCustomScan(scan);
            }           
        }

        public static void PlaceBoxCarScan(IScans m_scans, Parameters parameters, List<double> dynamicBox)
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

            
            var dynamicBoxString = BuildDynamicBoxString(parameters, dynamicBox);
            scan.Values["MsxInjectRanges"] = dynamicBoxString;

            Console.WriteLine("{0:HH:mm:ss,fff} placing Dynamic BoxCar MS1 scan {1}", DateTime.Now, dynamicBoxString);
            m_scans.SetCustomScan(scan);

        }

        public static string BuildDynamicBoxString(Parameters parameters, List<double> dynamicBox)
        {
            string dynamicBoxRanges = "[";
            List<double> mzs = new List<double>();
            foreach (var range in dynamicBox)
            {
                for (int i = 1; i < 60; i++)
                {
                    mzs.Add(range.ToMz(i));
                }
            }

            var mzsFiltered = mzs.Where(p => p > parameters.BoxCarScanSetting.BoxCarMzRangeLowBound && p < parameters.BoxCarScanSetting.BoxCarMzRangeHighBound).OrderBy(p => p).ToList();

            dynamicBoxRanges += "(";
            dynamicBoxRanges += parameters.BoxCarScanSetting.BoxCarMzRangeLowBound.ToString("0.000");
            dynamicBoxRanges += ",";
            if (mzsFiltered[0] - 5 < parameters.BoxCarScanSetting.BoxCarMzRangeLowBound)
            {
                dynamicBoxRanges += (mzsFiltered[0]).ToString("0.000");
            }
            else
            {
                dynamicBoxRanges += (mzsFiltered[0] - 5).ToString("0.000");
            }
            dynamicBoxRanges += "),";

            for (int i = 1; i < mzsFiltered.Count; i++)
            {
                var mz = mzsFiltered[i];
                var mz_front = mzsFiltered[i - 1];
                dynamicBoxRanges += "(";
                dynamicBoxRanges += (mz_front + 5).ToString("0.000");
                dynamicBoxRanges += ",";
                dynamicBoxRanges += (mz - 5).ToString("0.000");
                dynamicBoxRanges += "),";
            }
            dynamicBoxRanges += "(";
            dynamicBoxRanges += (mzsFiltered.Last() + 5).ToString("0.000");
            dynamicBoxRanges += ",";
            dynamicBoxRanges += parameters.BoxCarScanSetting.BoxCarMzRangeHighBound.ToString("0.000");
            dynamicBoxRanges += ")";

            dynamicBoxRanges += "]";
            return dynamicBoxRanges;
        }

    }
}
