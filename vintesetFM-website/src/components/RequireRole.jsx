import React from 'react';
import { useAuth } from '../context/AuthContext';
import { hasPermission, hasRole, ROLE_HIERARCHY } from '../config/permissions';

/**
 * Componente que exibe seu conteúdo (children) somente se o usuário
 * possuir a role ou permissão necessária.
 *
 * Props:
 *   - required: string com roles separadas por vírgula. Ex: "ADMIN,MODERATOR"
 *   - permission: string de permissão granular. Ex: "downloads:create"
 *   - fallback: componente alternativo a exibir quando sem permissão (opcional)
 *
 * Exemplos:
 *   <RequireRole required="ADMIN">...</RequireRole>
 *   <RequireRole permission="downloads:create">...</RequireRole>
 *   <RequireRole permission="admin:manage_roles" fallback={<p>Sem acesso</p>}>...</RequireRole>
 */
const RequireRole = ({ required, permission, fallback = null, children }) => {
  const { user, isLoading } = useAuth();

  if (isLoading || !user) {
    return fallback;
  }

  // Parsear roles do usuário
  let userRoles = user.roles;
  if (typeof userRoles === 'string') {
    try { userRoles = JSON.parse(userRoles); } catch { userRoles = [userRoles]; }
  }
  if (!Array.isArray(userRoles)) userRoles = ['USER'];

  // Check por permissão granular (usa hierarquia numérica)
  if (permission) {
    if (!hasPermission(userRoles, permission)) {
      return fallback;
    }
    return <>{children}</>;
  }

  // Check por role direta (retrocompatível)
  if (required) {
    const requiredList = required.split(',').map(r => r.trim());
    if (!hasRole(userRoles, requiredList)) {
      return fallback;
    }
  }

  return <>{children}</>;
};

export default RequireRole;
