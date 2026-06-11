import express from 'express';
import { PrismaClient } from '@prisma/client';
import multer from 'multer';
import { processPlantelHtml, previewMatchResultHtml, processMatchResultFinal } from '../services/ReiDaMesaAdminService.js';
import { requireAuth, requireRoles } from '../middleware/roles.js';
import { getDefaultCreatorId } from '../services/creatorContext.js';

const router = express.Router();
const prisma = new PrismaClient();
const upload = multer({ storage: multer.memoryStorage() });

// Ranking Geral
router.get('/ranking', async (req, res) => {
  try {
    const squads = await prisma.squad.findMany({
      include: { user: { select: { nickname: true, name: true, avatar: true, twitchId: true } } },
      orderBy: { totalScore: 'desc' },
      take: 50,
    });
    res.json(squads);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar ranking do BD' });
  }
});

// Top 3 Jogadores e Bagre da ÚLTIMA rodada fechada
router.get('/top-match', async (req, res) => {
  try {
    const lastRound = await prisma.round.findFirst({
      where: { scores: { some: {} } },
      orderBy: { number: 'desc' },
    });
    if (!lastRound) return res.json({ top3: [], bagre: null });

    const topScores = await prisma.playerScore.findMany({
      where: { roundId: lastRound.id },
      orderBy: { points: 'desc' },
      take: 3,
      include: { player: true }
    });

    const bagreInfo = lastRound.bagreId 
        ? await prisma.player.findUnique({ where: { id: lastRound.bagreId } })
        : null;

    res.json({
        top3: topScores.map(score => ({ ...score.player, matchPoints: score.points })),
        bagre: bagreInfo
    });
  } catch(error) {
    res.status(500).json({ error: 'Erro ao buscar dados do top match' });
  }
});

// Busca Jogadores Elegíveis p/ Escalação
router.get('/players', async (req, res) => {
  try {
    const rounds = await prisma.round.findMany({
      orderBy: { number: 'desc' },
      take: 2
    });
    
    const roundIds = rounds.map(r => r.id);

    const players = await prisma.player.findMany({ 
       where: { eligible: true },
       include: { scores: { where: { roundId: { in: roundIds } } } }
    });

    const currentRoundId = rounds[0]?.id;
    const prevRoundId = rounds[1]?.id;

    const mappedPlayers = players.map(p => {
       const currentScore = p.scores?.find(s => s.roundId === currentRoundId);
       const prevScore = p.scores?.find(s => s.roundId === prevRoundId);
       
       const { scores, ...playerData } = p;
       return { 
          ...playerData, 
          matchPoints: currentScore ? currentScore.points : null,
          details: currentScore ? currentScore.details : null,
          lastMatchPoints: prevScore ? prevScore.points : 0 
       };
    });

    res.json(mappedPlayers);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar jogadores' });
  }
});

// Busca TODOS os jogadores para o painel Admin (inclui inativos e stats)
router.get('/players/all', requireRoles(['OWNER', 'ADMIN_GERACAO']), async (req, res) => {
  try {
    const players = await prisma.player.findMany();
    // parse rawStats
    const parsed = players.map(p => ({
       ...p,
       rawStats: p.rawStats ? (typeof p.rawStats === 'string' ? JSON.parse(p.rawStats) : p.rawStats) : {}
    }));
    res.json(parsed);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao buscar todos os jogadores' });
  }
});

// Endpoint Temporário/Administrativo para Resetar Scores Manuais e Início de Temporada
router.post('/squads/reset-points', requireRoles(['OWNER', 'ADMIN_GERACAO']), async (req, res) => {
  try {
    const deletedScores = await prisma.playerScore.deleteMany({});
    const updated = await prisma.squad.updateMany({
      data: {
        roundScore: 0,
        totalScore: 0
      }
    });
    res.json({ success: true, message: `Histórico apagado (${deletedScores.count} scores). Pontuações de ${updated.count} esquadrões resetadas para 0.` });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao resetar pontuações', details: error.message });
  }
});

