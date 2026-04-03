import React, { useState, useEffect } from 'react';
import { Shield, Search, Filter, ArrowLeft, Activity, User, Ruler } from 'lucide-react';
import { Link } from 'react-router-dom';

const PlantelReiDaMesa = () => {
  const [players, setPlayers] = useState([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [activeFilter, setActiveFilter] = useState('ALL'); // ALL, GOL, DEF, MEI, ATA
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetch('/api/reidamesa/players')
      .then(res => res.json())
      .then(data => {
        setPlayers(data);
        setIsLoading(false);
      })
      .catch(err => {
        console.error(err);
        setIsLoading(false);
      });
  }, []);

  const filteredPlayers = players.filter(p => {
    const matchesSearch = p.name.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesFilter = activeFilter === 'ALL' || p.cartolaRole === activeFilter;
    return matchesSearch && matchesFilter;
  });

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        
        {/* Header */}
        <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-8 gap-4">
          <div>
            <h1 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3">
               <Shield className="text-accent" size={32} />
               Plantel de Jogadores
            </h1>
            <p className="text-gray-400 mt-2">Visão geral do elenco atual do save para você estudar suas próximas escalações.</p>
          </div>
          
          <Link to="/reidamesa" className="px-6 py-2 text-sm font-bold uppercase tracking-widest text-gray-400 hover:text-white hover:bg-white/5 rounded transition-all flex items-center gap-2 border border-white/10 bg-black/50">
            <ArrowLeft size={16} /> Voltar
          </Link>
        </div>

        {/* Filters and Search */}
        <div className="flex flex-col sm:flex-row gap-4 mb-8 bg-gray-900/50 p-4 rounded-xl border border-white/5">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={18} />
            <input 
              type="text" 
              placeholder="Buscar jogador por nome..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full bg-black/50 text-white rounded-lg pl-10 pr-4 py-2 border border-white/10 focus:border-accent focus:outline-none transition-colors"
            />
          </div>
          
          <div className="flex gap-2 overflow-x-auto pb-2 sm:pb-0 scrollbar-hide">
            {['ALL', 'DEF', 'MEI', 'ATA'].map(role => (
              <button
                key={role}
                onClick={() => setActiveFilter(role)}
                className={`px-4 py-2 rounded-lg font-bold text-sm tracking-wider uppercase transition-colors whitespace-nowrap ${
                  activeFilter === role 
                    ? 'bg-accent text-black shadow-[0_0_10px_rgba(255,215,0,0.3)]' 
                    : 'bg-black/50 text-gray-400 border border-white/10 hover:border-white/30'
                }`}
              >
                {role === 'ALL' ? 'Todos' : role === 'DEF' ? 'DEF/GOL' : role}
              </button>
            ))}
          </div>
        </div>

        {/* Players View */}
        {isLoading ? (
          <div className="flex justify-center py-20">
            <span className="w-10 h-10 border-4 border-accent border-t-transparent rounded-full animate-spin"></span>
          </div>
        ) : filteredPlayers.length === 0 ? (
          <div className="text-center py-20 bg-gray-900/50 rounded-2xl border border-white/5">
            <Filter className="w-12 h-12 text-gray-500 mx-auto mb-4" />
            <h3 className="text-xl font-bold text-gray-300">Nenhum jogador encontrado</h3>
            <p className="text-gray-500">Tente ajustar sua busca ou posição.</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {filteredPlayers.map(player => (
              <div key={player.id} className="bg-gray-900 border border-white/10 rounded-xl p-4 flex flex-col hover:border-accent/30 transition-colors group">
                <div className="flex justify-between items-start mb-4">
                  <div className="flex items-center gap-3">
                    <div className="w-12 h-12 rounded-full bg-black border border-white/20 flex items-center justify-center overflow-hidden">
                      <img src="/assets/portraits/default.png" alt="" className="w-full h-full object-cover opacity-60 group-hover:opacity-100 transition-opacity" onError={(e) => { e.target.src = `https://via.placeholder.com/150/111/fff?text=${player.name.charAt(0)}` }} />
                    </div>
                    <div>
                      <h3 className="font-bold text-white text-sm leading-tight truncate max-w-[120px]" title={player.name}>{player.name}</h3>
                      <p className="text-xs text-gray-500 font-bold">{player.realPosition || 'N/A'}</p>
                    </div>
                  </div>
                  <div className="bg-accent/10 px-2 py-1 rounded text-xs font-black tracking-widest uppercase border border-accent/20 text-accent">
                    {player.cartolaRole || '?'}
                  </div>
                </div>
                
                <div className="grid grid-cols-2 gap-y-2 gap-x-1 mt-auto pt-4 border-t border-white/5">
                  <div className="flex items-center gap-2 text-[10px] text-gray-400" title="Idade">
                    <User size={12} className="text-gray-500" /> {player.age ? `${player.age} anos` : '-'}
                  </div>
                  <div className="flex items-center gap-2 text-[10px] text-gray-400" title="Classificação Média">
                    <Activity size={12} className="text-accent" /> CM: <span className="text-white font-bold">{player.rawStats?.['Classificação'] || 'N/A'}</span>
                  </div>
                  <div className="flex items-center gap-2 text-[10px] text-gray-400" title="Minutos Jogados">
                    <span className="font-bold text-gray-500">MIN</span> <span className="text-white">{player.rawStats?.['Minutos'] || '0'}</span>
                  </div>
                  <div className="flex items-center gap-2 text-[10px] text-gray-400" title="Distância Percorrida/90">
                    <span className="font-bold text-gray-500">DIST</span> <span className="text-white">{player.rawStats?.['Dist/90'] || player.rawStats?.['Distância'] || '0'}</span>
                  </div>
                  <div className="flex items-center gap-2 text-[10px] text-gray-400" title="Gols">
                    <span className="font-bold text-gray-500">GOL</span> <span className="text-white text-center w-full">{player.rawStats?.['Golos'] || '0'}</span>
                  </div>
                  <div className="flex items-center gap-2 text-[10px] text-gray-400" title="Assistências">
                    <span className="font-bold text-gray-500">AST</span> <span className="text-white text-center w-full">{player.rawStats?.['Assist.'] || '0'}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default PlantelReiDaMesa;
