import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { User, ShieldAlert, BarChart3, Target, Goal, Sword, Trophy } from 'lucide-react';
import RoleBadge from '../components/RoleBadge';
import ConquistasManager from '../components/ConquistasManager';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { rdmFetch, useRdmBase } from '../services/reidamesa';

const PerfilManager = () => {
  const { user } = useAuth();
  const isOwner = user?.roles?.includes('OWNER');
  const base = useRdmBase();
  
  const [squad, setSquad] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    rdmFetch('/api/reidamesa/squad', { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error('Não foi possível carregar esquadrão');
        return res.json();
      })
      .then(data => {
        setSquad(data);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  }, []);

  const getRoleIcon = (role) => {
    switch (role?.toUpperCase()) {
      case 'DEF': return <ShieldAlert className="text-blue-400" size={16} />;
      case 'MEI': return <Target className="text-green-400" size={16} />;
      case 'ATA': return <Goal className="text-accent" size={16} />;
      default: return <Sword className="text-gray-400" size={16} />;
    }
  };

  const renderPlayerCard = (player, label, isBagre = false) => {
    if (!player) {
      return (
        <div className="bg-black/40 border border-white/5 rounded-xl p-4 flex flex-col items-center justify-center text-center opacity-50 h-full min-h-[140px]">
          <span className="text-xs uppercase font-bold text-gray-500 mb-2">{label}</span>
          <span className="text-sm font-bold text-gray-600">Nenhum</span>
        </div>
      );
    }
    return (
      <div className={`bg-gradient-to-b ${isBagre ? 'from-red-900/20 to-black/40 border-red-500/30' : 'from-accent/5 to-black/40 border-white/10'} border rounded-xl p-4 flex flex-col relative overflow-hidden h-full min-h-[140px] group`}>
        {isBagre && <div className="absolute top-0 inset-x-0 h-1 bg-red-500 z-20"></div>}
        <div className="flex justify-between items-start mb-4 relative z-20">
          <span className={`text-xs uppercase font-black ${isBagre ? 'text-red-400' : 'text-gray-400'}`}>{label}</span>
          <div className="bg-black/50 px-2 py-1 rounded text-xs font-mono font-bold flex items-center gap-1 border border-white/10 backdrop-blur-sm">
            {getRoleIcon(player.cartolaRole)} {player.cartolaRole}
          </div>
        </div>
        <div className="mt-auto relative z-20 max-w-[70%]">
          <h4 className="font-bold text-lg text-white leading-tight mb-1 drop-shadow-md">{player.name}</h4>
          <span className="text-xs font-bold text-gray-300 drop-shadow-md">{player.realPosition}</span>
        </div>
        
        {/* Foto do Jogador */}
        <div className="absolute -bottom-2 -right-4 w-28 h-28 opacity-40 mix-blend-luminosity group-hover:opacity-100 group-hover:mix-blend-normal transition-all duration-300 z-10">
          <img 
            src={`https://sortitoutsi.b-cdn.net/uploads/face/face_${player.uniqueId}.png`} 
            alt={player.name} 
            className="w-full h-full object-contain object-bottom pointer-events-none" 
            onError={(e) => { e.target.src = `https://via.placeholder.com/150/111/fff?text=${player.name.charAt(0)}` }}
          />
        </div>
      </div>
    );
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-bgDark flex justify-center items-center">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-accent"></div>
      </div>
    );
  }

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
          
          <Link to={base} className="px-6 py-2 bg-black/50 border border-white/10 hover:bg-white/10 text-sm font-bold uppercase tracking-widest text-white rounded transition-all">
            Voltar ao Dashboard
          </Link>
        </div>

        {/* Profile Header Novo */}
        <div className="bg-gray-900 border border-accent/30 rounded-3xl p-8 mb-12 shadow-[0_0_30px_rgba(255,215,0,0.1)] relative overflow-hidden flex flex-col md:flex-row items-center gap-8 text-center md:text-left">
           <div className="absolute inset-0 bg-gradient-to-r from-accent/5 to-transparent pointer-events-none"></div>
           
           {/* Avatar com Destaque de Role */}
           <div className="relative z-10 shrink-0 mx-auto md:mx-0">
              <div className={`w-32 h-32 rounded-full border-4 bg-black p-1 relative shadow-2xl flex items-center justify-center ${isOwner ? 'border-amber-500' : 'border-blue-500'}`}>
                 <div className="w-full h-full rounded-full overflow-hidden flex items-center justify-center">
                   {user?.avatar ? (
                     <img src={user.avatar} alt="Manager Profile" className="w-full h-full object-cover" />
                   ) : (
                     <span className={`text-5xl font-black ${isOwner ? 'text-amber-500' : 'text-blue-500'}`}>{(user?.nickname || user?.name || 'V')[0].toUpperCase()}</span>
                   )}
                 </div>
                 <RoleBadge roles={user?.roles} absolute />
              </div>
           </div>

           <div className="z-10 flex-1 mt-4 md:mt-0">
              <h2 className="text-4xl font-black uppercase tracking-tighter mb-6 flex items-center justify-center md:justify-start gap-3">
                Manager {user?.nickname || user?.name || user?.twitchId || 'Vinteseter'}
              </h2>

              <div className="flex gap-4 mt-4 md:mt-0 justify-center md:justify-start">
                  <div className="bg-black/50 border border-white/5 rounded-xl px-6 py-4 text-center min-w-[120px]">
                     <span className="text-xs uppercase font-bold text-gray-500 mb-1 block">Rodada</span>
                     <span className={`text-2xl font-black font-mono ${(squad?.roundScore || 0) >= 0 ? 'text-green-400' : 'text-red-500'}`}>{squad?.roundScore?.toFixed(1) || '0.0'}</span>
                  </div>
                  <div className="bg-black/50 border border-white/5 rounded-xl px-6 py-4 text-center min-w-[120px]">
                     <span className="text-xs uppercase font-bold text-gray-500 mb-1 block">Total</span>
                     <span className="text-2xl font-black font-mono text-accent">{squad?.totalScore?.toFixed(1) || '0.0'}</span>
                  </div>
               </div>
           </div>
        </div>

        {/* Conquistas (Gamificação G2) */}
        <div className="mb-12">
           <ConquistasManager />
        </div>

        {/* Scaled Team */}
        <div className="mb-8">
           <div className="flex justify-between items-center mb-6">
              <h3 className="text-xl font-bold uppercase tracking-widest flex items-center gap-2">
                 <BarChart3 className="text-gray-400" size={20} />
                 Esquadrão Atual
              </h3>
              {isOwner && (
                 <Link to={`${base}/escalar`} className="text-xs font-bold uppercase tracking-widest text-accent hover:text-white transition-colors">
                   Alterar Escalação
                 </Link>
              )}
           </div>

           {!squad ? (
             <div className="bg-gray-900 border border-white/10 rounded-2xl p-12 text-center">
               <ShieldAlert className="text-gray-500 mx-auto mb-4" size={48} />
               <h4 className="text-lg font-bold text-white mb-2">Você ainda não definiu um Esquadrão</h4>
               <p className="text-gray-400 text-sm mb-6 max-w-md mx-auto">Vá até a tela de escalação e monte seu time titular e escolha seu bagre para começar a pontuar nas lives.</p>
               {isOwner ? (
                 <Link to={`${base}/escalar`} className="inline-block bg-accent text-black px-8 py-3 rounded-xl font-bold uppercase text-xs tracking-widest hover:bg-accentHover transition-colors">
                   Escalar Agora
                 </Link>
               ) : (
                 <span className="inline-block bg-gray-800 text-gray-500 px-8 py-3 rounded-xl font-bold uppercase text-xs tracking-widest cursor-not-allowed">
                   Aguarde o Lançamento
                 </span>
               )}
             </div>
           ) : (
             <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                {renderPlayerCard(squad.defensor, 'Defensor')}
                {renderPlayerCard(squad.meio, 'Meio Além')}
                {renderPlayerCard(squad.ataque, 'Atacante')}
                {renderPlayerCard(squad.bagre, 'O Bagre', true)}
             </div>
           )}
        </div>

        {/* Botão Copiar Link */}
        <div className="bg-black/30 border border-white/5 rounded-xl p-4 flex flex-col items-center justify-center text-center">
           <p className="text-sm text-gray-400 mb-4">Em breve você poderá compartilhar seu esquadrão com os amigos no WhatsApp e X (Twitter).</p>
           <button disabled className="px-6 py-2 bg-white/5 text-gray-500 rounded font-bold uppercase text-xs tracking-widest cursor-not-allowed border border-white/10">
              Compartilhar Escalação (Breve)
           </button>
        </div>

      </div>
    </div>
  );
};

export default PerfilManager;
