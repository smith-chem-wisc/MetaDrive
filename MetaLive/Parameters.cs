using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaLive
{
    public class Parameters
    {
        public GeneralSetting GeneralSetting { get; set; }
        public BoxCarScanSetting BoxCarScanSetting { get; set; }
        public FullScanSetting FullScanSetting { get; set; }
        public MS1IonSelecting MS1IonSelecting { get; set; }
        public MS2ScanSetting MS2ScanSetting { get; set; }
    }

    public class GeneralSetting
    {
        public bool TestMod { get; set; }
        public int TotalTimeInMinute { get; set; }
        public bool IsBottomUp { get; set; }
        public int Polarity { get; set; }
        public int SourceCID { get; set; }
        public int AGC_Mode { get; set; }
    }

    public class BoxCarScanSetting
    {
        public bool BoxCarStatic { get; set; }
        public bool BoxCarDynamic { get; set; }
        public int BoxCarScans { get; set; }
        public int BoxCarBoxes { get; set; }
        public int BoxCarOverlap { get; set; }
        public int BoxCarMaxInjectTimeInMillisecond { get; set; }
        public int BoxCarResolution { get; set; }
        public int BoxCarAgcTarget { get; set; }
        public int BoxCarNormCharge { get; set; }
        public int BoxCarMzRangeLowBound { get; set; }
        public int BoxCarMzRangeHighBound { get; set; }
        public int BoxCarMicroScans { get; set; }

        //TO DO: The BoxCar Ranges should be optimized based on real data
        public string[] BoxCarMsxInjectRanges
        {
            get
            {
                var msxInjectRanges = new string[BoxCarScans];
                double x = ((double)BoxCarMzRangeHighBound - (double)BoxCarMzRangeLowBound) / BoxCarBoxes;
                double y = x / BoxCarScans;
                for (int i = 0; i < BoxCarScans; i++)
                {
                    msxInjectRanges[i] = "[";
                    for (int j = 0; j < BoxCarBoxes; j++)
                    {
                        msxInjectRanges[i] += "(";
                        var lbox = BoxCarMzRangeLowBound + x * j + y * i - (double)BoxCarOverlap / 2;
                        if (j == 0)
                        {
                            lbox += (double)BoxCarOverlap / 2;
                        }
                        msxInjectRanges[i] += lbox.ToString("0.0");

                        msxInjectRanges[i] += ",";

                        var rbox = BoxCarMzRangeLowBound + x * j + y * i + y + (double)BoxCarOverlap / 2;
                        if (j == 0)
                        {
                            rbox += (double)BoxCarOverlap / 2;
                        }
                        msxInjectRanges[i] += rbox.ToString("0.0");

                        msxInjectRanges[i] += ")";
                        if (j != BoxCarBoxes - 1)
                        {
                            msxInjectRanges[i] += ",";
                        }
                    }
                    msxInjectRanges[i] += "]";
                }

                return msxInjectRanges;
            }
        }

        public string BoxCarMsxInjectTargets
        {
            get
            {
                var msxInjectTarget = "[";
                for (int i = 0; i < BoxCarBoxes; i++)
                {
                    msxInjectTarget += BoxCarAgcTarget / BoxCarBoxes;
                    if (i != BoxCarBoxes - 1)
                    {
                        msxInjectTarget += ",";
                    }
                }
                msxInjectTarget += "]";
                return msxInjectTarget;
            }
        }

        public string BoxCarMsxInjectMaxITs
        {
            get
            {
                var msxInjectMaxITs= "[";
                for (int i = 0; i < BoxCarBoxes; i++)
                {
                    msxInjectMaxITs += BoxCarMaxInjectTimeInMillisecond / BoxCarBoxes;
                    if (i != BoxCarBoxes - 1)
                    {
                        msxInjectMaxITs += ",";
                    }
                }
                msxInjectMaxITs += "]";
                return msxInjectMaxITs;
            }
        }
    }

    public class FullScanSetting
    {
        public int MaxInjectTimeInMillisecond { get; set; }
        public int Resolution { get; set; }
        public int AgcTarget { get; set; }
        public int MzRangeLowBound { get; set; }
        public int MzRangeHighBound { get; set; }
        public int Microscans { get; set; }
    }

    public class MS1IonSelecting
    {
        public int TopN { get; set; }
        public bool DynamicExclusion { get; set; }
        public int ExclusionDuration { get; set; }
        public double IsolationWindow { get; set; }
        public int NormCharge { get; set; }
        public int MinCharge { get; set; }
        public int MaxCharge { get; set; }
        public int IntensityThreshold { get; set; }
    }

    public class MS2ScanSetting
    {
        public bool DoMS2 { get; set; }
        public int MS2MaxInjectTimeInMillisecond { get; set; }
        public int MS2Resolution { get; set; }
        public int NCE { get; set; }
        public string NCE_factors { get; set; }
        public int MS2AgcTarget { get; set; }
        public int MS2MzRangeLowBound { get; set; }
        public int MS2MzRangeHighBound { get; set; }
        public int MS2MicroScans { get; set; }
    }
}