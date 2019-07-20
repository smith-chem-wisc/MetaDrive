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

        //To use this function, the input neuCodeIsotopicEnvelops has to be ordered already by monoisotopicMass
        public static Dictionary<double, int> ExtractGlycoMS1features(NeuCodeIsotopicEnvelop[] neuCodeIsotopicEnvelops)
        {
            Dictionary<double, int> glycanCandidates = new Dictionary<double, int>();

            if (neuCodeIsotopicEnvelops.Length == 0)
            {
                return glycanCandidates;
            }

            var masses = neuCodeIsotopicEnvelops.Select(p => p.monoisotopicMass).ToArray();

            //Parallel doesn't help.
            for (int i = 0; i < masses.Length; i++)
            {
                var families = SugarMass.Select(p => masses[i] + p).ToArray();

                List<int> matchedInd = new List<int>();

                matchedInd.Add(i);

                foreach (var fm in families)
                {
                    var ind = Array.BinarySearch(masses, fm);
                    if (ind < 0)
                    {
                        ind = ~ind;
                    }

                    if (ind < masses.Length && tolerance.Within(fm, masses[ind]))
                    {
                        matchedInd.Add(ind);
                    }
                    else if (ind > 0 && tolerance.Within(fm, masses[ind - 1]))
                    {
                        matchedInd.Add(ind - 1);
                    }
                }

                if (matchedInd.Count() >= 3)
                {
                    foreach (var m in matchedInd)
                    {
                        if (!glycanCandidates.ContainsKey(masses[m]))
                        {
                            glycanCandidates.Add(masses[m], neuCodeIsotopicEnvelops[m].charge);
                        }
                    }
                }
            }

            return glycanCandidates;
        }

    }
}
