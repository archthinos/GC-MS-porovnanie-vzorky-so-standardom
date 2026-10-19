namespace GcmsPorovnanie;

public sealed class Nastavenia
{
    public double Tol { get; set; } = 0.5;        // tolerancia posunu (s), obojstranne
    public double Krok { get; set; } = 0.05;      // krok hľadania posunu (s)
    public double CosOk { get; set; } = 0.80;     // prah podobnosti spektra
    public double CosIsty { get; set; } = 0.85;   // pri dekonvolúcii: pod touto hodnotou = neisté
    public double Prom { get; set; } = 10;        // signál > Prom × šum
    public double PikyFaktor { get; set; } = 20;  // prominencia píkov TIC = faktor × medián |TIC|
    public bool Dekonvolucia { get; set; } = true;
    public double DekOkno { get; set; } = 5.0;    // ±s
    public int[] DekK { get; set; } = { 2, 3, 4, 5 };
    public int DekStarty { get; set; } = 5;
    public int DekIteracie { get; set; } = 3000;
    public double EicOkno { get; set; } = 3.0;    // ±s pre koreláciu tvaru píku
    public int PozadieOkno { get; set; } = 81;    // skenov
    // --- identifikácia látok (voliteľné) ---
    public bool NavrhLatok { get; set; } = false;          // hľadať v knižniciach .msp
    public string[] Kniznice { get; set; } = Array.Empty<string>(); // vybrané priložené knižnice (názvy súborov)
    public string RiZdroj { get; set; } = "odhad";         // alkany | odhad | ziadny
    public int PrvyAlkan { get; set; } = 0;                // počet C prvého alkánu; 0 = automaticky
    public double RiTol { get; set; } = 40;                // tolerancia RI pri alkánoch (±)
    public int MzOd { get; set; } = 85;                    // spektrá porovnávať od m/z
    public double RtTolVlastna { get; set; } = 6;          // ±s pre vlastnú knižnicu s RT (bez RI)
    public double CosNavrh { get; set; } = 0.85;           // zhoda pre „pravdepodobne“
    public double CosMozno { get; set; } = 0.75;           // zhoda pre „možno“

    public double[] Taus()
    {
        int n = (int)Math.Round(2 * Tol / Krok);
        return Enumerable.Range(0, n + 1).Select(i => Math.Round(-Tol + i * Krok, 6)).ToArray();
    }
}

public sealed class PikRiadok
{
    public double RtStdMin { get; set; }
    public double RtStdS { get; set; }
    public double VyskaStd { get; set; }
    public int HlavnyIon { get; set; }
    public string Ione { get; set; } = "";
    public double TauEic { get; set; }
    public double REic { get; set; }
    public double REic0 { get; set; }
    public double? VrcholVz { get; set; }
    public double? Posun { get; set; }
    public double? CosVrchol { get; set; }
    public double VyskaVz { get; set; }
    public double Sum { get; set; }
    public double? Pomer { get; set; }
    public string Stav { get; set; } = "";
    public double? DekCos { get; set; }
    public double? DekPosun { get; set; }
    public string? DekK { get; set; }
    public string? DekDetail { get; set; }
    public double CosKdekolvek { get; set; }
    public double RtKdekolvekMin { get; set; }
    public double? Ri { get; set; }
    public Navrh? Navrh { get; set; }
    public int[] SpecMz { get; set; } = Array.Empty<int>();
    public double[] SpecI { get; set; } = Array.Empty<double>();
    // pre graf detailu
    public double[] EicRtS { get; set; } = Array.Empty<double>();
    public double[] EicS { get; set; } = Array.Empty<double>();
    public double[] EicRtV { get; set; } = Array.Empty<double>();
    public double[] EicV { get; set; } = Array.Empty<double>();
}

public sealed class PikNavyse
{
    public double RtMin { get; set; }
    public double RtS { get; set; }
    public double Vyska { get; set; }
    public string Ione { get; set; } = "";
    public double NajblizsiStd { get; set; }
    public double? Ri { get; set; }
    public Navrh? Navrh { get; set; }
    public int[] SpecMz { get; set; } = Array.Empty<int>();
    public double[] SpecI { get; set; } = Array.Empty<double>();
}

