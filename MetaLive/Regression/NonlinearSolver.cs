using System;
using System.Collections.Generic;

using MathNet.Numerics.LinearAlgebra;

namespace MetaDrawGUI
{
    internal abstract class NonlinearSolver : Solver
    {
        public abstract void Estimate( Model model, SolverOptions solverOptions, int pointCount, 
            Vector<double> dataX, Vector<double> dataY, ref List<Vector<double>> iterations);

        protected static bool ShouldTerminate( double valueCurrent,  double valueNew, int iterationCount,  
            Vector<double> parametersCurrent, Vector<double> parametersNew,  SolverOptions solverOptions)
        {
            return (
                       Math.Abs(valueNew - valueCurrent) <= solverOptions.MinimumDeltaValue ||
                       parametersNew.Subtract(parametersCurrent).Norm(2.0) <= solverOptions.MinimumDeltaParameters ||
                       iterationCount >= solverOptions.MaximumIterations);
        }
    }
}
