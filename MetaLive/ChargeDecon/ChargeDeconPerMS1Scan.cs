using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaLive
{
    public class ChargeDeconPerMS1Scan
    {
        public ChargeDeconPerMS1Scan(List<ChargeDeconEnvelope> chargeDeconEnvelopes)
        {
            ChargeDeconEnvelopes = chargeDeconEnvelopes;
        }

        public List<ChargeDeconEnvelope> ChargeDeconEnvelopes { get; set; } = new List<ChargeDeconEnvelope>();

        public int OneBasedScanNumber { get { return ChargeDeconEnvelopes.First().OneBasedScanNumber; } }

        public double RT { get { return ChargeDeconEnvelopes.First().RT; } }
    }
}
