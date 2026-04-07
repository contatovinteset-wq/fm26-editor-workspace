import express from 'express';
import { PrismaClient } from '@prisma/client';
import { requireAuth } from '../middleware/roles.js';
import { judgeTopic } from '../services/aiModerator.js';
import multer from 'multer';

const router = express.Router();
const prisma = new PrismaClient();

// Mantém a imagem em memória (não salva no disco da VPS)
const upload = multer({ 
  storage: multer.memoryStorage(),
  limits: { fileSize: 5 * 1024 * 1024 }, // ImgBB tbm permite 32MB, mas 5MB evita lag no servidor
  fileFilter: (req, file, cb) => {
    if (file.mimetype.startsWith('image/')) cb(null, true);
    else cb(new Error('Apenas imagens são permitidas.'));
  }
});

// Helper JWT passivo
function getOptionalUser(req) {
  const token = req.cookies?.jwt;
  if (!token) return null;
  try {
     const decoded = jwt.verify(token, process.env.JWT_SECRET || 'fallback_secret');
     return decoded;
  } catch(e) { return null; }
}

function checkModeratorPermission(user) {
  if (!user || !user.roles) return false;
  return user.roles.includes('OWNER') || user.roles.includes('ADMIN') || user.roles.includes('MODERATOR');
}

function checkAdminPermission(user) {
  if (!user || !user.roles) return false;
  return user.roles.includes('OWNER') || user.roles.includes('ADMIN');
}

// Rota de Upload - Agora atua como Proxy para o ImgBB
router.post('/upload', requireAuth, upload.single('image'), async (req, res) => {
  if (!req.file) return res.status(400).json({ error: 'Nenhuma imagem recebida.' });
  
  const API_KEY = process.env.IMGBB_API_KEY;
  if (!API_KEY) {
    return res.status(500).json({ error: 'Falta configurar IMGBB_API_KEY no servidor.' });
  }

  try {
    const formData = new FormData();
    formData.append('key', API_KEY);
    // ImgBB aceita base64 diretamente
    formData.append('image', req.file.buffer.toString('base64'));

    const imgbbRes = await fetch('https://api.imgbb.com/1/upload', {
      method: 'POST',
      body: formData
    });
    
    const data = await imgbbRes.json();
    
    if (data.success) {
      res.json({ url: data.data.url });
    } else {
      res.status(500).json({ error: 'A API do ImgBB rejeitou a imagem.' });
    }
  } catch(e) {
    console.error('Erro proxy imgbb:', e);
    res.status(500).json({ error: 'Erro de comunicação com ImgBB.' });
  }
});

// 1. Listar Categorias
router.get('/categories', async (req, res) => {
  try {
    const categories = await prisma.forumCategory.findMany({
      include: {
        _count: { select: { topics: true } }
      }
    });

    // Anexar informações do ultimo post para cada categoria
    const categoryData = await Promise.all(categories.map(async (cat) => {
      const latestTopic = await prisma.forumTopic.findFirst({
        where: { categoryId: cat.id },
        orderBy: { updatedAt: 'desc' },
        include: { author: { select: { nickname: true, avatar: true } } }
      });
      return { ...cat, latestTopic };
    }));

    res.json(categoryData);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar categorias' });
  }
});

// 2. Listar Tópicos de uma Categoria Específica
router.get('/categories/:slug', async (req, res) => {
  try {
    const category = await prisma.forumCategory.findUnique({
      where: { slug: req.params.slug },
      include: {
        topics: {
          include: {
            author: { select: { nickname: true, avatar: true, roles: true } },
            _count: { select: { posts: true } }
          },
          where: {
            status: 'APPROVED'
          },
          orderBy: [
            { isPinned: 'desc' },
            { updatedAt: 'desc' }
          ]
        }
      }
    });

    if (!category) return res.status(404).json({ error: 'Categoria não encontrada' });
    res.json(category);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao buscar tópicos' });
  }
});

