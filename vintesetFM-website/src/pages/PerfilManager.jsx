import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { User, Activity, TrendingUp, Award, Calendar, ChevronDown, ChevronUp, Trophy } from 'lucide-react';
import { Link } from 'react-router-dom';

const PerfilManager = () => {
  // Histórico Mock
  const historico = [
    { rodada: 12, data: "19/03/2026", total: "+45.2", titulo: "A Grande Retranca", titulares: ["Léo Ortiz (5.5)", "De Arrascaeta (0.0)", "Pedro (12.5)"], banco: "Gerson (27.2)" },
    { rodada: 11, data: "18/03/2026", total: "+12.0", titulo: "Dia de Chuva", titulares: ["Fabricio Bruno (4.0)", "Pulgar (3.0)", "Bruno Henrique (5.0)"], banco: "Luiz Araújo (0.0)" },
    { rodada: 10, data: "15/03/2026", total: "+81.8", titulo: "Mito do Domingo", titulares: ["Ayrton Lucas (15.5)", "Gerson (10.0)", "Pedro (50.3)"], banco: "De Arrascaeta (6.0)" },
  ];

  const [expandedRound, setExpandedRound] = useState(12);

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        
        {/* Header */}
        <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-8 gap-4">
          <div>
            <h1 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3">
               <User className="text-accent" size={32} />
               Visão Geral do Manager
            </h1>
            <p className="text-gray-400 mt-2">Acompanhe seu desempenho histórico e suas escalações.</p>
          </div>
          
          <Link to="/reidamesa" className="px-6 py-2 bg-black/50 border border-white/10 hover:bg-white/10 text-sm font-bold uppercase tracking-widest text-white rounded transition-all">
            Voltar ao Dashboard
          </Link>
        </div>

        {/* Profile Header Novo (Troféus Cartola) */}
        <div className="bg-gray-900 border border-accent/30 rounded-3xl p-8 mb-12 shadow-[0_0_30px_rgba(255,215,0,0.1)] relative overflow-hidden flex flex-col md:flex-row items-center gap-8 text-center md:text-left">
           <div className="absolute inset-0 bg-gradient-to-r from-accent/5 to-transparent pointer-events-none"></div>
           
           {/* Avatar com Destaque Rei da Mesa */}
           <div className="relative z-10 shrink-0">
              <div className="w-32 h-32 rounded-full border-4 border-accent bg-black p-1 relative shadow-2xl">
                 <img src="https://i.pravatar.cc/300?img=11" alt="Manager Profile" className="w-full h-full rounded-full object-cover" />
                 <div className="absolute -bottom-4 left-1/2 -translate-x-1/2 bg-accent text-black text-[10px] font-black uppercase px-4 py-1.5 rounded-full border-2 border-black flex items-center gap-1 shadow-md w-max">
                   <Crown size={14} /> Rei da Mesa
                 </div>
              </div>
           </div>

           <div className="z-10 flex-1">
              <h2 className="text-4xl font-black uppercase tracking-tighter mb-2 flex items-center justify-center md:justify-start gap-3">
                Manager Vinteseter
              </h2>
              <p className="text-gray-400 mb-6 font-mono text-sm">Membro desde Março de 2026 • Equipe "Os Imbatíveis"</p>

              {/* Sala de Troféus do Usuário */}
              <div className="flex flex-wrap items-center justify-center md:justify-start gap-4">
                 <div className="bg-black/50 border border-white/20 px-5 py-3 rounded-2xl flex items-center gap-3 hover:border-accent/60 hover:bg-accent/5 transition-colors" title="Troféu Rei da Mesa (Campeão Mensal)">
                    <div className="w-10 h-10 rounded-full bg-accent/20 flex items-center justify-center border border-accent/30 shrink-0">
                      <Crown className="text-accent" size={20} />
                    </div>
                    <div className="text-left">
                      <p className="text-[9px] text-gray-500 uppercase font-black tracking-widest">Troféu Mensal</p>
                      <p className="text-sm font-bold text-accent">Rei da Mesa (1x)</p>
                    </div>
                 </div>
                 <div className="bg-black/50 border border-white/20 px-5 py-3 rounded-2xl flex items-center gap-3 hover:border-yellow-500/60 hover:bg-yellow-500/5 transition-colors" title="Troféu Carro do Ovo (Campeão da Live)">
                    <div className="w-12 h-12 rounded-full bg-yellow-500/10 flex items-center justify-center border border-yellow-500/30 shrink-0 overflow-visible p-1">
                      <img src="/carro-ovo-minimal.png" alt="Troféu Carro do Ovo" className="w-full h-full object-contain filter drop-shadow-md" />
                    </div>
                    <div className="text-left">
                      <p className="text-[9px] text-gray-500 uppercase font-black tracking-widest">Campeão da Live</p>
                      <p className="text-sm font-bold text-yellow-500">Carro do Ovo (3x)</p>
                    </div>
                 </div>
              </div>
           </div>
        </div>

        {/* Estatísticas do Topo */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12">
            <div className="bg-gray-900 border border-white/10 p-6 rounded-2xl flex items-center gap-4 shadow-xl">
               <div className="w-16 h-16 rounded-full bg-accent/20 border border-accent/40 flex items-center justify-center">
                 <Trophy size={28} className="text-accent drop-shadow-[0_0_10px_rgba(255,215,0,0.8)]" />
               </div>
               <div>
                  <h3 className="text-sm font-bold text-gray-400 uppercase tracking-widest">Maior Pontuação</h3>
                  <p className="text-3xl font-black text-white font-mono">81.8<span className="text-sm text-accent ml-1">pts</span></p>
               </div>
            </div>
            
            <div className="bg-gray-900 border border-white/10 p-6 rounded-2xl flex items-center gap-4 shadow-xl">
               <div className="w-16 h-16 rounded-full bg-green-500/20 border border-green-500/40 flex items-center justify-center">
                 <TrendingUp size={28} className="text-green-500" />
               </div>
               <div>
                  <h3 className="text-sm font-bold text-gray-400 uppercase tracking-widest">Média por Rodada</h3>
                  <p className="text-3xl font-black text-white font-mono">46.3<span className="text-sm text-green-500 ml-1">pts</span></p>
               </div>
            </div>

            <div className="bg-gray-900 border border-white/10 p-6 rounded-2xl flex items-center gap-4 shadow-xl">
               <div className="w-16 h-16 rounded-full bg-purple-500/20 border border-purple-500/40 flex items-center justify-center">
                 <Award size={28} className="text-purple-400" />
               </div>
               <div>
                  <h3 className="text-sm font-bold text-gray-400 uppercase tracking-widest">Aposta de Ouro</h3>
                  <p className="text-2xl font-black text-white uppercase tracking-tighter">Pedro</p>
                  <p className="text-xs text-gray-500">Quem mais te deu pontos</p>
               </div>
            </div>
        </div>

        {/* Histórico Escalações (Accordion) */}
        <div>
           <h2 className="text-xl font-black uppercase tracking-tight flex items-center gap-2 mb-6">
               <Activity className="text-gray-400" />
               Histórico de Rodadas
           </h2>

           <div className="space-y-4">
              {historico.map(rodada => (
                <div key={rodada.rodada} className="bg-gray-900 border border-white/10 rounded-2xl overflow-hidden transition-all shadow-lg hover:border-white/20">
                   {/* Card Header */}
                   <div 
                     className="p-6 flex flex-col sm:flex-row justify-between items-start sm:items-center cursor-pointer select-none gap-4"
                     onClick={() => setExpandedRound(expandedRound === rodada.rodada ? null : rodada.rodada)}
                   >
                     <div className="flex items-center gap-4">
                        <div className="bg-white/5 border border-white/10 w-12 h-12 flex items-center justify-center rounded-xl font-black text-xl">
                          {rodada.rodada}
                        </div>
                        <div>
                           <div className="flex items-center gap-2 mb-1">
                             <Calendar size={12} className="text-gray-500" />
                             <span className="text-xs font-bold text-gray-500 uppercase tracking-widest">{rodada.data}</span>
                           </div>
                           <h3 className="font-bold text-lg">{rodada.titulo}</h3>
                        </div>
                     </div>
                     <div className="flex items-center gap-6">
                        <div className="text-right">
                           <span className="font-black text-2xl text-accent font-mono">{rodada.total}</span>
                           <span className="text-xs text-gray-400 block uppercase">Pontos Salvos</span>
                        </div>
                        <div className="w-8 h-8 rounded-full bg-white/5 flex items-center justify-center text-gray-400">
                          {expandedRound === rodada.rodada ? <ChevronUp size={20} /> : <ChevronDown size={20} />}
                        </div>
                     </div>
                   </div>

                   {/* Card Body (Expanded) */}
                   <AnimatePresence>
                      {expandedRound === rodada.rodada && (
                        <motion.div 
                          initial={{ height: 0, opacity: 0 }}
                          animate={{ height: 'auto', opacity: 1 }}
                          exit={{ height: 0, opacity: 0 }}
                          className="overflow-hidden border-t border-white/5 bg-black/40"
                        >
                           <div className="p-6 grid grid-cols-1 md:grid-cols-4 gap-6">
                              <div className="md:col-span-3 space-y-4">
                                 <h4 className="text-xs font-bold text-gray-500 uppercase tracking-widest border-b border-white/10 pb-2 mb-4">Escolhas (Titulares 3x)</h4>
                                 <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                                    {rodada.titulares.map((titular, idx) => (
                                       <div key={idx} className="bg-white/5 border border-white/10 rounded-xl p-3 flex flex-col justify-center text-center">
                                          <span className="font-bold text-sm text-gray-300">{titular.split(' ')[0]} {titular.split(' ')[1]?.split('(')[0]}</span>
                                          <span className="text-xs text-green-400 font-mono mt-1 font-bold">{titular.match(/\(([^)]+)\)/)[1]} pts</span>
                                       </div>
                                    ))}
                                 </div>
                              </div>
                              <div className="md:col-span-1 space-y-4">
                                 <h4 className="text-xs font-bold text-accent uppercase tracking-widest border-b border-accent/20 pb-2 mb-4">Bônus (Reserva 1x)</h4>
                                 <div className="bg-accent/5 border border-accent/30 rounded-xl p-3 flex flex-col justify-center text-center h-[calc(100%-2.5rem)]">
                                     <span className="font-bold text-sm text-accent drop-shadow-md">{rodada.banco.split(' ')[0]}</span>
                                     <span className="text-xl text-accent font-mono mt-2 font-black">+{rodada.banco.match(/\(([^)]+)\)/)[1]}</span>
                                 </div>
                              </div>
                           </div>
                        </motion.div>
                      )}
                   </AnimatePresence>
                </div>
              ))}
           </div>
        </div>

      </div>
    </div>
  );
};

export default PerfilManager;
