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
      include: { user: { select: { name: true, avatar: true, twitchId: true } } },
      orderBy: { totalScore: 'desc' },
      take: 50,
    });
    res.json(squads);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar ranking do BD' });
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
    await prisma.player.deleteMany({});
    res.json({ success: true, message: 'Elenco deletado com sucesso!' });
  } catch (error) {
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
    const squad = await prisma.squad.findUnique({
      where: { userId: req.user.id },
      include: {
        defensor: true, meio: true, ataque: true, bagre: true
      }
    });
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

router.post('/status', (req, res) => {
  // TODO: Adicionar middleware de autenticação (requireRoles) depois
  if (typeof req.body.isOpen === 'boolean') {
    isMarketOpen = req.body.isOpen;
  }
  res.json({ isOpen: isMarketOpen });
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
