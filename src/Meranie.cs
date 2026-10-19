using System.Globalization;
using System.IO.Compression;
using System.Xml;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoFisher.CommonCore.RawFileReader;

namespace GcmsPorovnanie;

/// <summary>Jedno GC-MS meranie: MS1 skeny, čas (s), TIC a spektrá zbinované na celé m/z.</summary>
public sealed class Meranie
{
    public string Nazov = "";
    public double[] Rt = Array.Empty<double>();     // s
    public double[] Tic = Array.Empty<double>();
    public double[][] Spec = Array.Empty<double[]>(); // [sken][m/z - MzMin]
    public int MzMin;
    public int MzMax;
    public Dictionary<string, string> Info = new();

    public int N => Rt.Length;

    static (int lo, int hi) Rozsah(List<double[]> mzs)
    {
        int lo = int.MaxValue, hi = int.MinValue;
        foreach (var a in mzs)
            foreach (var m in a)
            {
                int b = (int)Math.Round(m, MidpointRounding.ToEven);
                if (b < lo) lo = b;
                if (b > hi) hi = b;
            }
        if (lo > hi) { lo = 0; hi = 0; }
        return (lo, hi);
    }

    static Meranie Zostav(string nazov, List<double> rt, List<double> tic, List<double[]> mzs, List<double[]> ints)
    {
        var (lo, hi) = Rozsah(mzs);
        int nb = hi - lo + 1;
        var spec = new double[rt.Count][];
        for (int s = 0; s < rt.Count; s++)
        {
            var v = new double[nb];
            var mz = mzs[s]; var it = ints[s];
            for (int j = 0; j < mz.Length; j++)
            {
                int b = (int)Math.Round(mz[j], MidpointRounding.ToEven) - lo;
                if (b >= 0 && b < nb) v[b] += it[j];
            }
            spec[s] = v;
        }
        return new Meranie { Nazov = nazov, Rt = rt.ToArray(), Tic = tic.ToArray(), Spec = spec, MzMin = lo, MzMax = hi };
    }

    public static Meranie Nacitaj(string cesta, string nazov)
    {
        var ext = Path.GetExtension(cesta).ToLowerInvariant();
        return ext switch
        {
            ".raw" => NacitajRaw(cesta, nazov),
            ".mzml" => NacitajMzml(cesta, nazov),
            _ => throw new InvalidDataException($"Nepodporovaný formát súboru {nazov} (očakáva sa .raw alebo .mzML)."),
        };
    }

    // ---------- Thermo .raw (RawFileReader) ----------
    public static Meranie NacitajRaw(string cesta, string nazov)
    {
        using var raw = RawFileReaderAdapter.FileFactory(cesta);
        if (!raw.IsOpen || raw.IsError)
            throw new InvalidDataException($"Súbor {nazov} sa nedá otvoriť ako Thermo .raw.");
        if (raw.InstrumentCount == 0 || raw.GetInstrumentCountOfType(Device.MS) == 0)
            throw new InvalidDataException($"Súbor {nazov} neobsahuje MS dáta.");
        raw.SelectInstrument(Device.MS, 1);
        var h = raw.RunHeaderEx;
        var rt = new List<double>(); var tic = new List<double>();
        var mzs = new List<double[]>(); var ints = new List<double[]>();
        for (int s = h.FirstSpectrum; s <= h.LastSpectrum; s++)
        {
            var f = raw.GetFilterForScanNumber(s);
            if (f.MSOrder != MSOrderType.Ms) continue;
            var st = raw.GetScanStatsForScanNumber(s);
            double[] mz, it;
            if (st.IsCentroidScan)
            {
                var seg = raw.GetSegmentedScanFromScanNumber(s, st);
                mz = seg.Positions; it = seg.Intensities;
            }
            else
            {
                var c = raw.GetCentroidStream(s, false);
                if (c != null && c.Masses != null && c.Masses.Length > 0) { mz = c.Masses; it = c.Intensities; }
                else
                {
                    var sc = Scan.ToCentroid(Scan.FromFile(raw, s));
                    mz = sc.PreferredMasses; it = sc.PreferredIntensities;
                }
            }
            rt.Add(raw.RetentionTimeFromScanNumber(s) * 60.0);
            tic.Add(st.TIC);
            mzs.Add(mz ?? Array.Empty<double>()); ints.Add(it ?? Array.Empty<double>());
        }
        var m = Zostav(nazov, rt, tic, mzs, ints);
        try
        {
            var si = raw.SampleInformation;
            m.Info["Názov vzorky"] = si.SampleName ?? "";
            if (si.SampleType.ToString() != "Unknown") m.Info["Typ"] = si.SampleType.ToString();
            m.Info["Vial"] = si.Vial ?? "";
            m.Info["Meranie"] = raw.FileHeader.CreationDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            m.Info["Prístroj"] = raw.GetInstrumentData().Model ?? "";
            m.Info["Metóda"] = si.InstrumentMethodFile ?? "";
        }
        catch { /* metadáta nie sú povinné */ }
        return m;
    }