// Deleta TODOS os jogadores do painel Admin (Truncate Plantel)
router.delete('/players/all', requireRoles(['OWNER', 'ADMIN_GERACAO']), async (req, res) => {
  try {
    // 1. Desvincula todos os jogadores dos elencos ativos para evitar erros de FK
    await prisma.squad.updateMany({
      data: {
        defensorId: null,
        meioId: null,
        ataqueId: null,
        bagreId: null,
        bancoId: null
      }
    });

    // 2. Desvincula o 'Bagre' das rodadas passadas
    await prisma.round.updateMany({
      data: { bagreId: null }
    });

    // 3. Deleta todas as pontuações registradas de jogadores
    await prisma.playerScore.deleteMany({});

    // 4. (Opcional, mas seguro) Zera a pontuação dos squads caso resete o jogo pro começo
    await prisma.squad.updateMany({
      data: {
        roundScore: 0,
        totalScore: 0
      }
    });

    // 5. Deleta todos os Jogadores do banco (O FK não vai estourar mais)
    await prisma.player.deleteMany({});

    res.json({ success: true, message: 'Elenco deletado com sucesso!' });
  } catch (error) {
    console.error("Erro ao deletar o plantel:", error);
    res.status(500).json({ error: 'Erro ao deletar jogadores' });
  }
});

// Patch da Role do Cartola
router.patch('/players/:id/role', requireRoles(['OWNER', 'ADMIN_GERACAO']), async (req, res) => {
  try {
    const { id } = req.params;
    const { cartolaRole } = req.body;
    await prisma.player.update({
      where: { id },
      data: { cartolaRole }
    });
    res.json({ success: true });
  } catch (err) {
    res.status(500).json({ error: 'Erro ao assinalar role' });
  }
});

// Busca Escalação do Usuário
router.get('/squad', requireAuth, async (req, res) => {
  try {
    const rounds = await prisma.round.findMany({
      orderBy: { number: 'desc' },
      take: 2
    });

    const roundIds = rounds.map(r => r.id);

    const creatorId = await getDefaultCreatorId(prisma);
    const squad = await prisma.squad.findUnique({
      where: { userId_creatorId: { userId: req.user.id, creatorId } },
      include: {
        defensor: { include: { scores: { where: { roundId: { in: roundIds } } } } },
        meio: { include: { scores: { where: { roundId: { in: roundIds } } } } },
        ataque: { include: { scores: { where: { roundId: { in: roundIds } } } } },
        bagre: { include: { scores: { where: { roundId: { in: roundIds } } } } },
        capitao: { include: { scores: { where: { roundId: { in: roundIds } } } } }
      }
    });

    const currentRoundId = rounds[0]?.id;
    const prevRoundId = rounds[1]?.id;

    if (squad) {
       const mapPoints = (p) => {
          if (!p) return null;
          const currentScore = p.scores?.find(s => s.roundId === currentRoundId);
          const prevScore = p.scores?.find(s => s.roundId === prevRoundId);

          p.matchPoints = currentScore ? currentScore.points : null;
          p.details = currentScore ? currentScore.details : null;
          p.lastMatchPoints = prevScore ? prevScore.points : 0;
          return p;
       };
       squad.defensor = mapPoints(squad.defensor);
       squad.meio = mapPoints(squad.meio);
       squad.ataque = mapPoints(squad.ataque);
       squad.bagre = mapPoints(squad.bagre);
       squad.capitao = mapPoints(squad.capitao);
    }

    res.json(squad || null);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar esquadrão' });
  }
});

// Salvar Escalação do Usuário
router.post('/squad', requireAuth, async (req, res) => {
  try {
    if (!(await getMarketOpen())) return res.status(403).json({ error: 'Mercado Fechado' });
    const { defensorId, meioId, ataqueId, bagreId, capitaoId } = req.body;

    const creatorId = await getDefaultCreatorId(prisma);
    const squad = await prisma.squad.upsert({
      where: { userId_creatorId: { userId: req.user.id, creatorId } },
      update: { defensorId, meioId, ataqueId, bancoId: null, bagreId, capitaoId },
      create: { userId: req.user.id, creatorId, defensorId, meioId, ataqueId, bancoId: null, bagreId, capitaoId }
    });

    const isFullSquad = defensorId && meioId && ataqueId && bagreId && capitaoId;

    if (isFullSquad && !squad.overlayNotified) {
        await emitOverlay('NEW_SQUAD', { user: req.user.nickname || req.user.name || 'Viewer' });
        await prisma.squad.update({ where: { id: squad.id }, data: { overlayNotified: true } });
    } else if (!isFullSquad && squad.overlayNotified) {
        // Se limpou o time, permite que apareça na overlay quando escalar de novo
        await prisma.squad.update({ where: { id: squad.id }, data: { overlayNotified: false } });
    }

    res.json(squad);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao salvar escalação' });
  }
});

