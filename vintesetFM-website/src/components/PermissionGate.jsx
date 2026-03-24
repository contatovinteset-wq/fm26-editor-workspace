import React from 'react';
import { useAuth } from '../context/AuthContext';
import { can } from '../utils/PermissionService';

/**
 * `<PermissionGate>` envelopa conteúdo sensível e aplica regras baseadas na Matriz.
 * 
 * Props:
 * - `action` (string): Ação que precisa ter permissão (ex: 'change_nickname').
 * - `role` (string): (Opcional) Força a exibição apenas se possuir esta role especifica (ou superior iterativa).
 * - `fallback` (node): O que renderizar caso não tenha permissão (ex: uma div ou botão disabled). Se nulo, não renderiza nada.
 * - `disableInsteadOfHide` (boolean): Se `true`, renderiza o `children` mas desabilita a interação e reduz opacidade via props do react (Clone). Util p/ botões.
 * - `disabledMessage` (string): Tooltip exibido caso `disableInsteadOfHide` for ativo.
 */
const PermissionGate = ({ 
  children, 
  action, 
  role,
  fallback = null, 
  disableInsteadOfHide = false,
  disabledMessage = 'Ação não permitida para o seu perfil.'
}) => {
  const { user } = useAuth();
  
  if (!user) return fallback;

  // Usa o serviço isomórfico
  let hasAccess = true;
  if (action) {
    hasAccess = can(user, action);
  } else if (role) {
    hasAccess = user.roles?.includes(role) || user.roles?.includes('OWNER');
  }

  if (hasAccess) {
    return <>{children}</>;
  }

  if (disableInsteadOfHide) {
    // Clona o primeiro filho injetando logs e classes de desativado
    return React.cloneElement(React.Children.only(children), {
      disabled: true,
      title: disabledMessage,
      className: `${children.props.className || ''} opacity-50 cursor-not-allowed`,
      onClick: (e) => e.preventDefault()
    });
  }

  return fallback;
};

export default PermissionGate;
