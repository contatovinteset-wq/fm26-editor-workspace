import React, { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Trophy, Star, User } from 'lucide-react';

const ReiDaMesaOverlay = () => {
    const [events, setEvents] = useState([]);

    useEffect(() => {
        // Assegurar fundo transparente no body (para captar no OBS)
        document.body.style.background = 'transparent';
        const rootElement = document.getElementById('root');
        if (rootElement) rootElement.style.background = 'transparent';

        const evtSource = new EventSource('/api/reidamesa/overlay/stream');

        evtSource.onmessage = (event) => {
            const data = JSON.parse(event.data);
            if (data.type === 'CONNECTED') return;

            // Ao receber um evento novo, cria um ID unico para ele
            const newEvent = { ...data, id: Date.now() + Math.random() };
            
            // Adiciona na fila e programa para remover depois de 10s
            setEvents(prev => [...prev, newEvent]);

            setTimeout(() => {
                setEvents(prev => prev.filter(e => e.id !== newEvent.id));
            }, 10000);
        };

        evtSource.onerror = (error) => {
            console.error("EventSource failed:", error);
            // Ele tenta reconectar sozinho pelo padrao do EventSource
        };

        return () => {
             evtSource.close();
             // Reverte os fundos da tela
             document.body.style.background = '';
             if (rootElement) rootElement.style.background = '';
        };
    }, []);

    const slideVariants = {
       initial: { y: 200, opacity: 0, scale: 0.8 },
       animate: { y: 0, opacity: 1, scale: 1, transition: { type: "spring", stiffness: 100, damping: 15 } },
       exit: { y: 200, opacity: 0, scale: 0.8, transition: { duration: 0.5 } }
    };

    return (
        <div className="w-screen h-screen overflow-hidden flex flex-col justify-end items-center pb-24 absolute top-0 left-0 pointer-events-none z-50">
           <AnimatePresence>
               {events.map((evt) => {
                   if (evt.type === 'NEW_SQUAD') {
                       return (
                           <motion.div 
                               key={evt.id}
                               variants={slideVariants}
                               initial="initial"
                               animate="animate"
                               exit="exit"
                               className="mb-4 bg-gray-900/90 backdrop-blur-md border border-green-500/50 shadow-[0_0_20px_rgba(34,197,94,0.3)] rounded-2xl p-6 text-center max-w-xl flex items-center gap-6"
                           >
                               <div className="bg-green-500/20 p-4 rounded-full">
                                   <User className="w-10 h-10 text-green-400" />
                               </div>
                               <div className="text-left">
                                   <h2 className="text-3xl font-black text-white uppercase tracking-wider">{evt.user}</h2>
                                   <p className="text-green-400 text-xl font-bold uppercase tracking-widest mt-1">Acabou de escalar o time!</p>
                               </div>
                           </motion.div>
                       );
                   }

                   if (evt.type === 'ROUND_FINISHED') {
                       return (
                          <motion.div 
                               key={evt.id}
                               variants={slideVariants}
                               initial="initial"
                               animate="animate"
                               exit="exit"
                               className="mb-8 flex flex-col items-center gap-6"
                           >
                               
                               {/* Painel do Craque do Chat */}
                               {evt.craque && (
                               <motion.div 
                                   initial={{ x: -100, opacity: 0 }}
                                   animate={{ x: 0, opacity: 1, transition: { delay: 0.5 } }}
                                   className="bg-gray-900/95 backdrop-blur-xl border border-purple-500/50 shadow-[0_0_30px_rgba(168,85,247,0.4)] rounded-2xl p-8 flex items-center gap-8 w-full max-w-3xl"
                               >
                                   <div className="bg-purple-500/20 p-5 rounded-full relative overflow-hidden group">
                                       <div className="absolute inset-0 bg-purple-500 blur-xl opacity-20 animate-pulse"></div>
                                       <Star className="w-14 h-14 text-purple-400 relative z-10" />
                                   </div>
                                   <div className="text-left flex-1">
                                       <p className="text-purple-400 text-lg font-bold uppercase tracking-widest mb-1">Craque do Chat 🌟</p>
                                       <h2 className="text-5xl font-black text-white uppercase tracking-wider bg-gradient-to-r from-purple-400 to-fuchsia-300 bg-clip-text text-transparent">{evt.craque.name}</h2>
                                   </div>
                               </motion.div>
                               )}

                               {/* Painel do Viewer Campeão */}
                               {evt.champion && (
                               <motion.div 
                                   initial={{ x: 100, opacity: 0 }}
                                   animate={{ x: 0, opacity: 1, transition: { delay: 1.0 } }}
                                   className="bg-gray-900/95 backdrop-blur-xl border border-yellow-500/50 shadow-[0_0_30px_rgba(234,179,8,0.4)] rounded-2xl p-8 flex items-center gap-8 w-full max-w-3xl"
                               >
                                   <div className="bg-yellow-500/20 p-5 rounded-full relative overflow-hidden">
                                       <div className="absolute inset-0 bg-yellow-500 blur-xl opacity-20 animate-pulse"></div>
                                       <Trophy className="w-14 h-14 text-yellow-400 relative z-10" />
                                   </div>
                                   <div className="text-left flex-1">
                                       <p className="text-yellow-400 text-lg font-bold uppercase tracking-widest mb-1">Viewer Campeão da Rodada 🏆</p>
                                       <h2 className="text-5xl font-black text-white uppercase tracking-wider">{evt.champion.nickname}</h2>
                                   </div>
                                   <div className="text-right border-l border-white/10 pl-8 ml-4">
                                       <p className="text-gray-400 uppercase tracking-widest text-sm mb-1">Pontos</p>
                                       <span className="text-5xl font-black text-green-400">{evt.champion.score}</span>
                                   </div>
                               </motion.div>
                               )}

                          </motion.div>
                       );
                   }

                   return null;

               })}
           </AnimatePresence>
        </div>
    );
};

export default ReiDaMesaOverlay;
