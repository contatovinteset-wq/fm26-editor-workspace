import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Users, Shield, Search, Save, Crown, X, Info, AlertCircle } from 'lucide-react';
import { Link } from 'react-router-dom';

// ─── Sub-componente: Foto do Jogador via Sortitoutsi ───────────────────────
const PlayerImage = ({ uniqueId, name, fallbackText, className }) => {
  const [error, setError] = useState(false);

  if (!uniqueId || error) {
    return (
      <div className={`flex items-center justify-center font-mono font-bold bg-gray-800 text-gray-400 ${className}`}>
        {fallbackText}
      </div>
    );
  }

  return (
    <img
      src={`https://sortitoutsi.b-cdn.net/uploads/face/face_${uniqueId}.png`}
      alt={name}
      className={`object-cover ${className}`}
      onError={() => setError(true)}
    />
  );
};

// ─── Sub-componente: Modal de Detalhe de Pontuação ────────────────────────
const ScoreDetailModal = ({ player, onClose }) => {
  if (!player) return null;
  let parsed = player.details;
  if (typeof parsed === 'string') {
    try { parsed = JSON.parse(parsed); } catch (e) { parsed = {}; }
  }
  if (!parsed) parsed = {};

  const multiplier = player.isCapitao ? 2 : (parsed.multiplier || 1);

  const rows = [
    { label: 'Minutos jogados', value: parsed.minsPlayed ? parsed.minsPlayed + "'" : "0'", points: (parsed.minsPlayed >= 60 ? 1.0 : (parsed.minsPlayed > 0 ? 0.5 : 0)) },
    { label: 'Gols marcados', value: parsed.goals || 0, points: (parsed.goals || 0) * 8.0 },
    { label: 'Assistências', value: parsed.assists || 0, points: (parsed.assists || 0) * 5.0 },
    { label: 'Gols Esperados (xG)', value: parsed.xG ? Number(parsed.xG).toFixed(2) : 0, points: (parsed.xG || 0) * 2.0 },
    { label: 'Assist. Esperadas (xA)', value: parsed.xA ? Number(parsed.xA).toFixed(2) : 0, points: (parsed.xA || 0) * 2.0 },
    { label: 'Oport. Flagrantes', value: parsed.chancesCriadas || 0, points: (parsed.chancesCriadas || 0) * 2.0 },
    { label: 'Passes Decisivos', value: parsed.passesDecisivos || parsed.keyPasses || 0, points: (parsed.passesDecisivos || parsed.keyPasses || 0) * 1.0 },
    { label: 'Fintas/Dribles', value: parsed.dribles || 0, points: (parsed.dribles || 0) * 0.5 },
    { label: 'Bateu na Barra', value: parsed.bateuBarra || 0, points: (parsed.bateuBarra || 0) * 1.5 },
    { label: 'Desarmes', value: parsed.desarmes || parsed.tackles || 0, points: (parsed.desarmes || parsed.tackles || 0) * 2.0 },
    { label: 'Intercepções', value: parsed.intercep || 0, points: (parsed.intercep || 0) * 0.5 },
    { label: 'Alívios', value: parsed.alivios || 0, points: (parsed.alivios || 0) * 0.2 },
    { label: 'Faltas Cometidas', value: parsed.faltasCom || 0, points: (parsed.faltasCom || 0) * -0.5 },
    { label: 'Defesas (Goleiro)', value: parsed.defesasGoleiro || parsed.saves || 0, points: (parsed.defesasGoleiro || parsed.saves || 0) * 1.5 },
    { label: 'Cartão Amarelo', value: parsed.yellowCars || parsed.yellowCards || parsed.yel || 0, points: (parsed.yellowCars || parsed.yellowCards || parsed.yel || 0) * -1.5 },
    { label: 'Cartão Vermelho', value: parsed.redCards || parsed.red || 0, points: (parsed.redCards || parsed.red || 0) * -3.0 },
  ].filter(r => r.value !== null && r.value !== 0 && r.value !== "0'" && r.value !== '0.00');

  const basePoints = parsed.total ? Number(parsed.total) : Number(player.matchPoints || 0);

  return (
    <div
      className="fixed inset-0 bg-black/75 backdrop-blur-sm z-50 flex items-center justify-center p-4"
      onClick={onClose}
    >
      <motion.div
        initial={{ scale: 0.9, opacity: 0 }}
        animate={{ scale: 1, opacity: 1 }}
        className="bg-gray-900 border border-white/20 rounded-2xl p-6 w-full max-w-sm shadow-2xl"
        onClick={e => e.stopPropagation()}
      >
        <div className="flex items-center justify-between mb-4">
          <div>
            <p className="text-xs text-gray-400 uppercase tracking-widest font-bold">Detalhe da Pontuação</p>
            <h3 className="font-black text-lg text-white flex items-center gap-2">
              {player.isCapitao && <Crown size={18} className="text-accent" />}
              {player.name}
            </h3>
          </div>
          <button
            onClick={onClose}
            className="w-8 h-8 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center transition-colors"
          >
            <X size={16} />
          </button>
        </div>
        <div className="divide-y divide-white/5 max-h-[300px] overflow-y-auto pr-2">
          {rows.length === 0 ? (
            <p className="text-sm text-gray-500 py-4 text-center">Nenhum dado de pontuação disponível.</p>
          ) : rows.map(r => (
            <div key={r.label} className="flex justify-between items-center py-2.5 text-sm">
              <span className="text-gray-400">{r.label}</span>
              <div className="flex items-center gap-3">
                <span className="font-bold text-white">{r.value}</span>
                {r.points !== undefined && r.points !== 0 && (
                  <span className={`text-[10px] px-2 py-0.5 rounded font-black ${r.points > 0 ? 'bg-green-500/20 text-green-400' : 'bg-red-500/20 text-red-500'}`}>
                    {r.points > 0 ? '+' : ''}{r.points.toFixed(2)} pts
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>
        <div className="mt-4 pt-4 border-t border-white/10 flex flex-col gap-2">
          {multiplier > 1 && (
            <>
              <div className="flex justify-between items-center">
                <span className="text-gray-400 text-sm">Pontuação Base</span>
                <span className={`font-bold text-sm ${basePoints > 0 ? 'text-green-500' : basePoints < 0 ? 'text-red-500' : 'text-gray-300'}`}>
                  {basePoints > 0 ? '+' : ''}{basePoints.toFixed(2)} pts
                </span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-gray-400 text-sm">Bônus de Capitão</span>
                <span className="font-bold text-sm text-accent">Ativo (x2)</span>
              </div>
              <div className="border-t border-white/5 my-1" />
            </>
          )}
          <div className="flex justify-between items-center">
            <span className="text-gray-400 text-sm font-bold">Total da Rodada</span>
            <span className="font-black text-2xl text-accent">
              {(basePoints * multiplier).toFixed(2)} pts
            </span>
          </div>
        </div>
      </motion.div>
    </div>
  );
};

// ─── Componente Principal ─────────────────────────────────────────────────
const Escalacao = () => {
  const [activeSlot, setActiveSlot] = useState(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [squad, setSquad] = useState({
    def: null,
    mei: null,
    ata: null,
    bagre: null,
    capitao: null,
    roundScore: 0,
    totalScore: 0
  });
  const [isSaved, setIsSaved] = useState(false);
  const [isMarketOpen, setIsMarketOpen] = useState(true);
  const [players, setPlayers] = useState([]);
  const [scoreModalPlayer, setScoreModalPlayer] = useState(null);

  const isSquadComplete = squad.def && squad.mei && squad.ata && squad.bagre;

  // ─── Fetch inicial ─────────────────────────────────────────────────
  useEffect(() => {
    fetch('/api/reidamesa/status')
      .then(res => res.json())
      .then(data => setIsMarketOpen(data.isOpen))
      .catch(console.error);

    fetch('/api/reidamesa/players')
      .then(res => res.json())
      .then(data => setPlayers(data))
      .catch(console.error);

    fetch('/api/reidamesa/squad', { credentials: 'include' })
      .then(res => res.json())
      .then(data => {
        if (data && data.defensor) {
          setSquad({
            def: data.defensor,
            mei: data.meio,
            ata: data.ataque,
            bagre: data.bagre,
            capitao: data.capitao,
            roundScore: data.roundScore,
            totalScore: data.totalScore
          });
          setIsSaved(true);
        }
      })
      .catch(console.error);
  }, []);

  // ─── Filtro do mercado ─────────────────────────────────────────────
  const filteredPlayers = players.filter(p => {
    const matchesSearch = p.name.toLowerCase().includes(searchTerm.toLowerCase());
    let matchesPosition = true;
    if (activeSlot === 'DEF') matchesPosition = p.cartolaRole === 'DEF';
    if (activeSlot === 'MEI') matchesPosition = p.cartolaRole === 'MEI';
    if (activeSlot === 'ATA') matchesPosition = p.cartolaRole === 'ATA';
    const isAlreadyPicked = (squad.def?.id === p.id || squad.mei?.id === p.id || squad.ata?.id === p.id || squad.bagre?.id === p.id);
    return matchesSearch && matchesPosition && !isAlreadyPicked;
  });

  // ─── Handlers ─────────────────────────────────────────────────────
  const handlePickPlayer = (player) => {
    if (!activeSlot) return;
    if (activeSlot === 'DEF') setSquad(prev => ({ ...prev, def: player }));
    if (activeSlot === 'MEI') setSquad(prev => ({ ...prev, mei: player }));
    if (activeSlot === 'ATA') setSquad(prev => ({ ...prev, ata: player }));
    if (activeSlot === 'BAGRE') setSquad(prev => ({ ...prev, bagre: player }));
    setIsSaved(false);
    if (activeSlot === 'DEF' && !squad.mei) setActiveSlot('MEI');
    else if (activeSlot === 'MEI' && !squad.ata) setActiveSlot('ATA');
    else if (activeSlot === 'ATA' && !squad.bagre) setActiveSlot('BAGRE');
    else setActiveSlot(null);
  };

  const handleRemovePlayer = (slot) => {
    const removingId = squad[slot]?.id;
    setSquad(prev => ({
      ...prev,
      [slot]: null,
      capitao: prev.capitao?.id === removingId ? null : prev.capitao
    }));
    setActiveSlot(slot.toUpperCase());
    setIsSaved(false);
  };

  const handleSetCapitao = (player) => {
    setSquad(prev => ({ ...prev, capitao: player }));
    setIsSaved(false);
  };

  const saveSquadToBackend = async () => {
    const payload = {
      defensorId: squad.def?.id,
      meioId: squad.mei?.id,
      ataqueId: squad.ata?.id,
      bagreId: squad.bagre?.id,
      capitaoId: squad.capitao?.id
    };
    try {
      const res = await fetch('/api/reidamesa/squad', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(payload)
      });
      if (res.ok) setIsSaved(true);
    } catch (err) {
      console.error('Erro ao salvar escalação.', err);
    }
  };

  const handleClearSquad = async () => {
    setSquad({ def: null, mei: null, ata: null, bagre: null, capitao: null });
    setIsSaved(false);
    setActiveSlot(null);
    try {
      await fetch('/api/reidamesa/squad', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ defensorId: null, meioId: null, ataqueId: null, bagreId: null, capitaoId: null })
      });
    } catch (err) {
      console.error('Erro ao limpar escalação.', err);
    }
  };

  // ─── Render ────────────────────────────────────────────────────────
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
              <p className="text-gray-400">1 Defensor/GOL · 1 Meia · 1 Atacante · 1 Bagre</p>
              {squad.roundScore !== undefined && squad.roundScore !== 0 && (
                <span className="bg-white/10 px-3 py-1 rounded-full text-xs font-bold text-accent border border-white/5">
                  Última Rodada: {Number(squad.roundScore).toFixed(2)} pts
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
              Limpar
            </button>
            <Link to="/reidamesa" className="px-6 py-2 text-sm font-bold uppercase tracking-widest text-gray-400 hover:text-white hover:bg-white/5 rounded transition-all text-center flex-1 border border-transparent">
              Voltar
            </Link>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 h-full">

          {/* ── CAMPINHO ─────────────────────────────────────────── */}
          <div className="lg:col-span-7 flex flex-col gap-4">
            <div className="w-full aspect-[4/5] sm:aspect-video lg:aspect-[3/4] rounded-3xl p-6 relative overflow-hidden flex flex-col items-center border-4 border-white/5 bg-green-900/40 shadow-2xl">
              {/* Gramado */}
              <div className="absolute inset-0 opacity-20 pointer-events-none" style={{ backgroundImage: "repeating-linear-gradient(0deg, transparent, transparent 50px, rgba(255,255,255,0.05) 50px, rgba(255,255,255,0.05) 100px)" }} />
              <div className="absolute top-0 w-1/2 h-32 border-4 border-t-0 border-white/20 rounded-b-[40px] pointer-events-none" />
              <div className="absolute bottom-0 w-1/2 h-32 border-4 border-b-0 border-white/20 rounded-t-[40px] pointer-events-none" />
              <div className="absolute top-1/2 left-0 w-full h-1 bg-white/20 -translate-y-1/2 pointer-events-none" />
              <div className="absolute top-1/2 left-1/2 w-32 h-32 rounded-full border-4 border-white/20 -translate-x-1/2 -translate-y-1/2 pointer-events-none" />
              {/* Zonas coloridas */}
              <div className="absolute top-0 left-0 w-full h-1/3 bg-blue-300/10 pointer-events-none border-b border-white/10" />
              <div className="absolute top-[33.33%] left-0 w-full h-1/3 bg-yellow-300/10 pointer-events-none" />
              <div className="absolute left-0 w-full h-1/3 bg-red-300/10 pointer-events-none border-t border-white/10" style={{ top: '66.66%' }} />

              {/* Posições */}
              <div className="flex-1 w-full flex flex-col justify-between py-12 z-10">
                {/* ATA */}
                <div className="w-full flex justify-center">
                  <SlotPlayer label="Atacante" type="ATA" player={squad.ata} isActive={activeSlot === 'ATA'}
                    onClick={() => setActiveSlot('ATA')} onRemove={() => handleRemovePlayer('ata')}
                    isMarketOpen={isMarketOpen} isCapitao={squad.capitao?.id === squad.ata?.id}
                    onScoreClick={() => setScoreModalPlayer({ ...squad.ata, isCapitao: squad.capitao?.id === squad.ata?.id })} />
                </div>
                {/* MEI */}
                <div className="w-full flex justify-center mt-[-2rem]">
                  <SlotPlayer label="Meio-Campo" type="MEI" player={squad.mei} isActive={activeSlot === 'MEI'}
                    onClick={() => setActiveSlot('MEI')} onRemove={() => handleRemovePlayer('mei')}
                    isMarketOpen={isMarketOpen} isCapitao={squad.capitao?.id === squad.mei?.id}
                    onScoreClick={() => setScoreModalPlayer({ ...squad.mei, isCapitao: squad.capitao?.id === squad.mei?.id })} />
                </div>
                {/* DEF */}
                <div className="w-full flex justify-center mt-[-2rem]">
                  <SlotPlayer label="Defensor / GOL" type="DEF" player={squad.def} isActive={activeSlot === 'DEF'}
                    onClick={() => setActiveSlot('DEF')} onRemove={() => handleRemovePlayer('def')}
                    isMarketOpen={isMarketOpen} isCapitao={squad.capitao?.id === squad.def?.id}
                    onScoreClick={() => setScoreModalPlayer({ ...squad.def, isCapitao: squad.capitao?.id === squad.def?.id })} />
                </div>
              </div>
            </div>

            {/* BAGRE */}
            <div className="bg-gray-900 border border-white/10 rounded-2xl p-4 flex items-center gap-4 justify-center w-full max-w-sm mx-auto">
              <div className="w-12 h-12 rounded-xl border border-dashed border-orange-500/50 bg-orange-500/10 flex shrink-0 items-center justify-center rotate-6 overflow-hidden">
                <img src="/bagre-emote.png" alt="Bagre Emote" className="w-10 h-10 object-contain scale-125 filter drop-shadow-md" />
              </div>
              <SlotPlayer label="O Bagre" type="BAGRE" player={squad.bagre} isActive={activeSlot === 'BAGRE'}
                onClick={() => setActiveSlot('BAGRE')} onRemove={() => handleRemovePlayer('bagre')}
                horizontal isMarketOpen={isMarketOpen}
                onScoreClick={() => setScoreModalPlayer({ ...squad.bagre, isCapitao: false })} />
            </div>

            {/* ── SELETOR DE CAPITÃO ─────────────────────────── */}
            {isSquadComplete && isMarketOpen && (
              <div className="bg-gray-900 border border-accent/30 rounded-2xl p-4 w-full">
                <div className="flex items-center gap-2 mb-3">
                  <Crown size={16} className="text-accent" />
                  <span className="text-xs font-bold uppercase tracking-widest text-accent">Escolher Capitão (x2 pts)</span>
                </div>
                <div className="flex gap-2 flex-wrap">
                  {[{ slot: 'def', label: 'DEF' }, { slot: 'mei', label: 'MEI' }, { slot: 'ata', label: 'ATA' }].map(({ slot, label }) => {
                    const p = squad[slot];
                    if (!p) return null;
                    const isCapt = squad.capitao?.id === p.id;
                    return (
                      <button
                        key={slot}
                        onClick={() => handleSetCapitao(isCapt ? null : p)}
                        className={`flex items-center gap-1.5 px-3 py-2 rounded-xl border text-xs font-bold transition-all ${
                          isCapt
                            ? 'bg-accent text-black border-accent shadow-[0_0_12px_rgba(255,215,0,0.4)]'
                            : 'bg-black/40 border-white/10 text-gray-300 hover:border-accent/50 hover:text-white'
                        }`}
                      >
                        {isCapt && <Crown size={12} />}
                        <span>{label}: {p.name}</span>
                      </button>
                    );
                  })}
                </div>
                {squad.capitao && (
                  <p className="text-[11px] text-gray-400 mt-2">
                    <strong className="text-accent">{squad.capitao.name}</strong> terá sua pontuação dobrada na rodada.
                  </p>
                )}
              </div>
            )}

            {/* Botão Confirmar */}
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

          {/* ── MERCADO DE JOGADORES ──────────────────────────────── */}
          <div className="lg:col-span-5 flex flex-col h-[700px]">
            <div className="bg-gray-900 border border-white/10 rounded-3xl p-6 flex flex-col h-full shadow-2xl relative overflow-hidden">

              {/* Cabeçalho */}
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
                        <PlayerImage
                          uniqueId={player.uniqueId}
                          name={player.name}
                          fallbackText={player.cartolaRole || '-'}
                          className="w-12 h-12 rounded-full border border-white/10 overflow-hidden flex-shrink-0"
                        />
                        <div>
                          <h4 className="font-bold text-sm text-white group-hover:text-accent transition-colors">{player.name}</h4>
                          <div className="flex items-center gap-2 mt-0.5">
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
                      <button className="w-8 h-8 rounded-full bg-accent text-black flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">+</button>
                    </motion.div>
                  ))}
                </AnimatePresence>
              </div>
            </div>
          </div>

        </div>
      </div>

      {/* Modal de Detalhe de Pontuação */}
      {scoreModalPlayer && (
        <ScoreDetailModal player={scoreModalPlayer} onClose={() => setScoreModalPlayer(null)} />
      )}
    </div>
  );
};

