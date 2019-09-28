using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

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

        public List<MzPeak> AllMzPeak
        {
            get
            {
                List<MzPeak> mzs = new List<MzPeak>();
                foreach (var d in distributions)
                {
                    if (d.isoEnvelop!=null)
                    {
                        mzs.AddRange(d.isoEnvelop.ExperimentIsoEnvelop);
                    }
                    else
                    {
                        mzs.Add(d.peak);
                    }
                }
                return mzs;
            }
        }

        public double ChargeDeconScore
        {
            get
            {
                return distributions.Where(p=>p.isoEnvelop!=null).Sum(p => p.isoEnvelop.MsDeconvScore);
            }
        }

        //The peaks used match for current Charge Envelop / The peaks already been used.
        public double UnUsedMzsRatio { get; set; }

        public int IsoEnveNum
        {
            get
            {
                return distributions.Where(p => p.isoEnvelop != null).Count();
            }
        }

        public List<double> mzs
        {
            get
            {
                return distributions.Select(p => p.peak.Mz).OrderBy(p=>p).ToList();
            }
        }

        //The number for ms2 box_car should be related with intensity
        public int Count_box
        {
            get
            {
                int count = distributions.Count() * 2 / 3;
                return count > 6 ? 6 : count;
            }
        }

        public List<double> mzs_box
        {
            get
            {
                return distributions.OrderByDescending(p => p.peak.Intensity).Take(Count_box).OrderBy(p => p.peak.Intensity).Select(p => p.peak.Mz).ToList();
            }
        }
    }
}
