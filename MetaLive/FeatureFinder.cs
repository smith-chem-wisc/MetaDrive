using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using MzLibUtil;
using Chemistry;

namespace MetaLive
{
    public static class FeatureFinder
    {
        //For simplicity, the A 291.09542 may not be considered.
        static double[] SugarMass = new double[10] { -406.15874, -365.13219, -203.07937, -162.05282, -146.05791, 146.05791, 162.05282, 203.07937, 365.13219, 406.15874 };
        static Tolerance tolerance = new PpmTolerance(10);

        private static int GetCloestIndex(double x, double[] array)
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

        //To use this function, the input neuCodeIsotopicEnvelops has to be ordered already by monoisotopicMass
        public static List<NeuCodeIsotopicEnvelop> ExtractGlycoMS1features(NeuCodeIsotopicEnvelop[] neuCodeIsotopicEnvelops)
        {
            List<NeuCodeIsotopicEnvelop> isotopicEnvelops = new List<NeuCodeIsotopicEnvelop>();
            //Dictionary<double, int> glycanCandidates = new Dictionary<double, int>();
            Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();  //key is ind, value is count of matched
 
            if (neuCodeIsotopicEnvelops.Length == 0)
            {
                return isotopicEnvelops;
            }

            var masses = neuCodeIsotopicEnvelops.Select(p => p.monoisotopicMass).ToArray();

            //Parallel doesn't help.
            for (int i = 0; i < masses.Length; i++)
            {
                if (masses[i] < 2000)
                {
                    continue;
                }
                var families = SugarMass.Select(p => masses[i] + p).ToArray();

                List<int> matchedInd = new List<int>();

                matchedInd.Add(i);

                foreach (var fm in families)
                {
                    var ind = GetCloestIndex(fm, masses);
               
                    if (tolerance.Within(fm, masses[ind]))
                    {
                        matchedInd.Add(ind);
                    }
     
                }

                if (matchedInd.Count() >= 3)
                {
                    foreach (var m in matchedInd)
                    {
                        if (!keyValuePairs.ContainsKey(m))
                        {
                            keyValuePairs.Add(m, 1);
                        }
                        else
                        {
                            keyValuePairs[m]++;
                        }
                    }
                }

            }

            foreach (var item in keyValuePairs)
            {
                //If the isotope is from current scan.
                if (neuCodeIsotopicEnvelops[item.Key].FromCurrentScan) 
                {
                    neuCodeIsotopicEnvelops[item.Key].MatchedFamilyCount = item.Value;
                    isotopicEnvelops.Add(neuCodeIsotopicEnvelops[item.Key]);
                }
            }

            return isotopicEnvelops;
        }

    }
}
