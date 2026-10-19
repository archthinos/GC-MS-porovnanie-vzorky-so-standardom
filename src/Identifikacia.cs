namespace GcmsPorovnanie;

public sealed class Navrh
{
    public string Nazov { get; set; } = "";
    public string Kategoria { get; set; } = "";
    public string Stav { get; set; } = "";
    public string Zdroj { get; set; } = "";
    public double Zhoda { get; set; }
    public double? RiKniznica { get; set; }
    public double? DRi { get; set; }
    public double? RtKniznica { get; set; }
    public string? NajKyselina { get; set; }
    public double? ZhodaKyseliny { get; set; }
    public string Alternativy { get; set; } = "";
    public string Poznamka { get; set; } = "";
    public int[] KnizMz { get; set; } = Array.Empty<int>();
    public double[] KnizI { get; set; } = Array.Empty<double>();
    public List<Kandidat> Kandidati { get; set; } = new();
}

public sealed class Kandidat
{
    public string Nazov { get; set; } = "";
    public string Kategoria { get; set; } = "";
    public double Zhoda { get; set; }
    public double? Ri { get; set; }
    public double? DRi { get; set; }
    public string Zdroj { get; set; } = "";
}

public sealed class Alkan
{
    public int C { get; set; }
    public double RtS { get; set; }
    public double RtMin { get; set; }
    public double Vyska { get; set; }
    public double Zhoda { get; set; }
}

/// <summary>Retenčný index z merania n-alkánov (van den Dool–Kratz).</summary>
public sealed class AlkanovaKalibracia
{
    public List<Alkan> Alkany = new();
    public List<string> Varovania = new();
    public string Subor = "";

    // typické EI spektrum n-alkánu (m/z ≥ 50)
    static readonly (int mz, double i)[] Vzor = { (57, 100), (71, 55), (85, 35), (99, 12), (113, 8), (127, 5), (141, 4), (155, 3) };

