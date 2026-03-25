import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Crown, HelpCircle, Trophy, BarChart3, Users, Clock, ArrowRight, Settings, UploadCloud, Lock, Unlock, AlertTriangle, ShieldAlert, Star } from 'lucide-react';
import { Link } from 'react-router-dom';
import EmConstrucao from '../components/EmConstrucao';
import { useAuth } from '../context/AuthContext';

const ReiDaMesa = () => {
  const { user } = useAuth();
  const isOwner = user?.roles?.includes('OWNER') || user?.roles?.includes('ADMIN');

  const [isAdminPanelOpen, setIsAdminPanelOpen] = useState(false);
  const [isMarketOpen, setIsMarketOpen] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  
  React.useEffect(() => {
    fetch('/api/reidamesa/status')
      .then(res => res.json())
      .then(data => setIsMarketOpen(data.isOpen))
      .catch(console.error);
  }, []);

  const toggleMarket = async (newStatus) => {
    try {
      const res = await fetch('/api/reidamesa/status', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ isOpen: newStatus })
      });
      const data = await res.json();
      setIsMarketOpen(data.isOpen);
    } catch(err) {
      console.error(err);
    }
  };

  const handleFakeUpload = (e) => {
     if(e.target.files.length > 0) {
        setIsUploading(true);
        setTimeout(() => {
          setIsUploading(false);
          alert('Upload computado com sucesso (Funcionalidade visual).');
        }, 1500);
     }
  };

  const ranking = [];

  const pontuacaoRules = [
    { label: "Gol", pts: "+8.0", color: "text-green-500", bg: "bg-green-500/10", border: "border-green-500/20" },
    { label: "Assistência", pts: "+5.0", color: "text-green-400", bg: "bg-green-400/10", border: "border-green-400/20" },
    { label: "Defesa de Pênalti (GK)", pts: "+7.0", color: "text-green-500", bg: "bg-green-500/10", border: "border-green-500/20" },
    { label: "Jogo s/ Sofrer Gol (Def/Gk)", pts: "+5.0", color: "text-blue-400", bg: "bg-blue-400/10", border: "border-blue-400/20" },
    { label: "Finalização na Trave", pts: "+3.0", color: "text-yellow-400", bg: "bg-yellow-400/10", border: "border-yellow-400/20" },
    { label: "Finalização Defendida", pts: "+1.2", color: "text-gray-300", bg: "bg-white/5", border: "border-white/10" },
    { label: "Passe Decisivo", pts: "+1.5", color: "text-gray-300", bg: "bg-white/5", border: "border-white/10" },
    { label: "Desarme", pts: "+1.0", color: "text-gray-300", bg: "bg-white/5", border: "border-white/10" },
    { label: "Falta Cometida", pts: "-0.5", color: "text-red-300", bg: "bg-red-300/10", border: "border-red-300/20" },
    { label: "Cartão Amarelo", pts: "-2.0", color: "text-yellow-500", bg: "bg-yellow-500/10", border: "border-yellow-500/20" },
    { label: "Cartão Vermelho", pts: "-5.0", color: "text-red-500", bg: "bg-red-500/10", border: "border-red-500/20" },
    { label: "Pênalti Perdido", pts: "-4.0", color: "text-red-500", bg: "bg-red-500/10", border: "border-red-500/20" },
    { label: "Gol Contra", pts: "-3.0", color: "text-red-500", bg: "bg-red-500/10", border: "border-red-500/20" },
  ];

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16 overflow-hidden relative">
      
      {/* MOCK LOGIN & ADMIN PANEL TOGGLE (Canto Superior Direito) */}
      <div className="fixed top-24 right-4 sm:right-8 z-50 flex flex-col items-end gap-3">

        {/* Botão Admin (Só aparece se for Dono) */}
        <AnimatePresence>
          {isOwner && (
            <motion.button 
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.8 }}
              onClick={() => setIsAdminPanelOpen(!isAdminPanelOpen)} 
              className={`px-4 py-3 rounded-xl shadow-[0_0_20px_rgba(0,0,0,0.5)] transition-all flex items-center gap-2 border ${isAdminPanelOpen ? 'bg-primary border-primary/50 text-white' : 'bg-gray-900 border-white/10 text-gray-300 hover:border-white/30 hover:bg-gray-800'}`}
              title="Abrir Painel do Streamer"
            >
              <Settings size={18} />
              <span className="font-bold text-sm tracking-wide uppercase">Painel Dono</span>
            </motion.button>
          )}
        </AnimatePresence>
      </div>

      {/* ADMIN / STREAMER PANEL */}
      <AnimatePresence>
        {isAdminPanelOpen && (
          <motion.section 
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            className="w-full bg-primary/10 border-b border-primary/30 mb-12 overflow-hidden"
          >
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
              <h2 className="text-2xl font-black uppercase tracking-tight flex items-center gap-3 mb-6">
                <ShieldAlert className="text-primary" />
                Painel do Streamer (Admin)
              </h2>
              
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                 {/* Card 1: Mercado */}
                 <div className="bg-black/40 border border-primary/20 p-6 rounded-2xl flex flex-col justify-between">
                    <div>
                      <h3 className="font-bold text-lg mb-2 flex items-center gap-2">
                        Status do Mercado
                      </h3>
                      <p className="text-gray-400 text-sm mb-6">Controle se os viewers podem montar o esquadrão ou não (durante a partida, o mercado fecha).</p>
                    </div>
                    {isMarketOpen ? (
                      <button onClick={() => toggleMarket(false)} className="w-full flex justify-center items-center gap-2 bg-red-500/20 hover:bg-red-500/30 text-red-500 border border-red-500/50 py-3 rounded-lg font-bold uppercase transition-all">
                        <Lock size={18} /> Fechar Mercado
                      </button>
                    ) : (
                      <button onClick={() => toggleMarket(true)} className="w-full flex justify-center items-center gap-2 bg-green-500/20 hover:bg-green-500/30 text-green-500 border border-green-500/50 py-3 rounded-lg font-bold uppercase transition-all">
                        <Unlock size={18} /> Abrir Mercado
                      </button>
                    )}
                 </div>

                 {/* Card 2: Upload de Elenco */}
                 <div className="bg-black/40 border border-primary/20 p-6 rounded-2xl flex flex-col justify-between">
                    <div>
                      <h3 className="font-bold text-lg mb-2">1. Carregar Elenco Atual</h3>
                      <p className="text-gray-400 text-sm mb-6">Faça o upload do HTML exportado do FM para disponibilizar os jogadores no formulário do Viewer.</p>
                    </div>
                    <div>
                      <label className="flex flex-col items-center justify-center w-full h-24 border-2 border-primary/30 border-dashed rounded-lg cursor-pointer bg-primary/5 hover:bg-primary/10 transition-colors">
                        <div className="flex flex-col items-center justify-center pt-5 pb-6">
                          <UploadCloud className="w-6 h-6 mb-2 text-primary" />
                          <p className="text-xs text-gray-400"><span className="font-bold text-white">{isUploading ? 'Processando...' : 'Clique para enviar'}</span> ou arraste o .html</p>
                        </div>
                        <input type="file" className="hidden" accept=".html" onChange={handleFakeUpload} />
                      </label>
                    </div>
                 </div>

                 {/* Card 3: Upload de Resultados */}
                 <div className="bg-black/40 border border-primary/20 p-6 rounded-2xl flex flex-col justify-between">
                    <div>
                      <h3 className="font-bold text-lg mb-2">2. Computar Resultados</h3>
                      <p className="text-gray-400 text-sm mb-6">Após o jogo da live, carregue o HTML das estatísticas da partida para gerar a pontuação e rankear os viewers.</p>
                    </div>
                    <div>
                      <label className="flex flex-col items-center justify-center w-full h-24 border-2 border-primary/30 border-dashed rounded-lg cursor-pointer bg-primary/5 hover:bg-primary/10 transition-colors">
                        <div className="flex flex-col items-center justify-center pt-5 pb-6">
                          <BarChart3 className="w-6 h-6 mb-2 text-primary" />
                          <p className="text-xs text-gray-400"><span className="font-bold text-white">{isUploading ? 'Processando HTML...' : 'Clique para enviar'}</span> html de estatísticas</p>
                        </div>
                        <input type="file" className="hidden" accept=".html" onChange={handleFakeUpload} />
                      </label>
                    </div>
                 </div>
              </div>
            </div>
          </motion.section>
        )}
      </AnimatePresence>
      
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
          
          <h1 className="text-5xl md:text-7xl font-black uppercase tracking-tighter leading-none mb-6">
            Escale, Assista<br/>
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-accent to-accentHover drop-shadow-md">
              E Seja o Rei
            </span>
          </h1>
          <p className="text-lg text-gray-400 mb-8 max-w-xl leading-relaxed">
            O Fantasy Game exclusivo da nossa comunidade. Escolha 3 titulares do meu save, um bônus do banco de reservas e aposte em quem será o Bagre da partida! Torça durante as lives e suba nos rankings com base no desempenho real dos jogadores no Football Manager!
          </p>
          
          {isMarketOpen ? (
            <Link 
             to="/reidamesa/escalar"
             className="flex items-center justify-center gap-2 font-black uppercase tracking-wide px-8 py-4 rounded-xl shadow-[0_0_20px_rgba(255,215,0,0.3)] transition-all duration-300 w-full sm:w-auto bg-accent hover:bg-accentHover text-black hover:scale-105"
            >
               <Crown size={20} /> Montar Meu Esquadrão
            </Link>
          ) : (
            <button 
             disabled
             className="flex items-center justify-center gap-2 font-black uppercase tracking-wide px-8 py-4 rounded-xl transition-all duration-300 w-full sm:w-auto bg-gray-800 text-gray-500 cursor-not-allowed border border-white/10 shadow-none"
            >
               <AlertTriangle size={20} /> Escalação Indisponível (Jogo em Andamento)
            </button>
          )}
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
                <p className="text-gray-400 text-sm">Escolha 3 titulares (incluindo o Goleiro na Defesa), 1 reserva e 1 bagre antes de rolar a bola.</p>
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

      {/* Tabela de Pontuação visível */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mb-20 bg-gray-900/50 border border-white/5 rounded-3xl p-6 md:p-10">
        <div className="text-center mb-10">
           <h2 className="text-2xl font-black uppercase tracking-tight flex items-center justify-center gap-3">
              <Star className="text-accent" />
              Regras de Pontuação (Cartola)
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

      {/* 3. Rodada Atual e Tabela de Ranking */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 grid grid-cols-1 lg:grid-cols-12 gap-10">
         
         {/* Esquadrão Mock */}
         <div className="lg:col-span-6 space-y-6">
            <h2 className="text-2xl font-black uppercase tracking-tight flex items-center gap-2">
               <Users className="text-accent" />
               Meu Esquadrão (Mock)
            </h2>
            <div className={`w-full border rounded-3xl p-8 flex flex-col items-center justify-center text-center relative overflow-hidden h-96 group transition-colors ${isMarketOpen ? 'bg-green-800/20 border-green-500/30' : 'bg-red-900/10 border-red-500/20'}`}>
               {/* Grass pattern */}
               <div className="absolute inset-0 opacity-10" style={{ backgroundImage: "url('data:image/svg+xml,%3Csvg width=\\'20\\' height=\\'20\\' viewBox=\\'0 0 20 20\\' xmlns=\\'http://www.w3.org/2000/svg\\'%3E%3Cg fill=\\'%2322c55e\\' fill-opacity=\\'1\\' fill-rule=\\'evenodd\\'%3E%3Ccircle cx=\\'3\\' cy=\\'3\\' r=\\'3\\'/%3E%3Ccircle cx=\\'13\\' cy=\\'13\\' r=\\'3\\'/%3E%3C/g%3E%3C/svg%3E')" }}></div>
               
               <div className="z-10 bg-black/60 p-6 rounded-2xl backdrop-blur-md border border-white/10 max-w-sm">
                  <Crown size={48} className={`mx-auto mb-4 ${isMarketOpen ? 'text-accent' : 'text-gray-500'}`} />
                  <h3 className="text-xl font-bold mb-2">{isMarketOpen ? 'Escale Agora' : 'Escalação Bloqueada'}</h3>
                  <p className="text-sm text-gray-300 mb-6">Em breve o sistema visual de escalação arrastar e soltar estará disponível.</p>
                  {isMarketOpen ? (
                    <Link to="/reidamesa/escalar" className="px-6 py-2 rounded-lg font-bold uppercase text-xs tracking-widest transition-colors w-full border bg-accent/10 hover:bg-accent/20 border-accent/30 text-accent inline-block text-center">
                      Ir para Formulário
                    </Link>
                  ) : (
                    <button disabled className="px-6 py-2 rounded-lg font-bold uppercase text-xs tracking-widest transition-colors w-full border bg-white/5 border-white/10 text-gray-500 cursor-not-allowed">
                      Jogo em Andamento
                    </button>
                  )}
               </div>
            </div>
         </div>

         {/* Ranking Tabela Mock */}
         <div className="lg:col-span-6 space-y-6">
            <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
              <h2 className="text-2xl font-black uppercase tracking-tight flex items-center gap-2">
                 <Trophy className="text-accent" />
                 Rankings
              </h2>
              <div className="flex gap-2 w-full md:w-auto">
                <Link to="/reidamesa/ranking" className="bg-black/50 hover:bg-white/10 px-4 py-2 text-xs font-bold uppercase tracking-widest border border-white/10 text-white rounded transition-colors text-center flex-1">
                  Ver Completo
                </Link>
                <Link to="/reidamesa/perfil" className="bg-black/50 hover:bg-white/10 px-4 py-2 text-xs font-bold uppercase tracking-widest border border-white/10 text-white rounded transition-colors text-center flex-1">
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
                       <td className="px-2 py-3 text-center text-green-400 font-mono">+{row.lastRound}</td>
                       <td className="px-4 py-3 text-right font-black text-accent font-mono">{row.total}</td>
                     </tr>
                   ))}
                   {/* Linha Fictícia "Você" */}
                   <tr className="hover:bg-white/5 transition-colors">
                     <td className="px-4 py-3 font-black text-center text-gray-500">...</td>
                     <td className="px-2 py-3 font-bold text-gray-400">Você</td>
                     <td className="px-2 py-3 text-center text-gray-500 font-mono">-</td>
                     <td className="px-4 py-3 text-right font-black text-gray-500 font-mono">0.0</td>
                   </tr>
                 </tbody>
               </table>
            </div>
         </div>

      </section>

    </div>
  );
};

export default ReiDaMesa;
