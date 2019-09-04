using MzLibUtil;
using System;
using System.Collections.Generic;
using System.Linq;


namespace MassSpectrometry
{
    public class MzSpectrumXY
    {

        public double[] XArray { get; private set; }
        public double[] YArray { get; private set; }

        public MzSpectrumXY(double[] mz, double[] intensities, bool shouldCopy)
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
        }

        #region Properties

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

        public double TotalIntensity { get { return YArray.Sum(); } }

        #endregion

        #region Basic Method

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

        //TO DO: speed up this function. 
        public IEnumerable<int> ExtractIndicesByY()
        {
            var YArrayCopy = new double[Size];
            Array.Copy(YArray, YArrayCopy, Size);
            var sorted = YArrayCopy.Select((x, i) => new KeyValuePair<double, int>(x, i)).OrderBy(x => x.Key).ToList();

            int z = Size - 1;
            while (z >= 0)
            {
                yield return sorted.Select(x => x.Value).ElementAt(z);
                z--;
            }
        }

        public static IEnumerable<int> ExtractIndicesByY_old(double[] Y)
        {
            var YArrayIndex = Enumerable.Range(0, Y.Length).ToArray();
            var YArrayCopy = new double[Y.Length];
            Array.Copy(Y, YArrayCopy, Y.Length);
            Array.Sort(YArrayCopy, YArrayIndex);
            int z = Y.Length - 1;
            while (z >= 0)
            {
                yield return YArrayIndex[z];
                z--;
            }
        }

        public static IEnumerable<int> ExtractIndicesByY_new(double[] Y)
        {
            var YArrayCopy = new double[Y.Length];
            Array.Copy(Y, YArrayCopy, Y.Length);

            var sorted = YArrayCopy.Select((x, i) => new KeyValuePair<double, int>(x, i)).OrderBy(x => x.Key);

            int z = Y.Length - 1;
            while (z >= 0)
            {
                yield return sorted.Select(x => x.Value).ElementAt(z);
                z--;
            }
        }

        #endregion

    }
}