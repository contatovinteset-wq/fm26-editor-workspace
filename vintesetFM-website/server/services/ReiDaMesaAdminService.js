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

  for (let i = 1; i < rows.length; i++) {
    const cols = $(rows[i]).find('td');
    if (cols.length === 0) continue;

    const name = $(cols[0]).text().trim();
    const ageText = $(cols[1]).text().trim(); // Assumindo idade na pos 1
    const uidName = `${name}-${ageText}`.replace(/\s+/g, '-').toLowerCase();

    // Supondo estrutura de Match: Gols = 2, Ast = 3, CA = 4, CV = 5, Desarmes = 6, Nota = 7
    const goals = parseInt($(cols[2]).text().trim()) || 0;
    const assists = parseInt($(cols[3]).text().trim()) || 0;
    const yellowCards = parseInt($(cols[4]).text().trim()) || 0;
    const redCards = parseInt($(cols[5]).text().trim()) || 0;
    const rating = parseFloat($(cols[7]).text().trim().replace(',', '.')) || 0;

    // Lógica do Cartola Mockada (- Goals: +8.0)
    let points = 0;
    points += goals * 8.0;
    points += assists * 5.0;
    points -= yellowCards * 2.0;
    points -= redCards * 5.0;
    points += rating > 7.0 ? 3.0 : 0; // bonus por rating
    
    // Procura o jogador
    const player = await prisma.player.findUnique({ where: { uidName } });
    if (player) {
      scores.push({
        playerId: player.id,
        roundId: openRound.id,
        rating,
        points,
        details: { goals, assists, yellowCards, redCards }
      });
    }
  }

  // Salva no banco as pontuações e atualiza Squads
  let processados = 0;
  for (const s of scores) {
    await prisma.playerScore.upsert({
      where: { playerId_roundId: { playerId: s.playerId, roundId: s.roundId } },
      update: { points: s.points, rating: s.rating, details: s.details },
      create: { playerId: s.playerId, roundId: s.roundId, points: s.points, rating: s.rating, details: s.details }
    });
    processados++;
  }

  // Recalcular totais para o Squad ativo deste round
  // 1. Pegar todos os squads atuais
  const squads = await prisma.squad.findMany({});
  
  for (const sq of squads) {
    let roundScore = 0;
    
    // Busca as pontuações da rodada atual para cada membro
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
    
    // bagre score invertido
    const bagrePts = await calcPts(sq.bagreId);
    // ex: se o bagre for negativo, manager ganha positivo.
    roundScore += (bagrePts < 0 ? Math.abs(bagrePts) * 2 : -bagrePts);

    await prisma.squad.update({
      where: { id: sq.id },
      data: {
        roundScore: roundScore,
        totalScore: { increment: roundScore }
      }
    });
  }

  return { success: true, scoresProcessados: processados };
}
