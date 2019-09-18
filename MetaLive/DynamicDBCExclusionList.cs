using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;

namespace MetaLive
{
    public class DynamicDBCExclusionList
    {
        //Tuple<Mass, charge, time>
        public Queue<DynamicDBCValue> DBCExclusionList { get; set; }

        public DynamicDBCExclusionList()
        {
            DBCExclusionList = new Queue<DynamicDBCValue>();
        }

        public int MatchExclusionList(double[] mzs, double range)
        {
            for (int i = 0; i < DBCExclusionList.Count; i++)
            {
                if (Match(mzs, DBCExclusionList.ElementAt(i).Mzs, range))
                {
                    DBCExclusionList.ElementAt(i).PlaceCount++;
                    return DBCExclusionList.ElementAt(i).PlaceCount;
                }
            }
            return 0;
        }

        private bool Match(double[] mzs, double[] exclusionMzs, double range)
        {
            int total = 0;

            foreach (var mz in mzs)
            {
                var ind = ChargeDecon.GetCloestIndex(mz, exclusionMzs);
                if (mz <= exclusionMzs[ind] + range && mz >= exclusionMzs[ind] - range)
                {
                    total++;
                }
            }
            if (total > mzs.Length / 2)
            {
                return true;
            }

            return false;
        }
    }

    public class DynamicDBCValue
    {
        public DynamicDBCValue(double[] mzs, int placeCount, DateTime dateTime)
        {
            Mzs = mzs;
            PlaceCount = placeCount;
            DateTime = dateTime;
        }

        public double[] Mzs { get; set; }

        public int PlaceCount { get; set; }

        public DateTime DateTime { get; set; }
    }
}