public sealed class TicRiadok
{
    public double Tau { get; set; }
    public double R { get; set; }
    public double A { get; set; }
    public double Rmse { get; set; }
    public double Mae { get; set; }
    public double Max { get; set; }
}

public sealed class SpolocnyPosun
{
    public double Tau { get; set; }
    public double R { get; set; }
    public int N { get; set; }
    public double Median { get; set; }
    public double Priemer { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double[] KrivkaTau { get; set; } = Array.Empty<double>();
    public double[] KrivkaR { get; set; } = Array.Empty<double>();
}

public sealed class Vysledok
{
    public string Standard { get; set; } = "";
    public string Vzorka { get; set; } = "";
    public Dictionary<string, string> InfoStd { get; set; } = new();
    public Dictionary<string, string> InfoVz { get; set; } = new();
    public List<PikRiadok> Piky { get; set; } = new();
    public List<PikNavyse> Navyse { get; set; } = new();
    public List<TicRiadok> Tic { get; set; } = new();
    public TicRiadok TicNaj { get; set; } = new();
    public SpolocnyPosun? Posun { get; set; }
    public Dictionary<string, int> Pocty { get; set; } = new();
    public string Zaver { get; set; } = "";
    public string? RiInfo { get; set; }
    public string? KniznicaInfo { get; set; }
    public List<Alkan> Alkany { get; set; } = new();
    public double[] RtS { get; set; } = Array.Empty<double>();
    public double[] TicS { get; set; } = Array.Empty<double>();
    public double[] RtV { get; set; } = Array.Empty<double>();
    public double[] TicV { get; set; } = Array.Empty<double>();
}

public static class Porovnanie
{
    public static readonly string[] Stavy =
    {
        "v tolerancii", "v tolerancii (rameno)", "v tolerancii (dekonvolúcia)", "neisté (dekonvolúcia)",
        "mimo tolerancie", "iné spektrum", "nenájdený",
    };

