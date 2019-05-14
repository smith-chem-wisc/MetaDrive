using MathNet.Numerics.LinearAlgebra;

namespace MetaDrawGUI
{
    internal struct SolverOptions
    {
        private readonly bool useInternalSolver;
        private readonly double minimumDeltaValue;
        private readonly double minimumDeltaParameters;
        private readonly int maximumIterations;
        private readonly Vector<double> guess;


        public SolverOptions( bool useInternalSolver, double minimumDeltaValue, 
            double minimumDeltaParameters, int maximumIterations, Vector<double> guess)
        {
            this.useInternalSolver = useInternalSolver;
            this.minimumDeltaValue = minimumDeltaValue;
            this.minimumDeltaParameters = minimumDeltaParameters;
            this.maximumIterations = maximumIterations;
            this.guess = guess;
        }


        public bool UseInternalSolver
        {
            get
            {
                return this.useInternalSolver;
            }
        }

        public double MinimumDeltaValue
        {
            get
            {
                return this.minimumDeltaValue;
            }
        }

        public double MinimumDeltaParameters
        {
            get
            {
                return this.minimumDeltaParameters;
            }
        }

        public int MaximumIterations
        {
            get
            {
                return this.maximumIterations;
            }
        }

        public Vector<double> Guess
        {
            get
            {
                return this.guess;
            }
        }

    }
}
