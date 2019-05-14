using Chemistry;
using MathNet.Numerics.Statistics;
using MzLibUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MetaLive;
using EngineLayer;

namespace MassSpectrometry
{
    public class MzSpectrumBU
    {
        private const int numAveraginesToGenerate = 1500;
        private static readonly double[][] allMasses = new double[numAveraginesToGenerate][];
        private static readonly double[][] allIntensities = new double[numAveraginesToGenerate][];
        private static readonly double[] mostIntenseMasses = new double[numAveraginesToGenerate];
        private static readonly double[] diffToMonoisotopic = new double[numAveraginesToGenerate];

        private MzPeak[] peakList;
        private int? indexOfpeakWithHighestY;
        private double? sumOfAllY;

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
                    var masses =  ye.Masses.ToArray();
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

        public MzSpectrumBU(double[,] mzintensities)
        {
            var count = mzintensities.GetLength(1);

            XArray = new double[count];
            YArray = new double[count];
            Buffer.BlockCopy(mzintensities, 0, XArray, 0, sizeof(double) * count);
            Buffer.BlockCopy(mzintensities, sizeof(double) * count, YArray, 0, sizeof(double) * count);
            peakList = new MzPeak[Size];
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

        public int? IndexOfPeakWithHighesetY
        {
            get
            {
                if (Size == 0)
                {
                    return null;
                }
                if (!indexOfpeakWithHighestY.HasValue)
                {
                    indexOfpeakWithHighestY = Array.IndexOf(YArray, YArray.Max());
                }
                return indexOfpeakWithHighestY.Value;
            }
        }

        public double? YofPeakWithHighestY
        {
            get
            {
                if (Size == 0)
                {
                    return null;
                }
                return YArray[IndexOfPeakWithHighesetY.Value];
            }
        }

        public double? XofPeakWithHighestY
        {
            get
            {
                if (Size == 0)
                {
                    return null;
                }
                return XArray[IndexOfPeakWithHighesetY.Value];
            }
        }

        public double SumOfAllY
        {
            get
            {
                if (!sumOfAllY.HasValue)
                {
                    sumOfAllY = YArray.Sum();
                }
                return sumOfAllY.Value;
            }
        }

        public static byte[] Get64Bitarray(IEnumerable<double> array)
        {
            var mem = new MemoryStream();
            foreach (var okk in array)
            {
                byte[] ok = BitConverter.GetBytes(okk);
                mem.Write(ok, 0, ok.Length);
            }
            mem.Position = 0;
            return mem.ToArray();
        }

        public byte[] Get64BitYarray()
        {
            return Get64Bitarray(YArray);
        }

        public byte[] Get64BitXarray()
        {
            return Get64Bitarray(XArray);
        }

        public override string ToString()
        {
            return string.Format("{0} (Peaks {1})", Range, Size);
        }

        // Mass tolerance must account for different isotope spacing!
        public IEnumerable<NeuCodeIsotopicEnvelop> DeconvoluteBU(MzRange theRange, DeconvolutionParameter deconvolutionParameter)
        {
            if (Size == 0)
            {
                yield break;
            }

            var isolatedMassesAndCharges = new List<NeuCodeIsotopicEnvelop>();

            HashSet<double> seenPeaks = new HashSet<double>();
            int cut = 50;

            ////Deconvolution by Intensity decending order
            foreach (var candidateForMostIntensePeak in ExtractIndicesByY())
            ////Deconvolution by MZ increasing order
            //foreach (var candidateForMostIntensePeak in ExtractIndices(theRange.Minimum, theRange.Maximum))
            {
                var candidateForMostIntensePeakMz = XArray[candidateForMostIntensePeak];

                if (seenPeaks.Contains(candidateForMostIntensePeakMz))
                {
                    continue;
                }
                //Console.WriteLine("candidateForMostIntensePeakMz: " + candidateForMostIntensePeakMz);
                var candidateForMostIntensePeakIntensity = YArray[candidateForMostIntensePeak];
                NeuCodeIsotopicEnvelop bestIsotopeEnvelopeForThisPeak = null;
                //TO DO: Find possible chargeState.

                //TO Do: Test this code.
                List<int> allPossibleChargeState = new List<int>();
                for (int i = candidateForMostIntensePeak + 1; i < XArray.Length; i++)
                {
                    if (XArray[i] - candidateForMostIntensePeakMz > 0.01 && XArray[i] - candidateForMostIntensePeakMz < 0.8)
                    {
                        var chargeDouble = 1 / (XArray[i] - candidateForMostIntensePeakMz);
                        int charge = Convert.ToInt32(chargeDouble);
                        if (Math.Abs(chargeDouble - charge) <= 0.2 && charge >= deconvolutionParameter.DeconvolutionMinAssumedChargeState && charge <= deconvolutionParameter.DeconvolutionMaxAssumedChargeState)
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

                    var test = GetEnvelopForThisPeak(candidateForMostIntensePeakMz, candidateForMostIntensePeakIntensity, massIndex, chargeState, deconvolutionParameter);

                    if (test.peaks.Count >= 2 && test.stDev < 0.00001 && ScoreIsotopeEnvelope(test) > ScoreIsotopeEnvelope(bestIsotopeEnvelopeForThisPeak))
                    {
                        //Console.WriteLine("Better charge state is " + test.charge);
                        //Console.WriteLine("peaks: " + string.Join(",", listOfPeaks.Select(b => b.Item1)));
                        bestIsotopeEnvelopeForThisPeak = test;
                    }
                }



                if (bestIsotopeEnvelopeForThisPeak != null && bestIsotopeEnvelopeForThisPeak.peaks.Count >= 2)
                {                   

                    var pairEnvelop =  GetNeucodeEnvelopForThisEnvelop(bestIsotopeEnvelopeForThisPeak, deconvolutionParameter);
                    isolatedMassesAndCharges.Add(bestIsotopeEnvelopeForThisPeak);
                    foreach (var peak in bestIsotopeEnvelopeForThisPeak.peaks.Select(p => p.mz))
                    {
                        seenPeaks.Add(peak);
                    }

                    if (pairEnvelop != null)
                    {
                        isolatedMassesAndCharges.Add(pairEnvelop);

                        foreach (var peak in pairEnvelop.peaks.Select(p => p.mz))
                        {
                            seenPeaks.Add(peak);
                        }
                    }
                                 
                }
                if (isolatedMassesAndCharges.Count > cut*2)
                {
                    break;
                }
            }


            //foreach (var ok in isolatedMassesAndCharges)
            //{
            //    yield return ok;
            //}

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

        public IEnumerable<NeuCodeIsotopicEnvelop> Deconvolute(MzRange theRange, DeconvolutionParameter deconvolutionParameter)
        {
            if (Size == 0)
            {
                yield break;
            }

            var isolatedMassesAndCharges = new List<NeuCodeIsotopicEnvelop>();

            HashSet<double> seenPeaks = new HashSet<double>();

            ////Deconvolution by Intensity decending order
            //foreach (var candidateForMostIntensePeak in ExtractIndicesByY())
            ////Deconvolution by MZ increasing order
            foreach (var candidateForMostIntensePeak in ExtractIndices(theRange.Minimum, theRange.Maximum))
            {
                var candidateForMostIntensePeakMz = XArray[candidateForMostIntensePeak];

                if (seenPeaks.Contains(candidateForMostIntensePeakMz))
                {
                    continue;
                }
                //Console.WriteLine("candidateForMostIntensePeakMz: " + candidateForMostIntensePeakMz);
                var candidateForMostIntensePeakIntensity = YArray[candidateForMostIntensePeak];
                NeuCodeIsotopicEnvelop bestIsotopeEnvelopeForThisPeak = null;
                //TO DO: Find possible chargeState.

                //TO Do: Test this code.
                List<int> allPossibleChargeState = new List<int>();
                for (int i = candidateForMostIntensePeak + 1; i < XArray.Length; i++)
                {
                    if (XArray[i] - candidateForMostIntensePeakMz > 0.01 && XArray[i] - candidateForMostIntensePeakMz < 0.8)
                    {
                        var chargeDouble = 1 / (XArray[i] - candidateForMostIntensePeakMz);
                        int charge = Convert.ToInt32(chargeDouble);
                        if (Math.Abs(chargeDouble - charge) <= 0.2 && charge >= deconvolutionParameter.DeconvolutionMinAssumedChargeState && charge <= deconvolutionParameter.DeconvolutionMaxAssumedChargeState)
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

                    var test = GetEnvelopForThisPeak(candidateForMostIntensePeakMz, candidateForMostIntensePeakIntensity, massIndex, chargeState, deconvolutionParameter);

                    if (test.peaks.Count >= 2 && test.stDev < 0.00001 && ScoreIsotopeEnvelope(test) > ScoreIsotopeEnvelope(bestIsotopeEnvelopeForThisPeak))
                    {
                        //Console.WriteLine("Better charge state is " + test.charge);
                        //Console.WriteLine("peaks: " + string.Join(",", listOfPeaks.Select(b => b.Item1)));
                        bestIsotopeEnvelopeForThisPeak = test;
                    }
                }

                if (bestIsotopeEnvelopeForThisPeak != null && bestIsotopeEnvelopeForThisPeak.peaks.Count >= 2)
                {
                    isolatedMassesAndCharges.Add(bestIsotopeEnvelopeForThisPeak);
                    foreach (var peak in bestIsotopeEnvelopeForThisPeak.peaks.Select(p => p.mz))
                    {
                        seenPeaks.Add(peak);
                    }
                }
            }


            //foreach (var ok in isolatedMassesAndCharges)
            //{
            //    yield return ok;
            //}

            HashSet<double> seen = new HashSet<double>(); //Do we still need this

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

        //Parallel the Deconvolution
        public IEnumerable<NeuCodeIsotopicEnvelop> ParallelDeconvolute(MzRange theRange, DeconvolutionParameter deconvolutionParameter, int core)
        {
            List<MzRange> mzRanges = new List<MzRange>();

            double piece = (theRange.Maximum - theRange.Minimum) / core;

            List<NeuCodeIsotopicEnvelop> neuCodeIsotopicEnvelops = new List<NeuCodeIsotopicEnvelop>();

            for (int i = 0; i < core ; i++)
            {
                MzRange mzRange = new MzRange(theRange.Minimum + i*piece - 2.5, theRange.Minimum + (i+1)*piece + 2.5); //TO DO: Determine the overlap
                mzRanges.Add(mzRange);
            }

            Object obj = new Object();

            Parallel.ForEach(mzRanges, range =>
            {
                var x = Deconvolute(range, deconvolutionParameter).ToList();
                lock (obj)
                {
                    neuCodeIsotopicEnvelops.AddRange(x);
                }
            });

            foreach (var ok in neuCodeIsotopicEnvelops)
            {
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

        public IEnumerable<int> ExtractIndicesByY()
        {
            var YArrayIndex = Enumerable.Range(0, Size).ToArray();
            var YArrayCopy = new double[Size];
            Array.Copy(YArray, YArrayCopy, Size );
            Array.Sort(YArrayCopy, YArrayIndex);
            Array.Reverse(YArrayIndex);
            int i = 0;
            while (i < Size - 1)
            {             
                yield return YArrayIndex[i];
                i++;
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

        public void ReplaceXbyApplyingFunction(Func<MzPeak, double> convertor)
        {
            for (int i = 0; i < Size; i++)
            {
                XArray[i] = convertor(GetPeak(i));
            }
            peakList = new MzPeak[Size];
        }

        public virtual double[,] CopyTo2DArray()
        {
            double[,] data = new double[2, Size];
            const int size = sizeof(double);
            Buffer.BlockCopy(XArray, 0, data, 0, size * Size);
            Buffer.BlockCopy(YArray, 0, data, size * Size, size * Size);
            return data;
        }

        public double? GetClosestPeakXvalue(double x)
        {
            if (Size == 0)
            {
                return null;
            }
            return XArray[GetClosestPeakIndex(x).Value];
        }

        public int NumPeaksWithinRange(double minX, double maxX)
        {
            int startingIndex = Array.BinarySearch(XArray, minX);
            if (startingIndex < 0)
            {
                startingIndex = ~startingIndex;
            }
            if (startingIndex >= Size)
            {
                return 0;
            }
            int endIndex = Array.BinarySearch(XArray, maxX);
            if (endIndex < 0)
            {
                endIndex = ~endIndex;
            }
            if (endIndex == 0)
            {
                return 0;
            }

            return endIndex - startingIndex;
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

        public IEnumerable<MzPeak> Extract(DoubleRange xRange)
        {
            return Extract(xRange.Minimum, xRange.Maximum);
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

        public IEnumerable<MzPeak> FilterByY(double minY, double maxY)
        {
            for (int i = 0; i < Size; i++)
            {
                if (YArray[i] >= minY && YArray[i] <= maxY)
                {
                    yield return GetPeak(i);
                }
            }
        }

        public IEnumerable<MzPeak> FilterByY(DoubleRange yRange)
        {
            return FilterByY(yRange.Minimum, yRange.Maximum);
        }

        public double CalculateDotProductSimilarity(MzSpectrum spectrumToCompare, Tolerance tolerance)
        {
            //get arrays of m/zs and intensities
            double[] mz1 = XArray;
            double[] intensity1 = YArray;

            double[] mz2 = spectrumToCompare.XArray;
            double[] intensity2 = spectrumToCompare.YArray;

            //convert spectra to vectors
            List<double> vector1 = new List<double>();
            List<double> vector2 = new List<double>();
            int i = 0; //iterate through mz1
            int j = 0; //iterate through mz2

            //find where peaks match 
            while (i != mz1.Length && j != mz2.Length)
            {
                double one = mz1[i];
                double two = mz2[j];
                if (tolerance.Within(one, two)) //if match
                {
                    vector1.Add(intensity1[i]);
                    vector2.Add(intensity2[j]);
                    i++;
                    j++;
                }
                else if (one > two)
                {
                    vector1.Add(0);
                    vector2.Add(intensity2[j]);
                    j++;
                }
                else //two>one
                {
                    vector1.Add(intensity1[i]);
                    vector2.Add(0);
                    i++;
                }
            }
            //wrap up leftover peaks
            for (; i < mz1.Length; i++)
            {
                vector1.Add(intensity1[i]);
                vector2.Add(0);
            }
            for (; j < mz2.Length; j++)
            {
                vector1.Add(0);
                vector2.Add(intensity2[j]);
            }

            //numerator of dot product
            double numerator = 0;
            for (i = 0; i < vector1.Count; i++)
            {
                numerator += vector1[i] * vector2[i];
            }

            //denominator of dot product
            double denominator = Math.Sqrt(vector1.Sum(x => x * x)) * Math.Sqrt(vector2.Sum(x => x * x));

            //return dot product
            return numerator / denominator;
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

        public List<ChargeDeconEnvelope> ChargeDeconvolution(int OneBasedScanNumber, double rt, List<NeuCodeIsotopicEnvelop> isotopicEnvelopes, List<double?> selectedMs2)
        {
            List<ChargeDeconEnvelope> chargeDeconEnvelopes = new List<ChargeDeconEnvelope>();
            SingleAbsoluteAroundZeroSearchMode massAccept = new SingleAbsoluteAroundZeroSearchMode(2.2);
            SinglePpmAroundZeroSearchMode massAcceptForNotch = new SinglePpmAroundZeroSearchMode(10);
            int i = 0;
            bool conditioner = true;
            while (conditioner)
            {
                if (i < isotopicEnvelopes.Count)
                {
                    var chargeDecon = new List<IsotopicEnvelope>();
                    chargeDecon.Add(isotopicEnvelopes[i]);

                    //The j here need to be break in a better way
                    for (int j = 1; j < 20; j++)
                    {
                        //Decide envelopes are from same mass or not, need better algorithm
                        //if (i + j < isotopicEnvelopes.Count
                        //    && massAccept.Accepts(isotopicEnvelopes[j + i].monoisotopicMass, isotopicEnvelopes[i].monoisotopicMass) == 0
                        //    && !chargeDecon.Exists(p => p.charge == isotopicEnvelopes[j + i].charge))
                        if (i + j < isotopicEnvelopes.Count && NotchTolerance(isotopicEnvelopes[i].monoisotopicMass, isotopicEnvelopes[j + i].monoisotopicMass, massAcceptForNotch))
                        {
                            if (!chargeDecon.Exists(p => p.charge == isotopicEnvelopes[j + i].charge))
                            {
                                chargeDecon.Add(isotopicEnvelopes[i + j]);
                            }
                        }
                        else
                        {
                            i = i + j;
                            break;
                        }
                    }
                    //Decide the charge deconvolution distribution, need better algorithm
                    if (chargeDecon.Count >= 3)
                    {
                        chargeDeconEnvelopes.Add(new ChargeDeconEnvelope(OneBasedScanNumber, rt, chargeDecon, selectedMs2));
                    }
                }
                else
                {
                    conditioner = false;
                }
            }
            return chargeDeconEnvelopes;
        }

        private bool NotchTolerance(double theMass1, double theMass2, SinglePpmAroundZeroSearchMode massAccept)
        {
            if (massAccept.Accepts(theMass1, theMass2) == 0 || massAccept.Accepts(theMass1 + 1, theMass2) == 0
                || massAccept.Accepts(theMass1 + 2, theMass2) == 0 || massAccept.Accepts(theMass1 + 3, theMass2) == 0
                || massAccept.Accepts(theMass1 + 4, theMass2) == 0)
            {
                return true;
            }
            return false;
        }
 
        private NeuCodeIsotopicEnvelop GetEnvelopForThisPeak(double candidateForMostIntensePeakMz, double candidateForMostIntensePeakIntensity, int massIndex, int chargeState, DeconvolutionParameter deconvolutionParameter)
        {
            var listOfPeaks = new List<(double, double)> { (candidateForMostIntensePeakMz, candidateForMostIntensePeakIntensity) };
            var listOfRatios = new List<double> { allIntensities[massIndex][0] / candidateForMostIntensePeakIntensity };

            // Assuming the test peak is most intense...
            // Try to find the rest of the isotopes!


            double differenceBetweenTheorAndActual = candidateForMostIntensePeakMz.ToMass(chargeState) - mostIntenseMasses[massIndex];
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
                if (Math.Abs(closestPeakmz.ToMass(chargeState) - theorMassThatTryingToFind) / theorMassThatTryingToFind * 1e6 <= deconvolutionParameter.DeconvolutionMassTolerance.Value
                    && Peak2satisfiesRatio(allIntensities[massIndex][0], allIntensities[massIndex][indexToLookAt], candidateForMostIntensePeakIntensity, closestPeakIntensity, deconvolutionParameter.DeconvolutionIntensityRatio)
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

            var extrapolatedMonoisotopicMass = candidateForMostIntensePeakMz.ToMass(chargeState) - diffToMonoisotopic[massIndex]; // Optimized for proteoforms!!
            var lowestMass = listOfPeaks.Min(b => b.Item1).ToMass(chargeState); // But may actually observe this small peak
            var monoisotopicMass = Math.Abs(extrapolatedMonoisotopicMass - lowestMass) < 0.5 ? lowestMass : extrapolatedMonoisotopicMass;

            var TheoryMasses = allMasses[massIndex].Select(p => p = p + differenceBetweenTheorAndActual).ToArray();
            var TheoryIntensities = allIntensities[massIndex];
            Array.Sort(TheoryMasses, TheoryIntensities);

            NeuCodeIsotopicEnvelop test = new NeuCodeIsotopicEnvelop(listOfPeaks, monoisotopicMass, chargeState, totalIntensity, MathNet.Numerics.Statistics.Statistics.StandardDeviation(listOfRatios), massIndex);

            return test;
        }

        private void BestMonoisotopicMassForThisEnvelop(NeuCodeIsotopicEnvelop BestIsotopicEnvelop, DeconvolutionParameter deconvolutionParameter)
        {
            //if this envelop have multiple peaks, which means that it is highly likely to have a confused most intense peak.
            if (BestIsotopicEnvelop.peaks.Count >= 3)
            {
                for (int i = 1; i < 3; i++)
                {
                    var test = GetEnvelopForThisPeak(BestIsotopicEnvelop.peaks[i].mz, BestIsotopicEnvelop.peaks[i].intensity, BestIsotopicEnvelop.massIndex, BestIsotopicEnvelop.charge, deconvolutionParameter);
                    if (ScoreIsotopeEnvelope(test) > ScoreIsotopeEnvelope(BestIsotopicEnvelop))
                    {
                        BestIsotopicEnvelop = test;
                    }

                }
            }

        }

        private NeuCodeIsotopicEnvelop GetNeucodeEnvelopForThisEnvelop(NeuCodeIsotopicEnvelop BestIsotopicEnvelop, DeconvolutionParameter deconvolutionParameter)
        {
            NeuCodeIsotopicEnvelop neuCodeIsotopicEnvelop = null;

            var MostIntensePeakMz = BestIsotopicEnvelop.peaks.First().mz;

            List<int> range = new List<int>();
            var j = 0-deconvolutionParameter.MaxmiumNeuCodeNumber;
            while (j <= deconvolutionParameter.MaxmiumNeuCodeNumber)
            {
                if (j != 0)
                {
                    range.Add(j);
                }
                j++;
            }

            foreach (var i in range)
            {
                var NeuCodeMostIntesePeakMz = MostIntensePeakMz + deconvolutionParameter.NeuCodeMassDefect * i / BestIsotopicEnvelop.charge/1000;

                var closestPeakIndex = GetClosestPeakIndex(NeuCodeMostIntesePeakMz);
                var closestPeakmz = XArray[closestPeakIndex.Value];
                var closestPeakIntensity = YArray[closestPeakIndex.Value];
                if (closestPeakmz != MostIntensePeakMz && Math.Abs(closestPeakmz.ToMass(BestIsotopicEnvelop.charge) - NeuCodeMostIntesePeakMz.ToMass(BestIsotopicEnvelop.charge)) / NeuCodeMostIntesePeakMz.ToMass(BestIsotopicEnvelop.charge) * 1e6 <= deconvolutionParameter.DeconvolutionMassTolerance.Value)
                {
                    var test = GetEnvelopForThisPeak(closestPeakmz, closestPeakIntensity, BestIsotopicEnvelop.massIndex, BestIsotopicEnvelop.charge, deconvolutionParameter);
                    if (ScoreIsotopeEnvelope(test) > ScoreIsotopeEnvelope(neuCodeIsotopicEnvelop))
                    {                     
                        neuCodeIsotopicEnvelop = test;
                        BestIsotopicEnvelop.IsNeuCode = true;
                        neuCodeIsotopicEnvelop.IsNeuCode = true;
                    }
                }

            }

            return neuCodeIsotopicEnvelop;
        }

    }
}