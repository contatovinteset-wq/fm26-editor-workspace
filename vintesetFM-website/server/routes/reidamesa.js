import express from 'express';
import { PrismaClient } from '@prisma/client';
import multer from 'multer';
import { processPlantelHtml, processMatchResultHtml } from '../services/ReiDaMesaAdminService.js';
import { requireAuth, requireRoles } from '../middleware/auth.js';

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

// Busca Escalação do Usuário
router.get('/squad', requireAuth, async (req, res) => {
  try {
    const squad = await prisma.squad.findUnique({
      where: { userId: req.user.id },
      include: {
        defensor: true, meio: true, ataque: true, banco: true, bagre: true
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
    const { defensorId, meioId, ataqueId, bancoId, bagreId } = req.body;
    
    const squad = await prisma.squad.upsert({
      where: { userId: req.user.id },
      update: { defensorId, meioId, ataqueId, bancoId, bagreId },
      create: { userId: req.user.id, defensorId, meioId, ataqueId, bancoId, bagreId }
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
