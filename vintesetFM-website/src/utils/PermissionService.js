/**
 * Utilitário Isomórfico (roda tanto no frontend, via React, quanto no backend, via NodeJS Express)
 * para gerenciar Hierarquia, Permissões e Perfis do sistema VintesetFM.
 */

export const ROLES = {
  OWNER: 'OWNER',
  ADMIN: 'ADMIN',
  ADMIN_DOWNLOADS: 'ADMIN_DOWNLOADS',
  ADMIN_GERACAO: 'ADMIN_GERACAO',
  USER: 'USER',
  GUEST: 'GUEST'
};

/**
 * Matriz de Hierarquia de Poder.
 * Nivel maior que outro significa que pode editar perfis inferiores (respeitando escopos).
 * OWNER (100) > ADMIN (80) > ADMIN_* (60) > USER (10) > GUEST (0)
 */
export const ROLE_LEVELS = {
  [ROLES.OWNER]: 100,
  [ROLES.ADMIN]: 80,
  [ROLES.ADMIN_DOWNLOADS]: 60,
  [ROLES.ADMIN_GERACAO]: 60,
  [ROLES.USER]: 10,
  [ROLES.GUEST]: 0
};

/**
 * Matriz de Permissões Explícitas Mapeadas
 * Um usuário pode ter a ação liberada se o cargo principal (ou herdado) der acesso a true.
 */
export const PERMISSIONS_MAP = {
  // Ações globais irreversiveis ou criticas
  'change_nickname': {
    [ROLES.OWNER]: true,     // Pode ignorar e forçar
    [ROLES.ADMIN]: true,     // Pode alterar de usuários comuns em moderação
    [ROLES.USER]: false      // User NUNCA pode acionar (usará a regra: apenas na 1ª vez, que é estado, não permissão bruta)
  },
  'ban_user': {
    [ROLES.OWNER]: true,
    [ROLES.ADMIN]: true,
    [ROLES.USER]: false
  },
  'view_logs': {
    [ROLES.OWNER]: true,
    [ROLES.ADMIN]: true,
    [ROLES.USER]: false
  },
  'approve_download': {
    [ROLES.OWNER]: true,
    [ROLES.ADMIN]: true,
    [ROLES.ADMIN_DOWNLOADS]: true,
    [ROLES.ADMIN_GERACAO]: false,
    [ROLES.USER]: false
  },
  'create_download_post': {
    [ROLES.OWNER]: true,
    [ROLES.ADMIN]: true,
    [ROLES.ADMIN_DOWNLOADS]: true,
    [ROLES.USER]: false // Precisa enviar, e o Admin criar o post. Ou envia e cai pendente. Na regra definida: User envia, cai pendente. Aqui validamos a criação PÚBLICA e direta.
  }
};

/**
 * Pega o nivel hierarquico maximo que o array de roles do usuario prove
 */
export const getUserMaxLevel = (userRoles = []) => {
  if (!userRoles || userRoles.length === 0) return ROLE_LEVELS[ROLES.GUEST];
  let max = 0;
  for (const role of userRoles) {
    if (ROLE_LEVELS[role] > max) max = ROLE_LEVELS[role];
  }
  return max;
};

/**
 * Verifica se targetUser pode ser afetado por sourceUser (Ex: ban, promote)
 */
export const canManageTarget = (sourceUserRoles, targetUserRoles) => {
  const sourceLvl = getUserMaxLevel(sourceUserRoles);
  const targetLvl = getUserMaxLevel(targetUserRoles);

  if (sourceLvl === ROLE_LEVELS[ROLES.OWNER]) return true; // Owner dita regras
  return sourceLvl > targetLvl; 
};

/**
 * Verifica se um user possui permissão para uma ação especifica.
 * Checa na Matriz o array inteiro de roles (basta 1 role com true).
 */
export const can = (user, action) => {
  if (!user) return false;
  const roles = user.roles || [ROLES.USER];
  
  if (roles.includes(ROLES.OWNER)) return true; // Owner tem todas permissions implícitas

  const config = PERMISSIONS_MAP[action];
  if (!config) return false;

  return roles.some(role => config[role] === true);
};

export const PermissionService = {
  ROLES,
  ROLE_LEVELS,
  PERMISSIONS_MAP,
  getUserMaxLevel,
  canManageTarget,
  can
};
