import React from 'react';
import { useAuth } from '../context/AuthContext';

/**
 * Componente que exibe seu conteúdo (children) somente se o usuário
 * estiver logado e possuir a role necessária (ou for OWNER).
 * 
 * @param {string} required - A permissão requisitada. Pode ser separada por vírgulas para multiplas permissões. Ex: "ADMIN_DOWNLOADS"
 */
const RequireRole = ({ required, children }) => {
  const { user, isLoading } = useAuth();

  if (isLoading || !user || !user.roles) {
    return null; // Não exibe o conteúdo se não estiver carregado ou sem dono
  }

  let userRoles = user.roles;
  if (typeof userRoles === 'string') {
    try { userRoles = JSON.parse(userRoles); } catch { userRoles = [userRoles]; }
  }

  // Se o usuário é o Owner, renderiza instantaneamente
  if (userRoles.includes('OWNER')) {
    return <>{children}</>;
  }

  // Verifica as roles requisitadas
  if (required) {
    const requiredList = required.split(',').map(r => r.trim());
    const hasRole = requiredList.some(role => userRoles.includes(role));
    if (!hasRole) {
      return null;
    }
  }

  return <>{children}</>;
};

export default RequireRole;
