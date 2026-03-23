import React, { useState, useEffect, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { PlayCircle, X, Search, ExternalLink, Filter, Youtube } from 'lucide-react';

import indexData from '../data/index.json';

const getYTId = (url) => {
  if (!url) return null;
  const match = url.match(/^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|&v=)([^#&?]*).*/);
  return match && match[2].length === 11 ? match[2] : null;
};

const getCategory = (title) => {
  const norm = title.toLowerCase();
  // Regra pedida pelo usuário: Relacionismo é Tática.
  if (norm.includes('relacionismo')) return "Tática";
  if (norm.includes('guia') || norm.includes('tutorial') || norm.includes('dicas')) return "Tutorial";
  return "Análise Dinâmica";
};

const TELECURSO_VIDEOS = indexData
  .filter(item => item.tipo === 'video' && item.uploadedYoutubeUrl)
  .map(item => {
    const isOriginalYT = item.originalYoutubeUrl && (item.originalYoutubeUrl.includes('youtube') || item.originalYoutubeUrl.includes('youtu.be'));
    return {
      id: item.id.includes('/') ? item.id.split('/').pop() : item.id,
      title: item.titulo,
      description: "Resumo em vídeo gerado e narrado por IA focado nos pontos táticos do material base.",
      videoUrl: item.uploadedYoutubeUrl,
      thumbUrl: item.uploadedYoutubeUrl ? `https://i.ytimg.com/vi/${getYTId(item.uploadedYoutubeUrl)}/hqdefault.jpg` : "/fallback_thumb.png",
      creatorName: item.creatorName || "Comunidade",
      originalUrl: item.originalYoutubeUrl || "",
      isOriginalYT,
      category: getCategory(item.titulo)
    };
  });

const CATEGORIES = ["Todos", "Tática", "Moneyball", "Análise Dinâmica", "Tutorial"];

const Telecurso = () => {
  const [selectedVideo, setSelectedVideo] = useState(null);
  const [durations, setDurations] = useState({});
  const [searchTerm, setSearchTerm] = useState("");
  const [activeCategory, setActiveCategory] = useState("Todos");

  useEffect(() => {
    // Buscar dinamicamente as durações precisas dos vídeos a partir das URLs.
    TELECURSO_VIDEOS.forEach((video) => {
      if (!durations[video.id] && video.videoUrl) {
        const isUploadedYT = video.videoUrl.includes('youtube.com') || video.videoUrl.includes('youtu.be');
        const badgeLabel = video.isOriginalYT ? "Vídeo YT" : "Artigo/Site";
        
        if (isUploadedYT) {
          setDurations((prev) => ({
            ...prev,
            [video.id]: badgeLabel
          }));
          return;
        }

        const vid = document.createElement('video');
        vid.src = video.videoUrl;
        vid.onloadedmetadata = () => {
          const minutes = Math.floor(vid.duration / 60);
          const seconds = Math.floor(vid.duration % 60);
          setDurations((prev) => ({
            ...prev,
            [video.id]: `${minutes}:${seconds.toString().padStart(2, '0')}`
          }));
        };
      }
    });

    // Fecha modal ao teclar ESC
    const handleEsc = (e) => {
      if (e.key === 'Escape') setSelectedVideo(null);
    };
    window.addEventListener('keydown', handleEsc);
    return () => window.removeEventListener('keydown', handleEsc);
  }, []);

  const filteredVideos = useMemo(() => {
    return TELECURSO_VIDEOS.filter(video => {
      const matchCategory = activeCategory === "Todos" || video.category === activeCategory;
      const matchSearch = video.title.toLowerCase().includes(searchTerm.toLowerCase()) || 
                          video.creatorName.toLowerCase().includes(searchTerm.toLowerCase());
      return matchCategory && matchSearch;
    });
  }, [searchTerm, activeCategory]);

  return (
    <div className="w-full bg-bgDark min-h-screen text-white pt-24 pb-16">
      
      {/* Hero Section */}
      <section className="relative px-4 sm:px-6 lg:px-8 max-w-7xl mx-auto mb-16">
        <div className="absolute top-0 right-0 w-[600px] h-[600px] bg-accent/10 rounded-full blur-[120px] pointer-events-none mix-blend-screen"></div>
        <motion.div 
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6 }}
          className="relative z-10"
        >
          <h1 className="text-5xl md:text-7xl font-black uppercase tracking-tighter mb-8 leading-tight">
            Telecurso <span className="text-transparent bg-clip-text bg-gradient-to-r from-accent to-accentHover">27 FM</span>
          </h1>
          
          <div className="bg-gradient-to-tr from-white/5 to-white/10 border border-white/10 p-6 md:p-10 rounded-2xl mb-12 backdrop-blur-sm max-w-4xl shadow-2xl relative overflow-hidden">
             <div className="absolute top-0 left-0 w-3 h-full bg-accent"></div>
             <div className="pl-4">
                 <p className="text-gray-300 text-lg md:text-xl leading-relaxed mb-6 font-medium">
                   O <strong className="text-white">Objetivo do Telecurso 27</strong> é quebrar a barreira do idioma e trazer em português-br os resumos originais, traduzidos e dissecados em masterclasses, criados brilhantemente pelos produtores de conteúdo de Football Manager de todo o mundo. 
                 </p>
                 <p className="text-accent text-xl md:text-2xl font-black italic tracking-wide">
                   "Muito obrigado a todos os criadores originais por compartilhar esse conhecimento de ponta.<br />Se não fosse por vocês, não teríamos o Telecurso 27!"
                 </p>
             </div>
          </div>
        </motion.div>
      </section>

      {/* Grid de Vídeos e Filtros */}
      <section className="px-4 sm:px-6 lg:px-8 max-w-7xl mx-auto relative z-10">
        
        {/* Painel de Busca e Filtros */}
        <div className="flex flex-col md:flex-row items-center justify-between gap-4 mb-8 bg-black/30 p-4 rounded-2xl border border-white/5 backdrop-blur-sm">
           
           {/* Busca */}
           <div className="relative w-full md:w-96">
             <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
               <Search size={18} className="text-gray-400" />
             </div>
             <input
               type="text"
               className="w-full bg-black/50 border border-white/10 rounded-xl py-2.5 pl-10 pr-4 text-sm text-white placeholder-gray-500 focus:outline-none focus:border-accent/50 focus:ring-1 focus:ring-accent/50 transition-all"
               placeholder="Buscar por aulas ou criadores..."
               value={searchTerm}
               onChange={(e) => setSearchTerm(e.target.value)}
             />
           </div>

           {/* Filtros em Pílulas */}
           <div className="flex flex-wrap items-center gap-2 w-full md:w-auto overflow-x-auto pb-2 md:pb-0 hide-scrollbar">
             <Filter size={18} className="text-gray-400 mr-2 md:hidden" />
             {CATEGORIES.map(category => (
               <button
                 key={category}
                 onClick={() => setActiveCategory(category)}
                 className={`px-4 py-2 rounded-full text-xs font-bold uppercase tracking-wider whitespace-nowrap transition-all duration-300 ${
                   activeCategory === category 
                     ? 'bg-accent text-black shadow-[0_0_15px_rgba(255,215,0,0.3)]' 
                     : 'bg-white/5 text-gray-400 hover:bg-white/10 hover:text-white'
                 }`}
               >
                 {category}
               </button>
             ))}
           </div>
        </div>

        <h2 className="text-2xl font-black uppercase tracking-wide mb-8 border-l-4 border-accent pl-4 flex items-center gap-2">
          Catálogo <span className="text-accent text-lg">({filteredVideos.length})</span>
        </h2>
        
        {filteredVideos.length > 0 ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            {filteredVideos.map((video, index) => (
              <motion.div
                key={video.id}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.4, delay: index * 0.05 }}
                onClick={() => setSelectedVideo(video)}
                className="group cursor-pointer flex flex-col bg-white/5 rounded-2xl overflow-hidden border border-white/5 hover:border-accent/30 hover:bg-white/10 transition-all duration-300"
              >
                <div 
                  className="relative w-full aspect-video bg-black/60 overflow-hidden bg-cover bg-center border-b border-white/5"
                  style={video.thumbUrl ? { backgroundImage: `url(${video.thumbUrl})` } : {}}
                >
                   <div className="absolute inset-0 opacity-40 mix-blend-overlay bg-gradient-to-tr from-black to-gray-800"></div>
                   <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center backdrop-blur-sm">
                      <PlayCircle size={56} className="text-accent drop-shadow-lg transform group-hover:scale-110 transition-transform duration-300" />
                   </div>
                   {durations[video.id] && (
                     <div className="absolute bottom-3 right-3 bg-black/90 px-2 py-1 rounded text-xs font-bold font-mono tracking-widest shadow-lg">
                       {durations[video.id]}
                     </div>
                   )}
                   <div className="absolute top-3 left-3 bg-accent text-black px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-widest shadow-lg">
                     {video.category}
                   </div>
                </div>
                
                <div className="p-4 flex-grow flex flex-col justify-between">
                  <h3 className="text-sm font-bold leading-relaxed group-hover:text-accent transition-colors line-clamp-2 mb-3">
                    {video.title}
                  </h3>
                  
                  <div className="flex items-center justify-between text-xs text-gray-400 mt-auto pt-3 border-t border-white/5">
                    <span className="flex items-center gap-1.5 truncate pr-2">
                      <span className="w-4 h-4 rounded-full bg-white/10 flex items-center justify-center flex-shrink-0">
                        {video.isOriginalYT ? '📺' : '🌐'}
                      </span>
                      <span className="truncate group-hover:text-gray-300 transition-colors">{video.creatorName}</span>
                    </span>
                  </div>
                </div>
              </motion.div>
            ))}
          </div>
        ) : (
          <div className="w-full py-20 flex flex-col items-center justify-center text-center bg-white/5 rounded-2xl border border-white/5 border-dashed">
             <Search size={48} className="text-gray-600 mb-4" />
             <h3 className="text-xl font-bold text-gray-300 mb-2">Nenhum resultado encontrado</h3>
             <p className="text-gray-500">Tente buscar por termos diferentes ou remova os filtros.</p>
             <button 
               onClick={() => { setSearchTerm(""); setActiveCategory("Todos"); }}
               className="mt-6 px-6 py-2 bg-white/10 hover:bg-accent hover:text-black rounded-xl font-bold transition-colors text-sm"
             >
               Limpar Filtros
             </button>
          </div>
        )}
      </section>

      {/* Modal HTML5 Native Video Player */}
      <AnimatePresence>
        {selectedVideo && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={() => setSelectedVideo(null)}
            className="fixed inset-0 z-50 flex items-center justify-center bg-black/95 p-4 backdrop-blur-xl"
          >
            <motion.div
              initial={{ scale: 0.95, opacity: 0, y: 20 }}
              animate={{ scale: 1, opacity: 1, y: 0 }}
              exit={{ scale: 0.95, opacity: 0, y: 20 }}
              transition={{ type: "spring", damping: 25, stiffness: 300 }}
              onClick={(e) => e.stopPropagation()}
              className="relative w-full max-w-6xl bg-bgDark rounded-2xl overflow-hidden border border-white/10 shadow-2xl flex flex-col lg:flex-row"
            >
              
              {/* Esquerda: Player de Vídeo (Ocupará maior parte) */}
              <div className="w-full lg:w-3/4 aspect-video bg-black flex items-center justify-center relative">
                 <button 
                   onClick={() => setSelectedVideo(null)}
                   className="absolute top-4 left-4 z-20 p-2 bg-black/50 hover:bg-accent hover:text-black rounded-full transition-all text-white backdrop-blur-md lg:hidden"
                 >
                   <X size={20} />
                 </button>
                 
                 {selectedVideo.videoUrl && (selectedVideo.videoUrl.includes('youtube.com') || selectedVideo.videoUrl.includes('youtu.be')) ? (
                   <div className="w-full h-full relative overflow-hidden bg-black flex items-center justify-center">
                     {/* Máscara superior para ocultar nome do canal e título no YouTube */}
                     <div className="absolute top-0 left-0 w-full h-[70px] bg-black z-10 pointer-events-none"></div>
                     {/* Máscara inferior para ocultar logo do YouTube */}
                     <div className="absolute bottom-0 right-0 w-[120px] h-[60px] bg-black z-10 pointer-events-none"></div>
                     <iframe
                       className="w-full h-full object-cover focus:outline-none"
                       src={`https://www.youtube.com/embed/${getYTId(selectedVideo.videoUrl)}?autoplay=1&rel=0&modestbranding=1&controls=1`}
                       title={selectedVideo.title}
                       frameBorder="0"
                       allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                       allowFullScreen
                     ></iframe>
                   </div>
                 ) : (
                   <video 
                     src={selectedVideo.videoUrl} 
                     controls 
                     autoPlay 
                     preload="auto"
                     playsInline
                     poster={selectedVideo.thumbUrl}
                     className="w-full h-full object-contain focus:outline-none"
                   >
                      Seu navegador não suporta o formato de vídeo.
                   </video>
                 )}
              </div>

              {/* Direita: Info, Créditos e Referências */}
              <div className="w-full lg:w-1/4 bg-gray-900 border-l border-white/10 p-6 flex flex-col justify-between">
                <div>
                  <div className="flex justify-between items-start mb-6">
                    <span className="px-2.5 py-1 bg-white/5 border border-white/10 rounded-md text-[10px] font-bold uppercase tracking-widest text-accent">
                      {selectedVideo.category}
                    </span>
                    <button 
                      onClick={() => setSelectedVideo(null)}
                      className="p-1.5 bg-white/5 hover:bg-white/20 rounded-full transition-all hidden lg:block"
                    >
                      <X size={20} />
                    </button>
                  </div>

                  <h3 className="font-black text-xl lg:text-2xl leading-tight mb-4 pr-4">
                    {selectedVideo.title}
                  </h3>
                  
                  <div className="w-12 h-1 bg-accent/50 rounded-full mb-6"></div>

                  <p className="text-gray-400 text-sm leading-relaxed mb-6">
                    Mergulhe nesta masterclass gerada a partir das anotações e sumários guiados da nossa comunidade.
                    {selectedVideo.description && (
                      <span className="block mt-4 text-white/80">{selectedVideo.description}</span>
                    )}
                  </p>
                </div>

                <div className="bg-black/50 p-6 rounded-xl border border-white/5 mt-auto flex flex-col gap-4">
                  <div>
                    <p className="text-xs uppercase tracking-widest text-gray-500 font-bold mb-1">Criador da Fonte Base</p>
                    <span className="font-bold text-lg text-white">{selectedVideo.creatorName}</span>
                  </div>
                  
                  {selectedVideo.originalUrl && (
                    <a 
                      href={selectedVideo.originalUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="flex items-center justify-center gap-2 w-full text-center py-3 text-sm font-bold bg-white/10 hover:bg-white/20 rounded-lg transition-colors border border-white/10 hover:border-white/30"
                    >
                      {selectedVideo.isOriginalYT ? <Youtube size={18} className="text-red-500" /> : <ExternalLink size={18} className="text-accent" />}
                      {selectedVideo.isOriginalYT ? "Assistir Vídeo Original" : "Ler Artigo Original"}
                    </a>
                  )}
                </div>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

    </div>
  );
};

export default Telecurso;
