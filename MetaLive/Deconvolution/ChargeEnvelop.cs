using System.Collections.Generic;
using System.Linq;


namespace MassSpectrometry
{
    public class ChargeEnvelop
    {
        public ChargeEnvelop(int firstIndex, double firstMz, double firstIntensity)
        {
            FirstIndex = firstIndex;
            FirstMz = firstMz;
            FirstIntensity = firstIntensity;
        }

        public List<(int charge, MzPeak peak, IsoEnvelop isoEnvelop)> distributions { get; set; } = new List<(int charge, MzPeak peak, IsoEnvelop isoEnvelop)>();

        public int FirstIndex { get; set; }

        public double FirstMz { get; set; }

        public double FirstIntensity { get; set; }

        //The peaks used match for current Charge Envelop / The peaks already been used.
        public double UnUsedMzsRatio { get; set; }

        public int IsoEnveNum
        {
            get
            {
                return distributions.Where(p => p.isoEnvelop != null).Count();
            }
        }

        //The Intensity of Matched Peaks / Intensity of Whole spectrum
        public double MatchedIntensityRatio { get; set; }

        public List<double> mzs
        {
            get
            {
                return distributions.Select(p => p.peak.Mz).ToList();
            }
        }

    }
}
