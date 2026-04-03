const fs = require('fs');
const cheerio = require('cheerio');

const moneyballHtml = fs.readFileSync('moneyball_export_20260402_155333.html', 'utf8');
const $m = cheerio.load(moneyballHtml);
const players = [];
$m('table tr').each((i, tr) => {
  if (i === 0) return;
  const cols = $m(tr).find('td');
  const role = $m(cols[1]).text().trim();
  const name = $m(cols[2]).text().trim();
  if (name) players.push({ id: name, name, realPosition: role });
});

const matchHtml = fs.readFileSync('match_stats_Hartle vs MFC_20260401_232250.html', 'utf8');
const $ = cheerio.load(matchHtml);
const playerStatsMap = {};
const tables = $('table');
tables.each((tableIndex, table) => {
  const headers = [];
  $(table).find('th').each((_, th) => headers.push($(th).text().trim()));
  const trs = $(table).find('tr');
  trs.each((i, tr) => {
    if (i === 0) return;
    const cols = $(tr).find('td');
    if (cols.length === 0) return;
    const name = $(cols[2]).text().trim();
    if (!name) return;
    if (!playerStatsMap[name]) playerStatsMap[name] = {};
    const pmap = playerStatsMap[name];
    if (!pmap.MinText) pmap.MinText = $(cols[1]).text().trim();
    headers.forEach((h, idx) => {
      pmap[h] = $(cols[idx]).text().trim();
    });
  });
});

const scores = [];
for (const name of Object.keys(playerStatsMap)) {
  const stats = playerStatsMap[name];
  const player = players.find(p => p.name === name);
  if (player) {
    const goals = parseInt(stats['Golos']) || 0;
    const assists = parseInt(stats['Assist.']) || 0;
    const xG = parseFloat((stats['xG'] || '').replace(',', '.')) || 0.0;
    const xA = parseFloat((stats['xA'] || '').replace(',', '.')) || 0.0;
    const chancesCriadas = parseInt(stats['Oportunidades Flagrantes']) || 0;
    const passesDecisivos = parseInt(stats['Passes Decisivos']) || 0;
    const dribles = parseInt(stats['Fintas']) || 0;
    const bateuBarra = parseInt(stats['Remate - Bateu na Barra']) || 0;
    const desarmes = parseInt(stats['Desarmes Decisivos']) || 0;
    const intercep = parseInt(stats['Intercepções Feitas']) || 0;
    const alivios = parseInt(stats['Alívios']) || 0;
    const faltasCom = parseInt(stats['Faltas Cometidas']) || 0;
    const defSeguras = parseInt(stats['Defesas Seguras']) || 0;
    const defPonta = parseInt(stats['Defesas com Ponta dos Dedos']) || 0;
    const defDesvio = parseInt(stats['Defesas Desviadas']) || 0;

    let yellowCars = 0;
    let redCards = 0;
    let minsPlayed = 0;
    const minText = stats['MinText'] || '';
    let points = 0;

    if (minText.includes('90')) minsPlayed = 90;
    else if (minText.includes('Sai')) minsPlayed = parseInt(minText) || 60;
    else if (minText.includes('Entra')) minsPlayed = 90 - (parseInt(minText) || 60);

    if (minsPlayed >= 60) points += 1.0;
    else if (minsPlayed > 0) points += 0.5;

    if (minText.toLowerCase().includes('ama') || minText.toLowerCase().includes('amarelo')) {
      yellowCars = 1; points -= 1.5;
    }
    if (minText.toLowerCase().includes('ver') || minText.toLowerCase().includes('vermelho')) {
      redCards = 1; points -= 3.0;
    }

    points += (goals * 8.0);
    points += (assists * 5.0);
    points += (xG * 2.0);
    points += (xA * 2.0);
    points += (chancesCriadas * 2.0);
    points += (passesDecisivos * 1.0);
    points += (dribles * 0.5);
    points += (bateuBarra * 1.5);
    points += (desarmes * 2.0);
    points += (intercep * 0.5);
    points += (alivios * 0.2);
    points -= (faltasCom * 0.5);

    const defesasGoleiro = defSeguras + defPonta + defDesvio;
    points += (defesasGoleiro * 1.5);

    scores.push({
      name: player.name,
      points,
      calc: `Min:${minsPlayed}m(${minsPlayed>=60?'+1.0':(minsPlayed>0?'+0.5':0)}), Gols:${goals}(+${goals*8}), Ass:${assists}(+${assists*5}), xG:${xG}(+${xG*2}), xA:${xA}(+${xA*2}), ChC:${chancesCriadas}(+${chancesCriadas*2}), PasD:${passesDecisivos}(+${passesDecisivos*1}), Fint:${dribles}(+${dribles*0.5}), Bar:${bateuBarra}(+${bateuBarra*1.5}), Des:${desarmes}(+${desarmes*2}), Int:${intercep}(+${intercep*0.5}), Aliv:${alivios}(+${alivios*0.2}), DefGol:${defesasGoleiro}(+${defesasGoleiro*1.5}), FaltCom:${faltasCom}(-${faltasCom*0.5}), Amarelo:${yellowCars}(-${yellowCars*1.5}), Verm:${redCards}(-${redCards*3})`,
      minsPlayed: minsPlayed
    });
  }
}

scores.sort((a, b) => b.points - a.points);
let activeScores = scores.filter(s => s.minsPlayed > 0);
let worstPlayer = activeScores[activeScores.length - 1];

console.log('\\n--- RESULTADOS GERAIS ---');
scores.forEach(s => {
  console.log(`Jogador: ${s.name} | Pontos: ${s.points.toFixed(2)}`);
  console.log(`       -> ${s.calc}`);
});

console.log('\\n--- TOP 3 ---');
if (scores.length >= 3) {
  console.log('1. ' + scores[0].name + ' (' + scores[0].points.toFixed(2) + ' pts)');
  console.log('2. ' + scores[1].name + ' (' + scores[1].points.toFixed(2) + ' pts)');
  console.log('3. ' + scores[2].name + ' (' + scores[2].points.toFixed(2) + ' pts)');
}

console.log('\\n--- O BAGRE DA RODADA ---');
if (worstPlayer) {
  console.log(worstPlayer.name + ' (' + worstPlayer.points.toFixed(2) + ' pts)');
}
