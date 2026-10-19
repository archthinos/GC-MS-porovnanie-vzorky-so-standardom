# GC-MS porovnanie vzorky so štandardom

[English](README.md) · **Slovensky**

Program porovná GC-MS merania vzoriek s meraním štandardu. Pri každom píku štandardu zistí, či je vo vzorke tá istá látka v tolerancii posunu v čase (predvolene ±0,5 s, krok 0,05 s).

Výsledok uvidíte v prehliadači: súhrn, tabuľku píkov, grafy a detail píku. Celý výsledok si stiahnete ako Excel.

Program beží len na vašom počítači. Súbory sa nikam neposielajú a netreba nič inštalovať.

Pri prvom spustení program požiada o súhlas s licenciou knižnice Thermo RawFileReader. Súhlas si zapamätá v priečinku používateľa (`GcmsPorovnanie/licencia-thermo-prijata.txt` v adresári aplikačných dát).

Program nie je zdravotnícka pomôcka. Výsledky vždy posúdi odborník podľa validovanej metódy laboratória.

## Spustenie

| Systém | Zip zo stránky Releases | Čo spustiť |
|---|---|---|
| Windows 10/11 | `…-win-x64.zip` | dvojklik na `GcmsPorovnanie.exe` |
| Mac s procesorom Apple (M1–M4) | `…-osx-arm64.zip` | dvojklik na `Spustit.command` |
| Mac s procesorom Intel | `…-osx-x64.zip` | dvojklik na `Spustit.command` |
| Linux (x64 / arm64) | `…-linux-x64.zip`, `…-linux-arm64.zip` | `./GcmsPorovnanie` v termináli |

- Otvorí sa okno konzoly a potom prehliadač s programom. Ak sa prehliadač neotvorí sám, otvorte adresu, ktorú vypíše konzola, napríklad `http://localhost:53712`.
- Program ukončíte tlačidlom **Ukončiť** v prehliadači alebo zatvorením okna konzoly.
- Na iný počítač skopírujte celý priečinok systému. Musí obsahovať program, `wwwroot`, `LICENCIE` a `kniznice` (bez neho nefunguje návrh látok z priložených knižníc). Program šírte len celý a bezplatne. Samotnú knižnicu Thermo RawFileReader nikto nesmie šíriť ďalej (pozri Licencie).
- **Mac:** keď program dostanete stiahnutý z internetu alebo e-mailom, macOS ho môže zablokovať. `Spustit.command` toto blokovanie sám odstráni. Ak to nepomôže, kliknite pravým tlačidlom na `Spustit.command` a zvoľte **Otvoriť**.
- **Windows:** ak sa objaví modré okno SmartScreen, kliknite na **Ďalšie informácie** a potom na **Spustiť aj tak**. Program nie je podpísaný komerčným certifikátom.

## Použitie
1. **Štandard:** pretiahnite alebo vyberte jeden súbor `.raw` (Thermo) alebo `.mzML`.
2. **Vzorky:** vyberte jeden alebo viac súborov.
3. Podľa potreby upravte **Nastavenia** a kliknite na **Porovnať**.
4. **Výsledky:** horná tabuľka sumarizuje všetky vzorky. Pod ňou sú záložky jednotlivých vzoriek. Kliknutím na riadok tabuľky píkov zobrazíte detail píku. Grafy priblížite ťahaním myšou, dvojklik vráti pôvodný pohľad.
5. **Stiahnuť Excel:** stiahne hárok „Prehľad“ (stav každého píku v každej vzorke) a pre každú vzorku hárky „píky“ a „navyše“. Pri zapnutej identifikácii pribudnú stĺpce s RI a návrhom látky a hárok „Identifikácia“.

## Identifikácia látok (voliteľné)
V časti **3. Identifikácia látok** si vyberiete, čo chcete použiť. Všetko je voliteľné a jednotlivé možnosti sa dajú kombinovať.
- **Návrh látok z knižnice spektier (.msp).** Spektrum každého píku štandardu a každého píku „len vo vzorke“ sa porovná s knižnicou. Porovnáva sa s celou knižnicou, nielen s kyselinami. Výsledkom je najlepšia látka, kategória (kyselina / aminokyselina / iné), zhoda, ΔRI a ďalší kandidáti. Detail píku ukáže zrkadlové porovnanie spektier.
  - Priložené knižnice sú v priečinku `kniznice` vedľa programu: `TMS_verejne_kniznice.msp` (6 258 spektier TMS derivátov) a `kyseliny_TMS.msp`. Ich pôvod a podmienky sú v `kniznice/README.md`.
  - Ďalšie knižnice `.msp` môžete nahrať, napríklad vlastnú knižnicu zo štandardu.
- **Retenčný index (RI)** spresní návrh podľa času elúcie:
  - **z merania alkánov** (najpresnejšie). Nahrajte meranie zmesi n-alkánov nameranej rovnakou metódou. Program nájde alkány podľa spektra (m/z 57, 71, 85…) a počet uhlíkov určí z molekulových iónov. Ak to nejde, zadajte najnižší alkán ručne. RI sa počíta lineárnou interpoláciou (van den Dool–Kratz), predvolená tolerancia je ±40;
  - **odhadnúť zo štandardu.** Bez alkánov sa RI odhadne z istých zásahov štandardu (zhoda ≥ 0,90) kvadratickou kalibráciou. Je to len hrubý odhad;
  - **nepoužiť RI.**
