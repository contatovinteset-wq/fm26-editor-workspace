import React, { useState, useEffect, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { PlayCircle, X, Search, ExternalLink, Filter } from 'lucide-react';

const TELECURSO_VIDEOS = [
  { 
    id: "8473c28a-90f2-44a4-b59b-6987d17b7d9f", 
    title: "O Novo Raumdeuter: Recriando a Função no FM26", 
    videoUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAyAbduRAN1xC4_RqnpUjmIOtjVbIIuX-Yt4bE9KiFIvlzORWYwU1BzeLw2kzRPM5gQ2fwGAAiFt0pYyXCdbr9yWfXyKp4JoD5vJmKHv8tV8ovIhZnS5htdN9ba2-PIu5vlYGbN6DQyOblbxyXYHmN385fc46H8=m22-dv", 
    thumbUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAwTOuaBuT2xYKOxNZzADvivv1HF2yWxPNsqLFXZWoULO4VbbBTghwqBy9w68v-NC4P7FOwBiXWV9WazMotDT1GKdLN4Yu2QiK3GTEWvyDGjM2l1fvQubHTwc3lJ5UfDUJKpibfh2t-jKwtkn-3yaE2b6gI7UQ=w2752-d-h1536-mp2",
    creatorName: "Mustermann",
    creatorLink: "https://youtube.com/@mustermannicon",
    category: "Tática"
  },
  { 
    id: "44f76157-2574-4670-9f09-044c6e0221fc", 
    title: "Mustermann Moneyballl FM26", 
    videoUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAzTRTyStPRxQ3C-zitpsM3-5ilPrwjaS06-myKLcO7ygeN8FBpRsE14m9sURojvWJj32u49asX3DhqpBlW132lB9aaV3v2WpIsHw_6JgSl8o4eyWz9GPJE9Otfxmj02vcjrM25dcv1PUJ1Y2mkMA0o9f_ajog=m22-dv", 
    thumbUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAz044NGGnPQbljjJh-zQ5W88tjVna5tykim5GoQlP3KHR1CajfXV9rwbl7g8Dq18MrBG8-mvUo7kwVfyDmzopl5AzUZz7-ZgOaCOIvClV4Jq0ZNxy21Ou0cRysndUjsfuc2GC0b4yiGsvnGreNa9I1QJt19poM=w2752-d-h1536-mp2",
    creatorName: "Mustermann",
    creatorLink: "https://youtube.com/@mustermannfm",
    category: "Moneyball"
  },
  { 
    id: "41ae276f-24e4-4e3c-b6cb-2345f06a06c8", 
    title: "A Anatomia da Tática: Como Avaliar sua Estratégia no FM26", 
    videoUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAwAcAoVzoj68AYrcnv2CvXi3fM4kavXQ5Ep3OM9vHPA751KNGoiSJyZJ_CWgwi0Sykny4fnMH7-x7f6W8ieaaD_XHjKKjfS6W1JDfCyt7C8dq--6lSQcz_bqIGU8lUh52FH2ZLeixNaf5bNK4IXY2j_pTmUdT0=m22-dv", 
    thumbUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAxpydFnxHFy_lh7I3k2uUpId91cDUmtlIyD0rTFbjkyEtmPFHbiXGIyrPxygOIya8KBCm6Anp1Wy3g4iyE3xbE3wR1veOQMkrOxNpZWI0UNOHoMr3uw5DaSv3PyIHvjDE5g-a45r9HH6XkA7VoJuL6fEnV1yIk=w2752-d-h1536-mp2",
    creatorName: "Omega Luke",
    creatorLink: "https://youtube.com/@omegaluke",
    category: "Tática"
  },
  { 
    id: "fd11fce7-ab05-42d8-9e44-39cf632fb631", 
    title: "O Fator Controle: O Moneyball da Juventus no FM26", 
    videoUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAwYiR3pHvMWydc87ymduUmBj4R0lbaqEYGo_I1-mw5yWB6BbRa58HsUenx1m7q1RPneDwRkIolY9LxjvkdNUUNYBIRMrk3RIvStkpmJg5Kq5WmCAJ1z87n1Y_AGv1h9OmlvDmevtAIk3P0ojXlnvpyi05ICww=m22-dv", 
    thumbUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAzbtnin4A4I1IK-gCUM_K-QtHlUA-5bHpUUn7oGCxGMT8Poo1TxMrN6YH3qTxJXUZtj_8KwIAnlod8RQSVHcZnkhIzaDSz7K3FRuVWfo4orxbva_ISsf0wC5S9ldAunC4uKIiJPeT4AeWCfMsUtVTR5bD8sjrM=w2752-d-h1536-mp2",
    creatorName: "RDF Tactics",
    creatorLink: "https://youtube.com/@rdftactics",
    category: "Moneyball"
  },
  { 
    id: "f5d83579-413a-4717-a117-b6ce6d749a36", 
    title: "O Dilema Tático: Identidade versus Elenco no Manchester United", 
    videoUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAw1jb51UoPfBwrqnyTMxQcQby_SauCu94_vTX7xTPaat_ZGrHaNj5VDkEkkk5i47C4eSoHHfY5tz8CtzVjhKTrzWx6rw5EhAjV5ozBEoHJZy_6z_AOcmnjzhpyHZongwUxsUS4yPO9fGH9G8LOLfgwv5Lk86iE=m22-dv", 
    thumbUrl: "/fallback_thumb.png",
    creatorName: "Zealand",
    creatorLink: "https://youtube.com/@zealand",
    category: "Análise Dinâmica"
  },
  { 
    id: "fe42b598-dd30-448f-ab71-01487e55d734", 
    title: "Estratégias de Pressão em Bloco Médio no Football Manager", 
    videoUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAy5liYhisUoF5IdlBn6TVxL_QdaIJpeKai3XftDu34MZt5o2u2xPbq1McIiBdB_6J_01XErptosWiQSlslltL0O_ky_axPEKhAUSp5LsyoTw3jsfeVRJD_fzRdD5ufZsKN19hK2D7cxg40gi1K6spBnB68-pKo=m22-dv", 
    thumbUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAzslroxrWth0laoRoWvGGIoTRQLfKNWLBWdcY4lck80xczFTokW7t-gGMDMlUa0SNnnRnH_7w0_VPwHzCKpcftIWq1DywswRzkQeoY55mSZnR5ajypzrQGF_bZFwenKR2ZXfBNySYPcm38cH7gevlH5yIGRALo=w2752-d-h1536-mp2",
    creatorName: "FM Scout",
    creatorLink: "https://youtube.com/@fmscout",
    category: "Tática"
  },
  { 
    id: "306c55ba-b77f-460e-9997-6354635b4f49", 
    title: "Dominando a Função Free Role no FM26", 
    videoUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAyEilk_PdDiWoVJKkmaR-xnIg-IzXZEbfNkNil7DqABJYNDyH-TcmyU9r9uudFWjW--wRjm_X1hL7Z9W0uzB3ZdbXfwJcjzuCKn3knq4QsALyt3BABjlbL3V_tuwDjTfrEl_bfY62PdSPYc_NqGXYgPLJs5jg=m22-dv", 
    thumbUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAwwGrnyWYBBT6DwcqNcF3ontgfBXGOY0nt4yn0C8H2IMbQa9SXMUw2ReADPPXp1H2lVZKMoUVeyer7tHiQmk2AgFn5nHsxGzTk4f_-d332YTNH_4yZ9xGosdlRFQePck6Ugx_uN3tdv5ajVxefF-6tNDwqqiw=w1536-d-h2752-mp2",
    creatorName: "BustTheNet",
    creatorLink: "https://youtube.com/@bustthenet",
    category: "Tática"
  },
  { 
    id: "6c8d831a-184b-40ac-8d99-e92f01d4563b", 
    title: "Estratégias de Espaçamento e Táticas 4-3-3 no FM26", 
    videoUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAweFod7c7f_mdRHFeVxF_az9_h3ezXH5T9NKB-UViueve1m9dBK7KCsNppUiozPQaWI_GK2-2YO6sq634gdMsqWSIax9zUK252tuAnQYDJrk1g6aO9ZwlLLOa0T_JX85Mtre_hcrQCJZUB9BEmuMZMyh0bpYqE=m22-dv", 
    thumbUrl: "/fallback_thumb.png",
    creatorName: "FM Scout",
    creatorLink: "https://youtube.com/@fmscout",
    category: "Tática"
  },
  { 
    id: "6721fefc-90b3-4432-a05c-0237e594f9d1", 
    title: "Guia do Avançado Ataca-Espaços no FM26", 
    videoUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAxO6WcL8L7Gc4raIS61205nPdJe_gONjejPm6_slgiFsp6K5USpVHuU3DHsBS_BF7zIaAVvS7l9GRLfuxFhIDkxO8qTPrpJbofSSV5XHVSVQeKvukavMegS_SYMiecsYVyg-9sYiz-jj_rdiUpcHp7vgp9tb0s=m22-dv", 
    thumbUrl: "/fallback_thumb.png",
    creatorName: "RDF Tactics",
    creatorLink: "https://youtube.com/@rdftactics",
    category: "Tática"
  },
  { 
    id: "405b2c4f-6ffd-47b8-86d2-e72d997bbeeb", 
    title: "Muralha de Anfield: Estruturas Defensivas no FM26", 
    videoUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAzl1UtLm81C-VTm9ieNh2jTakSLCuvx1N72-yprfUOlqxdafkI61ieVuhug2OUU1_ltWgtx3TtrH-JsVh7A_AqSjOShu_yOe_5MCf4y81n6CJKDV7wOd9c60aw9Zzh27FmERHSK71uSsRK8s-EEMiiqp4m7gPs=m22-dv", 
    thumbUrl: "/fallback_thumb.png",
    creatorName: "Zealand",
    creatorLink: "https://youtube.com/@zealand",
    category: "Tática"
  },
  { 
    id: "55099437-a4f1-459e-90b8-a9c58fd63fc8", 
    title: "O Guia do Meio-Campista Box-to-Box no FM26", 
    videoUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAxjZ9QxU2BdEkWACjm2nHc009lqkhR91D1hR3TzAJKP9vqCBfV7mkRPFO8T2BUPab9cMqwu7kqfuUfNw0x3JVhvJud39y6glzRbyXlI6o1VOI0My-WPakvvdm2vH8n-yv5JaI7-P9wGB_U8tfzCR1k_bISEEw=m22-dv", 
    thumbUrl: "/fallback_thumb.png",
    creatorName: "BustTheNet",
    creatorLink: "https://youtube.com/@bustthenet",
    category: "Tática"
  },
  { 
    id: "e4e71530-a4d2-411e-b9a3-b3d7bdf3074c", 
    title: "Guia de Bolas Paradas para Football Manager 2026", 
    videoUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAyPGOtQgzTjTIq7nu5ZEkuS79Rots7mrUMsvW2XASsja1HoLhHT5aFibVB8u6I68SXA_pvMn7rszI2ey8fTe97-GAMF-8HocJXOOj3zrRZsgzUbDQHPrwiDVRJylzUNOagC2A2U-2_i0THohlPPLEJP_ccpRg=m22-dv", 
    thumbUrl: "https://lh3.googleusercontent.com/notebooklm/ANHLwAwB7bURTohnYDS7Fc-CLKQz8EUcG1ObrDS09oJbrrr8y9d2Owi1usyrVMyViXL1Q1zOzvyhVyj4ndJ1ZV75nrbmEWMtbHaJQxpse3lfDM1w5lpSe_ZoacIxY2InG7awL1-4lHcip78ZknWYZ05LVOjbeZ_HSSQ=w1536-d-h2752-mp2",
    creatorName: "FM Scout",
    creatorLink: "https://youtube.com/@fmscout",
    category: "Tática"
  },
];

const CATEGORIES = ["Todos", "Tática", "Moneyball", "Análise Dinâmica"];

const Telecurso = () => {
  const [selectedVideo, setSelectedVideo] = useState(null);
  const [durations, setDurations] = useState({});
  const [searchTerm, setSearchTerm] = useState("");
  const [activeCategory, setActiveCategory] = useState("Todos");

  useEffect(() => {
    // Buscar dinamicamente as durações precisas dos vídeos a partir das URLs.
    TELECURSO_VIDEOS.forEach((video) => {
      if (!durations[video.id] && video.videoUrl) {
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
                        📺
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
                    Esta masterclass foi traduzida e sumarizada (NotebookLM Overview) a partir do conteúdo original criado pela comunidade de FM.
                  </p>
                </div>

                <div className="bg-black/50 p-4 rounded-xl border border-white/5 mt-auto">
                  <p className="text-xs uppercase tracking-widest text-gray-500 font-bold mb-2">Criador Original</p>
                  <div className="flex items-center justify-between">
                    <span className="font-bold">{selectedVideo.creatorName}</span>
                    <a 
                      href={selectedVideo.creatorLink} 
                      target="_blank" 
                      rel="noreferrer"
                      className="text-accent hover:text-white transition-colors"
                      title={`Acessar canal de ${selectedVideo.creatorName}`}
                    >
                      <ExternalLink size={18} />
                    </a>
                  </div>
                  <a 
                    href={selectedVideo.creatorLink}
                    target="_blank"
                    rel="noreferrer"
                    className="block w-full text-center py-2.5 mt-4 text-sm font-bold bg-white/5 hover:bg-white/10 rounded-lg transition-colors border border-white/5 hover:border-white/20"
                  >
                    Assistir Material Completo
                  </a>
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
