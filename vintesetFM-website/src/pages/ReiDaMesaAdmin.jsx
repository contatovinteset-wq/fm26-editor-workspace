import React, { useState, useEffect } from 'react';
import { ShieldAlert, BarChart3, UploadCloud, Lock, Unlock } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { Navigate } from 'react-router-dom';

const ReiDaMesaAdmin = () => {
  const { user } = useAuth();
  const isOwner = user?.roles?.includes('OWNER') || user?.roles?.includes('ADMIN_GERACAO') || user?.roles?.includes('ADMIN');

  const [isMarketOpen, setIsMarketOpen] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [allPlayers, setAllPlayers] = useState([]);
  const [savingRows, setSavingRows] = useState({});

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
        alert(`Upload Concluído! Backend Retornou: ${JSON.stringify(data)}`);
        
        if (type === 'PLANTEL') {
            const playersRes = await fetch('/api/reidamesa/players/all', { credentials: 'include' });
            if (playersRes.ok) {
                const refreshed = await playersRes.json();
                setAllPlayers(refreshed);
            }
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
            <h2 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3 mb-2">
              <ShieldAlert className="text-primary w-8 h-8" />
              Painel do Streamer - Rei da Mesa
            </h2>
          </div>

          <div className="flex flex-row gap-4 items-center">
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
