'use strict';
const $ = s => document.querySelector(s);
const STAV_CLS = {
  'v tolerancii': 'st-ok', 'v tolerancii (rameno)': 'st-ok2', 'v tolerancii (dekonvolúcia)': 'st-ok2',
  'neisté (dekonvolúcia)': 'st-warn', 'mimo tolerancie': 'st-warn', 'iné spektrum': 'st-bad', 'nenájdený': 'st-bad',
};
const STAV_FARBA = {
  'v tolerancii': '#2e9e4f', 'v tolerancii (rameno)': '#7cbf5a', 'v tolerancii (dekonvolúcia)': '#7cbf5a',
  'neisté (dekonvolúcia)': '#e0a100', 'mimo tolerancie': '#e07b00', 'iné spektrum': '#d0342c', 'nenájdený': '#d0342c',
};
const DEF = { tol: 0.5, krok: 0.05, cosOk: 0.80, cosIsty: 0.85, prom: 10, dekonvolucia: true };
const DEF_I = { riTol: 40, mzOd: 85, cosNavrh: 0.85, cosMozno: 0.75, rtTolVlastna: 6 };
let fStd = null, fVz = [], fKniz = [], fAlk = null, data = null, aktivna = 0, plots = [];
const NV_CLS = s => !s ? 'nv-none' : s.startsWith('vlastná') ? 'nv-own' : s === 'pravdepodobne' ? 'nv-ok' : s.startsWith('možno') ? 'nv-maybe' : 'nv-none';

const fmt = (x, d = 2) => (x === null || x === undefined || Number.isNaN(x)) ? '–' : Number(x).toFixed(d).replace('.', ',');
const fmtInt = x => (x === null || x === undefined) ? '–' : Math.round(x).toLocaleString('sk-SK');
const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const okExt = f => /\.(raw|mzml)$/i.test(f.name);
const okMsp = f => /\.(msp|txt)$/i.test(f.name);

