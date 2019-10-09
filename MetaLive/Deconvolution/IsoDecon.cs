using Chemistry;
using MzLibUtil;
using System;
using System.Collections.Generic;
using System.Linq;


namespace MassSpectrometry
{
    public static class IsoDecon
    {

        private const int numAveraginesToGenerate = 1500;
        private static readonly double[][] allMasses = new double[numAveraginesToGenerate][];
        private static readonly double[][] allIntensities = new double[numAveraginesToGenerate][];
        private static readonly double[] mostIntenseMasses = new double[numAveraginesToGenerate];
        private static readonly double[] diffToMonoisotopic = new double[numAveraginesToGenerate];

        static IsoDecon()
        {
            //AVERAGINE
            const double averageC = 4.9384;
            const double averageH = 7.7583;
            const double averageO = 1.4773;
            const double averageN = 1.3577;
            const double averageS = 0.0417;

            //Glycopeptide Averagine
            //const double averageC = 10.93;
            //const double averageH = 15.75;
            //const double averageO = 6.48;
            //const double averageN = 1.66;
            //const double averageS = 0.02;

            const double fineRes = 0.125;
            const double minRes = 1e-8;

            for (int i = 0; i < numAveraginesToGenerate; i++)
            {
                double averagineMultiplier = (i + 1) / 2.0;
                //Console.Write("numAveragines = " + numAveragines);
                ChemicalFormula chemicalFormula = new ChemicalFormula();
                chemicalFormula.Add("C", Convert.ToInt32(averageC * averagineMultiplier));
                chemicalFormula.Add("H", Convert.ToInt32(averageH * averagineMultiplier));
                chemicalFormula.Add("O", Convert.ToInt32(averageO * averagineMultiplier));
                chemicalFormula.Add("N", Convert.ToInt32(averageN * averagineMultiplier));
                chemicalFormula.Add("S", Convert.ToInt32(averageS * averagineMultiplier));

                {
                    var chemicalFormulaReg = chemicalFormula;
                    IsotopicDistribution ye = IsotopicDistribution.GetDistribution(chemicalFormulaReg, fineRes, minRes);
                    var masses = ye.Masses.ToArray();
                    var intensities = ye.Intensities.ToArray();
                    Array.Sort(intensities, masses);
                    Array.Reverse(intensities);
                    Array.Reverse(masses);

                    mostIntenseMasses[i] = masses[0];
                    diffToMonoisotopic[i] = masses[0] - chemicalFormulaReg.MonoisotopicMass;
                    allMasses[i] = masses;
                    allIntensities[i] = intensities;
                }
            }
        }

        public static double[][] AllMasses { get { return allMasses; } }
        public static double[][] AllIntensities { get { return allIntensities; } }

        #region New deconvolution method optimized from MsDeconv (by Lei)

        //MsDeconv Score peak
        private static double MsDeconvScore_peak(MzPeak experiment, MzPeak theory, double mass_error_tolerance = 0.02)
        {
            double score = 0;

            double mass_error = Math.Abs(experiment.Mz - theory.Mz);

            double mass_accuracy = 0;

            if (mass_error <= 0.02)
            {
                mass_accuracy = 1 - mass_error / mass_error_tolerance;
            }

            double abundance_diff = 0;

            if (experiment.Intensity < theory.Intensity && (theory.Intensity - experiment.Intensity) / experiment.Intensity <= 1)
            {
                abundance_diff = 1 - (theory.Intensity - experiment.Intensity) / experiment.Intensity;
            }
            else if (experiment.Intensity >= theory.Intensity && (experiment.Intensity - theory.Intensity) / experiment.Intensity <= 1)
            {
                abundance_diff = Math.Sqrt(1 - (experiment.Intensity - theory.Intensity) / experiment.Intensity);
            }

            score = Math.Sqrt(theory.Intensity) * mass_accuracy * abundance_diff;

            return score;
        }

        //MsDeconv Envelop
        public static double MsDeconvScore(IsoEnvelop isoEnvelop)
        {
            if (isoEnvelop == null)
            {
                return 0;
            }

            double score = 0;

            for (int i = 0; i < isoEnvelop.TheoIsoEnvelop.Length; i++)
            {
                score += MsDeconvScore_peak(isoEnvelop.ExperimentIsoEnvelop[i], isoEnvelop.TheoIsoEnvelop[i]);
            }

            isoEnvelop.MsDeconvScore = score;

            return score;
        }

