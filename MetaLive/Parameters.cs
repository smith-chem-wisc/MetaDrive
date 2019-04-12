using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaLive
{
    public class Parameters
    {
        public Parameters()
        {
            TestMod = false;

            TotalTimeInMinute = 1;

            BoxCarScans = 2;
            BoxCarBoxes = 12;
            BoxCarOverlap = 1;
            BoxCarMaxInjectTimeInMillisecond = 250;
            BoxCarResolution = 120000;
            BoxCarAgcTarget = 3000000;
            BoxCarMzRangeLowBound = 400;
            BoxCarMzRangeHighBound = 1200;

            MaxInjectTimeInMillisecond = 25;
            Resolution = 60000;
            AgcTarget = 3000000;
            MzRangeLowBound = 300;
            MzRangeHighBound = 1650;

            TopN = 10;
            DynamicExclusion = true;
            ExclusionDuration = 30;
            IsolationWindow = 1;
            NormCharge = 2;
            MinCharge = 2;
            MaxCharge = 5;
            IntensityThreshold = 0;

            MS2MaxInjectTimeInMillisecond = 28;
            MS2Resolution = 15000;
            NCE = 27;
            MS2AgcTarget = 100000;
            MS2MzRangeLowBound = 100;

        }

        //Software setting
        public bool TestMod { get; set; }

        //General setting
        public int TotalTimeInMinute { get; set; }     

        //BoxCar Scan setting
        public int BoxCarScans { get; set; }
        public int BoxCarBoxes { get; set; }
        public int BoxCarOverlap { get; set; }
        public int BoxCarMaxInjectTimeInMillisecond { get; set; }
        public int BoxCarResolution { get; set; }
        public int BoxCarAgcTarget { get; set;}
        public int BoxCarMzRangeLowBound { get; set; }
        public int BoxCarMzRangeHighBound { get; set; }

        //Full Scan setting
        public int MaxInjectTimeInMillisecond { get; set; }
        public int Resolution { get; set; }
        public int AgcTarget { get; set; }
        public int MzRangeLowBound { get; set; }
        public int MzRangeHighBound { get; set; }

        //MS1 ion selecting
        public int TopN { get; set; }
        public bool DynamicExclusion { get; set; }
        public int ExclusionDuration { get; set; }
        public double IsolationWindow { get; set; }
        public int NormCharge { get; set; }
        public int MinCharge { get; set; }
        public int MaxCharge { get; set; }
        public int IntensityThreshold { get; set; }

        //MS2 Scan setting
        public int MS2MaxInjectTimeInMillisecond { get; set; }
        public int MS2Resolution { get; set; }
        public int NCE { get; set; }
        public int MS2AgcTarget { get; set; }
        public int MS2MzRangeLowBound { get; set; }





    }
}
