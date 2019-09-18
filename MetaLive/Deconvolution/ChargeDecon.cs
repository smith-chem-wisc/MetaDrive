using System;
using System.Collections.Generic;
using System.Linq;
using MzLibUtil;


namespace MassSpectrometry
{
    public static class ChargeDecon
    {
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

        //Currently only get the length of continious charges.
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

            var x = continuousChangeLength.OrderByDescending(p => p).ToList();

            if (x.Count > 1)
            {
                return x[0] + x[1] - 1;
            }
            else
            {
                return x[0];
            }
        }

        private static bool QuickFilterIsoenvelop(Dictionary<int, MzPeak> matched_mz_z, MzSpectrumXY mzSpectrumXY, DeconvolutionParameter deconvolutionParameter)
        {
            //int lrs = 0;

            //foreach (var t in indexes)
            //{
            //    int lr = 0;

            //    int mint = t.Value - 2 < 0 ? 0 : t.Value - 2;
            //    int maxt = t.Value + 2 > mzSpectrumXY.Size - 1 ? mzSpectrumXY.Size - 1 : t.Value + 2;

            //    for (int i = mint; i <= maxt; i++)
            //    {
            //        if (deconvolutionParameter.DeconvolutionAcceptor.Within(Math.Abs(mzSpectrumXY.XArray[i] - matched_mz_z[t.Key].Mz) * t.Key, 1.0072))
            //        {
            //            lr++;
            //        }
            //    }

            //    if(lr >= 2)
            //    {
            //        lrs++;
            //    }
            //}

            //if (lrs >= 2)
            //{
            //    return true;
            //}

            //return false;

            int lrs = 0;
            foreach (var t in matched_mz_z)
            {
                
                var left = t.Value.Mz - 1.00289 / t.Key;
                var leftInd = GetCloestIndex(left, mzSpectrumXY.XArray);

                var right = t.Value.Mz + 1.00289 / t.Key;
                var rightInd = GetCloestIndex(right, mzSpectrumXY.XArray);

                if (deconvolutionParameter.DeconvolutionAcceptor.Within(left, mzSpectrumXY.XArray[leftInd])
                    && deconvolutionParameter.DeconvolutionAcceptor.Within(right, mzSpectrumXY.XArray[rightInd]))
                {
                    lrs++;
                }
            }

            return lrs >= 3;
        }

        private static Dictionary<int, MzPeak> GetMzsOfPeakAtCharge(MzSpectrumXY mzSpectrumXY, int index, int charge, DeconvolutionParameter deconvolutionParameter)
        {
            var mz = mzSpectrumXY.XArray[index];

            var mz_z = GenerateMzs(mz, charge);

            Dictionary<int, MzPeak> the_matched_mz_z = new Dictionary<int, MzPeak>();

            foreach (var amz in mz_z)
            {
                var ind = GetCloestIndex(amz.Value, mzSpectrumXY.XArray);

                if (deconvolutionParameter.DeconvolutionAcceptor.Within(amz.Value, mzSpectrumXY.XArray[ind]))
                {
                    the_matched_mz_z.Add(amz.Key, new MzPeak(mzSpectrumXY.XArray[ind], mzSpectrumXY.YArray[ind]));
                }
            }

            return the_matched_mz_z;
        }

