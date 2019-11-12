using System;
using System.Collections.Generic;
using System.Linq;
using MassSpectrometry;
namespace MetaLive
{
    public enum MethodTypes
    {
        ShotGun = 0,
        StaticBoxCar = 1,
        DynamicBoxCar = 2,
        GlycoFeature = 3,
        Partner = 4,
        UserDefined = 5
    }

    public class Parameters
    {
        public GeneralSetting GeneralSetting { get; set; }
        public BoxCarScanSetting BoxCarScanSetting { get; set; }
        public FullScanSetting FullScanSetting { get; set; }
        public MS1IonSelecting MS1IonSelecting { get; set; }
        public MS2ScanSetting MS2ScanSetting { get; set; }
        public GlycoSetting GlycoSetting { get; set; }
        public DeconvolutionParameter DeconvolutionParameter { get; set; }
    }

    public class GeneralSetting
    {
        public bool TestMod { get; set; }
        public MethodTypes MethodType { get; set; }
        public int TotalTimeInMinute { get; set; }
        public int Polarity { get; set; }
        public int SourceCID { get; set; }
        public int AGC_Mode { get; set; }
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
        public double BoxCarMzRangeLowBound { get; set; }
        public double BoxCarMzRangeHighBound { get; set; }
        public int BoxCarMicroScans { get; set; }
        public double DynamicBlockSize { get; set; }
        public bool DoDbcForMS1 { get; set; }
        public bool PrecursorSkipScan { get; set; } //Is the precursors from one MS1 scan before the nearest one
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
        public double ExclusionTolerance { get; set; }
        public double IsolationWindow { get; set; }
        public int NormCharge { get; set; }  // TO DO: What does this number mean? Once tried with 9 and crash. 
        public int IntensityThreshold { get; set; }
    }

    public class MS2ScanSetting
    {
        public bool DoMS2 { get; set; }
        public bool DoDbcMS2 { get; set; }
        public int MS2MaxInjectTimeInMillisecond { get; set; }
        public int MS2Resolution { get; set; }
        public int NCE { get; set; }
        public string NCE_factors { get; set; }
        public int MS2AgcTarget { get; set; }
        public int MS2MzRangeLowBound { get; set; }
        public int MS2MzRangeHighBound { get; set; }
        public int MS2MicroScans { get; set; }
    }

    public class GlycoSetting
    {
        public int InclusionDuration { get; set; }
        public int TopN { get; set; }
    }
}