// ---------- vstup ----------
function kresliZoznamy() {
  $('#listStd').innerHTML = fStd ? `<li><span>${esc(fStd.name)} <small>(${(fStd.size / 1e6).toFixed(1)} MB)</small></span><button data-x="std" title="Odstrániť">✕</button></li>` : '';
  $('#listVz').innerHTML = fVz.map((f, i) => `<li><span>${esc(f.name)} <small>(${(f.size / 1e6).toFixed(1)} MB)</small></span><button data-x="${i}" title="Odstrániť">✕</button></li>`).join('');
  $('#listKniz').innerHTML = fKniz.map((f, i) => `<li><span>${esc(f.name)} <small>(${(f.size / 1e6).toFixed(2)} MB)</small></span><button data-x="k${i}" title="Odstrániť">✕</button></li>`).join('');
  $('#listAlk').innerHTML = fAlk ? `<li><span>${esc(fAlk.name)} <small>(${(fAlk.size / 1e6).toFixed(1)} MB)</small></span><button data-x="alk" title="Odstrániť">✕</button></li>` : '';
  $('#btnRun').disabled = !(fStd && fVz.length);
}
function stavIdent() {
  const ri = document.querySelector('input[name=iRi]:checked').value;
  $('#iKnizBox').classList.toggle('off', !$('#iNavrh').checked);
  $('#iAlkBox').classList.toggle('off', ri !== 'alkany');
}
function pridaj(files, ktory) {
  const test = ktory === 'kniz' ? okMsp : okExt;
  const ok = [...files].filter(test), zle = [...files].filter(f => !test(f));
  if (zle.length) hlasenie(`Preskočené (nie je ${ktory === 'kniz' ? '.msp' : '.raw ani .mzML'}): ${zle.map(f => f.name).join(', ')}`, true);
  if (ktory === 'std') { if (ok[0]) fStd = ok[0]; }
  else if (ktory === 'alk') { if (ok[0]) { fAlk = ok[0]; document.querySelector('input[name=iRi][value=alkany]').checked = true; stavIdent(); } }
  else if (ktory === 'kniz') { for (const f of ok) if (!fKniz.some(g => g.name === f.name && g.size === f.size)) fKniz.push(f); if (ok.length) { $('#iNavrh').checked = true; stavIdent(); } }
  else for (const f of ok) if (!fVz.some(g => g.name === f.name && g.size === f.size)) fVz.push(f);
  kresliZoznamy();
stavIdent();
}
function dropZone(el, input, ktory) {
  input.addEventListener('change', e => { pridaj(e.target.files, ktory); input.value = ''; });
  el.addEventListener('dragover', e => { e.preventDefault(); el.classList.add('over'); });
  el.addEventListener('dragleave', () => el.classList.remove('over'));
  el.addEventListener('drop', e => { e.preventDefault(); el.classList.remove('over'); pridaj(e.dataTransfer.files, ktory); });
}
dropZone($('#dropStd'), $('#fStd'), 'std');
dropZone($('#dropVz'), $('#fVz'), 'vz');
dropZone($('#dropKniz'), $('#fKniz'), 'kniz');
dropZone($('#dropAlk'), $('#fAlk'), 'alk');
document.addEventListener('click', e => {
  const b = e.target.closest('button[data-x]'); if (!b) return;
  const x = b.dataset.x;
  if (x === 'std') fStd = null; else if (x === 'alk') fAlk = null;
  else if (x.startsWith('k')) fKniz.splice(+x.slice(1), 1); else fVz.splice(+x, 1);
  kresliZoznamy();
stavIdent();
});
$('#iNavrh').onchange = stavIdent;
document.querySelectorAll('input[name=iRi]').forEach(r => r.onchange = stavIdent);
fetch('/api/kniznice').then(r => r.json()).then(list => {
  $('#iKniz').innerHTML = list.length ? list.map(k => `<label><input type="checkbox" class="kn" value="${esc(k.nazov)}" checked> ${esc(k.nazov)} <small>(${k.spektier} spektier)</small></label>`).join('')
    : '<span class="small">žiadne (priečinok kniznice chýba)</span>';
}).catch(() => { $('#iKniz').innerHTML = '<span class="small">nepodarilo sa načítať</span>'; });
function nastavenia() {
  return {
    tol: +$('#sTol').value, krok: +$('#sKrok').value, cosOk: +$('#sCos').value, cosIsty: +$('#sCosIsty').value,
    prom: +$('#sProm').value, dekonvolucia: $('#sDek').checked,
    navrhLatok: $('#iNavrh').checked,
    kniznice: [...document.querySelectorAll('input.kn:checked')].map(x => x.value),
    riZdroj: document.querySelector('input[name=iRi]:checked').value,
    prvyAlkan: +$('#iPrvy').value || 0,
    riTol: +$('#iRiTol').value, mzOd: +$('#iMzOd').value, cosNavrh: +$('#iCosNavrh').value, cosMozno: +$('#iCosMozno').value,
    rtTolVlastna: +$('#iRtTol').value,
  };
}
$('#btnReset').onclick = () => {
  $('#sTol').value = DEF.tol; $('#sKrok').value = DEF.krok; $('#sCos').value = DEF.cosOk.toFixed(2);
  $('#sCosIsty').value = DEF.cosIsty.toFixed(2); $('#sProm').value = DEF.prom; $('#sDek').checked = DEF.dekonvolucia;
  $('#iRiTol').value = DEF_I.riTol; $('#iMzOd').value = DEF_I.mzOd; $('#iCosNavrh').value = DEF_I.cosNavrh.toFixed(2);
  $('#iCosMozno').value = DEF_I.cosMozno.toFixed(2); $('#iRtTol').value = DEF_I.rtTolVlastna; $('#iPrvy').value = '';
};
function hlasenie(t, chyba = false) { const s = $('#stav'); s.textContent = t; s.classList.toggle('err', chyba); }

// ---------- spustenie ----------
$('#btnRun').onclick = async () => {
  const n = nastavenia();
  if (!(n.tol > 0) || !(n.krok > 0) || n.krok > n.tol) { hlasenie('Tolerancia a krok musia byť kladné a krok ≤ tolerancia.', true); return; }
  if (n.navrhLatok && !n.kniznice.length && !fKniz.length) { hlasenie('Návrh látok je zapnutý – vyberte aspoň jednu knižnicu.', true); return; }
  if (n.riZdroj === 'alkany' && !fAlk) { hlasenie('Zvolili ste RI z merania alkánov – nahrajte meranie alkánov.', true); return; }
  const fd = new FormData();
  fd.append('standard', fStd);
  fVz.forEach(f => fd.append('vzorky', f));
  if (n.riZdroj === 'alkany' && fAlk) fd.append('alkany', fAlk);
  if (n.navrhLatok) fKniz.forEach(f => fd.append('kniznice', f));
  fd.append('nastavenia', JSON.stringify(n));
  $('#btnRun').disabled = true; $('#prog').classList.remove('hidden'); $('#progBar').style.width = '3%';
  hlasenie('Nahrávam súbory…');
  try {
    const r = await fetch('/api/porovnaj', { method: 'POST', body: fd });
    const j = await r.json();
    if (!r.ok) throw new Error(j.chyba || 'Chyba servera');
    await sleduj(j.id);
  } catch (e) {
    hlasenie('Chyba: ' + e.message, true);
  } finally {
    $('#btnRun').disabled = !(fStd && fVz.length);
  }
};
async function sleduj(id) {
  for (;;) {
    await new Promise(r => setTimeout(r, 700));
    const s = await (await fetch('/api/stav/' + id)).json();
    $('#progBar').style.width = Math.max(3, 100 * s.hotovo / s.spolu) + '%';
    hlasenie(s.stav);
    if (s.chyba) throw new Error(s.chyba);
    if (s.koniec) break;
  }
  data = await (await fetch('/api/vysledok/' + id)).json();
  $('#btnExcel').href = '/api/excel/' + id;
  $('#btnVlastna').classList.toggle('hidden', !data.vysledky.length);
  $('#prog').classList.add('hidden');
  hlasenie(`Hotovo – porovnaných vzoriek: ${data.vysledky.length}.`);
  aktivna = 0;
  kresliVysledky();
}