// STATUS DO MERCADO (persistido no banco — sobrevive a deploy/restart)
async function getMarketOpen() {
  const state = await prisma.reiDaMesaState.findUnique({ where: { id: 1 } });
  return state?.isMarketOpen ?? false;
}
async function setMarketOpen(isOpen) {
  await prisma.reiDaMesaState.upsert({
    where: { id: 1 },
    update: { isMarketOpen: isOpen },
    create: { id: 1, isMarketOpen: isOpen }
  });
}

// Insere um evento de overlay no banco (substitui a fila em memória).
async function emitOverlay(type, payload = {}) {
  await prisma.overlayEvent.create({ data: { type, payload } });
  // Limpeza: mantém o histórico enxuto (remove eventos com mais de 6h).
  await prisma.overlayEvent.deleteMany({
    where: { createdAt: { lt: new Date(Date.now() - 6 * 60 * 60 * 1000) } }
  });
}

router.get('/status', async (req, res) => {
  res.json({ isOpen: await getMarketOpen() });
});

router.post('/status', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO']), async (req, res) => {
  try {
    if (typeof req.body.isOpen === 'boolean') {
      const wasOpen = await getMarketOpen();
      const nowOpen = req.body.isOpen;
      await setMarketOpen(nowOpen);

      // Se o mercado for ABERTO agora, assumimos que uma Nova Rodada começou
      if (!wasOpen && nowOpen) {
         await prisma.squad.updateMany({ data: { overlayNotified: false } }); // Limpa notificações pro novo overlay da rodada
         const currentOpen = await prisma.round.findFirst({ where: { isOpen: true } });
         if (currentOpen) {
            // Fecha definitivamente
            await prisma.round.update({
               where: { id: currentOpen.id },
               data: { isOpen: false, isFinished: true }
            });
         }

         // As escolhas do Squad são mantidas para que o viewer possa ver seus desempenhos,
         // E também caso ele não escale novamente, mantém os mesmos jogadores na rodada atual.

         // Cria a próxima rodada
         const creatorId = await getDefaultCreatorId(prisma);
         const lastRound = await prisma.round.findFirst({ where: { creatorId }, orderBy: { number: 'desc' } });
         const nextNumber = lastRound ? lastRound.number + 1 : 1;

         await prisma.round.create({
            data: { number: nextNumber, isOpen: true, creatorId }
         });

         await prisma.squad.updateMany({
            data: { roundScore: 0 }
         });
      }
    }
    res.json({ isOpen: await getMarketOpen() });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao alterar status do mercado', details: error.message });
  }
});

// Busca últimas 5 rodadas
router.get('/rounds', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO']), async (req, res) => {
  try {
    const rounds = await prisma.round.findMany({
      orderBy: { number: 'desc' },
      take: 5
    });
    res.json(rounds);
  } catch (err) {
    res.status(500).json({ error: 'Erro ao buscar rodadas' });
  }
});

// Anular Rodada
router.delete('/round/:id', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO']), async (req, res) => {
  try {
    const { id } = req.params;
    
    const round = await prisma.round.findUnique({ where: { id } });
    if (!round) return res.status(404).json({ error: 'Rodada não encontrada' });

    await prisma.$transaction(async (tx) => {
      const squads = await tx.squad.findMany();
      for (const sq of squads) {
        if (sq.roundScore !== 0) {
          await tx.squad.update({
            where: { id: sq.id },
            data: {
              totalScore: { decrement: sq.roundScore },
              roundScore: 0
            }
          });
        }
      }

      await tx.playerScore.deleteMany({ where: { roundId: id } });

      await tx.round.update({
        where: { id },
        data: { bagreId: null }
      });
    });

    res.json({ success: true, message: 'Rodada anulada com sucesso!' });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao anular rodada' });
  }
});

// ---- CRAQUE DO JOGO (Votação) ----
router.get('/craque/status', requireAuth, async (req, res) => {
  try {
    const currentRound = await prisma.round.findFirst({ where: { isOpen: true } });
    if (!currentRound) return res.json({ mode: 'CLOSED' });

    // Mercado aberto = jogo não começou
    if (await getMarketOpen()) return res.json({ mode: 'CLOSED' });

    // Jogadores elegíveis para voto no front (usaremos os que não são bagre etc., ou todos ativos do html)
    // Se a rodada já tem bagreId, significa que a partida acabou e já foi processada.
    if (currentRound.bagreId) {
      return res.json({ mode: 'RESULTS', roundId: currentRound.id });
    }

    const userVote = await prisma.craqueVote.findUnique({
      where: { userId_roundId: { userId: req.user.id, roundId: currentRound.id } },
      include: { player: true }
    });

    res.json({ mode: 'VOTING', roundId: currentRound.id, userVote: userVote ? userVote.player : null });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao buscar status do craque' });
  }
});