    public static AlkanovaKalibracia Najdi(Meranie A, Nastavenia o)
    {
        var k = new AlkanovaKalibracia { Subor = A.Nazov };
        var spec = Sig.SpecMinusBaseline(A.Spec, o.PozadieOkno);
        var y = Sig.MinusBaseline(A.Tic, o.PozadieOkno);
        var p = Sig.FindPeaks(y, Sig.Median(y.Select(Math.Abs)) * o.PikyFaktor, 2);
        var vz = new double[A.MzMax - A.MzMin + 1];
        foreach (var (mz, i) in Vzor) if (mz >= A.MzMin && mz <= A.MzMax) vz[mz - A.MzMin] = i;
        var kand = new List<(double t, double h, double c, double[] s)>();
        int j50 = Math.Max(0, 50 - A.MzMin);   // vzor aj spektrum len od m/z 50 (nezávisle od začiatku skenu)
        double CosOd50(double[] s)
        {
            double ab = 0, aa = 0, bb = 0;
            for (int j = j50; j < s.Length; j++) { ab += s[j] * vz[j]; aa += s[j] * s[j]; bb += vz[j] * vz[j]; }
            return aa > 0 && bb > 0 ? ab / Math.Sqrt(aa * bb) : 0;
        }
        foreach (var i in p)
        {
            var s = spec[i];
            double c = CosOd50(s);
            if (c >= 0.70) kand.Add((Sig.Apex(A.Rt, y, i), y[i], c, s));
        }
        if (kand.Count == 0) { k.Varovania.Add("V meraní alkánov sa nenašli žiadne píky so spektrom n-alkánu (m/z 57, 71, 85…)."); return k; }
        double hmax = kand.Max(x => x.h);
        kand = kand.Where(x => x.h >= 0.02 * hmax).OrderBy(x => x.t).ToList();

        // počet uhlíkov z molekulového iónu M = 14n + 2 (najvyšší taký ión, ktorý je výrazne silnejší než M±14)
        int? CzM(double[] s)
        {
            double mx = s.Max(); if (mx <= 0) return null;
            double Rel(int m) => m >= A.MzMin && m <= A.MzMax ? s[m - A.MzMin] / mx : 0;
            for (int cc = 45; cc >= 6; cc--)
            {
                int m = 14 * cc + 2;
                if (m > A.MzMax || m < A.MzMin) continue;
                double r = Rel(m);
                if (r >= 0.005 && r >= 3 * Rel(m - 14) && r >= 3 * Rel(m + 14)) return cc;
            }
            return null;
        }
        var cz0 = kand.Select(x => CzM(x.s)).ToArray();
        // prijať len C podporené ďalším alkánom (cz_k − cz_j = k − j); osamotené odchýlky (slabý M+, rušivý ión) zahodiť
        var cz = new int?[cz0.Length];
        int nUrc = cz0.Count(x => x != null);
        for (int j = 0; j < cz0.Length; j++)
        {
            if (cz0[j] == null) continue;
            int sup = Enumerable.Range(0, cz0.Length).Count(q => q != j && cz0[q] != null && cz0[q] - cz0[j] == q - j);
            if (sup >= 1 || nUrc == 1) cz[j] = cz0[j];
        }
        int zahodene = nUrc - cz.Count(x => x != null);
        var C = new int[kand.Count];
        int nz = cz.Count(x => x != null);
        if (o.PrvyAlkan > 0)
        {
            for (int j = 0; j < C.Length; j++) C[j] = o.PrvyAlkan + j;
            int nes = Enumerable.Range(0, C.Length).Count(j => cz[j] != null && cz[j] != C[j]);
            if (nes > 0) k.Varovania.Add($"Pri {nes} alkánoch molekulový ión nezodpovedá zadanému číslovaniu od C{o.PrvyAlkan} – skontrolujte najnižší alkán.");
        }
        else if (nz > 0)
        {
            // známe C z M+; ostatné doplniť od najbližšieho známeho suseda
            for (int j = 0; j < C.Length; j++)
            {
                if (cz[j] != null) { C[j] = cz[j]!.Value; continue; }
                int jl = j - 1; while (jl >= 0 && cz[jl] == null) jl--;
                int jr = j + 1; while (jr < C.Length && cz[jr] == null) jr++;
                if (jl >= 0 && (jr >= C.Length || j - jl <= jr - j)) C[j] = cz[jl]!.Value + (j - jl);
                else C[j] = cz[jr]!.Value - (jr - j);
            }
            k.Varovania.Add($"Počty uhlíkov určené z molekulových iónov ({nz} z {C.Length} alkánov{(zahodene > 0 ? $", {zahodene} nejednoznačné vynechané" : "")}, ostatné doplnené podľa poradia) – skontrolujte ich.");
        }
        else
        {
            for (int j = 0; j < C.Length; j++) C[j] = 10 + j;
            k.Varovania.Add("Počet uhlíkov alkánov sa nepodarilo určiť z molekulových iónov – predpokladá sa, že prvý nájdený alkán je C10. Ak to nesedí, zadajte najnižší alkán v nastaveniach.");
        }
        // poradie musí rásť; dva píky s rovnakým C = chyba
        var zle = Enumerable.Range(1, C.Length - 1).Where(j => C[j] <= C[j - 1]).ToList();
        if (zle.Count > 0)
        {
            k.Varovania.Add("Číslovanie alkánov podľa molekulových iónov nie je rastúce – použité je poradie od prvého určeného alkánu. Zadajte najnižší alkán ručne.");
            int c0 = C[0]; for (int j = 0; j < C.Length; j++) C[j] = c0 + j;
        }
        for (int j = 0; j < kand.Count; j++)
            k.Alkany.Add(new Alkan { C = C[j], RtS = kand[j].t, RtMin = kand[j].t / 60, Vyska = kand[j].h, Zhoda = kand[j].c });
        // chýbajúce alkány: skok v číslovaní alebo nezvyčajne veľká medzera v čase
        for (int j = 1; j < k.Alkany.Count; j++)
        {
            var a0 = k.Alkany[j - 1]; var a1 = k.Alkany[j];
            if (a1.C - a0.C > 1)
                k.Varovania.Add($"Chýba alkán medzi C{a0.C} a C{a1.C} (nenašiel sa) – RI sa v tomto úseku interpoluje cez väčší interval.");
            else if (j >= 2 && j + 1 < k.Alkany.Count)
            {
                double d0 = a0.RtS - k.Alkany[j - 2].RtS, d1 = a1.RtS - a0.RtS, d2 = k.Alkany[j + 1].RtS - a1.RtS;
                if (d1 > 1.5 * (d0 + d2) / 2)
                    k.Varovania.Add($"Medzi C{a0.C} a C{a1.C} je nezvyčajne veľká medzera v čase – možno chýba alkán a číslovanie nesedí.");
            }
        }
        if (k.Alkany.Count < 2) k.Varovania.Add("Na výpočet RI treba aspoň dva alkány.");
        return k;
    }

