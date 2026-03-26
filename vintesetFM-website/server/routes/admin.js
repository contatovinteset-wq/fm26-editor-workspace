import express from 'express';
import { PrismaClient } from '@prisma/client';
import { requirePermission } from '../middleware/roles.js';
import { getAllRoles } from '../config/permissions.js';

const router = express.Router();
const prisma = new PrismaClient();

/**
 * GET /api/admin/users
 * Lista todos os usuários com suas roles.
 * Requer permissão: admin:view_users (ADMIN+)
 */
router.get('/users', requirePermission('admin:view_users'), async (req, res) => {
  try {
    const users = await prisma.user.findMany({
      select: {
        id: true,
        name: true,
        nickname: true,
        email: true,
        avatar: true,
        roles: true,
        createdAt: true,
      },
      orderBy: { createdAt: 'desc' },
    });
    res.json({ users });
  } catch (error) {
    console.error('[Admin] Erro ao listar usuários:', error);
    res.status(500).json({ error: 'Erro interno ao buscar usuários.' });
  }
});

/**
 * GET /api/admin/roles
 * Retorna todas as roles disponíveis no sistema.
 * Requer permissão: admin:view_panel (ADMIN+)
 */
router.get('/roles', requirePermission('admin:view_panel'), (req, res) => {
  res.json({ roles: getAllRoles() });
});

/**
 * PATCH /api/admin/users/:id/roles
 * Atualiza as roles de um usuário.
 * Requer permissão: admin:manage_roles (OWNER only)
 *
 * Body: { roles: ["USER", "MODERATOR"] }
 */
router.patch('/users/:id/roles', requirePermission('admin:manage_roles'), async (req, res) => {
  const { id } = req.params;
  const { roles } = req.body;

  if (!Array.isArray(roles) || roles.length === 0) {
    return res.status(400).json({ error: 'O campo "roles" deve ser um array não vazio.' });
  }

  // Impedir que o OWNER remova o próprio cargo de OWNER
  if (id === req.user.id && !roles.includes('OWNER')) {
    return res.status(400).json({ error: 'Você não pode remover seu próprio cargo de OWNER.' });
  }

  try {
    const updatedUser = await prisma.user.update({
      where: { id },
      data: { roles },
      select: {
        id: true,
        name: true,
        email: true,
        avatar: true,
        roles: true,
      },
    });
    res.json({ user: updatedUser, message: 'Roles atualizadas com sucesso.' });
  } catch (error) {
    console.error('[Admin] Erro ao atualizar roles:', error);
    if (error.code === 'P2025') {
      return res.status(404).json({ error: 'Usuário não encontrado.' });
    }
    res.status(500).json({ error: 'Erro interno ao atualizar roles.' });
  }
});

export default router;
