using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaLive
{
    public enum MethodTypes
    {
        Shutgun = 0,
        StaticBoxCar = 1,
        DynamicBoxCar = 2,
        GlycoFeature = 3     
    }

    public class Parameters
    {
        public GeneralSetting GeneralSetting { get; set; }
        public BoxCarScanSetting BoxCarScanSetting { get; set; }
        public FullScanSetting FullScanSetting { get; set; }
        public MS1IonSelecting MS1IonSelecting { get; set; }
        public MS2ScanSetting MS2ScanSetting { get; set; }
        public GlycoSetting GlycoSetting { get; set; }
    }

    public class GeneralSetting
    {
        public bool TestMod { get; set; }
        public MethodTypes MethodType { get; set; }
        public int TotalTimeInMinute { get; set; }
        public bool IsBottomUp { get; set; }
        public int Polarity { get; set; }
        public int SourceCID { get; set; }
        public int AGC_Mode { get; set; }
    }

    public class BoxCarScanSetting
    {
        public int BoxCarScans { get; set; }
        public int BoxCarBoxes { get; set; }
        public int BoxCarOverlap { get; set; }
        public int BoxCarMaxInjectTimeInMillisecond { get; set; }
        public int BoxCarResolution { get; set; }
        public int BoxCarAgcTarget { get; set; }
        public int BoxCarNormCharge { get; set; }
        public double BoxCarMzRangeLowBound { get; set; }
        public double BoxCarMzRangeHighBound { get; set; }
        public int BoxCarMicroScans { get; set; }
        public double DynamicBlockSize { get; set; }

        //TO DO: The BoxCar Ranges should be optimized based on real data
        public string[] BoxCarMsxInjectRanges
        {
            get
            {
                var msxInjectRanges = new string[BoxCarScans];
                double x = ((double)BoxCarMzRangeHighBound - (double)BoxCarMzRangeLowBound) / BoxCarBoxes;
                double y = x / BoxCarScans;
                for (int i = 0; i < BoxCarScans; i++)
                {
                    msxInjectRanges[i] = "[";
                    for (int j = 0; j < BoxCarBoxes; j++)
                    {
                        msxInjectRanges[i] += "(";
                        var lbox = BoxCarMzRangeLowBound + x * j + y * i - (double)BoxCarOverlap / 2;
                        if (j == 0)
                        {
                            lbox += (double)BoxCarOverlap / 2;
                        }
                        msxInjectRanges[i] += lbox.ToString("0.0");

                        msxInjectRanges[i] += ",";

                        var rbox = BoxCarMzRangeLowBound + x * j + y * i + y + (double)BoxCarOverlap / 2;
                        if (j == 0)
                        {
                            rbox += (double)BoxCarOverlap / 2;
                        }
                        msxInjectRanges[i] += rbox.ToString("0.0");

                        msxInjectRanges[i] += ")";
                        if (j != BoxCarBoxes - 1)
                        {
                            msxInjectRanges[i] += ",";
                        }
                    }
                    msxInjectRanges[i] += "]";
                }

                return msxInjectRanges;
            }

            set { }
        }

        public string BoxCarMsxInjectTargets
        {
            get
            {
                var msxInjectTarget = "[";
                for (int i = 0; i < BoxCarBoxes; i++)
                {
                    msxInjectTarget += BoxCarAgcTarget / BoxCarBoxes;
                    if (i != BoxCarBoxes - 1)
                    {
                        msxInjectTarget += ",";
                    }
                }
                msxInjectTarget += "]";
                return msxInjectTarget;
            }
        }

        public string BoxCarMsxInjectMaxITs
        {
            get
            {
                var msxInjectMaxITs= "[";
                for (int i = 0; i < BoxCarBoxes; i++)
                {
                    msxInjectMaxITs += BoxCarMaxInjectTimeInMillisecond / BoxCarBoxes;
                    if (i != BoxCarBoxes - 1)
                    {
                        msxInjectMaxITs += ",";
                    }
                }
                msxInjectMaxITs += "]";
                return msxInjectMaxITs;
            }
        }

        public Tuple<double, double, double>[] SelectRanges(double[] mz, double[] intensity)
        {
            List<double> filteredMz = new List<double>();
            List<double> filteredIntensity = new List<double>();
            List<double> filteredDistance = new List<double>();

            var theDistance = mz[1] - mz[0];

            int lowInd = Array.BinarySearch(mz, BoxCarMzRangeLowBound);
            if (lowInd < 0)
            {
                lowInd = ~lowInd;

                double firstIntensity = 0;
                double firstDistance = 0;
                if (lowInd > 0)
                {
                    firstIntensity = (mz[lowInd] - BoxCarMzRangeLowBound) / theDistance * intensity[lowInd - 1];
                    firstDistance = mz[lowInd] - BoxCarMzRangeLowBound;
                }
                else
                {
                    firstIntensity = intensity[0];
                    firstDistance = theDistance;
                }
                filteredMz.Add(BoxCarMzRangeLowBound);
                filteredIntensity.Add(firstIntensity);
                filteredDistance.Add(firstDistance);
            }


            int highInd = Array.BinarySearch(mz, BoxCarMzRangeHighBound);
            if (highInd < 0)
            {
                highInd = ~highInd;
            }

            for (int i = lowInd; i < highInd-1; i++)
            {
                filteredMz.Add(mz[i]);
                filteredIntensity.Add(intensity[i]);
                filteredDistance.Add(theDistance);
            }

            double lastIntensity = 0;
            double lastDistance = 0;
            if (BoxCarMzRangeHighBound - mz[highInd - 1] > theDistance)
            {
                lastIntensity = intensity[highInd - 1];
                lastDistance = theDistance;
            }
            else
            {
                lastIntensity = (BoxCarMzRangeHighBound - mz[highInd - 1]) / theDistance * intensity[highInd - 1];
                lastDistance = BoxCarMzRangeHighBound - mz[highInd - 1];
            }
            filteredMz.Add(mz[highInd - 1]);
            filteredIntensity.Add(lastIntensity);
            filteredDistance.Add(lastDistance);


            var tuples = new Tuple<double, double, double>[filteredMz.Count];
            for (int i = 0; i < filteredMz.Count; i++)
            {
                tuples[i] = new Tuple<double, double, double>(filteredMz[i], filteredIntensity[i], filteredDistance[i]);
            }
            return tuples;

        }

        public double[] CalculateMsxInjectRanges(Tuple<double, double, double>[] tuples)
        {
            double[] boxInd = new double[BoxCarBoxes*BoxCarScans];

            boxInd[0] = BoxCarMzRangeLowBound;           

            var totalIntensity = tuples.Select(p=>p.Item2).Sum();

            var singleIntensity = totalIntensity / (BoxCarBoxes*BoxCarScans);

            double currentIntensity = 0;
            int ind = 1;
            for (int i = 0; i < tuples.Length; i++)
            {
                if (ind > BoxCarBoxes * BoxCarScans - 1)
                {
                    break;
                }
                if (currentIntensity < singleIntensity)
                {
                    currentIntensity += tuples[i].Item2;
                    if (currentIntensity >= singleIntensity)
                    {
                        var percetage = ((currentIntensity - singleIntensity) / tuples[i].Item2) * tuples[i].Item3;
                        boxInd[ind] = tuples[i - 1].Item1 + 5.5 - percetage;
                        ind++;
                        currentIntensity = currentIntensity - singleIntensity;
                    }
                }           
            }

            return boxInd;
        }

        public string[] GenerateMsxInjectRanges(double[] boxInd)
        {
            var msxInjectRanges = new string[BoxCarScans];

            for (int i = 0; i < BoxCarScans; i++)
            {
                msxInjectRanges[i] = "[";
                for (int j = 0; j < BoxCarBoxes; j++)
                {
                    msxInjectRanges[i] += "(";
                    var lbox = boxInd[j*BoxCarScans + i] - (double)BoxCarOverlap / 2;
                    if (j == 0 && i==0)
                    {
                        lbox += (double)BoxCarOverlap / 2;
                    }
                    msxInjectRanges[i] += lbox.ToString("0.0");

                    msxInjectRanges[i] += ",";

                    double rbox = 0; 
                    if (j == BoxCarBoxes - 1 && i == BoxCarScans-1)
                    {
                        rbox = BoxCarMzRangeHighBound;
                    }
                    else
                    {
                        rbox = boxInd[j * BoxCarScans + i + 1] + (double)BoxCarOverlap / 2;
                    }
                    msxInjectRanges[i] += rbox.ToString("0.0");

                    msxInjectRanges[i] += ")";
                    if (j != BoxCarBoxes - 1)
                    {
                        msxInjectRanges[i] += ",";
                    }
                }
                msxInjectRanges[i] += "]";
            }

            return msxInjectRanges;
        }
    }

    public class FullScanSetting
    {
        public int MaxInjectTimeInMillisecond { get; set; }
        public int Resolution { get; set; }
        public int AgcTarget { get; set; }
        public int MzRangeLowBound { get; set; }
        public int MzRangeHighBound { get; set; }
        public int Microscans { get; set; }
    }

    public class MS1IonSelecting
    {
        public int TopN { get; set; }
        public bool DynamicExclusion { get; set; }
        public int ExclusionDuration { get; set; }
        public double IsolationWindow { get; set; }
        public int NormCharge { get; set; }
        public int MinCharge { get; set; }
        public int MaxCharge { get; set; }
        public int IntensityThreshold { get; set; }
    }

    public class MS2ScanSetting
    {
        public bool DoMS2 { get; set; }
        public int MS2MaxInjectTimeInMillisecond { get; set; }
        public int MS2Resolution { get; set; }
        public int NCE { get; set; }
        public string NCE_factors { get; set; }
        public int MS2AgcTarget { get; set; }
        public int MS2MzRangeLowBound { get; set; }
        public int MS2MzRangeHighBound { get; set; }
        public int MS2MicroScans { get; set; }
    }

    public class GlycoSetting
    {
        public int InclusionDuration { get; set; }
        public int TopN { get; set; }
    }
}