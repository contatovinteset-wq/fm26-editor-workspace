import jwt from 'jsonwebtoken';
import { hasPermission, hasRole as checkRole } from '../config/permissions.js';

/**
 * Helper: extrai e parseia as roles do token JWT.
 */
function extractRoles(decoded) {
  let roles = decoded.roles || [];
  if (typeof roles === 'string') {
    try { roles = JSON.parse(roles); } catch { roles = [roles]; }
  }
  return roles;
}

/**
 * Helper: decodifica o JWT do cookie e popula req.user.
 */
function decodeToken(req) {
  const token = req.cookies?.jwt;
  if (!token) return null;

  const secret = process.env.JWT_SECRET || 'fallback_secret';
  const decoded = jwt.verify(token, secret);
  decoded.roles = extractRoles(decoded);
  return decoded;
}

/**
 * Middleware que exige que o usuário possua pelo menos uma das roles listadas.
 * OWNER sempre tem passe livre (bypass automático via hasRole).
 *
 * Uso: requireRoles(['ADMIN', 'MODERATOR'])
 */
export const requireRoles = (requiredRoles = []) => {
  return (req, res, next) => {
    try {
      const decoded = decodeToken(req);
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
 * Consulta o PERMISSIONS map do permissions.js.
 *
 * Uso: requirePermission('downloads:create')
 */
export const requirePermission = (permission) => {
  return (req, res, next) => {
    try {
      const decoded = decodeToken(req);
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
 * Middleware simples que só exige autenticação (qualquer role).
 */
export const requireAuth = (req, res, next) => {
  try {
    const decoded = decodeToken(req);
    if (!decoded) return res.status(401).json({ error: 'Não autorizado. Faça login.' });
    req.user = decoded;
    next();
  } catch (err) {
    return res.status(401).json({ error: 'Sessão inválida ou expirada.' });
  }
};
