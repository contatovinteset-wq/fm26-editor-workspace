import React from 'react';
import { Youtube, Twitch, MessageCircle } from 'lucide-react';

const XIconSVG = ({ size = 24, className = "" }) => (
  <svg xmlns="http://www.w3.org/2000/svg" width={size} height={size} viewBox="0 0 1200 1227" className={`fill-current ${className}`}>
    <path d="M714.163 519.284L1160.89 0H1055.03L667.137 450.887L357.328 0H0L468.492 681.821L0 1226.37H105.866L515.491 750.218L842.672 1226.37H1200L714.137 519.284H714.163ZM569.165 687.828L521.697 619.934L144.011 79.6944H306.615L611.412 515.685L658.88 583.579L1055.08 1150.3H892.476L569.165 687.854V687.828Z" />
  </svg>
);

const Footer = () => {
  return (
    <footer className="bg-[#0A0D14] border-t border-white/5 rounded-t-[4rem] mt-24 pt-16 pb-8">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-12">
          
          {/* Brand Col */}
          <div className="col-span-1 md:col-span-2 space-y-4">
            <div className="flex items-center gap-3">
              <span className="text-2xl font-black tracking-tight text-white uppercase drop-shadow-md">
                vinteset<span className="text-primary">FM</span>
              </span>
            </div>
            <p className="text-gray-400 text-sm max-w-[20rem]">
              Sua central definitiva de Football Manager 26. Domine a arte do Moneyball, faça sua equipe decolar e deixe a mesmice no passado.
            </p>
            
            {/* System Status */}
            <div className="flex items-center gap-2 mt-6">
              <span className="relative flex h-3 w-3">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-3 w-3 bg-green-500"></span>
              </span>
              <span className="text-xs font-mono text-gray-400 tracking-widest uppercase">System Operational</span>
            </div>
          </div>

          {/* Links Col 1 */}
          <div className="space-y-4">
            <h4 className="text-white font-bold tracking-wide">Plataforma</h4>
            <ul className="space-y-2 text-sm text-gray-400">
              <li><a href="/telecurso" className="hover:text-white transition-colors">Telecurso</a></li>
              <li><a href="/downloads" className="hover:text-white transition-colors">Downloads</a></li>
              <li><a href="/reidamesa" className="hover:text-white transition-colors">Rei da Mesa</a></li>
            </ul>
          </div>

          {/* Socials Col */}
          <div className="space-y-4">
            <h4 className="text-white font-bold tracking-wide">Comunidade</h4>
            <div className="flex gap-4">
              <a href="https://x.com/vintesetFM" target="_blank" rel="noreferrer" className="text-gray-400 hover:text-white transition-colors flex items-center justify-center">
                <XIconSVG size={20} />
              </a>
              <a href="https://twitch.tv/vinteset" target="_blank" rel="noreferrer" className="text-gray-400 hover:text-[#9146FF] transition-colors">
                <Twitch size={24} />
              </a>
              <a href="https://youtube.com/vintesetFM" target="_blank" rel="noreferrer" className="text-gray-400 hover:text-[#FF0000] transition-colors">
                <Youtube size={24} />
              </a>
              <a href="https://whatsapp.com/channel/0029Van5rk6IHphDZzEsNc30" target="_blank" rel="noreferrer" className="text-gray-400 hover:text-[#25D366] transition-colors">
                <MessageCircle size={24} />
              </a>
              <a href="https://discord.gg/Z5XMk427vy" target="_blank" rel="noreferrer" className="text-gray-400 hover:text-[#5865F2] transition-colors flex items-center justify-center">
                 <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 127.14 96.36" className="fill-current">
                   <path d="M107.7,8.07A105.15,105.15,0,0,0,81.47,0a72.06,72.06,0,0,0-3.36,6.83A97.68,97.68,0,0,0,49,6.83,72.37,72.37,0,0,0,45.64,0,105.89,105.89,0,0,0,19.39,8.09C2.79,32.65-1.71,56.6.54,80.21h0A105.73,105.73,0,0,0,32.71,96.36,77.7,77.7,0,0,0,39.6,85.25a68.42,68.42,0,0,1-10.85-5.18c.91-.66,1.8-1.34,2.66-2a75.57,75.57,0,0,0,64.32,0c.87.71,1.76,1.39,2.66,2a68.68,68.68,0,0,1-10.87,5.19,77.7,77.7,0,0,0,6.89,11.1,105.25,105.25,0,0,0,32.19-16.14h0C129.24,52.84,122.09,29.11,107.7,8.07ZM42.45,65.69C36.18,65.69,31,60,31,53s5-12.74,11.43-12.74S54,46,53.89,53,48.84,65.69,42.45,65.69Zm42.24,0C78.41,65.69,73.31,60,73.31,53s5-12.74,11.43-12.74S96.2,46,96.12,53,91.08,65.69,84.69,65.69Z" />
                 </svg>
              </a>
            </div>
          </div>

        </div>

        <div className="border-t border-white/5 mt-12 pt-8 flex flex-col md:flex-row justify-between items-center gap-4">
          <p className="text-gray-500 text-xs">
            © 2026 vintesetFM. Todos os direitos reservados.
          </p>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
