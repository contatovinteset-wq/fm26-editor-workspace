import React, { Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import Pages from './pages/Pages';
import ErrorBoundary from './ErrorBoundary';
import ProtectedRoute from './components/ProtectedRoute';
import ScrollToTop from './components/ScrollToTop';
import RequireRole from './components/RequireRole';
import { AuthProvider } from './context/AuthContext';

function App() {
  return (
    <ErrorBoundary>
      <AuthProvider>
        <BrowserRouter>
          <ScrollToTop />
          <Routes>
          {/* ROTA PURA DO OBS OVERLAY SEM LAYOUT NAVBAR/FOOTER */}
          <Route path="/reidamesa/overlay" element={<Suspense fallback={null}><Pages.ReiDaMesaOverlay /></Suspense>} />

          <Route path="/" element={<Layout />}>
            <Route index element={<Pages.Home />} />
            <Route path="telecurso" element={<Pages.Telecurso />} />
            <Route path="downloads" element={<Pages.Downloads />} />
            <Route path="downloads/:id" element={<Pages.Topico />} />
            
            {/* Rotas do Fórum Novo */}
            <Route path="forum" element={<Pages.ForumHome />} />
            <Route path="forum/:slug" element={<Pages.ForumCategory />} />
            <Route path="forum/t/:id" element={<Pages.ForumThread />} />
            <Route path="login" element={<Pages.Login />} />
            <Route path="cadastro" element={<Pages.Cadastro />} />
            <Route path="minhaconta" element={<Pages.MinhaConta />} />
            
            {/* Protected Routes */}
            <Route element={<ProtectedRoute />}>
              <Route path="reidamesa" element={<Pages.ReiDaMesa />} />
              <Route path="reidamesa/admin" element={<Pages.ReiDaMesaAdmin />} />
              <Route path="reidamesa/escalar" element={<Pages.Escalacao />} />
              <Route path="reidamesa/plantel" element={<Pages.PlantelReiDaMesa />} />
              <Route path="reidamesa/ranking" element={<Pages.Ranking />} />
              <Route path="reidamesa/perfil" element={<Pages.PerfilManager />} />
              <Route path="admin" element={<Pages.AdminPanel />} />
              <Route path="moderacao" element={<Pages.ModeratorPanel />} />
            </Route>

            <Route path="ferramentas">
               <Route path="analise-de-dados" element={<Pages.AnaliseDados />} />
            </Route>
            
            <Route path="mods" element={<Navigate to="/downloads" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
      </AuthProvider>
    </ErrorBoundary>
  );
}

export default App;
