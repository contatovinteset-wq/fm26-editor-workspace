import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

// 🎮 Gamificação (G1) — Temporadas.
// creatorId é sempre injetado pelas rotas (req.creatorId) e passado explícito,
// mantendo o scoping multi-tenant — mesmo padrão do ReiDaMesaAdminService.

// Retorna a temporada ativa do criador. Se não houver nenhuma, cria a "Temporada 1".
// Idempotente: usado tanto na captura da rodada quanto nas telas.
export async function getActiveSeason(creatorId) {
  let season = await prisma.season.findFirst({
    where: { creatorId, isActive: true },
    orderBy: { number: 'desc' },
  });
  if (season) return season;

  // Nenhuma ativa: descobre o próximo número e cria.
  const last = await prisma.season.findFirst({
    where: { creatorId },
    orderBy: { number: 'desc' },
    select: { number: true },
  });
  const number = (last?.number || 0) + 1;
  season = await prisma.season.create({
    data: { creatorId, number, isActive: true },
  });
  return season;
}

// Classificação de uma temporada: soma das pontuações de cada viewer nas rodadas
// daquela temporada (a partir do histórico RoundEntry). Resolve nicks/avatares.
export async function getSeasonStandings(creatorId, seasonId) {
  if (!seasonId) return [];

  const grouped = await prisma.roundEntry.groupBy({
    by: ['userId'],
    where: { creatorId, seasonId },
    _sum: { score: true },
    _count: { _all: true },
  });

  if (grouped.length === 0) return [];

  const users = await prisma.user.findMany({
    where: { id: { in: grouped.map((g) => g.userId) } },
    select: { id: true, nickname: true, name: true, avatar: true },
  });
  const byId = new Map(users.map((u) => [u.id, u]));

  return grouped
    .map((g) => {
      const u = byId.get(g.userId);
      return {
        userId: g.userId,
        nickname: u?.nickname || u?.name || 'Viewer',
        avatar: u?.avatar || null,
        score: Number((g._sum.score || 0).toFixed(2)),
        rounds: g._count._all,
      };
    })
    .sort((a, b) => b.score - a.score);
}

// Encerra a temporada ativa: coroa o campeão (líder da classificação da temporada),
// fecha a temporada e abre a próxima. Retorna { closed, next }.
export async function closeActiveSeason(creatorId) {
  const active = await getActiveSeason(creatorId);

  const standings = await getSeasonStandings(creatorId, active.id);
  const champion = standings[0] || null;

  const closed = await prisma.season.update({
    where: { id: active.id },
    data: {
      isActive: false,
      endedAt: new Date(),
      championId: champion?.userId || null,
      championName: champion?.nickname || null,
      championScore: champion ? champion.score : null,
    },
  });

  const next = await prisma.season.create({
    data: { creatorId, number: active.number + 1, isActive: true },
  });

  return { closed, next };
}

// Lista todas as temporadas do criador (ativa + encerradas, mais recentes primeiro).
export async function listSeasons(creatorId) {
  return prisma.season.findMany({
    where: { creatorId },
    orderBy: { number: 'desc' },
  });
}

export { prisma as seasonPrisma };
