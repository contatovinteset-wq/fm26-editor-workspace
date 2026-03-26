import express from 'express';
import { PrismaClient } from '@prisma/client';

const router = express.Router();
const prisma = new PrismaClient();
import { isAuthenticated } from '../middleware/auth.js';

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

// Criar Tópico
router.post('/', isAuthenticated, async (req, res) => {
  try {
    const { title, content, category, externalLink } = req.body;
    
    // content = Descrição
    const fullContent = externalLink ? `${content}\n\nLink: ${externalLink}` : content;

    const newTopic = await prisma.topic.create({
      data: {
        title,
        content: fullContent,
        category,
        authorId: req.user.id
      }
    });
    res.status(201).json(newTopic);
  } catch (error) {
    console.error('[ERRO] Falha ao criar tópico:', error);
    res.status(500).json({ error: 'Erro de conexão com o banco de dados ao criar tópico.' });
  }
});

export default router;
