using MzLibUtil;

namespace MetaLive
{
    public class DeconvolutionParameter
    {
        public DeconvolutionParameter()
        {
            DeconvolutionMinAssumedChargeState = 2;
            DeconvolutionMaxAssumedChargeState = 6;
            DeconvolutionMassTolerance = 4;
            DeconvolutionIntensityRatio = 3;

            NeuCodeMassDefect = 32.7;
            MaxmiumNeuCodeNumber = 3;
            NeuCodePairRatio = 1;
            ParterMassTolerance = 10;
        }

        public double DeconvolutionIntensityRatio { get;  set; }
        public int DeconvolutionMinAssumedChargeState { get;  set; }
        public int DeconvolutionMaxAssumedChargeState { get;  set; }
        public double DeconvolutionMassTolerance { get;  set; }


        public double NeuCodeMassDefect { get; set; }
        public int MaxmiumNeuCodeNumber { get; set; }
        public double NeuCodePairRatio { get; set; }
        public double ParterMassTolerance { get; set; }
    }
}
