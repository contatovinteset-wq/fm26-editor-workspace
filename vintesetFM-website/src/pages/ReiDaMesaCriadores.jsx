import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Crown, Users, ArrowRight, Settings, Twitch, Youtube, Radio } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

// Plataformas suportadas (logo + cor + qual ícone).
const PLATFORMS = [
  { key: 'twitch', label: 'Twitch', color: '#9146FF' },
  { key: 'kick', label: 'Kick', color: '#53FC18' },
  { key: 'youtube', label: 'YouTube', color: '#FF0000' },
];

const PlatformIcon = ({ pKey, size = 16 }) => {
  if (pKey === 'twitch') return <Twitch size={size} />;
  if (pKey === 'youtube') return <Youtube size={size} />;
  // Kick não tem ícone no lucide — usa o monograma.
  return <span style={{ fontSize: size - 2, fontWeight: 900, lineHeight: 1 }}>K</span>;
};

// Diretório público dos criadores do Rei da Mesa (Fase 3c).
// Lista todos os criadores ativos (incluindo o vinteset). O card do vinteset
// leva ao /reidamesa bare (flagship); os demais a /reidamesa/c/:slug.
const ReiDaMesaCriadores = () => {
  const { user } = useAuth();
  const isOwner = user?.roles?.includes('OWNER');
  const [creators, setCreators] = useState([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetch('/api/reidamesa/creators')
      .then((res) => res.json())
      .then((data) => { setCreators(Array.isArray(data) ? data : []); setIsLoading(false); })
      .catch((err) => { console.error(err); setIsLoading(false); });
  }, []);

  const pathFor = (slug) => (slug === 'vinteset' ? '/reidamesa' : `/reidamesa/c/${slug}`);

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">

        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-3xl md:text-4xl font-black uppercase tracking-tight flex items-center justify-center gap-3">
            <Users className="text-accent" size={36} />
            Criadores do Rei da Mesa
          </h1>
          <p className="text-gray-400 mt-3 max-w-2xl mx-auto">
            Cada criador tem o seu próprio Rei da Mesa, com elenco e ranking independentes.
            Escolha um e jogue junto com a comunidade dele.
          </p>
          {isOwner && (
            <Link to="/reidamesa/admin/criadores" className="mt-5 inline-flex items-center gap-2 px-4 py-2 rounded-lg font-bold uppercase text-xs tracking-widest bg-white/10 hover:bg-white/20 border border-white/20 transition">
              <Settings size={14} /> Gerenciar criadores
            </Link>
          )}
        </div>

        {isLoading ? (
          <div className="flex justify-center py-20">
            <span className="w-10 h-10 border-4 border-accent border-t-transparent rounded-full animate-spin"></span>
          </div>
        ) : creators.length === 0 ? (
          <div className="text-center py-20 bg-gray-900/50 rounded-2xl border border-white/5">
            <Crown className="w-12 h-12 text-gray-500 mx-auto mb-4" />
            <h3 className="text-xl font-bold text-gray-300">Nenhum criador ativo no momento</h3>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {creators.map((c) => {
              const logo = c.branding?.logo;
              const platforms = c.branding?.platforms || {};
              const live = Array.isArray(c.livePlatforms) ? c.livePlatforms : [];
              const isLive = !!c.isLive;
              const platformList = PLATFORMS.filter((p) => platforms[p.key]);
              return (
                <div
                  key={c.slug}
                  className={`relative bg-gray-900 border rounded-2xl p-6 flex flex-col items-center text-center transition-all ${
                    isLive ? 'border-red-500/40 hover:border-red-500/70' : 'border-white/10 opacity-70 grayscale hover:grayscale-0 hover:opacity-100'
                  }`}
                >
                  {/* Selo AO VIVO / Offline */}
                  {isLive ? (
                    <span className="absolute top-3 right-3 inline-flex items-center gap-1 text-[10px] font-black uppercase tracking-widest text-red-400 bg-red-500/15 border border-red-500/40 px-2 py-0.5 rounded-full">
                      <Radio size={11} className="animate-pulse" /> Ao vivo
                    </span>
                  ) : (
                    <span className="absolute top-3 right-3 text-[10px] font-bold uppercase tracking-widest text-gray-500 bg-white/5 border border-white/10 px-2 py-0.5 rounded-full">
                      Offline
                    </span>
                  )}

                  <div className={`w-20 h-20 rounded-full bg-black border flex items-center justify-center overflow-hidden mb-4 ${isLive ? 'border-red-500/50' : 'border-white/20'}`}>
                    {logo ? (
                      <img src={logo} alt={c.name} className="w-full h-full object-cover" onError={(e) => { e.target.style.display = 'none'; }} />
                    ) : (
                      <Crown className="text-accent" size={32} />
                    )}
                  </div>

                  <h3 className="font-black text-lg text-white uppercase tracking-tight">{c.name}</h3>
                  {c.slug === 'vinteset' && (
                    <span className="mt-1 text-[10px] font-bold uppercase tracking-widest text-accent bg-accent/10 border border-accent/20 px-2 py-0.5 rounded">Oficial</span>
                  )}

                  <Link
                    to={pathFor(c.slug)}
                    className="mt-5 w-full px-4 py-2 rounded-lg font-bold uppercase text-xs tracking-widest bg-accent text-black hover:brightness-110 transition flex items-center justify-center gap-2"
                  >
                    Entrar no Rei da Mesa <ArrowRight size={14} />
                  </Link>

                  {platformList.length > 0 && (
                    <div className="mt-4 w-full">
                      <div className="text-[10px] font-bold uppercase tracking-widest text-gray-500 mb-2">Assistir a live</div>
                      <div className="flex items-center justify-center gap-2">
                        {platformList.map((p) => {
                          const onLive = live.includes(p.key);
                          return (
                            <a
                              key={p.key}
                              href={platforms[p.key]}
                              target="_blank"
                              rel="noopener noreferrer"
                              title={`${p.label}${onLive ? ' — AO VIVO' : ''}`}
                              className={`w-9 h-9 rounded-lg border flex items-center justify-center transition-all ${
                                onLive ? 'text-white shadow-lg scale-105' : 'text-gray-500 border-white/10 bg-black/40 hover:text-white'
                              }`}
                              style={onLive ? { backgroundColor: `${p.color}22`, borderColor: `${p.color}88`, color: p.color } : undefined}
                            >
                              <PlatformIcon pKey={p.key} />
                            </a>
                          );
                        })}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};

export default ReiDaMesaCriadores;
