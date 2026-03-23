import express from 'express';
import { PrismaClient } from '@prisma/client';

const router = express.Router();
const prisma = new PrismaClient();

// Listar Tópicos
router.get('/', async (req, res) => {
  try {
    const topics = await prisma.topic.findMany({
      include: { author: { select: { name: true, avatar: true, role: true } }, _count: { select: { comments: true } } },
      orderBy: { createdAt: 'desc' },
    });
    res.json(topics);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar tópicos do BD' });
  }
});

export default router;
