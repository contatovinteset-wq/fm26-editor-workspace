import express from 'express';
import { PrismaClient } from '@prisma/client';
import multer from 'multer';
import { processPlantelHtml, previewMatchResultHtml, processMatchResultFinal } from '../services/ReiDaMesaAdminService.js';
import { requireAuth, requireRoles } from '../middleware/roles.js';
import { attachCreatorContext, getDefaultCreatorId, RESERVED_SLUGS } from '../services/creatorContext.js';
import { getLivePlatforms } from '../services/creatorLiveService.js';

const router = express.Router();
const prisma = new PrismaClient();
const upload = multer({ storage: multer.memoryStorage() });

// Injeta req.creatorId em todo request do Rei da Mesa (Fase 3a/3c).
router.use(attachCreatorContext(prisma));

// Middleware (Fase 3d): autoriza gerir o Rei da Mesa do creator ALVO (req.creatorId).
// OWNER manda em tudo; o dono do creator gere o seu; ADMIN_GERACAO mantém poder
// no creator default (vinteset) por retrocompat. Exige requireAuth antes.
async function requireCreatorManager(req, res, next) {
  try {
    const roles = req.user?.roles || [];
    if (roles.includes('OWNER')) return next();

    const creator = await prisma.creator.findUnique({
      where: { id: req.creatorId },
      select: { ownerId: true }
    });
    if (creator && creator.ownerId === req.user.id) return next();

    if (roles.includes('ADMIN_GERACAO')) {
      const defId = await getDefaultCreatorId(prisma);
      if (req.creatorId === defId) return next();
    }

    return res.status(403).json({ error: 'Você não administra este Rei da Mesa.' });
  } catch (err) {
    console.error('requireCreatorManager:', err);
    return res.status(500).json({ error: 'Erro de autorização' });
  }
}

// 🎥 Diretório público de criadores ativos (Fase 3c) — alimenta /reidamesa/criadores.
router.get('/creators', async (req, res) => {
  try {
    const creators = await prisma.creator.findMany({
      where: { isActive: true },
      select: { name: true, slug: true, branding: true },
      orderBy: { createdAt: 'asc' }
    });

    // Status ao vivo (best-effort, cacheado). ?live=0 pula a checagem (mais rápido).
    if (req.query.live === '0') {
      return res.json(creators.map((c) => ({ ...c, livePlatforms: [], isLive: false })));
    }
    const withLive = await Promise.all(creators.map(async (c) => {
      const livePlatforms = await getLivePlatforms(c.branding).catch(() => []);
      return { ...c, livePlatforms, isLive: livePlatforms.length > 0 };
    }));
    res.json(withLive);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao listar criadores' });
  }
});

// Lista TODOS os criadores (ativos e inativos) — só OWNER, p/ a gestão (Fase 3e).
router.get('/creators/all', requireAuth, requireRoles(['OWNER']), async (req, res) => {
  try {
    const creators = await prisma.creator.findMany({
      select: {
        name: true, slug: true, branding: true, isActive: true, createdAt: true,
        owner: { select: { nickname: true, name: true } }
      },
      orderBy: { createdAt: 'asc' }
    });
    res.json(creators.map((c) => ({
      name: c.name, slug: c.slug, branding: c.branding, isActive: c.isActive,
      ownerName: c.owner?.nickname || c.owner?.name || '—'
    })));
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao listar criadores' });
  }
});

// Branding/validação de um criador pela slug (Fase 3c) — usado pelo CreatorContext.
router.get('/creator/:slug', async (req, res) => {
  try {
    const slug = (req.params.slug || '').trim().toLowerCase();
    const creator = await prisma.creator.findFirst({
      where: { slug, isActive: true },
      select: { name: true, slug: true, branding: true }
    });
    if (!creator) return res.status(404).json({ error: 'Criador não encontrado' });
    res.json(creator);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao buscar criador' });
  }
});

