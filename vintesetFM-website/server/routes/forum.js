import express from 'express';
import { PrismaClient } from '@prisma/client';

const router = express.Router();
const prisma = new PrismaClient();
import { requireAuth } from '../middleware/roles.js';

// Listar Tópicos
router.get('/', async (req, res) => {
  try {
    const topics = await prisma.topic.findMany({
      include: { author: { select: { nickname: true, avatar: true, roles: true } }, _count: { select: { comments: true } } },
      orderBy: { createdAt: 'desc' },
    });
    res.json(topics);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar tópicos do BD' });
  }
});

// Criar Tópico
router.post('/', requireAuth, async (req, res) => {
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

// Buscar Tópico Único
router.get('/:id', async (req, res) => {
  try {
    const topic = await prisma.topic.findUnique({
      where: { id: req.params.id },
      include: { 
        author: { select: { nickname: true, avatar: true, roles: true } },
        comments: { 
          include: { author: { select: { nickname: true, avatar: true } } },
          orderBy: { createdAt: 'asc' }
        }
      }
    });
    if (!topic) return res.status(404).json({ error: 'Tópico não encontrado.' });
    res.json(topic);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar dados do tópico.' });
  }
});

// Excluir Tópico (Admins/Owners)
router.delete('/:id', requireAuth, async (req, res) => {
  try {
    const topic = await prisma.topic.findUnique({ where: { id: req.params.id } });
    if (!topic) return res.status(404).json({ error: 'Tópico não encontrado.' });
    
    // Verificar Permissão: É o autor OU é Admin/Owner
    const isOwnerOrAdmin = req.user.roles?.includes('OWNER') || req.user.roles?.includes('ADMIN') || req.user.roles?.includes('ADMIN_DOWNLOADS');
    const isAuthor = topic.authorId === req.user.id;
    
    if (!isOwnerOrAdmin && !isAuthor) {
      return res.status(403).json({ error: 'Sem permissão para excluir este tópico.' });
    }

    // Excluir
    await prisma.topic.delete({ where: { id: req.params.id } });
    res.json({ success: true, message: 'Tópico excluído' });
  } catch (error) {
    console.error('[ERRO] Falha ao excluir tópico:', error);
    res.status(500).json({ error: 'Erro interno ao excluir tópico.' });
  }
});

export default router;