// ---------- výsledky ----------
const pocetTol = v => v.pocty['v tolerancii'] + v.pocty['v tolerancii (rameno)'] + v.pocty['v tolerancii (dekonvolúcia)'];
const infoText = (inf, subor) => [subor, inf['Názov vzorky'], inf['Typ'] && inf['Typ'] !== 'Unknown' ? inf['Typ'] : '', inf['Vial'] ? 'vial ' + inf['Vial'] : '', inf['Meranie']].filter(Boolean).join(' · ');

function kresliVysledky() {
  $('#vysledky').classList.remove('hidden');
  const v0 = data.vysledky[0];
  $('#tblPrehlad').innerHTML = `<thead><tr><th>Vzorka</th><th>V tolerancii</th><th>Neisté</th><th>Nenájdené / iné spektrum</th><th>Mimo tol.</th><th>Spoločný posun τ*</th><th>Píky len vo vzorke</th><th>Záver</th></tr></thead><tbody>` +
    data.vysledky.map((v, i) => `<tr class="click" data-i="${i}"><td>${esc(infoText(v.infoVz, v.vzorka))}</td>
      <td class="num"><b>${pocetTol(v)}</b> / ${v.piky.length}</td><td class="num">${v.pocty['neisté (dekonvolúcia)']}</td>
      <td class="num">${v.pocty['nenájdený'] + v.pocty['iné spektrum']}</td><td class="num">${v.pocty['mimo tolerancie']}</td>
      <td class="num">${v.posun ? fmt(v.posun.tau) + ' s' : '–'}</td><td class="num">${v.navyse.length}</td><td>${esc(v.zaver)}</td></tr>`).join('') + '</tbody>';
  $('#tblPrehlad').querySelectorAll('tr.click').forEach(tr => tr.onclick = () => { aktivna = +tr.dataset.i; kresliTabs(); kresliDetail(); });
  kresliTabs(); kresliDetail();
  $('#vysledky').scrollIntoView({ behavior: 'smooth' });
}
function kresliTabs() {
  $('#tabs').innerHTML = data.vysledky.map((v, i) => `<button class="${i === aktivna ? 'act' : ''}" data-t="${i}">${esc(v.vzorka)}</button>`).join('');
  $('#tabs').querySelectorAll('button').forEach(b => b.onclick = () => { aktivna = +b.dataset.t; kresliTabs(); kresliDetail(); });
}

function znicGrafy() { plots.forEach(p => p.destroy()); plots = []; }

// plugin: zvislé čiary na pozíciách píkov
function ciary(polohy) {
  return {
    hooks: {
      draw: u => {
        const ctx = u.ctx; ctx.save(); ctx.lineWidth = 1.5 * devicePixelRatio;
        for (const p of polohy) {
          const x = u.valToPos(p.x, 'x', true);
          if (x < u.bbox.left || x > u.bbox.left + u.bbox.width) continue;
          ctx.strokeStyle = p.c; ctx.globalAlpha = p.a ?? 0.45;
          ctx.beginPath(); ctx.moveTo(x, u.bbox.top); ctx.lineTo(x, u.bbox.top + u.bbox.height); ctx.stroke();
        }
        ctx.restore();
      },
    },
  };
}
function pasmo(x0, x1) {
  return { hooks: { drawClear: u => {
    const ctx = u.ctx, a = u.valToPos(x0, 'x', true), b = u.valToPos(x1, 'x', true);
    ctx.save(); ctx.fillStyle = 'rgba(31,111,235,0.10)'; ctx.fillRect(a, u.bbox.top, b - a, u.bbox.height); ctx.restore();
  } } };
}

