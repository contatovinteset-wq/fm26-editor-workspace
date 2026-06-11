import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Crown, Users, Tv, ArrowRight } from 'lucide-react';

// Diretório público dos criadores do Rei da Mesa (Fase 3c).
// Lista todos os criadores ativos (incluindo o vinteset). O card do vinteset
// leva ao /reidamesa bare (flagship); os demais a /reidamesa/c/:slug.
const ReiDaMesaCriadores = () => {
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
              const liveUrl = c.branding?.liveUrl;
              return (
                <div key={c.slug} className="bg-gray-900 border border-white/10 rounded-2xl p-6 flex flex-col items-center text-center hover:border-accent/40 transition-colors group">
                  <div className="w-20 h-20 rounded-full bg-black border border-white/20 flex items-center justify-center overflow-hidden mb-4">
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

                  {liveUrl && (
                    <a
                      href={liveUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="mt-2 w-full px-4 py-2 rounded-lg font-bold uppercase text-[11px] tracking-widest bg-black/50 border border-white/10 text-gray-300 hover:text-white hover:bg-white/5 transition flex items-center justify-center gap-2"
                    >
                      <Tv size={14} /> Assistir a live
                    </a>
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
