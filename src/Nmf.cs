namespace GcmsPorovnanie;

/// <summary>Dekonvolúcia úseku vzorky nezápornou faktorizáciou X ≈ C·S (Lee–Seung, multiplikatívne pravidlá).</summary>
public static class Nmf
{
    /// <summary>X: n×m (riadky = skeny). Vráti C (n×k) a S (k×m) v poliach po riadkoch.</summary>
    public static (double[] C, double[] S) Faktorizuj(double[] X, int n, int m, int k, int seed, int it)
    {
        var rng = new Random(seed);
        var C = new double[n * k]; var S = new double[k * m];
        for (int i = 0; i < C.Length; i++) C[i] = rng.NextDouble() + 0.1;
        for (int i = 0; i < S.Length; i++) S[i] = rng.NextDouble() + 0.1;
        var CtX = new double[k * m]; var CtC = new double[k * k]; var CtCS = new double[k * m];
        var XSt = new double[n * k]; var SSt = new double[k * k]; var CSSt = new double[n * k];
        for (int iter = 0; iter < it; iter++)
        {
            // S *= (C^T X) / (C^T C S)
            Array.Clear(CtX); Array.Clear(CtC);
            for (int r = 0; r < n; r++)
            {
                int rx = r * m, rc = r * k;
                for (int a = 0; a < k; a++)
                {
                    double ca = C[rc + a]; if (ca == 0) continue;
                    int oa = a * m;
                    for (int j = 0; j < m; j++) CtX[oa + j] += ca * X[rx + j];
                    for (int b = 0; b < k; b++) CtC[a * k + b] += ca * C[rc + b];
                }
            }
            Array.Clear(CtCS);
            for (int a = 0; a < k; a++)
                for (int b = 0; b < k; b++)
                {
                    double v = CtC[a * k + b]; int oa = a * m, ob = b * m;
                    for (int j = 0; j < m; j++) CtCS[oa + j] += v * S[ob + j];
                }
            for (int q = 0; q < S.Length; q++) S[q] *= CtX[q] / (CtCS[q] + 1e-12);
            // C *= (X S^T) / (C S S^T)
            Array.Clear(SSt);
            for (int a = 0; a < k; a++)
                for (int b = 0; b < k; b++)
                {
                    double s = 0; int oa = a * m, ob = b * m;
                    for (int j = 0; j < m; j++) s += S[oa + j] * S[ob + j];
                    SSt[a * k + b] = s;
                }
            for (int r = 0; r < n; r++)
            {
                int rx = r * m, rc = r * k;
                for (int a = 0; a < k; a++)
                {
                    double s = 0; int oa = a * m;
                    for (int j = 0; j < m; j++) s += X[rx + j] * S[oa + j];
                    XSt[rc + a] = s;
                }
                for (int a = 0; a < k; a++)
                {
                    double s = 0;
                    for (int b = 0; b < k; b++) s += C[rc + b] * SSt[b * k + a];
                    CSSt[rc + a] = s;
                }
            }
            for (int q = 0; q < C.Length; q++) C[q] *= XSt[q] / (CSSt[q] + 1e-12);
        }
        return (C, S);
    }

    public static List<(int k, double cos, double posun, double vyska, double zvysok)> Dekonvolucia(
        double rt0, double[] refSpec, int kIon, double[] rtV, double[][] specV, Nastavenia o, int seedBase)
    {
        var outp = new List<(int, double, double, double, double)>();
        var rows = Enumerable.Range(0, rtV.Length).Where(q => rtV[q] >= rt0 - o.DekOkno && rtV[q] <= rt0 + o.DekOkno).ToArray();
        if (rows.Length < 5) return outp;
        int nbAll = refSpec.Length;
        var cols = Enumerable.Range(0, nbAll).Where(j => rows.Any(q => specV[q][j] > 0)).ToArray();
        if (cols.Length == 0) return outp;
        int n = rows.Length, m = cols.Length;
        var X = new double[n * m];
        for (int r = 0; r < n; r++) for (int j = 0; j < m; j++) X[r * m + j] = specV[rows[r]][cols[j]];
        double xn = Math.Sqrt(X.Sum(v => v * v));
        var rRef = cols.Select(j => refSpec[j]).ToArray();
        var rt = rows.Select(q => rtV[q]).ToArray();
        int kk = Array.IndexOf(cols, kIon);
        foreach (var k in o.DekK)
        {
            double bestErr = double.MaxValue; double[]? bC = null, bS = null;
            for (int sd = 0; sd < o.DekStarty; sd++)
            {
                var (C, S) = Faktorizuj(X, n, m, k, sd, o.DekIteracie);
                double e = 0;
                for (int r = 0; r < n; r++)
                    for (int j = 0; j < m; j++)
                    {
                        double v = 0; for (int a = 0; a < k; a++) v += C[r * k + a] * S[a * m + j];
                        double d = X[r * m + j] - v; e += d * d;
                    }
                e = Math.Sqrt(e) / xn;
                if (e < bestErr) { bestErr = e; bC = C; bS = S; }
            }
            (double c, double d, double h) cbest = (double.MinValue, 0, 0);
            for (int a = 0; a < k; a++)
            {
                var prof = new double[n]; for (int r = 0; r < n; r++) prof[r] = bC![r * k + a];
                int jm = 0; for (int r = 1; r < n; r++) if (prof[r] > prof[jm]) jm = r;
                double ap = Sig.Apex(rt, prof, jm);
                double h = kk >= 0 ? prof[jm] * bS![a * m + kk] : 0.0;
                var sa = new double[m]; for (int j = 0; j < m; j++) sa[j] = bS![a * m + j];
                var cand = (Sig.Cos(rRef, sa), ap - rt0, h);
                if (cand.CompareTo(cbest) > 0) cbest = cand;
            }
            outp.Add((k, cbest.c, cbest.d, cbest.h, bestErr));
        }
        return outp;
    }
}