function graf(el, title, x, ys, labels, colors, plugins = [], h = 220, sync = null) {
  const w = Math.max(400, el.clientWidth || 800);
  const opts = {
    title, width: w, height: h, plugins,
    scales: { x: { time: false } },
    axes: [{ label: 'RT (min)', values: (u, v) => v.map(t => fmt(t, 2)) }, { size: 70, values: (u, v) => v.map(t => t >= 1e6 ? (t / 1e6).toFixed(1) + ' M' : t >= 1e3 ? (t / 1e3).toFixed(0) + ' k' : t.toFixed(t < 1 ? 2 : 0)) }],
    series: [{ label: 'RT (min)', value: (u, v) => v == null ? '–' : fmt(v, 3) }, ...ys.map((_, i) => ({ label: labels[i], stroke: colors[i], width: 1.2, points: { show: false } }))],
    cursor: sync ? { sync: { key: sync, setSeries: false }, drag: { x: true, y: false } } : { drag: { x: true, y: false } },
  };
  const p = new uPlot(opts, [x, ...ys], el);
  plots.push(p);
  return p;
}

function kresliDetail() {
  znicGrafy();
  const v = data.vysledky[aktivna], n = data.nastavenia;
  const legenda = Object.entries({ 'v tolerancii': '#2e9e4f', 'v tolerancii (rameno / dekonvolúcia)': '#7cbf5a', 'neisté / mimo tolerancie': '#e0a100', 'nenájdený / iné spektrum': '#d0342c' })
    .map(([k, c]) => `<span><i style="background:${c}"></i>${k}</span>`).join('');
  $('#detail').innerHTML = `
    <div class="info"><b>Štandard</b><span>${esc(infoText(v.infoStd, v.standard))}</span>
      <b>Vzorka</b><span>${esc(infoText(v.infoVz, v.vzorka))}</span>
      <b>Nastavenia</b><span>tolerancia ±${fmt(n.tol)} s, krok ${fmt(n.krok)} s, prah zhody ${fmt(n.cosOk)}, signál &gt; ${n.prom}× šum, dekonvolúcia ${n.dekonvolucia ? 'áno' : 'nie'}</span>
      ${v.posun ? `<b>Spoločný posun</b><span>τ* = ${fmt(v.posun.tau)} s (priemerná korelácia tvaru ${fmt(v.posun.r, 3)}, ${v.posun.n} píkov); posun vrcholov medián ${fmt(v.posun.median)} s, rozsah ${fmt(v.posun.min)} až ${fmt(v.posun.max)} s</span>` : ''}
      <b>TIC</b><span>korelácia celých záznamov ${fmt(v.ticNaj.r)} (len informatívne)</span>
      ${v.riInfo ? `<b>Retenčný index</b><span>${esc(v.riInfo)}</span>` : ''}
      ${v.alkany && v.alkany.length ? `<b>Alkány</b><span>${v.alkany.map(a => `C${a.c} ${fmt(a.rtMin)}`).join(' · ')}</span>` : ''}
      ${v.kniznicaInfo ? `<b>Knižnice</b><span>${esc(v.kniznicaInfo)}</span>` : ''}</div>
    <div class="zaver">${esc(v.zaver)}</div>
    <div class="legend">${legenda}<span>Myšou označte úsek grafu na priblíženie, dvojklik = späť.</span></div>
    <div class="plots"><div id="gStd"></div><div id="gVz"></div></div>
    <div class="split">
      <div><h3>Píky štandardu</h3><div class="tblwrap"><table class="tbl" id="tblPiky"></table></div></div>
      <div class="pikdet" id="pikDet"><p class="small">Kliknite na riadok tabuľky – zobrazí sa detail píku (hlavný ión v štandarde a vo vzorke).</p></div>
    </div>
    <h3>Píky len vo vzorke (bez páru v štandarde do ±${fmt(n.tol)} s)</h3>
    ${v.navyse.length ? `<table class="tbl" id="tblNavyse"><thead><tr><th>RT (min)</th><th>RT (s)</th><th>Výška (TIC)</th><th>5 hlavných iónov</th><th>Najbližší pík štandardu (s)</th>${maRi(v) ? '<th>RI</th>' : ''}${maNavrh(v) ? '<th>Návrh látky</th><th>Zhoda</th><th>ΔRI</th><th>Stav návrhu</th>' : ''}</tr></thead><tbody>
      ${v.navyse.map((e, i) => `<tr class="${e.navrh ? 'click' : ''}" data-n="${i}"><td class="num">${fmt(e.rtMin)}</td><td class="num">${fmt(e.rtS)}</td><td class="num">${fmtInt(e.vyska)}</td><td>${esc(e.ione)}</td><td class="num">${fmt(e.najblizsiStd)}</td>
        ${maRi(v) ? `<td class="num">${fmt(e.ri, 0)}</td>` : ''}${maNavrh(v) ? navrhBunky(e.navrh) : ''}</tr>`).join('')}</tbody></table>` : '<p class="small">žiadne</p>'}
    <div class="pikdet" id="navDet"></div>`;
  const pol = v.piky.map(p => ({ x: p.rtStdMin, c: STAV_FARBA[p.stav] }));
  const rtS = v.rtS.map(t => t / 60), rtV = v.rtV.map(t => t / 60);
  graf($('#gStd'), 'Štandard – TIC', rtS, [v.ticS], ['štandard'], ['#1f6feb'], [ciary(pol)], 220, 'tic');
  graf($('#gVz'), 'Vzorka – TIC', rtV, [v.ticV], ['vzorka'], ['#e8730c'], [ciary(pol)], 220, 'tic');
  // synchronizácia zoomu
  const [a, b] = plots;
  const syncX = (src, dst) => src.hooks.setScale = [(u, k) => { if (k === 'x') dst.setScale('x', { min: u.scales.x.min, max: u.scales.x.max }); }];
  a.root.addEventListener('mouseup', () => b.setScale('x', { min: a.scales.x.min, max: a.scales.x.max }));
  b.root.addEventListener('mouseup', () => a.setScale('x', { min: b.scales.x.min, max: b.scales.x.max }));
  a.root.addEventListener('dblclick', () => setTimeout(() => b.setScale('x', { min: rtV[0], max: rtV[rtV.length - 1] }), 0));
  b.root.addEventListener('dblclick', () => setTimeout(() => a.setScale('x', { min: rtS[0], max: rtS[rtS.length - 1] }), 0));

  $('#tblPiky').innerHTML = `<thead><tr><th>RT štd. (min)</th><th>Ión</th>${maRi(v) ? '<th>RI</th>' : ''}${maNavrh(v) ? '<th>Návrh látky</th>' : ''}<th>Výsledok</th><th>Posun vrcholu (s)</th><th>Zhoda spektra</th><th>τ tvaru (s)</th><th>Pomer vz/štd</th><th>Dekonv. zhoda</th><th>Dekonv. posun (s)</th></tr></thead><tbody>` +
    v.piky.map((p, i) => `<tr class="click" data-p="${i}"><td class="num">${fmt(p.rtStdMin)}</td><td class="num">${p.hlavnyIon}</td>
      ${maRi(v) ? `<td class="num">${fmt(p.ri, 0)}</td>` : ''}${maNavrh(v) ? `<td>${p.navrh ? `<span class="st nv ${NV_CLS(p.navrh.stav)}" title="${esc(p.navrh.stav)} – zhoda ${fmt(p.navrh.zhoda)}">${esc(p.navrh.nazov)}</span>` : '–'}</td>` : ''}
      <td><span class="st ${STAV_CLS[p.stav]}">${esc(p.stav)}</span></td><td class="num">${fmt(p.posun)}</td><td class="num">${fmt(p.cosVrchol)}</td>
      <td class="num">${fmt(p.tauEic)}</td><td class="num">${fmt(p.pomer, 3)}</td><td class="num">${fmt(p.dekCos)}</td><td class="num">${fmt(p.dekPosun)}</td></tr>`).join('') + '</tbody>';
  const tn = $('#tblNavyse');
  if (tn) tn.querySelectorAll('tr.click').forEach(tr => tr.onclick = () => {
    tn.querySelectorAll('tr.sel').forEach(x => x.classList.remove('sel')); tr.classList.add('sel');
    const e = v.navyse[+tr.dataset.n];
    $('#navDet').innerHTML = `<h3>Pík len vo vzorke ${fmt(e.rtMin)} min${e.ri != null ? `, RI ${fmt(e.ri, 0)}` : ''}</h3>` + navrhHtml(e.navrh, 'mirN');
    kresliZrkadlo($('#mirN'), e.specMz, e.specI, e.navrh);
  });
  $('#tblPiky').querySelectorAll('tr.click').forEach(tr => tr.onclick = () => {
    $('#tblPiky').querySelectorAll('tr.sel').forEach(x => x.classList.remove('sel'));
    tr.classList.add('sel'); kresliPik(v, +tr.dataset.p);
  });
}

