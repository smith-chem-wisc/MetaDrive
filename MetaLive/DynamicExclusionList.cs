using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaLive
{
    class DynamicExclusionList
    {
        public Queue<double> exclusionList { get; set; }

        public DynamicExclusionList()
        {
            exclusionList = new Queue<double>();
        }

        public void exclustionListDynamicChange(object o, System.Timers.ElapsedEventArgs e)
        {
            this.exclusionList.Dequeue();
            Console.WriteLine("{0:HH:mm:ss,fff} ExclusionListDequeue.", DateTime.Now);
        }

        public bool isNotInExclusionList(double value, double range)
        {
            foreach (var iv in exclusionList)
            {
                if (value - range <= iv && value + range >= iv)
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
