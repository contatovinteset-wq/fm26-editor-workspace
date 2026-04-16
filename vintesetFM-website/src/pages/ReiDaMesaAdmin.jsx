import React, { useState, useEffect } from 'react';
import { ShieldAlert, BarChart3, UploadCloud, Lock, Unlock, Trash2, Eye, Copy, CheckCircle, MonitorPlay } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { Navigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
const ReiDaMesaAdmin = () => {
  const { user } = useAuth();
  const isOwner = user?.roles?.includes('OWNER') || user?.roles?.includes('ADMIN_GERACAO') || user?.roles?.includes('ADMIN');

  const [isMarketOpen, setIsMarketOpen] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [allPlayers, setAllPlayers] = useState([]);
  const [savingRows, setSavingRows] = useState({});
  const [matchResult, setMatchResult] = useState(null);
  const [isResultModalOpen, setIsResultModalOpen] = useState(false);
  const [isPreviewModalOpen, setIsPreviewModalOpen] = useState(false);
  const [matchPreview, setMatchPreview] = useState(null);
  const [isProcessingFinal, setIsProcessingFinal] = useState(false);
  const [rounds, setRounds] = useState([]);
  const [selectedRound, setSelectedRound] = useState('');
  const [isAnularModalOpen, setIsAnularModalOpen] = useState(false);
  const [copiedOverlay, setCopiedOverlay] = useState(false);

  const testOverlay = async () => {
    try {
      await fetch('/api/reidamesa/overlay/test', {
         method: 'POST',
         credentials: 'include'
      });
    } catch(err) {
      console.error(err);
    }
  };

  const copyOverlayLink = () => {
    const url = window.location.origin + '/reidamesa/overlay';
    navigator.clipboard.writeText(url);
    setCopiedOverlay(true);
    setTimeout(() => setCopiedOverlay(false), 3000);
  };

  useEffect(() => {
    if (!isOwner) return;

    fetch('/api/reidamesa/players/all', { credentials: 'include' })
      .then(res => res.json())
      .then(data => setAllPlayers(Array.isArray(data) ? data : []))
      .catch(console.error);

    fetch('/api/reidamesa/status')
      .then(res => res.json())
      .then(data => setIsMarketOpen(data.isOpen))
      .catch(console.error);

    fetch('/api/reidamesa/rounds', { credentials: 'include' })
      .then(res => res.json())
      .then(data => {
         setRounds(Array.isArray(data) ? data : []);
         if(Array.isArray(data) && data.length > 0) setSelectedRound(data[0].id);
      })
      .catch(console.error);
  }, [isOwner]);

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

  const handleUpload = async (e, type) => {
    if(e.target.files.length > 0) {
      setIsUploading(true);
      const formData = new FormData();
      formData.append('file', e.target.files[0]);

      const endpoint = type === 'PLANTEL' ? '/api/reidamesa/upload-plantel' : '/api/reidamesa/upload-match';
      try {
        const res = await fetch(endpoint, {
          method: 'POST',
          body: formData,
          credentials: 'include'
        });
        const data = await res.json();
        setIsUploading(false);
        
        if (type === 'PLANTEL') {
            alert(`Upload Concluído! Backend Retornou: ${JSON.stringify(data)}`);
            const playersRes = await fetch('/api/reidamesa/players/all', { credentials: 'include' });
            if (playersRes.ok) {
                const refreshed = await playersRes.json();
                setAllPlayers(refreshed);
            }
        } else if (type === 'MATCH') {
            setMatchPreview(data.scores || []);
            setIsPreviewModalOpen(true);
        }
      } catch (err) {
        console.error(err);
        setIsUploading(false);
        alert('Ocorreu um erro no upload');
      }
    }
  };

  const handleDeleteAll = async () => {
    if (!window.confirm("ATENÇÃO: Você está prestes a excluir TODO o elenco do Banco de Dados. Esta ação é irreversível. Deseja continuar?")) return;
    
    try {
      const res = await fetch('/api/reidamesa/players/all', {
        method: 'DELETE',
        credentials: 'include'
      });
      if (res.ok) {
        setAllPlayers([]);
        alert('Elenco deletado com sucesso!');
      } else {
        alert('Erro ao tentar deletar elenco.');
      }
    } catch (err) {
      console.error(err);
      alert('Erro inesperado ao deletar elenco.');
    }
  };

  const handleOpenAnularModal = () => {
    fetch('/api/reidamesa/rounds', { credentials: 'include' })
      .then(res => res.json())
      .then(data => {
         setRounds(Array.isArray(data) ? data : []);
         if(Array.isArray(data) && data.length > 0) setSelectedRound(data[0].id);
         setIsAnularModalOpen(true);
      });
  };

  const handleConfirmAnular = async () => {
    if(!selectedRound) return;
    try {
      const res = await fetch(`/api/reidamesa/round/${selectedRound}`, {
        method: 'DELETE',
        credentials: 'include'
      });
      if(res.ok) {
        setIsAnularModalOpen(false);
        alert('Rodada anulada com sucesso! O Ranking Geral foi reprocessado.');
        setMatchResult(null); // limpa exibicao passada se houver
      } else {
        alert('Erro ao anular rodada.');
      }
    } catch(err) {
      console.error(err);
      alert('Erro inesperado ao anular rodada.');
    }
  };

  const handleRoleChange = async (playerId, newRole) => {
    setSavingRows(prev => ({ ...prev, [playerId]: true }));
    try {
      const res = await fetch(`/api/reidamesa/players/${playerId}/role`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ cartolaRole: newRole })
      });
      if (res.ok) {
        setAllPlayers(prev => prev.map(p => p.id === playerId ? { ...p, cartolaRole: newRole } : p));
      }
    } catch (err) {
      console.error(err);
    } finally {
      setSavingRows(prev => ({ ...prev, [playerId]: false }));
    }
  };

  if (!isOwner) {
    return <Navigate to="/reidamesa" replace />;
  }

  const defaultHeaders = [
    "Inf", "Escolhido", "Jogador", "Idade", "Altura", "Pé Preferido", 
    "Minutos", "Classificação", "Golos", "Assist.", "Pens", "Pens M", 
    "Remates", "Rem %", "Cab A", "Cabs", "xG", "xA", "Poss Perd/90", "OCG", 
    "Passes Ch", "Cr T", "Cr C", "CT-JA", "CC-JA", "Crt D", "Faltas Cometidas", 
    "Faltas Contra", "EPG", "Distância", "Fnt", "Pas A", "Ps C", "PeP", 
    "Press. tent.", "Press. conc.", "T Desa", "Des C"
  ];

  let orderedKeys = [];
  if (allPlayers.length > 0 && allPlayers[0]?.rawStats) {
    const availableKeys = Object.keys(allPlayers[0].rawStats);
    // REMOVIDO: O filtro k !== 'Inf' que ocultava a primeira coluna (que estava misturando os nomes dos S-21 no titulo "NOME")
    orderedKeys = [...new Set([...defaultHeaders, ...availableKeys])].filter(k => availableKeys.includes(k));
  }

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-8 overflow-x-hidden">
      <div className="w-full max-w-full px-2 sm:px-4">
        
        <div className="flex flex-col xl:flex-row gap-6 mb-6">
          <div className="flex-1">
            <div className="flex flex-col md:flex-row md:items-center justify-between mb-4 gap-4">
              <h2 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3">
                <ShieldAlert className="text-primary w-8 h-8" />
                Painel do Streamer - Rei da Mesa
              </h2>
              
              <div className="flex items-center gap-3">
                <button 
                  onClick={testOverlay}
                  className="flex items-center gap-2 bg-blue-600/20 hover:bg-blue-600/40 text-blue-400 border border-blue-500/30 px-4 py-2 rounded-lg font-bold uppercase tracking-wider transition-colors"
                  title="Enviar mensagem de teste para o OBS"
                >
                   <MonitorPlay className="w-5 h-5" />
                   Testar Overlay
                </button>
                <button 
                  onClick={copyOverlayLink}
                  className="flex items-center gap-2 bg-purple-600/20 hover:bg-purple-600/40 text-purple-400 border border-purple-500/30 px-4 py-2 rounded-lg font-bold uppercase tracking-wider transition-colors"
                  title="Copiar link do OBS"
                >
                   {copiedOverlay ? <CheckCircle className="w-5 h-5" /> : <Copy className="w-5 h-5" />}
                   {copiedOverlay ? 'Copiado!' : 'URL Overlay do OBS'}
                </button>
              </div>
            </div>
          </div>

          {/* Modal de Preview (Cartões Manuais) */}
          <AnimatePresence>
            {isPreviewModalOpen && matchPreview && (
              <motion.div 
                initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
                className="fixed inset-0 z-50 flex flex-col items-center justify-center p-4 bg-black/80 backdrop-blur-sm"
              >
                <motion.div 
                  initial={{ scale: 0.9, y: 20 }} animate={{ scale: 1, y: 0 }} exit={{ scale: 0.9, y: 20 }}
                  className="bg-[#0b0f19] border border-white/10 rounded-2xl shadow-2xl w-full max-w-4xl max-h-[90vh] overflow-hidden flex flex-col"
                >
                  <div className="px-6 py-4 border-b border-yellow-500/20 bg-[#161208] flex justify-between items-center">
                    <h3 className="font-black text-xl text-yellow-500 flex items-center gap-2">
                      <ShieldAlert className="text-yellow-500" /> Pré-Visualização e Punições
                    </h3>
                    <button onClick={() => setIsPreviewModalOpen(false)} className="text-gray-400 hover:text-white transition-colors bg-white/5 hover:bg-white/10 p-2 rounded-lg">
                       Fechar
                    </button>
                  </div>
                  
                  <div className="p-6 overflow-y-auto custom-scrollbar flex-1 text-sm bg-bgDark">
                     <p className="text-gray-300 mb-4 font-medium">Revisão de cartões. Caso o jogador tenha recebido cartões que não saíram na coluna Min devido a substituição ou erro do FM, adicione aqui (Ex: Amarelo: 2, Vermelho: 1).</p>
                     
                     <div className="w-full overflow-x-auto rounded-xl border border-white/10">
                        <table className="w-full text-left font-medium text-xs text-gray-300">
                           <thead className="bg-[#131b2a] text-gray-400 uppercase text-[10px] tracking-wider border-b border-white/10">
                             <tr>
                               <th className="px-4 py-3">Jogador</th>
                               <th className="px-4 py-3 text-center">Minutos</th>
                               <th className="px-4 py-3 text-center">Gol / Ass</th>
                               <th className="px-4 py-3 text-center">Cartões Amarelos</th>
                               <th className="px-4 py-3 text-center">Cartões Vermelhos</th>
                             </tr>
                           </thead>
                           <tbody className="divide-y divide-white/5">
                             {matchPreview.map((s, index) => (
                               <tr key={s.playerId} className="hover:bg-white/5 transition-colors">
                                 <td className="px-4 py-3 font-bold text-white flex items-center gap-2">
                                   {s.playerName} <span className="text-[10px] text-gray-500">[{s.realPosition}]</span>
                                 </td>
                                 <td className="px-4 py-3 text-center">
                                   <input 
                                     type="number" 
                                     min="0" 
                                     value={s.details.minsPlayed || 0} 
                                     onChange={(e) => {
                                       const newPreview = [...matchPreview];
                                       newPreview[index].details.minsPlayed = parseInt(e.target.value) || 0;
                                       setMatchPreview(newPreview);
                                     }}
                                     className="w-16 bg-black/60 border border-white/30 text-white rounded p-1 text-center outline-none focus:border-white"
                                   />
                                 </td>
                                 <td className="px-4 py-3 text-center">{s.details.goals} / {s.details.assists}</td>
                                 <td className="px-4 py-3 text-center">
                                   <input 
                                     type="number" 
                                     min="0" 
                                     value={s.details.yellowCars || 0} 
                                     onChange={(e) => {
                                       const newPreview = [...matchPreview];
                                       newPreview[index].details.yellowCars = parseInt(e.target.value) || 0;
                                       setMatchPreview(newPreview);
                                     }}
                                     className="w-16 bg-black/60 border border-yellow-500/30 text-white rounded p-1 text-center outline-none focus:border-yellow-500"
                                   />
                                 </td>
                                 <td className="px-4 py-3 text-center">
                                   <input 
                                     type="number" 
                                     min="0" 
                                     value={s.details.redCards || 0} 
                                     onChange={(e) => {
                                       const newPreview = [...matchPreview];
                                       newPreview[index].details.redCards = parseInt(e.target.value) || 0;
                                       setMatchPreview(newPreview);
                                     }}
                                     className="w-16 bg-black/60 border border-red-500/30 text-white rounded p-1 text-center outline-none focus:border-red-500"
                                   />
                                 </td>
                               </tr>
                             ))}
                             {matchPreview.length === 0 && (
                               <tr><td colSpan="5" className="px-4 py-8 text-center text-gray-500">Nenhum jogador para processar.</td></tr>
                             )}
                           </tbody>
                        </table>
                     </div>
                  </div>

                  <div className="p-4 bg-[#0e1420] border-t border-white/5 flex justify-end">
                     <button 
                       onClick={async () => {
                         setIsProcessingFinal(true);
                         try {
                           const res = await fetch('/api/reidamesa/process-match-final', {
                             method: 'POST',
                             headers: { 'Content-Type': 'application/json' },
                             credentials: 'include',
                             body: JSON.stringify({ scores: matchPreview })
                           });
                           const finalData = await res.json();
                           setIsProcessingFinal(false);
                           setIsPreviewModalOpen(false);
                           if (res.ok) {
                             setMatchResult(finalData);
                             setIsResultModalOpen(true);
                           } else {
                             alert('Erro ao processar a partida: ' + (finalData.error || 'Erro desconhecido.'));
                           }
                         } catch (e) {
                           console.error(e);
                           setIsProcessingFinal(false);
                           alert('Erro na requisição. Veja o console.');
                         }
                       }} 
                       disabled={isProcessingFinal || matchPreview.length === 0}
                       className="bg-green-600 hover:bg-green-500 text-white font-black uppercase tracking-wider px-6 py-2.5 rounded-lg flex items-center gap-2 transition-colors disabled:opacity-50"
                     >
                       {isProcessingFinal ? 'Processando...' : 'Confirmar e Processar Rodada'}
                     </button>
                  </div>
                </motion.div>
              </motion.div>
            )}
          </AnimatePresence>

          {/* Modal de Resultados da Partida */}
          <AnimatePresence>
            {isResultModalOpen && matchResult && (
              <motion.div 
                initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
                className="fixed inset-0 z-50 flex flex-col items-center justify-center p-4 bg-black/80 backdrop-blur-sm"
              >
                <motion.div 
                  initial={{ scale: 0.9, y: 20 }} animate={{ scale: 1, y: 0 }} exit={{ scale: 0.9, y: 20 }}
                  className="bg-[#0b0f19] border border-white/10 rounded-2xl shadow-2xl w-full max-w-4xl max-h-[90vh] overflow-hidden flex flex-col"
                >
                  <div className="px-6 py-4 border-b border-white/5 bg-[#0e1420] flex justify-between items-center">
                    <h3 className="font-black text-xl text-white flex items-center gap-2">
                      <BarChart3 className="text-accent" /> Relatório da Partida Processado
                    </h3>
                    <button onClick={() => setIsResultModalOpen(false)} className="text-gray-400 hover:text-white transition-colors bg-white/5 hover:bg-white/10 p-2 rounded-lg">
                       Fechar
                    </button>
                  </div>
                  
                  <div className="p-6 overflow-y-auto custom-scrollbar flex-1 text-sm bg-bgDark">
                     <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-6">
                        <div className="bg-primary/10 border border-primary/20 rounded-xl p-4 flex flex-col text-center">
                           <span className="text-primary text-xs font-bold uppercase tracking-widest mb-1">Jogadores Processados</span>
                           <span className="text-3xl font-black text-white">{matchResult.scoresProcessados || 0}</span>
                        </div>
                        <div className="bg-red-500/10 border border-red-500/20 rounded-xl p-4 flex flex-col text-center">
                           <span className="text-red-500 text-xs font-bold uppercase tracking-widest mb-1">Status da Rodada</span>
                           <span className="text-lg font-bold text-white mt-2">Pontuações Salvas no BD</span>
                        </div>
                     </div>

                     <h4 className="font-black text-white uppercase tracking-wider mb-3">Pontuações Calculadas</h4>
                     
                     <div className="w-full overflow-x-auto rounded-xl border border-white/10">
                        <table className="w-full text-left font-medium text-xs text-gray-300">
                           <thead className="bg-[#131b2a] text-gray-400 uppercase text-[10px] tracking-wider border-b border-white/10">
                             <tr>
                               <th className="px-4 py-3">Jogador</th>
                               <th className="px-4 py-3 text-center">Min</th>
                               <th className="px-4 py-3 text-center">Gol/Ass</th>
                               <th className="px-4 py-3 text-center">xG / xA</th>
                               <th className="px-4 py-3 text-center">Cartões</th>
                               <th className="px-4 py-3 text-right">Pts Finais</th>
                             </tr>
                           </thead>
                           <tbody className="divide-y divide-white/5">
                             {matchResult.scores && matchResult.scores.map(s => {
                               const isBagre = s.playerId === matchResult.bagreDaRodadaId;
                               return (
                                 <tr key={s.playerId} className={`hover:bg-white/5 transition-colors ${isBagre ? 'bg-orange-500/10' : ''}`}>
                                   <td className="px-4 py-3 font-bold text-white flex items-center gap-2">
                                     {isBagre && <span className="bg-orange-500 text-black text-[10px] px-1.5 py-0.5 rounded font-black uppercase">Bagre</span>}
                                     {s.playerName} <span className="text-[10px] text-gray-500">[{s.realPosition}]</span>
                                   </td>
                                   <td className="px-4 py-3 text-center">{s.details.minsPlayed || 0}'</td>
                                   <td className="px-4 py-3 text-center">{s.details.goals} / {s.details.assists}</td>
                                   <td className="px-4 py-3 text-center">{s.details.xG} / {s.details.xA}</td>
                                   <td className="px-4 py-3 text-center">
                                      {s.details.yellowCars > 0 && <span className="inline-block w-3 h-4 bg-yellow-400 rounded-sm mx-0.5"></span>}
                                      {s.details.redCards > 0 && <span className="inline-block w-3 h-4 bg-red-600 rounded-sm mx-0.5"></span>}
                                      {s.details.yellowCars === 0 && s.details.redCards === 0 && '-'}
                                   </td>
                                   <td className={`px-4 py-3 text-right font-black text-sm ${s.points > 0 ? 'text-green-400' : s.points < 0 ? 'text-red-400' : 'text-gray-400'}`}>
                                     {s.points > 0 ? '+' : ''}{s.points.toFixed(2)}
                                   </td>
                                 </tr>
                               );
                             })}
                             {(!matchResult.scores || matchResult.scores.length === 0) && (
                               <tr>
                                  <td colSpan="6" className="px-4 py-8 text-center text-gray-500">Nenhum jogador pontuou nesta partida.</td>
                               </tr>
                             )}
                           </tbody>
                        </table>
                     </div>
                  </div>
                </motion.div>
              </motion.div>
            )}
          </AnimatePresence>

          {/* Modal Anular Rodada */}
          <AnimatePresence>
            {isAnularModalOpen && (
              <motion.div 
                initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
                className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm"
              >
                <div className="bg-[#0b0f19] border border-white/10 rounded-2xl w-full max-w-sm p-6 flex flex-col shadow-2xl relative">
                   <h3 className="text-xl font-black text-red-500 mb-4 flex items-center gap-2">
                     <Trash2 /> Anular Rodada
                   </h3>
                   <p className="text-sm text-gray-300 mb-4">
                     Escolha a rodada que deseja cancelar. O ranking geral das carteiras será recalculado subtraindo os pontos dessa rodada.
                   </p>
                   <select 
                      className="bg-black/60 border border-white/20 text-white p-2 rounded-lg mb-6 w-full font-bold outline-none cursor-pointer"
                      value={selectedRound}
                      onChange={(e) => setSelectedRound(e.target.value)}
                   >
                      {rounds.map(r => (
                        <option key={r.id} value={r.id}>Rodada #{r.number} {r.isFinished ? '(Fechada)' : '(Aberta)'}</option>
                      ))}
                      {rounds.length === 0 && <option value="">Nenhuma rodada encontrada</option>}
                   </select>

                   <div className="flex gap-4">
                     <button onClick={() => setIsAnularModalOpen(false)} className="flex-1 py-2 rounded-lg bg-white/5 hover:bg-white/10 text-white font-bold transition-colors">
                       Pular
                     </button>
                     <button onClick={handleConfirmAnular} className="flex-1 py-2 rounded-lg bg-red-600 hover:bg-red-700 text-white font-black transition-colors" disabled={!selectedRound}>
                       ANULAR!
                     </button>
                   </div>
                </div>
              </motion.div>
            )}
          </AnimatePresence>

          <div className="flex flex-row flex-wrap gap-4 items-center">
             {/* Card 1: Mercado */}
             <div className="bg-black/40 border border-primary/20 px-4 py-3 rounded-xl flex flex-col justify-center min-w-[200px]">
                <div className="text-xs text-gray-400 mb-2 uppercase font-bold text-center">Mercado</div>
                {isMarketOpen ? (
                  <button onClick={() => toggleMarket(false)} className="w-full flex justify-center items-center gap-2 bg-red-500/20 hover:bg-red-500/30 text-red-500 border border-red-500/50 py-1.5 rounded-lg font-bold text-sm uppercase transition-all">
                    <Lock size={14} /> Fechar
                  </button>
                ) : (
                  <button onClick={() => toggleMarket(true)} className="w-full flex justify-center items-center gap-2 bg-green-500/20 hover:bg-green-500/30 text-green-500 border border-green-500/50 py-1.5 rounded-lg font-bold text-sm uppercase transition-all">
                    <Unlock size={14} /> Abrir
                  </button>
                )}
             </div>

             {/* Card 2: Upload de Elenco */}
             <div className="bg-black/40 border border-primary/20 px-4 py-3 rounded-xl flex flex-col justify-center min-w-[200px]">
                <div className="text-xs text-gray-400 mb-2 uppercase font-bold text-center">Elenco HTML</div>
                <label className="flex items-center justify-center w-full border border-primary/30 border-dashed rounded-lg cursor-pointer bg-primary/5 hover:bg-primary/10 transition-colors py-1.5 px-2">
                  <UploadCloud className="w-4 h-4 mr-2 text-primary" />
                  <span className="text-xs font-bold text-white uppercase">{isUploading ? '...' : 'Enviar'}</span>
                  <input type="file" className="hidden" accept=".html" onChange={(e) => handleUpload(e, 'PLANTEL')} />
                </label>
             </div>

             {/* Card 3: Upload de Resultados */}
             <div className="bg-black/40 border border-primary/20 px-4 py-3 rounded-xl flex flex-col justify-center min-w-[200px]">
                <div className="text-xs text-gray-400 mb-2 uppercase font-bold text-center">Partida HTML</div>
                <label className="flex items-center justify-center w-full border border-primary/30 border-dashed rounded-lg cursor-pointer bg-primary/5 hover:bg-primary/10 transition-colors py-1.5 px-2">
                  <BarChart3 className="w-4 h-4 mr-2 text-primary" />
                  <span className="text-xs font-bold text-white uppercase">{isUploading ? '...' : 'Enviar'}</span>
                  <input type="file" className="hidden" accept=".html" onChange={(e) => handleUpload(e, 'MATCH')} />
                </label>
             </div>

             {/* Botoes de Controle da Sessao */}
             <div className="flex flex-col gap-2 ml-auto">
               {matchResult && (
                 <button onClick={() => setIsResultModalOpen(true)} className="flex items-center justify-center w-full gap-2 bg-accent/20 hover:bg-accent/40 text-accent border border-accent/30 px-4 py-2 rounded-xl text-xs font-bold uppercase transition-all shadow-[0_0_15px_rgba(33,150,243,0.3)]">
                   <Eye size={16} /> Ver Último Upload
                 </button>
               )}
               <button onClick={handleOpenAnularModal} className="flex items-center justify-center w-full gap-2 bg-white/5 hover:bg-white/10 text-gray-400 border border-white/10 px-4 py-2 rounded-xl text-xs font-bold uppercase transition-all">
                 <Trash2 size={16} /> Anular Rodada
               </button>
               <button 
                 onClick={async () => {
                   if(!window.confirm('CERTEZA ABSOLUTA? Vai deletar TODOS os Históricos de Jogadores e Zerar Pontuação Total dos usuários! O elenco será mantido. Use isso SÓ antes da 1ª Rodada!')) return;
                   try {
                     const res = await fetch('/api/reidamesa/squads/reset-points', { method: 'POST', credentials: 'include' });
                     const text = await res.json();
                     alert(text.message || 'Reset realizado');
                   } catch(e) { console.error(e); }
                 }} 
                 className="flex items-center justify-center w-full gap-2 bg-red-500/10 hover:bg-red-500/20 text-red-500 border border-red-500/20 px-4 py-2 rounded-xl text-xs font-bold uppercase transition-all mt-2"
               >
                 <ShieldAlert size={16} /> Iniciar Nova Temp. (Resetar Histórico)
               </button>
             </div>
          </div>
        </div>

        {/* Tabela de edição de posição (Admin) */}
        <div className="bg-[#0b0f19] border border-white/10 rounded-xl overflow-hidden shadow-2xl flex flex-col">
            <div className="px-4 py-3 border-b border-white/5 flex justify-between items-center bg-[#0e1420]">
              <div className="flex items-center gap-4">
                <h3 className="font-bold text-lg text-white flex items-center gap-2">
                  Plantel Carregado
                </h3>
                <button onClick={handleDeleteAll} className="bg-red-600/20 hover:bg-red-600/40 text-red-400 border border-red-500/30 px-3 py-1 rounded-md text-[10px] font-bold uppercase transition-all flex items-center gap-1 cursor-pointer">
                  Zerar / Deletar Elenco Atual
                </button>
              </div>
              <div className="bg-primary/10 border border-primary/20 px-3 py-1 rounded-md text-primary font-bold text-xs uppercase tracking-wider">
                Jogadores: {allPlayers.length}
              </div>
            </div>
            
            <div className="w-full overflow-x-auto custom-scrollbar">
              <table className="w-full text-left font-medium text-[11px] text-gray-300 whitespace-nowrap">
                  <thead className="bg-[#131b2a] text-gray-400 uppercase text-[10px] tracking-wider border-b border-white/10">
                    <tr>
                        {orderedKeys.map((k) => {
                          if (k === 'Escolhido') {
                            return <th key={k} className="px-2 py-2 font-black text-accent bg-[#131b2a] min-w-[130px] border-r border-white/5">Definir Posição</th>;
                          }
                          if (k === 'Jogador') {
                            return <th key={k} className="px-3 py-2 font-black bg-[#131b2a] min-w-[160px] border-r border-white/5">Nome</th>;
                          }
                          return <th key={k} className="px-2 py-2 font-bold border-r border-white/5 last:border-0">{k}</th>;
                        })}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/5">
                    {allPlayers.map((p) => {
                      if (!p.rawStats) return null;
                      return (
                        <tr key={p.id} className="hover:bg-white/5 transition-colors group">
                          {orderedKeys.map((k) => {
                            const val = p.rawStats[k];
                            if (k === 'Escolhido') {
                              return (
                                <td key={k} className="px-2 py-1.5 bg-[#0b0f19] group-hover:bg-[#111726] border-r border-white/5 transition-colors">
                                  <div className="flex items-center gap-1">
                                    <select 
                                      value={p.cartolaRole || ""} 
                                      onChange={(e) => handleRoleChange(p.id, e.target.value)}
                                      className="bg-black/80 border border-white/20 hover:border-accent/50 text-white rounded-md p-1 text-[11px] font-bold outline-none cursor-pointer w-full"
                                    >
                                      <option value="" className="text-gray-500">-- Nenhuma --</option>
                                      <option value="DEF">Defensor  [DEF]</option>
                                      <option value="MEI">Meio-C.   [MEI]</option>
                                      <option value="ATA">Atacante  [ATA]</option>
                                    </select>
                                    {savingRows[p.id] && (
                                      <div className="w-3 h-3 rounded-full border-2 border-accent border-t-transparent animate-spin ml-1 shrink-0"></div>
                                    )}
                                  </div>
                                </td>
                              );
                            }
                            if (k === 'Jogador') {
                              return (
                                <td key={k} className="px-3 py-1.5 bg-[#0b0f19] group-hover:bg-[#111726] border-r border-white/5 font-bold text-white text-xs truncate max-w-[180px]">
                                  {p.name}
                                </td>
                              );
                            }
                            return (
                              <td key={k} className="px-2 py-1.5 text-gray-300 font-medium tracking-wide border-r border-white/5 last:border-0 text-center">
                                {val === '-' || !val ? <span className="opacity-30">-</span> : val}
                              </td>
                            );
                          })}
                        </tr>
                      );
                    })}
                  </tbody>
              </table>
            </div>
        </div>
      </div>
    </div>
  );
};

export default ReiDaMesaAdmin;
