using System.Globalization;
using System.Text.RegularExpressions;

namespace GcmsPorovnanie;

/// <summary>Jeden záznam knižnice EI spektier (.msp).</summary>
public sealed class Zaznam
{
    public string Nazov = "";
    public string Kluc = "";          // normalizovaný názov látky (bez D/L, TMS, minor/major…)
    public string Kategoria = "iné";  // kyselina / aminokyselina / iné
    public double Ri = double.NaN;    // retenčný index (Kováts)
    public double RtMin = double.NaN; // retenčný čas (min) – len vlastné knižnice
    public string Zdroj = "";         // súbor knižnice (+ pôvod spektra z COMMENT zdroj=…)
    public string Subor = "";
    public int[] Mz = Array.Empty<int>();
    public double[] I = Array.Empty<double>(); // normované na 999
    public bool Vlastna => !double.IsNaN(RtMin);
}

/// <summary>Načítanie knižníc .msp a vyhľadávanie spektier (kosínus, intenzity^0,5).</summary>
public sealed class Kniznica
{
    public readonly List<Zaznam> Zaznamy = new();
    public readonly List<string> Subory = new();
    static readonly Dictionary<string, (DateTime cas, List<Zaznam> z)> Cache = new();

    static readonly Regex ReAmino = new(
        @"glycine|alanine|valine|leucine|serine|threonine|proline|glutamine|asparagine|lysine|methionine|phenylalanine|tyrosine|" +
        @"tryptophan|histidine|arginine|ornithine|cysteine|glutamic acid|aspartic acid|pyroglutamic|amino", RegexOptions.Compiled);

    public static string Kluc(string name)
    {
        var n = name.Split(';')[0].Trim().ToLowerInvariant();
        n = Regex.Replace(n, @"\s*\((\d+|n|x)?\s*tms[^)]*\)|\s*\(\d*meox[^)]*\)", "");
        n = Regex.Replace(n, @"[-, ]+(di|tri|tetra|penta|bis|tris)?-?(tms|trimethylsilyl)( ether| ester| derivative)?$", "");
        n = Regex.Replace(n, @"^(dl-|d-|l-|\(s\)-|\(r\)-|\(\+/-\)-|\(\+\)-|\(-\)-)", "");
        n = Regex.Replace(n, @"\s+(minor|major)\d*$|\s+\d+$", "");
        return n.Trim();
    }

    public static string Kategoria(string kluc)
    {
        bool kys = kluc.Contains("acid") || (kluc.EndsWith("ate") && !kluc.Contains("phosphate"));
        if (ReAmino.IsMatch(kluc) && !kluc.StartsWith("n-acetyl")) return "aminokyselina";
        return kys ? "kyselina" : "iné";
    }

    public void Pridaj(string cesta, string? zobrazeneMeno = null)
    {
        var meno = zobrazeneMeno ?? Path.GetFileName(cesta);
        var fi = new FileInfo(cesta);
        List<Zaznam> z;
        lock (Cache)
        {
            if (Cache.TryGetValue(fi.FullName, out var c) && c.cas == fi.LastWriteTimeUtc) z = c.z;
            else { z = Citaj(cesta, meno); Cache[fi.FullName] = (fi.LastWriteTimeUtc, z); }
        }
        Zaznamy.AddRange(z);
        Subory.Add($"{meno} ({z.Count} spektier)");
    }

