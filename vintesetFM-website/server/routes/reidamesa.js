import express from 'express';
import { PrismaClient } from '@prisma/client';
import multer from 'multer';
import { processPlantelHtml, processMatchResultHtml } from '../services/ReiDaMesaAdminService.js';
import { requireAuth, requireRoles } from '../middleware/roles.js';

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
    const players = await prisma.player.findMany({ where: { eligible: true } });
    res.json(players);
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
    const lastRound = await prisma.round.findFirst({
      orderBy: { number: 'desc' },
    });

    const includeScores = lastRound ? { where: { roundId: lastRound.id } } : false;

    const squad = await prisma.squad.findUnique({
      where: { userId: req.user.id },
      include: {
        defensor: { include: { scores: includeScores } },
        meio: { include: { scores: includeScores } },
        ataque: { include: { scores: includeScores } },
        bagre: { include: { scores: includeScores } }
      }
    });

    // Injeta a property matchPoints no root de cada player pra facilitar no Front
    if (squad) {
       const mapPoints = (p) => {
          if (!p) return null;
          p.matchPoints = p.scores && p.scores.length > 0 ? p.scores[0].points : null;
          return p;
       };
       squad.defensor = mapPoints(squad.defensor);
       squad.meio = mapPoints(squad.meio);
       squad.ataque = mapPoints(squad.ataque);
       squad.bagre = mapPoints(squad.bagre);
    }

    res.json(squad || null);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar esquadrão' });
  }
});

// Salvar Escalação do Usuário
router.post('/squad', requireAuth, async (req, res) => {
  try {
    if (!isMarketOpen) return res.status(403).json({ error: 'Mercado Fechado' });
    const { defensorId, meioId, ataqueId, bagreId } = req.body;
    
    // Deixamos bancoId gravado caso haja DB velho, mas sempre null agora
    const squad = await prisma.squad.upsert({
      where: { userId: req.user.id },
      update: { defensorId, meioId, ataqueId, bancoId: null, bagreId },
      create: { userId: req.user.id, defensorId, meioId, ataqueId, bancoId: null, bagreId }
    });
    res.json(squad);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao salvar escalação' });
  }
});

// STATUS DO MERCADO (In-memory por enquanto, reseta ao reiniciar o servidor)
let isMarketOpen = true;

router.get('/status', (req, res) => {
  res.json({ isOpen: isMarketOpen });
});

router.post('/status', requireAuth, requireRoles(['OWNER', 'ADMIN_GERACAO']), async (req, res) => {
  try {
    if (typeof req.body.isOpen === 'boolean') {
      const wasOpen = isMarketOpen;
      isMarketOpen = req.body.isOpen;

      // Se o mercado for ABERTO agora, assumimos que uma Nova Rodada começou 
      if (!wasOpen && isMarketOpen) {
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
         const lastRound = await prisma.round.findFirst({ orderBy: { number: 'desc' } });
         const nextNumber = lastRound ? lastRound.number + 1 : 1;
         
         await prisma.round.create({
            data: { number: nextNumber, isOpen: true }
         });

         await prisma.squad.updateMany({
            data: { roundScore: 0 }
         });
      }
    }
    res.json({ isOpen: isMarketOpen });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Erro ao alterar status do mercado', details: error.message });
  }
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
    const result = await processMatchResultHtml(htmlString);
    res.json(result);
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: error.message });
  }
});
