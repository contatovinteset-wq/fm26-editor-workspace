import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { User, Activity, TrendingUp, Award, Calendar, ChevronDown, ChevronUp, Trophy, Crown } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const PerfilManager = () => {
  const { user } = useAuth();
  const isOwner = user?.roles?.includes('OWNER');
  const historico = [];

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

        {/* Profile Header Novo */}
        <div className="bg-gray-900 border border-accent/30 rounded-3xl p-8 mb-12 shadow-[0_0_30px_rgba(255,215,0,0.1)] relative overflow-hidden flex flex-col md:flex-row items-center gap-8 text-center md:text-left">
           <div className="absolute inset-0 bg-gradient-to-r from-accent/5 to-transparent pointer-events-none"></div>
           
           {/* Avatar com Destaque de Role */}
           <div className="relative z-10 shrink-0">
              <div className={`w-32 h-32 rounded-full border-4 bg-black p-1 relative shadow-2xl flex items-center justify-center overflow-hidden ${isOwner ? 'border-amber-500' : 'border-blue-500'}`}>
                 {user?.avatar ? (
                   <img src={user.avatar} alt="Manager Profile" className="w-full h-full rounded-full object-cover" />
                 ) : (
                   <span className={`text-5xl font-black ${isOwner ? 'text-amber-500' : 'text-blue-500'}`}>{(user?.nickname || 'V')[0].toUpperCase()}</span>
                 )}
                 <div className={`absolute -bottom-4 left-1/2 -translate-x-1/2 text-[10px] font-black uppercase px-4 py-1.5 rounded-full border-2 border-black flex items-center gap-1 shadow-md w-max ${isOwner ? 'bg-amber-500 text-black' : 'bg-blue-500 text-white'}`}>
                   {isOwner ? <><Crown size={14} /> Owner</> : <><User size={14} /> Membro</>}
                 </div>
              </div>
           </div>

           <div className="z-10 flex-1 mt-4 md:mt-0">
              <h2 className="text-4xl font-black uppercase tracking-tighter mb-6 flex items-center justify-center md:justify-start gap-3">
                Manager {user?.nickname || 'Vinteseter'}
              </h2>

              {/* Sala de Troféus Ocultada até o Engine do Campeonato Módulo Rei da Mesa Ficar Pronto */}
              <div className="opacity-40 text-xs text-gray-500 uppercase font-black tracking-widest border border-dashed border-gray-700 p-4 rounded-xl inline-block">
                Sala de Troféus: Aguardando Abertura da Temporada do Rei da Mesa
              </div>
           </div>
        </div>

        {/* Estatísticas Ocultadas Temporariamente */}
        {/* Histórico Escalações (Accordion) Ocultado Temporariamente */}

      </div>
    </div>
  );
};

export default PerfilManager;
