import * as cheerio from 'cheerio';
import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

export async function processPlantelHtml(htmlString) {
  const $ = cheerio.load(htmlString);
  const rows = $('table tr').toArray();
  const playersToAdd = [];

  const headers = [];
  $(rows[0]).find('th').each((_, th) => {
    headers.push($(th).text().trim());
  });

  const nameIndex = headers.findIndex(h => h === 'Jogador');
  const posIndex = headers.findIndex(h => h === 'Escolhido' || h === 'Posição');
  const ageIndex = headers.findIndex(h => h === 'Idade');
  const heightIndex = headers.findIndex(h => h === 'Altura');

  for (let i = 1; i < rows.length; i++) { // pula o header
    const cols = $(rows[i]).find('td');
    if (cols.length === 0) continue;

    const name = nameIndex >= 0 ? $(cols[nameIndex]).text().trim() : '';
    if (!name) continue;

    const realPosition = posIndex >= 0 ? $(cols[posIndex]).text().trim() : '';
    const ageText = ageIndex >= 0 ? $(cols[ageIndex]).text().trim() : '';
    const heightText = heightIndex >= 0 ? $(cols[heightIndex]).text().trim() : '';

    const uidName = `${name}-${ageText}`.replace(/\s+/g, '-').toLowerCase();

    // Monta o rawStats cruzando keys do header
    const rawStats = {};
    cols.each((j, td) => {
      if (headers[j]) {
        rawStats[headers[j]] = $(td).text().trim();
      }
    });

    playersToAdd.push({
      uidName,
      name,
      realPosition,
      age: parseInt(ageText) || null,
      height: heightText,
      eligible: true,
      rawStats
    });
  }

  // Deleta TODOS os jogadores antigos para o novo Plantel prevalecer absolutamente
  await prisma.player.deleteMany({});

  let countNew = 0;

  for (const pData of playersToAdd) {
    if (pData.name) {
      await prisma.player.create({
        data: pData
      });
      countNew++;
    }
  }

  return { message: "Plantel importado com sucesso!", inserted: countNew, total: rows.length - 1 };
}

