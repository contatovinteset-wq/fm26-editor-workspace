import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import Pages from './pages/Pages';
import ErrorBoundary from './ErrorBoundary';

function App() {
  return (
    <ErrorBoundary>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Layout />}>
            <Route index element={<Pages.Home />} />
            <Route path="telecurso" element={<Pages.Telecurso />} />
            <Route path="ferramentas" element={<Pages.Ferramentas />} />
            <Route path="mods" element={<Pages.Mods />} />
            <Route path="videos" element={<Pages.Videos />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ErrorBoundary>
  );
}

export default App;
