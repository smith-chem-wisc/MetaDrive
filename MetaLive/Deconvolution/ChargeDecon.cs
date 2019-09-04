using System;
using System.Collections.Generic;
using System.Linq;
using MzLibUtil;


namespace MassSpectrometry
{
    public static class ChargeDecon
    {
        static Tolerance tolerance = new PpmTolerance(5);

        public static int GetCloestIndex(double x, double[] array)
        {
            if (array.Length == 0)
            {
                return 0;
            }
            int index = Array.BinarySearch(array, x);
            if (index >= 0)
            {
                return index;
            }
            index = ~index;

            if (index >= array.Length)
            {
                return index - 1;
            }
            if (index == 0)
            {
                return index;
            }

            if (x - array[index - 1] > array[index] - x)
            {
                return index;
            }
            return index - 1;
        }

        public static double[] GenerateRuler()
        {
            List<double> chargeDiff = new List<double>();
            for (int i = 1; i <= 60; i++)
            {
                chargeDiff.Add(Math.Log(i));
            }
            return chargeDiff.ToArray();
        }

        //Dictinary<charge, mz>
        private static Dictionary<int, double> GenerateMzs(double mz, int charge)
        {
            Dictionary<int, double> mz_z = new Dictionary<int, double>();

            var monomass = mz * charge - charge * 1.0072;

            for (int i = 1; i <= 60; i++)
            {
                mz_z.Add(i, (monomass + i * 1.0072) / i);

            }

            return mz_z;
        }

        private static double ScoreCurrentCharge(MzSpectrumXY mzSpectrumBU_log, List<int> matchedCharges)
        {
            List<int> continuousChangeLength = new List<int>();

            if (matchedCharges.Count <= 1)
            {
                return 1;
            }

            int i = 0;
            int theChargeLength = 1;
            while (i < matchedCharges.Count() - 1)
            {
                int diff = matchedCharges[i + 1] - matchedCharges[i] - 1;
                if (diff == 0)
                {
                    theChargeLength++;
                }
                else
                {
                    continuousChangeLength.Add(theChargeLength);
                    theChargeLength = 1;
                }
                i++;

                if (i == matchedCharges.Count() - 1)
                {
                    continuousChangeLength.Add(theChargeLength);
                }
            }

            return continuousChangeLength.Max();

        }

        public static Dictionary<int, MzPeak> FindChargesForPeak(MzSpectrumXY mzSpectrumXY, int index, DeconvolutionParameter deconvolutionParameter)
        {
            var mz = mzSpectrumXY.XArray[index];

            Dictionary<int, MzPeak> matched_mz_z = new Dictionary<int, MzPeak>();

            double score = 1;

            //each charge state
            for (int i = deconvolutionParameter.DeconvolutionMinAssumedChargeState; i <= deconvolutionParameter.DeconvolutionMaxAssumedChargeState; i++)
            {
                var mz_z = GenerateMzs(mz, i);

                List<int> matchedIndexes = new List<int>();
                List<int> matchedCharges = new List<int>();

                foreach (var amz in mz_z)
                {
                    var ind = GetCloestIndex(amz.Value, mzSpectrumXY.XArray);

                    if (tolerance.Within(amz.Value, mzSpectrumXY.XArray[ind]))
                    {
                        matchedIndexes.Add(ind);
                        matchedCharges.Add(amz.Key);
                    }
                }

                double theScore = ScoreCurrentCharge(mzSpectrumXY, matchedCharges);

                if (theScore > score)
                {
                    matched_mz_z.Clear();
                    for (int j = 0; j < matchedIndexes.Count(); j++)
                    {
                        matched_mz_z.Add(matchedCharges[j], new MzPeak(mzSpectrumXY.XArray[matchedIndexes[j]], mzSpectrumXY.YArray[matchedIndexes[j]]));                      
                    }

                    score = theScore;
                }
            }

            return matched_mz_z;
        }

        public static List<ChargeEnvelop> FindChargesForScan(MzSpectrumXY mzSpectrumXY, DeconvolutionParameter deconvolutionParameter)
        {
            List<ChargeEnvelop> chargeEnvelops = new List<ChargeEnvelop>();
            HashSet<int> seenPeakIndex = new HashSet<int>();

            foreach (var peakIndex in mzSpectrumXY.ExtractIndicesByY())
            {
                if (seenPeakIndex.Contains(peakIndex))
                {
                    continue;
                }

                var mz_zs = FindChargesForPeak(mzSpectrumXY, peakIndex, deconvolutionParameter);

                if (mz_zs.Count >= 3)
                {
                    var chargeEnve = new ChargeEnvelop(peakIndex, mzSpectrumXY.XArray[peakIndex], mzSpectrumXY.YArray[peakIndex]);
                    int un_used_mzs = 0;
                    int total_mzs = 0;
                    double matched_intensities = 0;

                    foreach (var mzz in mz_zs)
                    {

                        List<int> arrayOfMatchedTheoPeakIndexes;
                        var iso = IsoDecon.GetETEnvelopForPeakAtChargeState(mzSpectrumXY, mzz.Value.Mz, mzz.Key, deconvolutionParameter, 0, out arrayOfMatchedTheoPeakIndexes);

                        chargeEnve.distributions.Add((mzz.Key, mzz.Value, iso));                     

                        foreach(var ind in arrayOfMatchedTheoPeakIndexes)
                        {
                            if (seenPeakIndex.Contains(ind))
                            {
                                un_used_mzs++;
                            }
                            else
                            {
                                seenPeakIndex.Add(ind);
                            }
                            total_mzs++;
                            matched_intensities += mzSpectrumXY.YArray[ind];
                        }                 
                    }

                    chargeEnve.UnUsedMzsRatio = (double)un_used_mzs / (double)total_mzs;

                    if (chargeEnve.UnUsedMzsRatio < 0.1 && chargeEnve.IsoEnveNum >=1)
                    {
                        chargeEnve.MatchedIntensityRatio = matched_intensities / mzSpectrumXY.TotalIntensity;
                        chargeEnvelops.Add(chargeEnve);
                    }
                }
            }

            return chargeEnvelops;
        }

    }
}
