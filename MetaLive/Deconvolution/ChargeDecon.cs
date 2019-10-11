using System;
using System.Collections.Generic;
using System.Linq;
using Chemistry;


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
        public static Dictionary<int, double> GenerateMzs(double monomass, double low, double high)
        {
            Dictionary<int, double> mz_z = new Dictionary<int, double>();

            int down = (int)(monomass/high) + 1;
            int up = (int)(monomass/low);
            for (int i = down; i <= up; i++)
            {
                mz_z.Add(i, monomass.ToMz(i));              
            }

            return mz_z;
        }

        //Currently only get the length of continious charges.
        private static double ScoreCurrentCharge(List<(int charge, double mz, double intensity, int index)> the_matched_mz_z)
        {
            List<int> matchedCharges = the_matched_mz_z.Select(p=>p.charge).ToList();

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

        private static bool QuickFilterIsoenvelop(List<(int charge, double mz, double intensity, int index)> matched_mz_z, MzSpectrumXY mzSpectrumXY, DeconvolutionParameter deconvolutionParameter)
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
                
                var left = t.mz - 1.00289 / t.charge;
                var leftInd = GetCloestIndex(left, mzSpectrumXY.XArray);

                var right = t.mz + 1.00289 / t.charge;
                var rightInd = GetCloestIndex(right, mzSpectrumXY.XArray);

                if (deconvolutionParameter.DeconvolutionAcceptor.Within(left, mzSpectrumXY.XArray[leftInd])
                    && deconvolutionParameter.DeconvolutionAcceptor.Within(right, mzSpectrumXY.XArray[rightInd]))
                {
                    lrs++;
                }
            }

            return lrs >= 3;
        }

        private static List<(int charge, double mz, double intensity, int index)> GetMzsOfPeakAtCharge(MzSpectrumXY mzSpectrumXY, int index, int charge, DeconvolutionParameter deconvolutionParameter)
        {
            var mz = mzSpectrumXY.XArray[index];

            var mz_z = GenerateMzs(mz.ToMass(charge), 400, 2000);

            List<(int charge, double mz, double intensity, int index)> the_matched_mz_z = new List<(int charge, double mz, double intensity, int index)>();
            foreach (var amz in mz_z)
            {
                var ind = GetCloestIndex(amz.Value, mzSpectrumXY.XArray);

                if (deconvolutionParameter.DeconvolutionAcceptor.Within(amz.Value, mzSpectrumXY.XArray[ind]))
                {
                    the_matched_mz_z.Add((amz.Key, mzSpectrumXY.XArray[ind], mzSpectrumXY.YArray[ind], ind));
                }
            }
            
            return the_matched_mz_z;
        }

        public static List<(int charge, double mz, double intensity, int index)> FindChargesForPeak(MzSpectrumXY mzSpectrumXY, int index, DeconvolutionParameter deconvolutionParameter)
        {
            var mz = mzSpectrumXY.XArray[index];

            //Key is charge, value.item1 is index in spectrum, value item2 is peak.
            List<(int charge, double mz, double intensity, int index)> matched_mz_z = new List<(int charge, double mz, double intensity, int index)>();
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
                List<int> indexes;
                List<(int charge, double mz, double intensity, int index)> the_matched_mz_z = GetMzsOfPeakAtCharge(mzSpectrumXY, index, i, deconvolutionParameter);

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
        public static List<ChargeEnvelop> FindChargesForScan(MzSpectrumXY mzSpectrumXY, DeconvolutionParameter deconvolutionParameter, int limit = 0)
        {
            List<ChargeEnvelop> chargeEnvelops = new List<ChargeEnvelop>();
            HashSet<int> seenPeakIndex = new HashSet<int>();

            foreach (var peakIndex in mzSpectrumXY.ExtractIndicesByY())
            {
                if (limit != 0 && chargeEnvelops.Count==limit)
                {
                    break;
                }

                if (seenPeakIndex.Contains(peakIndex))
                {
                    continue;
                }

                var mz_zs = FindChargesForPeak(mzSpectrumXY, peakIndex, deconvolutionParameter);

                if (mz_zs.Count >= 4)
                {
                    var chargeEnve = new ChargeEnvelop(mz_zs.First().mz.ToMass(mz_zs.First().charge));
                    int un_used_mzs = 0;
                    int total_mzs = 0;
                    double matched_intensities = 0;

                    foreach (var mzz in mz_zs)
                    {
                        List<int> arrayOfMatchedTheoPeakIndexes;
                        var iso = IsoDecon.GetETEnvelopForPeakAtChargeState(mzSpectrumXY, mzz.mz, mzz.charge, deconvolutionParameter, 0, out arrayOfMatchedTheoPeakIndexes);

                        chargeEnve.distributions.Add((mzz.charge,mzz.mz, mzz.intensity, iso));                     

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
                    var chargeEnve = new ChargeEnvelop(mz_zs.First().mz.ToMass(mz_zs.First().charge));
                    foreach (var mzz in mz_zs)
                    {
                        chargeEnve.distributions.Add((mzz.charge, mzz.mz, mzz.intensity, null));
                        if (seenMz.ContainsKey(mzz.mz))
                        {
                            continue;
                        }
                        var x = mzz.mz * mzz.charge;
                        var range = (5.581E-4 * x + 1.64 * Math.Log(x) - 2.608E-9 * Math.Pow(x, 2) - 6.58)/mzz.charge/2;
                        seenMz.Add(mzz.mz, range);
                        
                    }
                    chargeEnvelops.Add(chargeEnve);
                }
            }

            return chargeEnvelops;
        }

        //Find Charge for each peak, try get chargeEnvelop based on filters, then for each charge, try get isoEnvelop. 
        public static List<ChargeEnvelop> QuickChargeDeconForScan(MzSpectrumXY mzSpectrumXY, DeconvolutionParameter deconvolutionParameter, out List<IsoEnvelop> isoEnvelops)
        {
            List<ChargeEnvelop> chargeEnvelops = new List<ChargeEnvelop>();

            foreach (var peakIndex in mzSpectrumXY.ExtractIndices(mzSpectrumXY.Range.Minimum, mzSpectrumXY.Range.Maximum))
            {
                var mz_zs = FindChargesForPeak(mzSpectrumXY, peakIndex, deconvolutionParameter);

                if (mz_zs.Count >= 4)
                {
                    var chargeEnve = new ChargeEnvelop(mz_zs.First().mz.ToMass(mz_zs.First().charge));

                    foreach (var mzz in mz_zs)
                    {
                        List<int> arrayOfTheoPeakIndexes; //Is not used here, is used in ChargeDecon

                        var isoEnvelop = IsoDecon.GetETEnvelopForPeakAtChargeState(mzSpectrumXY, mzz.mz, mzz.charge, deconvolutionParameter, 0, out arrayOfTheoPeakIndexes);

                        IsoDecon.MsDeconvScore(isoEnvelop);

                        chargeEnve.distributions.Add((mzz.charge,  mzz.mz, mzz.intensity, isoEnvelop));

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

            isoEnvelops = IsoDecon.MsDeconv_Deconvolute(mzSpectrumXY, mzSpectrumXY.Range, deconvolutionParameter).Where(p => p.Charge >=5 && p.MsDeconvScore >= 50 && p.MsDeconvSignificance > 0.2).OrderByDescending(p => p.MsDeconvScore).ToList();

            return filteredChargeEnvelops;

        }

        //IsoDecon first, then ChargeDecon based on IsoEnvelops' peaks. The out 'isoEnvelop' are those not related to chargeEnvelop
        public static List<ChargeEnvelop> ChargeDeconIsoForScan(MzSpectrumXY mzSpectrumXY, DeconvolutionParameter deconvolutionParameter, out List<IsoEnvelop> isoEnvelops)
        {
            List<ChargeEnvelop> chargeEnvelops = new List<ChargeEnvelop>();

            isoEnvelops = new List<IsoEnvelop>();

            var isos = IsoDecon.MsDeconv_Deconvolute(mzSpectrumXY, mzSpectrumXY.Range, deconvolutionParameter).Where(p => p.Charge >= 5 && p.MsDeconvScore >= 50 && p.MsDeconvSignificance > 0.2).OrderByDescending(p => p.MsDeconvScore);


            HashSet<double> seenMz = new HashSet<double>();

            foreach (var iso in isos)
            {
                if (iso.Charge < 5 || seenMz.Overlaps(iso.ExperimentIsoEnvelop.Select(p => p.Mz)))
                {
                    continue;
                }

                var ind = iso.TheoPeakIndex.First();

                var mz_zs = GetMzsOfPeakAtCharge(mzSpectrumXY, ind, iso.Charge, deconvolutionParameter);

                if (mz_zs.Count > 4 && ScoreCurrentCharge(mz_zs) > 4)
                {
                    var chargeEnve = new ChargeEnvelop(mz_zs.First().mz.ToMass(mz_zs.First().charge));

                    foreach (var mzz in mz_zs)
                    {
                        var s = isos.Where(p => p.Charge == mzz.charge).Where(p => p.TheoPeakIndex.Contains(mzz.index)).FirstOrDefault();

                        chargeEnve.distributions.Add((mzz.charge, mzz.mz, mzz.intensity, s));

                        seenMz.Add(mzz.mz);
                    }

                    chargeEnve.IntensityRatio = chargeEnve.TotalIsoIntensity / mzSpectrumXY.TotalIntensity;

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
