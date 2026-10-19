using ClosedXML.Excel;

namespace GcmsPorovnanie;

public static class Excel
{
    static readonly Dictionary<string, string> Farba = new()
    {
        ["v tolerancii"] = "C6EFCE", ["v tolerancii (rameno)"] = "E2EFDA", ["v tolerancii (dekonvolúcia)"] = "E2EFDA",
        ["neisté (dekonvolúcia)"] = "FFEB9C", ["mimo tolerancie"] = "FFEB9C", ["iné spektrum"] = "FFC7CE", ["nenájdený"] = "FFC7CE",
    };

    static string FarbaNavrhu(string stav) =>
        stav.StartsWith("vlastná") ? "9BD3AE" : stav == "pravdepodobne" ? "C6EFCE" : stav.StartsWith("možno") ? "FFEB9C" : "EEEEEE";

    static readonly (string h, Func<double?, Navrh?, object?> f, string fmt, double w)[] NavrhStlpce =
    {
        ("RI", (ri, n) => ri, "0", 8),
        ("Návrh látky (knižnica)", (ri, n) => n?.Nazov, "@", 30),
        ("Kategória", (ri, n) => n?.Kategoria, "@", 12),
        ("Zhoda s knižnicou", (ri, n) => n?.Zhoda, "0.00", 9),
        ("RI knižnice", (ri, n) => n?.RiKniznica, "0", 8),
        ("ΔRI", (ri, n) => n?.DRi, "0", 7),
        ("Stav návrhu", (ri, n) => n?.Stav, "@", 20),
        ("Najlepšia kyselina", (ri, n) => n?.NajKyselina, "@", 24),
        ("Zhoda kyseliny", (ri, n) => n?.ZhodaKyseliny, "0.00", 9),
        ("Ďalší kandidáti", (ri, n) => n?.Alternativy, "@", 50),
        ("Poznámka k návrhu", (ri, n) => n?.Poznamka, "@", 30),
        ("Zdroj spektra", (ri, n) => n?.Zdroj, "@", 30),
    };

    static void ZapisHodnotu(IXLCell c, object? val, string fmt)
    {
        switch (val)
        {
            case null: break;
            case double d: c.Value = d; c.Style.NumberFormat.Format = fmt; break;
            case int i: c.Value = i; break;
            default: c.Value = val.ToString(); break;
        }
    }

    static string K(double x) => x.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

    static string Bezpecne(string s, int max)
    {
        foreach (var ch in new[] { ':', '\\', '/', '?', '*', '[', ']' }) s = s.Replace(ch, '_');
        return s.Length > max ? s[..max] : s;
    }