// 3. Criar Novo Tópico
router.post('/topics', requireAuth, async (req, res) => {
  try {
    const { title, content, categoryId } = req.body;

    if (!title || !content || !categoryId) {
      return res.status(400).json({ error: 'Título, conteúdo e categoria são obrigatórios.' });
    }

    const aiDecision = await judgeTopic(title, content);
    const initialStatus = checkModeratorPermission(req.user) ? 'APPROVED' : aiDecision.status;
    const modReason = checkModeratorPermission(req.user) ? 'Equipe Oficial' : aiDecision.reason;

    const topic = await prisma.forumTopic.create({
      data: {
        title,
        content,
        categoryId,
        status: initialStatus,
        moderationReason: modReason,
        authorId: req.user.id
      }
    });
    res.status(201).json({ ...topic, warning: initialStatus !== 'APPROVED' ? modReason : null });
  } catch (error) {
    console.error('Erro ao criar tópico:', error);
    res.status(500).json({ error: 'Falha ao criar tópico.' });
  }
});

// 4. Buscar Detalhes de um Tópico (Ler)
router.get('/topics/:id', async (req, res) => {
  try {
    const topic = await prisma.forumTopic.findUnique({
      where: { id: req.params.id },
      include: {
        category: true,
        author: { select: { id: true, nickname: true, avatar: true, roles: true } },
        posts: {
          where: { status: 'APPROVED' },
          include: {
            author: { select: { id: true, nickname: true, avatar: true, roles: true } }
          },
          orderBy: { createdAt: 'asc' }
        }
      }
    });

    if (!topic) return res.status(404).json({ error: 'Tópico não encontrado' });

    const isAuthor = topic.authorId === getOptionalUser(req)?.id;
    const isStaff = checkModeratorPermission(getOptionalUser(req));

    if (topic.status !== 'APPROVED' && !isAuthor && !isStaff) {
       return res.status(403).json({ error: 'Acesso Negado. Tópico reprovado ou em análise.' });
    }

    // Incrementa Views
    await prisma.forumTopic.update({
      where: { id: topic.id },
      data: { views: { increment: 1 } }
    });
    topic.views += 1;

    res.json(topic);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao carregar tópico.' });
  }
});

// 5. Editar Tópico
router.put('/topics/:id', requireAuth, async (req, res) => {
  try {
    const topicId = req.params.id;
    const { title, content } = req.body;

    const topic = await prisma.forumTopic.findUnique({ where: { id: topicId } });
    if (!topic) return res.status(404).json({ error: 'Tópico não encontrado.' });

    const isAuthor = topic.authorId === req.user.id;
    const isAdmin = checkAdminPermission(req.user);

    if (!isAuthor && !isAdmin) return res.status(403).json({ error: 'Sem permissão.' });

    const updated = await prisma.forumTopic.update({
      where: { id: topicId },
      data: { title, content }
    });

    res.json(updated);
  } catch (error) {
    res.status(500).json({ error: 'Erro ao atualizar tópico.' });
  }
});

// 6. Deletar Tópico
router.delete('/topics/:id', requireAuth, async (req, res) => {
  try {
    const topicId = req.params.id;
    const topic = await prisma.forumTopic.findUnique({ where: { id: topicId } });
    if (!topic) return res.status(404).json({ error: 'Tópico não encontrado.' });

    const isAuthor = topic.authorId === req.user.id;
    const isAdmin = checkAdminPermission(req.user);

    if (!isAuthor && !isAdmin) return res.status(403).json({ error: 'Sem permissão.' });

    await prisma.forumTopic.delete({ where: { id: topicId } });
    res.json({ success: true, message: 'Tópico removido.' });
  } catch (error) {
    res.status(500).json({ error: 'Erro ao deletar tópico.' });
  }
});

