namespace GcmsPorovnanie;

/// <summary>Numerické pomôcky – správanie zodpovedá numpy/scipy (np.interp, scipy.ndimage reflect, scipy.signal.find_peaks).</summary>
public static class Sig
{
    // index s okrajom "reflect" (d c b a | a b c d | d c b a) ako scipy.ndimage
    static int Refl(int i, int n)
    {
        if (n == 1) return 0;
        int p = 2 * n;
        i %= p; if (i < 0) i += p;
        return i < n ? i : p - 1 - i;
    }

    public static double[] MinFilter(double[] x, int size)
    {
        int n = x.Length, h = size / 2; var r = new double[n];
        // pre pole s okrajmi použijeme posuvné okno s dequeom nad rozšíreným poľom
        int m = n + 2 * h; var ext = new double[m];
        for (int i = 0; i < m; i++) ext[i] = x[Refl(i - h, n)];
        var dq = new int[m]; int head = 0, tail = 0;
        for (int i = 0; i < m; i++)
        {
            while (tail > head && ext[dq[tail - 1]] >= ext[i]) tail--;
            dq[tail++] = i;
            if (dq[head] <= i - size) head++;
            if (i >= size - 1) r[i - size + 1] = ext[dq[head]];
        }
        return r;
    }

    public static double[] MeanFilter(double[] x, int size)
    {
        int n = x.Length, h = size / 2; var r = new double[n];
        int m = n + 2 * h; var cs = new double[m + 1];
        for (int i = 0; i < m; i++) cs[i + 1] = cs[i] + x[Refl(i - h, n)];
        for (int i = 0; i < n; i++) r[i] = (cs[i + size] - cs[i]) / size;
        return r;
    }

    public static double[] Baseline(double[] y, int w = 81) => MeanFilter(MinFilter(y, w), w);

    /// <summary>y - pozadie (bez orezania).</summary>
    public static double[] MinusBaseline(double[] y, int w = 81)
    {
        var b = Baseline(y, w); var r = new double[y.Length];
        for (int i = 0; i < y.Length; i++) r[i] = y[i] - b[i];
        return r;
    }

    /// <summary>Spektrá (skeny × m/z) po odčítaní pozadia v každom m/z kanáli, záporné hodnoty = 0.</summary>
    public static double[][] SpecMinusBaseline(double[][] spec, int w = 81)
    {
        int n = spec.Length; if (n == 0) return spec;
        int nb = spec[0].Length;
        var r = new double[n][];
        for (int s = 0; s < n; s++) r[s] = new double[nb];
        Parallel.For(0, nb, j =>
        {
            var col = new double[n];
            for (int s = 0; s < n; s++) col[s] = spec[s][j];
            var b = Baseline(col, w);
            for (int s = 0; s < n; s++) { var v = col[s] - b[s]; r[s][j] = v > 0 ? v : 0; }
        });
        return r;
    }

    public static double Median(IEnumerable<double> v)
    {
        var a = v.ToArray(); if (a.Length == 0) return double.NaN;
        Array.Sort(a); int n = a.Length;
        return n % 2 == 1 ? a[n / 2] : 0.5 * (a[n / 2 - 1] + a[n / 2]);
    }

    /// <summary>np.interp: mimo rozsahu vráti krajné hodnoty (alebo left/right ak sú zadané).</summary>
    public static double Interp(double x, double[] xp, double[] fp, double? left = null, double? right = null)
    {
        int n = xp.Length;
        if (x < xp[0]) return left ?? fp[0];
        if (x > xp[n - 1]) return right ?? fp[n - 1];
        if (x == xp[n - 1]) return fp[n - 1];
        int lo = 0, hi = n - 1;
        while (hi - lo > 1) { int mid = (lo + hi) >> 1; if (xp[mid] <= x) lo = mid; else hi = mid; }
        double t = (x - xp[lo]) / (xp[hi] - xp[lo]);
        return fp[lo] + t * (fp[hi] - fp[lo]);
    }

