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

        public List<(int charge, double mz, double intensity, IsoEnvelop isoEnvelop)> distributions_withIso
        {
            get
            {
                return distributions.Where(p => p.isoEnvelop != null).ToList();
            }
        }

        public List<int> TheoPeakIndex { get; set; }

        public double TotalIsoIntensity
        {
            get
            {
                return distributions_withIso.Sum(p => p.isoEnvelop.TotalIntensity);
            }
        }

        public double IntensityRatio { get; set; }

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

        //How to select 'Fragmentation Mesh'
        public List<double> mzs_box
        {
            get
            {
                ////Method 1:
                //int edge = distributions_withIso.Count >= 8 ? 2 : 1;
                //return distributions_withIso.OrderByDescending(p => p.isoEnvelop.TotalIntensity)
                //    .Take(distributions_withIso.Count*2/3 + edge).OrderBy(p=>p.charge)
                //    .Where((x, i) => i % 2 == 0)
                //    .Select(p => p.isoEnvelop.ExperimentIsoEnvelop.First().Mz).ToList();

                //Method 2:
                return distributions_withIso.OrderBy(p => p.charge).Skip(1)
                    .Where((x, i) => i % 2 == 0)
                    .Select(p => p.isoEnvelop.ExperimentIsoEnvelop.First().Mz).Take(3).ToList();
            }
        }
    }
}
