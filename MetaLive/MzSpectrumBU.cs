using Chemistry;
using MathNet.Numerics.Statistics;
using MzLibUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MassSpectrometry
{
    public class MzSpectrumBU
    {
        private const int numAveraginesToGenerate = 1500;
        private static readonly double[][] allMasses = new double[numAveraginesToGenerate][];
        private static readonly double[][] allIntensities = new double[numAveraginesToGenerate][];
        private static readonly double[] mostIntenseMasses = new double[numAveraginesToGenerate];
        private static readonly double[] diffToMonoisotopic = new double[numAveraginesToGenerate];
        public static bool DoNeucodeModel; 

        private MzPeak[] peakList;

        public double[] XArray { get; private set; }
        public double[] YArray { get; private set; }

        public double[][] AllMasses { get { return allMasses; } }
        public double[][] AllIntensities { get { return allIntensities; } }

        static MzSpectrumBU()
        {
            // AVERAGINE
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
                    var masses = MassConvertToNeuCode( ye.Masses.ToArray(), 0.034, DoNeucodeModel);
                    var intensities = IntenConvertToNeuCode( ye.Intensities.ToArray(), 1, DoNeucodeModel);
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

        public MzSpectrumBU(double[] mz, double[] intensities, bool shouldCopy)
        {
            if (shouldCopy)
            {
                XArray = new double[mz.Length];
                YArray = new double[intensities.Length];
                Array.Copy(mz, XArray, mz.Length);
                Array.Copy(intensities, YArray, intensities.Length);
            }
            else
            {
                XArray = mz;
                YArray = intensities;
            }
            peakList = new MzPeak[Size];
        }

        public MzRange Range
        {
            get
            {
                if (Size == 0)
                {
                    return null;
                }
                return new MzRange(FirstX.Value, LastX.Value);
            }
        }

        public double? FirstX
        {
            get
            {
                if (Size == 0)
                {
                    return null;
                }
                return XArray[0];
            }
        }

        public double? LastX
        {
            get
            {
                if (Size == 0)
                {
                    return null;
                }
                return XArray[Size - 1];
            }
        }

        public int Size { get { return XArray.Length; } }

        public override string ToString()
        {
            return string.Format("{0} (Peaks {1})", Range, Size);
        }

        // Mass tolerance must account for different isotope spacing!
        public IEnumerable<IsotopicEnvelope> DeconvoluteBU(MzRange theRange, int minAssumedChargeState, int maxAssumedChargeState, double deconvolutionTolerancePpm, double intensityRatioLimit)
        {
            if (Size == 0)
            {
                yield break;
            }

            var isolatedMassesAndCharges = new List<IsotopicEnvelope>();

            foreach (var candidateForMostIntensePeak in ExtractIndices(theRange.Minimum, theRange.Maximum))
            {
                IsotopicEnvelope bestIsotopeEnvelopeForThisPeak = null;

                var candidateForMostIntensePeakMz = XArray[candidateForMostIntensePeak];
                //Console.WriteLine("candidateForMostIntensePeakMz: " + candidateForMostIntensePeakMz);
                var candidateForMostIntensePeakIntensity = YArray[candidateForMostIntensePeak];

                //TO DO: Find possible chargeState
                List<int> allPossibleChargeState = new List<int>();
                for (int i = candidateForMostIntensePeak + 1; i < XArray.Length; i++)
                {
                    if (XArray[i] - candidateForMostIntensePeakMz < 0.8) 
                    {
                        var chargeDouble = 1 / (XArray[i] - candidateForMostIntensePeakMz);
                        int charge = Convert.ToInt32(chargeDouble);
                        if (Math.Abs(chargeDouble - charge) <= 0.2 )
                        {
                            allPossibleChargeState.Add(charge);
                        }
                    }
                }

                foreach(var chargeState in allPossibleChargeState)
                {
                    //Console.WriteLine(" chargeState: " + chargeState);
                    var testMostIntenseMass = candidateForMostIntensePeakMz.ToMass(chargeState);

                    var massIndex = Array.BinarySearch(mostIntenseMasses, testMostIntenseMass);
                    if (massIndex < 0)
                        massIndex = ~massIndex;
                    if (massIndex == mostIntenseMasses.Length)
                    {
                        //Console.WriteLine("Breaking  because mass is too high: " + testMostIntenseMass);
                        break;
                    }
                    //Console.WriteLine("  massIndex: " + massIndex);

                    var listOfPeaks = new List<(double, double)> { (candidateForMostIntensePeakMz, candidateForMostIntensePeakIntensity) };
                    var listOfRatios = new List<double> { allIntensities[massIndex][0] / candidateForMostIntensePeakIntensity };
                    // Assuming the test peak is most intense...
                    // Try to find the rest of the isotopes!

                    double differenceBetweenTheorAndActual = testMostIntenseMass - mostIntenseMasses[massIndex];
                    double totalIntensity = candidateForMostIntensePeakIntensity;
                    for (int indexToLookAt = 1; indexToLookAt < allIntensities[massIndex].Length; indexToLookAt++)
                    {
                        //Console.WriteLine("   indexToLookAt: " + indexToLookAt);
                        double theorMassThatTryingToFind = allMasses[massIndex][indexToLookAt] + differenceBetweenTheorAndActual;
                        //Console.WriteLine("   theorMassThatTryingToFind: " + theorMassThatTryingToFind);
                        //Console.WriteLine("   theorMassThatTryingToFind.ToMz(chargeState): " + theorMassThatTryingToFind.ToMz(chargeState));
                        var closestPeakToTheorMass = GetClosestPeakIndex(theorMassThatTryingToFind.ToMz(chargeState));
                        var closestPeakmz = XArray[closestPeakToTheorMass.Value];
                        //Console.WriteLine("   closestPeakmz: " + closestPeakmz);
                        var closestPeakIntensity = YArray[closestPeakToTheorMass.Value];
                        if (Math.Abs(closestPeakmz.ToMass(chargeState) - theorMassThatTryingToFind) / theorMassThatTryingToFind * 1e6 <= deconvolutionTolerancePpm
                            && Peak2satisfiesRatio(allIntensities[massIndex][0], allIntensities[massIndex][indexToLookAt], candidateForMostIntensePeakIntensity, closestPeakIntensity, intensityRatioLimit)
                            && !listOfPeaks.Contains((closestPeakmz, closestPeakIntensity)))
                        {
                            //Found a match to an isotope peak for this charge state!
                            //Console.WriteLine(" *   Found a match to an isotope peak for this charge state!");
                            //Console.WriteLine(" *   chargeState: " + chargeState);
                            //Console.WriteLine(" *   closestPeakmz: " + closestPeakmz);
                            listOfPeaks.Add((closestPeakmz, closestPeakIntensity));
                            totalIntensity += closestPeakIntensity;
                            listOfRatios.Add(allIntensities[massIndex][indexToLookAt] / closestPeakIntensity);
                        }
                        else
                        {
                            break;
                        }
                    }

                    var extrapolatedMonoisotopicMass = testMostIntenseMass - diffToMonoisotopic[massIndex]; // Optimized for proteoforms!!
                    var lowestMass = listOfPeaks.Min(b => b.Item1).ToMass(chargeState); // But may actually observe this small peak
                    var monoisotopicMass = Math.Abs(extrapolatedMonoisotopicMass - lowestMass) < 0.5 ? lowestMass : extrapolatedMonoisotopicMass;

                    IsotopicEnvelope test = new IsotopicEnvelope(listOfPeaks, monoisotopicMass, chargeState, totalIntensity, MathNet.Numerics.Statistics.Statistics.StandardDeviation(listOfRatios), massIndex);

                    if (listOfPeaks.Count >= 2 && ScoreIsotopeEnvelope(test) > ScoreIsotopeEnvelope(bestIsotopeEnvelopeForThisPeak))
                    {
                        //Console.WriteLine("Better charge state is " + test.charge);
                        //Console.WriteLine("peaks: " + string.Join(",", listOfPeaks.Select(b => b.Item1)));
                        bestIsotopeEnvelopeForThisPeak = test;
                    }
                }

                if (bestIsotopeEnvelopeForThisPeak != null && bestIsotopeEnvelopeForThisPeak.peaks.Count >= 4)
                {
                    isolatedMassesAndCharges.Add(bestIsotopeEnvelopeForThisPeak);
                }
            }

            HashSet<double> seen = new HashSet<double>();
            foreach (var ok in isolatedMassesAndCharges.OrderByDescending(b => ScoreIsotopeEnvelope(b)))
            {
                if (seen.Overlaps(ok.peaks.Select(b => b.mz)))
                {
                    continue;
                }
                foreach (var ah in ok.peaks.Select(b => b.mz))
                {
                    seen.Add(ah);
                }
                yield return ok;
            }
        }

        public IEnumerable<int> ExtractIndices(double minX, double maxX)
        {
            int ind = Array.BinarySearch(XArray, minX);
            if (ind < 0)
            {
                ind = ~ind;
            }
            while (ind < Size && XArray[ind] <= maxX)
            {
                yield return ind;
                ind++;
            }
        }

        public int? GetClosestPeakIndex(double x)
        {
            if (Size == 0)
            {
                return null;
            }
            int index = Array.BinarySearch(XArray, x);
            if (index >= 0)
            {
                return index;
            }
            index = ~index;

            if (index >= Size)
            {
                return index - 1;
            }
            if (index == 0)
            {
                return index;
            }

            if (x - XArray[index - 1] > XArray[index] - x)
            {
                return index;
            }
            return index - 1;
        }

        public IEnumerable<MzPeak> FilterByNumberOfMostIntense(int topNPeaks)
        {
            var quantile = 1.0 - (double)topNPeaks / Size;
            quantile = Math.Max(0, quantile);
            quantile = Math.Min(1, quantile);
            double cutoffYvalue = YArray.Quantile(quantile);

            for (int i = 0; i < Size; i++)
            {
                if (YArray[i] >= cutoffYvalue)
                {
                    yield return GetPeak(i);
                }
            }
        }

        public IEnumerable<MzPeak> Extract(double minX, double maxX)
        {
            int ind = Array.BinarySearch(XArray, minX);
            if (ind < 0)
            {
                ind = ~ind;
            }
            while (ind < Size && XArray[ind] <= maxX)
            {
                yield return GetPeak(ind);
                ind++;
            }
        }

        private double ScoreIsotopeEnvelope(IsotopicEnvelope b)
        {
            if (b == null)
            {
                return 0;
            }
            return b.totalIntensity / Math.Pow(b.stDev, 0.13) * Math.Pow(b.peaks.Count, 0.4) / Math.Pow(b.charge, 0.06);
        }

        private bool Peak2satisfiesRatio(double peak1theorIntensity, double peak2theorIntensity, double peak1intensity, double peak2intensity, double intensityRatio)
        {
            var comparedShouldBe = peak1intensity / peak1theorIntensity * peak2theorIntensity;

            if (peak2intensity < comparedShouldBe / intensityRatio || peak2intensity > comparedShouldBe * intensityRatio)
            {
                return false;
            }
            return true;
        }

        private MzPeak GetPeak(int index)
        {
            if (peakList[index] == null)
            {
                peakList[index] = GeneratePeak(index);
            }
            return peakList[index];
        }

        private MzPeak GeneratePeak(int index)
        {
            return new MzPeak(XArray[index], YArray[index]);
        }

        private static double[] MassConvertToNeuCode(double[] masses, double neuCodeDiff, bool doNeucode)
        {
            if (!doNeucode)
            {
                return masses;
            }
            double[] massNeu = new double[masses.Length*2];
            for (int i = 0; i < masses.Length; i++)
            {
                massNeu[2 * i] = masses[i];
                massNeu[2 * i + 1] = masses[i] + neuCodeDiff;
            }
            return massNeu;
        }

        private static double[] IntenConvertToNeuCode(double[] intens, int fold, bool doNeucode)
        {
            if (!doNeucode)
            {
                return intens;
            }

            double[] intenNeu = new double[intens.Length * 2];
            for (int i = 0; i < intens.Length; i++)
            {
                intenNeu[2 * i] = intens[i];
                intenNeu[2 * i + 1] = intens[i]*fold;
            }
            return intenNeu;
        }
    }
}