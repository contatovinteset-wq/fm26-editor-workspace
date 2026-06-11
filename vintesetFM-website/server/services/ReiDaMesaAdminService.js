import * as cheerio from 'cheerio';
import { PrismaClient } from '@prisma/client';
import { reiDaMesaEvents } from './eventBus.js';
import { getDefaultCreatorId } from './creatorContext.js';

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
  const uniqueIdIndex = headers.findIndex(h => h === 'Unique ID' || h === 'UID');

  for (let i = 1; i < rows.length; i++) { // pula o header
    const cols = $(rows[i]).find('td');
    if (cols.length === 0) continue;

    const name = nameIndex >= 0 ? $(cols[nameIndex]).text().trim() : '';
    if (!name) continue;

    const realPosition = posIndex >= 0 ? $(cols[posIndex]).text().trim() : '';
    const ageText = ageIndex >= 0 ? $(cols[ageIndex]).text().trim() : '';
    const heightText = heightIndex >= 0 ? $(cols[heightIndex]).text().trim() : '';
    const uniqueIdText = uniqueIdIndex >= 0 ? $(cols[uniqueIdIndex]).text().trim() : '';

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
      rawStats,
      uniqueId: uniqueIdText || null
    });
  }

  // Alterado para UPSERT do lado da criação para atualizar. 
  // Na limpeza, excluímos manualmente todos que não vieram, junto com suas FKs.
  let countUpdated = 0;
  const incomingUids = [];
  const creatorId = await getDefaultCreatorId(prisma);

  for (const pData of playersToAdd) {
    if (pData.name) {
      incomingUids.push(pData.uidName);
      await prisma.player.upsert({
        where: { creatorId_uidName: { creatorId, uidName: pData.uidName } },
        update: {
          name: pData.name,
          realPosition: pData.realPosition,
          age: pData.age,
          height: pData.height,
          rawStats: pData.rawStats,
          uniqueId: pData.uniqueId
          // Não alteramos cartolaRole para respeitar as atribuições do admin!
        },
        create: { ...pData, creatorId }
      });
      countUpdated++;
    }
  }

  // REGRA EXTRAÍDA DA MENSAGEM: Quando um novo elenco é subido, as escolhas dos squads são resetadas
  await prisma.squad.updateMany({
    data: {
      defensorId: null,
      meioId: null,
      ataqueId: null,
      bancoId: null,
      bagreId: null,
      capitaoId: null
    }
  });

  // Excluímos quem não veio no HTML novo (Manter apenas os jogadores ativos)
  if (incomingUids.length > 0) {
    const playersToDelete = await prisma.player.findMany({
      where: { creatorId, uidName: { notIn: incomingUids } },
      select: { id: true }
    });
    
    if (playersToDelete.length > 0) {
      const idsToDelete = playersToDelete.map(p => p.id);

      // Limpar tabelas dependentes
      await prisma.playerScore.deleteMany({
        where: { playerId: { in: idsToDelete } }
      });
      await prisma.craqueVote.deleteMany({
        where: { playerId: { in: idsToDelete } }
      });
      await prisma.round.updateMany({
        where: { bagreId: { in: idsToDelete } },
        data: { bagreId: null }
      });

      // E finalmente excluir o jogador
      await prisma.player.deleteMany({
        where: { id: { in: idsToDelete } }
      });
    }

    // Garante que todos que vieram estejam ativos (eligible: true)
    await prisma.player.updateMany({
      where: { creatorId, uidName: { in: incomingUids } },
      data: { eligible: true }
    });
  }

  return { message: "Plantel importado com sucesso! Os elencos da rodada foram resetados e os inativos excluídos.", inserted: countUpdated, total: rows.length - 1 };
}

export async function previewMatchResultHtml(htmlString) {
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
      
      const classText = (stats['Classificação'] || '').replace(',', '.');
      const rating = parseFloat(classText) || 0.0;
      
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
        playerName: player.name,
        realPosition: player.realPosition,
        roundId: openRound.id,
        rating: rating, 
        points: Number(points.toFixed(2)),
        details: { 
          goals, assists, xG, xA, yellowCars, redCards, minsPlayed,
          chancesCriadas, passesDecisivos, dribles, desarmes, defesasGoleiro,
          bateuBarra, intercep, alivios, faltasCom
        }
      });
    }
  }

  scores.sort((a,b) => b.points - a.points);
  
  return { success: true, scores };
}

