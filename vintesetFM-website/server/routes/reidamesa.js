import express from 'express';
import { PrismaClient } from '@prisma/client';

const router = express.Router();
const prisma = new PrismaClient();

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
