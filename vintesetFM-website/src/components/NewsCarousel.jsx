import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ArrowRight, Sparkles } from 'lucide-react';
import { Link } from 'react-router-dom';

import indexData from '../data/index.json';

const getYTId = (url) => {
  if (!url) return null;
  const match = url.match(/^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|&v=)([^#&?]*).*/);
  return match && match[2].length === 11 ? match[2] : null;
};

const defaultNews = [
  {
    id: 2,
    tag: "TELECURSO",
    title: "Aprenda a jogar FM26 de um jeito fácil",
    description: "Assista às nossas masterclasses em vídeo geradas pela comunidade e domine as táticas do jogo.",
    link: "/telecurso",
    image: "mosaic",
    isExternal: false
  }
];

export const NewsCarousel = () => {
  const newsItems = defaultNews;
  const [currentIndex, setCurrentIndex] = useState(0);

  useEffect(() => {
    if (newsItems.length <= 1) return;
    const timer = setInterval(() => {
      setCurrentIndex((prev) => (prev + 1) % newsItems.length);
    }, 6000);
    return () => clearInterval(timer);
  }, []);

  // Pegar os 8 primeiros videos para o mosaico
  const mosaicVideos = indexData.filter(i => i.tipo === 'video' && i.uploadedYoutubeUrl).slice(0, 8);

  return (
    <div className="w-full mb-12 relative rounded-2xl overflow-hidden shadow-[0_0_40px_rgba(255,215,0,0.1)] border border-white/10 glass-card">

      <div className="relative h-[250px] sm:h-[300px] w-full bg-black">
        <AnimatePresence mode="wait">
          <motion.div
            key={currentIndex}
            initial={{ opacity: 0, scale: 1.05 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.8 }}
            className="absolute inset-0"
          >
            {/* Imagem de Fundo com Gradient Shadow */}
            <div className="absolute inset-0 z-10 bg-gradient-to-t from-bgDark via-bgDark/80 to-transparent"></div>
            <div className="absolute inset-0 z-10 bg-gradient-to-r from-bgDark via-bgDark/40 to-transparent"></div>
            
            {newsItems[currentIndex].image === "mosaic" ? (
              <div className="absolute inset-0 grid grid-cols-4 grid-rows-2 gap-0 filter brightness-50 grayscale hover:grayscale-0 transition-all duration-700">
                {mosaicVideos.map((item, idx) => (
                  <div key={idx} className="w-full h-full relative overflow-hidden border border-white/5">
                    <img 
                      src={`https://i.ytimg.com/vi/${getYTId(item.uploadedYoutubeUrl)}/hqdefault.jpg`} 
                      className="w-full h-full object-cover opacity-80"
                      alt=""
                    />
                  </div>
                ))}
              </div>
            ) : (
              <img 
                src={newsItems[currentIndex].image} 
                alt={newsItems[currentIndex].title}
                className="w-full h-full object-cover filter brightness-75 grayscale-[30%]"
              />
            )}

            {/* Conteúdo Textual */}
            <div className="absolute inset-0 z-20 flex flex-col justify-end p-6 sm:p-8 max-w-3xl">
              <motion.span 
                initial={{ y: 20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                transition={{ delay: 0.3 }}
                className="text-accent text-sm font-bold tracking-widest uppercase mb-2"
              >
                {newsItems[currentIndex].tag}
              </motion.span>
              
              <motion.h2 
                initial={{ y: 20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                transition={{ delay: 0.4 }}
                className="text-2xl sm:text-4xl text-white font-black uppercase tracking-tight mb-3 leading-none drop-shadow-md"
              >
                {newsItems[currentIndex].title}
              </motion.h2>

              <motion.p 
                initial={{ y: 20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                transition={{ delay: 0.5 }}
                className="text-gray-300 text-sm sm:text-base hidden sm:block mb-6 max-w-xl"
              >
                {newsItems[currentIndex].description}
              </motion.p>

              <motion.div
                initial={{ y: 20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                transition={{ delay: 0.6 }}
              >
                {newsItems[currentIndex].isExternal ? (
                  <a href={newsItems[currentIndex].link} target="_blank" rel="noreferrer" className="inline-flex items-center gap-2 bg-white/10 hover:bg-accent text-white hover:text-black border border-white/20 hover:border-transparent transition-all duration-300 px-5 py-2.5 rounded-lg text-sm font-bold shadow-lg group">
                    Saber mais <ArrowRight size={16} className="group-hover:translate-x-1 transition-transform" />
                  </a>
                ) : (
                  <Link to={newsItems[currentIndex].link} className="inline-flex items-center gap-2 bg-white/10 hover:bg-accent text-white hover:text-black border border-white/20 hover:border-transparent transition-all duration-300 px-5 py-2.5 rounded-lg text-sm font-bold shadow-lg group">
                    Saber mais <ArrowRight size={16} className="group-hover:translate-x-1 transition-transform" />
                  </Link>
                )}
              </motion.div>
            </div>
          </motion.div>
        </AnimatePresence>
      </div>

      {/* Indicadores do Carousel */}
      {newsItems.length > 1 && (
        <div className="absolute bottom-4 right-4 z-30 flex gap-2">
          {newsItems.map((_, idx) => (
            <button
              key={idx}
              onClick={() => setCurrentIndex(idx)}
              className={`transition-all duration-300 h-1.5 rounded-full ${
                idx === currentIndex ? 'w-8 bg-accent' : 'w-4 bg-white/30 hover:bg-white/50'
              }`}
            />
          ))}
        </div>
      )}
    </div>
  );
};