// ─── Sub-componente: Slot no Campo ────────────────────────────────────────
const SlotPlayer = ({ label, type, player, isActive, onClick, onRemove, horizontal = false, isMarketOpen = true, isCapitao = false, onScoreClick }) => {
  const displayPoints = player?.matchPoints !== undefined && player?.matchPoints !== null 
    ? player.matchPoints * (isCapitao ? 2 : 1) 
    : null;

  return (
    <div className={`relative flex ${horizontal ? 'flex-row items-center justify-between w-full' : 'flex-col items-center justify-center'} gap-2 group`}>
      {!player ? (
        <motion.button
          onClick={onClick}
          animate={{ scale: isActive ? 1.05 : 1 }}
          className={`
            flex flex-col items-center justify-center border-2 border-dashed transition-all rounded-full bg-black/50 backdrop-blur-sm
            ${isActive ? 'border-accent shadow-[0_0_20px_rgba(255,215,0,0.5)] z-20' : 'border-white/30 hover:border-white/50 text-white/50 hover:text-white'}
            ${horizontal ? 'w-full max-w-[250px] h-16 px-4 rounded-xl flex-row justify-between border-dashed' : 'w-24 h-24 sm:w-28 sm:h-28'}
          `}
        >
          <span className="text-[10px] md:text-xs font-bold uppercase tracking-wider z-10 text-center">
            {horizontal ? (type === 'BAGRE' ? 'Escolher Bagre' : 'Escolher Bônus') : '+' + type}
          </span>
          {!horizontal && <span className="text-[10px] text-white/40 absolute bottom-3">{label}</span>}
        </motion.button>
      ) : horizontal ? (
        <div className={`
          relative flex items-center justify-between border transition-all bg-black shadow-xl
          ${isCapitao ? 'border-accent shadow-[0_0_16px_rgba(255,215,0,0.5)]' : 'border-accent/50'}
          w-full max-w-[300px] h-16 px-4 rounded-xl flex-row gap-4
        `}>
          {isMarketOpen && (
            <button
              onClick={onRemove}
              className="absolute right-2 w-6 h-6 bg-red-500 rounded-full flex items-center justify-center text-white border-2 border-black opacity-0 group-hover:opacity-100 scale-75 group-hover:scale-100 transition-all z-20"
            >
              <X size={12} />
            </button>
          )}

          <div className="z-10 flex flex-row w-full justify-between items-center gap-1 p-2">
            <div className="flex items-center gap-3">
              <PlayerImage
                uniqueId={player.uniqueId}
                name={player.name}
                fallbackText={player.cartolaRole || '-'}
                className="w-10 h-10 rounded-full border border-white/10 overflow-hidden flex-shrink-0 bg-gray-800"
              />
              <div className="flex flex-col">
                <span className="text-xs text-accent font-bold">{player.cartolaRole || '-'}</span>
                <span className="font-black text-sm">{player.name}</span>
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-2 justify-end">
              {displayPoints !== null && (
                <button
                  onClick={(e) => { e.stopPropagation(); onScoreClick && onScoreClick(); }}
                  className={`text-[10px] px-1.5 py-0.5 rounded font-bold hover:opacity-80 transition-opacity ${displayPoints > 0 ? 'bg-green-500/20 text-green-400' : displayPoints < 0 ? 'bg-red-500/20 text-red-400' : 'bg-gray-700 text-gray-300'}`}>
                  Atual: {displayPoints > 0 ? '+' : ''}{Number(displayPoints).toFixed(2)} pts 🔍
                </button>
              )}
              <span className={`text-[10px] px-1.5 py-0.5 rounded font-bold ${player.lastMatchPoints > 0 ? 'bg-blue-500/20 text-blue-400' : player.lastMatchPoints < 0 ? 'bg-orange-500/20 text-orange-400' : 'bg-gray-700 text-gray-300'}`}>
                Última: {player.lastMatchPoints > 0 ? '+' : ''}{Number(player.lastMatchPoints || 0).toFixed(2)} pts
              </span>
            </div>
          </div>
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center relative w-[120px]">
          {/* Círculo do Jogador */}
          <div className={`
            relative flex items-center justify-center border-2 transition-all rounded-full bg-black shadow-xl overflow-visible z-10
            ${isCapitao ? 'border-accent shadow-[0_0_16px_rgba(255,215,0,0.5)]' : 'border-accent/50'}
            w-20 h-20 sm:w-24 sm:h-24
          `}>
             {/* Badge de Capitão */}
             {isCapitao && (
               <div className="absolute -top-3 left-1/2 -translate-x-1/2 bg-accent text-black text-[9px] font-black px-2 py-0.5 rounded-full flex items-center gap-1 z-20 shadow-md">
                 <Crown size={9} /> CAP
               </div>
             )}

             {isMarketOpen && (
               <button
                 onClick={onRemove}
                 className="absolute -top-1 -right-1 w-6 h-6 bg-red-500 rounded-full flex items-center justify-center text-white border-2 border-black opacity-0 group-hover:opacity-100 scale-75 group-hover:scale-100 transition-all z-20"
               >
                 <X size={12} />
               </button>
             )}

             <div className="absolute inset-0 overflow-hidden rounded-full flex items-center justify-center bg-gray-900 border-[3px] border-black">
               <PlayerImage
                 uniqueId={player.uniqueId}
                 name={player.name}
                 fallbackText={<Shield size={40} strokeWidth={1} className="text-gray-600" />}
                 className="w-full h-full object-cover opacity-90"
               />
             </div>
          </div>

          {/* Plaquinha de Informação em baixo */}
          <div className="bg-gray-900/90 backdrop-blur-sm border border-white/10 rounded-lg px-2 py-1 mt-[-10px] z-20 flex flex-col items-center text-center shadow-lg w-full max-w-[140px]">
             <span className="text-[10px] text-accent font-bold uppercase tracking-widest">{player.cartolaRole || '-'}</span>
             <span className="text-[11px] sm:text-xs font-black uppercase leading-tight truncate w-full" title={player.name}>{player.name}</span>
             
             <div className="flex items-center justify-center gap-1 mt-1 flex-wrap w-full">
               {displayPoints !== null && (
                 <button
                   onClick={(e) => { e.stopPropagation(); onScoreClick && onScoreClick(); }}
                   className={`text-[9px] px-1 rounded-sm font-bold shadow-sm hover:opacity-80 transition-opacity whitespace-nowrap ${displayPoints > 0 ? 'bg-green-500 text-black' : displayPoints < 0 ? 'bg-red-500 text-white' : 'bg-gray-700 text-white'}`}>
                   {displayPoints > 0 ? '+' : ''}{Number(displayPoints).toFixed(2)} 🔍
                 </button>
               )}
               <span className={`text-[9px] px-1 rounded-sm font-bold shadow-sm whitespace-nowrap ${player.lastMatchPoints > 0 ? 'bg-blue-600/80 text-white' : player.lastMatchPoints < 0 ? 'bg-orange-600/80 text-white' : 'bg-gray-800 text-gray-300'}`}>
                 Ul: {player.lastMatchPoints > 0 ? '+' : ''}{Number(player.lastMatchPoints || 0).toFixed(2)}
               </span>
             </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Escalacao;
