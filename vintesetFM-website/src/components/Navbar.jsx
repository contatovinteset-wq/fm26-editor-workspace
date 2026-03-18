import React from 'react';
import { Home, MonitorPlay, Wrench, Palette, Video, Twitter, Youtube, DiscIcon as DiscordIcon, Twitch } from 'lucide-react';
import { Link, useLocation } from 'react-router-dom';

const Navbar = () => {
  const location = useLocation();

  const navLinks = [
    { name: 'Início', path: '/', icon: Home },
    { name: 'Telecurso 27', path: '/telecurso', icon: MonitorPlay },
    { name: 'Ferramentas', path: '/ferramentas', icon: Wrench },
    { name: 'Mods', path: '/mods', icon: Palette },
    { name: 'Vídeos', path: '/videos', icon: Video },
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

          {/* Social Icons */}
          <div className="hidden lg:flex items-center gap-3">
            <a href="https://discord.gg/Z5XMk427vy" target="_blank" rel="noreferrer" className="p-2 text-gray-400 hover:text-[#5865F2] hover:bg-[#5865F2]/10 rounded-full transition-colors">
              <DiscordIcon size={20} />
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