    public bool VRozsahu(double tS) => Alkany.Count >= 2 && tS >= Alkany[0].RtS && tS <= Alkany[^1].RtS;

    public double? Ri(double tS)
    {
        if (Alkany.Count < 2) return null;
        int j = 0;
        while (j < Alkany.Count - 2 && tS > Alkany[j + 1].RtS) j++;
        var a = Alkany[j]; var b = Alkany[j + 1];
        return 100.0 * (a.C + (tS - a.RtS) / (b.RtS - a.RtS) * (b.C - a.C));
    }
}

/// <summary>Doplní k výsledkom porovnania retenčný index a návrh látky z knižníc.</summary>
public static class Identifikacia
{
    sealed class Riadok
    {
        public double TS; public double[] Spec = Array.Empty<double>(); public int Lo;
        public List<(double cos, Zaznam z)> Cand = new();
        public bool Std; public object Ciel = null!;
        public double? Ri; public double Tol; public bool VRozsahu;
        public Navrh? N;
    }

    /// <summary>Spektrum v okolí času t (vrchol ±1 sken, po odčítaní pozadia).</summary>
    static double[] Spektrum(Meranie M, double[][] spec, double tS)
    {
        int i = 0; double dm = double.MaxValue;
        for (int q = 0; q < M.N; q++) { double d = Math.Abs(M.Rt[q] - tS); if (d < dm) { dm = d; i = q; } }
        int a0 = Math.Max(i - 2, 0), a1 = Math.Min(i + 2, M.N - 1), ib = i; double bs = -1;
        for (int q = a0; q <= a1; q++) { double s = spec[q].Sum(); if (s > bs) { bs = s; ib = q; } }
        int nb = spec[0].Length; var v = new double[nb]; int n = 0;
        for (int q = Math.Max(ib - 1, 0); q <= Math.Min(ib + 1, M.N - 1); q++) { for (int b = 0; b < nb; b++) v[b] += spec[q][b]; n++; }
        for (int b = 0; b < nb; b++) v[b] /= n;
        return v;
    }

    static (int[] mz, double[] i) Top(double[] v, int lo, int n = 80)
    {
        double mx = v.Max(); if (mx <= 0) return (Array.Empty<int>(), Array.Empty<double>());
        var idx = Enumerable.Range(0, v.Length).Where(j => v[j] > 0.002 * mx).OrderByDescending(j => v[j]).Take(n).OrderBy(j => j).ToArray();
        return (idx.Select(j => j + lo).ToArray(), idx.Select(j => Math.Round(999.0 * v[j] / mx, 1)).ToArray());
    }

    static string Fmt(double x, string f = "0.00") => x.ToString(f, Program.Sk);

    public static double[][] PripravSpektra(Meranie M, Nastavenia o) => Sig.SpecMinusBaseline(M.Spec, o.PozadieOkno);

