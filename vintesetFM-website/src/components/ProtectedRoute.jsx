import React from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const ProtectedRoute = () => {
  const { user, isLoading } = useAuth();
  const location = useLocation();

  // Enquanto verifica a sessão (/api/auth/me), mostra um loader.
  // Evita o "flash" de tela preta e o redirect prematuro antes do fetch terminar.
  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <div className="w-10 h-10 border-4 border-white/20 border-t-primary rounded-full animate-spin" />
      </div>
    );
  }

  // Deslogado: manda pro login guardando a URL pretendida (pra voltar depois de logar).
  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <Outlet />;
};

export default ProtectedRoute;
