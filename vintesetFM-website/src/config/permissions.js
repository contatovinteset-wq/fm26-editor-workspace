/**
 * ================================================================
 * RBAC — FONTE ÚNICA DE VERDADE (isomórfico: frontend + backend)
 * ================================================================
 *
 * Modelo de GRANTS explícitos por role (não-linear): cada cargo recebe
 * um conjunto FECHADO de permissões, sem "vazar" poder entre áreas.
 * Admins especializados (ADMIN_DOWNLOADS, ADMIN_GERACAO) ficam restritos
 * à sua área. OWNER é curinga ('*').
 *
 * Consumido direto pelo frontend (../config/permissions) e, via shims finos,
 * por server/config/permissions.js e src/utils/PermissionService.js.
 */

export const ROLES = {
  OWNER: 'OWNER',
  ADMIN: 'ADMIN',
  ADMIN_DOWNLOADS: 'ADMIN_DOWNLOADS',
  ADMIN_GERACAO: 'ADMIN_GERACAO',
  MODERATOR: 'MODERATOR',
  USER: 'USER',
  GUEST: 'GUEST',
};

// Hierarquia numérica usada SOMENTE para "quem gerencia quem" (canManageTarget)
// e ordenação de exibição. NÃO decide permissões de feature (isso é via GRANTS).
export const ROLE_LEVELS = {
  [ROLES.OWNER]: 100,
  [ROLES.ADMIN]: 80,
  [ROLES.ADMIN_DOWNLOADS]: 60,
  [ROLES.ADMIN_GERACAO]: 60,
  [ROLES.MODERATOR]: 50,
  [ROLES.USER]: 10,
  [ROLES.GUEST]: 0,
};
// Alias retrocompatível (RoleBadge, RequireRole importam ROLE_HIERARCHY).
export const ROLE_HIERARCHY = ROLE_LEVELS;

// Permissões base de qualquer usuário logado.
const USER_GRANTS = [
  'downloads:view',
  'telecurso:view',
  'forum:view',
  'forum:create',
  'forum:edit_own',
  'reidamesa:play',
];

/**
 * GRANTS: role -> lista de permissões concedidas.
 * OWNER usa curinga '*' (tudo, inclusive admin:manage_roles).
 */
export const GRANTS = {
  [ROLES.OWNER]: ['*'],

  [ROLES.ADMIN]: [
    ...USER_GRANTS,
    // Fórum (moderação)
    'forum:edit_any', 'forum:delete_any', 'forum:pin',
    // Downloads
    'downloads:create', 'downloads:edit', 'downloads:delete',
    'approve_download', 'create_download_post',
    // Telecurso
    'telecurso:manage',
    // Rei da Mesa
    'reidamesa:manage',
    // Painel admin (usuários) — exceto gerir roles (só OWNER)
    'admin:view_panel', 'admin:view_users',
    // Moderação de usuário
    'change_nickname', 'ban_user', 'view_logs',
  ],

  [ROLES.MODERATOR]: [
    ...USER_GRANTS,
    'forum:edit_any', 'forum:delete_any', 'forum:pin',
  ],

  // Especializado: só Downloads.
  [ROLES.ADMIN_DOWNLOADS]: [
    ...USER_GRANTS,
    'downloads:create', 'downloads:edit', 'downloads:delete',
    'approve_download', 'create_download_post',
  ],

  // Especializado: só Rei da Mesa.
  [ROLES.ADMIN_GERACAO]: [
    ...USER_GRANTS,
    'reidamesa:manage',
  ],

  [ROLES.USER]: [...USER_GRANTS],
  [ROLES.GUEST]: [],
};

// Retrocompat: matriz permissão->roles derivada dos GRANTS
// (componentes antigos importavam PERMISSIONS_MAP do PermissionService).
export const PERMISSIONS_MAP = Object.freeze(
  Object.entries(GRANTS).reduce((acc, [role, perms]) => {
    if (perms.includes('*')) return acc;
    for (const p of perms) {
      acc[p] = acc[p] || {};
      acc[p][role] = true;
    }
    return acc;
  }, {})
);

// ── Helpers ──────────────────────────────────────────────────

/** Normaliza entrada (array de roles, objeto user, ou string JSON) -> array de roles. */
function resolveRoles(input) {
  if (!input) return [];
  let roles = Array.isArray(input) ? input : (input.roles ?? input);
  if (typeof roles === 'string') {
    try { roles = JSON.parse(roles); } catch { roles = [roles]; }
  }
  return Array.isArray(roles) ? roles : [];
}

/** Maior nível hierárquico entre as roles (p/ gerenciamento e ordenação). */
export function getUserMaxLevel(input) {
  const roles = resolveRoles(input);
  if (roles.length === 0) return ROLE_LEVELS[ROLES.GUEST];
  return Math.max(0, ...roles.map((r) => ROLE_LEVELS[r] || 0));
}
// Alias retrocompat (config antigo exportava getHighestLevel).
export const getHighestLevel = getUserMaxLevel;

/** Verdadeiro se as roles concedem a permissão (via GRANTS). OWNER = curinga. */
export function can(input, permission) {
  const roles = resolveRoles(input);
  if (roles.includes(ROLES.OWNER)) return true;
  return roles.some((r) => {
    const g = GRANTS[r] || [];
    return g.includes('*') || g.includes(permission);
  });
}
// hasPermission e can passam a ser o MESMO mecanismo (unificados).
export const hasPermission = (input, permission) => can(input, permission);

/** Verdadeiro se possui ao menos uma das roles exigidas (match direto). OWNER bypassa. */
export function hasRole(input, requiredRoles = []) {
  const roles = resolveRoles(input);
  if (roles.includes(ROLES.OWNER)) return true;
  return requiredRoles.some((role) => roles.includes(role));
}

/** Source gerencia Target (ban/promote) se tiver nível estritamente maior. OWNER manda. */
export function canManageTarget(sourceRoles, targetRoles) {
  const src = getUserMaxLevel(sourceRoles);
  const tgt = getUserMaxLevel(targetRoles);
  if (src === ROLE_LEVELS[ROLES.OWNER]) return true;
  return src > tgt;
}

/** Lista de roles atribuíveis (exclui GUEST), ordenada por nível desc. */
export function getAllRoles() {
  return Object.entries(ROLE_LEVELS)
    .filter(([name]) => name !== ROLES.GUEST)
    .sort((a, b) => b[1] - a[1])
    .map(([name, level]) => ({ name, level }));
}

export const PermissionService = {
  ROLES, ROLE_LEVELS, ROLE_HIERARCHY, GRANTS, PERMISSIONS_MAP,
  getUserMaxLevel, getHighestLevel, can, hasPermission, hasRole,
  canManageTarget, getAllRoles,
};
