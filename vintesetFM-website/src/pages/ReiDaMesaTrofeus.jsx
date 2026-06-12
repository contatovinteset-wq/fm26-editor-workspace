import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Trophy, Crown, Star, Flame, ThumbsDown, ArrowLeft, Users, CalendarDays, Award } from 'lucide-react';
import { rdmFetch, useRdmBase } from '../services/reidamesa';

const Face = ({ uniqueId, name, size = 56 }) => {
  const [err, setErr] = useState(false);
  if (!uniqueId || err) {
    return (
      <div className="rounded-full bg-black/60 border border-white/15 flex items-center justify-center font-black text-gray-400" style={{ width: size, height: size }}>
        {(name || '?').charAt(0).toUpperCase()}
      </div>
    );
  }
  return (
    <img
      src={`https://sortitoutsi.b-cdn.net/uploads/face/face_${uniqueId}.png`}
      alt={name}
      onError={() => setErr(true)}
      className="rounded-full object-cover border border-white/15 bg-black/60"
      style={{ width: size, height: size }}
    />
  );
};

// Card de troféu genérico (jogador).
const TrophyCard = ({ icon: Icon, label, color, player, metric }) => (
  <div className="bg-gray-900 border border-white/10 rounded-2xl p-5 flex items-center gap-4 hover:border-white/20 transition-colors">
    <div className="shrink-0 w-11 h-11 rounded-xl flex items-center justify-center" style={{ backgroundColor: `${color}1f`, color }}>
      <Icon size={22} />
    </div>
    <div className="flex items-center gap-3 min-w-0 flex-1">
      {player ? (
        <>
          <Face uniqueId={player.uniqueId} name={player.name} size={48} />
          <div className="min-w-0">
            <div className="text-[10px] font-bold uppercase tracking-widest text-gray-500">{label}</div>
            <div className="font-bold text-white truncate">{player.name}</div>
            {metric && <div className="text-xs font-mono" style={{ color }}>{metric}</div>}
          </div>
        </>
      ) : (
        <div>
          <div className="text-[10px] font-bold uppercase tracking-widest text-gray-500">{label}</div>
          <div className="text-gray-600 text-sm">Ainda sem dados</div>
        </div>
      )}
    </div>
  </div>
);

const ReiDaMesaTrofeus = () => {
  const base = useRdmBase();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    rdmFetch('/api/reidamesa/trofeus')
      .then((r) => r.json())
      .then((d) => { setData(d); setLoading(false); })
      .catch((e) => { console.error(e); setLoading(false); });
  }, []);

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-8 gap-4">
          <div>
            <h1 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3">
              <Trophy className="text-accent" size={32} /> Sala de Troféus
            </h1>
            <p className="text-gray-400 mt-2">Os destaques do Rei da Mesa: líder, artilheiro e os favoritos da galera.</p>
          </div>
          <Link to={base} className="px-6 py-2 text-sm font-bold uppercase tracking-widest text-gray-400 hover:text-white border border-white/10 bg-black/50 rounded flex items-center gap-2">
            <ArrowLeft size={16} /> Voltar
          </Link>
        </div>

        {loading ? (
          <div className="flex justify-center py-20"><span className="w-10 h-10 border-4 border-accent border-t-transparent rounded-full animate-spin" /></div>
        ) : !data ? (
          <div className="text-center py-20 text-gray-500">Não foi possível carregar a Sala de Troféus.</div>
        ) : (
          <>
            {/* Líder — destaque */}
            <div className="relative overflow-hidden bg-gradient-to-br from-accent/20 to-transparent border border-accent/30 rounded-3xl p-6 mb-6">
              <div className="flex items-center gap-5">
                <div className="shrink-0 relative">
                  {data.lider?.avatar ? (
                    <img src={data.lider.avatar} alt="" className="w-20 h-20 rounded-full object-cover border-2 border-accent/60" />
                  ) : (
                    <div className="w-20 h-20 rounded-full bg-black/60 border-2 border-accent/60 flex items-center justify-center"><Crown className="text-accent" size={32} /></div>
                  )}
                  <Crown className="absolute -top-2 -right-1 text-accent drop-shadow" size={24} />
                </div>
                <div>
                  <div className="text-[11px] font-bold uppercase tracking-widest text-accent">Rei da Mesa (líder geral)</div>
                  <div className="text-2xl font-black text-white">{data.lider?.nickname || '—'}</div>
                  {data.lider && <div className="font-mono text-green-400 font-bold">{data.lider.totalScore} pts</div>}
                  {!data.lider && <div className="text-gray-500 text-sm">Ainda sem pontuação registrada.</div>}
                </div>
              </div>
            </div>

            {/* Troféus */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <TrophyCard icon={Star} label="Artilheiro do Rei da Mesa" color="#22c55e" player={data.artilheiro} metric={data.artilheiro ? `${data.artilheiro.points} pts no total` : null} />
              <TrophyCard icon={Flame} label="Mais escalado (titular)" color="#f59e0b" player={data.maisEscalado} metric={data.maisEscalado ? `${data.maisEscalado.count}x escalado` : null} />
              <TrophyCard icon={Crown} label="Capitão favorito" color="#a855f7" player={data.capitaoFavorito} metric={data.capitaoFavorito ? `${data.capitaoFavorito.count}x capitão` : null} />
              <TrophyCard icon={ThumbsDown} label="Bagre mais escalado" color="#ef4444" player={data.bagreMaisEscalado} metric={data.bagreMaisEscalado ? `${data.bagreMaisEscalado.count}x apostado` : null} />
              <TrophyCard icon={Award} label="Bagre da última rodada" color="#94a3b8" player={data.bagreUltimaRodada} metric={data.bagreUltimaRodada ? `Rodada ${data.bagreUltimaRodada.roundNumber}` : null} />
            </div>

            {/* Totais */}
            <div className="grid grid-cols-2 gap-4 mt-6">
              <div className="bg-gray-900 border border-white/10 rounded-2xl p-5 flex items-center gap-3">
                <Users className="text-accent" size={22} />
                <div><div className="text-2xl font-black text-white">{data.totals?.managers ?? 0}</div><div className="text-xs uppercase tracking-widest text-gray-500">Managers</div></div>
              </div>
              <div className="bg-gray-900 border border-white/10 rounded-2xl p-5 flex items-center gap-3">
                <CalendarDays className="text-accent" size={22} />
                <div><div className="text-2xl font-black text-white">{data.totals?.rounds ?? 0}</div><div className="text-xs uppercase tracking-widest text-gray-500">Rodadas</div></div>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default ReiDaMesaTrofeus;
