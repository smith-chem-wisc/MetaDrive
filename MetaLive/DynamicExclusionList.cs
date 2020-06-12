using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MetaDrive
{
    public class DynamicExclusionList
    {
        //private SinglePpmAroundZeroSearchMode _exclusionPpmTolerance = new SinglePpmAroundZeroSearchMode(20);

        //Tuple<Mass, charge, time>
        public Queue<Tuple<double, int, DateTime>> exclusionList { get; set; }

        public DynamicExclusionList()
        {
            exclusionList = new Queue<Tuple<double, int, DateTime>>();
        }

        public bool isNotInExclusionList(double value, double range)
        {
            foreach (var iv in exclusionList)
            {
                if (value - range <= iv.Item1 && value + range >= iv.Item1)
                //(_exclusionPpmTolerance.Accepts(value, iv.Item1)>=0)
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