    static double? Cislo(string s)
    {
        s = s.Trim().Replace(',', '.');
        var m = Regex.Match(s, @"^-?\d+(\.\d+)?");
        return m.Success && double.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static List<Zaznam> Citaj(string cesta, string meno)
    {
        var res = new List<Zaznam>();
        var text = File.ReadAllText(cesta);
        var bloky = Regex.Split(text.Replace("\r\n", "\n").Replace('\r', '\n'), @"\n\s*\n");
        var rePair = new Regex(@"(\d+(?:\.\d+)?)[ \t:]+(\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)");
        foreach (var b in bloky)
        {
            var lines = b.Split('\n');
            int np = Array.FindIndex(lines, l => l.TrimStart().StartsWith("num peaks", StringComparison.OrdinalIgnoreCase));
            if (np < 0) continue;
            var z = new Zaznam { Subor = meno, Zdroj = meno };
            for (int i = 0; i < np; i++)
            {
                int c = lines[i].IndexOf(':'); if (c <= 0) continue;
                var k = lines[i][..c].Trim().ToUpperInvariant(); var v = lines[i][(c + 1)..].Trim();
                switch (k)
                {
                    case "NAME": z.Nazov = v; break;
                    case "RETENTIONINDEX": case "RI": case "RETENTION_INDEX": { var x = Cislo(v); if (x > 0) z.Ri = x.Value; break; }
                    case "RETENTIONTIME": case "RT": case "RETENTION_TIME": { var x = Cislo(v); if (x > 0) z.RtMin = x.Value; break; }
                    case "COMMENT":
                        var m = Regex.Match(v, @"zdroj=([^;]+)");
                        if (m.Success) z.Zdroj = meno + " / " + m.Groups[1].Value.Trim();
                        break;
                }
            }
            var pk = new SortedDictionary<int, double>();
            for (int i = np + 1; i < lines.Length; i++)
                foreach (Match m in rePair.Matches(lines[i]))
                {
                    int mz = (int)Math.Round(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), MidpointRounding.ToEven);
                    double it = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                    if (mz > 0) pk[mz] = Math.Max(pk.GetValueOrDefault(mz), it);
                }
            if (string.IsNullOrWhiteSpace(z.Nazov) || pk.Count == 0) continue;
            double mx = pk.Values.Max(); if (mx <= 0) continue;
            z.Mz = pk.Keys.ToArray(); z.I = pk.Values.Select(x => 999.0 * x / mx).ToArray();
            z.Kluc = Kluc(z.Nazov); z.Kategoria = Kategoria(z.Kluc);
            res.Add(z);
        }
        return res;
    }

    // ---------- vyhľadávanie ----------
    float[]? mat; int mLo, mHi;

    void Priprav(int lo, int hi)
    {
        if (mat != null && mLo == lo && mHi == hi) return;
        int nb = hi - lo + 1;
        mat = new float[Zaznamy.Count * nb];
        for (int r = 0; r < Zaznamy.Count; r++)
        {
            var z = Zaznamy[r]; double nn = 0; int off = r * nb;
            for (int j = 0; j < z.Mz.Length; j++)
            {
                int b = z.Mz[j] - lo; if (b < 0 || b >= nb) continue;
                float v = (float)Math.Sqrt(z.I[j]); mat[off + b] = v; nn += v * v;
            }
            float f = (float)(1.0 / (Math.Sqrt(nn) + 1e-12));
            for (int b = 0; b < nb; b++) mat[off + b] *= f;
        }
        mLo = lo; mHi = hi;
    }

    /// <summary>Najlepšie zhody (jedna na látku) pre spektrum v rozsahu m/z [specLo, …].</summary>
    public List<(double cos, Zaznam z)> Hladaj(double[] spec, int specLo, int mzOd, int pocet = 400)
    {
        var vysl = new List<(double, Zaznam)>();
        if (Zaznamy.Count == 0) return vysl;
        int lo = Math.Max(mzOd, specLo), hi = specLo + spec.Length - 1;
        if (hi < lo) return vysl;
        lock (this) Priprav(lo, hi);
        int nb = hi - lo + 1;
        var q = new float[nb]; double nn = 0;
        for (int b = 0; b < nb; b++) { double v = spec[b + lo - specLo]; v = v > 0 ? Math.Sqrt(v) : 0; q[b] = (float)v; nn += v * v; }
        if (nn <= 0) return vysl;
        float f = (float)(1.0 / Math.Sqrt(nn)); for (int b = 0; b < nb; b++) q[b] *= f;
        var sc = new float[Zaznamy.Count];
        var m = mat!;
        Parallel.For(0, Zaznamy.Count, r =>
        {
            int off = r * nb; float s = 0;
            for (int b = 0; b < nb; b++) s += m[off + b] * q[b];
            sc[r] = s;
        });
        var idx = Enumerable.Range(0, sc.Length).OrderByDescending(i => sc[i]).Take(pocet);
        var best = new Dictionary<string, (double, Zaznam)>();
        foreach (var i in idx)
        {
            var z = Zaznamy[i]; var k = (z.Vlastna ? "§" : "") + z.Kluc;
            if (!best.ContainsKey(k)) best[k] = (sc[i], z);
        }
        return best.Values.OrderByDescending(x => x.Item1).ToList();
    }
}