- **Stav návrhu:**
  - *vlastná knižnica – zhoda*: spektrum zodpovedá vlastnej knižnici v tolerancii RT/RI;
  - *pravdepodobne*: zhoda ≥ 0,85 a RI sedí;
  - *možno*: bez RI, mimo rozsahu RI, RI na hrane, duplicitné priradenie alebo zhoda 0,75–0,85;
  - *neurčené*.
- **Vlastná knižnica zo štandardu.** Tlačidlo vo výsledkoch uloží píky štandardu ako `.msp`: spektrum z vášho prístroja, RT, RI a názov, ktorý doplníte. Pri ďalších vzorkách ju nahrajte medzi knižnice. Má prednosť pred verejnými knižnicami.
- Návrh látky je **pomôcka na kontrolu, nie identifikácia**. Izoméry (D/L, cis/trans, citrát/izocitrát) sa spektrom nerozlíšia.

Overenie (2026-10-16): návrhy pre 46 píkov štandardu a vzorky sa zhodujú s nezávislým skriptom v Pythone. RI zo syntetického merania alkánov C11–C28 sa od odhadu zo štandardu líši o 0–7 jednotiek a počty uhlíkov sa určili správne. Skutočné meranie alkánov zatiaľ nebolo k dispozícii.

## Ako sa porovnáva
- **Píky štandardu** sa hľadajú v TIC po odčítaní pozadia.
- **Pre každý pík:**
  - zoberie sa jeho hlavný ión;
  - posun τ ∈ [−tol, +tol] s krokom 0,05 s sa nájde podľa korelácie tvaru píku, vzorka sa lineárne interpoluje;
  - vrchol píku v štandarde aj vo vzorke sa spresní parabolou;
  - spektrum vo vrchole sa porovná so štandardom kosínusovou podobnosťou, prah je 0,80.
- **Výsledok pre každý pík:** *v tolerancii*, *v tolerancii (rameno)*, *v tolerancii (dekonvolúcia)*, *neisté (dekonvolúcia)*, *mimo tolerancie*, *iné spektrum* alebo *nenájdený*.
- **Dekonvolúcia** sa robí len pri sporných píkoch. Úsek vzorky ±5 s sa rozloží nezápornou faktorizáciou (NMF) na k = 2–5 zložiek. Nájdená zložka sa ráta ako istá, ak sa objaví aspoň pri dvoch hodnotách k a jej zhoda je aspoň 0,85.
- **Spoločný posun τ\*** je maximum priemernej korelácie tvaru cez píky v tolerancii.
- **Píky len vo vzorke** sú píky vzorky, ktoré nemajú pík štandardu do ±tol.

Postup bol overený 2026-10-16 na troch meraniach z Thermo ITQ 1100 a výsledky sa zhodujú s pôvodným skriptom v Pythone:
- štandard proti sebe samému: 28 z 28 píkov;
- slepý pokus (hexán): 0 z 28;
- reálna vzorka: 23 z 28.

**Obmedzenia:**
- Prahy (0,80 / 0,85 / 10× šum) sú zvolené na tomto súbore dát, nie sú validované.
- Pri dekonvolúcii sa čísla medzi behmi mierne líšia, lebo výpočet štartuje z náhodných hodnôt.
- Porovnanie vzorky so štandardom prebieha v čase. RI sa používa len pri návrhu látok.
- Číta len Thermo `.raw` a `.mzML`.

## Zdrojový kód
Zdrojový kód (C#, .NET 8) je v repozitári projektu. Zostavenie pre všetky systémy spustíte príkazom `bash build.sh`. Knižnicu Thermo RawFileReader si skript stiahne z oficiálneho repozitára Thermo (`fetch-thermo.sh`). Anglický popis projektu je v `README.md`.

## Licencie
RawFileReader reading tool. Copyright © 2016 by Thermo Fisher Scientific, Inc. All rights reserved.

Knižnicu RawFileReader od Thermo Fisher je dovolené používať len nekomerčne. Používaním programu súhlasíte s tým, že ju nebudete ďalej šíriť. Celé podmienky sú v `LICENCIE/THERMO_LICENSE.doc`.

Ďalšie použité komponenty: OpenMcdf (MPL-2.0), CommandLineParser (MIT), uPlot (MIT), ClosedXML (MIT), .NET (MIT).

Priložené knižnice spektier (`kniznice/`) pochádzajú z verejných zdrojov (MS-DIAL/MoNA, HMDB, NIST WebBook) s vlastnými podmienkami použitia, pozri `kniznice/README.md`. Nie sú súčasťou licencie MIT.

## Autor

Vyvinul **Martin Pohorelský** ([martenech](https://github.com/martenech)). Zdrojový kód je pod licenciou MIT, pozri [`LICENSE`](LICENSE).
