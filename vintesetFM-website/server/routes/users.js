import express from 'express';
import { PrismaClient } from '@prisma/client';
import { requireAuth } from '../middleware/roles.js';
import { can } from '../../src/utils/PermissionService.js';

const prisma = new PrismaClient();
const router = express.Router();

// ATUALIZAR PERFIL (Nickname e Avatar)
// Uso de requireAuth garante padronização de segurança em todo o sistema
router.patch('/profile', requireAuth, async (req, res) => {
  const { nickname, avatar } = req.body;
  const userId = req.user.id; // requireAuth popula req.user

  try {
    // Buscar usuário atual para verificar throttling
    const currentUser = await prisma.user.findUnique({ where: { id: userId } });
    if (!currentUser) return res.status(404).json({ error: 'Usuário não encontrado' });

    // Avaliar permissões usando os níveis atualizados do banco (evita JWT stale)
    const hasNicknamePermission = can(currentUser, 'change_nickname');

    // Validar nickname (apenas letras, números e underscores, 3-15 chars)
    if (nickname && nickname !== currentUser.nickname) {
      // Regra de Trava Irreversível de Nickname
      if (currentUser.nickname && !hasNicknamePermission) {
         return res.status(403).json({ 
           error: 'Segurança: Seu nickname já foi definido definitivamente e não pode ser alterado.' 
         });
      }

      const nickRegex = /^[a-zA-Z0-9_]{3,15}$/;
      if (!nickRegex.test(nickname)) {
        return res.status(400).json({ error: 'Nickname inválido. Use 3-15 caracteres (letras, números e underscores).' });
      }

      // Verificar se o nickname já existe
      const existingUser = await prisma.user.findUnique({
        where: { nickname }
      });

      if (existingUser && existingUser.id !== userId) {
        return res.status(400).json({ error: 'Este nickname já está em uso.' });
      }
    }

    const isDefiningNicknameFirstTime = !!nickname && nickname !== currentUser.nickname && !currentUser.nickname_defined;

    const updatedUser = await prisma.user.update({
      where: { id: userId },
      data: {
        ...(nickname && nickname !== currentUser.nickname && { 
          nickname, 
          nickname_defined: true,
          lastNicknameChange: new Date() 
        }),
        ...(avatar && { avatar })
      }
    });

    // Auditoria Obrigatória
    if (isDefiningNicknameFirstTime) {
      await prisma.auditLog.create({
        data: {
          userId: userId,
          action: 'SET_NICKNAME',
          details: { old: currentUser.nickname, new: nickname },
          ipAddress: req.ip || req.connection.remoteAddress
        }
      });
    }

    res.json({ success: true, user: updatedUser });
  } catch (error) {
    console.error('Erro ao atualizar perfil:', error);
    res.status(500).json({ error: 'Erro interno ao atualizar perfil.' });
  }
});

export default router;
