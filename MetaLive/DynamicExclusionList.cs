using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MetaLive
{
    public class DynamicExclusionList
    {
        public Queue<Tuple<double, DateTime>> exclusionList { get; set; }

        public DynamicExclusionList()
        {
            exclusionList = new Queue<Tuple<double, DateTime>>();
        }

        public bool isNotInExclusionList(double value, double range)
        {
            foreach (var iv in exclusionList)
            {
                if (value - range <= iv.Item1 && value + range >= iv.Item1)
                {

                    Console.WriteLine("{0} Is In Exclusion List. Won't be place.", value);
                    return false;
                }
            }
            Console.WriteLine("{0} Is Not In Exclusion List. Will be placed.", value);
            return true;
        }

    }
}