function kresliPik(v, i) {
  const p = v.piky[i], n = data.nastavenia;
  while (plots.length > 2) plots.pop().destroy();
  const el = $('#pikDet');
  el.innerHTML = `<h3>Pík ${fmt(p.rtStdMin)} min (${fmt(p.rtStdS)} s), hlavný ión m/z ${p.hlavnyIon}</h3>
    <p><span class="st ${STAV_CLS[p.stav]}">${esc(p.stav)}</span> &nbsp; ióny: ${esc(p.ione)}</p>
    <div id="gPik"></div>
    <table class="tbl"><tbody>
      <tr><td>Vrchol vo vzorke</td><td class="num">${fmt(p.vrcholVz)} s</td><td>Posun vrcholu</td><td class="num">${fmt(p.posun)} s</td></tr>
      <tr><td>Posun podľa tvaru τ</td><td class="num">${fmt(p.tauEic)} s</td><td>Korelácia tvaru</td><td class="num">${fmt(p.rEic, 3)}</td></tr>
      <tr><td>Zhoda spektra vo vrchole</td><td class="num">${fmt(p.cosVrchol)}</td><td>Výška iónu vz / štd</td><td class="num">${fmt(p.pomer, 3)}</td></tr>
      <tr><td>Výška iónu vo vzorke</td><td class="num">${fmtInt(p.vyskaVz)}</td><td>Šum</td><td class="num">${fmtInt(p.sum)}</td></tr>
      <tr><td>Najlepšia zhoda kdekoľvek</td><td class="num">${fmt(p.cosKdekolvek)}</td><td>kde</td><td class="num">${fmt(p.rtKdekolvekMin)} min</td></tr>
      ${p.dekDetail ? `<tr><td>Dekonvolúcia</td><td colspan="3">${esc(p.dekDetail)}${p.dekK ? ` <br>nájdené pri k = ${esc(p.dekK)}` : ''}</td></tr>` : ''}
    </tbody></table>
    ${p.navrh || p.ri != null ? `<h3>Návrh látky${p.ri != null ? ` · RI ${fmt(p.ri, 0)}` : ''}</h3>${navrhHtml(p.navrh, 'mirP')}` : ''}
    <p class="small">Graf: hlavný ión (po odčítaní pozadia), každá krivka normovaná na svoje maximum. Modré pásmo = tolerancia ±${fmt(n.tol)} s okolo vrcholu štandardu; oranžová čiara = nájdený vrchol vo vzorke${p.stav.includes('dekonvolúcia') ? ' (zložka z dekonvolúcie)' : ''}.</p>`;
  // spoločná os x (s, relatívne k vrcholu štandardu)
  const xs = [...new Set([...p.eicRtS, ...p.eicRtV])].sort((a, b) => a - b);
  const mS = Math.max(...p.eicS, 1e-9), mV = Math.max(...p.eicV, 1e-9);
  const yS = xs.map(t => { const k = p.eicRtS.indexOf(t); return k >= 0 ? p.eicS[k] / mS : null; });
  const yV = xs.map(t => { const k = p.eicRtV.indexOf(t); return k >= 0 ? p.eicV[k] / mV : null; });
  const x = xs.map(t => t - p.rtStdS);
  const opts = {
    width: Math.max(320, el.clientWidth - 10), height: 220,
    scales: { x: { time: false }, y: { range: [0, 1.08] } },
    plugins: [pasmo(-n.tol, n.tol), ciary([{ x: 0, c: '#1f6feb', a: .8 }, ...(p.stav.includes('dekonvolúcia') && p.dekPosun != null ? [{ x: p.dekPosun, c: '#e8730c', a: .9 }] : p.posun != null ? [{ x: p.posun, c: '#e8730c', a: .9 }] : [])])],
    axes: [{ label: 'čas voči vrcholu štandardu (s)' }, { size: 40 }],
    series: [{ label: 's', value: (u, v) => v == null ? '–' : fmt(v, 2) },
      { label: 'štandard', stroke: '#1f6feb', width: 1.5, spanGaps: true, points: { show: true, size: 4 } },
      { label: 'vzorka', stroke: '#e8730c', width: 1.5, spanGaps: true, points: { show: true, size: 4 } }],
  };
  plots.push(new uPlot(opts, [x, yS, yV], $('#gPik')));
  if (p.navrh) kresliZrkadlo($('#mirP'), p.specMz, p.specI, p.navrh);
}

