import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Crown, HelpCircle, Trophy, BarChart3, Users, Clock, ArrowRight, Settings, UploadCloud, Lock, Unlock, AlertTriangle, ShieldAlert, Star, ClipboardList, Calendar } from 'lucide-react';
import { Link } from 'react-router-dom';
import EmConstrucao from '../components/EmConstrucao';
import { useAuth } from '../context/AuthContext';
import { rdmFetch, useRdmBase, useCreator } from '../services/reidamesa';

const ReiDaMesa = () => {
  const { user } = useAuth();
  const base = useRdmBase();
  const { slug, creator } = useCreator();
  // Quem pode administrar um Rei da Mesa (o backend isola pelo dono).
  // OWNER em qualquer página; CREATOR/ADMIN_GERACAO só dentro de um criador (slug).
  const roles = user?.roles || [];
  const canManage = roles.includes('OWNER') || roles.includes('ADMIN') ||
    ((roles.includes('CREATOR') || roles.includes('ADMIN_GERACAO')) && !!slug);

  const [isMarketOpen, setIsMarketOpen] = useState(true);
  const [ranking, setRanking] = useState([]);
  const [topMatch, setTopMatch] = useState({ top3: [], bagre: null });
  const [mySquad, setMySquad] = useState(null);
  const [craqueData, setCraqueData] = useState({ mode: 'CLOSED' });
  const [craqueCandidates, setCraqueCandidates] = useState([]);
  const [selectedCraque, setSelectedCraque] = useState(null);
  
  React.useEffect(() => {
    rdmFetch('/api/reidamesa/status')
      .then(res => res.json())
      .then(data => setIsMarketOpen(data.isOpen))
      .catch(console.error);

    rdmFetch('/api/reidamesa/ranking')
      .then(res => res.json())
      .then(data => {
         const formatted = data.slice(0,5).map((sq, i) => ({
             position: i + 1,
             name: sq.user?.nickname || sq.user?.name || sq.user?.twitchId || 'Desconhecido',
             lastRound: sq.roundScore || 0,
             total: sq.totalScore || 0
         }));
         setRanking(formatted);
      })
      .catch(console.error);

    rdmFetch('/api/reidamesa/top-match')
      .then(res => res.json())
      .then(data => setTopMatch(data))
      .catch(console.error);

    rdmFetch('/api/reidamesa/squad', {
       headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
    })
      .then(res => res.json())
      .then(data => {
         if(data && Object.keys(data).length > 0) setMySquad(data);
      })
      .catch(console.error);

    // Craque do Chat Status
    rdmFetch('/api/reidamesa/craque/status', {
       headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
    })
      .then(res => res.json())
      .then(data => {
         setCraqueData(data);
         if (data.mode === 'VOTING') {
            setSelectedCraque(data.userVote ? data.userVote.id : null);
            rdmFetch('/api/reidamesa/players')
               .then(playersRes => playersRes.json())
               .then(playersData => {
                   setCraqueCandidates(playersData);
               });
         } else if (data.mode === 'RESULTS') {
            rdmFetch('/api/reidamesa/craque/results')
               .then(resultsRes => resultsRes.json())
               .then(resultsData => {
                   setCraqueData({ mode: 'RESULTS', top3: resultsData.top3, totalVotes: resultsData.totalVotes });
               });
         }
      })
      .catch(console.error);
  }, []);

  const handleVoteCraque = async (playerId) => {
     try {
         const res = await rdmFetch('/api/reidamesa/craque/vote', {
             method: 'POST',
             headers: { 
                 'Content-Type': 'application/json',
                 'Authorization': `Bearer ${localStorage.getItem('token')}` 
             },
             body: JSON.stringify({ playerId })
         });
         const data = await res.json();
         if (data.success) {
            setSelectedCraque(playerId);
         } else {
            alert(data.error || 'Erro ao votar no craque!');
         }
     } catch (err) {
         console.error(err);
         alert('Erro ao votar no craque!');
     }
  };

  // Funções de Admin movidas para ReiDaMesaAdmin.jsx

  const pontuacaoRules = [
    { label: "Jogou +60m", pts: "+1.0", color: "text-green-500", bg: "bg-green-500/10", border: "border-green-500/20" },
    { label: "Gol", pts: "+8.0", color: "text-green-500", bg: "bg-green-500/10", border: "border-green-500/20" },
    { label: "Assistência", pts: "+5.0", color: "text-green-400", bg: "bg-green-400/10", border: "border-green-400/20" },
    { label: "xG e xA (Por Ponto)", pts: "+2.0", color: "text-green-300", bg: "bg-green-300/10", border: "border-green-300/20" },
    { label: "Chance Criada", pts: "+2.0", color: "text-blue-400", bg: "bg-blue-400/10", border: "border-blue-400/20" },
    { label: "Passe Decisivo", pts: "+1.0", color: "text-blue-400", bg: "bg-blue-400/10", border: "border-blue-400/20" },
    { label: "Finta Exito", pts: "+0.5", color: "text-purple-400", bg: "bg-purple-400/10", border: "border-purple-400/20" },
    { label: "Chute na Trave", pts: "+1.5", color: "text-yellow-400", bg: "bg-yellow-400/10", border: "border-yellow-400/20" },
    { label: "Desarme Certo", pts: "+2.0", color: "text-teal-400", bg: "bg-teal-400/10", border: "border-teal-400/20" },
    { label: "Intercepção", pts: "+0.5", color: "text-teal-400", bg: "bg-teal-400/10", border: "border-teal-400/20" },
    { label: "Defesa (Goleiro)", pts: "+1.5", color: "text-white", bg: "bg-white/5", border: "border-white/10" },
    { label: "Alívio", pts: "+0.2", color: "text-gray-400", bg: "bg-white/5", border: "border-white/10" },
    { label: "Falta Cometida", pts: "-0.5", color: "text-red-300", bg: "bg-red-300/10", border: "border-red-300/20" },
    { label: "Cartão Amarelo", pts: "-1.5", color: "text-yellow-500", bg: "bg-yellow-500/10", border: "border-yellow-500/20" },
    { label: "Cartão Vermelho", pts: "-3.0", color: "text-red-500", bg: "bg-red-500/10", border: "border-red-500/20" },
  ];


  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16 overflow-hidden relative">
      
      {/* MOCK LOGIN & ADMIN PANEL TOGGLE (Canto Superior Direito) */}
      <div className="fixed top-24 right-4 sm:right-8 z-50 flex flex-col items-end gap-3">
        <AnimatePresence>
          {canManage && (
            <Link
              to={`${base}/admin`}
              className="px-4 py-3 rounded-xl shadow-[0_0_20px_rgba(0,0,0,0.5)] transition-all flex items-center gap-2 border bg-gray-900 border-white/10 text-gray-300 hover:border-white/30 hover:bg-gray-800"
              title="Ir para o Painel do Streamer"
            >
              <Settings size={18} />
              <span className="font-bold text-sm tracking-wide uppercase">Painel</span>
            </Link>
          )}
        </AnimatePresence>
      </div>
      
      {/* 1. Hero Section */}
      <section className="relative w-full max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mb-20 flex flex-col md:flex-row items-center gap-12">
        <motion.div 
          key={isMarketOpen ? "open" : "closed"}
          initial={{ opacity: 0, x: -50 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.6 }}
          className="w-full md:w-1/2"
        >
          {isMarketOpen ? (
            <div className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-green-500/10 border border-green-500/20 text-green-500 font-bold text-sm tracking-widest uppercase mb-6 shadow-[0_0_15px_rgba(34,197,94,0.2)]">
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-500 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-green-500"></span>
              </span>
              Mercado Aberto
            </div>
          ) : (
            <div className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-red-500/10 border border-red-500/20 text-red-500 font-bold text-sm tracking-widest uppercase mb-6 shadow-[0_0_15px_rgba(239,68,68,0.2)]">
              <Lock size={14} className="text-red-500" />
              Mercado Fechado
            </div>
          )}
          
          {creator && (
            <div className="mb-4 inline-flex items-center gap-2 self-start bg-accent/10 border border-accent/30 px-3 py-1 rounded-full">
              <Crown size={14} className="text-accent" />
              <span className="text-xs font-bold uppercase tracking-widest text-accent">Rei da Mesa de {creator.name}</span>
            </div>
          )}

          <h1 className="text-5xl md:text-7xl font-black uppercase tracking-tighter leading-none mb-6">
            Escale, Assista<br/>
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-accent to-accentHover drop-shadow-md">
              E Seja o Rei
            </span>
          </h1>
          <p className="text-lg text-gray-400 mb-8 max-w-xl leading-relaxed">
            O Fantasy Game exclusivo da nossa comunidade. Escolha 3 titulares do meu save e aposte em quem será o Bagre da partida! Torça durante as lives e suba nos rankings com base no desempenho real dos jogadores no Football Manager!
          </p>
          
          <div className="flex flex-col sm:flex-row gap-4">
            {isMarketOpen ? (
              <Link 
               to={`${base}/escalar`}
               className="flex items-center justify-center gap-2 font-black uppercase tracking-wide px-8 py-4 rounded-xl shadow-[0_0_20px_rgba(255,215,0,0.3)] transition-all duration-300 w-full sm:w-auto bg-accent hover:bg-accentHover text-black hover:scale-105"
              >
                 <Crown size={20} /> Montar Meu Esquadrão
              </Link>
            ) : (
              <Link 
               to={`${base}/escalar`}
               className="flex items-center justify-center gap-2 font-black uppercase tracking-wide px-8 py-4 rounded-xl transition-all duration-300 w-full sm:w-auto bg-gray-800 text-gray-400 hover:bg-gray-700 hover:text-white border border-white/10 shadow-none"
              >
                 <AlertTriangle size={20} /> Meu Esquadrão (Mercado Fechado)
              </Link>
            )}

            <Link 
             to={`${base}/plantel`}
             className="flex items-center justify-center gap-2 font-black uppercase tracking-wide px-8 py-4 rounded-xl transition-all duration-300 w-full sm:w-auto bg-black/50 text-gray-300 hover:bg-white/10 border border-white/10"
            >
               <Users size={20} /> Estudar Plantel
            </Link>
          </div>
        </motion.div>
        
        <motion.div 
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 0.6, delay: 0.2 }}
          className="w-full md:w-1/2 flex justify-center relative"
        >
          <div className="absolute inset-0 bg-accent/20 blur-[100px] rounded-full mix-blend-screen pointer-events-none"></div>
          <img 
            src="/ReiDaMesaFM-Logo.jpg" 
            alt="Rei Da Mesa FM Logo" 
            className="w-full max-w-md object-contain z-10 drop-shadow-2xl rounded-full border-4 border-white/5"
            style={{ shapeOutside: 'circle()', filter: 'drop-shadow(0 0 30px rgba(255,215,0,0.3))' }}
          />
        </motion.div>
      </section>

      {/* 2. Como Funciona */}
      <section className="bg-black/30 border-y border-white/5 py-16 mb-20 relative">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
             <h2 className="text-3xl md:text-4xl font-black uppercase tracking-tight flex items-center justify-center gap-3">
                <HelpCircle className="text-accent" />
                Como Funciona?
             </h2>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-7 gap-6">
             <div className="col-span-1 md:col-span-2 bg-gray-900 border border-white/10 p-6 rounded-2xl flex flex-col items-center text-center group hover:border-accent/40 transition-colors">
                <div className="w-16 h-16 rounded-full bg-accent/10 flex items-center justify-center mb-4 group-hover:scale-110 transition-transform">
                  <Users size={32} className="text-accent" />
                </div>
                <h3 className="font-bold text-lg mb-2">1. Escale o Time</h3>
                <p className="text-gray-400 text-sm">Escolha 3 titulares (incluindo o Goleiro na Defesa) e 1 bagre antes de rolar a bola.</p>
             </div>
             
             <div className="hidden md:flex items-center justify-center opacity-30 text-white">
               <ArrowRight size={32} />
             </div>
             
             <div className="col-span-1 md:col-span-1 bg-gray-900 border border-white/10 p-6 rounded-2xl flex flex-col items-center text-center group hover:border-accent/40 transition-colors">
                <div className="w-16 h-16 rounded-full bg-primary/20 flex items-center justify-center mb-4 group-hover:scale-110 transition-transform">
                  <Clock size={32} className="text-primary" />
                </div>
                <h3 className="font-bold text-lg mb-2">2. Assista</h3>
                <p className="text-gray-400 text-sm">Acompanhe nas Lives.</p>
             </div>
             
             <div className="hidden md:flex items-center justify-center opacity-30 text-white">
               <ArrowRight size={32} />
             </div>
             
             <div className="col-span-1 md:col-span-2 bg-gray-900 border border-white/10 p-6 rounded-2xl flex flex-col items-center text-center group hover:border-accent/40 transition-colors relative overflow-hidden">
                <div className="absolute inset-0 bg-gradient-to-tr from-accent/5 to-transparent pointer-events-none"></div>
                <div className="w-16 h-16 rounded-full bg-accent/20 flex items-center justify-center mb-4 group-hover:scale-110 transition-transform border border-accent/30 shadow-[0_0_15px_rgba(255,215,0,0.5)] z-10">
                  <Trophy size={32} className="text-accent drop-shadow-lg" />
                </div>
                <h3 className="font-bold text-lg mb-2 z-10">3. Receba Pontos</h3>
                <p className="text-gray-400 text-sm z-10">As estatísticas nas partidas viram pontuação para os rankings: Geral (Soma de pontos) e o Rei da Mesa (Maior pontuador da Live).</p>
             </div>
          </div>
        </div>
      </section>

      {/* CRAQUE DO CHAT SECTION */}
      {craqueData.mode !== 'CLOSED' && (
        <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mb-20">
          <div className="bg-gradient-to-r from-[#2a1744] to-[#120a1f] border border-purple-500/30 rounded-3xl p-6 md:p-10 relative overflow-hidden shadow-[0_0_40px_rgba(139,92,246,0.15)]">
            {/* Efeitos de Fundo */}
            <div className="absolute top-0 right-0 w-64 h-64 bg-purple-600/20 blur-[80px] rounded-full pointer-events-none"></div>
            <div className="absolute bottom-0 left-0 w-64 h-64 bg-accent/10 blur-[80px] rounded-full pointer-events-none"></div>

            <div className="text-center mb-8 relative z-10">
               <h2 className="text-3xl md:text-4xl font-black uppercase tracking-tight flex items-center justify-center gap-3">
                  <Star className="text-purple-400 animate-pulse" size={36} />
                  Craque do Chat
                  <Star className="text-purple-400 animate-pulse" size={36} />
               </h2>
               <p className="text-gray-300 mt-3 max-w-2xl mx-auto">
                 {craqueData.mode === 'VOTING' 
                   ? "O Mercado fechou e a bola está rolando! Vote em quem você acha que será eleito o Craque da Partida!" 
                   : "O jogo acabou! Estes foram os jogadores favoritos da galera nesta rodada!"}
               </p>
            </div>

            {craqueData.mode === 'VOTING' && (
               <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4 relative z-10 max-h-[400px] overflow-y-auto custom-scrollbar p-2">
                 {craqueCandidates.map(p => (
                   <div 
                     key={p.id}
                     onClick={() => handleVoteCraque(p.id)}
                     className={`cursor-pointer rounded-xl p-3 border text-center transition-all duration-300 ${
                       selectedCraque === p.id 
                         ? 'bg-purple-600/30 border-purple-400 shadow-[0_0_20px_rgba(168,85,247,0.4)] scale-105' 
                         : 'bg-black/40 border-white/10 hover:border-purple-500/50 hover:bg-purple-900/20'
                     }`}
                   >
                     <div className={`w-16 h-16 mx-auto rounded-full bg-black border-2 flex items-center justify-center overflow-hidden mb-2 ${selectedCraque === p.id ? 'border-purple-400' : 'border-gray-700'}`}>
                        <img 
                          src={`https://sortitoutsi.b-cdn.net/uploads/face/face_${p.uniqueId}.png`} 
                          alt={p.name} 
                          className="w-full h-full object-cover" 
                          onError={(e) => { e.target.src = 'https://via.placeholder.com/150/111/fff?text=' + p.name.charAt(0) }} 
                        />
                     </div>
                     <p className="font-bold text-xs sm:text-sm truncate">{p.name}</p>
                     <p className="text-[10px] text-gray-400 font-bold uppercase">{p.realPosition}</p>
                     {selectedCraque === p.id && (
                       <span className="mt-2 inline-block bg-purple-500 text-white text-[10px] font-black uppercase px-2 py-1 rounded">Votado</span>
                     )}
                   </div>
                 ))}
               </div>
            )}

            {craqueData.mode === 'RESULTS' && (
               <div className="flex flex-col md:flex-row justify-center items-end gap-6 md:gap-12 relative z-10 pt-8">
                 {craqueData.top3?.map((player, index) => {
                    const isFirst = index === 0;
                    const height = isFirst ? 'h-48 md:h-56' : index === 1 ? 'h-40 md:h-48' : 'h-32 md:h-40';
                    const color = isFirst ? 'from-yellow-400/80 to-yellow-600' : index === 1 ? 'from-gray-300/80 to-gray-500' : 'from-orange-400/80 to-orange-700';
                    const borderColor = isFirst ? 'border-yellow-400' : index === 1 ? 'border-gray-300' : 'border-orange-400';
                    
                    return (
                      <div key={player.id} className="flex flex-col items-center justify-end w-full md:w-48 group">
                         <h3 className="font-black text-xl text-white text-center truncate w-full mb-1">{player.name}</h3>
                         <p className="font-bold text-purple-300 text-sm mb-4">{player.percentage}% ({player.votes} votos)</p>
                         
                         <div className="relative w-24 h-24 mb-4 z-10">
                            {isFirst && <Crown size={32} className="absolute -top-6 left-1/2 -translate-x-1/2 text-yellow-500 drop-shadow-lg z-20 animate-bounce" />}
                            <div className={`w-full h-full rounded-full bg-black border-4 flex items-center justify-center overflow-hidden shadow-2xl ${borderColor}`}>
                               <img 
                                 src={`https://sortitoutsi.b-cdn.net/uploads/face/face_${player.uniqueId}.png`} 
                                 alt={player.name} 
                                 className="w-full h-full object-cover" 
                                 onError={(e) => { e.target.src = 'https://via.placeholder.com/150/111/fff?text=' + player.name.charAt(0) }} 
                               />
                            </div>
                         </div>
                         
                         <div className={`w-full ${height} bg-gradient-to-t ${color} rounded-t-xl border-x-2 border-t-2 ${borderColor} flex items-end justify-center pb-4 shadow-[0_0_30px_rgba(0,0,0,0.5)]`}>
                            <span className="font-black text-4xl text-black/50">{index + 1}</span>
                         </div>
                      </div>
                    )
                 })}

                 {(!craqueData.top3 || craqueData.top3.length === 0) && (
                    <p className="text-gray-400 text-center w-full">Nenhum voto registrado para a partida.</p>
                 )}
               </div>
            )}
          </div>
        </section>
      )}

      {/* Tabela de Pontuação visível */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mb-20 bg-gray-900/50 border border-white/5 rounded-3xl p-6 md:p-10">
        <div className="text-center mb-10">
           <h2 className="text-2xl font-black uppercase tracking-tight flex items-center justify-center gap-3">
              <Star className="text-accent" />
              Regras de Pontuação (Rei da Mesa)
           </h2>
           <p className="text-gray-400 text-sm mt-3">É assim que seus jogadores vão ganhar ou perder pontos no fim da partida.</p>
        </div>
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          {pontuacaoRules.map((r, i) => (
             <div key={i} className={`${r.bg} border ${r.border} rounded-xl p-4 flex justify-between items-center hover:scale-[1.02] transition-transform`}>
               <span className="font-bold text-xs uppercase text-gray-300">{r.label}</span>
               <span className={`font-black font-mono text-lg ${r.color}`}>{r.pts}</span>
             </div>
          ))}
        </div>
      </section>

      {/* Destaques da Última Rodada */}
      {(topMatch.top3.length > 0 || topMatch.bagre) && (
        <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mb-20">
          <div className="text-center mb-10">
             <h2 className="text-3xl font-black uppercase tracking-tight flex items-center justify-center gap-3">
                <Trophy className="text-accent" />
                Destaques da Última Rodada
             </h2>
             <p className="text-gray-400 text-sm mt-3">Os melhores em campo e o Bagre Oficial.</p>
          </div>
          
          <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
            {/* Top 3 */}
            <div className="lg:col-span-3 grid grid-cols-1 md:grid-cols-3 gap-6">
              {topMatch.top3.map((player, index) => (
                <div key={index} className="bg-gradient-to-b from-gray-800 to-gray-900 border border-white/10 rounded-2xl p-6 relative overflow-hidden group hover:border-accent/50 transition-colors flex flex-col items-center">
                  <div className="absolute top-4 right-4 w-8 h-8 rounded-full bg-black/50 flex items-center justify-center font-black text-accent border border-accent/20 z-10">
                    {index + 1}
                  </div>
                  <div className="w-20 h-20 rounded-full bg-black border-2 border-accent mb-4 flex items-center justify-center overflow-hidden">
                     <img src={`https://sortitoutsi.b-cdn.net/uploads/face/face_${player.uniqueId}.png`} alt={player.name} className="w-full h-full object-cover opacity-50" onError={(e) => { e.target.src = 'https://via.placeholder.com/150/111/fff?text=' + player.name.charAt(0) }} />
                  </div>
                  <h3 className="font-black text-lg text-center leading-tight mb-1">{player.name}</h3>
                  <p className="text-xs text-gray-400 font-bold uppercase tracking-widest mb-4">{player.realPosition}</p>
                  
                  <div className="bg-accent/10 border border-accent/20 px-4 py-2 rounded-xl w-full flex justify-between items-center mt-auto">
                    <span className="text-xs font-bold text-accent uppercase">Pontos</span>
                    <span className="font-black font-mono text-xl text-white">+{player.matchPoints}</span>
                  </div>
                </div>
              ))}
            </div>

            {/* O Bagre */}
            {topMatch.bagre && (
              <div className="lg:col-span-1 bg-gradient-to-b from-red-900/40 to-black border border-red-500/30 rounded-2xl p-6 relative overflow-hidden group hover:border-red-500/60 transition-colors flex flex-col items-center">
                  <div className="absolute top-4 right-4 w-8 h-8 rounded-full bg-red-500/20 flex items-center justify-center font-black text-red-500 border border-red-500/30">
                    <ShieldAlert size={16} />
                  </div>
                  <div className="w-20 h-20 rounded-full bg-black border-2 border-red-500 mb-4 flex items-center justify-center overflow-hidden grayscale">
                     <img src={`https://sortitoutsi.b-cdn.net/uploads/face/face_${topMatch.bagre.uniqueId}.png`} alt={topMatch.bagre.name} className="w-full h-full object-cover opacity-50" onError={(e) => { e.target.src = 'https://via.placeholder.com/150/550000/fff?text=' + topMatch.bagre.name.charAt(0) }} />
                  </div>
                  <h3 className="font-black text-lg text-center leading-tight text-red-100 mb-1">{topMatch.bagre.name}</h3>
                  <p className="text-xs text-red-400/80 font-bold uppercase tracking-widest mb-4">O Bagre Oficial</p>
                  
                  <div className="bg-red-500/20 border border-red-500/30 px-4 py-2 rounded-xl w-full text-center mt-auto">
                    <span className="text-xs font-bold text-red-400 uppercase">Pior da Partida</span>
                  </div>
              </div>
            )}
          </div>
        </section>
      )}

      {/* 3. Rodada Atual e Tabela de Ranking */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 grid grid-cols-1 lg:grid-cols-12 gap-10">
         
         {/* Esquadrão */}
         <div className="lg:col-span-6 space-y-6">
            <h2 className="text-2xl font-black uppercase tracking-tight flex items-center gap-2">
               <Users className="text-accent" />
               Meu Esquadrão
            </h2>
            <div className={`w-full border rounded-3xl p-8 flex flex-col items-center justify-center text-center relative overflow-hidden h-96 group transition-colors ${isMarketOpen ? 'bg-green-800/20 border-green-500/30' : 'bg-red-900/10 border-red-500/20'}`}>
               {/* Grass pattern */}
               <div className="absolute inset-0 opacity-10" style={{ backgroundImage: "url('data:image/svg+xml,%3Csvg width=\\'20\\' height=\\'20\\' viewBox=\\'0 0 20 20\\' xmlns=\\'http://www.w3.org/2000/svg\\'%3E%3Cg fill=\\'%2322c55e\\' fill-opacity=\\'1\\' fill-rule=\\'evenodd\\'%3E%3Ccircle cx=\\'3\\' cy=\\'3\\' r=\\'3\\'/%3E%3Ccircle cx=\\'13\\' cy=\\'13\\' r=\\'3\\'/%3E%3C/g%3E%3C/svg%3E')" }}></div>
               
               {(!isMarketOpen && mySquad) ? (
                  <div className="z-10 w-full flex flex-col h-full justify-between items-center bg-black/40 p-4 rounded-xl backdrop-blur-sm border border-white/10">
                     <div className="w-full flex justify-between items-center border-b border-white/10 pb-2 mb-2">
                        <span className="font-bold uppercase text-xs tracking-widest text-gray-400">Pts na Rodada:</span>
                        <span className={`font-black text-xl ${(mySquad.roundScore || 0) >= 0 ? 'text-accent' : 'text-red-500'}`}>
                           {mySquad.roundScore?.toFixed(2) || '0.00'}
                        </span>
                     </div>
                     <div className="w-full flex flex-col gap-2 flex-1 justify-center">
                        {[mySquad.ataque, mySquad.meio, mySquad.defensor, mySquad.bagre].map((p, idx) => {
                           if(!p) return null;
                           const isBagre = idx === 3;
                           return (
                              <div key={p.id} className={`flex items-center justify-between text-left p-2 rounded-lg border ${isBagre ? 'bg-red-900/30 border-red-500/30' : 'bg-white/5 border-white/10'}`}>
                                 <div className="flex flex-col">
                                    <span className={`text-[10px] font-bold uppercase ${isBagre ? 'text-red-400' : 'text-gray-400'}`}>{isBagre ? 'Bagre' : p.cartolaRole}</span>
                                    <span className="text-sm font-black truncate max-w-[150px]">{p.name}</span>
                                 </div>
                                 {isBagre ? (
                                    (p.matchPoints !== null && p.matchPoints !== undefined) ? (
                                       <span className={`font-bold text-xs sm:text-sm px-2 py-1 rounded bg-black/50 ${topMatch?.bagre?.id === p.id ? 'text-green-400 border border-green-500/50' : 'text-red-400 border border-red-500/50'}`}>
                                         {topMatch?.bagre?.id === p.id ? '5.00' : '-5.00'} pts
                                       </span>
                                    ) : (
                                       <span className="font-bold text-[10px] text-gray-500">Última: {p.lastMatchPoints?.toFixed(2) || '0.00'}</span>
                                    )
                                 ) : (
                                    <div className="flex flex-col items-end">
                                      {(p.matchPoints !== null && p.matchPoints !== undefined) && (
                                        <span className={`font-bold text-sm ${p.matchPoints > 0 ? 'text-green-400' : p.matchPoints < 0 ? 'text-red-400' : 'text-gray-500'}`}>
                                           At: {p.matchPoints > 0 ? '+' : ''}{p.matchPoints?.toFixed(2) || '0.00'}
                                        </span>
                                      )}
                                      <span className={`text-[10px] font-bold ${p.lastMatchPoints > 0 ? 'text-blue-400/80' : p.lastMatchPoints < 0 ? 'text-orange-400/80' : 'text-gray-600'}`}>
                                         Ul: {p.lastMatchPoints > 0 ? '+' : ''}{p.lastMatchPoints?.toFixed(2) || '0.00'}
                                      </span>
                                    </div>
                                 )}
                              </div>
                           )
                        })}
                     </div>
                     <Link to={`${base}/escalar`} className="mt-4 px-6 py-2 rounded-lg font-bold uppercase text-xs tracking-widest transition-colors w-full border bg-white/10 hover:bg-white/20 border-white/20 text-white inline-block text-center shadow-lg">
                        Ver no Campinho
                     </Link>
                  </div>
               ) : (
                  <div className="z-10 bg-black/60 p-6 rounded-2xl backdrop-blur-md border border-white/10 max-w-sm">
                     <Crown size={48} className={`mx-auto mb-4 ${isMarketOpen ? 'text-accent' : 'text-gray-500'}`} />
                     <h3 className="text-xl font-bold mb-2">{isMarketOpen ? 'Escale Agora' : 'Escalação Bloqueada'}</h3>
                     <p className="text-sm text-gray-300 mb-6">Monte seu time para pontuar nesta rodada.</p>
                     
                     <Link to={`${base}/escalar`} className="px-6 py-2 rounded-lg font-bold uppercase text-xs tracking-widest transition-colors w-full border bg-accent/10 hover:bg-accent/20 border-accent/30 text-accent inline-block text-center">
                        {isMarketOpen ? 'Ir para Escalação' : 'Ver Meu Time'}
                     </Link>
                  </div>
               )}
            </div>
         </div>

         {/* Ranking Tabela */}
         <div className="lg:col-span-6 space-y-6">
            <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
              <h2 className="text-2xl font-black uppercase tracking-tight flex items-center gap-2">
                 <Trophy className="text-accent" />
                 Rankings
              </h2>
              <div className="flex gap-2 w-full md:w-auto">
                <Link to={`${base}/ranking`} className="bg-black/50 hover:bg-white/10 px-4 py-2 text-xs font-bold uppercase tracking-widest border border-white/10 text-white rounded transition-colors text-center flex-1">
                  Ver Completo
                </Link>
                <Link to={`${base}/trofeus`} className="bg-black/50 hover:bg-white/10 px-4 py-2 text-xs font-bold uppercase tracking-widest border border-white/10 text-white rounded transition-colors text-center flex-1 flex items-center justify-center gap-1.5">
                  <Trophy size={14} className="text-accent" /> Troféus
                </Link>
                <Link to={`${base}/temporadas`} className="bg-black/50 hover:bg-white/10 px-4 py-2 text-xs font-bold uppercase tracking-widest border border-white/10 text-white rounded transition-colors text-center flex-1 flex items-center justify-center gap-1.5">
                  <Calendar size={14} className="text-accent" /> Temporadas
                </Link>
                <Link to={`${base}/escalacoes`} className="bg-black/50 hover:bg-white/10 px-4 py-2 text-xs font-bold uppercase tracking-widest border border-white/10 text-white rounded transition-colors text-center flex-1 flex items-center justify-center gap-1.5">
                  <ClipboardList size={14} className="text-accent" /> Escalações
                </Link>
                <Link to={`${base}/perfil`} className="bg-black/50 hover:bg-white/10 px-4 py-2 text-xs font-bold uppercase tracking-widest border border-white/10 text-white rounded transition-colors text-center flex-1">
                  Meu Perfil
                </Link>
              </div>
            </div>
            
            <div className="bg-gray-900 border border-white/10 rounded-2xl overflow-hidden p-1">
               <table className="w-full text-left text-sm">
                 <thead className="bg-black/50 text-gray-400 font-bold uppercase tracking-wider text-[10px]">
                   <tr>
                     <th className="px-4 py-4 rounded-tl-xl">Pos</th>
                     <th className="px-2 py-4">Manager</th>
                     <th className="px-2 py-4 text-center">Rodada</th>
                     <th className="px-4 py-4 text-right rounded-tr-xl">Total (Geral)</th>
                   </tr>
                 </thead>
                 <tbody className="divide-y divide-white/5">
                   {ranking.map((row) => (
                     <tr key={row.position} className="hover:bg-white/5 transition-colors">
                       <td className="px-4 py-3 font-black text-center w-12">
                         {row.position === 1 ? <Crown size={16} className="text-accent mx-auto" /> : 
                          row.position === 2 ? <span className="text-gray-300">2</span> : 
                          row.position === 3 ? <span className="text-[#CD7F32]">3</span> : row.position}
                       </td>
                       <td className="px-2 py-3 font-bold text-white">{row.name}</td>
                       <td className={`px-2 py-3 text-center font-mono font-bold ${row.lastRound > 0 ? 'text-green-500' : row.lastRound < 0 ? 'text-red-500' : 'text-blue-400'}`}>
                         {row.lastRound > 0 ? '+' : ''}{(row.lastRound || 0).toFixed(2)}
                       </td>
                       <td className={`px-4 py-3 text-right font-black font-mono ${row.total > 0 ? 'text-green-500' : row.total < 0 ? 'text-red-500' : 'text-blue-400'}`}>
                         {row.total > 0 ? '+' : ''}{(row.total || 0).toFixed(2)}
                       </td>
                     </tr>
                   ))}
                   {ranking.length === 0 && (
                     <tr>
                       <td colSpan={4} className="px-4 py-8 text-center text-gray-500">Nenhum ranking disponível no momento.</td>
                     </tr>
                   )}
                 </tbody>
               </table>
            </div>
         </div>

      </section>

    </div>
  );
};

export default ReiDaMesa;
