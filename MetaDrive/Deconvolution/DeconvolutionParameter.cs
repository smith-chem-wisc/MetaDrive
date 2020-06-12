using MzLibUtil;

namespace MassSpectrometry
{
    public class DeconvolutionParameter
    {
        public DeconvolutionParameter()
        {
            DeconvolutionMinAssumedChargeState = 5;
            DeconvolutionMaxAssumedChargeState = 60;
            DeconvolutionMassTolerance = 4;
            DeconvolutionIntensityRatio = 3;
        }

        public double DeconvolutionIntensityRatio { get; set; }
        public int DeconvolutionMinAssumedChargeState { get; set; }
        public int DeconvolutionMaxAssumedChargeState { get; set; }
        public double DeconvolutionMassTolerance { get; set; }

        public Tolerance DeconvolutionAcceptor
        {
            get
            {
                return new PpmTolerance(DeconvolutionMassTolerance);
            }
        }
    }
}
