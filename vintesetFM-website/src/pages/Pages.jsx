import Home from './Home';
import Telecurso from './Telecurso';
import Downloads from './Downloads';
import ReiDaMesa from './ReiDaMesa';
import Escalacao from './Escalacao';
import Ranking from './Ranking';
import PerfilManager from './PerfilManager';
import Login from './Login';
import Cadastro from './Cadastro';
import Topico from './Topico';
import MinhaConta from './MinhaConta';
import AdminPanel from './AdminPanel';
import ModeratorPanel from './ModeratorPanel';
import ReiDaMesaAdmin from './ReiDaMesaAdmin';

const Pages = {
  Home,
  Telecurso: () => <Telecurso />,
  Downloads: () => <Downloads />,
  ReiDaMesa: () => <ReiDaMesa />,
  Escalacao: () => <Escalacao />,
  Ranking: () => <Ranking />,
  PerfilManager: () => <PerfilManager />,
  Login: () => <Login />,
  Cadastro: () => <Cadastro />,
  Topico: () => <Topico />,
  MinhaConta: () => <MinhaConta />,
  AdminPanel: () => <AdminPanel />,
  ModeratorPanel: () => <ModeratorPanel />
};

export default Pages;
