using System;

using MathNet.Numerics.LinearAlgebra;

namespace MetaDrawGUI
{
    internal sealed class GaussianModel : NonlinearModel
    {

        public override string Name
        {
            get { return "y = 1/(squrt(2*pi*b^2) * e^((-(x-a)^2 / (2*b^2))"; }
        }

        public GaussianModel()
            : base(new[] { "a", "b" })
        {
        }

        public override void GetValue( double x, Vector<double> parameters, out double y)
        {
            //y = 1 / Math.Sqrt(2 * Math.PI * Math.Pow( parameters[1], 2)) * Math.Pow( Math.E, ( -Math.Pow(x - parameters[0], 2) / (2 * Math.Pow( parameters[1],2))));

            //log-likelihood
            y = -0.5 * (Math.Log(2 * Math.PI) + Math.Log(parameters[1]) + 1 / parameters[1] * Math.Pow(x - parameters[0], 2));
        }

        public override void GetGradient(double x, Vector<double> parameters, ref Vector<double> gradient)
        {
            //log-likelihood
            gradient[0] = 1 / parameters[1] * (x - parameters[0]);
            gradient[1] = -0.5 * parameters[1] * (1 - 1 / parameters[1] * Math.Pow(x - parameters[0], 2));
        }
    }
}
