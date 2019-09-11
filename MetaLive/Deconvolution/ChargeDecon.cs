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

        private static double ScoreCurrentCharge(Dictionary<int, MzPeak> the_matched_mz_z)
        {
            List<int> matchedCharges = the_matched_mz_z.Keys.ToList();

            List<int> continuousChangeLength = new List<int>();

            if (the_matched_mz_z.Count <= 1)
            {
                return 1;
            }

            int i = 0;
            int theChargeLength = 1;
            while (i < the_matched_mz_z.Count() - 1)
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

            var x = continuousChangeLength.OrderByDescending(p=>p).ToList();

            if (x.Count > 1)
            {
                return x[0] + x[1] - 1;
            }
            else
            {
                return x[0];
            }

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

                Dictionary<int, MzPeak> the_matched_mz_z = new Dictionary<int, MzPeak>();

                foreach (var amz in mz_z)
                {
                    var ind = GetCloestIndex(amz.Value, mzSpectrumXY.XArray);

                    if (tolerance.Within(amz.Value, mzSpectrumXY.XArray[ind]))
                    {
                        the_matched_mz_z.Add(amz.Key, new MzPeak(mzSpectrumXY.XArray[ind], mzSpectrumXY.YArray[ind]));
                    }                  
                }

                

                if (the_matched_mz_z.Count >= 5 )
                {
                    double theScore = ScoreCurrentCharge(the_matched_mz_z);

                    if (theScore > score && theScore >= 5)
                    {
                        matched_mz_z = the_matched_mz_z;

                        score = theScore;
                    }

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
                if (chargeEnvelops.Count() >= 5) //Set chargeEnvelops.Count() >= 5 is to increase the real-time deconvolution time
                {
                    break;
                }

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

        public static List<ChargeEnvelop> QuickFindChargesForScan(MzSpectrumXY mzSpectrumXY, DeconvolutionParameter deconvolutionParameter)
        {
            List<ChargeEnvelop> chargeEnvelops = new List<ChargeEnvelop>();

            HashSet<double> seenMz = new HashSet<double>();

            var indByY = mzSpectrumXY.ExtractIndicesByY();
            foreach (var peakIndex in indByY)
            {
                if (chargeEnvelops.Count >= 5 || mzSpectrumXY.YArray[peakIndex] / mzSpectrumXY.YArray[indByY.First()] < 0.02)
                {
                    break;
                }

                if (PeakSeenInRange(mzSpectrumXY.XArray[peakIndex], seenMz))
                {
                    continue;
                }

                var mz_zs = FindChargesForPeak(mzSpectrumXY, peakIndex, deconvolutionParameter);

                if (mz_zs.Count >= 5)
                {
                    var chargeEnve = new ChargeEnvelop(peakIndex, mzSpectrumXY.XArray[peakIndex], mzSpectrumXY.YArray[peakIndex]);
                    foreach (var mzz in mz_zs)
                    {
                        seenMz.Add(mzz.Value.Mz);
                        chargeEnve.distributions.Add((mzz.Key, mzz.Value, null));
                    }
                    chargeEnvelops.Add(chargeEnve);
                }
            }

            return chargeEnvelops;
        }

        private static bool PeakSeenInRange(double theMz, HashSet<double> seenMz, double range = 0.7)
        {
            foreach (var mz in seenMz)
            {
                if (theMz >= mz - range && theMz <= mz + range)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
