/**
 * ===============================================
 * RBAC - Espelho Frontend (Roles + Permissions)
 * ===============================================
 *
 * Mesmo conteúdo do server/config/permissions.js
 * para uso nos componentes React (RequireRole, AdminPanel, etc).
 */

export const ROLES = {
  OWNER: 'OWNER',
  ADMIN: 'ADMIN',
  MODERATOR: 'MODERATOR',
  USER: 'USER',
};

export const ROLE_HIERARCHY = {
  [ROLES.OWNER]: 100,
  [ROLES.ADMIN]: 50,
  [ROLES.MODERATOR]: 30,
  [ROLES.USER]: 10,
};

export const PERMISSIONS = {
  // Downloads / Mods
  'downloads:view': 10,
  'downloads:create': 50,
  'downloads:edit': 50,
  'downloads:delete': 50,

  // Telecurso
  'telecurso:view': 10,
  'telecurso:manage': 50,

  // Fórum
  'forum:view': 10,
  'forum:create': 10,
  'forum:edit_own': 10,
  'forum:edit_any': 30,
  'forum:delete_any': 30,
  'forum:pin': 30,

  // Rei da Mesa
  'reidamesa:play': 10,
  'reidamesa:manage': 50,

  // Admin
  'admin:view_panel': 50,
  'admin:view_users': 50,
  'admin:manage_roles': 100,
};

export function getHighestLevel(roles) {
  if (!Array.isArray(roles) || roles.length === 0) return 0;
  return Math.max(0, ...roles.map((r) => ROLE_HIERARCHY[r] || 0));
}

export function hasPermission(userRoles, permission) {
  const requiredLevel = PERMISSIONS[permission];
  if (requiredLevel === undefined) return false;
  return getHighestLevel(userRoles) >= requiredLevel;
}

export function hasRole(userRoles, requiredRoles) {
  if (!Array.isArray(userRoles)) return false;
  if (userRoles.includes(ROLES.OWNER)) return true;
  return requiredRoles.some((role) => userRoles.includes(role));
}

export function getAllRoles() {
  return Object.entries(ROLE_HIERARCHY)
    .sort((a, b) => b[1] - a[1])
    .map(([name, level]) => ({ name, level }));
}