router.post('/craque/vote', requireAuth, async (req, res) => {
  try {
    const { playerId } = req.body;
    const currentRound = await prisma.round.findFirst({ where: { isOpen: true } });
    
    if (!currentRound || (await getMarketOpen()) || currentRound.bagreId) {
       return res.status(403).json({ error: 'Votação não permitida neste momento.' });
    }

    const creatorId = await getDefaultCreatorId(prisma);
    const vote = await prisma.craqueVote.upsert({
      where: { userId_roundId: { userId: req.user.id, roundId: currentRound.id } },
      update: { playerId },
      create: { userId: req.user.id, roundId: currentRound.id, playerId, creatorId }
    });

    res.json({ success: true, vote });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao salvar voto' });
  }
});

router.get('/craque/results', async (req, res) => {
  try {
    const currentRound = await prisma.round.findFirst({ where: { isOpen: true } });
    if (!currentRound) return res.json({ top3: [], totalVotes: 0 });

    const votesCount = await prisma.craqueVote.groupBy({
      by: ['playerId'],
      where: { roundId: currentRound.id },
      _count: { playerId: true },
      orderBy: { _count: { playerId: 'desc' } },
      take: 3
    });

    const totalVotes = await prisma.craqueVote.count({
      where: { roundId: currentRound.id }
    });

    const top3 = await Promise.all(votesCount.map(async (v) => {
       const player = await prisma.player.findUnique({ where: { id: v.playerId } });
       return {
          ...player,
          votes: v._count.playerId,
          percentage: totalVotes > 0 ? Math.round((v._count.playerId / totalVotes) * 100) : 0
       };
    }));

    res.json({ top3, totalVotes });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao buscar resultados' });
  }
});

// ---- OBS OVERLAY (POLLING — eventos persistidos no banco) ----
// FrontEnd acessa a cada 2s passando o ID do último evento que ele viu
router.get('/overlay/poll', async (req, res) => {
  try {
    const since = parseInt(req.query.since || '0', 10);

    // Se o front pede um id maior que o último existente (ex.: tabela recém-criada
    // pós-deploy, ou histórico limpo), pede pra ele ressincronizar pro topo atual.
    const last = await prisma.overlayEvent.findFirst({ orderBy: { id: 'desc' } });
    const maxId = last?.id || 0;
    if (since > maxId) {
      return res.json({ resetSync: true, events: [] });
    }

    const events = await prisma.overlayEvent.findMany({
      where: { id: { gt: since } },
      orderBy: { id: 'asc' },
      take: 20
    });
    res.json({ events: events.map(e => ({ id: e.id, type: e.type, ...(e.payload || {}) })) });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro no poll do overlay' });
  }
});

router.post('/overlay/test', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'ADMIN']), async (req, res) => {
  await emitOverlay('NEW_SQUAD', { user: 'Teste da Live' });
  res.json({ success: true });
});

export default router;

// ADMIN ROUTES: UPLOADS 
router.post('/upload-plantel', requireAuth, requireRoles('OWNER', 'ADMIN_GERACAO'), upload.single('file'), async (req, res) => {
  try {
    if (!req.file) return res.status(400).json({ error: 'Nenhum arquivo enviado.' });
    const htmlString = req.file.buffer.toString('utf-8');
    const result = await processPlantelHtml(htmlString);
    res.json(result);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: error.message });
  }
});

router.post('/upload-match', requireAuth, requireRoles('OWNER', 'ADMIN_GERACAO'), upload.single('file'), async (req, res) => {
  try {
    if (!req.file) return res.status(400).json({ error: 'Nenhum arquivo enviado.' });
    const htmlString = req.file.buffer.toString('utf-8');
    const result = await previewMatchResultHtml(htmlString);
    res.json(result);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: error.message });
  }
});

router.post('/process-match-final', requireAuth, requireRoles('OWNER', 'ADMIN_GERACAO'), async (req, res) => {
  try {
    const { scores } = req.body;
    if (!scores || !Array.isArray(scores)) {
      return res.status(400).json({ error: 'Array de scores inválido.' });
    }
    const result = await processMatchResultFinal(scores);
    res.json(result);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: error.message });
  }
});
