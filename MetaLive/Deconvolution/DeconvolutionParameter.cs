using MzLibUtil;

namespace MetaLive
{
    public class DeconvolutionParameter
    {
        public DeconvolutionParameter()
        {
            DeconvolutionMinAssumedChargeState = 2;
            DeconvolutionMaxAssumedChargeState = 6;
            DeconvolutionMassTolerance = new PpmTolerance(5);
            DeconvolutionIntensityRatio = 3;
            CheckNeuCode = false;
            NeuCodeMassDefect = 32.7;
            MaxmiumNeuCodeNumber = 3;
        }

        public double DeconvolutionIntensityRatio { get;  set; }
        public int DeconvolutionMinAssumedChargeState { get;  set; }
        public int DeconvolutionMaxAssumedChargeState { get;  set; }
        public Tolerance DeconvolutionMassTolerance { get;  set; }

        //NeuCode
        public bool CheckNeuCode { get; set; }
        public double NeuCodeMassDefect { get; set; }
        public int MaxmiumNeuCodeNumber { get; set; }

    }
}
