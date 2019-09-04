using MzLibUtil;

namespace MassSpectrometry
{
    public class DeconvolutionParameter
    {
        public DeconvolutionParameter()
        {
            DeconvolutionMinAssumedChargeState = 2;
            DeconvolutionMaxAssumedChargeState = 6;
            DeconvolutionMassTolerance = 4;
            DeconvolutionIntensityRatio = 3;

            PartnerMassDiff = 0.018;
            MaxmiumLabelNumber = 3;
            PartnerPairRatio = 1;
            ParterMassTolerance = 10;
        }

        public double DeconvolutionIntensityRatio { get; set; }
        public int DeconvolutionMinAssumedChargeState { get; set; }
        public int DeconvolutionMaxAssumedChargeState { get; set; }
        public double DeconvolutionMassTolerance { get; set; }


        public double PartnerMassDiff { get; set; }
        public int MaxmiumLabelNumber { get; set; }
        public double PartnerPairRatio { get; set; }
        public double ParterMassTolerance { get; set; }

        public Tolerance DeconvolutionAcceptor
        {
            get
            {
                return new PpmTolerance(DeconvolutionMassTolerance);
            }
        }

        public Tolerance PartnerAcceptor
        {
            get
            {
                return new PpmTolerance(ParterMassTolerance);
            }
        }
    }
}
