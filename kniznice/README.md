# EI spectral libraries (`kniznice/`)

GcmsPorovnanie loads all `*.msp` files from this folder. In the section **Identifikácia látok** (compound identification) you choose which of them are used. Further `.msp` libraries (including your own in-house library from the standard) can be uploaded directly in the program.

| File | Content |
|---|---|
| `TMS_verejne_kniznice.msp` | 6,258 EI spectra of TMS derivatives (3,386 compounds) from public sources, without duplicates. `RETENTIONINDEX` is the median Kovats RI of the compound in the MS-DIAL library (non-polar column). `COMMENT: zdroj=` gives the origin of the spectrum (*zdroj* = source). |
| `kyseliny_TMS.msp` | 20 spectra: lactate 2TMS, fumarate 2TMS (MS-DIAL, HMDB, NIST WebBook) and vanillactate 3TMS (NIST WebBook). |

## Data origin and terms of use

The sources have different licenses. Check them before publishing the libraries or using them commercially. The libraries are not covered by this project's MIT license.

- **MS-DIAL "GCMS DB-Public-KovatsRI-VS3"** (RIKEN, Zenodo, record 10951913). It is compiled from public sources (MoNA, MassBank, Golm Metabolome Database, Fiehn lab and others). Each spectrum keeps the license of its original source (e.g. CC BY, CC BY-NC).
- **HMDB 5.0 – experimental GC-MS spectra** (hmdb.ca; the copy used is the file from 07/2022 from the Internet Archive). According to HMDB, the database is freely available to the public. Commercial use or redistribution (in whole or in part) requires explicit permission of the authors and citation of HMDB.
- **NIST Chemistry WebBook** (NIST Standard Reference Data, © U.S. Secretary of Commerce): spectra of lactate 2TMS, fumarate 2TMS and vanillactate 3TMS. Redistribution of NIST data may be restricted.

Selection from the public libraries: TMS derivatives only (name contains TMS, or ion m/z 147 ≥ 5 % / m/z 73 ≥ 20 %), without TBS derivatives and plain esters.

## `.msp` format

```
NAME: Fumaric acid (2TMS)
RETENTIONINDEX: 1344.0        (optional – Kovats RI)
RETENTIONTIME: 12.3210        (optional – in minutes; typical for in-house libraries)
COMMENT: zdroj=…              (optional – source)
Num Peaks: 3
147 860
245 999
246 192
```

Records are separated by a blank line. A record with `RETENTIONTIME` is treated as an **in-house library** entry. If RI is missing, it is matched within an RT tolerance (default ±6 s). If the spectral match is at least 0.80, the in-house library takes precedence over the public ones.

**An in-house library from the standard** is created in the program with the button *Vlastná knižnica zo štandardu…* (in-house library from the standard) in the results. It saves the spectra of the standard's peaks from your instrument with RT, RI (if available) and the names you enter.