export async function processMatchResultHtml(htmlString) {
  const $ = cheerio.load(htmlString);
  const rows = $('table tr').toArray();
  const scores = [];

  // Pega a rodada aberta
  const openRound = await prisma.round.findFirst({ where: { isOpen: true } });
  if (!openRound) {
    throw new Error('Nenhuma rodada aberta no momento.');
  }

  // Identificar quais arrays de colunas pertecem a qual "Tabela" processada
  // O FM gera várias table <table> na mesma página.
  let currentCategory = '';
  // Vamos juntar tudo num DICIONÁRIO de jogador
  const playerStatsMap = {};

  const tables = $('table');
  tables.each((tableIndex, table) => {
    const prevH3 = $(table).prevAll('h3').first().text().trim();
    const headers = [];
    $(table).find('th').each((_, th) => headers.push($(th).text().trim()));

    const trs = $(table).find('tr');
    trs.each((i, tr) => {
      if (i === 0) return; // headers
      const cols = $(tr).find('td');
      if (cols.length === 0) return;

      const name = $(cols[2]).text().trim(); // A Coluna 2 é NOME em todas as tabelas
      if (!name) return;

      if (!playerStatsMap[name]) playerStatsMap[name] = {};
      const pmap = playerStatsMap[name];

      // Busca por min e amarelos/vermelhos em Min (coluna 1)
      if (!pmap.MinText) pmap.MinText = $(cols[1]).text().trim();

      // Mapeia todas as celulas da row para seus headers
      headers.forEach((h, idx) => {
        pmap[h] = $(cols[idx]).text().trim();
      });
    });
  });

  // Iterar no Mapa Unificado de stats extraídos
  for (const name of Object.keys(playerStatsMap)) {
    const stats = playerStatsMap[name];

    // Busca jogador no DB
    const playerArray = await prisma.player.findMany({ where: { name: name } });
    const player = playerArray.length > 0 ? playerArray[0] : null;

    if (player) {
      // Regras de Cálculos
      // 'Golos'
      const goals = parseInt(stats['Golos']) || 0;
      const assists = parseInt(stats['Assist.']) || 0;
      const xgText = (stats['xG'] || '').replace(',', '.');
      const xG = parseFloat(xgText) || 0.0;
      const xaText = (stats['xA'] || '').replace(',', '.');
      const xA = parseFloat(xaText) || 0.0;

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
      const sofridos = parseInt(stats['Remates Sofridos']) || 0; // Se isso é total de chutes ao gol? Para o goleiro se ele sofre gol, geralmente não aparece aí direto, mas usaremos a soma das defesas como base de bônus

      // Calcula os cartões da coluna Min
      let yellowCars = 0;
      let redCards = 0;
      let minsPlayed = 0;

      const minText = stats['MinText'] || '';
      
      let points = 0;
      
      // Pontuação Frouxa do Minutos:
      if (minText.includes('90')) minsPlayed = 90;
      else if (minText.includes('Sai')) minsPlayed = parseInt(minText) || 60;
      else if (minText.includes('Entra')) minsPlayed = 90 - (parseInt(minText) || 60);

      if (minsPlayed >= 60) points += 1.0;
      else if (minsPlayed > 0) points += 0.5;

      if (minText.toLowerCase().includes('ama') || minText.toLowerCase().includes('amarelo')) {
        yellowCars = 1;
        points -= 1.5;
      }
      if (minText.toLowerCase().includes('ver') || minText.toLowerCase().includes('vermelho')) {
        redCards = 1;
        points -= 3.0;
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
      points -= (faltasCom * 0.5); // Desconta faltas feitas!

      const defesasGoleiro = defSeguras + defPonta + defDesvio;
      points += (defesasGoleiro * 1.5);
      // Faltaria descontos a gols sofridos baseados num dado mais preciso.

      scores.push({
        playerId: player.id,
        roundId: openRound.id,
        rating: 0, 
        points: Number(points.toFixed(2)),
        details: { goals, assists, xG, xA, yellowCars, redCards, minsPlayed }
      });
    }
  }

  // Pós processamento e encontrar "O BAGRE DA PARTIDA"
  let processados = 0;
  let worstPlayerId = null;
  let minPoints = 9999;

  for (const s of scores) {
    // Determina o bagre (o que jogou e pontuou menos, ignora não-escalados ou banco? 
    // Só ignora se não entrou (minsPlayed === 0)
    if (s.details.minsPlayed > 0 && s.points < minPoints) {
      minPoints = s.points;
      worstPlayerId = s.playerId;
    }

    await prisma.playerScore.upsert({
      where: { playerId_roundId: { playerId: s.playerId, roundId: s.roundId } },
      update: { points: s.points, details: s.details },
      create: { playerId: s.playerId, roundId: s.roundId, points: s.points, details: s.details }
    });
    processados++;
  }

  // Update logic para Round
  await prisma.round.update({
    where: { id: openRound.id },
    data: { bagreId: worstPlayerId }
  });

  // Atualizar pontuação total de usuários
  const squads = await prisma.squad.findMany({});
  
  for (const sq of squads) {
    let roundScore = 0;
    
    const calcPts = async (pid) => {
      if (!pid) return 0;
      const ps = await prisma.playerScore.findUnique({
        where: { playerId_roundId: { playerId: pid, roundId: openRound.id } }
      });
      return ps ? ps.points : 0;
    };

    roundScore += await calcPts(sq.defensorId);
    roundScore += await calcPts(sq.meioId);
    roundScore += await calcPts(sq.ataqueId);

    // Lógica nova do Bônus do Bagre:
    // Se o bagre escolhido pelo viewer for == worstPlayerId, Ganha 5pts.
    // Senão, Perde 5pts.
    if (sq.bagreId) {
      if (sq.bagreId === worstPlayerId) {
        roundScore += 5.0; // BINGO Do bagre!
      } else {
        roundScore -= 5.0; // Errou o bagre, punido com as leis dos Deuses do FM.
      }
    }

    await prisma.squad.update({
      where: { id: sq.id },
      data: {
        roundScore: Number(roundScore.toFixed(2)),
        totalScore: { increment: Number(roundScore.toFixed(2)) }
      }
    });
  }

  return { success: true, scoresProcessados: processados, bagreDaRodadaId: worstPlayerId };
}