        public static Dictionary<int, MzPeak> FindChargesForPeak(MzSpectrumXY mzSpectrumXY, int index, DeconvolutionParameter deconvolutionParameter)
        {
            var mz = mzSpectrumXY.XArray[index];

            //Key is charge, value.item1 is index in spectrum, value item2 is peak.
            Dictionary<int, MzPeak> matched_mz_z = new Dictionary<int, MzPeak>();
            double score = 1;

            //Find possible chargeStates.
            List<int> allPossibleChargeState = new List<int>();
            for (int i = index + 1; i < mzSpectrumXY.XArray.Length; i++)
            {
                if (mzSpectrumXY.XArray[i] - mz < 1.1) //In case charge is +1
                {
                    var chargeDouble = 1.00289 / (mzSpectrumXY.XArray[i] - mz);
                    int charge = Convert.ToInt32(chargeDouble);
                    if (deconvolutionParameter.DeconvolutionAcceptor.Within(mz + 1.00289 / chargeDouble, mzSpectrumXY.XArray[i])
                        && charge >= deconvolutionParameter.DeconvolutionMinAssumedChargeState
                        && charge <= deconvolutionParameter.DeconvolutionMaxAssumedChargeState
                        && !allPossibleChargeState.Contains(charge))
                    {
                        allPossibleChargeState.Add(charge);
                    }
                }
                else
                {
                    break;
                }
            }


            //each charge state
            foreach(var i in allPossibleChargeState)
            {
                Dictionary<int, MzPeak> the_matched_mz_z = GetMzsOfPeakAtCharge(mzSpectrumXY, index, i, deconvolutionParameter);

                if (the_matched_mz_z.Count >= 4 && QuickFilterIsoenvelop(the_matched_mz_z, mzSpectrumXY, deconvolutionParameter))
                {
                    double theScore = ScoreCurrentCharge(the_matched_mz_z);

                    if (theScore > score && theScore >= 4)
                    {
                        matched_mz_z = the_matched_mz_z;

                        score = theScore;
                    }

                }
            }

            return matched_mz_z;
        }

        private static bool PeakSeenInRange(double theMz, Dictionary<double, double> seenMzRange)
        {
            foreach (var seen in seenMzRange)
            {
                if (theMz >= seen.Key - seen.Value && theMz <= seen.Key + seen.Value)
                {
                    return true;
                }
            }
            return false;
        }


        //Find Charge for intensity ordered peak, try get isoEnvelop for each charge, filtered by seen peak in each Envelop 
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