// ---------- identifikácia ----------
const maRi = v => v.piky.some(p => p.ri != null) || v.navyse.some(e => e.ri != null);
const maNavrh = v => v.piky.some(p => p.navrh) || v.navyse.some(e => e.navrh);
function navrhBunky(n) {
  if (!n) return '<td>–</td><td></td><td></td><td></td>';
  return `<td>${esc(n.nazov)}</td><td class="num">${fmt(n.zhoda)}</td><td class="num">${fmt(n.dRi, 0)}</td><td><span class="st nv ${NV_CLS(n.stav)}">${esc(n.stav)}</span></td>`;
}
function navrhHtml(n, idc) {
  if (!n) return '<p class="small">Návrh látky nie je k dispozícii (knižnica nebola zapnutá).</p>';
  return `<p><span class="st nv ${NV_CLS(n.stav)}">${esc(n.stav)}</span> &nbsp; <b>${esc(n.nazov)}</b> (${esc(n.kategoria)}) – zhoda ${fmt(n.zhoda)}${n.riKniznica != null ? `, RI knižnice ${fmt(n.riKniznica, 0)}, ΔRI ${fmt(n.dRi, 0)}` : ''}${n.rtKniznica != null ? `, RT knižnice ${fmt(n.rtKniznica)} min` : ''}</p>
    ${n.poznamka ? `<p class="small warn">${esc(n.poznamka)}</p>` : ''}
    <canvas class="mirror" id="${idc}"></canvas>
    <p class="small">Hore spektrum píku (po odčítaní pozadia), dole zrkadlovo spektrum z knižnice (${esc(n.zdroj)}).</p>
    <table class="tbl"><thead><tr><th>Kandidát</th><th>Kategória</th><th>Zhoda</th><th>RI knižnice</th><th>ΔRI</th></tr></thead><tbody>
    ${(n.kandidati || []).map(k => `<tr><td>${esc(k.nazov)}</td><td>${esc(k.kategoria)}</td><td class="num">${fmt(k.zhoda)}</td><td class="num">${fmt(k.ri, 0)}</td><td class="num">${fmt(k.dRi, 0)}</td></tr>`).join('')}</tbody></table>`;
}
function kresliZrkadlo(cv, mz, it, n) {
  if (!cv || !n) return;
  const dpr = devicePixelRatio || 1, W = cv.clientWidth || 400, H = cv.clientHeight || 240;
  cv.width = W * dpr; cv.height = H * dpr;
  const ctx = cv.getContext('2d'); ctx.scale(dpr, dpr); ctx.clearRect(0, 0, W, H);
  const all = [...(mz || []), ...(n.knizMz || [])]; if (!all.length) return;
  const lo = Math.max(0, Math.min(...all) - 5), hi = Math.max(...all) + 5, mid = H / 2, L = 36, R = W - 8;
  const X = m => L + (m - lo) / (hi - lo) * (R - L);
  ctx.strokeStyle = '#999'; ctx.beginPath(); ctx.moveTo(L, mid); ctx.lineTo(R, mid); ctx.stroke();
  ctx.font = '10px system-ui'; ctx.fillStyle = '#555';
  for (let m = Math.ceil(lo / 50) * 50; m <= hi; m += 50) { ctx.fillText(m, X(m) - 8, H - 2); }
  const kresli = (ms, is, smer, farba) => {
    const mx = Math.max(...is, 1e-9); ctx.strokeStyle = farba; ctx.lineWidth = 1.3;
    const lab = ms.map((m, i) => [m, is[i]]).sort((a, b) => b[1] - a[1]).slice(0, 6);
    ms.forEach((m, i) => { const h = is[i] / mx * (mid - 16); ctx.beginPath(); ctx.moveTo(X(m), mid); ctx.lineTo(X(m), mid - smer * h); ctx.stroke(); });
    ctx.fillStyle = farba; lab.forEach(([m, i]) => { const h = i / mx * (mid - 16); ctx.fillText(m, X(m) - 8, smer > 0 ? mid - h - 3 : mid + h + 10); });
  };
  kresli(mz || [], it || [], 1, '#1f6feb');
  kresli(n.knizMz || [], n.knizI || [], -1, '#d0342c');
  ctx.fillStyle = '#1f6feb'; ctx.fillText('pík', 2, 12); ctx.fillStyle = '#d0342c'; ctx.fillText('knižnica', 2, H - 14);
}