export async function processMatchResultFinal(scoresFromFrontend) {
  const creatorId = await getDefaultCreatorId(prisma);
  const openRound = await prisma.round.findFirst({ where: { isOpen: true, creatorId } });
  if (!openRound) {
    throw new Error('Nenhuma rodada aberta no momento.');
  }

  let processados = 0;
  let worstPlayerId = null;
  let minPoints = 9999;
  
  const finalScores = [];

  for (const s of scoresFromFrontend) {
    let points = 0;
    
    if (s.details.minsPlayed >= 60) points += 1.0;
    else if (s.details.minsPlayed > 0) points += 0.5;

    points -= (s.details.yellowCars * 1.5);
    points -= (s.details.redCards * 3.0);

    // Regra severa: Classificação abaixo ou igual a 6.0 perde 5 pontos para torná-lo o Bagre
    if (s.rating > 0 && s.rating <= 6.0) {
       points -= 5.0;
    }

    points += (s.details.goals * 8.0);
    points += (s.details.assists * 5.0);
    points += (s.details.xG * 2.0);
    points += (s.details.xA * 2.0);
    points += (s.details.chancesCriadas * 2.0);
    points += (s.details.passesDecisivos * 1.0);
    points += (s.details.dribles * 0.5);
    points += (s.details.bateuBarra * 1.5);
    points += (s.details.desarmes * 2.0);
    points += (s.details.intercep * 0.5);
    points += (s.details.alivios * 0.2);
    points -= (s.details.faltasCom * 0.5);
    points += (s.details.defesasGoleiro * 1.5);

    s.points = Number(points.toFixed(2));
    finalScores.push(s);

    if (s.details.minsPlayed >= 25 && s.points < minPoints) {
      minPoints = s.points;
      worstPlayerId = s.playerId;
    }

    await prisma.playerScore.upsert({
      where: { playerId_roundId: { playerId: s.playerId, roundId: openRound.id } },
      update: { points: s.points, details: s.details },
      create: { playerId: s.playerId, roundId: openRound.id, points: s.points, details: s.details, creatorId }
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
    
    const calcPts = async (pid, isCaptain) => {
      if (!pid) return 0;
      const ps = await prisma.playerScore.findUnique({
        where: { playerId_roundId: { playerId: pid, roundId: openRound.id } }
      });
      let pts = ps ? ps.points : 0;
      if (isCaptain) {
         pts = pts * 2.0;
      }
      return pts;
    };

    roundScore += await calcPts(sq.defensorId, sq.defensorId === sq.capitaoId);
    roundScore += await calcPts(sq.meioId, sq.meioId === sq.capitaoId);
    roundScore += await calcPts(sq.ataqueId, sq.ataqueId === sq.capitaoId);

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

    sq.roundScoreCalculated = Number(roundScore.toFixed(2));
  }

  finalScores.sort((a,b) => b.points - a.points);
  
  // Encontrar O campeão e o Craque
  let roundChampion = null;
  const sortedSquads = [...squads].sort((a,b) => (b.roundScoreCalculated || 0) - (a.roundScoreCalculated || 0));
  if (sortedSquads.length > 0 && sortedSquads[0].roundScoreCalculated !== 0) {
     const ch = await prisma.user.findUnique({ where: { id: sortedSquads[0].userId }});
     roundChampion = { nickname: ch?.nickname || ch?.name || 'Viewer', score: sortedSquads[0].roundScoreCalculated };
  }

  const votesCount = await prisma.craqueVote.groupBy({
      by: ['playerId'],
      where: { roundId: openRound.id },
      _count: { playerId: true },
      orderBy: { _count: { playerId: 'desc' } },
      take: 1
  });
  let craqueChat = null;
  if (votesCount.length > 0) {
      const crqPlayer = await prisma.player.findUnique({ where: { id: votesCount[0].playerId }, select: { name: true, realPosition: true } });
      craqueChat = crqPlayer; // Passa o objeto se achar
  }

  reiDaMesaEvents.emit('overlay_event', {
     type: 'ROUND_FINISHED',
     craque: craqueChat,
     champion: roundChampion
  });
  
  return { success: true, scoresProcessados: processados, bagreDaRodadaId: worstPlayerId, scores: finalScores };
}
