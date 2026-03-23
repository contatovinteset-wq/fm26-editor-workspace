import React from 'react';
import { Home, MonitorPlay, Twitter, Youtube, Twitch, DownloadCloud, Crown } from 'lucide-react';
import { Link, useLocation } from 'react-router-dom';

const DiscordIconSVG = ({ size = 24, className = "" }) => (
  <svg xmlns="http://www.w3.org/2000/svg" width={size} height={size} viewBox="0 0 127.14 96.36" className={`fill-current ${className}`}>
    <path d="M107.7,8.07A105.15,105.15,0,0,0,81.47,0a72.06,72.06,0,0,0-3.36,6.83A97.68,97.68,0,0,0,49,6.83,72.37,72.37,0,0,0,45.64,0,105.89,105.89,0,0,0,19.39,8.09C2.79,32.65-1.71,56.6.54,80.21h0A105.73,105.73,0,0,0,32.71,96.36,77.7,77.7,0,0,0,39.6,85.25a68.42,68.42,0,0,1-10.85-5.18c.91-.66,1.8-1.34,2.66-2a75.57,75.57,0,0,0,64.32,0c.87.71,1.76,1.39,2.66,2a68.68,68.68,0,0,1-10.87,5.19,77.7,77.7,0,0,0,6.89,11.1,105.25,105.25,0,0,0,32.19-16.14h0C129.24,52.84,122.09,29.11,107.7,8.07ZM42.45,65.69C36.18,65.69,31,60,31,53s5-12.74,11.43-12.74S54,46,53.89,53,48.84,65.69,42.45,65.69Zm42.24,0C78.41,65.69,73.31,60,73.31,53s5-12.74,11.43-12.74S96.2,46,96.12,53,91.08,65.69,84.69,65.69Z" />
  </svg>
);

const Navbar = () => {
  const location = useLocation();

  const navLinks = [
    { name: 'Início', path: '/', icon: Home },
    { name: 'Telecurso 27', path: '/telecurso', icon: MonitorPlay },
    { name: 'Downloads', path: '/downloads', icon: DownloadCloud },
    { name: 'Rei Da Mesa', path: '/reidamesa', icon: Crown },
  ];

  return (
    <nav className="fixed top-0 w-full z-50 glass border-b border-white/5 bg-bgDark/80">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-20">
          
          {/* Logo Section */}
          <div className="flex items-center gap-3">
            <Link to="/" className="flex flex-col items-center justify-center group relative h-full"> 
              <div className="relative group-hover:scale-105 transition-transform flex items-center justify-center">
                <img 
                  src="/vinteset_escudo.png" 
                  alt="vintesetFM Logo" 
                  className="h-14 md:h-16 w-auto object-contain drop-shadow-[0_0_15px_rgba(59,91,219,0.3)] mt-1"
                />
              </div>
            </Link>
          </div>

          {/* Navigation */}
          <div className="hidden md:flex space-x-1">
            {navLinks.map((link) => {
              const Icon = link.icon;
              const isActive = location.pathname === link.path;
              
              return (
                <Link
                  key={link.name}
                  to={link.path}
                  className={`flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold transition-all duration-300 ${
                    isActive 
                      ? 'bg-primary/20 text-white shadow-inner border border-primary/30' 
                      : 'text-gray-400 hover:text-white hover:bg-white/5'
                  }`}
                >
                  <Icon size={16} className={isActive ? "text-accent" : ""} />
                  {link.name}
                </Link>
              );
            })}
          </div>

          {/* Auth & Social Icons */}
          <div className="hidden lg:flex items-center gap-4">
             {/* Botoes de Visita */}
             <div className="flex items-center gap-3 mr-2 border-r border-white/10 pr-4">
                <Link to="/login" className="text-xs font-bold uppercase tracking-widest text-gray-400 hover:text-white transition-colors">Entrar</Link>
                <Link to="/cadastro" className="bg-accent hover:bg-accentHover text-black px-4 py-1.5 rounded-lg font-black uppercase text-xs tracking-wider transition-all">Criar Conta</Link>
             </div>
             
             {/* Mock de Conta Logada (comentado para o futuro)
             <div className="flex items-center gap-3 mr-2 border-r border-white/10 pr-4">
                <Link to="/minhaconta" className="flex items-center gap-2 bg-white/5 hover:bg-white/10 border border-white/10 px-3 py-1.5 rounded-full font-bold uppercase text-xs transition-colors">
                  <div className="w-5 h-5 bg-accent rounded-full border border-black"></div>
                  Painel
                </Link>
             </div>
             */}
            <a href="https://discord.gg/Z5XMk427vy" target="_blank" rel="noreferrer" className="p-2 text-gray-400 hover:text-[#5865F2] hover:bg-[#5865F2]/10 rounded-full transition-colors">
              <DiscordIconSVG size={20} />
            </a>
            <a href="https://twitch.tv/vinteset" target="_blank" rel="noreferrer" className="p-2 text-gray-400 hover:text-[#9146FF] hover:bg-[#9146FF]/10 rounded-full transition-colors">
              <Twitch size={20} />
            </a>
            <a href="https://youtube.com/@vintesetFM" target="_blank" rel="noreferrer" className="p-2 text-gray-400 hover:text-[#FF0000] hover:bg-[#FF0000]/10 rounded-full transition-colors">
              <Youtube size={20} />
            </a>
            <a href="https://x.com/vintesetFM" target="_blank" rel="noreferrer" className="p-2 text-gray-400 hover:text-white hover:bg-white/10 rounded-full transition-colors">
              <Twitter size={20} />
            </a>
          </div>

        </div>
      </div>
    </nav>
  );
};

export default Navbar;
