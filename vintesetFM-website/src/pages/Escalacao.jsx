import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Users, AlertTriangle, Shield, TrendingUp, DollarSign, Search, Filter, ArrowRight, Save, Crown, X, Info, AlertCircle } from 'lucide-react';
import { Link } from 'react-router-dom';

const Escalacao = () => {
  // ==== ESTADOS ====
  const [formation, setFormation] = useState('4-4-2');
  const [budget, setBudget] = useState(100.0);
  const [activeSlot, setActiveSlot] = useState(null); // 'DEF', 'MEI', 'ATA', 'BAGRE'
  const [searchTerm, setSearchTerm] = useState('');
  const [squad, setSquad] = useState({
    def: null,
    mei: null,
    ata: null,
    bagre: null,
    roundScore: 0,
    totalScore: 0
  });
  const [isSaved, setIsSaved] = useState(false);
  const [isMarketOpen, setIsMarketOpen] = useState(true);
  const [players, setPlayers] = useState([]);

  const isSquadComplete = squad.def && squad.mei && squad.ata && squad.bagre;

  // Fetch init do backend
  useEffect(() => {
    // Busca status do mercado
    fetch('/api/reidamesa/status')
      .then(res => res.json())
      .then(data => setIsMarketOpen(data.isOpen))
      .catch(console.error);

    // Busca jogadores do pool
    fetch('/api/reidamesa/players')
      .then(res => res.json())
      .then(data => setPlayers(data))
      .catch(console.error);

    // Busca esquadrão salvo
    fetch('/api/reidamesa/squad', { credentials: 'include' })
      .then(res => res.json())
      .then(data => {
        if (data && data.defensor) {
          setSquad({
            def: data.defensor,
            mei: data.meio,
            ata: data.ataque,
            bagre: data.bagre,
            roundScore: data.roundScore,
            totalScore: data.totalScore
          });
          setIsSaved(true);
        }
      })
      .catch(console.error);
  }, []);

  // Filtra mercado baseado no slot ativo
  const filteredPlayers = players.filter(p => {
    // Busca por nome
    const matchesSearch = p.name.toLowerCase().includes(searchTerm.toLowerCase());
    
    let matchesPosition = true;
    if (activeSlot === 'DEF') matchesPosition = p.cartolaRole === 'DEF';
    if (activeSlot === 'MEI') matchesPosition = p.cartolaRole === 'MEI';
    if (activeSlot === 'ATA') matchesPosition = p.cartolaRole === 'ATA';
    // Se bank, pode ser qualquer um
    
    // Nao mostrar jogadores já escalados
    const isAlreadyPicked = (squad.def?.id === p.id || squad.mei?.id === p.id || squad.ata?.id === p.id || squad.bagre?.id === p.id);
    
    return matchesSearch && matchesPosition && !isAlreadyPicked;
  });

  const handlePickPlayer = (player) => {
    if (!activeSlot) return;

    if (activeSlot === 'DEF') setSquad({ ...squad, def: player });
    if (activeSlot === 'MEI') setSquad({ ...squad, mei: player });
    if (activeSlot === 'ATA') setSquad({ ...squad, ata: player });
    if (activeSlot === 'BAGRE') setSquad({ ...squad, bagre: player });

    // Avança automático para o próximo slot vazio ou fecha o mercado
    if (activeSlot === 'DEF' && !squad.mei) setActiveSlot('MEI');
    else if (activeSlot === 'MEI' && !squad.ata) setActiveSlot('ATA');
    else if (activeSlot === 'ATA' && !squad.bagre) setActiveSlot('BAGRE');
    else setActiveSlot(null);
  };

  const handleRemovePlayer = (slot) => {
    setSquad({ ...squad, [slot]: null });
    setActiveSlot(slot.toUpperCase());
    setIsSaved(false);
  };

  const saveSquadToBackend = async () => {
    const payload = {
      defensorId: squad.def?.id,
      meioId: squad.mei?.id,
      ataqueId: squad.ata?.id,
      bagreId: squad.bagre?.id
    };
    try {
      const res = await fetch('/api/reidamesa/squad', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(payload)
      });
      if (res.ok) {
        setIsSaved(true);
      } else {
        alert('Erro ao salvar. Verifique se o mercado está aberto.');
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleClearSquad = async () => {
    setSquad({ def: null, mei: null, ata: null, bagre: null });
    setIsSaved(false);
    setActiveSlot(null);

    try {
      await fetch('/api/reidamesa/squad', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({
          defensorId: null,
          meioId: null,
          ataqueId: null,
          bagreId: null
        })
      });
    } catch (err) {
      console.error('Erro ao limpar banco de dados.', err);
    }
  };

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        
        {/* Header */}
        <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-8 gap-4">
          <div>
            <h1 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3">
               <Shield className="text-accent" size={32} />
               Meu Esquadrão
            </h1>
            <div className="flex items-center gap-4 mt-2">
               <p className="text-gray-400">1 Defensor/GOL, 1 Meia, 1 Atacante, 1 Bagre.</p>
               {squad.roundScore !== undefined && squad.roundScore !== 0 && (
                  <span className="bg-white/10 px-3 py-1 rounded-full text-xs font-bold text-accent border border-white/5">
                     Última Rodada: {squad.roundScore.toFixed(2)} pts
                  </span>
               )}
            </div>
          </div>
          
          <div className="flex bg-black/50 p-1 rounded-lg border border-white/10 w-full md:w-auto gap-2">
            <button
               onClick={handleClearSquad}
               disabled={!isMarketOpen}
               className={`px-4 py-2 text-sm font-bold uppercase tracking-widest rounded transition-all text-center flex-1 border ${
                 isMarketOpen 
                   ? 'text-red-400 hover:text-white hover:bg-red-500/20 border-red-500/20' 
                   : 'text-gray-600 bg-black/50 border-gray-800 cursor-not-allowed opacity-50'
               }`}
            >
              Limpar Escalação
            </button>
            <Link to="/reidamesa" className="px-6 py-2 text-sm font-bold uppercase tracking-widest text-gray-400 hover:text-white hover:bg-white/5 rounded transition-all text-center flex-1 border border-transparent">
              Voltar ao Início
            </Link>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 h-full">
           
           {/* CAMPINHO (Visão do Elenco) */}
           <div className="lg:col-span-7 flex flex-col gap-4">
              <div className="w-full aspect-[4/5] sm:aspect-video lg:aspect-[3/4] rounded-3xl p-6 relative overflow-hidden flex flex-col items-center border-4 border-white/5 bg-green-900/40 shadow-2xl">
                 {/* Padrão do Gramado Analógico */}
                 <div className="absolute inset-0 opacity-20 pointer-events-none" style={{ backgroundImage: "repeating-linear-gradient(0deg, transparent, transparent 50px, rgba(255,255,255,0.05) 50px, rgba(255,255,255,0.05) 100px)" }}></div>
                 <div className="absolute top-0 w-1/2 h-32 border-4 border-t-0 border-white/20 rounded-b-[40px] pointer-events-none"></div>
                 <div className="absolute bottom-0 w-1/2 h-32 border-4 border-b-0 border-white/20 rounded-t-[40px] pointer-events-none"></div>
                 <div className="absolute top-1/2 left-0 w-full h-1 bg-white/20 -translate-y-1/2 pointer-events-none"></div>
                 <div className="absolute top-1/2 left-1/2 w-32 h-32 rounded-full border-4 border-white/20 -translate-x-1/2 -translate-y-1/2 pointer-events-none"></div>

                 {/* Tiers/Terços do campo para as posições */}
                 <div className="absolute top-0 left-0 w-full h-1/3 bg-blue-300/10 pointer-events-none border-b border-white/10"></div>
                 <div className="absolute top-[33.33%] left-0 w-full h-1/3 bg-yellow-300/10 pointer-events-none"></div>
                 <div className="absolute top-[2/3] left-0 w-full h-1/3 bg-red-300/10 pointer-events-none border-t border-white/10" style={{top: '66.66%'}}></div>

                 {/* Titulares: Flex Layout para as posições */}
                 <div className="flex-1 w-full flex flex-col justify-between py-12 z-10">
                    {/* ATA */}
                     <div className="w-full flex justify-center">
                        <SlotPlayer 
                          label="Atacante" 
                          type="ATA" 
                          player={squad.ata} 
                          isActive={activeSlot === 'ATA'} 
                          onClick={() => setActiveSlot('ATA')}
                          onRemove={() => handleRemovePlayer('ata')}
                          isMarketOpen={isMarketOpen}
                        />
                     </div>
                    
                    {/* MEI */}
                     <div className="w-full flex justify-center mt-[-2rem]">
                        <SlotPlayer 
                          label="Meio-Campo" 
                          type="MEI" 
                          player={squad.mei} 
                          isActive={activeSlot === 'MEI'} 
                          onClick={() => setActiveSlot('MEI')}
                          onRemove={() => handleRemovePlayer('mei')}
                          isMarketOpen={isMarketOpen}
                        />
                     </div>

                    {/* DEF */}
                     <div className="w-full flex justify-center mt-[-2rem]">
                        <SlotPlayer 
                          label="Defensor / GOL" 
                          type="DEF" 
                          player={squad.def} 
                          isActive={activeSlot === 'DEF'} 
                          onClick={() => setActiveSlot('DEF')}
                          onRemove={() => handleRemovePlayer('def')}
                          isMarketOpen={isMarketOpen}
                        />
                     </div>
                 </div>
              </div>

              {/* O BAGRE */}
              <div className="grid grid-cols-1 gap-4 w-full">
                 <div className="bg-gray-900 border border-white/10 rounded-2xl p-4 flex items-center gap-4 justify-center w-full max-w-sm mx-auto">
                    <div className="w-12 h-12 rounded-xl border border-dashed border-orange-500/50 bg-orange-500/10 flex shrink-0 items-center justify-center rotate-6 overflow-hidden">
                       <img src="/bagre-emote.png" alt="Bagre Emote" className="w-10 h-10 object-contain scale-125 filter drop-shadow-md" />
                    </div>
                    <SlotPlayer 
                      label="O Bagre" 
                      type="BAGRE" 
                      player={squad.bagre} 
                      isActive={activeSlot === 'BAGRE'} 
                      onClick={() => setActiveSlot('BAGRE')}
                      onRemove={() => handleRemovePlayer('bagre')}
                      horizontal
                      isMarketOpen={isMarketOpen}
                    />
                 </div>
              </div>

              {/* Ação de Confirmar */}
              <motion.button 
                initial={false}
                animate={isSaved ? { scale: [1, 1.05, 1], transition: { duration: 0.3 } } : {}}
                disabled={!isSquadComplete || isSaved || !isMarketOpen}
                onClick={saveSquadToBackend}
                className={`w-full py-4 rounded-xl flex items-center justify-center gap-2 font-black uppercase tracking-wider transition-all shadow-xl ${
                  isSaved 
                   ? 'bg-green-500 text-black shadow-[0_0_20px_rgba(34,197,94,0.4)]' 
                   : isSquadComplete 
                      ? 'bg-accent hover:bg-accentHover text-black shadow-[0_0_20px_rgba(255,215,0,0.3)] hover:scale-[1.02]' 
                      : 'bg-gray-800 text-gray-500 cursor-not-allowed border border-white/10'
                }`}
              >
                 {isSaved ? <Save size={20} /> : <Shield size={20} />}
                 {isSaved ? 'Escalação Confirmada!' : (isSquadComplete ? 'Confirmar Escalação' : 'Escalação Incompleta')}
              </motion.button>
           </div>

           {/* MERCADO DE JOGADORES */}
           <div className="lg:col-span-5 flex flex-col h-[700px]">
              <div className="bg-gray-900 border border-white/10 rounded-3xl p-6 flex flex-col h-full shadow-2xl relative overflow-hidden">
                
                {/* Cabeçalho do Mercado */}
                <div className="flex items-center justify-between mb-6 z-10">
                  <h3 className="font-black text-xl uppercase flex items-center gap-2">
                    <Users className="text-accent" /> Escolha do Plantel
                  </h3>
                  {activeSlot && (
                    <span className="bg-accent/20 text-accent text-xs font-bold px-3 py-1 rounded border border-accent/30">
                      Filtrando: {activeSlot}
                    </span>
                  )}
                </div>

                {/* Busca */}
                <div className="relative mb-6 z-10">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500" size={18} />
                  <input 
                    type="text"
                    placeholder="Buscar jogador..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="w-full bg-black/50 border border-white/10 rounded-xl py-3 pl-10 pr-4 text-sm text-white focus:outline-none focus:border-accent/50 transition-colors"
                  />
                </div>

                {/* Lista */}
                <div className="flex-1 overflow-y-auto space-y-3 pr-2 scrollbar-thin scrollbar-thumb-white/10 scrollbar-track-transparent z-10">
                   {!activeSlot && (
                     <div className="text-center py-10 bg-black/20 rounded-xl border border-white/5 border-dashed">
                       <Info className="mx-auto text-gray-500 mb-3" size={24} />
                       <p className="text-gray-400 text-sm">Clique em um slot no<br/>campo para abrir as opções.</p>
                     </div>
                   )}
                   
                   {activeSlot && filteredPlayers.length === 0 && (
                     <div className="text-center py-10 opacity-60">
                       <AlertCircle className="mx-auto text-gray-500 mb-3" size={24} />
                       <p className="text-sm">Nenhum jogador encontrado.</p>
                     </div>
                   )}

                   <AnimatePresence>
                     {activeSlot && filteredPlayers.map(player => (
                       <motion.div 
                         initial={{ opacity: 0, y: 10 }}
                         animate={{ opacity: 1, y: 0 }}
                         exit={{ opacity: 0, scale: 0.95 }}
                         key={player.id}
                         onClick={() => handlePickPlayer(player)}
                         className="flex items-center justify-between bg-black/40 border border-white/5 p-4 rounded-xl hover:bg-white/5 hover:border-accent/30 transition-all cursor-pointer group"
                       >
                         <div className="flex items-center gap-4">
                           <div className="w-12 h-12 rounded-full bg-gray-800 border border-white/10 flex items-center justify-center text-xs font-bold font-mono">
                             {player.cartolaRole || '-'}
                           </div>
                            <div>
                              <h4 className="font-bold text-sm text-white group-hover:text-accent transition-colors">{player.name}</h4>
                              <div className="flex items-center gap-2">
                                <p className="text-xs text-gray-400 font-mono">Idade: {player.age}</p>
                                {player.matchPoints !== null && player.matchPoints !== undefined && (
                                  <span className={`text-[10px] px-1.5 py-0.5 rounded font-bold ${player.matchPoints > 0 ? 'bg-green-500/20 text-green-400' : player.matchPoints < 0 ? 'bg-red-500/20 text-red-400' : 'bg-gray-700 text-gray-300'}`}>
                                    Atual: {player.matchPoints > 0 ? '+' : ''}{Number(player.matchPoints).toFixed(2)} pts
                                  </span>
                                )}
                                <span className={`text-[10px] px-1.5 py-0.5 rounded font-bold ${player.lastMatchPoints > 0 ? 'bg-blue-500/20 text-blue-400' : player.lastMatchPoints < 0 ? 'bg-orange-500/20 text-orange-400' : 'bg-gray-700 text-gray-300'}`}>
                                  Última: {player.lastMatchPoints > 0 ? '+' : ''}{Number(player.lastMatchPoints || 0).toFixed(2)} pts
                                </span>
                              </div>
                            </div>
                          </div>
                         <button className="w-8 h-8 rounded-full bg-accent text-black flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                           +
                         </button>
                       </motion.div>
                     ))}
                   </AnimatePresence>
                </div>
              </div>
           </div>

        </div>
      </div>
    </div>
  );
};

