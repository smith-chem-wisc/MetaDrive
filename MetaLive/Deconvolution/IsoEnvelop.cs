using System.Collections.Generic;
using System.Linq;
using Chemistry;
using System;
using System.Collections;

namespace MassSpectrometry
{
    public class IsoEnvelop
    {
        public IsoEnvelop() { }

        public IsoEnvelop(MzPeak[] exp, MzPeak[] theo, double mass, int charge, List<int> theoPeakIndex)
        {
            ExperimentIsoEnvelop = exp;
            TheoIsoEnvelop = theo;
            MonoisotopicMass = mass;
            Charge = charge;
            TheoPeakIndex = theoPeakIndex;
        }

        public MzPeak[] ExperimentIsoEnvelop { get; set; }

        public double MostIntensePeak
        {
            get
            {
                return ExperimentIsoEnvelop.First().Mz;
            }
        }

        public MzPeak[] TheoIsoEnvelop { get; set; }

        public List<int> TheoPeakIndex { get; set; }

        public double MonoisotopicMass { get; set; }

        public double Mz
        {
            get
            {
                return MonoisotopicMass.ToMz(Charge);
            }
        }

        public MzPeak[] ExistedExperimentPeak
        {
            get
            {
                return ExperimentIsoEnvelop.Where(p => p.Intensity > 0).ToArray();
            }
        }

        public double TotalIntensity
        {
            get
            {
                return ExperimentIsoEnvelop.Sum(p => p.Intensity);
            }
        }

        public double IntensityRatio { get; set; }

        public int Charge { get; set; }

        public double MsDeconvScore { get; set; }

        public double MsDeconvSignificance { get; set; }

        public int ScanNum { get; set; }

        public double RT { get; set; }

        //For NeuCode Feature
        public bool HasPartner { get; set; } = false;

        public bool IsLight { get; set; } = false;

        public IsoEnvelop Partner { get; set; }

        public bool Overlap(IsoEnvelop other)
        {
            if (this.ExistedExperimentPeak.Select(p => p.Mz).Intersect(other.ExistedExperimentPeak.Select(p => p.Mz)).Count() > 0)
            {
                return true;
            }
            return false;
        }

    }
}
