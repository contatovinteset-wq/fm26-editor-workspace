import React, { useState, useEffect, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Twitch, Youtube, PlayCircle, CalendarDays, ExternalLink } from 'lucide-react';

export const MediaCarousel = ({ twitchData, youtubeVideo, loading }) => {
  const [activeIndex, setActiveIndex] = useState(0); // 0 = Twitch, 1 = Youtube
  const [isHovered, setIsHovered] = useState(false);
  const [isPlayingYoutube, setIsPlayingYoutube] = useState(false);
  const [isPlayingTwitch, setIsPlayingTwitch] = useState(false);
  const carouselRef = useRef(null);

  // Auto-play Slider (Ignorar auto-play se o mouse estiver em cima ou Twitch for Live)
  useEffect(() => {
    // Se a twitch está Live, pausamos eternamente no index 0 a não ser que o usuário clique no slider 1
    // Também pausar auto-play se o usuário estiver assistindo o iframe manualmente
    if ((twitchData?.isLive && activeIndex === 0) || isPlayingYoutube || isPlayingTwitch) return;
    
    // Se hover, pausa a rotação para os vídeos
    if (isHovered) return;

    const timer = setInterval(() => {
      setActiveIndex((prev) => (prev === 0 ? 1 : 0));
    }, 8000); // 8 segundos cada

    return () => clearInterval(timer);
  }, [activeIndex, isHovered, twitchData?.isLive]);

  if (loading) {
    return (
      <div className="glass-card w-full h-[400px] flex items-center justify-center animate-pulse border-white/5 shadow-2xl">
        <div className="flex flex-col items-center gap-4 text-primary/50">
          <Twitch className="animate-bounce" size={48} />
          <p className="tracking-widest text-xs font-bold uppercase">Carregando Integrações...</p>
        </div>
      </div>
    );
  }

  // Se Twitch estiver Live, queremos embutir o Iframe do Iframe Oficial
  // e auto-passar um pause-on-hover generalizado no carrossel.
  const isTwitchLive = twitchData?.isLive;

  return (
    <div 
      className="w-full relative shadow-[0_0_30px_rgba(0,0,0,0.5)] rounded-2xl overflow-hidden glass-card border border-white/10"
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      ref={carouselRef}
    >
      
      {/* Abas Superior Estilo Twitch */}
      <div className="flex items-center w-full border-b border-white/10 bg-black/40">
        <button 
          onClick={() => { setActiveIndex(0); setIsPlayingYoutube(false); }}
          className={`flex-1 py-3 px-4 flex items-center justify-center gap-2 font-bold text-sm tracking-wide uppercase transition-colors ${activeIndex === 0 ? 'text-[#9146FF] bg-[#9146FF]/10 border-b-2 border-[#9146FF]' : 'text-gray-500 hover:text-gray-300'}`}
        >
          {isTwitchLive ? (
            <span className="relative flex h-2 w-2 mr-1">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"></span>
              <span className="relative inline-flex rounded-full h-2 w-2 bg-red-500"></span>
            </span>
          ) : (
            <Twitch size={16} />
          )}
          {isTwitchLive ? 'Ao Vivo Agora' : 'Última Live'}
        </button>
        <button 
          onClick={() => { setActiveIndex(1); setIsPlayingTwitch(false); }}
          className={`flex-1 py-3 px-4 flex items-center justify-center gap-2 font-bold text-sm tracking-wide uppercase transition-colors ${activeIndex === 1 ? 'text-[#FF0000] bg-[#FF0000]/10 border-b-2 border-[#FF0000]' : 'text-gray-500 hover:text-gray-300'}`}
        >
          <Youtube size={16} /> Último Vídeo
        </button>
      </div>

      <div className="relative h-[350px] w-full bg-black overflow-hidden">
        <AnimatePresence mode="popLayout" initial={false}>
          
          {/* SLIDE 0: TWITCH */}
          {activeIndex === 0 && (
            <motion.div
              key="twitch-slide"
              initial={{ x: -300, opacity: 0 }}
              animate={{ x: 0, opacity: 1 }}
              exit={{ x: 300, opacity: 0 }}
              transition={{ type: "spring", stiffness: 300, damping: 30 }}
              className="absolute inset-0 w-full h-full flex flex-col"
            >
              {isTwitchLive ? (
                // IFRAME LIVE EMBED - AUTO PLAY WITH MUTE
                <div className="w-full h-full relative bg-bgDark">
                   <iframe
                      src={`https://player.twitch.tv/?channel=vinteset&parent=localhost&parent=vintesetfm.com.br&parent=www.vintesetfm.com.br&parent=vintesetfm.cloud&autoplay=true&muted=true`}
                      className="absolute inset-0 w-full h-full border-none"
                      allowFullScreen
                    ></iframe>
                </div>
              ) : isPlayingTwitch ? (
                 <div className="w-full h-full relative bg-bgDark">
                   <iframe
                      src={`https://player.twitch.tv/?video=${twitchData?.lastVod?.id}&parent=localhost&parent=vintesetfm.com.br&parent=www.vintesetfm.com.br&parent=vintesetfm.cloud&autoplay=true`}
                      className="absolute inset-0 w-full h-full border-none"
                      allowFullScreen
                    ></iframe>
                 </div>
              ) : (
                // VOD MODO
                <div onClick={() => setIsPlayingTwitch(true)} className="block w-full h-full relative group cursor-pointer">
                  <div className="absolute inset-0 z-10 bg-gradient-to-t from-bgDark via-bgDark/40 to-transparent pointer-events-none"></div>
                  <img 
                    src={twitchData?.lastVod?.thumbnail_url ? twitchData.lastVod.thumbnail_url.replace('%{width}', '1920').replace('%{height}', '1080') : "https://images.unsplash.com/photo-1542751371-adc38448a05e?q=80&w=2070"} 
                    alt="Twitch VOD"
                    className="w-full h-full object-cover filter brightness-75 group-hover:scale-105 transition-transform duration-700"
                  />
                  
                  {/* Tempo */}
                  <div className="absolute top-4 right-4 z-20 bg-black/80 backdrop-blur-md px-2 py-1 rounded border border-white/10 text-xs font-mono text-white pointer-events-none">
                    {twitchData?.lastVod?.duration || "Recente"}
                  </div>

                  <div className="absolute inset-0 z-20 flex flex-col justify-end p-6 pointer-events-none">
                    <h3 className="text-white font-bold text-2xl leading-none mb-2 drop-shadow-lg group-hover:text-[#9146FF] transition-colors">
                      {twitchData?.lastVod?.title || "Stream Offline"}
                    </h3>
                    <p className="text-gray-300 text-sm flex items-center gap-2">
                      <CalendarDays size={14} /> 
                      {new Date(twitchData?.lastVod?.created_at || new Date()).toLocaleDateString('pt-BR')}
                    </p>
                  </div>

                  <div className="absolute inset-0 z-20 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none">
                    <div className="bg-[#9146FF] text-white p-4 rounded-full shadow-[0_0_30px_rgba(145,70,255,0.6)] transform scale-90 group-hover:scale-100 transition-all">
                      <PlayCircle size={40} />
                    </div>
                  </div>
                </div>
              )}
            </motion.div>
          )}

          {/* SLIDE 1: YOUTUBE */}
          {activeIndex === 1 && (
            <motion.div
              key="youtube-slide"
              initial={{ x: 300, opacity: 0 }}
              animate={{ x: 0, opacity: 1 }}
              exit={{ x: -300, opacity: 0 }}
              transition={{ type: "spring", stiffness: 300, damping: 30 }}
              className="absolute inset-0 w-full h-full"
            >
              {isPlayingYoutube ? (
                <div className="w-full h-full relative bg-bgDark">
                   <iframe
                      src={`https://www.youtube.com/embed/${youtubeVideo?.id}?autoplay=1&origin=https://vintesetfm.cloud`}
                      className="absolute inset-0 w-full h-full border-none"
                      allowFullScreen
                      referrerPolicy="strict-origin-when-cross-origin"
                    ></iframe>
                </div>
              ) : (
                <div onClick={() => setIsPlayingYoutube(true)} className="block w-full h-full relative group cursor-pointer">
                  <div className="absolute inset-0 z-10 bg-gradient-to-t from-bgDark via-bgDark/40 to-transparent pointer-events-none"></div>
                  <img 
                    src={youtubeVideo?.thumbnail || "https://images.unsplash.com/photo-1542751371-adc38448a05e?q=80&w=2070"} 
                    alt="Youtube Thumbnail"
                    className="w-full h-full object-cover filter brightness-75 group-hover:scale-105 transition-transform duration-700"
                  />
                  
                  {/* Tempo */}
                  <div className="absolute top-4 right-4 z-20 bg-[#FF0000]/90 px-2 py-1 rounded text-[10px] font-mono text-white font-bold shadow-md pointer-events-none">
                     {youtubeVideo?.duration || "NOVO"}
                  </div>

                  <div className="absolute inset-0 z-20 flex flex-col justify-end p-6 pointer-events-none">
                    <h3 className="text-white font-bold text-2xl leading-none mb-2 drop-shadow-lg group-hover:text-[#FF0000] transition-colors">
                      {youtubeVideo?.title || "Carregando..."}
                    </h3>
                    <p className="text-gray-300 text-sm flex items-center gap-2">
                      <CalendarDays size={14} /> 
                      {youtubeVideo?.publishedAt ? new Date(youtubeVideo.publishedAt).toLocaleDateString('pt-BR') : "Hoje"}
                    </p>
                  </div>

                   <div className="absolute inset-0 z-20 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none">
                      <div className="bg-[#FF0000] text-white p-4 rounded-full shadow-[0_0_30px_rgba(255,0,0,0.6)] transform scale-90 group-hover:scale-100 transition-all">
                        <PlayCircle size={40} />
                      </div>
                    </div>
                </div>
              )}
            </motion.div>
          )}

        </AnimatePresence>
      </div>
    </div>
  );
};
