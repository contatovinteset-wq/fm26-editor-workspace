import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Trophy, Crown, Medal, User, ChevronLeft, ChevronRight, Search } from 'lucide-react';
import { Link, Navigate } from 'react-router-dom';

const Ranking = () => {
  const [activeTab, setActiveTab] = useState('GERAL');
  const [search, setSearch] = useState('');

  const [rankingGeral, setRankingGeral] = useState([]);
  const [rankingLive, setRankingLive] = useState([]);

  React.useEffect(() => {
    fetch('/api/reidamesa/ranking')
      .then(res => res.json())
      .then(data => {
         const formattedGeral = data.map((sq, i) => ({
             position: i + 1,
             name: sq.user?.nickname || sq.user?.name || sq.user?.twitchId || 'Desconhecido',
             score: sq.totalScore || 0,
             isReiDaMesa: i === 0,
             hasCarroDoOvo: sq.roundScore > 50 
         }));
         setRankingGeral(formattedGeral);

         const formattedLive = [...data]
           .sort((a,b) => (b.roundScore || 0) - (a.roundScore || 0))
           .map((sq, i) => ({
             position: i + 1,
             name: sq.user?.nickname || sq.user?.name || sq.user?.twitchId || 'Desconhecido',
             score: sq.roundScore || 0,
             // The overall winner is always the first item in the original 'data' array
             isReiDaMesa: data.length > 0 && sq.id === data[0].id,
             hasCarroDoOvo: sq.roundScore > 50 
         }));
         
         setRankingLive(formattedLive);
      })
      .catch(console.error);
  }, []);

  const dataToUse = activeTab === 'GERAL' ? rankingGeral : rankingLive;
  const filteredData = dataToUse.filter(item => item.name.toLowerCase().includes(search.toLowerCase()));

  return (
    <div className="w-full min-h-screen bg-bgDark text-white pt-24 pb-16">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        
        {/* Header */}
        <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-8 gap-4">
          <div>
            <h1 className="text-3xl font-black uppercase tracking-tight flex items-center gap-3">
               <Trophy className="text-accent" size={32} />
               Ranking Oficial
            </h1>
            <p className="text-gray-400 mt-2">Visão completa dos maiores pontuadores do Rei da Mesa.</p>
          </div>
          
          <Link to="/reidamesa" className="px-6 py-2 bg-black/50 border border-white/10 hover:bg-white/10 text-sm font-bold uppercase tracking-widest text-white rounded transition-all">
            Voltar
          </Link>
        </div>

        {/* Tabelas e Filtros */}
        <div className="bg-gray-900 border border-white/10 rounded-3xl overflow-hidden shadow-2xl">
           
           {/* Controles: Abas e Busca */}
           <div className="p-6 border-b border-white/10 flex flex-col sm:flex-row justify-between items-center gap-4 bg-black/30">
              <div className="flex bg-black p-1 rounded-lg border border-white/10 w-full sm:w-auto">
                <button 
                  onClick={() => setActiveTab('GERAL')}
                  className={`flex-1 sm:flex-none px-6 py-2 text-sm font-bold uppercase tracking-widest rounded transition-all ${activeTab === 'GERAL' ? 'bg-white/10 text-white' : 'text-gray-500 hover:text-white'}`}
                >
                  Geral
                </button>
                <button 
                  onClick={() => setActiveTab('LIVE')}
                  className={`flex-1 sm:flex-none px-6 py-2 text-sm font-bold uppercase tracking-widest rounded transition-all ${activeTab === 'LIVE' ? 'bg-white/10 text-white' : 'text-gray-500 hover:text-white'}`}
                >
                  Rodada Atual
                </button>
              </div>

              <div className="relative w-full sm:w-64">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500" size={16} />
                <input 
                  type="text"
                  placeholder="Buscar manager..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="w-full bg-black/50 border border-white/10 rounded-lg py-2 pl-9 pr-4 text-sm text-white focus:outline-none focus:border-accent/50"
                />
              </div>
           </div>

           {/* Lista */}
           <div className="overflow-x-auto">
             <table className="w-full text-left text-sm whitespace-nowrap">
               <thead className="bg-black/50 text-gray-400 font-bold uppercase tracking-wider text-xs border-b border-white/5">
                 <tr>
                   <th className="px-8 py-5 w-24 text-center">Posição</th>
                   <th className="px-4 py-5">Manager</th>
                   <th className="px-8 py-5 text-right text-accent">Pontos {activeTab === 'LIVE' && '(Live)'}</th>
                 </tr>
               </thead>
               <tbody className="divide-y divide-white/5">
                 {filteredData.slice(0, 50).map((row) => (
                   <motion.tr 
                     initial={{ opacity: 0 }}
                     animate={{ opacity: 1 }}
                     key={row.position + row.name} 
                     className="hover:bg-white/5 transition-colors group"
                   >
                     <td className="px-8 py-4 font-black flex justify-center items-center h-full">
                       {row.position === 1 ? <Crown size={24} className="text-accent" /> : 
                        row.position === 2 ? <Medal size={24} className="text-gray-300" /> : 
                        row.position === 3 ? <Medal size={24} className="text-[#CD7F32]" /> : 
                        <span className="text-gray-500 text-lg w-6 text-center">{row.position}</span>}
                     </td>
                     <td className="px-4 py-4">
                       <div className="flex items-center gap-3">
                         <div className={`w-8 h-8 rounded-full flex items-center justify-center ${row.isReiDaMesa ? 'bg-accent/20 border border-accent/50 shadow-[0_0_10px_rgba(255,215,0,0.3)]' : 'bg-white/5 border border-white/10'}`}>
                           <User size={14} className={row.isReiDaMesa ? 'text-accent' : 'text-gray-400'} />
                         </div>
                         <div className="flex flex-col">
                           <span className="font-bold text-white group-hover:text-accent transition-colors flex items-center gap-2">
                             {row.name}
                             {row.isReiDaMesa && <span className="text-accent ml-1" title="Rei da Mesa (Campeão Mensal)"><Crown size={18} /></span>}
                             {row.hasCarroDoOvo && (
                               <img src="/carro-ovo-minimal.png" title="Troféu Carro do Ovo (Campeão da Live)" className="w-10 h-10 object-contain inline-block ml-1 opacity-90 hover:opacity-100 transition-opacity filter drop-shadow hover:drop-shadow-lg transform hover:scale-110" alt="Carro do Ovo" />
                             )}
                           </span>
                         </div>
                       </div>
                     </td>
                     <td className="px-8 py-4 text-right">
                        <span className={`bg-black/30 px-3 py-1 rounded font-mono font-bold border border-white/5 ${row.score > 0 ? 'text-green-500' : row.score < 0 ? 'text-red-500' : 'text-blue-400'}`}>
                          {row.score}
                        </span>
                     </td>
                   </motion.tr>
                 ))}
                 {filteredData.length === 0 && (
                    <tr>
                      <td colSpan={3} className="px-8 py-12 text-center text-gray-500">
                        Nenhum manager encontrado no ranking no momento.
                      </td>
                    </tr>
                 )}
               </tbody>
             </table>
           </div>

           {/* Paginação */}
           <div className="p-4 border-t border-white/10 flex justify-between items-center bg-black/30">
              <span className="text-xs text-gray-500 font-bold uppercase tracking-wider">Mostrando {filteredData.length} resultados</span>
              <div className="flex gap-2">
                 <button className="w-8 h-8 rounded bg-white/5 hover:bg-white/10 flex items-center justify-center text-gray-400 transition-colors">
                   <ChevronLeft size={16} />
                 </button>
                 <button className="w-8 h-8 rounded bg-white/5 hover:bg-white/10 flex items-center justify-center text-gray-400 transition-colors">
                   <ChevronRight size={16} />
                 </button>
              </div>
           </div>
        </div>

      </div>
    </div>
  );
};

export default Ranking;