// ➕ Criar um novo criador (Fase 3d) — só OWNER. Define o dono (por email; sem
// email, o próprio OWNER) e concede o cargo CREATOR a ele.
router.post('/creators', requireAuth, requireRoles(['OWNER']), async (req, res) => {
  try {
    const { name, branding, ownerEmail } = req.body;
    const slug = (req.body.slug || '').trim().toLowerCase();

    if (!name || !name.trim()) return res.status(400).json({ error: 'Nome é obrigatório.' });
    if (!/^[a-z0-9-]{2,30}$/.test(slug)) {
      return res.status(400).json({ error: 'Slug inválida (use 2-30 caracteres: a-z, 0-9, hífen).' });
    }
    if (RESERVED_SLUGS.has(slug)) return res.status(400).json({ error: 'Essa slug é reservada.' });

    // Resolve o dono: por email informado, ou o próprio OWNER logado.
    let owner = req.user;
    if (ownerEmail && ownerEmail.trim()) {
      owner = await prisma.user.findUnique({ where: { email: ownerEmail.trim().toLowerCase() } });
      if (!owner) return res.status(404).json({ error: 'Usuário (dono) não encontrado por esse email.' });
    }

    const exists = await prisma.creator.findUnique({ where: { slug } });
    if (exists) return res.status(409).json({ error: 'Já existe um criador com essa slug.' });

    const creator = await prisma.creator.create({
      data: { name: name.trim(), slug, branding: branding || undefined, ownerId: owner.id }
    });

    // Concede CREATOR ao dono (se ainda não tiver).
    let roles = owner.roles;
    if (typeof roles === 'string') { try { roles = JSON.parse(roles); } catch { roles = [roles]; } }
    if (!Array.isArray(roles)) roles = [];
    if (!roles.includes('CREATOR') && !roles.includes('OWNER')) {
      await prisma.user.update({ where: { id: owner.id }, data: { roles: [...roles, 'CREATOR'] } });
    }

    res.status(201).json(creator);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao criar criador', details: error.message });
  }
});

// ✏️ Editar criador (nome/branding/ativo) — OWNER ou o próprio dono.
router.patch('/creator/:slug', requireAuth, async (req, res) => {
  try {
    const slug = (req.params.slug || '').trim().toLowerCase();
    const creator = await prisma.creator.findFirst({ where: { slug } });
    if (!creator) return res.status(404).json({ error: 'Criador não encontrado' });

    const roles = req.user?.roles || [];
    if (!roles.includes('OWNER') && creator.ownerId !== req.user.id) {
      return res.status(403).json({ error: 'Você não administra este criador.' });
    }

    const { name, branding, isActive } = req.body;
    const data = {};
    if (name !== undefined) data.name = String(name).trim();
    if (branding !== undefined) data.branding = branding;

    // Critério de ativação (Fase 3e): nome de exibição + ao menos 1 plataforma.
    const finalName = data.name !== undefined ? data.name : creator.name;
    const finalBranding = data.branding !== undefined ? data.branding : creator.branding;
    const pf = (finalBranding && finalBranding.platforms) || {};
    const hasPlatform = !!(pf.twitch || pf.kick || pf.youtube || (finalBranding && finalBranding.liveUrl));
    const meets = !!(finalName && finalName.trim()) && hasPlatform;

    // Desativar é sempre permitido; ativar (auto ou explícito) só cumprindo o critério.
    data.isActive = isActive === false ? false : meets;

    const updated = await prisma.creator.update({ where: { id: creator.id }, data });
    res.json({ ...updated, meetsActivation: meets });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao editar criador', details: error.message });
  }
});

