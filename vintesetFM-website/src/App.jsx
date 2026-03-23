import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import Pages from './pages/Pages';
import ErrorBoundary from './ErrorBoundary';
import ProtectedRoute from './components/ProtectedRoute';

function App() {
  return (
    <ErrorBoundary>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Layout />}>
            <Route index element={<Pages.Home />} />
            <Route path="telecurso" element={<Pages.Telecurso />} />
            <Route path="downloads" element={<Pages.Downloads />} />
            <Route path="downloads/:id" element={<Pages.Topico />} />
            <Route path="login" element={<Pages.Login />} />
            <Route path="cadastro" element={<Pages.Cadastro />} />
            <Route path="minhaconta" element={<Pages.MinhaConta />} />
            
            {/* Protected Routes */}
            <Route element={<ProtectedRoute />}>
              <Route path="reidamesa" element={<Pages.ReiDaMesa />} />
              <Route path="reidamesa/escalar" element={<Pages.Escalacao />} />
              <Route path="reidamesa/ranking" element={<Pages.Ranking />} />
              <Route path="reidamesa/perfil" element={<Pages.PerfilManager />} />
            </Route>

            <Route path="ferramentas" element={<Navigate to="/downloads" replace />} />
            <Route path="mods" element={<Navigate to="/downloads" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ErrorBoundary>
  );
}

export default App;