    static string Info(Dictionary<string, string> d, string subor)
    {
        var parts = new List<string> { subor };
        foreach (var k in new[] { "Názov vzorky", "Typ", "Vial", "Meranie" })
            if (d.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v)) parts.Add($"{k.ToLower()}: {v}");
        return string.Join(" – ", parts);
    }

    public static byte[] Zostav(List<Vysledok> vys, Nastavenia o)
    {
        using var wb = new XLWorkbook();
        // bez mena používateľa OS v metadátach súboru
        wb.Properties.Author = "GcmsPorovnanie"; wb.Properties.LastModifiedBy = "GcmsPorovnanie"; wb.Properties.Company = "";
        // --- Prehľad cez všetky vzorky ---
        var p = wb.AddWorksheet("Prehľad");
        p.Cell(1, 1).Value = "Porovnanie vzoriek so štandardom (GC-MS)";
        p.Cell(1, 1).Style.Font.Bold = true; p.Cell(1, 1).Style.Font.FontSize = 13;
        if (vys.Count > 0) p.Cell(2, 1).Value = "Štandard: " + Info(vys[0].InfoStd, vys[0].Standard);
        p.Cell(3, 1).Value = $"Tolerancia ±{K(o.Tol)} s, krok {K(o.Krok)} s, prah zhody spektra {K(o.CosOk)}, signál > {K(o.Prom)}× šum, dekonvolúcia: {(o.Dekonvolucia ? "áno" : "nie")}";
        int r0 = 5;
        p.Cell(r0, 1).Value = "Vzorka"; p.Cell(r0, 2).Value = "Píkov v tolerancii"; p.Cell(r0, 3).Value = "Neisté";
        p.Cell(r0, 4).Value = "Nenájdené / iné spektrum"; p.Cell(r0, 5).Value = "Mimo tolerancie"; p.Cell(r0, 6).Value = "Spoločný posun τ* (s)";
        p.Cell(r0, 7).Value = "Píky len vo vzorke"; p.Cell(r0, 8).Value = "Záver";
        p.Row(r0).Style.Font.Bold = true;
        int rr = r0 + 1;
        foreach (var v in vys)
        {
            int ntol = v.Pocty["v tolerancii"] + v.Pocty["v tolerancii (rameno)"] + v.Pocty["v tolerancii (dekonvolúcia)"];
            p.Cell(rr, 1).Value = Info(v.InfoVz, v.Vzorka);
            p.Cell(rr, 2).Value = $"{ntol} / {v.Piky.Count}";
            p.Cell(rr, 3).Value = v.Pocty["neisté (dekonvolúcia)"];
            p.Cell(rr, 4).Value = v.Pocty["nenájdený"] + v.Pocty["iné spektrum"];
            p.Cell(rr, 5).Value = v.Pocty["mimo tolerancie"];
            if (v.Posun != null) { p.Cell(rr, 6).Value = v.Posun.Tau; p.Cell(rr, 6).Style.NumberFormat.Format = "0.00"; }
            else p.Cell(rr, 6).Value = "–";
            p.Cell(rr, 7).Value = v.Navyse.Count;
            p.Cell(rr, 8).Value = v.Zaver;
            rr++;
        }
        // matica stavov: pík štandardu × vzorka
        rr += 1;
        p.Cell(rr, 1).Value = "Stav píkov štandardu v jednotlivých vzorkách"; p.Cell(rr, 1).Style.Font.Bold = true; rr++;
        bool ident = vys.Count > 0 && vys[0].Piky.Any(x => x.Ri != null || x.Navrh != null);
        int c0 = ident ? 5 : 3;
        p.Cell(rr, 1).Value = "RT štandard (min)"; p.Cell(rr, 2).Value = "Hlavný ión";
        if (ident) { p.Cell(rr, 3).Value = "RI"; p.Cell(rr, 4).Value = "Návrh látky (štandard)"; }
        for (int j = 0; j < vys.Count; j++) p.Cell(rr, c0 + j).Value = vys[j].Vzorka;
        p.Row(rr).Style.Font.Bold = true; rr++;
        if (vys.Count > 0)
            for (int i = 0; i < vys[0].Piky.Count; i++)
            {
                var p0 = vys[0].Piky[i];
                p.Cell(rr, 1).Value = p0.RtStdMin; p.Cell(rr, 1).Style.NumberFormat.Format = "0.00";
                p.Cell(rr, 2).Value = p0.HlavnyIon;
                if (ident)
                {
                    if (p0.Ri != null) { p.Cell(rr, 3).Value = p0.Ri.Value; p.Cell(rr, 3).Style.NumberFormat.Format = "0"; }
                    if (p0.Navrh != null)
                    {
                        var cn = p.Cell(rr, 4); cn.Value = $"{p0.Navrh.Nazov} ({p0.Navrh.Stav}, {p0.Navrh.Zhoda:0.00})";
                        cn.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + FarbaNavrhu(p0.Navrh.Stav));
                    }
                }
                for (int j = 0; j < vys.Count; j++)
                {
                    var pk = vys[j].Piky.ElementAtOrDefault(i); if (pk == null) continue;
                    var c = p.Cell(rr, c0 + j); c.Value = pk.Stav;
                    c.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + Farba[pk.Stav]);
                }
                rr++;
            }
        p.Column(1).Width = 60; p.Column(2).Width = 12;
        for (int j = 3; j <= Math.Max(8, c0 - 1 + vys.Count); j++) p.Column(j).Width = 24;
        if (ident) { p.Column(4).Width = 46; }
        p.Column(8).Width = Math.Max(24, vys.Count >= 6 ? 24 : 70);
        for (int q = r0 + 1; q < r0 + 1 + vys.Count; q++) p.Cell(q, 8).Style.Alignment.WrapText = true;
        p.Row(r0).Style.Alignment.WrapText = true;
        p.PageSetup.PageOrientation = XLPageOrientation.Landscape; p.PageSetup.FitToPages(1, 0);

        // --- hárky pre každú vzorku ---
        var pouzite = new HashSet<string> { "Prehľad" };
        foreach (var v in vys)
        {
            var baseName = Bezpecne(Path.GetFileNameWithoutExtension(v.Vzorka), 24);
            string nm = baseName; int c2 = 2;
            while (pouzite.Contains(nm + " píky")) nm = baseName[..Math.Min(baseName.Length, 21)] + "_" + c2++;
            pouzite.Add(nm + " píky");
            HarokPiky(wb.AddWorksheet(nm + " píky"), v, o);
            HarokNavyse(wb.AddWorksheet(nm + " navyše"), v);
        }
        if (vys.Any(v => v.RiInfo != null || v.KniznicaInfo != null)) HarokIdentifikacia(wb.AddWorksheet("Identifikácia"), vys);
        var a = wb.AddWorksheet("O programe");
        a.Cell(1, 1).Value = "GC-MS porovnanie vzorky so štandardom, verzia " + Program.Verzia;
        a.Cell(3, 1).Value = "RawFileReader reading tool. Copyright © 2016 by Thermo Fisher Scientific, Inc. All rights reserved.";
        a.Cell(4, 1).Value = "Knižnicu RawFileReader nie je dovolené ďalej šíriť (licencia Thermo Fisher, priečinok LICENCIE).";
        a.Cell(5, 1).Value = "Grafy: uPlot (MIT). Excel: ClosedXML (MIT). OpenMcdf (MPL-2.0), CommandLineParser (MIT).";
        a.Cell(6, 1).Value = "Priložené knižnice spektier pochádzajú z verejných zdrojov (MS-DIAL / MoNA, HMDB, NIST WebBook) – podmienky ich použitia sú v kniznice/README.md.";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    static void HarokPiky(IXLWorksheet ws, Vysledok v, Nastavenia o)
    {
        ws.Cell(1, 1).Value = "Štandard: " + Info(v.InfoStd, v.Standard);
        ws.Cell(2, 1).Value = "Vzorka: " + Info(v.InfoVz, v.Vzorka);
        ws.Cell(3, 1).Value = v.Zaver;
        if (v.Posun != null)
            ws.Cell(4, 1).Value = $"Spoločný posun τ* = {v.Posun.Tau:0.00} s (priemerná korelácia tvaru {v.Posun.R:0.000}, {v.Posun.N} píkov); posun vrcholov medián {v.Posun.Median:0.00} s, rozsah {v.Posun.Min:0.00} až {v.Posun.Max:0.00} s. Korelácia TIC: {v.TicNaj.R:0.00}.";
        var hdr = new (string h, Func<PikRiadok, object?> f, string fmt, double w)[]
        {
            ("RT štandard (min)", r => r.RtStdMin, "0.00", 9), ("RT štandard (s)", r => r.RtStdS, "0.00", 9),
            ("Výška píku štd. (TIC)", r => r.VyskaStd, "#,##0", 12), ("Hlavný ión m/z", r => r.HlavnyIon, "0", 8),
            ("5 hlavných iónov", r => r.Ione, "@", 20), ($"Posun τ podľa tvaru píku (s), krok {o.Krok}", r => r.TauEic, "0.00", 11),
            ("Korelácia tvaru pri τ", r => r.REic, "0.000", 10), ("Korelácia tvaru pri τ = 0", r => r.REic0, "0.000", 10),
            ("Vrchol vo vzorke (s)", r => r.VrcholVz, "0.00", 10), ("Posun vrcholu (s)", r => r.Posun, "0.00", 9),
            ("Podobnosť spektra vo vrchole (kosínus)", r => r.CosVrchol, "0.00", 11),
            ("Výška hlavného iónu vzorka", r => r.VyskaVz, "#,##0", 12), ("Šum iónu vo vzorke", r => r.Sum, "#,##0", 9),
            ("Pomer výšok iónu vzorka/štd.", r => r.Pomer, "0.000", 10), ("Výsledok", r => r.Stav, "@", 18),
            ("Dekonvolúcia: najlepšia zhoda spektra", r => r.DekCos, "0.00", 11),
            ("Dekonvolúcia: posun vrcholu zložky (s)", r => r.DekPosun, "0.00", 11),
            ("Dekonvolúcia: nájdené pri k zložkách", r => r.DekK, "@", 10),
            ("Dekonvolúcia: podrobnosti", r => r.DekDetail, "@", 40),
            ("Najlepšia zhoda spektra kdekoľvek vo vzorke", r => r.CosKdekolvek, "0.00", 11),
            ("Kde (RT min)", r => r.RtKdekolvekMin, "0.00", 9),
        };
        bool ident = v.Piky.Any(x => x.Ri != null || x.Navrh != null);
        if (ident) hdr = hdr.Concat(NavrhStlpce.Select(n => (n.h, (Func<PikRiadok, object?>)(r => n.f(r.Ri, r.Navrh)), n.fmt, n.w))).ToArray();
        int h0 = 6;
        for (int j = 0; j < hdr.Length; j++)
        {
            var c = ws.Cell(h0, j + 1); c.Value = hdr[j].h; c.Style.Font.Bold = true;
            c.Style.Alignment.WrapText = true; c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            ws.Column(j + 1).Width = hdr[j].w;
        }
        int r = h0 + 1;
        foreach (var pk in v.Piky)
        {
            for (int j = 0; j < hdr.Length; j++)
            {
                var c = ws.Cell(r, j + 1); var val = hdr[j].f(pk);
                switch (val)
                {
                    case null: break;
                    case double d: c.Value = d; c.Style.NumberFormat.Format = hdr[j].fmt; break;
                    case int i: c.Value = i; break;
                    default: c.Value = val.ToString(); break;
                }
            }
            ws.Cell(r, 15).Style.Fill.BackgroundColor = XLColor.FromHtml("#" + Farba[pk.Stav]);
            if (ident && pk.Navrh != null) ws.Cell(r, 21 + 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#" + FarbaNavrhu(pk.Navrh.Stav));
            r++;
        }
        ws.SheetView.FreezeRows(h0);
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);
    }

    static void HarokIdentifikacia(IXLWorksheet ws, List<Vysledok> vys)
    {
        ws.Cell(1, 1).Value = "Identifikácia látok – retenčný index a návrh z knižníc spektier"; ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 13;
        int r = 3;
        var riInfo = vys.Select(v => v.RiInfo).Where(x => x != null).Distinct().ToList();
        var kInfo = vys.Select(v => v.KniznicaInfo).Where(x => x != null).Distinct().ToList();
        foreach (var t in riInfo) { ws.Cell(r, 1).Value = "Retenčný index: " + t; ws.Cell(r, 1).Style.Alignment.WrapText = true; r++; }
        foreach (var t in kInfo) { ws.Cell(r, 1).Value = "Knižnice: " + t; ws.Cell(r, 1).Style.Alignment.WrapText = true; r++; }
        r++;
        string[] pozn =
        {
            "Stav návrhu: „vlastná knižnica – zhoda“ = spektrum zodpovedá záznamu vlastnej knižnice v tolerancii RT/RI; „pravdepodobne“ = zhoda spektra ≥ prah a RI sedí v rozsahu kalibrácie; „možno“ = zhoda bez RI, mimo rozsahu RI, RI na hrane, duplicitné priradenie, alebo nižšia zhoda; „neurčené“ = žiadna dostatočná zhoda.",
            "Kandidáti sa porovnávajú s celou knižnicou (nielen s kyselinami) a zoraďujú podľa zhody spektra mínus 0,002 × |ΔRI|.",
            "Je to návrh na kontrolu, nie identifikácia. Izoméry (D/L, cis/trans, citrát/izocitrát) sa spektrom nerozlíšia; spektrá z iontovej pasce sa od knižničných mierne líšia.",
        };
        foreach (var t in pozn) { ws.Cell(r, 1).Value = t; ws.Cell(r, 1).Style.Alignment.WrapText = true; r++; }
        var alk = vys.FirstOrDefault(v => v.Alkany.Count > 0)?.Alkany;
        if (alk != null)
        {
            r++;
            ws.Cell(r, 1).Value = "Alkány (meranie alkánov)"; ws.Cell(r, 1).Style.Font.Bold = true; r++;
            ws.Cell(r, 1).Value = "Alkán"; ws.Cell(r, 2).Value = "RT (min)"; ws.Cell(r, 3).Value = "Výška (TIC)"; ws.Cell(r, 4).Value = "Zhoda so vzorom n-alkánu";
            ws.Row(r).Style.Font.Bold = true; r++;
            foreach (var x in alk)
            {
                ws.Cell(r, 1).Value = "C" + x.C;
                ws.Cell(r, 2).Value = x.RtMin; ws.Cell(r, 2).Style.NumberFormat.Format = "0.000";
                ws.Cell(r, 3).Value = x.Vyska; ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0";
                ws.Cell(r, 4).Value = x.Zhoda; ws.Cell(r, 4).Style.NumberFormat.Format = "0.00";
                r++;
            }
        }
        ws.Column(1).Width = 120; ws.Columns(2, 4).Width = 14;
    }

    static void HarokNavyse(IXLWorksheet ws, Vysledok v)
    {
        ws.Cell(1, 1).Value = $"Píky vzorky {v.Vzorka}, ktoré nemajú pík štandardu v tolerancii";
        ws.Cell(1, 1).Style.Font.Bold = true;
        string[] h = { "RT vzorka (min)", "RT vzorka (s)", "Výška píku (TIC, bez pozadia)", "5 hlavných iónov", "Najbližší pík štandardu (s)" };
        for (int j = 0; j < h.Length; j++) { ws.Cell(3, j + 1).Value = h[j]; ws.Cell(3, j + 1).Style.Font.Bold = true; }
        bool ident = v.Navyse.Any(x => x.Ri != null || x.Navrh != null);
        if (ident)
            for (int j = 0; j < NavrhStlpce.Length; j++)
            {
                var c = ws.Cell(3, 6 + j); c.Value = NavrhStlpce[j].h; c.Style.Font.Bold = true;
                ws.Column(6 + j).Width = NavrhStlpce[j].w;
            }
        int r = 4;
        foreach (var e in v.Navyse)
        {
            ws.Cell(r, 1).Value = e.RtMin; ws.Cell(r, 1).Style.NumberFormat.Format = "0.00";
            ws.Cell(r, 2).Value = e.RtS; ws.Cell(r, 2).Style.NumberFormat.Format = "0.00";
            ws.Cell(r, 3).Value = e.Vyska; ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0";
            ws.Cell(r, 4).Value = e.Ione;
            ws.Cell(r, 5).Value = e.NajblizsiStd; ws.Cell(r, 5).Style.NumberFormat.Format = "0.00";
            if (ident)
            {
                for (int j = 0; j < NavrhStlpce.Length; j++) ZapisHodnotu(ws.Cell(r, 6 + j), NavrhStlpce[j].f(e.Ri, e.Navrh), NavrhStlpce[j].fmt);
                if (e.Navrh != null) ws.Cell(r, 6 + 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#" + FarbaNavrhu(e.Navrh.Stav));
            }
            r++;
        }
        ws.Columns(1, 5).Width = 16;
        ws.Row(3).Style.Alignment.WrapText = true;
    }
}
