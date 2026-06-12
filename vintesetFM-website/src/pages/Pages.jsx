import { lazy } from 'react';

/**
 * Páginas carregadas sob demanda (code-splitting via React.lazy).
 * Cada rota vira um chunk separado, reduzindo o bundle inicial.
 * O <Suspense> que cobre essas páginas fica no Layout (em torno do <Outlet/>)
 * e, para a rota standalone do overlay, em App.jsx.
 */
const Pages = {
  Home: lazy(() => import('./Home')),
  Telecurso: lazy(() => import('./Telecurso')),
  Downloads: lazy(() => import('./Downloads')),
  ReiDaMesa: lazy(() => import('./ReiDaMesa')),
  Escalacao: lazy(() => import('./Escalacao')),
  Ranking: lazy(() => import('./Ranking')),
  PerfilManager: lazy(() => import('./PerfilManager')),
  PlantelReiDaMesa: lazy(() => import('./PlantelReiDaMesa')),
  Login: lazy(() => import('./Login')),
  Cadastro: lazy(() => import('./Cadastro')),
  Topico: lazy(() => import('./Topico')),
  MinhaConta: lazy(() => import('./MinhaConta')),
  AdminPanel: lazy(() => import('./AdminPanel')),
  ModeratorPanel: lazy(() => import('./ModeratorPanel')),
  ReiDaMesaAdmin: lazy(() => import('./ReiDaMesaAdmin')),
  ReiDaMesaOverlay: lazy(() => import('./ReiDaMesaOverlay')),
  ReiDaMesaCriadores: lazy(() => import('./ReiDaMesaCriadores')),
  ReiDaMesaCriadoresAdmin: lazy(() => import('./ReiDaMesaCriadoresAdmin')),
  ReiDaMesaPerfilCriador: lazy(() => import('./ReiDaMesaPerfilCriador')),
  ReiDaMesaTrofeus: lazy(() => import('./ReiDaMesaTrofeus')),
  ForumHome: lazy(() => import('./forum/ForumHome')),
  ForumCategory: lazy(() => import('./forum/ForumCategory')),
  ForumThread: lazy(() => import('./forum/ForumThread')),
  AnaliseDados: lazy(() => import('./ferramentas/AnaliseDados')),
};

export default Pages;
