import React, { useEffect, useState } from 'react';
import { Crown, Medal, Flame, Shield, Trophy, Star, Zap, Award, Lock } from 'lucide-react';
import { rdmFetch } from '../services/reidamesa';

const ICONS = { crown: Crown, medal: Medal, flame: Flame, shield: Shield, trophy: Trophy, star: Star, zap: Zap };

// Cor do nível: bronze → prata → ouro → diamante.
const TIER_COLORS = ['#9ca3af', '#cd7f32', '#cbd5e1', '#facc15', '#22d3ee'];
const tierColor = (level) => TIER_COLORS[Math.min(level, TIER_COLORS.length - 1)];

const Badge = ({ c }) => {
  const Icon = ICONS[c.icon] || Award;
  const color = c.unlocked ? tierColor(c.level) : '#475569';
  const tiered = c.maxLevel > 1;
  const pct =
    c.goal != null && c.value != null
      ? Math.max(4, Math.min(100, Math.round((c.value / c.goal) * 100)))
      : c.unlocked ? 100 : 0;

  return (
    <div
      className={`relative rounded-2xl p-4 border flex flex-col gap-3 transition-colors ${
        c.unlocked ? 'bg-gray-900 border-white/10' : 'bg-black/40 border-white/5'
      }`}
    >
      <div className="flex items-start gap-3">
        <div
          className="shrink-0 w-12 h-12 rounded-xl flex items-center justify-center relative"
          style={{ backgroundColor: `${color}1f`, color }}
        >
          <Icon size={24} className={c.unlocked ? '' : 'opacity-50'} />
          {!c.unlocked && (
            <span className="absolute -bottom-1 -right-1 w-5 h-5 rounded-full bg-black border border-white/10 flex items-center justify-center">
              <Lock size={10} className="text-gray-500" />
            </span>
          )}
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <h4 className={`font-bold truncate ${c.unlocked ? 'text-white' : 'text-gray-500'}`}>{c.title}</h4>
            {c.maxed && <span className="text-[9px] font-black uppercase tracking-wider px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-400">MAX</span>}
          </div>
          {/* Estrelas de nível para conquistas com níveis */}
          {tiered && (
            <div className="flex gap-0.5 mt-1">
              {Array.from({ length: c.maxLevel }).map((_, i) => (
                <Star key={i} size={11} className={i < c.level ? '' : 'opacity-25'} style={{ color: i < c.level ? color : '#64748b' }} fill={i < c.level ? color : 'none'} />
              ))}
            </div>
          )}
        </div>
      </div>

      <p className="text-xs text-gray-400 leading-snug">{c.desc}</p>

      {/* Progresso até o próximo objetivo (só quando faz sentido) */}
      {c.goal != null && c.value != null && (
        <div>
          <div className="h-1.5 bg-white/5 rounded-full overflow-hidden">
            <div className="h-full rounded-full transition-all" style={{ width: `${pct}%`, backgroundColor: color }} />
          </div>
          <div className="text-[10px] text-gray-500 mt-1 font-mono">
            {c.value} / {c.goal}{tiered && c.level > 0 ? ` · próximo nível` : ''}
          </div>
        </div>
      )}
    </div>
  );
};

const ConquistasManager = () => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    rdmFetch('/api/reidamesa/conquistas', { credentials: 'include' })
      .then((r) => (r.ok ? r.json() : null))
      .then((d) => { setData(d); setLoading(false); })
      .catch(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="flex justify-center py-10">
        <span className="w-8 h-8 border-4 border-accent border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }
  if (!data) return null;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h3 className="text-xl font-bold uppercase tracking-widest flex items-center gap-2">
          <Award className="text-accent" size={20} /> Conquistas
        </h3>
        <span className="text-sm font-mono text-gray-400">
          <span className="text-accent font-bold">{data.summary.unlocked}</span> / {data.summary.total} desbloqueadas
        </span>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {data.conquistas.map((c) => <Badge key={c.key} c={c} />)}
      </div>
    </div>
  );
};

export default ConquistasManager;
