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
    }

    public class BoxCarScanSetting
    {
        public int BoxCarScans { get; set; }
        public int BoxCarBoxes { get; set; }
        public int BoxCarOverlap { get; set; }
        public int BoxCarMaxInjectTimeInMillisecond { get; set; }
        public int BoxCarResolution { get; set; }
        public int BoxCarAgcTarget { get; set; }
        public int BoxCarNormCharge { get; set; }
        public int BoxCarMzRangeLowBound { get; set; }
        public int BoxCarMzRangeHighBound { get; set; }
    }

    public class FullScanSetting
    {
        public int MaxInjectTimeInMillisecond { get; set; }
        public int Resolution { get; set; }
        public int AgcTarget { get; set; }
        public int MzRangeLowBound { get; set; }
        public int MzRangeHighBound { get; set; }
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
        public int MS2MaxInjectTimeInMillisecond { get; set; }
        public int MS2Resolution { get; set; }
        public int NCE { get; set; }
        public int MS2AgcTarget { get; set; }
        public int MS2MzRangeLowBound { get; set; }
        public int MS2MzRangeHighBound { get; set; }
    }
}