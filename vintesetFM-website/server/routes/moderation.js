import express from 'express';
import { PrismaClient } from '@prisma/client';
import { requireRoles } from '../middleware/roles.js';

const router = express.Router();
const prisma = new PrismaClient();

// Só a Elite pode acessar essas rotas (Bouncer do Moderador pra cima)
router.use(requireRoles(['OWNER', 'ADMIN', 'MODERATOR']));

// GET Fila de moderação
router.get('/pending', async (req, res) => {
  try {
    const topics = await prisma.topic.findMany({
      where: { status: 'PENDING' },
      include: { author: { select: { nickname: true, avatar: true, roles: true } } },
      orderBy: { createdAt: 'desc' },
    });
    res.json(topics);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar fila de moderação.' });
  }
});

// GET Fila de Rejeitados
router.get('/rejected', async (req, res) => {
  try {
    const topics = await prisma.topic.findMany({
      where: { status: 'REJECTED' },
      include: { author: { select: { nickname: true, avatar: true, roles: true } } },
      orderBy: { createdAt: 'desc' },
    });
    res.json(topics);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar tópicos rejeitados.' });
  }
});

// POST Aprovar
router.post('/:id/approve', async (req, res) => {
  try {
     const topicId = req.params.id;
     await prisma.topic.update({
       where: { id: topicId },
       data: { 
         status: 'APPROVED', 
         moderationReason: `Aprovado por Moderação Manual via ${req.user.nickname}` 
       }
     });
     res.json({ success: true, message: 'Tópico aprovado com sucesso!' });
  } catch (error) {
     res.status(500).json({ error: 'Erro ao lançar aprovação de tópico.' });
  }
});

// POST Rejeitar
router.post('/:id/reject', async (req, res) => {
  try {
     const topicId = req.params.id;
     const { reason } = req.body;
     await prisma.topic.update({
       where: { id: topicId },
       data: { 
         status: 'REJECTED', 
         moderationReason: reason || `Rejeitado Manualmente pela Equipe (${req.user.nickname})` 
       }
     });
     res.json({ success: true, message: 'Tópico atirado do penhasco com êxito.' });
  } catch (error) {
     res.status(500).json({ error: 'Erro dramático ao rejeitar tópico.' });
  }
});

export default router;
