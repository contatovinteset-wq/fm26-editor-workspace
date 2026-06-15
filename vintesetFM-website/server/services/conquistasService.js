import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

// 🏅 Gamificação (G2) — Conquistas.
// Tudo é DERIVADO do histórico (RoundEntry / Season / Round) — não há tabela nova.
// creatorId é injetado pelas rotas (req.creatorId); conquistas são por Rei da Mesa.

// Helper de conquista com níveis (bronze→prata→ouro...). Recebe o valor atual e os
// limiares de cada nível; devolve nível alcançado, próximo objetivo e se desbloqueou.
function tiered(value, tiers) {
  let level = 0;
  for (const t of tiers) {
    if (value >= t) level++;
    else break;
  }
  return {
    value,
    tiers,
    level,
    maxLevel: tiers.length,
    goal: level < tiers.length ? tiers[level] : null, // próximo limiar (null = maxado)
    maxed: level === tiers.length,
    unlocked: level > 0,
  };
}

export async function computeConquistas(creatorId, userId) {
  // Histórico do viewer neste Rei da Mesa.
  const entries = await prisma.roundEntry.findMany({
    where: { creatorId, userId },
    select: { score: true, rank: true, round: { select: { number: true } } },
  });

  const roundsPlayed = entries.length;
  const wins = entries.filter((e) => e.rank === 1).length;
  const podiums = entries.filter((e) => e.rank != null && e.rank <= 3).length;
  const bestRoundScore = entries.reduce((m, e) => Math.max(m, e.score || 0), 0);

  // Maior sequência de vitórias (rodadas consecutivas em 1º), ordenando por número.
  const ordered = entries
    .filter((e) => e.round)
    .sort((a, b) => a.round.number - b.round.number);
  let maxStreak = 0;
  let cur = 0;
  for (const e of ordered) {
    if (e.rank === 1) { cur++; maxStreak = Math.max(maxStreak, cur); }
    else cur = 0;
  }

  // Títulos de temporada.
  const seasonTitles = await prisma.season.count({ where: { creatorId, championId: userId } });

  // Recordista: detém a maior pontuação de rodada já registrada neste Rei da Mesa.
  const rec = await prisma.round.findFirst({
    where: { creatorId, topScore: { not: null } },
    orderBy: { topScore: 'desc' },
    select: { championId: true, topScore: true },
  });
  const isRecordHolder = !!rec && rec.championId === userId;

  // Catálogo.
  const conquistas = [];

  conquistas.push({
    key: 'rei_rodada', title: 'Rei da Rodada', icon: 'crown',
    desc: 'Vença rodadas como o manager de maior pontuação.',
    ...tiered(wins, [1, 5, 10, 25]),
  });

  conquistas.push({
    key: 'podio', title: 'Pódio', icon: 'medal',
    desc: 'Termine no top 3 de uma rodada.',
    ...tiered(podiums, [1, 5, 15, 30]),
  });

  conquistas.push({
    key: 'embalado', title: 'Embalado', icon: 'flame',
    desc: 'Vença rodadas consecutivas.',
    ...tiered(maxStreak, [2, 3, 5]),
  });

  conquistas.push({
    key: 'veterano', title: 'Veterano', icon: 'shield',
    desc: 'Participe de rodadas escalando seu time.',
    ...tiered(roundsPlayed, [5, 15, 30, 50]),
  });

  const noite = tiered(bestRoundScore, [10, 20, 30]);
  noite.value = Number(bestRoundScore.toFixed(1));
  conquistas.push({
    key: 'noite_iluminada', title: 'Noite Iluminada', icon: 'star',
    desc: 'Faça uma grande pontuação numa única rodada.',
    ...noite,
  });

  conquistas.push({
    key: 'campeao_temporada', title: 'Campeão de Temporada', icon: 'trophy',
    desc: 'Conquiste o título de uma temporada.',
    ...tiered(seasonTitles, [1, 3, 5]),
  });

  conquistas.push({
    key: 'recordista', title: 'Recordista', icon: 'zap',
    desc: 'Detenha o recorde de pontuação numa rodada deste Rei da Mesa.',
    value: isRecordHolder ? Number((rec.topScore || 0).toFixed(1)) : 0,
    level: isRecordHolder ? 1 : 0, maxLevel: 1, goal: null,
    maxed: isRecordHolder, unlocked: isRecordHolder,
  });

  const unlocked = conquistas.filter((c) => c.unlocked).length;

  return {
    summary: { unlocked, total: conquistas.length, roundsPlayed, wins, podiums, seasonTitles, bestRoundScore: Number(bestRoundScore.toFixed(1)) },
    conquistas,
  };
}
