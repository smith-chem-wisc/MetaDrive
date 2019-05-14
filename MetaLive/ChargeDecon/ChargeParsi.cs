using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaLive
{
    public class ChargeParsi
    {
        public List<int> ExsitedMS1Scans { get { return chargeDeconEnvelopes.Select(p => p.OneBasedScanNumber).ToList(); } }

        public List<ChargeDeconEnvelope> chargeDeconEnvelopes { get; set; } = new List<ChargeDeconEnvelope>();

        public int MS2ScansCount { get { return chargeDeconEnvelopes.Sum(p => p.SelectedMs2s.Count); } }

        public List<double> RTlist { get { return chargeDeconEnvelopes.Select(p => p.RT).ToList(); } }
    }
}