    public static void Dopln(Vysledok v, Meranie S, double[][] specS, Meranie V, Kniznica? kniz, AlkanovaKalibracia? alk, Nastavenia o)
    {
        {
            var specV = Sig.SpecMinusBaseline(V.Spec, o.PozadieOkno);
            var rows = new List<Riadok>();
            foreach (var p in v.Piky) rows.Add(new Riadok { TS = p.RtStdS, Spec = Spektrum(S, specS, p.RtStdS), Lo = S.MzMin, Std = true, Ciel = p });
            foreach (var e in v.Navyse) rows.Add(new Riadok { TS = e.RtS, Spec = Spektrum(V, specV, e.RtS), Lo = V.MzMin, Std = false, Ciel = e });
            foreach (var r in rows)
            {
                var (mz, ii) = Top(r.Spec, r.Lo);
                if (r.Ciel is PikRiadok pr) { pr.SpecMz = mz; pr.SpecI = ii; } else if (r.Ciel is PikNavyse pn) { pn.SpecMz = mz; pn.SpecI = ii; }
                if (kniz != null) r.Cand = kniz.Hladaj(r.Spec, r.Lo, o.MzOd);
            }

            // --- retenčný index ---
            string riInfo; double tolIn = o.RiTol, tolOut = 2 * o.RiTol;
            Func<double, double?> riOf = _ => null; Func<double, bool> inRange = _ => false;
            if (o.RiZdroj == "alkany" && alk != null && alk.Alkany.Count >= 2)
            {
                riOf = t => alk.Ri(t); inRange = alk.VRozsahu;
                riInfo = $"RI z merania alkánov {alk.Subor}: C{alk.Alkany[0].C}–C{alk.Alkany[^1].C} ({Fmt(alk.Alkany[0].RtMin)}–{Fmt(alk.Alkany[^1].RtMin)} min), lineárna interpolácia (van den Dool–Kratz); tolerancia ±{Fmt(tolIn, "0")} (mimo rozsahu alkánov ±{Fmt(tolOut, "0")}).";
                if (alk.Varovania.Count > 0) riInfo += " " + string.Join(" ", alk.Varovania);
            }
            else if (o.RiZdroj == "alkany")
                riInfo = "RI sa nepočíta – meranie alkánov chýba alebo sa v ňom nenašli aspoň dva alkány." + (alk != null && alk.Varovania.Count > 0 ? " " + string.Join(" ", alk.Varovania) : "");
            else if (o.RiZdroj == "odhad" && kniz != null)
            {
                var pts = rows.Where(r => r.Std && r.Cand.Count > 0 && r.Cand[0].cos >= 0.90 && !double.IsNaN(r.Cand[0].z.Ri))
                              .OrderBy(r => r.TS).Select(r => (t: r.TS / 60, ri: r.Cand[0].z.Ri, nm: r.Cand[0].z.Nazov)).ToList();
                var cal = Kalibruj(pts);
                if (cal != null)
                {
                    var (C, lo, hi, sd, pouzite) = cal.Value;
                    tolIn = Math.Max(3 * sd, 40); tolOut = 100;
                    riOf = t => { double m = t / 60; return m < lo ? Pol(C, lo) + Der(C, lo) * (m - lo) : m > hi ? Pol(C, hi) + Der(C, hi) * (m - hi) : Pol(C, m); };
                    inRange = t => t / 60 >= lo - 0.5 && t / 60 <= hi + 0.5;
                    riInfo = $"RI len odhadnutý zo štandardu (bez alkánov): kvadratická kalibrácia z {pouzite.Count} istých zásahov ({string.Join(", ", pouzite)}), sd {Fmt(sd, "0")}, rozsah {Fmt(lo)}–{Fmt(hi)} min; tolerancia ±{Fmt(tolIn, "0")} (mimo rozsahu ±{Fmt(tolOut, "0")}). Pre tieto body ΔRI nie je nezávislé overenie.";
                }
                else riInfo = "RI sa nepodarilo odhadnúť zo štandardu (málo istých zásahov s RI v knižnici) – návrhy sú len podľa spektra.";
            }
            else riInfo = "RI sa nepoužíva – návrhy sú len podľa spektra.";
            v.RiInfo = riInfo;
            if (alk != null) v.Alkany = alk.Alkany;

            foreach (var r in rows)
            {
                r.Ri = riOf(r.TS); r.VRozsahu = r.Ri != null && inRange(r.TS); r.Tol = r.VRozsahu ? tolIn : tolOut;
                if (r.Ciel is PikRiadok pr) pr.Ri = r.Ri; else if (r.Ciel is PikNavyse pn) pn.Ri = r.Ri;
            }
            if (kniz == null) return;

            // --- návrhy ---
            foreach (var r in rows)
            {
                if (r.Cand.Count == 0) continue;
                bool Ok((double cos, Zaznam z) c)
                {
                    if (r.Ri != null && !double.IsNaN(c.z.Ri)) return Math.Abs(c.z.Ri - r.Ri.Value) <= r.Tol;
                    if (c.z.Vlastna) return Math.Abs(c.z.RtMin * 60 - r.TS) <= o.RtTolVlastna;
                    return true;
                }
                double Skore((double cos, Zaznam z) c)
                {
                    double s = c.cos;
                    if (r.Ri != null && !double.IsNaN(c.z.Ri)) s -= 0.002 * Math.Abs(c.z.Ri - r.Ri.Value);
                    else if (r.Ri != null) s -= 0.03;
                    if (c.z.Vlastna && c.cos >= o.CosOk) s += 1;   // vlastná knižnica má prednosť
                    return s;
                }
                var ok = r.Cand.Where(Ok).OrderByDescending(Skore).ToList();
                var b = ok.Count > 0 ? ok[0] : r.Cand[0];
                double? dri = r.Ri != null && !double.IsNaN(b.z.Ri) ? b.z.Ri - r.Ri.Value : null;
                string stav;
                if (b.z.Vlastna && b.cos >= o.CosOk && ok.Count > 0) stav = "vlastná knižnica – zhoda";
                else if (b.cos >= o.CosNavrh && dri != null && Math.Abs(dri.Value) <= Math.Min(r.Tol, tolIn) && r.VRozsahu) stav = "pravdepodobne";
                else if (b.cos >= o.CosNavrh)
                    stav = r.Ri == null ? "možno (bez RI)" : double.IsNaN(b.z.Ri) ? "možno (RI v knižnici chýba)" : !r.VRozsahu ? "možno (mimo rozsahu RI)" : "možno (RI na hrane)";
                else if (b.cos >= o.CosMozno) stav = "možno";
                else stav = "neurčené";
                var kys = ok.FirstOrDefault(c => c.z.Kategoria == "kyselina");
                var n = new Navrh
                {
                    Nazov = b.z.Nazov, Kategoria = b.z.Kategoria, Stav = stav, Zdroj = b.z.Zdroj, Zhoda = b.cos,
                    RiKniznica = double.IsNaN(b.z.Ri) ? null : b.z.Ri, DRi = dri, RtKniznica = b.z.Vlastna ? b.z.RtMin : null,
                    NajKyselina = kys.z?.Nazov, ZhodaKyseliny = kys.z != null ? kys.cos : null,
                    Alternativy = string.Join("; ", ok.Skip(1).Take(3).Select(c => $"{c.z.Nazov} {Fmt(c.cos)}" + (double.IsNaN(c.z.Ri) ? "" : $" (RI {Fmt(c.z.Ri, "0")})"))),
                    KnizMz = b.z.Mz, KnizI = b.z.I.Select(x => Math.Round(x, 1)).ToArray(),
                    Kandidati = ok.Take(5).Select(c => new Kandidat
                    {
                        Nazov = c.z.Nazov, Kategoria = c.z.Kategoria, Zhoda = c.cos, Zdroj = c.z.Zdroj,
                        Ri = double.IsNaN(c.z.Ri) ? null : c.z.Ri, DRi = r.Ri != null && !double.IsNaN(c.z.Ri) ? c.z.Ri - r.Ri.Value : null,
                    }).ToList(),
                };
                if (ok.Count == 0) n.Poznamka = "Žiadny kandidát nesedí v RI/RT – uvedená je najlepšia zhoda spektra bez ohľadu na RI.";
                r.N = n;
                if (r.Ciel is PikRiadok pr) pr.Navrh = n; else if (r.Ciel is PikNavyse pn) pn.Navrh = n;
            }
            // tá istá látka navrhnutá pri rôznych časoch
            foreach (var r in rows.Where(r => r.N != null && (r.N.Stav == "pravdepodobne" || r.N.Stav.StartsWith("vlastná"))))
            {
                var ine = rows.Where(o2 => o2 != r && o2.N != null && Kniznica.Kluc(o2.N.Nazov) == Kniznica.Kluc(r.N!.Nazov)
                                         && (o2.N.Stav == "pravdepodobne" || o2.N.Stav.StartsWith("vlastná") || o2.N.Stav.StartsWith("možno"))
                                         && Math.Abs(o2.TS - r.TS) > 6).ToList();
                if (ine.Count == 0) continue;
                r.N!.Poznamka = "Tá istá látka navrhnutá aj pri " + string.Join(", ", ine.Select(x => Fmt(x.TS / 60) + " min")) + " – aspoň jedno priradenie je zlé.";
                if (r.N.Stav == "pravdepodobne") r.N.Stav = "možno (duplicitné priradenie)";
            }
            v.KniznicaInfo = string.Join(", ", kniz.Subory) + $"; porovnanie spektier od m/z {o.MzOd}, prah „pravdepodobne“ {Fmt(o.CosNavrh)}, „možno“ {Fmt(o.CosMozno)}.";
        }
    }

