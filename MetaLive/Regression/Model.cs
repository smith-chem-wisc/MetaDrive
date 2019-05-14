using System;
using System.Collections.ObjectModel;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace MetaDrawGUI
{
    internal abstract class Model
    {
        public abstract string Name { get; }

        public ReadOnlyCollection<string> ParameterNames
        {
            get { return this.parameterNames; }
        }

        private readonly ReadOnlyCollection<string> parameterNames;

        protected Model(string[] parameterNames)
        {
            this.parameterNames = new ReadOnlyCollection<string>(parameterNames);
        }

        public abstract void GetValue( double x, Vector<double> parameters, out double y);

        public abstract void GetGradient( double x, Vector<double> parameters, ref Vector<double> gradient);

        public void GetResidualVector(int pointCount, Vector<double> dataX, Vector<double> dataY, 
            Vector<double> parameters, ref Vector<double> residual)
        {
            double y;

            for (int j = 0; j < pointCount; j++)
            {
                GetValue( dataX[j], parameters, out y);
                residual[j] = (y - dataY[j]);
            }
        }

        public Vector<double> GetValueVector(int pointCount, Vector<double> dataX, Vector<double> parameters)
        {
            Vector<double> dataY = new DenseVector(pointCount);
            double y;
            for (int j = 0; j < pointCount; j++)
            {
                GetValue(dataX[j], parameters, out y);
                dataY[j] = y;
            }
            return dataY;
        }

        public Vector<double> GetPowerEValueVector(int pointCount, Vector<double> dataX, Vector<double> parameters)
        {
            Vector<double> dataYPowerE = new DenseVector(pointCount);
            double y;
            for (int j = 0; j < pointCount; j++)
            {
                GetValue(dataX[j], parameters, out y);
                dataYPowerE[j] = Math.Pow(Math.E, y);
            }
            return dataYPowerE;
        }

        public Vector<double> GetPowerETo1ValueVector(int pointCount, Vector<double> dataX, Vector<double> parameters)
        {
            double max;
            GetValue(parameters[0], parameters, out max);
            Vector<double> dataYPowerE = new DenseVector(pointCount);
            double y;
            for (int j = 0; j < pointCount; j++)
            {
                GetValue(dataX[j], parameters, out y);
                dataYPowerE[j] = Math.Pow(Math.E, y) / Math.Pow(Math.E, max);
            }
            return dataYPowerE;
        }

        public Vector<double> GetPowerEResidualVector(int pointCount, Vector<double> dataX, Vector<double> dataY, Vector<double> parameters)
        {
            Vector<double> PowerEResidual = new DenseVector(pointCount);
            double y;
            for (int j = 0; j < pointCount; j++)
            {
                GetValue(dataX[j], parameters, out y);
                PowerEResidual[j] = Math.Pow(Math.E, y) - dataY[j];
            }
            return PowerEResidual;
        }

        public double GetPowerEMSE(int pointCount, Vector<double> dataX, Vector<double> dataY, Vector<double> parameters)
        {
            double mse = 0;
            double y;
            for (int j = 0; j < pointCount; j++)
            {
                GetValue(dataX[j], parameters, out y);
                mse += Math.Pow((dataY[j] - Math.Pow(Math.E, y)), 2);
            }
            return mse/pointCount;
        }

        public Vector<double> LogTransform(Vector<double> dataY)
        {
            Vector<double> dataYlog = new DenseVector(dataY.Count);
            for (int i = 0; i < dataY.Count; i++)
            {
                dataYlog[i] = Math.Log(dataY[i]);
            }
            return dataYlog;
        }

    }
}