    // ---------- mzML ----------
    public static Meranie NacitajMzml(string cesta, string nazov)
    {
        var rt = new List<double>(); var tic = new List<double>();
        var mzs = new List<double[]>(); var ints = new List<double[]>();
        var nast = new XmlReaderSettings { IgnoreWhitespace = true, IgnoreComments = true, DtdProcessing = DtdProcessing.Ignore };
        using var xr = XmlReader.Create(cesta, nast);
        var info = new Dictionary<string, string>();
        while (xr.Read())
        {
            if (xr.NodeType != XmlNodeType.Element) continue;
            if (xr.LocalName == "spectrum")
            {
                using var sub = xr.ReadSubtree();
                var sp = CitajSpektrum(sub);
                if (sp.msLevel != 1 || sp.rt == null) continue;
                rt.Add(sp.rt.Value); mzs.Add(sp.mz ?? Array.Empty<double>()); ints.Add(sp.it ?? Array.Empty<double>());
                tic.Add(sp.tic ?? (sp.it?.Sum() ?? 0));
            }
            else if (xr.LocalName == "cvParam" && xr.GetAttribute("accession") == "MS:1000031")
                info.TryAdd("Prístroj", xr.GetAttribute("name") ?? "");
        }
        var m = Zostav(nazov, rt, tic, mzs, ints);
        foreach (var kv in info) m.Info[kv.Key] = kv.Value;
        return m;
    }

    static (int msLevel, double? rt, double? tic, double[]? mz, double[]? it) CitajSpektrum(XmlReader r)
    {
        int ms = 1; double? rt = null, tic = null; double[]? mz = null, it = null;
        string? kind = null; bool z = false, d64 = false;
        bool inArray = false;
        while (r.Read())
        {
            if (r.NodeType == XmlNodeType.EndElement && r.LocalName == "binaryDataArray") { inArray = false; kind = null; z = false; d64 = false; continue; }
            if (r.NodeType != XmlNodeType.Element) continue;
            switch (r.LocalName)
            {
                case "binaryDataArray": inArray = true; break;
                case "cvParam":
                    var acc = r.GetAttribute("accession");
                    var val = r.GetAttribute("value");
                    if (inArray)
                    {
                        if (acc == "MS:1000514") kind = "mz";
                        else if (acc == "MS:1000515") kind = "int";
                        else if (acc == "MS:1000574") z = true;
                        else if (acc == "MS:1000523") d64 = true;
                        else if (acc == "MS:1000521") d64 = false;
                    }
                    else if (acc == "MS:1000511" && val != null) ms = int.Parse(val, CultureInfo.InvariantCulture);
                    else if (acc == "MS:1000016" && val != null)
                    {
                        double t = double.Parse(val, CultureInfo.InvariantCulture);
                        var unit = r.GetAttribute("unitAccession") ?? r.GetAttribute("unitName") ?? "";
                        rt = (unit.Contains("UO:0000031") || unit.Contains("minute")) ? t * 60.0 : t;
                    }
                    else if (acc == "MS:1000285" && val != null) tic = double.Parse(val, CultureInfo.InvariantCulture);
                    break;
                case "binary":
                    var txt = r.ReadElementContentAsString();
                    var arr = Dekoduj(txt, z, d64);
                    if (kind == "mz") mz = arr; else if (kind == "int") it = arr;
                    if (r.NodeType == XmlNodeType.EndElement && r.LocalName == "binaryDataArray") { inArray = false; kind = null; z = false; d64 = false; }
                    break;
            }
        }
        return (ms, rt, tic, mz, it);
    }

    static double[] Dekoduj(string b64, bool zlib, bool d64)
    {
        if (string.IsNullOrWhiteSpace(b64)) return Array.Empty<double>();
        byte[] bytes = Convert.FromBase64String(b64.Trim());
        if (zlib)
        {
            using var ms = new MemoryStream(bytes);
            using var zs = new ZLibStream(ms, CompressionMode.Decompress);
            using var outp = new MemoryStream();
            zs.CopyTo(outp);
            bytes = outp.ToArray();
        }
        if (d64)
        {
            var a = new double[bytes.Length / 8];
            Buffer.BlockCopy(bytes, 0, a, 0, a.Length * 8);
            return a;
        }
        var f = new float[bytes.Length / 4];
        Buffer.BlockCopy(bytes, 0, f, 0, f.Length * 4);
        return Array.ConvertAll(f, x => (double)x);
    }

    /// <summary>Kópia spektier v spoločnom rozsahu m/z [lo, hi].</summary>
    public double[][] SpecVRozsahu(int lo, int hi)
    {
        int nb = hi - lo + 1, off = MzMin - lo;
        var r = new double[N][];
        for (int s = 0; s < N; s++)
        {
            var v = new double[nb];
            var src = Spec[s];
            for (int j = 0; j < src.Length; j++)
            {
                int b = j + off;
                if (b >= 0 && b < nb) v[b] = src[j];
            }
            r[s] = v;
        }
        return r;
    }
}