    static double Pol(double[] C, double x) => C[0] * x * x + C[1] * x + C[2];
    static double Der(double[] C, double x) => 2 * C[0] * x + C[1];

    /// <summary>Robustná kvadratická kalibrácia RT(min) → RI: vyhodí body, ktoré porušujú poradie alebo odbiehajú.</summary>
    static (double[] C, double lo, double hi, double sd, List<string> nm)? Kalibruj(List<(double t, double ri, string nm)> pts)
    {
        var P = pts.ToList();
        while (P.Count >= 5)
        {
            var C = Fit2(P); var res = P.Select(p => p.ri - Pol(C, p.t)).ToArray();
            double sd = Math.Sqrt(res.Select(x => x * x).Average());
            var bad = new HashSet<int>();
            for (int k = 0; k + 1 < P.Count; k++) if (P[k].ri > P[k + 1].ri) { bad.Add(k); bad.Add(k + 1); }
            for (int k = 0; k < P.Count; k++) if (Math.Abs(res[k]) > Math.Max(2.5 * sd, 25)) bad.Add(k);
            if (bad.Count == 0 || P.Count <= 5)
            {
                if (C[0] < 0 && Der(C, P[^1].t) <= 0) return null;
                return (C, P[0].t, P[^1].t, sd, P.Select(p => p.nm).ToList());
            }
            int w = bad.OrderByDescending(k => Math.Abs(res[k])).First();
            P.RemoveAt(w);
        }
        return null;
    }

    static double[] Fit2(List<(double t, double ri, string nm)> P)
    {
        // normálne rovnice pre y = a x² + b x + c
        double[,] A = new double[3, 4];
        foreach (var p in P)
        {
            double[] x = { p.t * p.t, p.t, 1 };
            for (int i = 0; i < 3; i++) { for (int j = 0; j < 3; j++) A[i, j] += x[i] * x[j]; A[i, 3] += x[i] * p.ri; }
        }
        for (int i = 0; i < 3; i++)
        {
            int piv = i; for (int r = i + 1; r < 3; r++) if (Math.Abs(A[r, i]) > Math.Abs(A[piv, i])) piv = r;
            for (int c = 0; c < 4; c++) (A[i, c], A[piv, c]) = (A[piv, c], A[i, c]);
            for (int r = 0; r < 3; r++)
            {
                if (r == i) continue; double f = A[r, i] / A[i, i];
                for (int c = 0; c < 4; c++) A[r, c] -= f * A[i, c];
            }
        }
        return new[] { A[0, 3] / A[0, 0], A[1, 3] / A[1, 1], A[2, 3] / A[2, 2] };
    }
}
