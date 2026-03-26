import jwt from 'jsonwebtoken';
import { hasPermission, hasRole as checkRole } from '../config/permissions.js';
import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

/**
 * Helper: extrai e parseia as roles do token/banco.
 */
function extractRoles(decoded) {
  let roles = decoded.roles || [];
  if (typeof roles === 'string') {
    try { roles = JSON.parse(roles); } catch { roles = [roles]; }
  }
  return roles;
}

/**
 * Helper: decodifica o JWT apenas para extrair o ID assinado.
 */
function decodeToken(req) {
  const token = req.cookies?.jwt;
  if (!token) return null;

  const secret = process.env.JWT_SECRET || 'fallback_secret';
  const decoded = jwt.verify(token, secret);
  return decoded;
}

/**
 * Super Helper: Busca o usuário real do banco para evitar 
 * que um Token vencido ou cacheado impeça o acesso caso as Roles tenham mudado!
 */
async function getFreshUser(req) {
  const decoded = decodeToken(req);
  if (!decoded) return null;

  const freshUser = await prisma.user.findUnique({ where: { id: decoded.id } });
  if (!freshUser) return null;

  decoded.roles = extractRoles({ roles: freshUser.roles });
  decoded.nickname = freshUser.nickname;
  return decoded;
}

/**
 * Middleware que exige que o usuário possua pelo menos uma das roles listadas.
 * OWNER sempre tem passe livre (bypass automático via hasRole).
 */
export const requireRoles = (requiredRoles = []) => {
  return async (req, res, next) => {
    try {
      const decoded = await getFreshUser(req);
      if (!decoded) return res.status(401).json({ error: 'Não autorizado. Faça login.' });

      if (requiredRoles.length > 0 && !checkRole(decoded.roles, requiredRoles)) {
        return res.status(403).json({ error: 'Acesso Negado. Você não tem o cargo necessário.' });
      }

      req.user = decoded;
      next();
    } catch (err) {
      return res.status(401).json({ error: 'Sessão inválida ou expirada.' });
    }
  };
};

/**
 * Middleware que exige uma permissão granular baseada na hierarquia.
 * Consulta o PERMISSIONS map atualizado sempre do Banco de Dados.
 */
export const requirePermission = (permission) => {
  return async (req, res, next) => {
    try {
      const decoded = await getFreshUser(req);
      if (!decoded) return res.status(401).json({ error: 'Não autorizado. Faça login.' });

      if (!hasPermission(decoded.roles, permission)) {
        return res.status(403).json({ error: `Acesso Negado. Permissão necessária: ${permission}` });
      }

      req.user = decoded;
      next();
    } catch (err) {
      return res.status(401).json({ error: 'Sessão inválida ou expirada.' });
    }
  };
};

/**
 * Middleware simples que só exige autenticação viva no Banco.
 */
export const requireAuth = async (req, res, next) => {
  try {
    const decoded = await getFreshUser(req);
    if (!decoded) return res.status(401).json({ error: 'Não autorizado. Faça login.' });
    req.user = decoded;
    next();
  } catch (err) {
    return res.status(401).json({ error: 'Sessão inválida ou expirada.' });
  }
};
