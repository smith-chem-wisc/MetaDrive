using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;

namespace MetaLive
{
    public class IsotopesForGlycoFeature
    {
        public Queue<Tuple<NeuCodeIsotopicEnvelop, DateTime>> isotopeList { get; set; }

        public IsotopesForGlycoFeature()
        {
            isotopeList = new Queue<Tuple<NeuCodeIsotopicEnvelop, DateTime>>();
        }
    }
}
