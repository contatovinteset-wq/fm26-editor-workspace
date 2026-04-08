import { useState } from 'react';
import { Home, MonitorPlay, Youtube, Twitch, DownloadCloud, Crown, Settings, LogOut, Menu, X, ShieldAlert, MessageSquare, Wrench } from 'lucide-react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { motion, AnimatePresence } from 'framer-motion';

const DiscordIconSVG = ({ size = 24, className = "" }) => (
  <svg xmlns="http://www.w3.org/2000/svg" width={size} height={size} viewBox="0 0 127.14 96.36" className={`fill-current ${className}`}>
    <path d="M107.7,8.07A105.15,105.15,0,0,0,81.47,0a72.06,72.06,0,0,0-3.36,6.83A97.68,97.68,0,0,0,49,6.83,72.37,72.37,0,0,0,45.64,0,105.89,105.89,0,0,0,19.39,8.09C2.79,32.65-1.71,56.6.54,80.21h0A105.73,105.73,0,0,0,32.71,96.36,77.7,77.7,0,0,0,39.6,85.25a68.42,68.42,0,0,1-10.85-5.18c.91-.66,1.8-1.34,2.66-2a75.57,75.57,0,0,0,64.32,0c.87.71,1.76,1.39,2.66,2a68.68,68.68,0,0,1-10.87,5.19,77.7,77.7,0,0,0,6.89,11.1,105.25,105.25,0,0,0,32.19-16.14h0C129.24,52.84,122.09,29.11,107.7,8.07ZM42.45,65.69C36.18,65.69,31,60,31,53s5-12.74,11.43-12.74S54,46,53.89,53,48.84,65.69,42.45,65.69Zm42.24,0C78.41,65.69,73.31,60,73.31,53s5-12.74,11.43-12.74S96.2,46,96.12,53,91.08,65.69,84.69,65.69Z" />
  </svg>
);

const XIconSVG = ({ size = 24, className = "" }) => (
  <svg xmlns="http://www.w3.org/2000/svg" width={size} height={size} viewBox="0 0 1200 1227" className={`fill-current ${className}`}>
    <path d="M714.163 519.284L1160.89 0H1055.03L667.137 450.887L357.328 0H0L468.492 681.821L0 1226.37H105.866L515.491 750.218L842.672 1226.37H1200L714.137 519.284H714.163ZM569.165 687.828L521.697 619.934L144.011 79.6944H306.615L611.412 515.685L658.88 583.579L1055.08 1150.3H892.476L569.165 687.854V687.828Z" />
  </svg>
);

