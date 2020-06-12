using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.Interfaces.InstrumentAccess_V1.Control.Scans;

namespace MetaDrive
{
    enum UserDefinedScanType
    {
        FullScan,
        DataDependentScan,
        BoxCarScan

    }

    class UserDefinedScan
    {
        public UserDefinedScan(UserDefinedScanType userDefinedScanType)
        {
            UserDefinedScanType = userDefinedScanType;
            dynamicBox = new List<double>();
        }

        public UserDefinedScanType UserDefinedScanType { get; }
        public List<double> dynamicBox { get; set; }
        public double Mz { get; set; }
    }
}
