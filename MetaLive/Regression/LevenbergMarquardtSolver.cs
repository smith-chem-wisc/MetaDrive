using System.Collections.Generic;

using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra;

namespace MetaDrawGUI
{
    internal sealed class LevenbergMarquardtSolver : NonlinearSolver
    {
        private const double LambdaInitial = 0.001;
        private const double LambdaFactor = 10.0;

        public override void Estimate( Model model, SolverOptions solverOptions, int pointCount, 
            Vector<double> dataX, Vector<double> dataY, ref List<Vector<double>> iterations)
        {
            int n = solverOptions.Guess.Count;
            double lambda = LambdaInitial;

            Vector<double> parametersCurrent = new DenseVector(solverOptions.Guess.ToArray());
            Vector<double> parametersNew = new DenseVector(n);

            double valueCurrent;
            double valueNew;

            GetObjectiveValue( model, pointCount, dataX, dataY, parametersCurrent, out valueCurrent);

            while (true)
            {
                Matrix<double> jacobian = new DenseMatrix(pointCount, n);
                Vector<double> residual = new DenseVector(pointCount);

                GetObjectiveJacobian( model, pointCount, dataX, dataY, parametersCurrent, ref jacobian);

                model.GetResidualVector( pointCount, dataX, dataY, parametersCurrent, ref residual);

                // compute approximate Hessian
                Matrix<double> hessian = jacobian.Transpose().Multiply(jacobian);

                // create diagonal matrix for proper scaling
                Matrix<double> diagonal = new DiagonalMatrix(n, n, hessian.Diagonal().ToArray());

                // compute Levenberg-Marquardt step
                Vector<double> step = (hessian.Add(diagonal.Multiply(lambda))).Cholesky().Solve(jacobian.Transpose().Multiply(residual));

                // update estimated model parameters
                parametersCurrent.Subtract(step, parametersNew);

                GetObjectiveValue( model, pointCount, dataX, dataY, parametersNew, out valueNew);

                iterations.Add(new DenseVector(parametersNew.ToArray()));

                if (ShouldTerminate( valueCurrent, valueNew, iterations.Count, parametersCurrent, parametersNew, solverOptions))
                {
                    break;
                }

                if (valueNew < valueCurrent)
                {
                    // the step have decreased objective function value - decrease lambda
                    lambda = (lambda / LambdaFactor);

                    parametersNew.CopyTo(parametersCurrent);
                    valueCurrent = valueNew;
                }
                else
                {
                    // the step have not decreated objective function value - increase lambda
                    lambda = (lambda * LambdaFactor);
                }
            }
        }
    }
}
