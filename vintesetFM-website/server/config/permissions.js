/**
 * ===============================================
 * RBAC - Roles, Hierarquia e Permission Sets
 * ===============================================
 *
 * Arquivo centralizado que define toda a lógica de acesso do sistema VintesetFM.
 * Usado tanto pelo backend (middleware) quanto espelhado no frontend.
 */

// ── Definição de Roles ──────────────────────────
export const ROLES = {
  OWNER: 'OWNER',
  ADMIN: 'ADMIN',
  MODERATOR: 'MODERATOR',
  USER: 'USER',
};

// ── Hierarquia: maior nível = mais poder ────────
export const ROLE_HIERARCHY = {
  [ROLES.OWNER]: 100,
  [ROLES.ADMIN]: 50,
  [ROLES.MODERATOR]: 30,
  [ROLES.USER]: 10,
};

// ── Permission Sets por área do site ────────────
// Valor = nível mínimo de role necessário
export const PERMISSIONS = {
  // ─ Downloads / Mods ─
  'downloads:view': 10,       // USER
  'downloads:create': 50,     // ADMIN
  'downloads:edit': 50,       // ADMIN
  'downloads:delete': 50,     // ADMIN

  // ─ Telecurso ─
  'telecurso:view': 10,       // USER (público)
  'telecurso:manage': 50,     // ADMIN

  // ─ Fórum ─
  'forum:view': 10,           // USER
  'forum:create': 10,         // USER
  'forum:edit_own': 10,       // USER
  'forum:edit_any': 30,       // MODERATOR
  'forum:delete_any': 30,     // MODERATOR
  'forum:pin': 30,            // MODERATOR

  // ─ Rei da Mesa ─
  'reidamesa:play': 10,       // USER
  'reidamesa:manage': 50,     // ADMIN (gerenciar rodadas, jogadores)

  // ─ Admin ─
  'admin:view_panel': 50,     // ADMIN
  'admin:view_users': 50,     // ADMIN
  'admin:manage_roles': 100,  // OWNER only
};

// ── Funções utilitárias ─────────────────────────

/**
 * Retorna o maior nível de poder dentre as roles do usuário.
 */
export function getHighestLevel(roles) {
  if (!Array.isArray(roles) || roles.length === 0) return 0;
  return Math.max(0, ...roles.map((r) => ROLE_HIERARCHY[r] || 0));
}

/**
 * Checa se o usuário possui a permissão especificada,
 * baseado na hierarquia numérica das suas roles.
 */
export function hasPermission(userRoles, permission) {
  const requiredLevel = PERMISSIONS[permission];
  if (requiredLevel === undefined) return false;

  const userLevel = getHighestLevel(userRoles);
  return userLevel >= requiredLevel;
}

/**
 * Checa se o usuário possui pelo menos uma das roles listadas.
 */
export function hasRole(userRoles, requiredRoles) {
  if (!Array.isArray(userRoles)) return false;
  // OWNER bypass
  if (userRoles.includes(ROLES.OWNER)) return true;
  return requiredRoles.some((role) => userRoles.includes(role));
}

/**
 * Retorna todas as roles disponíveis ordenadas por nível (decrescente).
 */
export function getAllRoles() {
  return Object.entries(ROLE_HIERARCHY)
    .sort((a, b) => b[1] - a[1])
    .map(([name, level]) => ({ name, level }));
}

/**
 * Retorna todas as permissões agrupadas por seção.
 */
export function getPermissionsBySection() {
  const sections = {};
  for (const [key, level] of Object.entries(PERMISSIONS)) {
    const [section] = key.split(':');
    if (!sections[section]) sections[section] = [];
    sections[section].push({ permission: key, level });
  }
  return sections;
}