        //Scale Theoretical Envelope
        private static MzPeak[] ScaleTheoEnvelop(MzPeak[] experiment, MzPeak[] theory, string method = "sum")
        {
            var scale_Theory = new MzPeak[theory.Length];
            switch (method)
            {
                case "sum":
                    var total_abundance = experiment.Sum(p => p.Intensity);
                    scale_Theory = theory.Select(p => new MzPeak(p.Mz, p.Intensity * total_abundance)).ToArray();
                    break;
                default:
                    break;
            }
            return scale_Theory;
        }

        //Change the workflow for different score method.
        public static IsoEnvelop GetETEnvelopForPeakAtChargeState(MzSpectrumXY mzSpectrumXY, double candidateForMostIntensePeakMz, int chargeState, DeconvolutionParameter deconvolutionParameter, double noiseLevel, out List<int> arrayOfTheoPeakIndexes)
        {
            var testMostIntenseMass = candidateForMostIntensePeakMz.ToMass(chargeState);

            var massIndex = GetClosestIndexInArray(testMostIntenseMass, mostIntenseMasses).Value;

            var differenceBetweenTheorAndActual = candidateForMostIntensePeakMz.ToMass(chargeState) - mostIntenseMasses[massIndex];

            var theoryIsoEnvelopLength = 0;

            for (int i = 0; i < allIntensities[massIndex].Length; i++)
            {
                theoryIsoEnvelopLength++;

                if (allIntensities[massIndex][i]/allIntensities[massIndex][0] <= 0.05 && i >= 2)
                {
                    break;
                }
            }

            var arrayOfPeaks = new MzPeak[theoryIsoEnvelopLength];
            var arrayOfTheoPeaks = new MzPeak[theoryIsoEnvelopLength];
            arrayOfTheoPeakIndexes = new List<int>(); //For top-down to calculate MsDeconvSignificance

            for (int indexToLookAt = 0; indexToLookAt < theoryIsoEnvelopLength; indexToLookAt++)
            {
                double theorMassThatTryingToFind = allMasses[massIndex][indexToLookAt] + differenceBetweenTheorAndActual;
                arrayOfTheoPeaks[indexToLookAt] = new MzPeak(theorMassThatTryingToFind.ToMz(chargeState), allIntensities[massIndex][indexToLookAt]);

                var closestPeakToTheorMassIndex = GetClosestIndexInArray(theorMassThatTryingToFind.ToMz(chargeState), mzSpectrumXY.XArray);
                var closestPeakmz = mzSpectrumXY.XArray[closestPeakToTheorMassIndex.Value];
                var closestPeakIntensity = mzSpectrumXY.YArray[closestPeakToTheorMassIndex.Value];


                if (!deconvolutionParameter.DeconvolutionAcceptor.Within(theorMassThatTryingToFind, closestPeakmz.ToMass(chargeState)) || closestPeakIntensity < noiseLevel)
                {
                    closestPeakmz = theorMassThatTryingToFind.ToMz(chargeState);
                    closestPeakIntensity = 0;
                }
                else
                {
                    //if the peak was matched
                    arrayOfTheoPeakIndexes.Add(closestPeakToTheorMassIndex.Value);
                }
                arrayOfPeaks[indexToLookAt] = new MzPeak(closestPeakmz, closestPeakIntensity);
            }

            if (FilterEEnvelop(arrayOfPeaks))
            {
                var scaleArrayOfTheoPeaks = ScaleTheoEnvelop(arrayOfPeaks, arrayOfTheoPeaks);

                //The following 3 lines are for calculating monoisotopicMass, origin from Stephan, I don't understand it, and may optimize it in the future. (Lei)
                var extrapolatedMonoisotopicMass = candidateForMostIntensePeakMz.ToMass(chargeState) - diffToMonoisotopic[massIndex]; // Optimized for proteoforms!!
                var lowestMass = arrayOfPeaks.Min(b => b.Mz).ToMass(chargeState); // But may actually observe this small peak
                var monoisotopicMass = Math.Abs(extrapolatedMonoisotopicMass - lowestMass) < 0.5 ? lowestMass : extrapolatedMonoisotopicMass;

                IsoEnvelop isoEnvelop = new IsoEnvelop(arrayOfPeaks, scaleArrayOfTheoPeaks, monoisotopicMass, chargeState, arrayOfTheoPeakIndexes);
                return isoEnvelop;
            }

            return null;
        }

