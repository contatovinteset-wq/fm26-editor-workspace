import Home from './Home';
import Telecurso from './Telecurso';
import Downloads from './Downloads';
import ReiDaMesa from './ReiDaMesa';
import Escalacao from './Escalacao';
import Ranking from './Ranking';
import PerfilManager from './PerfilManager';
import PlantelReiDaMesa from './PlantelReiDaMesa';
import Login from './Login';
import Cadastro from './Cadastro';
import Topico from './Topico';
import MinhaConta from './MinhaConta';
import AdminPanel from './AdminPanel';
import ModeratorPanel from './ModeratorPanel';
import ReiDaMesaAdmin from './ReiDaMesaAdmin';
import ReiDaMesaOverlay from './ReiDaMesaOverlay';
import ForumHome from './forum/ForumHome';
import ForumCategory from './forum/ForumCategory';
import ForumThread from './forum/ForumThread';
import AnaliseDados from './ferramentas/AnaliseDados';

const Pages = {
  Home,
  Telecurso: () => <Telecurso />,
  Downloads: () => <Downloads />,
  ReiDaMesa: () => <ReiDaMesa />,
  Escalacao: () => <Escalacao />,
  Ranking: () => <Ranking />,
  PerfilManager: () => <PerfilManager />,
  PlantelReiDaMesa: () => <PlantelReiDaMesa />,
  Login: () => <Login />,
  Cadastro: () => <Cadastro />,
  Topico: () => <Topico />,
  MinhaConta: () => <MinhaConta />,
  AdminPanel: () => <AdminPanel />,
  ModeratorPanel: () => <ModeratorPanel />,
  ReiDaMesaAdmin: () => <ReiDaMesaAdmin />,
  ReiDaMesaOverlay: () => <ReiDaMesaOverlay />,
  ForumHome: () => <ForumHome />,
  ForumCategory: () => <ForumCategory />,
  ForumThread: () => <ForumThread />,
  AnaliseDados: () => <AnaliseDados />
};

export default Pages;
