import Home from './Home';
import Telecurso from './Telecurso';

const PlaceholderPage = ({ title }) => {
  return (
    <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-4 pt-24">
      <h1 className="text-4xl md:text-6xl font-black text-white mb-6 uppercase tracking-tighter">
        {title}
      </h1>
      <p className="text-gray-400 max-w-lg mb-8">
        Esta página está atualmente em construção. Logo teremos conteúdo inédito para o seu Football Manager 26.
      </p>
      <div className="w-16 h-1 bg-accent rounded-full mb-8"></div>
    </div>
  );
};

const Pages = {
  Home,
  Telecurso: () => <Telecurso />,
  Ferramentas: () => <PlaceholderPage title="Ferramentas & Plugins" />,
  Mods: () => <PlaceholderPage title="Skins & Mods" />,
  Videos: () => <PlaceholderPage title="Acervo de Vídeos" />
};

export default Pages;