// 🗑️ Excluir criador (Fase 3e) — só OWNER, e só se NÃO tiver dados de jogo
// (senão orienta a desativar). O criador principal (vinteset) não pode ser excluído.
router.delete('/creator/:slug', requireAuth, requireRoles(['OWNER']), async (req, res) => {
  try {
    const slug = (req.params.slug || '').trim().toLowerCase();
    const creator = await prisma.creator.findFirst({ where: { slug } });
    if (!creator) return res.status(404).json({ error: 'Criador não encontrado' });

    const defId = await getDefaultCreatorId(prisma);
    if (creator.id === defId) {
      return res.status(400).json({ error: 'O criador principal não pode ser excluído.' });
    }

    const [players, rounds, squads, scores, votes] = await Promise.all([
      prisma.player.count({ where: { creatorId: creator.id } }),
      prisma.round.count({ where: { creatorId: creator.id } }),
      prisma.squad.count({ where: { creatorId: creator.id } }),
      prisma.playerScore.count({ where: { creatorId: creator.id } }),
      prisma.craqueVote.count({ where: { creatorId: creator.id } }),
    ]);
    if (players + rounds + squads + scores + votes > 0) {
      return res.status(409).json({ error: 'Esse Rei da Mesa já tem dados. Desative-o em vez de excluir.' });
    }

    // Sem dados de jogo: limpa estado/overlay e remove.
    await prisma.overlayEvent.deleteMany({ where: { creatorId: creator.id } });
    await prisma.reiDaMesaState.deleteMany({ where: { creatorId: creator.id } });
    await prisma.creator.delete({ where: { id: creator.id } });
    res.json({ success: true });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao excluir criador', details: error.message });
  }
});

// 🛠️ "Meu criador" — qual creator o usuário logado administra (Fase 3d).
// OWNER recebe o default (vinteset). CREATOR recebe o seu. Usado pelo painel.
router.get('/my-creator', requireAuth, async (req, res) => {
  try {
    const roles = req.user?.roles || [];
    // Acha o criador do dono mesmo que INATIVO (ele precisa achar p/ preencher e ativar).
    let creator = await prisma.creator.findFirst({
      where: { ownerId: req.user.id },
      select: { name: true, slug: true, branding: true, isActive: true },
      orderBy: { createdAt: 'asc' }
    });
    if (!creator && roles.includes('OWNER')) {
      const defId = await getDefaultCreatorId(prisma);
      creator = await prisma.creator.findUnique({
        where: { id: defId },
        select: { name: true, slug: true, branding: true, isActive: true }
      });
    }
    if (!creator) return res.status(404).json({ error: 'Você não administra nenhum Rei da Mesa.' });
    res.json(creator);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao resolver seu criador' });
  }
});

// Ranking Geral
router.get('/ranking', async (req, res) => {
  try {
    const squads = await prisma.squad.findMany({
      where: { creatorId: req.creatorId },
      include: { user: { select: { nickname: true, name: true, avatar: true, twitchId: true } } },
      orderBy: { totalScore: 'desc' },
      take: 50,
    });
    res.json(squads);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar ranking do BD' });
  }
});