const Navbar = () => {
  const location = useLocation();
  const { user, logout } = useAuth();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  // Close mobile menu when route changes
  useState(() => setIsMobileMenuOpen(false), [location]);

  const isOwner = user?.roles?.includes('OWNER');
  const isAdmin = user?.roles?.includes('ADMIN') || isOwner;
  const isModerator = user?.roles?.includes('MODERATOR') || isAdmin;

  const navLinks = [
    { name: 'Início', path: '/', icon: Home },
    { name: 'Telecurso 27', path: '/telecurso', icon: MonitorPlay },
    { name: 'Fórum', path: '/forum', icon: MessageSquare },
    { name: 'Ferramentas', path: '/ferramentas/analise-de-dados', icon: Wrench },
    { name: 'Downloads', path: '/downloads', icon: DownloadCloud },
    { name: 'Rei Da Mesa', path: '/reidamesa', icon: Crown },
  ];

  return (
    <nav className="fixed top-0 w-full z-50 glass border-b border-white/5 bg-bgDark/90 backdrop-blur-md">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-20">
          
          {/* Logo Section */}
          <div className="flex items-center gap-3 shrink-0">
            <Link to="/" className="flex flex-col items-center justify-center group relative h-full shrink-0"> 
              <div className="relative group-hover:scale-105 transition-transform flex items-center justify-center">
                <img 
                  src="/vinteset_escudo.png" 
                  alt="vintesetFM Logo" 
                  className="h-12 sm:h-14 md:h-16 w-auto object-contain drop-shadow-[0_0_15px_rgba(59,91,219,0.3)] mt-1"
                />
              </div>
            </Link>
          </div>

          {/* Hamburger Menu Toggle */}
          <div className="md:hidden flex items-center">
             <button 
               onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
               className="p-2 text-gray-400 hover:text-white transition-colors"
             >
               {isMobileMenuOpen ? <X size={28} /> : <Menu size={28} />}
             </button>
          </div>

          {/* Navigation - Desktop */}
          <div className="hidden md:flex space-x-1 flex-1 justify-center px-4">
            {navLinks.map((link) => {
              const Icon = link.icon;
              const isActive = location.pathname === link.path;
              
              return (
                <Link
                  key={link.name}
                  to={link.path}
                  className={`flex items-center gap-1.5 lg:gap-2 px-2.5 lg:px-4 py-2 rounded-xl text-[13px] lg:text-sm font-semibold transition-all duration-300 whitespace-nowrap ${
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

          {/* Auth & Social Icons - Desktop */}
          <div className="hidden md:flex items-center gap-2 lg:gap-4 shrink-0">
             {!user ? (
               <div className="flex items-center gap-2 lg:gap-3 mr-1 lg:mr-2 border-r border-white/10 pr-2 lg:pr-4">
                  <Link to="/login" className="text-xs font-bold uppercase tracking-widest text-gray-400 hover:text-white transition-colors">Entrar</Link>
                  <Link to="/cadastro" className="bg-accent hover:bg-accentHover text-black px-3 lg:px-4 py-1.5 rounded-lg font-black uppercase text-[10px] lg:text-xs tracking-wider transition-all">Criar Conta</Link>
               </div>
             ) : (
               <div className="flex items-center gap-2 lg:gap-3 mr-1 lg:mr-2 border-r border-white/10 pr-2 lg:pr-4">
                   {(isOwner || isAdmin) && (
                    <Link to="/admin" className="hidden lg:flex items-center gap-2 bg-accent/10 border border-accent/30 text-accent hover:bg-accent/20 px-3 py-1.5 rounded-lg font-black uppercase text-[10px] tracking-widest transition-all">
                      <Settings size={14} /> Admin
                    </Link>
                  )}
                  {isModerator && (
                    <Link to="/moderacao" className="hidden lg:flex items-center gap-2 bg-green-500/10 border border-green-500/30 text-green-400 hover:bg-green-500/20 px-3 py-1.5 rounded-lg font-black uppercase text-[10px] tracking-widest transition-all">
                      <ShieldAlert size={14} /> Fila
                    </Link>
                  )}
                  <Link to="/minhaconta" className="flex items-center gap-2 bg-white/5 hover:bg-white/10 border border-white/10 px-2 lg:px-3 py-1.5 rounded-full font-bold uppercase text-[10px] lg:text-xs transition-colors group">
                    <div className="w-6 h-6 bg-accent/20 rounded-full border border-accent/40 flex items-center justify-center overflow-hidden">
                      {user.avatar ? <img src={user.avatar} alt="" className="w-full h-full object-cover" /> : <span className="text-[10px] text-accent">{(user.nickname || 'M')?.charAt(0)}</span>}
                    </div>
                    <span className="max-w-[80px] lg:max-w-[100px] truncate group-hover:text-accent transition-colors">{user.nickname || 'Manager'}</span>
                  </Link>
               </div>
             )}
             
            <div className="hidden lg:flex items-center gap-1">
              <a href="https://discord.gg/Z5XMk427vy" target="_blank" rel="noreferrer" className="p-2 text-gray-400 hover:text-[#5865F2] hover:bg-[#5865F2]/10 rounded-full transition-colors">
                <DiscordIconSVG size={18} />
              </a>
              <a href="https://twitch.tv/vinteset" target="_blank" rel="noreferrer" className="p-2 text-gray-400 hover:text-[#9146FF] hover:bg-[#9146FF]/10 rounded-full transition-colors">
                <Twitch size={18} />
              </a>
              <a href="https://youtube.com/@vintesetFM" target="_blank" rel="noreferrer" className="p-2 text-gray-400 hover:text-[#FF0000] hover:bg-[#FF0000]/10 rounded-full transition-colors">
                <Youtube size={18} />
              </a>
              <a href="https://x.com/vintesetFM" target="_blank" rel="noreferrer" className="p-2 text-gray-400 hover:text-white hover:bg-white/10 rounded-full transition-colors flex items-center justify-center">
                <XIconSVG size={16} />
              </a>
            </div>
          </div>

        </div>
      </div>

      {/* Mobile Drawer */}
      <AnimatePresence>
        {isMobileMenuOpen && (
          <motion.div 
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
            className="md:hidden border-t border-white/10 bg-bgDark/95 backdrop-blur-xl overflow-hidden"
          >
            <div className="px-4 py-6 space-y-6">
              
              {/* Mobile Profile / Auth */}
              <div className="mb-4 pb-4 border-b border-white/5">
                {user ? (
                  <div className="flex flex-col gap-4">
                    <Link to="/minhaconta" onClick={() => setIsMobileMenuOpen(false)} className="flex items-center gap-3">
                      <div className="w-10 h-10 bg-accent/20 rounded-full border-2 border-accent/40 flex items-center justify-center overflow-hidden">
                        {user.avatar ? <img src={user.avatar} alt="" className="w-full h-full object-cover" /> : <span className="text-xs text-accent">{(user.nickname || 'M')?.charAt(0)}</span>}
                      </div>
                      <div className="flex-1">
                        <div className="text-sm font-bold truncate text-white">{user.nickname || 'Manager'}</div>
                        <div className="text-[10px] text-accent uppercase tracking-wider font-bold">Ver Perfil</div>
                      </div>
                    </Link>
                    
                     <div className="grid grid-cols-2 gap-2 mt-2">
                       {isModerator && (
                         <Link to="/moderacao" onClick={() => setIsMobileMenuOpen(false)} className="flex items-center justify-center gap-2 bg-green-500/10 border border-green-500/30 text-green-400 hover:bg-green-500/20 px-3 py-2 rounded-lg font-black uppercase text-[10px] tracking-widest transition-all">
                           <ShieldAlert size={14} /> Moderação
                         </Link>
                       )}
                       {(isOwner || isAdmin) && (
                         <Link to="/admin" onClick={() => setIsMobileMenuOpen(false)} className="flex items-center justify-center gap-2 bg-accent/10 border border-accent/30 text-accent hover:bg-accent/20 px-3 py-2 rounded-lg font-black uppercase text-[10px] tracking-widest transition-all">
                           <Settings size={14} /> Admin
                         </Link>
                       )}
                       <button onClick={() => { logout(); setIsMobileMenuOpen(false); }} className="flex items-center justify-center gap-2 bg-red-500/10 border border-red-500/30 text-red-500 hover:bg-red-500/20 px-3 py-2 rounded-lg font-black uppercase text-[10px] tracking-widest transition-all col-span-2">
                         <LogOut size={14} /> Sair
                       </button>
                    </div>
                  </div>
                ) : (
                  <div className="flex flex-col gap-3">
                    <Link to="/cadastro" onClick={() => setIsMobileMenuOpen(false)} className="bg-accent hover:bg-accentHover text-black px-4 py-3 text-center rounded-xl font-black uppercase text-xs tracking-wider transition-all">
                      Criar Conta
                    </Link>
                    <Link to="/login" onClick={() => setIsMobileMenuOpen(false)} className="bg-white/5 border border-white/10 text-white px-4 py-3 text-center rounded-xl font-bold uppercase text-xs tracking-wider hover:bg-white/10 transition-colors">
                      Entrar
                    </Link>
                  </div>
                )}
              </div>

              {/* Mobile Navigation Links */}
              <div className="flex flex-col gap-2">
                {navLinks.map((link) => {
                  const Icon = link.icon;
                  const isActive = location.pathname === link.path;
                  
                  return (
                    <Link
                      key={link.name}
                      to={link.path}
                      onClick={() => setIsMobileMenuOpen(false)}
                      className={`flex items-center gap-4 px-4 py-3 rounded-xl font-bold transition-all ${
                        isActive 
                          ? 'bg-primary/20 text-accent border border-primary/30' 
                          : 'text-gray-300 hover:text-white hover:bg-white/5'
                      }`}
                    >
                      <Icon size={20} className={isActive ? "text-accent" : "opacity-70"} />
                      {link.name}
                    </Link>
                  );
                })}
              </div>

              {/* Mobile Socials */}
              <div className="pt-4 mt-2 border-t border-white/5 flex items-center justify-center gap-6">
                <a href="https://discord.gg/Z5XMk427vy" target="_blank" rel="noreferrer" className="text-gray-400 hover:text-[#5865F2] transition-colors"><DiscordIconSVG size={24} /></a>
                <a href="https://twitch.tv/vinteset" target="_blank" rel="noreferrer" className="text-gray-400 hover:text-[#9146FF] transition-colors"><Twitch size={24} /></a>
                <a href="https://youtube.com/@vintesetFM" target="_blank" rel="noreferrer" className="text-gray-400 hover:text-[#FF0000] transition-colors"><Youtube size={24} /></a>
                <a href="https://x.com/vintesetFM" target="_blank" rel="noreferrer" className="text-gray-400 hover:text-white transition-colors flex items-center justify-center"><XIconSVG size={22} /></a>
              </div>

            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </nav>
  );
};

export default Navbar;
