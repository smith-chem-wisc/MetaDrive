using System.Collections.Generic;
using System.Linq;

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

        public MzPeak[] TheoIsoEnvelop { get; set; }

        public List<int> TheoPeakIndex { get; set; }

        public double MonoisotopicMass { get; set; }

        public double TotalIntensity
        {
            get
            {
                return ExperimentIsoEnvelop.Sum(p => p.Intensity);
            }
        }

        public int Charge { get; set; }

        public double MsDeconvScore { get; set; }

        public double MsDeconvSignificance { get; set; }

        public int ScanNum { get; set; }
        public double RT { get; set; }

        //For NeuCode Feature
        public bool HasPartner { get; set; } = false;

        public IsoEnvelop Partner { get; set; }

    }
}
