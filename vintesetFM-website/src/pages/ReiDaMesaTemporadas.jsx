import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Calendar, Crown, Trophy, ArrowLeft, Medal, Flag, Loader2 } from 'lucide-react';
import { rdmFetch, useRdmBase } from '../services/reidamesa';

const medalColor = (i) => (i === 0 ? '#facc15' : i === 1 ? '#cbd5e1' : i === 2 ? '#d97706' : '#64748b');

const StandingRow = ({ row, index }) => (
  <div className="flex items-center gap-3 px-4 py-3 border-b border-white/5 last:border-0 hover:bg-white/[0.03]">
    <div className="w-8 text-center font-black" style={{ color: medalColor(index) }}>
      {index + 1}
    </div>
    {row.avatar ? (
      <img src={row.avatar} alt="" className="w-9 h-9 rounded-full object-cover border border-white/10" />
    ) : (
      <div className="w-9 h-9 rounded-full bg-black/60 border border-white/10 flex items-center justify-center font-black text-gray-400">
        {(row.nickname || '?').charAt(0).toUpperCase()}
      </div>
    )}
    <div className="min-w-0 flex-1">
      <div className="font-bold text-white truncate">{row.nickname}</div>
      <div className="text-[11px] text-gray-500">{row.rounds} {row.rounds === 1 ? 'rodada' : 'rodadas'}</div>
    </div>
    <div className="font-mono font-bold text-green-400">{row.score} pts</div>
  </div>
);

const ReiDaMesaTemporadas = () => {
  const base = useRdmBase();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [canManage, setCanManage] = useState(false);
  const [closing, setClosing] = useState(false);

  const load = () => {
    setLoading(true);
    Promise.all([
      rdmFetch('/api/reidamesa/seasons').then((r) => r.json()),
      rdmFetch('/api/reidamesa/lineup-print/permissions', { credentials: 'include' })
        .then((r) => (r.ok ? r.json() : { canManage: false }))
        .catch(() => ({ canManage: false })),
    ])
      .then(([d, perm]) => {
        setData(d);
        setCanManage(!!perm.canManage);
        setLoading(false);
      })
      .catch((e) => { console.error(e); setLoading(false); });
  };

  useEffect(() => { load(); /* eslint-disable-next-line */ }, []);

  const closeSeason = async () => {
    const n = data?.active?.number;
    if (!window.confirm(`Encerrar a Temporada ${n}? O líder atual será coroado campeão e uma nova temporada começa do zero.`)) return;
    setClosing(true);
    try {
      const res = await rdmFetch('/api/reidamesa/seasons/close', { method: 'POST', credentials: 'include' });
      if (!res.ok) { const d = await res.json().catch(() => ({})); throw new Error(d.error || 'Falha ao encerrar'); }
      const r = await res.json();
      alert(r.champion ? `🏆 Temporada ${r.closedNumber} encerrada! Campeão: ${r.champion.name} (${r.champion.score} pts). Temporada ${r.nextNumber} aberta.` : `Temporada ${r.closedNumber} encerrada (sem participantes). Temporada ${r.nextNumber} aberta.`);
      load();
    } catch (err) { alert(err.message); }
    finally { setClosing(false); }
  };

  const standings = data?.standings || [];
  const hall = data?.hallOfFame || [];

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-8 gap-4">
          <div>
            <h1 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3">
              <Calendar className="text-accent" size={32} /> Temporadas
            </h1>
            <p className="text-gray-400 mt-2">A corrida pelo título da temporada. Tudo zera quando uma nova começa.</p>
          </div>
          <Link to={base} className="px-6 py-2 text-sm font-bold uppercase tracking-widest text-gray-400 hover:text-white border border-white/10 bg-black/50 rounded flex items-center gap-2">
            <ArrowLeft size={16} /> Voltar
          </Link>
        </div>

        {loading ? (
          <div className="flex justify-center py-20"><span className="w-10 h-10 border-4 border-accent border-t-transparent rounded-full animate-spin" /></div>
        ) : !data ? (
          <div className="text-center py-20 text-gray-500">Não foi possível carregar as Temporadas.</div>
        ) : (
          <>
            {/* Temporada ativa + classificação */}
            <div className="bg-gradient-to-br from-accent/15 to-transparent border border-accent/30 rounded-3xl overflow-hidden mb-6">
              <div className="flex items-center justify-between gap-4 px-5 py-4 border-b border-white/10">
                <div className="flex items-center gap-3">
                  <Flag className="text-accent" size={22} />
                  <div>
                    <div className="text-[11px] font-bold uppercase tracking-widest text-accent">Temporada em andamento</div>
                    <div className="text-xl font-black text-white">{data.active?.name || `Temporada ${data.active?.number}`}</div>
                  </div>
                </div>
                {canManage && (
                  <button
                    onClick={closeSeason}
                    disabled={closing}
                    className="shrink-0 px-4 py-2 text-xs font-bold uppercase tracking-widest rounded bg-accent text-black hover:brightness-110 disabled:opacity-50 flex items-center gap-2"
                  >
                    {closing ? <Loader2 size={14} className="animate-spin" /> : <Trophy size={14} />}
                    Encerrar temporada
                  </button>
                )}
              </div>

              {standings.length === 0 ? (
                <div className="px-5 py-10 text-center text-gray-500 text-sm">Ainda sem pontuação nesta temporada. Processe uma rodada para começar a corrida.</div>
              ) : (
                <div>
                  {standings.map((row, i) => <StandingRow key={row.userId} row={row} index={i} />)}
                </div>
              )}
            </div>

            {/* Hall da Fama — campeões de temporadas passadas */}
            <h2 className="text-sm font-black uppercase tracking-widest text-gray-400 mb-3 flex items-center gap-2">
              <Crown size={16} className="text-accent" /> Hall da Fama
            </h2>
            {hall.length === 0 ? (
              <div className="bg-gray-900 border border-white/10 rounded-2xl px-5 py-8 text-center text-gray-600 text-sm">
                Nenhuma temporada encerrada ainda. O primeiro campeão entra pra história quando você encerrar a temporada atual.
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {hall.map((s) => (
                  <div key={s.number} className="bg-gray-900 border border-white/10 rounded-2xl p-5 flex items-center gap-4">
                    <div className="shrink-0 w-11 h-11 rounded-xl flex items-center justify-center bg-amber-500/15 text-amber-400"><Medal size={22} /></div>
                    <div className="min-w-0">
                      <div className="text-[10px] font-bold uppercase tracking-widest text-gray-500">{s.name || `Temporada ${s.number}`}</div>
                      <div className="font-bold text-white truncate flex items-center gap-2"><Crown size={14} className="text-amber-400 shrink-0" /> {s.championName || '—'}</div>
                      {s.championScore != null && <div className="text-xs font-mono text-amber-400">{s.championScore} pts</div>}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
};

export default ReiDaMesaTemporadas;