        private static int GetConsecutiveLength(MzPeak[] experiment, out int secondConsecutiveLenth)
        {
            var experimentOrderByMz = experiment.OrderBy(p => p.Mz).ToArray();
            List<int> inds = new List<int>();
            for (int i = 0; i < experimentOrderByMz.Length; i++)
            {
                if (experimentOrderByMz[i].Intensity == 0)
                {
                    inds.Add(i);
                }
            }

            if (inds.Count == 1)
            {
                secondConsecutiveLenth = 0;
                return inds.First();
            }
            else if (inds.Count > 1)
            {
                List<int> lens = new List<int>();

                lens.Add(inds[0]);

                for (int i = 0; i < inds.Count() - 1; i++)
                {
                    lens.Add(inds[i + 1] - inds[i] - 1);
                }
                secondConsecutiveLenth = lens.OrderByDescending(p => p).ElementAt(1);
                return lens.Max();
            }
            secondConsecutiveLenth = 0;
            return experiment.Length;
        }

        private static bool FilterEEnvelop(MzPeak[] experiment)
        {
            int secondConsecutiveLength = 0;
            int consecutiveLength = GetConsecutiveLength(experiment, out secondConsecutiveLength);
            if (consecutiveLength < 3 || consecutiveLength + secondConsecutiveLength < experiment.Length * 1 / 2)
            {
                return false;
            }

            return true;
        }