    public static Vysledok Porovnaj(Meranie S, Meranie V, Nastavenia o)
    {
        var taus = o.Taus();
        double TOL = o.Tol, eps = 1e-9;
        int lo = Math.Min(S.MzMin, V.MzMin), hi = Math.Max(S.MzMax, V.MzMax);
        var rtS = S.Rt; var rtV = V.Rt; var ticS = S.Tic; var ticV = V.Tic;

        // --- 1) TIC (informatívne) ---
        var ticRows = new List<TicRiadok>();
        foreach (var tau in taus)
        {
            var y = new List<double>(); var g = new List<double>();
            for (int i = 0; i < rtS.Length; i++)
            {
                double v = Sig.Interp(rtS[i] + tau, rtV, ticV, double.NaN, double.NaN);
                if (!double.IsNaN(v)) { y.Add(ticS[i]); g.Add(v); }
            }
            var ya = y.ToArray(); var ga = g.ToArray();
            double r = Sig.Corr(ya, ga);
            double yg = 0, gg = 0; for (int i = 0; i < ya.Length; i++) { yg += ya[i] * ga[i]; gg += ga[i] * ga[i]; }
            double a = yg / gg; double se = 0, sa = 0, mx = 0;
            for (int i = 0; i < ya.Length; i++) { double e = a * ga[i] - ya[i]; se += e * e; sa += Math.Abs(e); mx = Math.Max(mx, Math.Abs(e)); }
            ticRows.Add(new TicRiadok { Tau = tau, R = r, A = a, Rmse = Math.Sqrt(se / ya.Length), Mae = sa / ya.Length, Max = mx });
        }
        var best = ticRows.OrderByDescending(t => double.IsNaN(t.R) ? double.MinValue : t.R).First();

        // --- 2) píky štandardu ---
        var specS = Sig.SpecMinusBaseline(S.SpecVRozsahu(lo, hi), o.PozadieOkno);
        var specV = Sig.SpecMinusBaseline(V.SpecVRozsahu(lo, hi), o.PozadieOkno);
        int nb = hi - lo + 1;
        var yS = Sig.MinusBaseline(ticS, o.PozadieOkno);
        double promS = Sig.Median(yS.Select(Math.Abs)) * o.PikyFaktor;
        var p = Sig.FindPeaks(yS, promS, 2);

        // normované spektrá vzorky (zhoda „kdekoľvek“)
        var nVall = specV.Select(v => { double nn = Math.Sqrt(v.Sum(x => x * x)) + 1e-9; return v.Select(x => x / nn).ToArray(); }).ToArray();
        var rows = new PikRiadok[p.Count];
        var rsAll = new double[p.Count][];

        Parallel.For(0, p.Count, pi =>
        {
            int i = p[pi];
            var sp = specS[i];
            var top = Enumerable.Range(0, nb).OrderByDescending(j => sp[j]).Take(5).Where(j => sp[j] > 0).ToArray();
            if (top.Length == 0) top = new[] { Array.IndexOf(sp, sp.Max()) };
            int k = top[0];
            var eS = specS.Select(v => v[k]).ToArray();
            var eV = specV.Select(v => v[k]).ToArray();
            int a0 = Math.Max(i - 2, 0), a1 = Math.Min(i + 3, eS.Length);
            int ik = a0; for (int q = a0; q < a1; q++) if (eS[q] > eS[ik]) ik = q;
            double rt0 = Sig.Apex(rtS, eS, ik);

            // a) posun podľa tvaru píku (korelácia EIC)
            var wIdx = Enumerable.Range(0, rtS.Length).Where(q => rtS[q] >= rt0 - o.EicOkno && rtS[q] <= rt0 + o.EicOkno).ToArray();
            var eSw = wIdx.Select(q => eS[q]).ToArray();
            var rs = new double[taus.Length];
            for (int t = 0; t < taus.Length; t++)
            {
                var g = wIdx.Select(q => Sig.Interp(rtS[q] + taus[t], rtV, eV)).ToArray();
                double r = g.Length > 1 && Sig.Std(g) > 0 ? Sig.Corr(eSw, g) : 0.0;
                rs[t] = double.IsNaN(r) ? 0.0 : r;
            }
            int jt = 0; for (int t = 1; t < rs.Length; t++) if (rs[t] > rs[jt]) jt = t;

            // b) vrchol hlavného iónu vo vzorke
            var ii = Enumerable.Range(0, rtV.Length).Where(q => rtV[q] >= rt0 - 5 && rtV[q] <= rt0 + 5).ToArray();
            var eVw = ii.Select(q => eV[q]).ToArray();
            double noise = 1.0;
            if (eVw.Length > 1)
            {
                var d = new double[eVw.Length - 1];
                for (int q = 0; q < d.Length; q++) d[q] = Math.Abs(eVw[q + 1] - eVw[q]);
                noise = 1.4826 * Sig.Median(d) / Math.Sqrt(2) + 1.0;
            }
            var pk = eVw.Length > 2 ? Sig.FindPeaks(eVw, o.Prom * noise) : new List<int>();
            var win = Enumerable.Range(0, rtV.Length).Where(q => rtV[q] >= rt0 - TOL - eps && rtV[q] <= rt0 + TOL + eps).ToArray();
            double hV = win.Length > 0 ? win.Max(q => eV[q]) : 0.0;
            double? near = null, cosv = null;
            var cands = new List<(double d, double t, double h, double c)>();
            foreach (var q in pk)
            {
                double t = Sig.Apex(rtV, eV, ii[q]);
                var sv = Sig.InterpRows(t, rtV, specV);
                cands.Add((Math.Abs(t - rt0), t, eV[ii[q]], sv == null ? 0 : Sig.Cos(sp, sv)));
            }
            cands.Sort();
            var inTol = cands.Where(c => c.d <= TOL + eps && c.c >= o.CosOk).ToList();
            var outTol = cands.Where(c => c.d > TOL + eps && c.c >= o.CosOk).ToList();
            (double c, double tau, double h) cbest = (-1, 0, 0);
            foreach (var tau in taus)
            {
                var sv = Sig.InterpRows(rt0 + tau, rtV, specV);
                var cand = (sv != null ? Sig.Cos(sp, sv) : 0.0, tau, Sig.Interp(rt0 + tau, rtV, eV));
                if (cand.CompareTo(cbest) > 0) cbest = cand;
            }
            string stav;
            if (inTol.Count > 0) { near = inTol[0].t; hV = inTol[0].h; cosv = inTol[0].c; stav = "v tolerancii"; }
            else if (cbest.c >= o.CosOk && cbest.h > o.Prom * noise) { near = rt0 + cbest.tau; cosv = cbest.c; hV = cbest.h; stav = "v tolerancii (rameno)"; }
            else if (outTol.Count > 0) { near = outTol[0].t; hV = outTol[0].h; cosv = outTol[0].c; stav = "mimo tolerancie"; }
            else if (hV > o.Prom * noise)
            {
                if (cands.Count > 0) { near = cands[0].t; hV = cands[0].h; cosv = cands[0].c; }
                stav = "iné spektrum";
            }
            else stav = "nenájdený";
            double? posun = near.HasValue ? near - rt0 : null;

            // c) dekonvolúcia problémových píkov
            double? dCos = null, dPos = null; string? dK = null, dDet = null;
            // pri „nenájdený“ má zmysel len vtedy, keď je signál aspoň blízko hranice (zložka nemôže byť vyššia než dáta)
            if (o.Dekonvolucia && (stav == "iné spektrum" || stav == "v tolerancii (rameno)" || (stav == "nenájdený" && hV > 0.8 * o.Prom * noise)))
            {
                var resK = Nmf.Dekonvolucia(rt0, sp, k, rtV, specV, o, pi);
                var ok = resK.Where(x => x.cos >= o.CosOk && Math.Abs(x.posun) <= TOL + eps && x.vyska > o.Prom * noise).ToList();
                if (resK.Count > 0)
                {
                    var bx = resK.OrderByDescending(x => x.cos).First();
                    dCos = bx.cos; dPos = bx.posun; dK = string.Join(",", ok.Select(x => x.k));
                    dDet = string.Join("; ", resK.Select(x => $"k={x.k}: zhoda {x.cos:0.00}, posun {x.posun:+0.00;-0.00} s"));
                    if (ok.Count >= 2)
                    {
                        var okb = ok.OrderByDescending(x => x.cos).First();
                        dCos = okb.cos; dPos = okb.posun;
                        stav = okb.cos >= o.CosIsty ? "v tolerancii (dekonvolúcia)" : "neisté (dekonvolúcia)";
                    }
                    else if (ok.Count == 1) { stav = "neisté (dekonvolúcia)"; dCos = ok[0].cos; dPos = ok[0].posun; }
                }
            }
            if (stav == "v tolerancii") rsAll[pi] = rs;

            double spn = Math.Sqrt(sp.Sum(x => x * x)) + 1e-9;
            int jg = 0; double cgb = double.MinValue;
            for (int q = 0; q < nVall.Length; q++)
            {
                double c = 0; var v = nVall[q]; for (int j = 0; j < nb; j++) c += v[j] * sp[j] / spn;
                if (c > cgb) { cgb = c; jg = q; }
            }
            // detail EIC ±10 s
            var dS = Enumerable.Range(0, rtS.Length).Where(q => Math.Abs(rtS[q] - rt0) <= 10).ToArray();
            var dV = Enumerable.Range(0, rtV.Length).Where(q => Math.Abs(rtV[q] - rt0) <= 10).ToArray();
            rows[pi] = new PikRiadok
            {
                RtStdMin = rt0 / 60, RtStdS = rt0, VyskaStd = yS[i], HlavnyIon = k + lo,
                Ione = string.Join(", ", top.Select(t => t + lo)),
                TauEic = taus[jt], REic = rs[jt], REic0 = rs[taus.Length / 2],
                VrcholVz = near, Posun = posun, CosVrchol = cosv, VyskaVz = hV, Sum = noise,
                Pomer = sp[k] != 0 ? hV / sp[k] : null, Stav = stav,
                DekCos = dCos, DekPosun = dPos, DekK = dK, DekDetail = dDet,
                CosKdekolvek = cgb, RtKdekolvekMin = rtV[jg] / 60,
                EicRtS = dS.Select(q => rtS[q]).ToArray(), EicS = dS.Select(q => eS[q]).ToArray(),
                EicRtV = dV.Select(q => rtV[q]).ToArray(), EicV = dV.Select(q => eV[q]).ToArray(),
            };
        });

        var res = new Vysledok
        {
            Standard = S.Nazov, Vzorka = V.Nazov, InfoStd = S.Info, InfoVz = V.Info,
            Piky = rows.ToList(), Tic = ticRows, TicNaj = best,
            RtS = rtS, TicS = ticS, RtV = rtV, TicV = ticV,
        };
        // spoločný posun
        var R = rsAll.Where(x => x != null).ToList();
        if (R.Count > 0)
        {
            var m = new double[taus.Length];
            for (int t = 0; t < taus.Length; t++) m[t] = R.Average(x => x[t]);
            int j = 0; for (int t = 1; t < m.Length; t++) if (m[t] > m[j]) j = t;
            var sh = res.Piky.Where(r => r.Stav == "v tolerancii").Select(r => r.Posun!.Value).ToArray();
            res.Posun = new SpolocnyPosun
            {
                Tau = taus[j], R = m[j], N = R.Count, Median = Sig.Median(sh), Priemer = sh.Average(),
                Min = sh.Min(), Max = sh.Max(), KrivkaTau = taus, KrivkaR = m,
            };
        }
        // píky len vo vzorke
        var yV = Sig.MinusBaseline(ticV, o.PozadieOkno);
        var pv = Sig.FindPeaks(yV, Sig.Median(yV.Select(Math.Abs)) * o.PikyFaktor, 2);
        var rtsStd = res.Piky.Select(r => r.RtStdS).Concat(p.Select(i => Sig.Apex(rtS, yS, i))).ToArray();
        foreach (var j in pv)
        {
            double tv = Sig.Apex(rtV, yV, j);
            double d = rtsStd.Length > 0 ? rtsStd.Min(x => Math.Abs(x - tv)) : double.PositiveInfinity;
            if (d > TOL + eps)
            {
                var spv = specV[j];
                var top = Enumerable.Range(0, nb).OrderByDescending(q => spv[q]).Take(5);
                res.Navyse.Add(new PikNavyse { RtMin = tv / 60, RtS = tv, Vyska = yV[j], Ione = string.Join(", ", top.Select(q => q + lo)), NajblizsiStd = d });
            }
        }
        // počty a záver
        foreach (var s in Stavy) res.Pocty[s] = res.Piky.Count(r => r.Stav == s);
        int n = res.Piky.Count;
        int ntol = res.Pocty["v tolerancii"] + res.Pocty["v tolerancii (rameno)"] + res.Pocty["v tolerancii (dekonvolúcia)"];
        var z = $"{ntol} z {n} píkov štandardu je vo vzorke v tolerancii ±{TOL.ToString("0.##", Program.Sk).Replace('.', ',')} s";
        if (res.Pocty["v tolerancii (dekonvolúcia)"] > 0) z += $" (z toho {res.Pocty["v tolerancii (dekonvolúcia)"]} až po dekonvolúcii)";
        if (res.Pocty["v tolerancii (rameno)"] > 0) z += $" (z toho {res.Pocty["v tolerancii (rameno)"]} ako rameno nerozdeleného píku – overiť)";
        if (res.Pocty["mimo tolerancie"] > 0) z += $"; {res.Pocty["mimo tolerancie"]} je posunutých mimo tolerancie";
        if (res.Pocty["neisté (dekonvolúcia)"] > 0) z += $"; {res.Pocty["neisté (dekonvolúcia)"]} neisté (po dekonvolúcii zhoda len hraničná alebo nestabilná – overiť ručne)";
        int nen = res.Pocty["iné spektrum"] + res.Pocty["nenájdený"];
        if (nen > 0) z += $"; {nen} sa nenašlo (nenájdený / iné spektrum)";
        z += ".";
        if (V.Info.TryGetValue("Typ", out var typ) && typ.Contains("Blank", StringComparison.OrdinalIgnoreCase))
            z += " Vzorka je v metadátach označená ako Blank – overte, či ide o správny súbor vzorky.";
        res.Zaver = z;
        return res;
    }
}