// vlastná knižnica zo štandardu
$('#btnVlastna').onclick = () => {
  const v = data.vysledky[aktivna];
  $('#tblVlastna').innerHTML = `<thead><tr><th>Uložiť</th><th>RT (min)</th><th>RI</th><th>Názov látky</th><th>Návrh z knižnice</th><th>Hlavné ióny</th></tr></thead><tbody>` +
    v.piky.map((p, i) => {
      const n = p.navrh, pred = n && (n.stav === 'pravdepodobne' || n.stav.startsWith('vlastná')) ? n.nazov : '';
      return `<tr><td><input type="checkbox" class="vlChk" data-i="${i}" checked></td><td class="num">${fmt(p.rtStdMin, 3)}</td><td class="num">${fmt(p.ri, 0)}</td>
        <td><input type="text" class="vlNm" data-i="${i}" value="${esc(pred)}" placeholder="Neznámy RT ${fmt(p.rtStdMin)}"></td>
        <td class="small">${n ? `${esc(n.nazov)} (${esc(n.stav)}, ${fmt(n.zhoda)})` : '–'}</td><td class="small">${esc(p.ione)}</td></tr>`;
    }).join('') + '</tbody>';
  $('#vlNazov').value = 'vlastna_kniznica_' + v.standard.replace(/\.[^.]+$/, '').replace(/[^A-Za-z0-9_-]+/g, '_') + '.msp';
  $('#dlgVlastna').showModal();
};
$('#vlStiahni').onclick = () => {
  const v = data.vysledky[aktivna];
  const nm = {}; document.querySelectorAll('.vlNm').forEach(x => nm[x.dataset.i] = x.value.trim());
  const dnes = new Date().toISOString().slice(0, 10);
  let out = '';
  document.querySelectorAll('.vlChk:checked').forEach(c => {
    const p = v.piky[+c.dataset.i]; if (!p.specMz || !p.specMz.length) return;
    const meno = nm[c.dataset.i] || `Neznámy RT ${p.rtStdMin.toFixed(2)}`;
    out += `NAME: ${meno}\nRETENTIONTIME: ${p.rtStdMin.toFixed(4)}\n` + (p.ri != null ? `RETENTIONINDEX: ${p.ri.toFixed(1)}\n` : '') +
      `COMMENT: zdroj=vlastná knižnica zo štandardu ${v.standard}, ${dnes}\nNum Peaks: ${p.specMz.length}\n` +
      p.specMz.map((m, i) => `${m} ${p.specI[i]}`).join('\n') + '\n\n';
  });
  if (!out) { alert('Nie je vybraný žiadny pík.'); return; }
  const a = document.createElement('a');
  a.href = URL.createObjectURL(new Blob([out], { type: 'text/plain;charset=utf-8' }));
  a.download = $('#vlNazov').value || 'vlastna_kniznica.msp'; a.click();
  setTimeout(() => URL.revokeObjectURL(a.href), 2000);
};

