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
                //var extrapolatedMonoisotopicMass = candidateForMostIntensePeakMz.ToMass(chargeState) - diffToMonoisotopic[massIndex]; // Optimized for proteoforms!!
                //var lowestMass = arrayOfPeaks.Min(b => b.Mz).ToMass(chargeState); // But may actually observe this small peak
                //var monoisotopicMass = Math.Abs(extrapolatedMonoisotopicMass - lowestMass) < 0.5 ? lowestMass : extrapolatedMonoisotopicMass;

                var monoisotopicMass = candidateForMostIntensePeakMz.ToMass(chargeState) - diffToMonoisotopic[massIndex];

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
                        if (temp.ExperimentIsoEnvelop.Where(p=>p.Intensity!=0).Count() >= bestIsotopeEnvelopeForThisPeak.ExperimentIsoEnvelop.Where(p => p.Intensity != 0).Count() + 3 
                            && cd > 1 && temp.Charge == bestIsotopeEnvelopeForThisPeak.Charge * cd)
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

            ////Deconvolution by MZ increasing order
            //double intensityThread = mzSpectrumXY.TotalIntensity / mzSpectrumXY.Size;
            for (int candidateForMostIntensePeak = 0; candidateForMostIntensePeak < mzSpectrumXY.XArray.Length - 1; candidateForMostIntensePeak++)
            {
                //if (mzSpectrumXY.YArray[candidateForMostIntensePeak] <= intensityThread)
                //{
                //    continue;
                //}

                double noiseLevel = CalNoiseLevel();

                //TO THINK: Only get one isoEnvelop per best peak. It is possible this is a overlap best peak with different charge state.
                IsoEnvelop bestIsotopeEnvelopeForThisPeak = MsDeconvExperimentPeak(mzSpectrumXY, candidateForMostIntensePeak, deconvolutionParameter, noiseLevel);

                if (bestIsotopeEnvelopeForThisPeak != null)
                {
                    bestIsotopeEnvelopeForThisPeak.MsDeconvSignificance = CalIsoEnvelopNoise(mzSpectrumXY, bestIsotopeEnvelopeForThisPeak);
                    bestIsotopeEnvelopeForThisPeak.IntensityRatio = bestIsotopeEnvelopeForThisPeak.TotalIntensity / mzSpectrumXY.TotalIntensity;

                    isolatedMassesAndCharges.Add(bestIsotopeEnvelopeForThisPeak);
                }
            }

            HashSet<double> seen = new HashSet<double>(); //Do we still need this

            List<IsoEnvelop> isoEnvelops = new List<IsoEnvelop>();

            //TO DO: consider peak overlap
            foreach (var ok in isolatedMassesAndCharges.OrderByDescending(b => b.MsDeconvScore))
            {
                //if (seen.Overlaps(ok.ExperimentIsoEnvelop.Select(b => b.Mz)))
                //{
                //    continue;
                //}

                int noOverlap = 0;
                foreach (var ah in ok.ExistedExperimentPeak.Select(b => b.Mz))
                {
                    if (!seen.Contains(ah))
                    {
                        noOverlap++;
                    }
                }
                if (noOverlap < 2)
                {
                    continue;
                }
                foreach (var ah in ok.ExperimentIsoEnvelop.Select(b => b.Mz))
                {
                    seen.Add(ah);
                }

                isoEnvelops.Add(ok);
            }

            
            var orderedIsoEnvelops = isoEnvelops.OrderBy(p => p.ExperimentIsoEnvelop.First().Mz).ToList();

            return orderedIsoEnvelops;
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


        public static Tuple<double, double>[] GenerateIntervals(List<IsoEnvelop> isoEnvelops)
        {
            HashSet<double> indics = new HashSet<double>();
            foreach (var iso in isoEnvelops)
            {
                indics.Add(iso.ExistedExperimentPeak.Min(p => p.Mz));
                indics.Add(iso.ExistedExperimentPeak.Max(p => p.Mz));
            }

            var intervals = indics.OrderBy(p => p).ToArray();

            Tuple<double, double>[] tuples = new Tuple<double, double>[intervals.Length + 1];
            tuples[0] = new Tuple<double, double>(int.MinValue, intervals[0]);

            for (int i = 1; i < intervals.Length; i++)
            {
                tuples[i] = new Tuple<double, double>(intervals[i - 1], intervals[i]);
            }

            tuples[intervals.Length] = new Tuple<double, double>(intervals[intervals.Length - 1], int.MaxValue);

            return tuples;
        }

        public static IEnumerable<IEnumerable<T>> GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetKCombs(list, length - 1).SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0), (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        public static bool IsosWithOverlap(List<IsoEnvelop> isoEnvelops, IEnumerable<int> indexes)
        {
            var ks = GetKCombs(indexes, 2);
            foreach (var k in ks)
            {
                if (isoEnvelops[k.ElementAt(0)].Overlap(isoEnvelops[k.ElementAt(1)]))
                {
                    return true;
                }
            }
            return false;
        }


        public static List<List<int>> CalIsoComb(List<IsoEnvelop> isoEnvelops, List<int> indexes)
        {
            List<List<int>> isoEnvelopLists = new List<List<int>>();

            List<int> isoEnvelopList = new List<int>();
            isoEnvelopLists.Add(isoEnvelopList);

            //TO DO: calculate composition
            //Consider overlap
            for (int i = 1; i <= isoEnvelops.Count(); i++)
            {
                var combs = GetKCombs(indexes, i);

                if (i >= 2)
                {
                    foreach (var comb in combs)
                    {                      
                        if (!IsosWithOverlap(isoEnvelops, comb))
                        {
                            isoEnvelopLists.Add(comb.ToList());
                        }
                        
                    }
                }
                else
                {
                    foreach (var comb in combs)
                    {
                        isoEnvelopLists.Add(comb.ToList());
                    }
                }
            }

            return isoEnvelopLists;
        }


        public static bool DecideToAddEdge(Vertex<Tuple<int, List<int>>> v1, Vertex<Tuple<int, List<int>>> v2, out double score)
        {
            score = 0;
            //TO DO: rules to add edge

            if (v1.Value.Item2.Count == v2.Value.Item2.Count && v1.Value.Item2.All(v2.Value.Item2.Contains))
            {
                //TO DO: edge score can also be added here.
                score = 0;
                return true;
            }

            //var x = v1.Value.Item2.Except(v2.Value.Item2);
            if (v1.Value.Item2.Count == v2.Value.Item2.Count + 1 && v2.Value.Item2.Any(v1.Value.Item2.Contains))
            {
                return true;
            }

            //var y = v2.Value.Item2.Except(v1.Value.Item2);
            if (v1.Value.Item2.Count + 1 == v2.Value.Item2.Count && v1.Value.Item2.Any(v2.Value.Item2.Contains))
            {
                return true;
            }
            
            return false;
        }

        public static double GetScore(List<IsoEnvelop> isoEnvelops, Tuple<double, double> tuple, List<int> isoIndependentInd)
        {
            foreach (var ind in isoIndependentInd)
            {
                if (isoEnvelops[ind].ExistedExperimentPeak.Min(p=>p.Mz) == tuple.Item1)
                {
                    return isoEnvelops[ind].MsDeconvScore;
                }
            }
            return 0;
        }

        public static Graph<Tuple<int, List<int>>> GenerateGraph(List<IsoEnvelop> isoEnvelops, Tuple<double, double>[] tuples)
        {
            Graph<Tuple<int, List<int>>> graph = new Graph<Tuple<int, List<int>>>();

            for (int i = 0; i < tuples.Length; i++)
            {
                var t = tuples[i];
                var isoinds = new List<int>();
                for (int j = 0; j < isoEnvelops.Count(); j++)
                {
                    var min = isoEnvelops[j].ExistedExperimentPeak.Min(p => p.Mz);
                    var max = isoEnvelops[j].ExistedExperimentPeak.Max(p => p.Mz);
                    if ((min >= t.Item1  && min < t.Item2) || (max > t.Item1 && max <= t.Item2) || (min <= t.Item1 && max >= t.Item2))
                    {
                        isoinds.Add(j);
                    }
                }

                foreach (var isoIndependentInd in CalIsoComb(isoEnvelops, isoinds))
                {
                    var vtx = new Vertex<Tuple<int, List<int>>>(new Tuple<int, List<int>>(i, isoIndependentInd));
                    //TO DO: To calculate score
                    
                    graph.AddToList(vtx);
                }
            }

            for (int i = 0; i < tuples.Length -1; i++)
            {
                foreach (var v1 in graph.Vertices.Where(p => p.Value.Item1 == i))
                {
                    foreach (var v2 in graph.Vertices.Where(p => p.Value.Item1 == i + 1))
                    {
                        double score;
                        if (DecideToAddEdge(v1, v2, out score))
                        {
                            v1.AddEdge(v2);
                        }
                    }
                }
      
            }

            return graph;
        }


        public static void CalBestPath(Graph<Tuple<int, List<IsoEnvelop>>> graph, Tuple<double, double>[] tuples)
        {
            for (int i = tuples.Length - 2; i >= 0; i--)
            {
                foreach (var v1 in graph.Vertices.Where(p => p.Value.Item1 == i))
                {
                    double bestScore = 0;
                    for (int j = 0; j < v1.Neighbors.Count(); j++)
                    {
                        //The score of the current path
                        var currentScore = v1.Score + v1.Neighbors[j].UpScore;
                        if (currentScore >= bestScore)
                        {
                            v1.BestScoreNeighbor = v1.Neighbors[j];
                            v1.UpScore = currentScore;
                            bestScore = currentScore;
                        }
                    }

                }
            }
        }

        #endregion
    }
}
