import express from 'express';
import { PrismaClient } from '@prisma/client';
import { requirePermission } from '../middleware/roles.js';
import { getAllRoles } from '../config/permissions.js';
import { ensureCreatorForUser, deactivateCreatorsForUser } from '../services/creatorContext.js';

const router = express.Router();
const prisma = new PrismaClient();

function parseRolesJson(roles) {
  if (Array.isArray(roles)) return roles;
  if (typeof roles === 'string') { try { return JSON.parse(roles); } catch { return [roles]; } }
  return [];
}

/**
 * GET /api/admin/users
 * Lista todos os usuários com suas roles.
 * Requer permissão: admin:view_users (ADMIN+)
 */
router.get('/users', requirePermission('admin:view_users'), async (req, res) => {
  try {
    const isOwner = req.user.roles.includes('OWNER');

    // Busca no servidor (escala p/ milhares de usuários): filtra no banco e
    // limita o retorno. Sem busca, devolve os mais recentes.
    const q = (req.query.q || '').toString().trim();
    const where = q
      ? { OR: [
          { nickname: { contains: q } },
          { name: { contains: q } },
          { email: { contains: q } },
        ] }
      : {};

    const users = await prisma.user.findMany({
      where,
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
      take: q ? 50 : 30,
    });

    const maskEmailPartial = (email) => {
      if (!email) return null;
      const parts = email.split('@');
      if (parts.length !== 2) return email;
      const [name, domain] = parts;
      if (name.length <= 2) return `***@${domain}`;
      return `${name[0]}***${name[name.length - 1]}@${domain}`;
    };

    const secureUsers = users.map(u => {
      let displayEmail = '[Protegido]';
      if (isOwner) {
        displayEmail = maskEmailPartial(u.email);
      } else if (u.id === req.user.id) {
        displayEmail = u.email; // O admin pode ver o seu próprio email intacto
      }
      return { ...u, email: displayEmail };
    });

    res.json({ users: secureUsers });
  } catch (error) {
    console.error('[Admin] Erro ao listar usuários:', error);
    res.status(500).json({ error: 'Erro interno ao buscar usuários.' });
  }
});

/**
 * GET /api/admin/users/stats
 * Contadores por cargo + total (1 query leve, só o campo roles).
 * Alimenta os cards do painel sem baixar a base inteira.
 */
router.get('/users/stats', requirePermission('admin:view_users'), async (req, res) => {
  try {
    const all = await prisma.user.findMany({ select: { roles: true } });
    const counts = {};
    for (const u of all) {
      let r = u.roles;
      if (typeof r === 'string') { try { r = JSON.parse(r); } catch { r = []; } }
      if (Array.isArray(r)) for (const role of r) counts[role] = (counts[role] || 0) + 1;
    }
    res.json({ total: all.length, counts });
  } catch (error) {
    console.error('[Admin] Erro ao calcular stats:', error);
    res.status(500).json({ error: 'Erro ao calcular estatísticas.' });
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
    // Estado anterior (p/ reconciliar o cargo CREATOR com o Rei da Mesa).
    const before = await prisma.user.findUnique({
      where: { id },
      select: { id: true, nickname: true, name: true, roles: true }
    });

    const updatedUser = await prisma.user.update({
      where: { id },
      data: { roles },
      select: {
        id: true,
        name: true,
        nickname: true,
        email: true,
        avatar: true,
        roles: true,
      },
    });

    // Reconcilia o Rei da Mesa do usuário com o cargo CREATOR (Fase 3e):
    // ganhou CREATOR -> cria o Rei da Mesa dele (INATIVO, ativa ao preencher perfil);
    // perdeu CREATOR (e não é OWNER) -> desativa os dele (preserva dados).
    try {
      const hadCreator = parseRolesJson(before?.roles).includes('CREATOR');
      const hasCreator = roles.includes('CREATOR');
      if (hasCreator && !hadCreator) {
        await ensureCreatorForUser(prisma, { id, nickname: before?.nickname, name: before?.name });
      } else if (!hasCreator && hadCreator && !roles.includes('OWNER')) {
        await deactivateCreatorsForUser(prisma, id);
      }
    } catch (e) {
      console.error('[Admin] Falha ao reconciliar Creator com cargo CREATOR:', e);
    }

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