// ---------- ostatné ----------
fetch('/api/info').then(r => r.json()).then(j => { $('#ver').textContent = j.verzia; });
// súhlas s licenciou Thermo RawFileReader (vyžaduje ho jej licencia, bod 3.3)
fetch('/api/licencia').then(r => r.json()).then(j => {
  const odkazy = (j.subory || []).filter(f => f.startsWith('THERMO')).map(f => `<a href="/api/licencia/${encodeURIComponent(f)}" target="_blank">${esc(f)}</a>`).join(', ') || 'priečinok LICENCIE';
  $('#licSubory').innerHTML = odkazy; $('#licSubory2').innerHTML = odkazy;
  if (!j.ok) $('#dlgLic').showModal();
});
$('#dlgLic').addEventListener('cancel', e => e.preventDefault());
$('#licChk').onchange = () => { $('#licOk').disabled = !$('#licChk').checked; };
$('#licOk').onclick = async () => { await fetch('/api/licencia/suhlas', { method: 'POST' }); $('#dlgLic').close(); };
$('#btnAbout').onclick = () => $('#dlgAbout').showModal();
$('#btnKoniec').onclick = async () => {
  if (!confirm('Ukončiť program? Neuložené výsledky sa stratia (stiahnite si Excel).')) return;
  try { await fetch('/api/koniec', { method: 'POST' }); } catch { }
  document.body.innerHTML = '<main><div class="card"><h2>Program je ukončený.</h2><p>Toto okno môžete zavrieť.</p></div></main>';
};
window.addEventListener('resize', () => { if (data) kresliDetail(); });
kresliZoznamy();
stavIdent();
