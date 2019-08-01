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
        public double SelectedMz { get; set; }

        //For NeuCode Feature
        public bool IsNeuCode { get; set; } = false;
        public NeuCodeIsotopicEnvelop Partner { get; set; }

        //For Glyco family construct
        public int MatchedFamilyCount { get; set; }
        public bool FromCurrentScan { get; set; } = true;
        public bool AlreadyExist{get; set;} = false;

        private static double SelectedRangeRatio(double[] x, double[] y, int massIndex, double SelectedMz)
        {
            double rangeIntensities = 0;
            int ind_down = massIndex;
            while (x[ind_down] < SelectedMz + 0.5 && x[ind_down] > SelectedMz + 1)
            {
                rangeIntensities += y[ind_down];
                ind_down--;
            }
            int ind_up = massIndex + 1;
            while (x[ind_up] < SelectedMz + 0.5 && x[ind_up] > SelectedMz + 1)
            {
                rangeIntensities += y[ind_up];
                ind_up++;
            }
            return rangeIntensities;
        }

        public static double EnvolopeToRangeRatio(double[] x, double[] y, int massIndex, double SelectedMz, double totalIntensity)
        {
            var rangeIntensity = SelectedRangeRatio(x, y, massIndex, SelectedMz);
            return totalIntensity / rangeIntensity;
        }

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
