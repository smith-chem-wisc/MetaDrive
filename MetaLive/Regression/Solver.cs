using System;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra;

namespace MetaDrawGUI
{
    class Solver
    {
        protected static void GetObjectiveValue( Model model, int pointCount, Vector<double> dataX, 
            Vector<double> dataY, Vector<double> parameters, out double value)
        {
            value = 0.0;

            double y = 0.0;

            for (int j = 0; j < pointCount; j++)
            {
                model.GetValue( dataX[j], parameters, out y);

                value += Math.Pow( y - dataY[j],  2.0);
            }

            value *= 0.5;
        }

        protected void GetObjectiveJacobian( Model model, int pointCount, Vector<double> dataX, 
            Vector<double> dataY, Vector<double> parameters, ref Matrix<double> jacobian)
        {
            int parameterCount = parameters.Count;

            // fill rows of the Jacobian matrix
            // j-th row of a Jacobian is the gradient of model function in j-th measurement
            for (int j = 0; j < pointCount; j++)
            {
                Vector<double> gradient = new DenseVector(parameterCount);

                model.GetGradient( dataX[j], parameters, ref gradient);

                jacobian.SetRow(j, gradient);
            }
        }
    }
}