    /// <summary>Interpolácia riadkov matice M v čase t; null mimo rozsahu.</summary>
    public static double[]? InterpRows(double t, double[] rt, double[][] M)
    {
        if (t < rt[0] || t > rt[^1]) return null;
        int n = rt.Length;
        // searchsorted(rt, t) - 1  (left)
        int lo = 0, hi = n;
        while (lo < hi) { int mid = (lo + hi) >> 1; if (rt[mid] < t) lo = mid + 1; else hi = mid; }
        int j = Math.Clamp(lo - 1, 0, n - 2);
        double w = (t - rt[j]) / (rt[j + 1] - rt[j]);
        var a = M[j]; var b = M[j + 1]; var r = new double[a.Length];
        for (int k = 0; k < a.Length; k++) r[k] = (1 - w) * a[k] + w * b[k];
        return r;
    }

    /// <summary>Spresnenie vrcholu parabolou cez 3 body (čas v s).</summary>
    public static double Apex(double[] rt, double[] y, int i)
    {
        if (i > 0 && i < y.Length - 1)
        {
            double a = y[i - 1], b = y[i], c = y[i + 1];
            double den = a - 2 * b + c;
            if (den < 0)
            {
                double d = 0.5 * (a - c) / den;
                double x = i + d;
                int k = (int)Math.Floor(x); k = Math.Clamp(k, 0, rt.Length - 2);
                return rt[k] + (x - k) * (rt[k + 1] - rt[k]);
            }
        }
        return rt[i];
    }

    public static double Cos(double[] a, double[] b)
    {
        double ab = 0, aa = 0, bb = 0;
        for (int i = 0; i < a.Length; i++) { ab += a[i] * b[i]; aa += a[i] * a[i]; bb += b[i] * b[i]; }
        return aa > 0 && bb > 0 ? ab / (Math.Sqrt(aa) * Math.Sqrt(bb)) : 0.0;
    }

    /// <summary>Pearsonova korelácia; NaN ak je rozptyl nulový.</summary>
    public static double Corr(double[] x, double[] y)
    {
        int n = x.Length; double mx = 0, my = 0;
        for (int i = 0; i < n; i++) { mx += x[i]; my += y[i]; }
        mx /= n; my /= n;
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; sxy += a * b; sxx += a * a; syy += b * b; }
        return sxy / Math.Sqrt(sxx * syy);
    }

    public static double Std(double[] x)
    {
        double m = x.Average(); double s = 0; foreach (var v in x) s += (v - m) * (v - m);
        return Math.Sqrt(s / x.Length);
    }

    /// <summary>scipy.signal.find_peaks s podmienkou prominence (min) a voliteľne width (min, rel_height 0,5).</summary>
    public static List<int> FindPeaks(double[] x, double prominence, double? width = null)
    {
        int n = x.Length;
        var peaks = new List<int>();
        int i = 1, iMax = n - 1;
        while (i < iMax)
        {
            if (x[i - 1] < x[i])
            {
                int ia = i + 1;
                while (ia < iMax && x[ia] == x[i]) ia++;
                if (x[ia] < x[i])
                {
                    int left = i, right = ia - 1;
                    peaks.Add((left + right) / 2);
                    i = ia;
                }
            }
            i++;
        }
        var outp = new List<int>();
        foreach (var p in peaks)
        {
            // prominencia
            double lmin = x[p]; int lb = p; int k = p;
            while (0 <= k && x[k] <= x[p]) { if (x[k] < lmin) { lmin = x[k]; lb = k; } k--; }
            double rmin = x[p]; int rb = p; k = p;
            while (k <= n - 1 && x[k] <= x[p]) { if (x[k] < rmin) { rmin = x[k]; rb = k; } k++; }
            double prom = x[p] - Math.Max(lmin, rmin);
            if (prom < prominence) continue;
            if (width.HasValue)
            {
                double hgt = x[p] - prom * 0.5;
                k = p; while (lb < k && hgt < x[k]) k--;
                double lip = k; if (x[k] < hgt) lip += (hgt - x[k]) / (x[k + 1] - x[k]);
                k = p; while (k < rb && hgt < x[k]) k++;
                double rip = k; if (x[k] < hgt) rip -= (hgt - x[k]) / (x[k - 1] - x[k]);
                if (rip - lip < width.Value) continue;
            }
            outp.Add(p);
        }
        return outp;
    }
}
