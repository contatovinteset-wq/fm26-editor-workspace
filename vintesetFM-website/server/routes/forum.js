import express from 'express';
import { PrismaClient } from '@prisma/client';
import jwt from 'jsonwebtoken';
import { requireAuth } from '../middleware/roles.js';
import { judgeTopic } from '../services/aiModerator.js';

const router = express.Router();
const prisma = new PrismaClient();

// Helper JWT passivo para saber quem é o leitor sem barrá-lo
function getOptionalUser(req) {
  const token = req.cookies?.jwt;
  if (!token) return null;
  try {
     const decoded = jwt.verify(token, process.env.JWT_SECRET || 'fallback_secret');
     return decoded; // Retorna { id, roles: [...] }
  } catch(e) { return null; }
}

// Listar Tópicos (Apenas APPROVED, a menos que o Autor seja o Leitor ou o Leitor seja Admin/Staff)
router.get('/', async (req, res) => {
  try {
    const user = getOptionalUser(req);
    const userId = user?.id;
    let whereClause = { status: 'APPROVED' };

    // Se estiver logado, ele vê os próprios tópicos PENDING/REJECTED tbm
    if (userId) {
       whereClause = {
         OR: [
           { status: 'APPROVED' },
           { authorId: userId }
         ]
       };
    }

    // Nota: Mods/Admins verão a fila de pendentes em outra Rota (/api/moderation)
    
    const topics = await prisma.topic.findMany({
      where: whereClause,
      include: { author: { select: { nickname: true, avatar: true, roles: true } }, _count: { select: { comments: true, likes: true } } },
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
    
    const fullContent = externalLink ? `${content}\n\nLink: ${externalLink}` : content;

    let initialStatus = 'PENDING';
    let modReason = 'Aguardando validação da Vinteset AI.';

    // Bypasses pra chefia (Role Bypass Protection)
    const userRoles = req.user.roles || [];
    const isVIP = userRoles.includes('OWNER') || userRoles.includes('ADMIN') || userRoles.includes('MODERATOR');

    if (isVIP) {
       initialStatus = 'APPROVED';
       modReason = 'Equipe Oficial - Bypass Direto';
    }

    const newTopic = await prisma.topic.create({
      data: {
        title,
        content: fullContent,
        category,
        status: initialStatus,
        moderationReason: modReason,
        authorId: req.user.id
      }
    });

    if (!isVIP) {
      // Auto-Moderador age em background (assíncrono)! Retira o delay da resposta da web
      judgeTopic(title, fullContent).then(async (aiDecision) => {
         try {
            await prisma.topic.update({
               where: { id: newTopic.id },
               data: {
                  status: aiDecision.status,
                  moderationReason: aiDecision.reason
               }
            });
         } catch(e) {
            console.error('[AI_MODERATOR] Erro atualizando banco no background:', e);
         }
      }).catch(err => {
         console.error('[AI_MODERATOR] Erro na execução de background da moderação:', err);
      });
    }

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
        _count: { select: { likes: true } },
        comments: { 
          include: { author: { select: { nickname: true, avatar: true } } },
          orderBy: { createdAt: 'asc' }
        }
      }
    });
    if (!topic) return res.status(404).json({ error: 'Tópico não encontrado.' });

    // Proteção Anti-Abuso e Vazamento de Links Diretos para Threads Pendentes/Rejeitadas
    if (topic.status !== 'APPROVED') {
       const user = getOptionalUser(req);
       const isAuthor = user && user.id === topic.authorId;
       const isStaff = user && user.roles && (user.roles.includes('OWNER') || user.roles.includes('ADMIN') || user.roles.includes('MODERATOR') || user.roles.includes('ADMIN_DOWNLOADS'));
       
       if (!isAuthor && !isStaff) {
          // Enganar atacantes devolvendo 404 seria ideal, mas pra UX melhor dar 403 pra não bugar a cabeça do cara
          return res.status(403).json({ error: 'Acesso Negado. Este tópico encontra-se retido na moderação ou foi reprovado.' });
       }
    }

    // Se tiver acesso, incrementamos as views de forma assíncrona/lazy na base
    await prisma.topic.update({
       where: { id: req.params.id },
       data: { views: { increment: 1 } }
    });
    
    // Atualiza obj em memória pro front
    topic.views = (topic.views || 0) + 1;

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

// Curtir / Descurtir Tópico
router.post('/:id/like', requireAuth, async (req, res) => {
  try {
    const topicId = req.params.id;
    const userId = req.user.id;

    const topic = await prisma.topic.findUnique({ where: { id: topicId } });
    if (!topic) return res.status(404).json({ error: 'Tópico não encontrado.' });

    const existingLike = await prisma.topicLike.findUnique({
      where: { topicId_userId: { topicId, userId } }
    });

    if (existingLike) {
      await prisma.topicLike.delete({ where: { id: existingLike.id } });
      const currentCount = await prisma.topicLike.count({ where: { topicId } });
      return res.json({ liked: false, likesCount: currentCount });
    } else {
      await prisma.topicLike.create({ data: { topicId, userId } });
      const currentCount = await prisma.topicLike.count({ where: { topicId } });
      return res.json({ liked: true, likesCount: currentCount });
    }
  } catch (error) {
    console.error('[ERRO] Falha ao curtir/descurtir:', error);
    res.status(500).json({ error: 'Erro interno na curtida.' });
  }
});

// Criar Comentário
router.post('/:id/comment', requireAuth, async (req, res) => {
  try {
    const topicId = req.params.id;
    const { content } = req.body;
    const authorId = req.user.id;

    if (!content || !content.trim()) {
      return res.status(400).json({ error: 'O comentário não pode estar vazio.' });
    }

    const topic = await prisma.topic.findUnique({ where: { id: topicId } });
    if (!topic) return res.status(404).json({ error: 'Tópico não encontrado.' });

    const comment = await prisma.comment.create({
      data: {
        content,
        topicId,
        authorId
      },
      include: {
        author: { select: { nickname: true, avatar: true } }
      }
    });

    res.status(201).json(comment);
  } catch (error) {
    console.error('[ERRO] Falha ao criar comentário:', error);
    res.status(500).json({ error: 'Erro interno ao publicar comentário.' });
  }
});

// Excluir Comentário
router.delete('/comment/:commentId', requireAuth, async (req, res) => {
  try {
    const { commentId } = req.params;
    const userId = req.user.id;
    const userRoles = req.user.roles || [];

    const comment = await prisma.comment.findUnique({ where: { id: commentId } });
    if (!comment) return res.status(404).json({ error: 'Comentário não encontrado.' });

    const isOwnerOrAdmin = userRoles.includes('OWNER') || userRoles.includes('ADMIN') || userRoles.includes('ADMIN_DOWNLOADS');
    const isAuthor = comment.authorId === userId;

    if (!isOwnerOrAdmin && !isAuthor) {
      return res.status(403).json({ error: 'Sem permissão para excluir.' });
    }

    await prisma.comment.delete({ where: { id: commentId } });
    res.json({ message: 'Comentário excluído com sucesso.' });
  } catch (error) {
    console.error('[ERRO] Falha ao excluir comentário:', error);
    res.status(500).json({ error: 'Erro interno ao excluir comentário.' });
  }
});

export default router;