// Subcomponente de Slot no Campo
const SlotPlayer = ({ label, type, player, isActive, onClick, onRemove, horizontal = false, isMarketOpen = true }) => {
  return (
    <div className={`relative flex ${horizontal ? 'flex-row items-center justify-between w-full' : 'flex-col items-center justify-center'} gap-2 group`}>
      {!player ? (
        // Slot Vazio
        <motion.button
          onClick={onClick}
          animate={{ scale: isActive ? 1.05 : 1 }}
          className={`
            flex flex-col items-center justify-center border-2 border-dashed transition-all rounded-full bg-black/50 backdrop-blur-sm
            ${isActive ? 'border-accent shadow-[0_0_20px_rgba(255,215,0,0.5)] z-20' : 'border-white/30 hover:border-white/50 text-white/50 hover:text-white'}
            ${horizontal ? 'w-full max-w-[250px] h-16 px-4 rounded-xl flex-row justify-between border-dashed' : 'w-24 h-24 sm:w-28 sm:h-28'}
          `}
        >
          <span className="text-[10px] md:text-xs font-bold uppercase tracking-wider z-10 text-center">{horizontal ? (type === 'BAGRE' ? 'Escolher Bagre' : 'Escolher Bônus') : '+' + type}</span>
          {!horizontal && <span className="text-[10px] text-white/40 absolute bottom-3">{label}</span>}
        </motion.button>
      ) : (
        // Carta de Jogador Preenchida
          <div className={`
          relative flex flex-col items-center justify-center border transition-all rounded-full bg-black shadow-xl
          ${horizontal ? 'w-full max-w-[300px] h-16 px-4 rounded-xl border-accent/50 flex-row gap-4' : 'w-24 h-24 sm:w-28 sm:h-28 border-accent/50'}
        `}>
          {isMarketOpen && (
             <button 
               onClick={onRemove}
               className={`absolute ${horizontal ? 'right-2' : '-top-2 -right-2'} w-6 h-6 bg-red-500 rounded-full flex items-center justify-center text-white border-2 border-black opacity-0 group-hover:opacity-100 scale-75 group-hover:scale-100 transition-all z-20`}
             >
               <X size={12} />
             </button>
          )}
          
          <div className={`flex items-center justify-center text-accent/20 absolute inset-0 pt-2 ${horizontal ? 'hidden' : ''}`}>
             <Shield size={64} strokeWidth={1} />
          </div>
          
          <div className={`z-10 flex ${horizontal ? 'flex-row w-full justify-between items-center' : 'flex-col items-center text-center'} gap-1 p-2`}>
              {horizontal ? (
                <>
                  <div className="flex flex-col">
                    <span className="text-xs text-accent font-bold">{player.cartolaRole || '-'}</span>
                    <span className="font-black text-sm">{player.name}</span>
                  </div>
                  <div className="flex flex-wrap items-center gap-2 justify-end">
                     {player.matchPoints !== null && player.matchPoints !== undefined && (
                       <span className={`text-[10px] px-1.5 py-0.5 rounded font-bold ${player.matchPoints > 0 ? 'bg-green-500/20 text-green-400' : player.matchPoints < 0 ? 'bg-red-500/20 text-red-400' : 'bg-gray-700 text-gray-300'}`}>
                          Atual: {player.matchPoints > 0 ? '+' : ''}{Number(player.matchPoints).toFixed(2)} pts
                       </span>
                     )}
                     <span className={`text-[10px] px-1.5 py-0.5 rounded font-bold ${player.lastMatchPoints > 0 ? 'bg-blue-500/20 text-blue-400' : player.lastMatchPoints < 0 ? 'bg-orange-500/20 text-orange-400' : 'bg-gray-700 text-gray-300'}`}>
                        Última: {player.lastMatchPoints > 0 ? '+' : ''}{Number(player.lastMatchPoints || 0).toFixed(2)} pts
                     </span>
                  </div>
                </>
             ) : (
                <>
                 <span className="text-[10px] text-accent font-bold uppercase">{player.cartolaRole || '-'}</span>
                 <span className="text-xs sm:text-sm font-black uppercase leading-tight">{player.name}</span>
                 <div className="flex items-center justify-center gap-1 mt-1 flex-wrap">
                     {player.matchPoints !== null && player.matchPoints !== undefined && (
                       <span className={`text-[9px] px-1 rounded-sm font-bold ${player.matchPoints > 0 ? 'bg-green-500/20 text-green-400' : player.matchPoints < 0 ? 'bg-red-500/20 text-red-400' : 'bg-gray-700 text-gray-300'}`}>
                          At: {player.matchPoints > 0 ? '+' : ''}{Number(player.matchPoints).toFixed(2)}
                       </span>
                     )}
                     <span className={`text-[9px] px-1 rounded-sm font-bold ${player.lastMatchPoints > 0 ? 'bg-blue-500/20 text-blue-400' : player.lastMatchPoints < 0 ? 'bg-orange-500/20 text-orange-400' : 'bg-gray-700 text-gray-300'}`}>
                        Ul: {player.lastMatchPoints > 0 ? '+' : ''}{Number(player.lastMatchPoints || 0).toFixed(2)}
                     </span>
                 </div>
                </>
             )}
           </div>
        </div>
      )}
    </div>
  );
};

export default Escalacao;
