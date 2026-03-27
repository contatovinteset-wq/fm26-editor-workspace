import React from 'react';
import { Crown, ShieldAlert, Shield, User } from 'lucide-react';
import { ROLE_HIERARCHY } from '../config/permissions';

const ROLE_COLORS = {
  OWNER: 'bg-amber-500 text-black border-amber-600',
  ADMIN: 'bg-blue-500 text-white border-blue-600',
  MODERATOR: 'bg-emerald-500 text-white border-emerald-600',
  USER: 'bg-gray-700 text-white border-gray-900',
};

const ROLE_LABELS = {
  OWNER: 'Owner',
  ADMIN: 'Administrador',
  MODERATOR: 'Moderador',
  USER: 'Membro',
};

const RoleIcons = {
  OWNER: Crown,
  ADMIN: ShieldAlert,
  MODERATOR: Shield,
  USER: User,
};

const RoleBadge = ({ role, roles, small = false, absolute = false, className = '' }) => {
  // Determinar a role de maior hierarquia
  let displayRole = 'USER';
  
  if (role) {
    displayRole = role;
  } else if (roles) {
    let parsedRoles = roles;
    if (typeof roles === 'string') {
      try { parsedRoles = JSON.parse(roles); } catch { parsedRoles = [roles]; }
    }
    if (!Array.isArray(parsedRoles)) parsedRoles = ['USER'];
    
    // Ordena do maior pro menor
    const sorted = [...parsedRoles].sort((a, b) => (ROLE_HIERARCHY[b] || 0) - (ROLE_HIERARCHY[a] || 0));
    if (sorted.length > 0) {
      displayRole = sorted[0];
    }
  }

  const colors = ROLE_COLORS[displayRole] || ROLE_COLORS.USER;
  const label = ROLE_LABELS[displayRole] || 'Membro';
  const Icon = RoleIcons[displayRole] || User;

  const baseClasses = `inline-flex items-center justify-center gap-1 font-black tracking-wider uppercase rounded-full shadow-md z-20 whitespace-nowrap`;
  const sizeClasses = small ? 'px-2 py-0.5 text-[9px] border' : 'px-4 py-1.5 text-[10px] border-2';
  const positionClasses = absolute ? 'absolute -bottom-4 left-1/2 -translate-x-1/2 w-max' : '';

  return (
    <div className={`${baseClasses} ${sizeClasses} ${positionClasses} ${colors} ${className}`}>
      <Icon size={small ? 10 : 14} />
      {label}
    </div>
  );
};

export default RoleBadge;