        public static IsoEnvelop MsDeconvExperimentPeak(MzSpectrumXY mzSpectrumXY, int candidateForMostIntensePeak, DeconvolutionParameter deconvolutionParameter, double noiseLevel)
        {
            IsoEnvelop bestIsotopeEnvelopeForThisPeak = null;

            var candidateForMostIntensePeakMz = mzSpectrumXY.XArray[candidateForMostIntensePeak];


            //Find possible chargeStates.
            List<int> allPossibleChargeState = new List<int>();
            for (int i = candidateForMostIntensePeak + 1; i < mzSpectrumXY.XArray.Length; i++)
            {
                if (mzSpectrumXY.XArray[i] - candidateForMostIntensePeakMz < 1.1) //In case charge is +1
                {
                    var chargeDouble = 1.00289 / (mzSpectrumXY.XArray[i] - candidateForMostIntensePeakMz);
                    int charge = Convert.ToInt32(chargeDouble);
                    if (deconvolutionParameter.DeconvolutionAcceptor.Within(candidateForMostIntensePeakMz + 1.00289 / chargeDouble, mzSpectrumXY.XArray[i])
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

            foreach (var chargeState in allPossibleChargeState)
            {
                List<int> arrayOfTheoPeakIndexes; //Is not used here, is used in ChargeDecon

                var isoEnvelop = GetETEnvelopForPeakAtChargeState(mzSpectrumXY, candidateForMostIntensePeakMz, chargeState, deconvolutionParameter, noiseLevel, out arrayOfTheoPeakIndexes);

                if (MsDeconvScore(isoEnvelop) > MsDeconvScore(bestIsotopeEnvelopeForThisPeak))
                {
                    var temp = bestIsotopeEnvelopeForThisPeak;
                    bestIsotopeEnvelopeForThisPeak = isoEnvelop;

                    //This is to refine mis charge ones. But not working perfect.
                    if (temp != null && bestIsotopeEnvelopeForThisPeak != null)
                    {
                        int cd = temp.Charge / bestIsotopeEnvelopeForThisPeak.Charge;
                        if (cd > 1 && temp.Charge == bestIsotopeEnvelopeForThisPeak.Charge * cd)
                        {
                            bestIsotopeEnvelopeForThisPeak = temp;
                        }
                    }
                }
            }
            return bestIsotopeEnvelopeForThisPeak;
        }

        //Kind of similar as a S/N filter. It works for top-down, Not working for bottom-up.
        private static double CalIsoEnvelopNoise(MzSpectrumXY mzSpectrumXY, IsoEnvelop isoEnvelop)
        {
            double intensityInRange = 0;

            int minInd = isoEnvelop.TheoPeakIndex.Min();

            int maxInd = isoEnvelop.TheoPeakIndex.Max();

            for (int i = minInd; i <= maxInd; i++)
            {
                intensityInRange += mzSpectrumXY.YArray[i];
            }

            //less peak and lower intensity means low ratio.
            double ratio = (isoEnvelop.TotalIntensity / intensityInRange) * ((double)isoEnvelop.ExperimentIsoEnvelop.Where(p => p.Intensity != 0).Count() / ((double)maxInd - (double)minInd + 1));

            return ratio;

        }

        private static double CalNoiseLevel()
        {
            return 0;
        }

        public static List<IsoEnvelop> MsDeconv_Deconvolute(MzSpectrumXY mzSpectrumXY, MzRange theRange, DeconvolutionParameter deconvolutionParameter)
        {
            var isolatedMassesAndCharges = new List<IsoEnvelop>();

            if (mzSpectrumXY.Size == 0)
            {
                return isolatedMassesAndCharges;
            }

            //HashSet<double> seenPeaks = new HashSet<double>();

            ////Deconvolution by Intensity decending order
            //foreach (var candidateForMostIntensePeak in ExtractIndicesByY())
            ////Deconvolution by MZ increasing order
            foreach (var candidateForMostIntensePeak in mzSpectrumXY.ExtractIndices(theRange.Minimum, theRange.Maximum))
            {
                //if (seenPeaks.Contains(XArray[candidateForMostIntensePeak]))
                //{
                //    continue;
                //}

                double noiseLevel = CalNoiseLevel();

                IsoEnvelop bestIsotopeEnvelopeForThisPeak = MsDeconvExperimentPeak(mzSpectrumXY, candidateForMostIntensePeak, deconvolutionParameter, noiseLevel);

                if (bestIsotopeEnvelopeForThisPeak != null)
                {
                    bestIsotopeEnvelopeForThisPeak.MsDeconvSignificance = CalIsoEnvelopNoise(mzSpectrumXY, bestIsotopeEnvelopeForThisPeak);

                    isolatedMassesAndCharges.Add(bestIsotopeEnvelopeForThisPeak);
                    //foreach (var peak in bestIsotopeEnvelopeForThisPeak.ExperimentIsoEnvelop.Select(p => p.Item1))
                    //{
                    //    seenPeaks.Add(peak);
                    //}
                }
            }

            HashSet<double> seen = new HashSet<double>(); //Do we still need this

            List<IsoEnvelop> isoEnvelops = new List<IsoEnvelop>();

            foreach (var ok in isolatedMassesAndCharges.OrderByDescending(b => b.MsDeconvScore))
            {
                if (seen.Overlaps(ok.ExperimentIsoEnvelop.Select(b => b.Mz)))
                {
                    continue;
                }
                foreach (var ah in ok.ExperimentIsoEnvelop.Select(b => b.Mz))
                {
                    seen.Add(ah);
                }
                //yield return ok;
                isoEnvelops.Add(ok);
            }

            var orderedIsoEnvelops = isoEnvelops.OrderBy(p => p.ExperimentIsoEnvelop.First().Mz).ToList();
            FindLabelPair(orderedIsoEnvelops, deconvolutionParameter);

            return orderedIsoEnvelops;
        }

        //isoEnvelops should be already ordered by mono mass
        //TO DO: need to be improved
        private static void FindLabelPair(List<IsoEnvelop> isoEnvelops, DeconvolutionParameter deconvolutionParameter)
        {
            if (!deconvolutionParameter.ToGetPartner)
            {
                return;
            }

            double[] monoMzs = isoEnvelops.Select(p => p.ExperimentIsoEnvelop.First().Mz).ToArray();

            foreach (var iso in isoEnvelops)
            {
                if (iso.HasPartner)
                {
                    continue;
                }

                for (int i = 1; i <= deconvolutionParameter.MaxmiumLabelNumber; i++)
                {
                    var possiblePairMass = iso.MonoisotopicMass + deconvolutionParameter.PartnerMassDiff * i;
                    var possiblePairMz = possiblePairMass.ToMz(iso.Charge);

                    var closestIsoIndex = GetClosestIndexInArray(possiblePairMz, monoMzs);

                    if (isoEnvelops.ElementAt(closestIsoIndex.Value).MonoisotopicMass != iso.MonoisotopicMass
                        && deconvolutionParameter.PartnerAcceptor.Within(isoEnvelops.ElementAt(closestIsoIndex.Value).MonoisotopicMass, possiblePairMass)
                        && iso.Charge == isoEnvelops.ElementAt(closestIsoIndex.Value).Charge)
                    {
                        var ratio = iso.TotalIntensity / isoEnvelops.ElementAt(closestIsoIndex.Value).TotalIntensity;
                        if (0.5 <= ratio && ratio <= 2)
                        {
                            iso.HasPartner = true;
                            iso.IsLight = true;
                            iso.Partner = isoEnvelops.ElementAt(closestIsoIndex.Value);
                            isoEnvelops.ElementAt(closestIsoIndex.Value).HasPartner = true;
                        }
                    }
                }
            }
        }

        private static int? GetClosestIndexInArray(double x, double[] array)
        {
            if (array.Length == 0)
            {
                return null;
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

        #endregion
    }
}
