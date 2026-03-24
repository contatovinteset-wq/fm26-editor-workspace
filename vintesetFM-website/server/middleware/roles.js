import jwt from 'jsonwebtoken';

/**
 * Middleware para checar se o usuário logado via JWT possui os papéis (roles) necessários.
 * Se o usuário for 'OWNER', sempre ganha passe livre.
 * 
 * @param {string[]} requiredRoles - Array de strings representando os cargos necessários.
 */
export const requireRoles = (requiredRoles = []) => {
  return (req, res, next) => {
    // Pegar o token do cookie (salvo no login)
    const token = req.cookies?.jwt;
    if (!token) return res.status(401).json({ error: 'Não autorizado. Faça login.' });

    try {
      const secret = process.env.JWT_SECRET || 'fallback_secret';
      const decoded = jwt.verify(token, secret); // { id, roles, name, avatar }

      // Se a array de roles for string por algum motivo, tentamos parsear (fallback extra)
      let userRoles = decoded.roles || [];
      if (typeof userRoles === 'string') {
         try { userRoles = JSON.parse(userRoles); } catch { userRoles = [userRoles]; }
      }

      // O OWNER sempre tem acesso a tudo
      if (userRoles.includes('OWNER')) {
        req.user = decoded; // Popula pra próxima handler
        return next();
      }

      // Se passou roles pra checar
      if (requiredRoles.length > 0) {
        // Verifica se qualquer um dos cargos requeridos existe na lista do user
        const hasRequiredRole = requiredRoles.some((role) => userRoles.includes(role));

        if (!hasRequiredRole) {
          return res.status(403).json({ error: 'Acesso Negado. Você não tem o cargo necessário.' });
        }
      }

      req.user = decoded;
      next();
    } catch (err) {
      return res.status(401).json({ error: 'Sessão inválida ou expirada.' });
    }
  };
};