// 🏆 Sala de Troféus + Agregados (Fase 6) — tudo calculado sobre dados atuais,
// escopado por criador. Sem migration.
router.get('/trofeus', async (req, res) => {
  try {
    const creatorId = req.creatorId;

    const [liderSquad, squads, artilheiroAgg, totalManagers, totalRounds, lastFinished] = await Promise.all([
      prisma.squad.findFirst({
        where: { creatorId, totalScore: { gt: 0 } },
        orderBy: { totalScore: 'desc' },
        include: { user: { select: { nickname: true, name: true, avatar: true } } }
      }),
      prisma.squad.findMany({
        where: { creatorId },
        select: { defensorId: true, meioId: true, ataqueId: true, bagreId: true, capitaoId: true }
      }),
      prisma.playerScore.groupBy({
        by: ['playerId'],
        where: { creatorId },
        _sum: { points: true },
        orderBy: { _sum: { points: 'desc' } },
        take: 1
      }),
      prisma.squad.count({ where: { creatorId } }),
      prisma.round.count({ where: { creatorId } }),
      prisma.round.findFirst({
        where: { creatorId, bagreId: { not: null } },
        orderBy: { number: 'desc' },
        include: { bagre: { select: { name: true, uniqueId: true } } }
      }),
    ]);

    // Contagem do mais frequente numa lista de ids.
    const tally = (arr) => {
      const m = {};
      for (const id of arr) if (id) m[id] = (m[id] || 0) + 1;
      let best = null;
      for (const [id, count] of Object.entries(m)) if (!best || count > best.count) best = { id, count };
      return best;
    };
    const titularBest = tally(squads.flatMap((s) => [s.defensorId, s.meioId, s.ataqueId]));
    const bagreBest = tally(squads.map((s) => s.bagreId));
    const capitaoBest = tally(squads.map((s) => s.capitaoId));

    // Resolve nomes/fotos dos jogadores vencedores num único query.
    const ids = [titularBest?.id, bagreBest?.id, capitaoBest?.id, artilheiroAgg[0]?.playerId].filter(Boolean);
    const players = ids.length
      ? await prisma.player.findMany({ where: { id: { in: ids } }, select: { id: true, name: true, realPosition: true, uniqueId: true } })
      : [];
    const pMap = Object.fromEntries(players.map((p) => [p.id, p]));
    const mk = (best) => (best && pMap[best.id] ? { ...pMap[best.id], count: best.count } : null);

    res.json({
      lider: liderSquad ? {
        nickname: liderSquad.user?.nickname || liderSquad.user?.name || 'Manager',
        avatar: liderSquad.user?.avatar || null,
        totalScore: Number((liderSquad.totalScore || 0).toFixed(2))
      } : null,
      artilheiro: (artilheiroAgg[0] && pMap[artilheiroAgg[0].playerId]) ? {
        ...pMap[artilheiroAgg[0].playerId],
        points: Number((artilheiroAgg[0]._sum.points || 0).toFixed(2))
      } : null,
      maisEscalado: mk(titularBest),
      bagreMaisEscalado: mk(bagreBest),
      capitaoFavorito: mk(capitaoBest),
      bagreUltimaRodada: lastFinished?.bagre ? {
        name: lastFinished.bagre.name, uniqueId: lastFinished.bagre.uniqueId, roundNumber: lastFinished.number
      } : null,
      totals: { managers: totalManagers, rounds: totalRounds }
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao montar a Sala de Troféus' });
  }
});

// 📋 Escalações da Rodada (Fase 6) — tabela ao vivo dos picks de cada manager.
router.get('/escalacoes', async (req, res) => {
  try {
    const squads = await prisma.squad.findMany({
      where: { creatorId: req.creatorId, defensorId: { not: null } },
      include: {
        user: { select: { nickname: true, name: true, avatar: true } },
        defensor: { select: { id: true, name: true, uniqueId: true } },
        meio: { select: { id: true, name: true, uniqueId: true } },
        ataque: { select: { id: true, name: true, uniqueId: true } },
        bagre: { select: { id: true, name: true, uniqueId: true } },
      },
      orderBy: { updatedAt: 'desc' },
      take: 300,
    });
    res.json(squads.map((s) => ({
      manager: s.user?.nickname || s.user?.name || 'Manager',
      avatar: s.user?.avatar || null,
      def: s.defensor, mei: s.meio, ata: s.ataque, bagre: s.bagre,
      capitaoId: s.capitaoId,
      roundScore: s.roundScore,
    })));
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao buscar escalações' });
  }
});

// 🖼️ Print da escalação do criador — público (leitura) / manager (escrita).
router.get('/lineup-print', async (req, res) => {
  try {
    const c = await prisma.creator.findUnique({
      where: { id: req.creatorId },
      select: { lineupPrint: true, lineupPrintAt: true }
    });
    res.json({ image: c?.lineupPrint || null, updatedAt: c?.lineupPrintAt || null });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao buscar o print' });
  }
});

router.post('/lineup-print', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'CREATOR']), requireCreatorManager, async (req, res) => {
  try {
    const { image } = req.body; // dataURL base64, ou null para limpar
    if (image !== null && image !== undefined) {
      if (typeof image !== 'string' || !/^data:image\/(png|jpe?g|webp);base64,/.test(image)) {
        return res.status(400).json({ error: 'Formato de imagem inválido.' });
      }
      if (image.length > 3_000_000) {
        return res.status(413).json({ error: 'Imagem muito grande. Tente uma menor.' });
      }
    }
    await prisma.creator.update({
      where: { id: req.creatorId },
      data: { lineupPrint: image || null, lineupPrintAt: image ? new Date() : null }
    });
    res.json({ success: true });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao salvar o print' });
  }
});

