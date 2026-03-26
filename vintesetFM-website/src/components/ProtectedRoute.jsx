import React from 'react';
import { Outlet } from 'react-router-dom';

const ProtectedRoute = () => {
  return <Outlet />; // Bypass temporário para gravação
};

export default ProtectedRoute;