// 7. Moderação: Fixar/Desfixar Tópico
router.post('/topics/:id/pin', requireAuth, async (req, res) => {
  try {
    if (!checkAdminPermission(req.user)) return res.status(403).json({ error: 'Sem permissão.' });
    
    const topicId = req.params.id;
    const topic = await prisma.forumTopic.findUnique({ where: { id: topicId } });
    if (!topic) return res.status(404).json({ error: 'Tópico não encontrado' });

    const updated = await prisma.forumTopic.update({
      where: { id: topicId },
      data: { isPinned: !topic.isPinned }
    });

    res.json({ success: true, isPinned: updated.isPinned });
  } catch (error) {
    res.status(500).json({ error: 'Falha ao fixar tópico.' });
  }
});

// 8. Moderação: Fechar/Abrir Tópico
router.post('/topics/:id/close', requireAuth, async (req, res) => {
  try {
    if (!checkModeratorPermission(req.user)) return res.status(403).json({ error: 'Sem permissão.' });
    
    const topicId = req.params.id;
    const topic = await prisma.forumTopic.findUnique({ where: { id: topicId } });
    if (!topic) return res.status(404).json({ error: 'Tópico não encontrado' });

    const updated = await prisma.forumTopic.update({
      where: { id: topicId },
      data: { isClosed: !topic.isClosed }
    });

    res.json({ success: true, isClosed: updated.isClosed });
  } catch (error) {
    res.status(500).json({ error: 'Falha ao fechar tópico.' });
  }
});

// 9. Responder Tópico (Criar Post)
router.post('/topics/:id/posts', requireAuth, async (req, res) => {
  try {
    const topicId = req.params.id;
    const { content } = req.body;

    if (!content) return res.status(400).json({ error: 'Conteúdo vazio.' });

    const topic = await prisma.forumTopic.findUnique({ where: { id: topicId } });
    if (!topic) return res.status(404).json({ error: 'Tópico não encontrado.' });
    
    if (topic.isClosed && !checkModeratorPermission(req.user)) {
      return res.status(403).json({ error: 'Este tópico está trancado para novas respostas.' });
    }

    const aiDecision = await judgeTopic('Resposta do Fórum', content);
    let initialStatus = checkModeratorPermission(req.user) ? 'APPROVED' : aiDecision.status;
    let modReason = checkModeratorPermission(req.user) ? 'Equipe Oficial' : aiDecision.reason;

    const post = await prisma.forumPost.create({
      data: {
        content,
        topicId,
        status: initialStatus,
        moderationReason: modReason,
        authorId: req.user.id
      },
      include: {
        author: { select: { nickname: true, avatar: true, roles: true } }
      }
    });

    // Atualiza o updatedAt do tópico ("bump") apenas se for aprovado (para não hypar tópico pendente)
    if (initialStatus === 'APPROVED') {
      await prisma.forumTopic.update({
        where: { id: topicId },
        data: { updatedAt: new Date() }
      });
    }

    res.status(201).json({ ...post, warning: initialStatus !== 'APPROVED' ? modReason : null });
  } catch (error) {
    res.status(500).json({ error: 'Erro ao publicar resposta.' });
  }
});

// 10. Deletar Post (Resposta)
router.delete('/posts/:postId', requireAuth, async (req, res) => {
  try {
    const postId = req.params.postId;
    const post = await prisma.forumPost.findUnique({ where: { id: postId } });
    if (!post) return res.status(404).json({ error: 'Post não encontrado.' });

    const isAuthor = post.authorId === req.user.id;
    const isAdmin = checkAdminPermission(req.user);

    if (!isAuthor && !isAdmin) return res.status(403).json({ error: 'Sem permissão.' });

    await prisma.forumPost.delete({ where: { id: postId } });
    res.json({ success: true, message: 'Post removido.' });
  } catch(error) {
    res.status(500).json({ error: 'Erro ao remover resposta.' });
  }
});

export default router;