// Top 3 Jogadores e Bagre da ÚLTIMA rodada fechada
router.get('/top-match', async (req, res) => {
  try {
    const lastRound = await prisma.round.findFirst({
      where: { creatorId: req.creatorId, scores: { some: {} } },
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
      where: { creatorId: req.creatorId },
      orderBy: { number: 'desc' },
      take: 2
    });

    const roundIds = rounds.map(r => r.id);

    const players = await prisma.player.findMany({
       where: { creatorId: req.creatorId, eligible: true },
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
router.get('/players/all', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'CREATOR']), requireCreatorManager, async (req, res) => {
  try {
    const players = await prisma.player.findMany({ where: { creatorId: req.creatorId } });
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
router.post('/squads/reset-points', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'CREATOR']), requireCreatorManager, async (req, res) => {
  try {
    const deletedScores = await prisma.playerScore.deleteMany({ where: { creatorId: req.creatorId } });
    const updated = await prisma.squad.updateMany({
      where: { creatorId: req.creatorId },
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
router.delete('/players/all', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'CREATOR']), requireCreatorManager, async (req, res) => {
  try {
    // 1. Desvincula todos os jogadores dos elencos ativos para evitar erros de FK
    await prisma.squad.updateMany({
      where: { creatorId: req.creatorId },
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
      where: { creatorId: req.creatorId },
      data: { bagreId: null }
    });

    // 3. Deleta todas as pontuações registradas de jogadores
    await prisma.playerScore.deleteMany({ where: { creatorId: req.creatorId } });

    // 4. (Opcional, mas seguro) Zera a pontuação dos squads caso resete o jogo pro começo
    await prisma.squad.updateMany({
      where: { creatorId: req.creatorId },
      data: {
        roundScore: 0,
        totalScore: 0
      }
    });

    // 5. Deleta todos os Jogadores do banco (O FK não vai estourar mais)
    await prisma.player.deleteMany({ where: { creatorId: req.creatorId } });

    res.json({ success: true, message: 'Elenco deletado com sucesso!' });
  } catch (error) {
    console.error("Erro ao deletar o plantel:", error);
    res.status(500).json({ error: 'Erro ao deletar jogadores' });
  }
});

// Patch da Role do Cartola
router.patch('/players/:id/role', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'CREATOR']), requireCreatorManager, async (req, res) => {
  try {
    const { id } = req.params;
    const { cartolaRole } = req.body;
    await prisma.player.updateMany({
      where: { id, creatorId: req.creatorId },
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
      where: { creatorId: req.creatorId },
      orderBy: { number: 'desc' },
      take: 2
    });

    const roundIds = rounds.map(r => r.id);

    const squad = await prisma.squad.findUnique({
      where: { userId_creatorId: { userId: req.user.id, creatorId: req.creatorId } },
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
    if (!(await getMarketOpen(req.creatorId))) return res.status(403).json({ error: 'Mercado Fechado' });
    const { defensorId, meioId, ataqueId, bagreId, capitaoId } = req.body;

    const squad = await prisma.squad.upsert({
      where: { userId_creatorId: { userId: req.user.id, creatorId: req.creatorId } },
      update: { defensorId, meioId, ataqueId, bancoId: null, bagreId, capitaoId },
      create: { userId: req.user.id, creatorId: req.creatorId, defensorId, meioId, ataqueId, bancoId: null, bagreId, capitaoId }
    });

    const isFullSquad = defensorId && meioId && ataqueId && bagreId && capitaoId;

    if (isFullSquad && !squad.overlayNotified) {
        await emitOverlay(req.creatorId, 'NEW_SQUAD', { user: req.user.nickname || req.user.name || 'Viewer' });
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

// STATUS DO MERCADO (persistido no banco — sobrevive a deploy/restart).
// Fase 3b: 1 estado por criador (chaveado por creatorId).
async function getMarketOpen(creatorId) {
  const state = await prisma.reiDaMesaState.findUnique({ where: { creatorId } });
  return state?.isMarketOpen ?? false;
}
async function setMarketOpen(creatorId, isOpen) {
  await prisma.reiDaMesaState.upsert({
    where: { creatorId },
    update: { isMarketOpen: isOpen },
    create: { creatorId, isMarketOpen: isOpen }
  });
}

// Insere um evento de overlay no banco (substitui a fila em memória).
// Fase 3b: cada evento pertence a um criador.
async function emitOverlay(creatorId, type, payload = {}) {
  await prisma.overlayEvent.create({ data: { creatorId, type, payload } });
  // Limpeza: mantém o histórico enxuto (remove eventos do criador com mais de 6h).
  await prisma.overlayEvent.deleteMany({
    where: { creatorId, createdAt: { lt: new Date(Date.now() - 6 * 60 * 60 * 1000) } }
  });
}

router.get('/status', async (req, res) => {
  res.json({ isOpen: await getMarketOpen(req.creatorId) });
});

router.post('/status', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'CREATOR']), requireCreatorManager, async (req, res) => {
  try {
    if (typeof req.body.isOpen === 'boolean') {
      const wasOpen = await getMarketOpen(req.creatorId);
      const nowOpen = req.body.isOpen;
      await setMarketOpen(req.creatorId, nowOpen);

      // Se o mercado for ABERTO agora, assumimos que uma Nova Rodada começou
      if (!wasOpen && nowOpen) {
         await prisma.squad.updateMany({ where: { creatorId: req.creatorId }, data: { overlayNotified: false } }); // Limpa notificações pro novo overlay da rodada
         const currentOpen = await prisma.round.findFirst({ where: { creatorId: req.creatorId, isOpen: true } });
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
         const lastRound = await prisma.round.findFirst({ where: { creatorId: req.creatorId }, orderBy: { number: 'desc' } });
         const nextNumber = lastRound ? lastRound.number + 1 : 1;

         await prisma.round.create({
            data: { number: nextNumber, isOpen: true, creatorId: req.creatorId }
         });

         await prisma.squad.updateMany({
            where: { creatorId: req.creatorId },
            data: { roundScore: 0 }
         });
      }
    }
    res.json({ isOpen: await getMarketOpen(req.creatorId) });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao alterar status do mercado', details: error.message });
  }
});

// Busca últimas 5 rodadas
router.get('/rounds', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'CREATOR']), requireCreatorManager, async (req, res) => {
  try {
    const rounds = await prisma.round.findMany({
      where: { creatorId: req.creatorId },
      orderBy: { number: 'desc' },
      take: 5
    });
    res.json(rounds);
  } catch (err) {
    res.status(500).json({ error: 'Erro ao buscar rodadas' });
  }
});

// Anular Rodada
router.delete('/round/:id', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'CREATOR']), requireCreatorManager, async (req, res) => {
  try {
    const { id } = req.params;
    
    const round = await prisma.round.findFirst({ where: { id, creatorId: req.creatorId } });
    if (!round) return res.status(404).json({ error: 'Rodada não encontrada' });

    await prisma.$transaction(async (tx) => {
      const squads = await tx.squad.findMany({ where: { creatorId: req.creatorId } });
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
    const currentRound = await prisma.round.findFirst({ where: { creatorId: req.creatorId, isOpen: true } });
    if (!currentRound) return res.json({ mode: 'CLOSED' });

    // Mercado aberto = jogo não começou
    if (await getMarketOpen(req.creatorId)) return res.json({ mode: 'CLOSED' });

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
    const currentRound = await prisma.round.findFirst({ where: { creatorId: req.creatorId, isOpen: true } });

    if (!currentRound || (await getMarketOpen(req.creatorId)) || currentRound.bagreId) {
       return res.status(403).json({ error: 'Votação não permitida neste momento.' });
    }

    const vote = await prisma.craqueVote.upsert({
      where: { userId_roundId: { userId: req.user.id, roundId: currentRound.id } },
      update: { playerId },
      create: { userId: req.user.id, roundId: currentRound.id, playerId, creatorId: req.creatorId }
    });

    res.json({ success: true, vote });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao salvar voto' });
  }
});

router.get('/craque/results', async (req, res) => {
  try {
    const currentRound = await prisma.round.findFirst({ where: { creatorId: req.creatorId, isOpen: true } });
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

    // Se o front pede um id maior que o último existente do criador (ex.: tabela
    // recém-criada pós-deploy, ou histórico limpo), pede pra ele ressincronizar.
    const last = await prisma.overlayEvent.findFirst({ where: { creatorId: req.creatorId }, orderBy: { id: 'desc' } });
    const maxId = last?.id || 0;
    if (since > maxId) {
      return res.json({ resetSync: true, events: [] });
    }

    const events = await prisma.overlayEvent.findMany({
      where: { creatorId: req.creatorId, id: { gt: since } },
      orderBy: { id: 'asc' },
      take: 20
    });
    res.json({ events: events.map(e => ({ id: e.id, type: e.type, ...(e.payload || {}) })) });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro no poll do overlay' });
  }
});

router.post('/overlay/test', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'ADMIN', 'CREATOR']), requireCreatorManager, async (req, res) => {
  await emitOverlay(req.creatorId, 'NEW_SQUAD', { user: 'Teste da Live' });
  res.json({ success: true });
});

export default router;

// ADMIN ROUTES: UPLOADS 
router.post('/upload-plantel', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'CREATOR']), requireCreatorManager, upload.single('file'), async (req, res) => {
  try {
    if (!req.file) return res.status(400).json({ error: 'Nenhum arquivo enviado.' });
    const htmlString = req.file.buffer.toString('utf-8');
    const result = await processPlantelHtml(htmlString, req.creatorId);
    res.json(result);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: error.message });
  }
});

router.post('/upload-match', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'CREATOR']), requireCreatorManager, upload.single('file'), async (req, res) => {
  try {
    if (!req.file) return res.status(400).json({ error: 'Nenhum arquivo enviado.' });
    const htmlString = req.file.buffer.toString('utf-8');
    const result = await previewMatchResultHtml(htmlString, req.creatorId);
    res.json(result);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: error.message });
  }
});

router.post('/process-match-final', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO', 'CREATOR']), requireCreatorManager, async (req, res) => {
  try {
    const { scores } = req.body;
    if (!scores || !Array.isArray(scores)) {
      return res.status(400).json({ error: 'Array de scores inválido.' });
    }
    const result = await processMatchResultFinal(scores, req.creatorId);

    // Emite o overlay de fim de rodada como evento persistido (por criador).
    if (result.overlayEvent) {
      const { type, ...payload } = result.overlayEvent;
      await emitOverlay(req.creatorId, type, payload);
    }

    res.json(result);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: error.message });
  }
});
