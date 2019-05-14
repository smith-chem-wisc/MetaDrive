using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;

namespace MassSpectrometry
{
    public class NeuCodeIsotopicEnvelop : IsotopicEnvelope
    {
        public NeuCodeIsotopicEnvelop(List<(double mz, double intensity)> bestListOfPeaks, double bestMonoisotopicMass, int bestChargeState, double bestTotalIntensity, double bestStDev, int bestMassIndex) :
            base(bestListOfPeaks, bestMonoisotopicMass, bestChargeState, bestTotalIntensity, bestStDev, bestMassIndex)
        {

        }
          
        public int ScanNum { get; set; }
        public double RT { get; set; }
        public double ScanTotalIntensity { get; set; }
        public bool IsNeuCode { get; set; }

        public static string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("ScanNum" + "\t");
                sb.Append("RT" + "\t");
                sb.Append("monoisotopicMass" + "\t");
                sb.Append("MZ" + "\t");
                sb.Append("ScanTotalIntensity" + "\t");             
                sb.Append("charge" + "\t");
                sb.Append("totalIntensity" + "\t");
                sb.Append("peaks.Count" + "\t");
                sb.Append("stDev" + "\t");
                sb.Append("IsNeuCode" + "\t");
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ScanNum + "\t");
            sb.Append(RT + "\t");
            sb.Append(monoisotopicMass + "\t");
            sb.Append(ClassExtensions.ToMz(monoisotopicMass, charge) + "\t");
            sb.Append(ScanTotalIntensity + "\t");           
            sb.Append(charge + "\t");
            sb.Append(totalIntensity + "\t");         
            sb.Append(peaks.Count + "\t");
            sb.Append(stDev + "\t");
            sb.Append((IsNeuCode ? 1:0) + "\t");
            return sb.ToString();
        }
    }
}
