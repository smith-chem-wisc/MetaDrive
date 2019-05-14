using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MetaDrawGUI;

namespace MetaLive
{
    public class ChargeDeconEnvelope
    {
        //private List<IsotopicEnvelope> isotopicEnvelopes;

        public ChargeDeconEnvelope(int oneBasedScanNumber, double rt, List<IsotopicEnvelope> iso, List<double?> selectedMs2s)
        {
            
            SelectedMs2s = new List<double?>();
            OneBasedScanNumber = oneBasedScanNumber;
            RT = rt;
            isotopicEnvelopes = iso;
            chargeStates = new List<int>();
            foreach (var item in isotopicEnvelopes)
            {
                chargeStates.Add(item.charge);
                findSelectedMs2InEnvelopes(item, selectedMs2s);
            }

            chargeStatesFit = new double[isotopicEnvelopes.Count];
            intensitiesFit = new double[isotopicEnvelopes.Count];
            mzFit = new double[isotopicEnvelopes.Count];
            for (int i = 0; i < isotopicEnvelopes.Count; i++)
            {
                chargeStatesFit[i] = isotopicEnvelopes[i].charge;
                intensitiesFit[i] = isotopicEnvelopes[i].totalIntensity;
                mzFit[i] = isotopicEnvelopes[i].peaks.Average(p => p.mz);
            }
            var intensitiesMax = intensitiesFit.Max();
            for (int i = 0; i < isotopicEnvelopes.Count; i++)
            {
                intensitiesFit[i] = intensitiesFit[i]/intensitiesMax;
            }
            
            //Array.Sort(mzFit, intensitiesFit);
            //Array.Sort(chargeStatesFit);
            double[] intenses = new double[isotopicEnvelopes.Count];
            double mse = new double();
            GetMSE(out mse,out intenses);
            intensitiesModel = intenses;
            MSE = mse;
        }

        public List<IsotopicEnvelope> isotopicEnvelopes { get; set; }

        public double isotopicMass { get { return isotopicEnvelopes.First().monoisotopicMass; } }

        public int numOfEnvelopes { get { return isotopicEnvelopes.Count; } }

        public double RT { get; }

        public List<int> chargeStates{ get; }

        public int OneBasedScanNumber { get; }

        public List<double?> SelectedMs2s { get; }

        public double[] chargeStatesFit { get; }

        public double[] intensitiesFit { get; }

        public double[] intensitiesModel { get; }

        public double[] mzFit { get; }

        public double MSE { get; }

        private void findSelectedMs2InEnvelopes(IsotopicEnvelope isotopicEnvelope, List<double?> selectedMs2s)
        {

                foreach (var Ms2 in selectedMs2s)
                {
                    //There may have a bug if the selected ms2 is not in the envelop, peaks here is not ordered from low to high
                    if ( Ms2 >= isotopicEnvelope.peaks.Min(p=>p.mz) && Ms2 <= isotopicEnvelope.peaks.Max(p=>p.mz))
                    {
                        SelectedMs2s.Add(Ms2);
                    }
                }

        }

        private void GetMSE(out double outMse, out double[] outIntensitiesModel)
        {
            var model = new GaussianModel();
            var solver = new LevenbergMarquardtSolver();            
            Vector<double> dataX = new DenseVector(chargeStatesFit);
            Vector<double> dataY = new DenseVector(intensitiesFit);
            List<Vector<double>> iterations = new List<Vector<double>>();
            int pointCount = dataX.Count;
            var solverOptions = new SolverOptions(true, 0.0001, 0.0001, 1000, new DenseVector(new[] { 10.0, 10.0 }));
            NonlinearSolver nonlinearSolver = (solver as NonlinearSolver);
            nonlinearSolver.Estimate(model, solverOptions, pointCount, dataX, model.LogTransform(dataY), ref iterations);
            outIntensitiesModel = model.GetPowerETo1ValueVector(pointCount, dataX, iterations[iterations.Count - 1]).ToArray();
            outMse = model.GetPowerEMSE(pointCount, dataX, dataY, iterations[iterations.Count - 1]);
        }

    }
}