                if (mz_zs.Count >= 4)
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
                        chargeEnvelops.Add(chargeEnve);
                    }
                }
            }

            return chargeEnvelops;
        }

        //Find Charge for intensity ordered peak, try get chargeEnvelop based on filters.
        public static List<ChargeEnvelop> QuickFindChargesForScan(MzSpectrumXY mzSpectrumXY, DeconvolutionParameter deconvolutionParameter)
        {
            List<ChargeEnvelop> chargeEnvelops = new List<ChargeEnvelop>();

            //mz and range
            Dictionary<double, double> seenMz = new Dictionary<double, double>();
            int size = mzSpectrumXY.Size / 8;
            foreach (var peakIndex in mzSpectrumXY.ExtractIndicesByY().Take(size))
            {
                if (PeakSeenInRange(mzSpectrumXY.XArray[peakIndex], seenMz))
                {
                    continue;
                }

                var mz_zs = FindChargesForPeak(mzSpectrumXY, peakIndex, deconvolutionParameter);

                if (mz_zs.Count >= 4)
                {
                    var chargeEnve = new ChargeEnvelop(peakIndex, mzSpectrumXY.XArray[peakIndex], mzSpectrumXY.YArray[peakIndex]);
                    foreach (var mzz in mz_zs)
                    {
                        chargeEnve.distributions.Add((mzz.Key, mzz.Value, null));
                        if (seenMz.ContainsKey(mzz.Value.Mz))
                        {
                            continue;
                        }
                        var x = mzz.Value.Mz * mzz.Key;
                        var range = (5.581E-4 * x + 1.64 * Math.Log(x) - 2.608E-9 * Math.Pow(x, 2) - 6.58)/mzz.Key/2;
                        seenMz.Add(mzz.Value.Mz, range);
                        
                    }
                    chargeEnvelops.Add(chargeEnve);
                }
            }

            return chargeEnvelops;
        }

        //Find Charge for each peak, try get chargeEnvelop based on filters, then for each charge, try get isoEnvelop. 
        public static List<ChargeEnvelop> QuickChargeDeconForScan(MzSpectrumXY mzSpectrumXY, DeconvolutionParameter deconvolutionParameter)
        {
            List<ChargeEnvelop> chargeEnvelops = new List<ChargeEnvelop>();

            foreach (var peakIndex in mzSpectrumXY.ExtractIndices(mzSpectrumXY.Range.Minimum, mzSpectrumXY.Range.Maximum))
            {
                var mz_zs = FindChargesForPeak(mzSpectrumXY, peakIndex, deconvolutionParameter);

                if (mz_zs.Count >= 4)
                {
                    var chargeEnve = new ChargeEnvelop(peakIndex, mzSpectrumXY.XArray[peakIndex], mzSpectrumXY.YArray[peakIndex]);

                    foreach (var mzz in mz_zs)
                    {
                        List<int> arrayOfTheoPeakIndexes; //Is not used here, is used in ChargeDecon

                        var isoEnvelop = IsoDecon.GetETEnvelopForPeakAtChargeState(mzSpectrumXY, mzz.Value.Mz, mzz.Key, deconvolutionParameter, 0, out arrayOfTheoPeakIndexes);

                        IsoDecon.MsDeconvScore(isoEnvelop);

                        chargeEnve.distributions.Add((mzz.Key, mzz.Value, isoEnvelop));

                    }
                    chargeEnvelops.Add(chargeEnve);
                }
            }

            List<ChargeEnvelop> filteredChargeEnvelops = new List<ChargeEnvelop>();


            HashSet<double> seenMz = new HashSet<double>();

            foreach (var ce in chargeEnvelops.OrderByDescending(p=>p.ChargeDeconScore))
            {
                //TO DO: here the overlap should count the overlap percent.
                if (seenMz.Overlaps(ce.AllMzPeak.Select(p=>p.Mz)))
                {
                    continue;
                }
                foreach (var ah in ce.AllMzPeak.Select(p => p.Mz))
                {
                    seenMz.Add(ah);
                }
                //yield return ok;
                filteredChargeEnvelops.Add(ce);
            }

            //var isos = IsoDecon.MsDeconv_Deconvolute(mzSpectrumXY, mzSpectrumXY.Range, deconvolutionParameter).OrderByDescending(p => p.MsDeconvScore);

            return filteredChargeEnvelops;

        }

        //IsoDecon first, then ChargeDecon based on IsoEnvelops' peaks. The out 'isoEnvelop' are those not related to chargeEnvelop
        public static List<ChargeEnvelop> ChargeDeconIonForScan(MzSpectrumXY mzSpectrumXY, DeconvolutionParameter deconvolutionParameter, out List<IsoEnvelop> isoEnvelops)
        {
            List<ChargeEnvelop> chargeEnvelops = new List<ChargeEnvelop>();

            isoEnvelops = new List<IsoEnvelop>();

            var isos = IsoDecon.MsDeconv_Deconvolute( mzSpectrumXY, mzSpectrumXY.Range, deconvolutionParameter).OrderByDescending(p=>p.MsDeconvScore);

            //Dictionary<double, double> seenMz = new Dictionary<double, double>();
            HashSet<double> seenMz = new HashSet<double>();

            foreach (var iso in isos)
            {
                //if (iso.Charge < 5 || PeakSeenInRange(iso.ExperimentIsoEnvelop.First().Mz, seenMz))
                //{
                //    continue;
                //}

                if (iso.Charge < 5 || seenMz.Overlaps(iso.ExperimentIsoEnvelop.Select(p=>p.Mz)))
                {
                    continue;
                }

                var ind = iso.TheoPeakIndex.First();

                var mzs = GetMzsOfPeakAtCharge(mzSpectrumXY, ind, iso.Charge, deconvolutionParameter);

                if (mzs.Count > 4 && ScoreCurrentCharge(mzs) > 4)
                {
                    var chargeEnve = new ChargeEnvelop(ind, mzSpectrumXY.XArray[ind], mzSpectrumXY.YArray[ind]);
                    foreach (var mzz in mzs)
                    {
                        chargeEnve.distributions.Add((mzz.Key, mzz.Value, null));

                        //if (seenMz.ContainsKey(mzz.Value.Mz))
                        //{
                        //    continue;
                        //}
                        //var x = mzz.Value.Mz * mzz.Key;
                        //var range = (5.581E-4 * x + 1.64 * Math.Log(x) - 2.608E-9 * Math.Pow(x, 2) - 6.58) / mzz.Key / 2;
                        //seenMz.Add(mzz.Value.Mz, range);

                        seenMz.Add(mzz.Value.Mz);
                    }
                    chargeEnvelops.Add(chargeEnve);
                }
                else
                {
                    isoEnvelops.Add(iso);
                }
            }

            return chargeEnvelops;
        }


    }
}
