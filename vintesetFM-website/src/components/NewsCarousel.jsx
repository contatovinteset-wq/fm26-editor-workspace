import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ArrowRight, Sparkles } from 'lucide-react';
import { Link } from 'react-router-dom';

const newsItems = [
  {
    id: 1,
    tag: "NOVA FERRAMENTA",
    title: "FM26 Player Export V4",
    description: "Extraia CP e CA dos jogadores ocultos com extrema facilidade, burlando o Scout da Match Engine.",
    link: "/ferramentas",
    image: "https://images.unsplash.com/photo-1542751371-adc38448a05e?q=80&w=1200&auto=format&fit=crop"
  },
  {
    id: 2,
    tag: "TELECURSO",
    title: "A Arte do Moneyball",
    description: "Aprenda a encontrar os jogadores mais desvalorizados do FM usando métricas avançadas baseadas nos números.",
    link: "/telecurso",
    image: "https://images.unsplash.com/photo-1518605368461-1ee7e53a56cf?q=80&w=1200&auto=format&fit=crop"
  },
  {
    id: 3,
    tag: "ATUALIZAÇÃO",
    title: "Database Brasil 2026",
    description: "Os elencos completos da Série A até a D, com status corrigidos e potêncial em dia baseados na vintesetFM.",
    link: "/mods",
    image: "https://images.unsplash.com/photo-1579952363873-27f3bade9f55?q=80&w=1200&auto=format&fit=crop"
  }
];

export const NewsCarousel = () => {
  const [currentIndex, setCurrentIndex] = useState(0);

  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentIndex((prev) => (prev + 1) % newsItems.length);
    }, 6000);
    return () => clearInterval(timer);
  }, []);

  return (
    <div className="w-full mb-12 relative rounded-2xl overflow-hidden shadow-[0_0_40px_rgba(255,215,0,0.1)] border border-white/10 glass-card">
      <div className="absolute top-4 left-4 z-30 flex items-center gap-2 bg-accent/90 text-black px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider shadow-lg">
        <Sparkles size={14} /> Destaques da Base
      </div>

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
            
            <img 
              src={newsItems[currentIndex].image} 
              alt={newsItems[currentIndex].title}
              className="w-full h-full object-cover filter brightness-75 grayscale-[30%]"
            />

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
                <Link to={newsItems[currentIndex].link} className="inline-flex items-center gap-2 bg-white/10 hover:bg-accent text-white hover:text-black border border-white/20 hover:border-transparent transition-all duration-300 px-5 py-2.5 rounded-lg text-sm font-bold shadow-lg group">
                  Saber mais <ArrowRight size={16} className="group-hover:translate-x-1 transition-transform" />
                </Link>
              </motion.div>
            </div>
          </motion.div>
        </AnimatePresence>
      </div>

      {/* Indicadores do Carousel */}
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
    </div>
  );
};
