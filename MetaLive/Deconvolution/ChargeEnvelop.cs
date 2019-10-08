using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace MassSpectrometry
{
    public class ChargeEnvelop
    {
        public ChargeEnvelop(double monoMass)
        {
            MonoMass = MonoMass;
        }

        public List<(int charge, double mz, double intensity, IsoEnvelop isoEnvelop)> distributions { get; set; } = new List<(int charge, double mz, double intensity, IsoEnvelop isoEnvelop)>();

        public double MonoMass { get; }

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
                        mzs.Add(new MzPeak(d.mz, d.intensity));
                    }
                }
                return mzs;
            }
        }

        public List<int> TheoPeakIndex { get; set; }

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
                return distributions.Select(p => p.mz).ToList();
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
                return distributions.OrderByDescending(p => p.intensity).Take(Count_box).OrderBy(p => p.intensity).Select(p => p.mz).ToList();
            }
        }
    }
}
