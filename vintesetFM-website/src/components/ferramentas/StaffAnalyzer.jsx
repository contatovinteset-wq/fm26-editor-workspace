import React, { useState } from 'react';
import { Download, Upload, Info, Youtube, LayoutGrid, List as ListIcon, Award, Activity, Heart, Eye, Banknote } from 'lucide-react';

const RATING_MAP = {
  'Inadequado': 1,
  'Razoável': 4,
  'Competente': 7,
  'Mediano': 9,
  'Bom': 12,
  'Muito bom': 15,
  'Excepcional': 18,
  'Elite': 20
};

const TEC_ATTRS = [
  'Avaliação da Capacidade do Jogador',
  'Avaliação do Potencial do Jogador',
  'Conhecimento Táctico',
  'Análise de Dados',
  'Avaliação da Capacidade da Equipa Técnica'
];

const MENT_ATTRS = [
  'Capacidade Negocial',
  'Adaptabilidade',
  'Gestão de Pessoal',
  'Determinação',
  'Motivação',
  'Autoridade'
];

export default function StaffAnalyzer() {
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [staffData, setStaffData] = useState([]);
  const [viewMode, setViewMode] = useState('cards'); // 'cards' | 'table'
  const [loading, setLoading] = useState(false);

  const categories = [
    { id: 'preparadores', label: 'Preparadores (Adjunto)', icon: <Activity className="w-5 h-5"/> },
    { id: 'fisioterapeutas', label: 'Fisioterapeutas', icon: <Heart className="w-5 h-5"/> },
    { id: 'olheiro', label: 'Olheiros', icon: <Eye className="w-5 h-5"/> }
  ];

  const handleFileUpload = (e) => {
    const file = e.target.files[0];
    if (!file) return;

    setLoading(true);
    const reader = new FileReader();
    reader.onload = (event) => {
      const htmlText = event.target.result;
      processHtmlData(htmlText);
    };
    reader.readAsText(file);
  };

  const processHtmlData = (html) => {
    try {
      const parser = new DOMParser();
      const doc = parser.parseFromString(html, 'text/html');
      const table = doc.querySelector('table');
      if (!table) throw new Error("A tabela não foi encontrada no arquivo submetido.");

      const rows = Array.from(table.querySelectorAll('tr'));
      const theaders = Array.from(rows[0].querySelectorAll('th')).map(th => th.innerText.trim());

      const data = [];

      for (let i = 1; i < rows.length; i++) {
        const tds = Array.from(rows[i].querySelectorAll('td'));
        if (tds.length === 0) continue;

        const p = {};
        theaders.forEach((header, index) => {
           if(tds[index]) p[header] = tds[index].innerText.trim();
        });

        // Filtrar Baseado na Categoria Selecionada pelas Palavras-Chave no "Função Preferida"
        const funcPref = (p['Função Preferida'] || '').toLowerCase();
        let passFilter = true;

        if (selectedCategory === 'preparadores') {
          passFilter = funcPref.includes('preparador') || funcPref.includes('treinador');
        } else if (selectedCategory === 'fisioterapeutas') {
          passFilter = funcPref.includes('fisioterapeuta') || funcPref.includes('médico') || funcPref.includes('cientista');
        } else if (selectedCategory === 'olheiro') {
          passFilter = funcPref.includes('olheiro') || funcPref.includes('recrutamento') || funcPref.includes('diretor técnico');
        }

        if(!passFilter) continue;

        // Cálculos e Médias
        let tecSum = 0; let tecCount = 0;
        let mentSum = 0; let mentCount = 0;

        TEC_ATTRS.forEach(attr => {
           if (p[attr] && RATING_MAP[p[attr]]) {
             tecSum += RATING_MAP[p[attr]];
             tecCount++;
           }
        });

        MENT_ATTRS.forEach(attr => {
           // Adaptabilidade bug: empty '-' or blank string => skip math
           if (p[attr] !== '-' && p[attr] !== '' && RATING_MAP[p[attr]]) {
             mentSum += RATING_MAP[p[attr]];
             mentCount++;
           }
        });

        p.tecAvg = tecCount > 0 ? (tecSum / tecCount).toFixed(1) : 0;
        p.mentAvg = mentCount > 0 ? (mentSum / mentCount).toFixed(1) : 0;
        p.overall = (((parseFloat(p.tecAvg) + parseFloat(p.mentAvg)) / 2).toFixed(1));

        data.push(p);
      }

      // Ordenar do melhor para o pior
      data.sort((a,b) => b.overall - a.overall);

      setStaffData(data);
    } catch(err) {
      alert("Erro ao processar arquivo: " + err.message);
    } finally {
      setLoading(false);
    }
  };

  const getAvatarUrl = (uid) => {
    return uid ? `https://sortitoutsi.b-cdn.net/uploads/face/face_${uid}.png` : '';
  }

  const renderGrade = (num) => {
     let color = "text-gray-400";
     if (num >= 15) color = "text-green-400 font-bold";
     else if (num >= 12) color = "text-yellow-400 font-semibold";
     else if (num >= 8) color = "text-orange-400";
     else if (num < 8 && num > 0) color = "text-red-400";

     return <span className={color}>{num || 'N/A'}</span>;
  }

  return (
    <div className="flex flex-col gap-6 mt-4">
      {/* NOVO PAINEL DE CONTROLE SUPERIOR (Top Header Grid) */}
      <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-4">
         
         {/* 1. SELETOR DE CATEGORIAS */}
         <div className="bg-[#1f2229] border border-gray-700/50 rounded-xl p-4 shadow-lg flex flex-col justify-center order-2 xl:order-1 lg:col-span-2 xl:col-span-1">
           <h4 className="text-xs font-bold text-gray-500 uppercase tracking-wider mb-3">Selecione o Cargo</h4>
           <div className="flex gap-2 overflow-x-auto pb-2 scrollbar-hide">
             {categories.map((cat) => (
                <button
                   key={cat.id}
                   onClick={() => { setSelectedCategory(cat.id); setStaffData([]); }}
                   className={`flex-shrink-0 flex items-center px-4 py-2.5 rounded-lg text-sm transition-all border ${selectedCategory === cat.id ? 'bg-accent/10 text-accent font-semibold border-accent/40 shadow-inner' : 'text-gray-400 hover:bg-gray-800/80 border-gray-700/50 bg-gray-800/30'}`}
                >
                   <div className={`mr-2 ${selectedCategory === cat.id ? 'text-accent' : 'text-gray-500'}`}>
                     {cat.icon}
                  </div>
                  {cat.label}
               </button>
             ))}
           </div>
         </div>
         {/* 2. INSTRUÇÕES & DOWNLOAD VIEW */}
         <div className="bg-[#1f2229] border border-gray-700/50 rounded-xl p-5 shadow-lg relative overflow-hidden group flex flex-col justify-center order-1 xl:order-2">
            <div className="absolute top-0 right-0 w-32 h-32 bg-accent/5 rounded-full blur-3xl -mr-10 -mt-10 pointer-events-none"></div>
            <div className="flex items-center justify-between mb-2">
               <h3 className="text-xl font-bold flex items-center text-white">
                 <Award className="w-5 h-5 text-accent mr-2" /> Decida Como Um Pró
               </h3>
               <a href="https://drive.google.com/file/d/1rxZlZTwY3tYXKKXFNEHF8DbTzO3GuuSs/view?usp=drive_link" target="_blank" rel="noreferrer"
                  className="flex items-center justify-center space-x-1.5 bg-gray-800 hover:bg-gray-700 border border-gray-600 text-white rounded-lg py-1.5 px-3 text-xs font-semibold transition-all shadow-sm">
                  <Download className="w-3 h-3 text-accent" />
                  <span>Baixar Views</span>
               </a>
            </div>
            <p className="text-sm text-gray-400 leading-relaxed pr-4">
              Aqui você terá ajuda definitiva para decidir quais profissionais contratar para compor a staff do seu time. Mapeie o mercado com precisão!
            </p>
         </div>

         {/* 3. CTA PREMIUM (Lado a Lado) */}
         <div className="bg-gradient-to-br from-indigo-900/40 via-bgDark to-accent/10 border border-accent/30 rounded-xl p-5 shadow-2xl relative flex flex-col justify-center order-3 xl:order-3">
            <span className="absolute -top-3 -right-3 bg-accent text-bgDark text-[10px] font-black px-3 py-1 rounded-full shadow-lg border border-accent animate-pulse uppercase tracking-wider">Premium</span>
            <h3 className="text-lg font-extrabold text-white mb-2 decoration-accent flex items-center justify-between">
               <span><span className="text-transparent bg-clip-text bg-gradient-to-r from-accent to-yellow-400 text-xl block">FM26PlayerExport v5</span></span>
            </h3>
            <p className="text-xs text-gray-400 mb-4 font-medium">Extraia todas as informações necessárias num clique e hackeie o mercado sendo Membro.</p>
            
            <div className="flex flex-row gap-2">
               <a href="https://www.youtube.com/channel/UCN7QD3RR37kN9_f6Dzs_3jg/join" target="_blank" rel="noreferrer"
                  className="flex-1 h-9 flex items-center justify-center space-x-1.5 bg-red-600 hover:bg-red-500 text-white rounded-lg text-[10px] sm:text-[11px] font-bold shadow-lg transition-transform hover:-translate-y-0.5 border border-red-500">
                 <Youtube className="w-3 h-3 sm:w-3.5 sm:h-3.5" /> <span>Youtube</span>
               </a>
               <a href="https://www.patreon.com/posts/fm26playerexport-154546270?utm_medium=clipboard_copy&utm_source=copyLink&utm_campaign=postshare_creator&utm_content=join_link" target="_blank" rel="noreferrer"
                  className="flex-1 h-9 flex items-center justify-center space-x-1.5 bg-black hover:bg-neutral-900 text-white rounded-lg text-[10px] sm:text-[11px] font-bold shadow-lg transition-transform hover:-translate-y-0.5 border border-neutral-800">
                 <svg className="w-3 h-3 fill-current" viewBox="0 0 24 24"><path d="M15.386 0c-4.764 0-8.64 3.876-8.64 8.64 0 4.75 3.876 8.613 8.64 8.613 4.75 0 8.614-3.864 8.614-8.613C24 3.876 20.136 0 15.386 0zM0 .004v23.996h5.666V.004H0z"/></svg>
                 <span>Patreon</span>
               </a>
               <a href="https://livepix.gg/vinteset/socio-torcedor-27" target="_blank" rel="noreferrer"
                  className="flex-1 h-9 flex items-center justify-center space-x-1.5 bg-[#32BCAD] hover:bg-[#2eaa9c] text-white rounded-lg text-[10px] sm:text-[11px] font-bold shadow-lg transition-transform hover:-translate-y-0.5 border border-[#32BCAD]">
                 <Banknote className="w-3 h-3 sm:w-3.5 sm:h-3.5" /> <span className="whitespace-nowrap">Pague com Pix</span>
               </a>
            </div>
         </div>
      </div>

      {/* Main Área Analise - FULL WIDTH */}
      <div className="w-full flex flex-col bg-[#1a1c22] rounded-xl border border-gray-700/30 p-2 md:p-4 shadow-xl relative min-h-[500px]">
        {!selectedCategory ? (
          <div className="flex-1 flex flex-col items-center justify-center border-2 border-dashed border-gray-700/50 rounded-2xl bg-gray-800/10 h-full">
             <div className="w-20 h-20 rounded-full bg-gray-800/80 text-gray-400 flex items-center justify-center mb-6 shadow-inner border border-gray-700">
                <svg className="w-10 h-10 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M15 15l-2 5L9 9l11 4-5 2zm0 0l5 5M7.188 2.239l.777 2.897M5.136 7.965l-2.898-.777M13.95 4.05l-2.122 2.122m-5.657 5.656l-2.12 2.122" /></svg>
             </div>
             <p className="text-gray-400 text-lg font-medium">Selecione uma Categoria no painel superior para começar.</p>
          </div>
        ) : (
          <>
            <div className="flex justify-between items-end mb-4 border-b border-gray-700 pb-4">
               <div className="flex flex-col">
                  <h2 className="text-2xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-gray-100 to-gray-500">
                     Dashboard de {categories.find(c => c.id === selectedCategory)?.label}
                  </h2>
                  <div className="mt-2 text-xs flex gap-2">
                    <span className="flex items-center text-orange-400/90 font-medium bg-orange-400/10 px-2 py-0.5 rounded border border-orange-400/20 shadow-sm">
                      <Info className="w-3 h-3 mr-1" /> Bug da Engine:
                    </span>
                    <span className="text-gray-400 mt-0.5">O FM26 não exporta o atributo <span className="text-gray-300 font-bold">Adaptabilidade</span>. Retiramos e o ignoramos na matemática analítica para preservar a imparcialidade do rank e não viciar a nota média mental da equipe técnica pra baixo artificialmente.</span>
                  </div>
               </div>

               {staffData.length > 0 && (
                 <div className="flex bg-gray-900 rounded-lg p-1 border border-gray-700 h-9 shrink-0 ml-4">
                    <button onClick={() => setViewMode('cards')} className={`p-1.5 rounded transition max-h-full ${viewMode === 'cards' ? 'bg-gray-700 text-white shadow' : 'text-gray-500 hover:text-white'}`} title="Cartões Profiler">
                       <LayoutGrid className="w-4 h-4" />
                    </button>
                    <button onClick={() => setViewMode('table')} className={`p-1.5 rounded transition max-h-full ${viewMode === 'table' ? 'bg-gray-700 text-white shadow' : 'text-gray-500 hover:text-white'}`} title="Lista Analítica">
                       <ListIcon className="w-4 h-4" />
                    </button>
                 </div>
               )}
            </div>

            {loading ? (
              <div className="flex-1 flex justify-center items-center"><div className="w-8 h-8 border-4 border-accent border-t-transparent rounded-full animate-spin"></div></div>
            ) : staffData.length === 0 ? (
              <div className="flex-1 flex items-center justify-center min-h-[400px]">
                <label className="w-full cursor-pointer h-full border-2 border-dashed border-accent/40 hover:border-accent bg-accent/5 hover:bg-accent/10 transition-all rounded-2xl flex flex-col items-center justify-center py-20 px-4 group">
                  <div className="w-20 h-20 bg-[#1f2229] border border-gray-700 rounded-full flex items-center justify-center group-hover:scale-110 transition-transform shadow-2xl mb-4">
                    <Upload className="w-8 h-8 text-accent" />
                  </div>
                  <h3 className="text-xl font-bold mb-2">Importar Dados</h3>
                  <p className="text-gray-400 text-center max-w-sm mb-4">Arraste seu arquivo <span className="text-white font-mono bg-gray-800 px-1 rounded">.html</span> extraído do jogo graças ao FM26PlayerExport V5 aqui dentro.</p>
                  <span className="bg-gray-800 text-white text-xs px-4 py-1.5 rounded-full border border-gray-700 font-medium">Ler Arquivo</span>
                  <input type="file" accept=".html,.htm" className="hidden" onChange={handleFileUpload} />
                </label>
              </div>
            ) : (
              <div className="flex-1 space-y-4">
                 {viewMode === 'cards' ? (
                   <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
                     {staffData.map((staff, i) => (
                       <div key={staff['ID Único'] + i} className={`bg-[#1f2229] rounded-xl border relative overflow-hidden flex flex-col transition-all hover:-translate-y-1 ${i === 0 ? 'border-accent/50 shadow-[0_0_20px_rgba(255,215,0,0.15)] ring-1 ring-accent/20' : 'border-gray-700/60 shadow-lg'}`}>
                          {i === 0 && <div className="absolute top-0 right-0 bg-gradient-to-bl from-accent to-yellow-500 text-bgDark text-[10px] uppercase tracking-widest font-bold py-1 px-3 rounded-bl-lg z-10 shadow-lg">👑 Top 1</div>}
                          
                          <div className="flex p-4 pb-2 border-b border-gray-800 bg-gradient-to-b from-gray-800/30 to-transparent relative">
                            <div className="w-16 h-16 rounded-lg bg-gray-800 overflow-hidden shadow-xl shrink-0 border border-gray-700/80">
                               <img src={getAvatarUrl(staff['ID Único'])} alt="Face" className="w-full h-full object-cover" onError={(e) => { e.target.onerror = null; e.target.src = `https://ui-avatars.com/api/?name=${encodeURIComponent(staff['Pessoa'])}&background=1f2229&color=eab308&bold=true&size=128`; }} />
                            </div>
                            <div className="ml-3 flex-1 min-w-0 flex flex-col justify-center">
                              <h4 className="font-bold text-white text-base leading-tight" title={staff['Pessoa']}>{staff['Pessoa']}</h4>
                              <p className="text-[12px] text-gray-400 mb-1" title={staff['Função Preferida']}>{staff['Função Preferida']}</p>
                              <div className="flex flex-wrap items-center gap-1.5 mt-1">
                                <span className="bg-gray-800 border border-gray-700 px-2 py-0.5 rounded text-[11px] text-gray-300">💼 {staff['Clube'] || 'Livres'}</span>
                                <span className="bg-gray-800 border border-gray-700 px-2 py-0.5 rounded text-[11px] text-gray-300">{staff['Salário'] || 'N/A'}</span>
                                {staff['Personalidade'] && <span className="bg-gray-800 border border-gray-700 px-2 py-0.5 rounded text-[11px] text-accent font-medium">🧠 {staff['Personalidade']}</span>}
                              </div>
                            </div>
                          </div>

                          <div className="p-3 bg-[#181a20] flex-1 flex flex-col justify-center">
                             <div className="flex justify-between items-center px-1 mb-2">
                               <div className="text-center">
                                 <p className="text-[10px] text-gray-500 font-bold uppercase tracking-wider mb-0.5">Técnico</p>
                                 <p className="text-lg font-bold">{renderGrade(staff.tecAvg)}</p>
                               </div>
                               <div className="h-8 w-px bg-gray-700/50"></div>
                               <div className="text-center">
                                 <p className="text-[10px] text-gray-500 font-bold uppercase tracking-wider mb-0.5">Mental</p>
                                 <p className="text-lg font-bold">{renderGrade(staff.mentAvg)}</p>
                               </div>
                               <div className="h-8 w-px bg-gray-700/50"></div>
                               <div className={`text-center px-3 py-1 rounded-lg ${i === 0 ? 'bg-accent/10 border border-accent/20' : 'bg-gray-800 border border-gray-700'}`}>
                                 <p className="text-[10px] text-white/50 font-bold uppercase tracking-wider mb-0.5">Geral</p>
                                 <p className={`text-xl font-extrabold ${i === 0 ? 'text-accent' : 'text-white'}`}>{staff.overall}</p>
                               </div>
                             </div>
                             
                             <div className="flex flex-col gap-y-1.5 mt-2 pt-3 border-t border-gray-800/50 text-xs">
                                 {[...TEC_ATTRS, ...MENT_ATTRS].filter(a => a !== 'Adaptabilidade' && staff[a] && staff[a] !== '-' && staff[a] !== '').slice(0, 6).map(attr => (
                                   <div key={attr} className="flex justify-between items-center text-gray-400">
                                     <span className="truncate pr-2" title={attr}>{attr}</span>
                                     <span className="font-medium text-gray-300 bg-gray-800 px-1.5 py-0.5 rounded ml-1 whitespace-nowrap">{RATING_MAP[staff[attr]]}</span>
                                   </div>
                                 ))}
                              </div>
                          </div>
                          
                       </div>
                     ))}
                   </div>
                 ) : (
                   <div className="overflow-x-auto rounded-xl border border-gray-700/50 bg-[#1f2229] shadow-lg scrollbar-hide">
                      <table className="w-full text-sm text-left">
                        <thead className="bg-[#181a20] text-xs text-gray-400 uppercase border-b border-gray-700">
                          <tr>
                            <th className="px-4 py-3 font-semibold w-10">Rk</th>
                            <th className="px-4 py-3 font-semibold">Perfil</th>
                            <th className="px-4 py-3 font-semibold truncate hidden md:table-cell">Clube</th>
                            <th className="px-4 py-3 font-semibold truncate hidden lg:table-cell">Função Preferida</th>
                            <th className="px-4 py-3 font-semibold truncate hidden xl:table-cell">Personalidade</th>
                            <th className="px-4 py-3 font-semibold truncate hidden xl:table-cell">Salário</th>
                            <th className="px-4 py-3 font-semibold truncate hidden xl:table-cell">Expira</th>
                            <th className="px-4 py-3 font-semibold text-center bg-gray-800/50 border-x border-gray-800">Técnico</th>
                            <th className="px-4 py-3 font-semibold text-center bg-gray-800/50 border-r border-gray-800">Mental</th>
                            <th className="px-4 py-3 font-semibold text-center text-accent bg-accent/5">Geral</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-800/60">
                            {staffData.map((staff, i) => (
                              <tr key={staff['ID Único'] + i} className={`hover:bg-gray-800/30 transition-colors ${i === 0 ? 'bg-accent/5' : ''}`}>
                                <td className={`px-4 py-3 font-bold text-center ${i === 0 ? 'text-accent' : 'text-gray-500'}`}>{i + 1}</td>
                                <td className="px-4 py-4">
                                   <div className="flex items-center">
                                     <img src={getAvatarUrl(staff['ID Único'])} className="w-10 h-10 rounded-full border-2 border-[#1f2229] shadow-md mr-3" alt="" onError={(e) => { e.target.onerror = null; e.target.src = `https://ui-avatars.com/api/?name=${encodeURIComponent(staff['Pessoa'])}&background=1f2229&color=eab308&bold=true&size=128`; }} />
                                     <div>
                                       <div className={`font-bold text-sm tracking-wide ${i === 0 ? 'text-white' : 'text-gray-200'}`}>{staff['Pessoa']}</div>
                                       <div className="text-[11px] text-gray-500 font-medium">{staff['Função Preferida']}</div>
                                     </div>
                                   </div>
                                 </td>
                                <td className="px-4 py-3 text-gray-400 text-xs hidden md:table-cell">{staff['Clube'] || '-'}</td>
                                <td className="px-4 py-3 text-gray-400 text-xs hidden lg:table-cell">{staff['Função Preferida'] || '-'}</td>
                                <td className="px-4 py-3 text-gray-400 text-xs hidden xl:table-cell">{staff['Personalidade'] || '-'}</td>
                                <td className="px-4 py-3 text-gray-400 text-xs hidden xl:table-cell">{staff['Salário'] || '-'}</td>
                                <td className="px-4 py-3 text-gray-400 text-xs hidden xl:table-cell">{staff['Expira'] || '-'}</td>
                                <td className="px-4 py-3 text-center bg-gray-800/20 font-bold border-x border-gray-800/50">{renderGrade(staff.tecAvg)}</td>
                                <td className="px-4 py-3 text-center bg-gray-800/20 font-bold border-r border-gray-800/50">{renderGrade(staff.mentAvg)}</td>
                                <td className={`px-4 py-3 text-center font-extrabold ${i === 0 ? 'bg-accent/10 text-accent' : 'bg-gray-800/20 text-white'}`}>{staff.overall}</td>
                              </tr>
                           ))}
                        </tbody>
                      </table>
                   </div>
                 )}
              </div>
            )}
          </>
        )}
      </div>

    </div>
  );
}
