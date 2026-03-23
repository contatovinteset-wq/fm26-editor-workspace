import React, { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { checkChannelLive } from '../services/twitch';
import { getLatestNonLiveVideo } from '../services/youtube';
import { Twitch, Youtube, PlayCircle, ExternalLink, CalendarDays, DiscIcon as DiscordIcon, MessageCircle } from 'lucide-react';
import { Link } from 'react-router-dom';
import { NewsCarousel } from '../components/NewsCarousel';
import { MediaCarousel } from '../components/MediaCarousel';

const ParticlesBackground = () => {
  return (
    <div className="absolute inset-0 z-0 overflow-hidden pointer-events-none">
      {[...Array(20)].map((_, i) => (
        <motion.div
          key={i}
          className="absolute bg-white rounded-full opacity-[0.03]"
          style={{
            width: Math.random() * 4 + 1 + 'px',
            height: Math.random() * 4 + 1 + 'px',
            left: Math.random() * 100 + '%',
            top: Math.random() * 100 + '%',
          }}
          animate={{
            y: [0, -100, 0],
            opacity: [0.01, 0.1, 0.01],
          }}
          transition={{
            duration: Math.random() * 10 + 10,
            repeat: Infinity,
            ease: "linear"
          }}
        />
      ))}
    </div>
  );
};

const Home = () => {
  const [twitchData, setTwitchData] = useState(null);
  const [youtubeVideo, setYoutubeVideo] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [twitchRes, ytRes] = await Promise.all([
          checkChannelLive(),
          getLatestNonLiveVideo()
        ]);
        setTwitchData(twitchRes);
        setYoutubeVideo(ytRes);
      } catch (error) {
        console.error("Error fetching media:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  return (
    <div className="w-full">
      {/* 
        HERO SECTION 
        Dark Tactical Background + Abstract glow 
      */}
      <section className="relative flex flex-col items-center justify-start pt-24 pb-16 overflow-hidden bg-pitchDark bg-tactical-board bg-grid-pattern">
        
        <ParticlesBackground />
        
        {/* Glow Effects */}
        <div className="absolute top-1/4 left-1/4 w-[500px] h-[500px] bg-primary/20 rounded-full blur-[128px] pointer-events-none mix-blend-screen"></div>
        <div className="absolute bottom-1/4 right-1/4 w-[500px] h-[500px] bg-accent/15 rounded-full blur-[128px] pointer-events-none mix-blend-screen"></div>

        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full z-10 relative">
          
          {/* Texto Intermitente Horário de Lives */}
          <div className="flex justify-center mb-6 relative z-20">
            <motion.div 
               animate={{ opacity: [0.5, 1, 0.5] }}
               transition={{ duration: 2.5, repeat: Infinity, ease: "easeInOut" }}
               className="bg-black/50 border border-accent/20 text-accent px-6 py-1.5 rounded-full font-bold tracking-widest text-xs uppercase shadow-[0_0_20px_rgba(255,215,0,0.1)] backdrop-blur-md flex items-center gap-3"
            >
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-accent opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-accent text-white"></span>
              </span>
              Lives de Segunda à sexta a partir das 23hrs
            </motion.div>
          </div>

          <NewsCarousel />

          <div className="grid grid-cols-1 lg:grid-cols-12 gap-12 items-center">
            {/* Left Column: Título e Texto Principal */}
            <div className="lg:col-span-6 text-center lg:text-left space-y-6 relative">
              <div className="absolute -left-8 top-4 w-2 h-24 bg-gradient-to-b from-accent to-transparent hidden lg:block rounded-full"></div>
              <motion.div
                initial={{ y: 20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                transition={{ duration: 0.6 }}
              >
                <div className="inline-block px-4 py-1.5 rounded-full bg-accent/10 border border-accent/20 text-accent font-bold text-sm tracking-widest uppercase mb-6 shadow-[0_0_15px_rgba(255,215,0,0.1)]">
                  Conteúdo Premium de FM26
                </div>
                <h1 className="text-5xl md:text-6xl xl:text-7xl font-black text-white leading-none uppercase tracking-tighter drop-shadow-lg">
                  O Próximo Nível<br />
                  <span className="text-transparent bg-clip-text bg-gradient-to-r from-accent to-accentHover drop-shadow-md">Do Seu Save</span>
                </h1>
              </motion.div>
              
              <motion.p 
                className="text-lg md:text-xl text-gray-300 font-medium leading-relaxed drop-shadow-md"
                initial={{ y: 20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                transition={{ duration: 0.6, delay: 0.2 }}
              >
                Moneyball, vídeos para ensinar você a jogar e as melhores ferramentas e mods para <span className="text-white font-bold border-b-2 border-primary/50">Football Manager</span>.
              </motion.p>

              {/* Botões CTA Novos */}
              <motion.div 
                className="flex flex-col sm:flex-row items-center sm:items-start justify-center lg:justify-start gap-4 pt-4"
                initial={{ y: 20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                transition={{ duration: 0.6, delay: 0.4 }}
              >
                <Link to="/telecurso" className="w-full sm:w-auto px-8 py-3.5 bg-accent text-black font-black uppercase tracking-wide rounded-xl hover:bg-accentHover hover:scale-105 transition-all duration-300 shadow-[0_0_20px_rgba(255,215,0,0.3)] text-center">
                  Aprenda a JOGAR o FM26
                </Link>
                <a href="#comunidade" onClick={(e) => {
                  e.preventDefault();
                  document.getElementById('comunidade').scrollIntoView({ behavior: 'smooth' });
                }} className="w-full sm:w-auto px-8 py-3.5 bg-white/5 border border-white/10 text-white font-bold uppercase tracking-wide rounded-xl hover:bg-white/10 hover:border-white/30 transition-all duration-300 glass cursor-pointer text-center">
                  Comunidade Info
                </a>
              </motion.div>
            </div>

            {/* Right Column: Media Carousel Grid */}
            <div className="lg:col-span-6 flex items-stretch h-full">
               <MediaCarousel twitchData={twitchData} youtubeVideo={youtubeVideo} loading={loading} />
            </div>
          </div> {/* Closes lg:grid-cols-12 */}
        </div>

        {/* Scroll Indicator */}
        <motion.div 
          className="absolute bottom-4 left-1/2 -translate-x-1/2 z-20 flex flex-col items-center gap-2 text-gray-400 opacity-60 hidden md:flex"
          animate={{ y: [0, 8, 0] }}
          transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
        >
          <span className="text-[10px] font-bold tracking-widest uppercase">Rolar Para Baixo</span>
          <div className="w-5 h-8 border-2 border-gray-400 rounded-full flex justify-center p-1">
            <div className="w-1 h-2 bg-gray-400 rounded-full animate-bounce"></div>
          </div>
        </motion.div>
      </section>

      {/* Social Cards Area */}
      <section id="comunidade" className="py-24 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 border-t border-white/5 relative z-10 bg-bgDark">
        <div className="text-center mb-16">
          <h2 className="text-3xl md:text-5xl font-black text-white uppercase tracking-tighter mb-4">Comunidade Base</h2>
          <p className="text-gray-400">Junte-se a nós para a melhor experiência em Football Manager.</p>
        </div>

        <div className="flex flex-wrap justify-center gap-6">
          {/* Discord Card */}
          <a href="https://discord.gg/Z5XMk427vy" target="_blank" rel="noreferrer" className="glass-card p-6 flex flex-col items-center text-center hover:-translate-y-2 group flex-1 min-w-[220px] max-w-[280px]">
            <div className="w-16 h-16 rounded-full bg-[#5865F2]/20 flex items-center justify-center mb-4 group-hover:bg-[#5865F2] transition-colors">
              <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 127.14 96.36" className="fill-[#5865F2] group-hover:fill-white transition-colors">
                <path d="M107.7,8.07A105.15,105.15,0,0,0,81.47,0a72.06,72.06,0,0,0-3.36,6.83A97.68,97.68,0,0,0,49,6.83,72.37,72.37,0,0,0,45.64,0,105.89,105.89,0,0,0,19.39,8.09C2.79,32.65-1.71,56.6.54,80.21h0A105.73,105.73,0,0,0,32.71,96.36,77.7,77.7,0,0,0,39.6,85.25a68.42,68.42,0,0,1-10.85-5.18c.91-.66,1.8-1.34,2.66-2a75.57,75.57,0,0,0,64.32,0c.87.71,1.76,1.39,2.66,2a68.68,68.68,0,0,1-10.87,5.19,77.7,77.7,0,0,0,6.89,11.1,105.25,105.25,0,0,0,32.19-16.14h0C129.24,52.84,122.09,29.11,107.7,8.07ZM42.45,65.69C36.18,65.69,31,60,31,53s5-12.74,11.43-12.74S54,46,53.89,53,48.84,65.69,42.45,65.69Zm42.24,0C78.41,65.69,73.31,60,73.31,53s5-12.74,11.43-12.74S96.2,46,96.12,53,91.08,65.69,84.69,65.69Z" />
              </svg>
            </div>
            <h3 className="text-xl font-bold text-white mb-2">Sala do Manager</h3>
            <p className="text-gray-400 text-sm mb-6 flex-grow">O Discord da comunidade brasileira de FM</p>
            <span className="text-[#5865F2] font-semibold flex items-center gap-2 group-hover:underline">Entrar no Discord <ExternalLink size={14} /></span>
          </a>

          {/* Twitch Card */}
          <a href="https://twitch.tv/vinteset" target="_blank" rel="noreferrer" className="glass-card p-6 flex flex-col items-center text-center hover:-translate-y-2 group flex-1 min-w-[220px] max-w-[280px]">
            <div className="w-16 h-16 rounded-full bg-[#9146FF]/20 flex items-center justify-center mb-4 group-hover:bg-[#9146FF] transition-colors">
              <Twitch size={32} className="text-[#9146FF] group-hover:text-white transition-colors" />
            </div>
            <h3 className="text-xl font-bold text-white mb-2">Live de FM26</h3>
            <p className="text-gray-400 text-sm mb-6 flex-grow">Acompanhe as lives oficiais do canal.</p>
            <span className="text-[#9146FF] font-semibold flex items-center gap-2 group-hover:underline">Seguir na Twitch <ExternalLink size={14} /></span>
          </a>

          {/* YouTube Card */}
          <a href="https://youtube.com/@vintesetFM" target="_blank" rel="noreferrer" className="glass-card p-6 flex flex-col items-center text-center hover:-translate-y-2 group flex-1 min-w-[220px] max-w-[280px]">
            <div className="w-16 h-16 rounded-full bg-[#FF0000]/20 flex items-center justify-center mb-4 group-hover:bg-[#FF0000] transition-colors">
              <Youtube size={32} className="text-[#FF0000] group-hover:text-white transition-colors" />
            </div>
            <h3 className="text-xl font-bold text-white mb-2">Conteúdos sobre FM</h3>
            <p className="text-gray-400 text-sm mb-6 flex-grow">Tutoriais extensos, dicas e gameplays.</p>
            <span className="text-[#FF0000] font-semibold flex items-center gap-2 group-hover:underline">Inscrever-se <ExternalLink size={14} /></span>
          </a>

          {/* X Card */}
          <a href="https://x.com/vintesetFM" target="_blank" rel="noreferrer" className="glass-card p-6 flex flex-col items-center text-center hover:-translate-y-2 group flex-1 min-w-[220px] max-w-[280px]">
            <div className="w-16 h-16 rounded-full bg-white/10 flex items-center justify-center mb-4 group-hover:bg-white transition-colors">
              <span className="text-3xl font-black text-gray-300 group-hover:text-black transition-colors">X</span>
            </div>
            <h3 className="text-xl font-bold text-white mb-2">Atualizações Diárias</h3>
            <p className="text-gray-400 text-sm mb-6 flex-grow">Fique por dentro das novidades do FM26</p>
            <span className="text-gray-300 font-semibold flex items-center gap-2 group-hover:underline">Seguir no X <ExternalLink size={14} /></span>
          </a>

          {/* WhatsApp Card */}
          <a href="https://whatsapp.com/channel/0029Van5rk6IHphDZzEsNc30" target="_blank" rel="noreferrer" className="glass-card p-6 flex flex-col items-center text-center hover:-translate-y-2 group flex-1 min-w-[220px] max-w-[280px] border border-white/5 hover:border-[#25D366]/50">
            <div className="w-16 h-16 rounded-full bg-[#25D366]/20 flex items-center justify-center mb-4 group-hover:bg-[#25D366] transition-colors relative">
               <span className="absolute -top-1 -right-1 flex h-4 w-4">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-[#25D366] opacity-75"></span>
                <span className="relative inline-flex rounded-full h-4 w-4 bg-[#25D366]"></span>
              </span>
              <MessageCircle size={32} className="text-[#25D366] group-hover:text-white transition-colors" />
            </div>
            <h3 className="text-xl font-bold text-white mb-2">Canal do WhatsApp</h3>
            <p className="text-gray-400 text-sm mb-6 flex-grow">Canal dedicado para saber tudo sobre as lives e novidades do jogo</p>
            <span className="text-[#25D366] font-semibold flex items-center gap-2 group-hover:underline">Participar <ExternalLink size={14} /></span>
          </a>
        </div>
      </section>

    </div>
  );
};

export default Home;
