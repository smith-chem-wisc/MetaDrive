using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using MzLibUtil;

namespace MetaLive
{
    public class IsotopesForGlycoFeature
    {
        Tolerance tolerance = new PpmTolerance(5);
        //bool is duplicated with isotopes from current scan.
        public Queue<Tuple<NeuCodeIsotopicEnvelop, DateTime>> isotopeList { get; set; }

        public IsotopesForGlycoFeature()
        {
            isotopeList = new Queue<Tuple<NeuCodeIsotopicEnvelop, DateTime>>();
        }

        public void AddIsotopeIntoList(NeuCodeIsotopicEnvelop[] currentIsotopes, DateTime dateTime)
        {
            for (int i = 0; i < isotopeList.Count; i++)
            {
                isotopeList.ElementAt(i).Item1.FromCurrentScan = false;
            }

            foreach (var isotop in currentIsotopes)
            {
                for (int i = 0; i < isotopeList.Count; i++)
                {
                    if (tolerance.Within(isotopeList.ElementAt(i).Item1.monoisotopicMass, isotop.monoisotopicMass))
                    {
                        isotopeList.ElementAt(i).Item1.AlreadyExist = true;
                    }
                }

                isotopeList.Enqueue(new Tuple<NeuCodeIsotopicEnvelop, DateTime>(isotop, dateTime));
            }
        }
    }
}
