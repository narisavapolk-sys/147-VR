using System;

namespace PoolPhysics.Tests
{
    /// <summary>
    /// Compact bounded CMA-ES (Covariance Matrix Adaptation Evolution Strategy)
    /// following Hansen's tutorial algorithm. Pure C#, deterministic (seeded RNG),
    /// no external deps. Optimizes in a normalized [0,1]^n box (each coordinate
    /// scaled to its physical bound) so parameters of very different magnitudes
    /// (friction ~0.05 vs spinDecay ~4) are comparably conditioned.
    /// </summary>
    public sealed class CmaEs
    {
        readonly Random _rnd;
        public CmaEs(int seed = 20261026) { _rnd = new Random(seed); }

        double _spare; bool _hasSpare;
        double Randn()
        {
            if (_hasSpare) { _hasSpare = false; return _spare; }
            double u1 = 1.0 - _rnd.NextDouble(), u2 = _rnd.NextDouble();
            double r = Math.Sqrt(-2.0 * Math.Log(u1));
            _spare = r * Math.Sin(2 * Math.PI * u2); _hasSpare = true;
            return r * Math.Cos(2 * Math.PI * u2);
        }

        public static double[] Cholesky(double[,] a, int n)
        {
            // returns lower-triangular L packed row-major [n*n]; a symmetric PSD
            double[,] L = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j <= i; j++)
                {
                    double s = a[i, j];
                    for (int k = 0; k < j; k++) s -= L[i, k] * L[j, k];
                    if (i == j) L[i, i] = Math.Sqrt(Math.Max(1e-12, s));
                    else L[i, j] = s / L[j, j];
                }
            double[] outp = new double[n * n];
            for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) outp[i * n + j] = L[i, j];
            return outp;
        }

        /// <summary>Minimize f over [lower,upper]. Returns best real-space x and its value.</summary>
        public (double[] bestX, double bestF) Minimize(
            Func<double[], double> f, double[] lower, double[] upper, double[] x0,
            int maxEvals = 400, int popsize = 0, double sigma0 = 0.1)
        {
            int n = x0.Length;
            double[] m = new double[n];
            for (int i = 0; i < n; i++) m[i] = (x0[i] - lower[i]) / (upper[i] - lower[i]);

            int lambda = popsize > 0 ? popsize : (int)(4 + 3 * Math.Log(n));
            int mu = lambda / 2;
            double[] w = new double[mu]; double sumw = 0, mueff;
            for (int i = 0; i < mu; i++) { w[i] = Math.Log((lambda + 1) / 2.0) - Math.Log(i + 1); sumw += w[i]; }
            for (int i = 0; i < mu; i++) w[i] /= sumw;
            double tmp = 0; for (int i = 0; i < mu; i++) tmp += w[i] * w[i];
            mueff = 1.0 / tmp;

            double cs = (mueff + 2) / (n + mueff + 5);
            double ds = 1 + 2 * Math.Max(0, Math.Sqrt((mueff - 1) / (n + 1.0)) - 1) + cs;
            double cc = (4 + mueff / n) / (n + 4 + 2 * mueff / n);
            double c1 = 2 / ((n + 1.3) * (n + 1.3) + mueff);
            double cmu = Math.Min(1 - c1, 2 * (mueff - 2 + 1 / mueff) / ((n + 2) * (n + 2) + 2 * mueff));
            double chiN = Math.Sqrt(n) * (1 - 1.0 / (4.0 * n) + 1.0 / (21.0 * n * n));

            double sigma = sigma0;
            double[,] C = new double[n, n];
            for (int i = 0; i < n; i++) C[i, i] = 1.0;
            double[] ps = new double[n], pc = new double[n];

            double bestF = double.PositiveInfinity;
            double[] bestX = ToReal(m, lower, upper);
            int evals = 0;

            while (evals < maxEvals)
            {
                double[,] L = new double[n, n];
                double[] Lp = Cholesky(C, n);
                for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) L[i, j] = Lp[i * n + j];

                double[][] y = new double[lambda][];
                double[][] xre = new double[lambda][];
                double[] fx = new double[lambda];
                for (int k = 0; k < lambda; k++)
                {
                    double[] z = new double[n]; for (int i = 0; i < n; i++) z[i] = Randn();
                    double[] yk = new double[n];
                    for (int i = 0; i < n; i++)
                    {
                        double s = 0; for (int j = 0; j <= i; j++) s += L[i, j] * z[j];
                        yk[i] = m[i] + sigma * s;
                    }
                    y[k] = yk;
                    xre[k] = ToReal(Clamp01(yk), lower, upper);
                    fx[k] = f(xre[k]); evals++;
                    if (fx[k] < bestF) { bestF = fx[k]; bestX = (double[])xre[k].Clone(); }
                }

                // sort indices by f ascending
                int[] idx = new int[lambda]; for (int i = 0; i < lambda; i++) idx[i] = i;
                Array.Sort(idx, (a, b) => fx[a].CompareTo(fx[b]));

                double[] yw = new double[n];
                for (int i = 0; i < n; i++)
                {
                    double s = 0; for (int j = 0; j < mu; j++) s += w[j] * y[idx[j]][i];
                    yw[i] = s;
                }
                double[] oldM = (double[])m.Clone();

                // mean update
                for (int i = 0; i < n; i++) m[i] = yw[i];
                for (int i = 0; i < n; i++) m[i] = Math.Min(1.0, Math.Max(0.0, m[i])); // keep mean in box

                // evolution paths
                double[] CinvSqrt_yw = new double[n]; // here C approx identity-ish early; use L^-1
                // solve L * u = yw (approx C^-1/2 * yw for diagonal-ish C) — use full via cholesky of C
                double[] u = SolveLower(L, yw, n);
                for (int i = 0; i < n; i++)
                {
                    ps[i] = (1 - cs) * ps[i] + Math.Sqrt(cs * (2 - cs) * mueff) * u[i];
                }
                double hsig = (Norm(ps) / Math.Sqrt(1 - Math.Pow(1 - cs, 2 * (evals / (double)lambda + 1)) * chiN) < 1.4 + 2.0 / (n + 1)) ? 1 : 0;
                for (int i = 0; i < n; i++)
                {
                    pc[i] = (1 - cc) * pc[i] + hsig * Math.Sqrt(cc * (2 - cc) * mueff) * (m[i] - oldM[i]) / sigma;
                }

                // covariance update
                double[] dy = new double[n];
                for (int i = 0; i < n; i++) dy[i] = (m[i] - oldM[i]) / sigma;
                for (int i = 0; i < n; i++) for (int j = 0; j < n; j++)
                {
                    double rankmu = 0;
                    for (int k = 0; k < mu; k++)
                        rankmu += w[k] * y[idx[k]][i] * y[idx[k]][j];
                    rankmu /= (sigma * sigma);
                    C[i, j] = (1 - c1 - cmu) * C[i, j]
                            + c1 * (pc[i] * pc[j] + (1 - hsig) * cc * (2 - cc) * C[i, j])
                            + cmu * rankmu;
                }

                // sigma path adaptation
                sigma *= Math.Exp((cs / ds) * (Norm(ps) / chiN - 1));
                sigma = Math.Min(sigma, 0.3);
            }
            return (bestX, bestF);
        }

        static double[] Clamp01(double[] v) { double[] r = new double[v.Length]; for (int i = 0; i < v.Length; i++) r[i] = Math.Min(1, Math.Max(0, v[i])); return r; }
        static double[] ToReal(double[] norm, double[] lo, double[] up)
        {
            double[] r = new double[norm.Length];
            for (int i = 0; i < norm.Length; i++) r[i] = lo[i] + norm[i] * (up[i] - lo[i]);
            return r;
        }
        static double Norm(double[] v) { double s = 0; foreach (var x in v) s += x * x; return Math.Sqrt(s); }
        static double[] SolveLower(double[,] L, double[] b, int n)
        {
            double[] x = new double[n];
            for (int i = 0; i < n; i++) { double s = b[i]; for (int j = 0; j < i; j++) s -= L[i, j] * x[j]; x[i] = s / L[i, i]; }
            return x;
        }
    }
}
