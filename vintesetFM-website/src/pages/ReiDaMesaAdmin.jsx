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

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16 overflow-hidden">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <h2 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3 mb-8">
          <ShieldAlert className="text-primary w-8 h-8" />
          Painel do Streamer - Rei da Mesa
        </h2>
        
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Card 1: Mercado */}
            <div className="bg-black/40 border border-primary/20 p-6 rounded-2xl flex flex-col justify-between">
              <div>
                <h3 className="font-bold text-lg mb-2 flex items-center gap-2">
                  Status do Mercado
                </h3>
                <p className="text-gray-400 text-sm mb-6">Controle se os viewers podem montar o esquadrão ou não.</p>
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
                  <input type="file" className="hidden" accept=".html" onChange={(e) => handleUpload(e, 'PLANTEL')} />
                </label>
              </div>
            </div>

            {/* Card 3: Upload de Resultados */}
            <div className="bg-black/40 border border-primary/20 p-6 rounded-2xl flex flex-col justify-between">
              <div>
                <h3 className="font-bold text-lg mb-2">2. Computar Resultados</h3>
                <p className="text-gray-400 text-sm mb-6">Carregue o HTML das estatísticas da partida para gerar a pontuação e rankear os viewers.</p>
              </div>
              <div>
                <label className="flex flex-col items-center justify-center w-full h-24 border-2 border-primary/30 border-dashed rounded-lg cursor-pointer bg-primary/5 hover:bg-primary/10 transition-colors">
                  <div className="flex flex-col items-center justify-center pt-5 pb-6">
                    <BarChart3 className="w-6 h-6 mb-2 text-primary" />
                    <p className="text-xs text-gray-400"><span className="font-bold text-white">{isUploading ? 'Processando HTML...' : 'Clique para enviar'}</span> html de estatísticas</p>
                  </div>
                  <input type="file" className="hidden" accept=".html" onChange={(e) => handleUpload(e, 'MATCH')} />
                </label>
              </div>
            </div>
        </div>

        {/* Tabela de edição de posição (Admin) */}
        <div className="mt-8 bg-[#0b0f19] border border-white/10 p-2 sm:p-6 rounded-2xl overflow-x-auto shadow-2xl">
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-6">
              <div>
                <h3 className="font-black text-xl text-white flex items-center gap-2">
                  Gerenciamento de Posições
                </h3>
                <p className="text-sm text-gray-400 mt-1">Sincronize a posição in-game com a posição válida no Rei da Mesa.</p>
              </div>
              <div className="mt-4 sm:mt-0 bg-primary/10 border border-primary/20 px-4 py-2 rounded-lg text-primary font-bold text-sm">
                Jogadores Cadastrados: {allPlayers.length}
              </div>
            </div>
            
            <div className="w-full overflow-x-auto max-h-[600px] overflow-y-auto custom-scrollbar border border-white/5 rounded-xl">
              <table className="w-full text-left font-medium text-sm text-gray-300 whitespace-nowrap">
                  <thead className="bg-[#131b2a] text-gray-400 uppercase text-[11px] tracking-wider sticky top-0 z-20 shadow-md">
                    <tr>
                        {allPlayers.length > 0 && allPlayers[0]?.rawStats && Object.keys(allPlayers[0].rawStats)
                          .map((k, index) => {
                            if (k === 'Inf') return null;
                            if (k === 'Escolhido') {
                              return <th key={k} className="px-5 py-4 font-black text-accent sticky left-0 z-30 bg-[#131b2a]">Definir Posição</th>;
                            }
                            if (k === 'Jogador') {
                              return <th key={k} className="px-5 py-4 font-black sticky left-[160px] z-30 bg-[#131b2a] shadow-[4px_0_10px_-5px_rgba(0,0,0,0.5)]">Jogador</th>;
                            }
                            return <th key={k} className="px-5 py-4 font-bold">{k}</th>;
                          })
                        }
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/5">
                    {allPlayers.map((p) => {
                      if (!p.rawStats) return null;
                      const keys = Object.keys(p.rawStats);
                      return (
                        <tr key={p.id} className="hover:bg-white/5 transition-colors group">
                          {keys.map((k) => {
                            if (k === 'Inf') return null;
                            if (k === 'Escolhido') {
                              return (
                                <td key={k} className="px-5 py-3 sticky left-0 z-10 bg-[#0b0f19] group-hover:bg-[#111726] transition-colors">
                                  <div className="flex items-center gap-2">
                                    <select 
                                      value={p.cartolaRole || ""} 
                                      onChange={(e) => handleRoleChange(p.id, e.target.value)}
                                      className="bg-black/50 border border-white/20 hover:border-accent/50 text-white rounded-lg p-2 text-sm outline-none font-bold transition-all min-w-[140px] focus:ring-2 focus:ring-accent/50 focus:border-accent cursor-pointer"
                                    >
                                      <option value="" className="text-gray-500">-- Nenhuma --</option>
                                      <option value="DEF">Defensor  [DEF]</option>
                                      <option value="MEI">Meio-C.   [MEI]</option>
                                      <option value="ATA">Atacante  [ATA]</option>
                                    </select>
                                    {savingRows[p.id] && (
                                      <span className="w-4 h-4 rounded-full border-2 border-accent border-t-transparent animate-spin ml-2"></span>
                                    )}
                                  </div>
                                </td>
                              );
                            }
                            if (k === 'Jogador') {
                              return (
                                <td key={k} className="px-5 py-3 sticky left-[160px] z-10 bg-[#0b0f19] group-hover:bg-[#111726] transition-colors shadow-[4px_0_10px_-5px_rgba(0,0,0,0.5)]">
                                  <div className="font-bold text-white text-base">{p.name}</div>
                                </td>
                              );
                            }
                            return (
                              <td key={k} className="px-5 py-3 text-gray-300 font-medium tracking-wide">
                                {p.rawStats[k] === '-' ? <span className="opacity-30">-</span> : p.rawStats[k] || <span className="opacity-30">-</span>}
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
