using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace MetaLive
{
    public class ChargeDeconEnvelope
    {
        public ChargeDeconEnvelope(int oneBasedScanNumber, List<IsotopicEnvelope> iso)
        {         
            isotopicEnvelopes = iso;

            chargeStates = new List<int>();
            foreach (var item in isotopicEnvelopes)
            {
                chargeStates.Add(item.charge);
            }

        }

        public List<IsotopicEnvelope> isotopicEnvelopes { get; set; }

        public List<int> chargeStates{ get; }
    }
}
