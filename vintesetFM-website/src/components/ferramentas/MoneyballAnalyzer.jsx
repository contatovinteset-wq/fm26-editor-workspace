import React, { useState, useMemo } from 'react';
import { Download, Upload, Info, Youtube, LayoutGrid, List as ListIcon, Award, FilterX, X, Heart } from 'lucide-react';
import ExcelColors from './moneyball_colors.json';
import { processMoneyballHtml } from './MoneyballLogic';
import MoneyballPlayerModal from './MoneyballPlayerModal';

const CATEGORIES = [
  { id: 'Goleiros', label: 'Goleiros' },
  { id: 'Zagueiros', label: 'Zagueiros' },
  { id: 'Laterais', label: 'Laterais' },
  { id: 'Volantes', label: 'Volantes' },
  { id: 'Box-To-Box', label: 'Box-To-Box' },
  { id: 'Armadores', label: 'Armadores' },
  { id: 'Avançados', label: 'Avançados' },
];

const PercentileBar = ({ percentile }) => {
  const blocks = 10;
  const filledBlocks = Math.round((percentile / 100) * blocks);
  
  let colorClass = 'bg-[#15af59]'; 
  if (percentile < 25) colorClass = 'bg-[#d84841]'; 
  else if (percentile < 50) colorClass = 'bg-[#f4a13f]'; 
  else if (percentile < 75) colorClass = 'bg-[#f4d13f]'; 
  
  return (
    <div className="flex gap-[2px] h-[5px] w-full">
      {Array.from({ length: blocks }).map((_, idx) => (
        <div key={idx} className={`flex-1 rounded-sm ${idx < filledBlocks ? colorClass : 'bg-[#1e232b]'}`}></div>
      ))}
    </div>
  );
};

const StatRow = ({ label, value, percentile, tooltip }) => {
  return (
    <div className="flex flex-row items-center gap-2 mb-[2px]" title={tooltip}>
       <div className="w-[42px] shrink-0 text-right">
          <span className="text-white font-black text-[13px] leading-none">
             {typeof value === 'number' && !Number.isInteger(value) ? value.toFixed(2) : value}
          </span>
       </div>
       <div className="flex-1 flex flex-col justify-center min-w-0 pr-2">
          <span className="text-[9px] text-[rgba(255,255,255,0.7)] font-bold uppercase tracking-wider leading-none mb-[3px] truncate w-full block">
             {label}
          </span>
          <PercentileBar percentile={percentile} />
       </div>
    </div>
  );
};

const FootIcon = ({ pe }) => {
  const p = (pe || '').toLowerCase();
  const isLeft = p.includes('esq') || p.includes('canhoto') || p.includes('ambos');
  const isRight = p.includes('dir') || p.includes('destro') || p.includes('ambos');
  let label = '-';
  if (isLeft && isRight) label = 'A';
  else if (isLeft) label = 'E';
  else if (isRight) label = 'D';

  return (
    <div className="flex flex-col items-center justify-center bg-[#1f2229] border border-gray-700 w-12 py-1 rounded shadow-inner" title={`Pé Preferido: ${pe || '?'}`}>
       <span className="text-[8px] text-gray-400 font-bold leading-none mb-0.5 uppercase tracking-wider">Pé</span>
       <span className="text-white font-black text-[13px] leading-none">{label}</span>
    </div>
  );
};

export default function MoneyballAnalyzer() {
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [playersData, setPlayersData] = useState([]);
  const [originalHeaders, setOriginalHeaders] = useState([]);
  const [viewMode, setViewMode] = useState('cards');
  const [sortConfig, setSortConfig] = useState({ key: null, direction: 'desc' });
  const [loading, setLoading] = useState(false);
  
  const [selectedPlayer, setSelectedPlayer] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [hasLocalStorage, setHasLocalStorage] = useState(false);
  
  const [minMinutesFilter, setMinMinutesFilter] = useState('');
  const [minRatingFilter, setMinRatingFilter] = useState('');
  const [excludedPlayers, setExcludedPlayers] = useState([]);

  React.useEffect(() => {
    if (selectedCategory) {
      const storedHtml = localStorage.getItem(`vinteset_moneyball_html_${selectedCategory}`);
      if (storedHtml) {
        setLoading(true);
        setTimeout(() => {
          try {
            const { players, originalHeaders } = processMoneyballHtml(storedHtml, selectedCategory);
            setPlayersData(players);
            setOriginalHeaders(originalHeaders);
            setHasLocalStorage(true);
          } catch(err) {
            clearLocalStorage();
          } finally {
            setLoading(false);
          }
        }, 10);
      } else {
        setPlayersData([]);
        setOriginalHeaders([]);
        setHasLocalStorage(false);
      }
    }
  }, [selectedCategory]);

  const clearLocalStorage = () => {
    if (selectedCategory) {
      localStorage.removeItem(`vinteset_moneyball_html_${selectedCategory}`);
      setPlayersData([]);
      setOriginalHeaders([]);
      setHasLocalStorage(false);
    }
  };

  const internalKeys = ['uid', 'percentiles', 'topStats', 'Goals', 'ExpectedAssists', 'NonPenaltyXG', 'Shots', 'Dribbles', 'ProgressivePasses', 'PassesCompleted', 'PossessionLost', 'TacklesWon', 'PressuresAttempted', 'HeadersWon', 'Assist', 'YellowCards', 'RedCards', 'Appearances', 'Age', 'Height', 'Wage', 'Value', 'Expires', 'Nation', 'Club', 'Position', 'Foot', 'Jogador', 'Pé', 'Personality', 'GoalsPer90', 'ExpectedAssistsPer90', 'NonPenaltyXGPer90', 'ShotsPer90', 'DribblesPer90', 'ProgressivePassesPer90', 'PassesCompletedPer90', 'PossessionLostPer90', 'PossessionWonPer90', 'TacklesWonPer90', 'PressuresAttemptedPer90', 'HeadersWonPer90'];

  const extraCols = originalHeaders.length > 0 ? originalHeaders.filter(obj => {
     const k = typeof obj === 'string' ? obj : obj.id;
     return !k.startsWith('_') && 
     !internalKeys.includes(k) && 
     !['Jogador', 'Jogador.1', 'Clube', 'Classificação', 'Nota média', 'Clas. Méd', 'Minutos', 'Min', 'ID Único', 'Unique ID', 'Inf'].includes(k);
  }) : [];

  const displayPlayers = [...playersData].filter(p => {
      const minMins = Number(minMinutesFilter) || 0;
      const minRat = Number(minRatingFilter) || 0;
      const uidToExclude = p.uid || p.Jogador;
      if (excludedPlayers.includes(uidToExclude)) return false;
      return (Number(p._rawMinutes) || 0) >= minMins && (Number(p._rawRating) || 0) >= minRat;
  }).sort((a,b) => {
    if (!sortConfig.key) return 0;
    let valA = a[sortConfig.key];
    let valB = b[sortConfig.key];
    
    if (typeof valA === "string" && valA.includes(",")) valA = valA.replace(",", ".");
    if (typeof valB === "string" && valB.includes(",")) valB = valB.replace(",", ".");
    
    let numA = parseFloat(valA);
    let numB = parseFloat(valB);
    
    // Sort % properly
    if (typeof a[sortConfig.key] === "string" && a[sortConfig.key].includes("%")) numA = parseFloat(a[sortConfig.key].replace("%",""));
    if (typeof b[sortConfig.key] === "string" && b[sortConfig.key].includes("%")) numB = parseFloat(b[sortConfig.key].replace("%",""));

    if (!isNaN(numA) && !isNaN(numB)) {
      valA = numA; valB = numB;
    }

    if (valA < valB) return sortConfig.direction === 'asc' ? -1 : 1;
    if (valA > valB) return sortConfig.direction === 'asc' ? 1 : -1;
    return 0;
  });

  const requestSort = (key) => {
    let direction = 'desc';
    if (sortConfig.key === key && sortConfig.direction === 'desc') {
      direction = 'asc';
    }
    setSortConfig({ key, direction });
  };

  const displayHeaders = useMemo(() => {
    return originalHeaders.filter(h => {
       const col = typeof h === 'string' ? h : h.id;
       return col !== 'Inf' && col !== 'ID Único' && col !== 'Unique ID' && col !== 'UID';
    });
  }, [originalHeaders]);

  const parseFMValue = (val, colName) => {
    if (val === undefined || val === null || val === '') return NaN;
    if (typeof val === 'number') return val;
    let s = val.toString().trim();
    if (s === 'N/D') return NaN;
    if (colName === 'Data Final do Contrato' || colName === 'Data Final do contrato' || colName === 'Contrato') {
       const parts = s.split('/');
       if (parts.length === 3) {
          return parseInt(parts[2]) + parseInt(parts[1])/12 + parseInt(parts[0])/365;
       }
    }
    // Salario e Valor
    let mult = 1;
    if (s.toLowerCase().includes('m')) mult = 1000000;
    if (s.toLowerCase().includes('mil')) mult = 1000;
    if (s.toLowerCase().includes('k')) mult = 1000;
    
    // clean up non numeric except , and . and -
    s = s.replace(/[^0-9,-]/g, '').replace(',', '.');
    return parseFloat(s) * mult;
  };

  const columnStats = useMemo(() => {
    const stats = {};
    if (!playersData || playersData.length === 0) return stats;

    for (let i = 0; i < displayHeaders.length; i++) {
      const headerObj = displayHeaders[i];
      const colName = typeof headerObj === 'string' ? headerObj : headerObj.id;
      let min = Infinity;
      let max = -Infinity;
      playersData.forEach(p => {
        const val = parseFMValue(p[colName], colName);
        if (!isNaN(val)) {
          if (val < min) min = val;
          if (val > max) max = val;
        }
      });
      if (min === Infinity) min = 0;
      if (max === -Infinity) max = 0;
      stats[colName] = { min, max };
    }
    return stats;
  }, [playersData, displayHeaders]);

  const getCellStyle = (colName, player) => {
    const baseStyle = { 
        padding: '0.75rem', 
        borderBottom: '1px solid #1f2937', 
        textAlign: 'center',
        whiteSpace: 'nowrap'
    };

    if (colName.includes('Jogador') || colName === 'NAC' || colName === 'Clube' || colName === 'Equipe') {
        const isJog = colName.includes('Jogador');
        return { 
            ...baseStyle, 
            color: '#e5e7eb',
            fontWeight: isJog ? '600' : '400', 
            textAlign: 'left',
            position: isJog ? 'sticky' : 'static',
            left: isJog ? '38px' : 'auto',
            zIndex: isJog ? 20 : 'auto',
            backgroundColor: isJog ? '#1f2229' : 'transparent',
            boxShadow: isJog ? '2px 0 5px rgba(0,0,0,0.3)' : 'none',
            borderRight: isJog ? '1px solid #374151' : 'none'
        };
    }

    let val = parseFMValue(player[colName], colName);
    if (isNaN(val)) return { ...baseStyle, color: '#9ca3af' };

    let textColor = '#d1d5db'; // text-gray-300 by default
    let fontWeight = '400';

    if (colName === 'Idade') {
        const valIdade = parseInt(val) || 0;
        if (valIdade > 0) {
            if (valIdade <= 23) textColor = '#4ade80';
            else if (valIdade <= 28) textColor = '#facc15';
            else textColor = '#f87171';
            fontWeight = '600';
            return { ...baseStyle, color: textColor, fontWeight };
        }
    }

    if (colName === 'Classificação' || colName === 'Clas. Méd' || colName === 'Nota média') {
        const nf = parseFloat(val) || 0;
        if (nf > 0) {
             if (nf >= 7.0) textColor = '#4ade80';
             else if (nf >= 6.70) textColor = '#facc15';
             else textColor = '#f87171';
             fontWeight = '600';
             return { ...baseStyle, color: textColor, fontWeight };
        }
    }

    if (colName === 'Impedimentos') {
        const valImp = parseInt(val) || 0;
        if (valImp < 12) textColor = '#4ade80';
        else if (valImp <= 18) textColor = '#facc15';
        else textColor = '#f87171';
        fontWeight = '600';
        return { ...baseStyle, color: textColor, fontWeight };
    }

    if (colName === 'Dist / 90' || colName === 'Dist/90' || colName === 'DK') {
        const dVal = parseFloat(val) || 0;
        if (dVal > 0) {
            if (dVal >= 12.0) textColor = '#4ade80';
            else if (dVal >= 10.0) textColor = '#facc15';
            else textColor = '#f87171';
            fontWeight = '600';
            return { ...baseStyle, color: textColor, fontWeight };
        }
    }

    const rules = ExcelColors[colName];
    if (!rules || rules.length === 0) return { ...baseStyle, color: '#9ca3af' };

    let computedMin = rules.find(r => r.type === "min")?.val;
    let computedMax = rules.find(r => r.type === "max")?.val;

    const stats = columnStats[colName];
    if (computedMin === null || computedMin === undefined) computedMin = stats?.min || 0;
    if (computedMax === null || computedMax === undefined) computedMax = stats?.max || 0;

    const vMin = parseFloat(computedMin) || 0;
    const vMax = parseFloat(computedMax) || 0;

    const reverse = vMin > vMax;
    const actualMin = Math.min(vMin, vMax);
    const actualMax = Math.max(vMin, vMax);
    const range = actualMax - actualMin;
    
    if (range !== 0) {
        const pctObj = (val - actualMin) / range;
        const clampPct = Math.max(0, Math.min(1, pctObj));
        const normalizedPct = reverse ? (1 - clampPct) : clampPct;
        
        if (normalizedPct >= 0.8) {
            textColor = '#4ade80'; // green-400 (Top performant)
            fontWeight = '700';
        } else if (normalizedPct >= 0.5) {
            textColor = '#facc15'; // yellow-400 (Mid performant)
            fontWeight = '600';
        } else if (normalizedPct <= 0.2) {
            textColor = '#f87171'; // red-400 (Bottom performant)
            fontWeight = '600';
        }
    }

    return { 
       ...baseStyle, 
       color: textColor,
       fontWeight: fontWeight
    };
  };

  const formatCellValue = (val, headerObj) => {
    const colName = typeof headerObj === 'string' ? headerObj : headerObj.id;
    const type = typeof headerObj === 'object' ? headerObj.type : 'number';

    if (typeof val === 'number') {
      if (type === 'percentage') {
          let pct = (val * 100).toFixed(1).replace('.', ',');
          return pct.endsWith(',0') ? pct.replace(',0', '') + '%' : pct + '%';
      }
      
      if (type === 'number') {
          return Math.round(val);
      }
      
      // Strict Integer Rules - Jogos Completos and Minutos por partida never have decimals in FM UI
      if (colName === 'Jogos completos' || colName === 'Minutos por partida') {
          return Math.round(val);
      }
      
      if (val % 1 !== 0) {
        return val.toFixed(2).replace('.', ',');
      }
      return val;
    }
    return val;
  };

  const handleFileUpload = (e) => {
    const file = e.target.files[0];
    if (!file) return;

    setLoading(true);
    const reader = new FileReader();
    reader.onload = (event) => {
      const htmlText = event.target.result;
      try {
        const { players, originalHeaders } = processMoneyballHtml(htmlText, selectedCategory);
        
        // Anti-Erro: Validar se os headers contêm as colunas clássicas de elenco
        const headersNames = originalHeaders.map(h => typeof h === 'string' ? h.toLowerCase() : h.id.toLowerCase());
        if (selectedCategory !== 'Goleiros') {
           const isConfigPlayers = headersNames.includes('altura') || headersNames.includes('peso') || headersNames.some(h => h.includes('dist'));
           if (!isConfigPlayers) throw new Error("O arquivo importado não é compatível com Análise Moneyball. Certifique-se de não estar importando uma lista de Staff.");
        }

        setPlayersData(players);
        setOriginalHeaders(originalHeaders);

        if (selectedCategory) {
           localStorage.setItem(`vinteset_moneyball_html_${selectedCategory}`, htmlText);
           setHasLocalStorage(true);
        }
      } catch (err) {
        alert("Erro ao processar arquivo: " + err.message);
      } finally {
        setLoading(false);
      }
    };
    reader.readAsText(file);
  };

  const getAvatarUrl = (uid) => {
    return uid ? `https://sortitoutsi.b-cdn.net/uploads/face/face_${uid}.png` : '';
  }

  const openPlayerModal = (player) => {
     setSelectedPlayer(player);
     setIsModalOpen(true);
  }

  return (
    <div className="flex flex-col gap-6 mt-4">
      <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-4">
         <div className="bg-[#1f2229] border border-gray-700/50 rounded-xl p-4 shadow-lg flex flex-col justify-center order-2 xl:order-1 lg:col-span-2 xl:col-span-1 overflow-hidden">
           <h4 className="text-xs font-bold text-gray-500 uppercase tracking-wider mb-3">Selecione a Posição</h4>
           <div className="flex overflow-x-auto gap-2 pb-3 scrollbar-thin scrollbar-thumb-gray-600 hover:scrollbar-thumb-gray-500 scrollbar-track-transparent">
             {CATEGORIES.map((cat) => (
                 <button
                   key={cat.id}
                   onClick={() => { setSelectedCategory(cat.id); setPlayersData([]); setOriginalHeaders([]); setExcludedPlayers([]); }}
                   className={`flex-shrink-0 flex items-center px-4 py-2.5 rounded-lg text-sm transition-all border ${selectedCategory === cat.id ? 'bg-accent/10 text-accent font-semibold border-accent/40 shadow-inner' : 'text-gray-400 hover:bg-gray-800/80 border-gray-700/50 bg-gray-800/30'}`}
                 >
                 {cat.label}
               </button>
             ))}
           </div>
         </div>
         
         <div className="bg-[#1f2229] border border-gray-700/50 rounded-xl p-5 shadow-lg relative overflow-hidden group flex flex-col justify-center order-1 xl:order-2">
            <div className="absolute top-0 right-0 w-32 h-32 bg-accent/5 rounded-full blur-3xl -mr-10 -mt-10 pointer-events-none"></div>
            <div className="flex items-center justify-between mb-2">
               <h3 className="text-xl font-bold flex items-center text-white">
                 <Award className="w-5 h-5 text-accent mr-2" /> Moneyball Analítico
               </h3>
               <div className="flex items-center gap-2">
                 <a href="https://drive.google.com/file/d/1rxZlZTwY3tYXKKXFNEHF8DbTzO3GuuSs/view?usp=drive_link" target="_blank" rel="noreferrer"
                    className="flex items-center justify-center space-x-1.5 bg-gray-800 hover:bg-gray-700 border border-gray-600 text-white rounded-lg py-1.5 px-3 text-xs font-semibold transition-all shadow-sm">
                    <Download className="w-3 h-3 text-accent" />
                    <span>Baixar Views</span>
                 </a>
               </div>
            </div>
            <p className="text-sm text-gray-400 leading-relaxed pr-4">
              Baseado na metodologia do Moneyball presente na planilha do <a href="https://www.youtube.com/@AllanFCL" target="_blank" rel="noopener noreferrer" className="text-accent hover:underline">AllanFCL</a>, é possivel avaliar os dados dos jogadores obtidos durante as partidas direto no navegador.
            </p>
         </div>

         <div className="bg-gradient-to-br from-gray-900 via-bgDark to-[#1f2229] border border-gray-700/80 rounded-xl p-5 shadow-2xl relative flex flex-col justify-center order-3">
            <span className="absolute -top-3 -right-3 bg-green-500 text-white text-[10px] font-black px-3 py-1 rounded-full shadow-lg border border-green-400 uppercase tracking-wider">Grátis</span>
            <h3 className="text-lg font-extrabold text-white mb-2 flex flex-col">
               <span>FM26PlayerExport</span>
            </h3>
            <p className="text-xs text-gray-400 mb-4 font-medium">Extraia os dados instantaneamente usando o nosso plugin gratuito!</p>
            
            <div className="flex flex-row gap-2">
               <a href="https://drive.google.com/file/d/1ZcMZxsr9VG8TAuVD17qSBYlt5KyuqEc-/view?usp=drive_link" target="_blank" rel="noreferrer"
                  className="flex-1 h-9 flex items-center justify-center space-x-1.5 bg-gray-800 hover:bg-gray-700 text-white rounded-lg text-[11px] font-bold shadow-lg transition-transform hover:-translate-y-0.5 border border-gray-700">
                 <Download className="w-3.5 h-3.5 text-green-400" /> <span>Download V4</span>
               </a>
               <a href="https://livepix.gg/vinteset/socio-torcedor-27" target="_blank" rel="noreferrer"
                  className="flex-1 h-9 flex items-center justify-center space-x-1.5 bg-[#32BCAD] hover:bg-[#2eaa9c] text-white rounded-lg text-[11px] font-bold shadow-lg transition-transform hover:-translate-y-0.5 border border-[#32BCAD]">
                 <Heart className="w-3.5 h-3.5 text-white fill-white" /> <span>Apoie através do PIX</span>
               </a>
            </div>
         </div>
      </div>

      <div className="w-full flex flex-col bg-[#1a1c22] rounded-xl border border-gray-700/30 p-2 md:p-4 shadow-xl relative min-h-[500px]">
        {!selectedCategory ? (
          <div className="flex-1 flex flex-col items-center justify-center border-2 border-dashed border-gray-700/50 rounded-2xl bg-gray-800/10 h-full">
             <div className="w-20 h-20 rounded-full bg-gray-800/80 text-gray-400 flex items-center justify-center mb-6 shadow-inner border border-gray-700">
                <svg className="w-10 h-10 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" /></svg>
             </div>
             <p className="text-gray-400 text-lg font-medium">Selecione uma Posição no painel superior para começar.</p>
          </div>
        ) : (
          <>
            <div className="flex justify-between items-end mb-4 border-b border-gray-700 pb-4">
               <div>
                  <h2 className="text-2xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-gray-100 to-gray-500">
                     Dashboard: {CATEGORIES.find(c => c.id === selectedCategory)?.label || selectedCategory}
                  </h2>
               </div>

               {playersData.length > 0 && (
                 <div className="flex bg-[#111822] rounded-lg p-1.5 border border-gray-700 mx-4 shadow-inner flex-wrap md:flex-nowrap gap-2 items-center">
                    <div className="flex items-center gap-1.5 px-2" title="Ocultar jogadores com menos minutos">
                       <label className="text-xs text-gray-400 font-bold tracking-wider">MINUTOS (≥):</label>
                       <input 
                          type="number" 
                          min="0"
                          title="Filtre pela minutagem"
                          value={minMinutesFilter} 
                          onChange={(e) => setMinMinutesFilter(e.target.value)} 
                          className="w-16 bg-[#1f2229] border border-gray-600 focus:border-accent rounded px-2 h-7 text-white font-bold text-sm outline-none transition-colors" 
                       />
                    </div>
                    <div className="w-px bg-gray-700 h-6"></div>
                    <div className="flex items-center gap-1.5 px-2">
                       <label className="text-xs text-gray-400 font-bold tracking-wider">NOTA (≥):</label>
                       <input 
                          type="number" 
                          step="0.1" 
                          min="0"
                          max="10"
                          title="Filtre por Nota Média mínima"
                          value={minRatingFilter} 
                          onChange={(e) => setMinRatingFilter(e.target.value)} 
                          className="w-14 bg-[#1f2229] border border-gray-600 focus:border-accent rounded px-2 h-7 text-white font-bold text-sm outline-none transition-colors" 
                       />
                    </div>
                    <button onClick={() => {setMinMinutesFilter(''); setMinRatingFilter(''); setExcludedPlayers([]);}} className="p-1.5 text-gray-500 hover:text-red-400" title="Limpar Filtros"><FilterX className="w-4 h-4"/></button>
                    <div className="w-px bg-gray-700 h-6 mx-1"></div>
                    {hasLocalStorage && (
                       <button onClick={clearLocalStorage} className="p-1.5 rounded transition max-h-full mr-1 text-red-400 hover:text-red-300 hover:bg-gray-800" title="Apagar Cache e Reimportar">
                          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                       </button>
                    )}
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
            ) : playersData.length === 0 ? (
              <div className="flex-1 flex items-center justify-center min-h-[400px]">
                <label className="w-full cursor-pointer h-full border-2 border-dashed border-accent/40 hover:border-accent bg-accent/5 hover:bg-accent/10 transition-all rounded-2xl flex flex-col items-center justify-center py-20 px-4 group">
                  <div className="w-20 h-20 bg-[#1f2229] border border-gray-700 rounded-full flex items-center justify-center group-hover:scale-110 transition-transform shadow-2xl mb-4">
                    <Upload className="w-8 h-8 text-accent" />
                  </div>
                  <h3 className="text-xl font-bold mb-2">Importar Exportação HTML</h3>
                  <p className="text-gray-400 text-center max-w-sm mb-4">Arraste seu arquivo <span className="text-white font-mono bg-gray-800 px-1 rounded">.html</span> da lista de jogadores da referida posição aqui dentro.</p>
                  <span className="bg-gray-800 text-white text-xs px-4 py-1.5 rounded-full border border-gray-700 font-medium">Ler Arquivo</span>
                  <input type="file" accept=".html,.htm" className="hidden" onChange={handleFileUpload} />
                </label>
              </div>
            ) : (
              <div className="flex-1 space-y-4">
                 {/* VIEW DE CARDS */}
                 {viewMode === 'cards' ? (
                   <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4 gap-4">
                     {displayPlayers.map((player, i) => (
                       <div key={player.uid + i} className="bg-[#081014] rounded-xl border border-[#2a303c] overflow-hidden text-left flex flex-col w-full shadow-[0_8px_30px_rgb(0,0,0,0.5)] relative max-w-sm mx-auto hover:border-gray-500 transition-colors">
                          {/* Header */}
                          <div className="p-3 flex gap-3 border-b border-[#2a303c] relative overflow-hidden bg-[#111822]">
                             {/* Background accent line on top */}
                             <div className="absolute top-0 left-0 right-0 h-[2px] bg-gradient-to-r from-purple-500 via-accent to-blue-500"></div>

                             <button 
                                onClick={(e) => { e.stopPropagation(); setExcludedPlayers(prev => [...prev, player.uid || player.Jogador]); }}
                                className="absolute top-1 right-1 p-1 text-gray-500 hover:text-red-500 hover:bg-red-500/10 rounded transition-colors z-20"
                                title="Remover Jogador"
                              >
                                <X className="w-3.5 h-3.5" />
                              </button>

                             <div className="w-[60px] h-[75px] rounded border border-gray-600 bg-gradient-to-t from-[#2a303c] to-[#4a5568] relative z-10 shrink-0 overflow-hidden flex items-end justify-center">
                                <img src={getAvatarUrl(player.uid)} alt="Face" className="min-w-[115%] min-h-[115%] object-contain object-bottom" onError={(e) => { e.target.onerror = null; e.target.src = `https://ui-avatars.com/api/?name=${encodeURIComponent(player['Jogador'])}&background=2a303c&color=ffffff&size=128`; }} />
                             </div>
                             
                             <div className="flex flex-col flex-1 justify-center z-10 max-w-[calc(100%-75px)]">
                                <div className="flex items-center gap-2 mb-0.5">
                                  <h3 className="text-white font-bold text-[15px] leading-tight truncate" title={player['Jogador']}>{player['Jogador']}</h3>
                                  <span className="bg-gray-300 text-gray-900 text-[9px] font-black px-1 rounded flex-shrink-0">{player.Age}</span>
                                </div>
                                
                                <p className="text-gray-400 text-xs font-medium leading-tight truncate mt-1">{player.Position || CATEGORIES.find(c => c.id === selectedCategory)?.label}</p>
                                
                                <div className="text-[11px] text-gray-400 mt-1 truncate flex items-center">
                                   <span className="font-bold text-accent/80">{player.Nation}</span>
                                   <span className="mx-1.5 text-gray-600">|</span>
                                   <span className="truncate">{player.Club || 'Livre'}</span>
                                </div>
                                
                                <div className="text-[10px] text-gray-400 truncate flex gap-1.5 items-center my-1 font-medium bg-gray-800/30 px-1.5 py-0.5 rounded border border-gray-700 w-fit">
                                   <span title="Salário Mensal">💰 {player.Wage || 'N/A'}</span> <span className="opacity-40">|</span> <span title="Valor Estimado">💎 {player.Value || 'N/A'}</span> <span className="opacity-40">|</span> <span title="Garantido até">🗓️ {(player.Expires || '').split(' ')[0] || 'N/A'}</span>
                                </div>
                                
                                <div className="flex items-center gap-2 mt-1">
                                   <button onClick={(e) => { e.stopPropagation(); openPlayerModal(player); }} className="bg-purple-600/90 hover:bg-purple-500 border border-purple-500/50 text-white text-[10px] font-bold px-2 py-1 rounded w-fit flex items-center gap-1.5 transition-colors shadow-lg">
                                     <span className="text-[8px]">▼</span>
                                     Gráfico
                                   </button>
                                </div>
                              </div>
                           </div>

                           {/* HEADER UNIFICADO */}
                           <div className="flex border-b border-[#2a303c] bg-[#141820] items-center justify-between px-3 py-2">
                               <div className="flex items-center gap-3">
                                  <div className="flex flex-col items-center justify-center -mt-0.5">
                                      <span className="text-blue-300 font-black text-xl leading-none">{player._rawMinutes || 0}</span>
                                      <span className="text-[8px] text-gray-400 uppercase font-black tracking-[0.2em] mt-1">Minutos</span>
                                  </div>
                               </div>
                               
                               {player.isGoalkeeper ? (
                                 /* BADGES DO GOLEIRO */
                                 <div className="flex items-center gap-1.5 pl-2">
                                    <div className="flex flex-col items-center justify-center bg-[#1f2229] border border-gray-700 w-12 py-1 rounded shadow-inner" title="Clean Sheets">
                                      <span className="text-[8px] text-gray-400 font-bold leading-none mb-0.5 uppercase tracking-wider">CS</span>
                                      <span className="text-emerald-400 font-black text-[13px] leading-none">{player.GK_CleanSheets || 0}</span>
                                    </div>

                                    <div className="flex flex-col items-center justify-center bg-[#1f2229] border border-gray-700 w-12 py-1 rounded shadow-inner" title="Gols Sofridos">
                                      <span className="text-[8px] text-gray-400 font-bold leading-none mb-0.5 uppercase tracking-wider">GS</span>
                                      <span className={`font-black text-[13px] leading-none ${(player.GK_GoalsConceded || 0) > 20 ? 'text-[#f87171]' : 'text-[#facc15]'}`}>{player.GK_GoalsConceded || 0}</span>
                                    </div>

                                    <div className="flex flex-col items-center justify-center bg-[#1f2229] border border-gray-700 w-12 py-1 rounded shadow-inner" title="Idade">
                                      <span className="text-[8px] text-gray-400 font-bold leading-none mb-0.5 uppercase tracking-wider">Idade</span>
                                      <span className="text-white font-black text-[13px] leading-none">{player.Age || '-'}</span>
                                    </div>

                                    <div className="flex flex-col items-center justify-center bg-[#1f2229] border border-gray-700 w-12 py-1 rounded shadow-inner" title="Classificação Média">
                                      <span className="text-[8px] text-gray-400 font-bold leading-none mb-0.5 uppercase tracking-wider">Nota</span>
                                      <span className="text-yellow-400 font-black text-[13px] leading-none">{player._rawRating || '-'}</span>
                                    </div>

                                    <div className="flex flex-col items-center justify-center bg-[#1f2229] border border-gray-700 px-2 py-1 rounded shadow-inner" title={'I.A. Rating (' + player._notaIA + ' pts)'}>
                                      <span className="text-[8px] text-gray-400 font-bold leading-none mb-0.5 uppercase tracking-wider">Rec. IA</span>
                                      <span className={(player._notaIA >= 50 ? 'text-accent ' : 'text-gray-300 ') + 'font-black text-[13px] leading-none'}>{player._notaIA}</span>
                                    </div>
                                 </div>
                               ) : (
                                 /* BADGES DO JOGADOR DE LINHA */
                                 <div className="flex items-center gap-1.5 pl-2">
                                    <FootIcon pe={player.Foot || player['Pé Preferido'] || player['Pé']} />
                                    
                                    <div className="flex flex-col items-center justify-center bg-[#1f2229] border border-gray-700 w-12 py-1 rounded shadow-inner" title="Dist / 90">
                                      <span className="text-[8px] text-gray-400 font-bold leading-none mb-0.5 uppercase tracking-wider">Dis/90</span>
                                      <span className={`font-black text-[13px] leading-none ${player.DistancePer90 >= 12.0 ? 'text-[#4ade80]' : player.DistancePer90 >= 10.0 ? 'text-[#facc15]' : 'text-[#f87171]'}`}>{typeof player.DistancePer90 === 'number' ? player.DistancePer90.toFixed(1) : player.DistancePer90 || '-'}</span>
                                    </div>

                                    <div className="flex flex-col items-center justify-center bg-[#1f2229] border border-gray-700 w-12 py-1 rounded shadow-inner" title="Idade">
                                      <span className="text-[8px] text-gray-400 font-bold leading-none mb-0.5 uppercase tracking-wider">Idade</span>
                                      <span className="text-white font-black text-[13px] leading-none">{player.Age || '-'}</span>
                                    </div>

                                    <div className="flex flex-col items-center justify-center bg-[#1f2229] border border-gray-700 w-12 py-1 rounded shadow-inner" title="Classificação Média">
                                      <span className="text-[8px] text-gray-400 font-bold leading-none mb-0.5 uppercase tracking-wider">Nota</span>
                                      <span className="text-yellow-400 font-black text-[13px] leading-none">{player._rawRating || '-'}</span>
                                    </div>

                                    <div className="flex flex-col items-center justify-center bg-[#1f2229] border border-gray-700 px-2 py-1 rounded shadow-inner" title={'I.A. Rating (' + player._notaIA + ' pts)'}>
                                      <span className="text-[8px] text-gray-400 font-bold leading-none mb-0.5 uppercase tracking-wider">Rec. IA</span>
                                      <span className={(player._notaIA >= 50 ? 'text-accent ' : 'text-gray-300 ') + 'font-black text-[13px] leading-none'}>{player._notaIA}</span>
                                    </div>
                                 </div>
                               )}
                           </div>

                           {/* STATS AREA */}
                           <div className="p-3">
                              {player.isGoalkeeper ? (
                                <>
                                {/* ====== CARD DE GOLEIRO ====== */}
                                {/* 🧤 DEFESAS */}
                                <div className="flex mb-1.5">
                                   <div className="w-6 shrink-0 flex flex-col justify-center items-center border border-gray-800 rounded-l overflow-hidden relative bg-[#0a1e16]">
                                      <span className="-rotate-90 absolute text-[9px] uppercase tracking-[0.25em] text-emerald-400 font-bold whitespace-nowrap opacity-90 shadow-sm drop-shadow-md">Defesas</span>
                                   </div>
                                   <div className="flex-1 border border-l-0 border-gray-800 rounded-r p-1.5 flex flex-col gap-0.5 bg-[#081014]">
                                      <StatRow label="Def. Totais" value={player.GK_SavesTotal} percentile={player.percentiles?.GK_SavesTotal || 0} tooltip="Total de defesas realizadas" />
                                      <StatRow label="Def. Seguras" value={player.GK_SavesSafe} percentile={player.percentiles?.GK_SavesSafe || 0} tooltip="Defesas seguras (encaixadas)" />
                                      <StatRow label="Def. P. Dedos" value={player.GK_SavesTipped} percentile={player.percentiles?.GK_SavesTipped || 0} tooltip="Defesas com a ponta dos dedos" />
                                      <StatRow label="Def. Desviadas" value={player.GK_SavesParried} percentile={player.percentiles?.GK_SavesParried || 0} tooltip="Defesas desviadas (espalmadas)" />
                                      <StatRow label="% Def Difíceis" value={typeof player.GK_DifficultSavePct === 'number' ? Math.round(player.GK_DifficultSavePct * 100) + '%' : player.GK_DifficultSavePct} percentile={player.percentiles?.GK_DifficultSavePct || 0} tooltip="Percentual de defesas difíceis" />
                                      <StatRow label="xG Defendidos" value={typeof player.GK_xGSaved === 'number' ? player.GK_xGSaved.toFixed(2) : player.GK_xGSaved} percentile={player.percentiles?.GK_xGSaved || 0} tooltip="Expected Goals defendidos" />
                                      <StatRow label="Pên Enfrent." value={player.GK_PenFaced} percentile={player.percentiles?.GK_PenFaced || 0} tooltip="Pênaltis enfrentados" />
                                      <StatRow label="Pên Defend." value={player.GK_PenSaved} percentile={player.percentiles?.GK_PenSaved || 0} tooltip="Pênaltis defendidos" />
                                   </div>
                                </div>

                                {/* ⚡ AÇÕES */}
                                <div className="flex mb-1.5">
                                   <div className="w-6 shrink-0 flex flex-col justify-center items-center border border-gray-800 rounded-l overflow-hidden relative bg-[rgba(15,30,60,0.8)]">
                                      <span className="-rotate-90 absolute text-[9px] uppercase tracking-[0.25em] text-blue-300 font-bold whitespace-nowrap opacity-90 drop-shadow-md">Ações</span>
                                   </div>
                                   <div className="flex-1 border border-l-0 border-gray-800 rounded-r p-1.5 flex flex-col gap-0.5 bg-[#081014]">
                                      <StatRow label="Saídas 1v1 T." value={player.GK_SweepAttempts} percentile={player.percentiles?.GK_SweepAttempts || 0} tooltip="Tentativas de saída do gol para 1v1" />
                                      <StatRow label="Saídas Suces." value={player.GK_SweepSuccess} percentile={player.percentiles?.GK_SweepSuccess || 0} tooltip="Saídas do gol com sucesso" />
                                      <StatRow label="Ações Tent." value={typeof player.GK_ActionsTried === 'number' ? Math.round(player.GK_ActionsTried) : player.GK_ActionsTried} percentile={player.percentiles?.GK_ActionsTried || 0} tooltip="Ações tentadas pelo goleiro" />
                                      <StatRow label="Ações Suces." value={typeof player.GK_ActionsSuccess === 'number' ? Math.round(player.GK_ActionsSuccess) : player.GK_ActionsSuccess} percentile={player.percentiles?.GK_ActionsSuccess || 0} tooltip="Ações com sucesso" />
                                   </div>
                                </div>

                                {/* 📐 PASSES */}
                                <div className="flex mb-1">
                                   <div className="w-6 shrink-0 flex flex-col justify-center items-center border border-gray-800 rounded-l overflow-hidden relative bg-[#1a1520]">
                                      <span className="-rotate-90 absolute text-[9px] uppercase tracking-[0.25em] text-purple-300 font-bold whitespace-nowrap opacity-90 drop-shadow-md">Passes</span>
                                   </div>
                                   <div className="flex-1 border border-l-0 border-gray-800 rounded-r p-1.5 flex flex-col gap-0.5 bg-[#081014]">
                                      <StatRow label="Passes Tent." value={player.GK_PassesAttempted} percentile={player.percentiles?.GK_PassesAttempted || 0} tooltip="Passes tentados" />
                                      <StatRow label="Passes Compl." value={player.GK_PassesCompleted} percentile={player.percentiles?.GK_PassesCompleted || 0} tooltip="Passes completados" />
                                   </div>
                                </div>
                                </>
                              ) : (
                                <>
                                {/* ====== CARD DE JOGADOR DE LINHA ====== */}
                                {/* AREA DEFESA */}
                                <div className="flex mb-1.5">
                                   <div className="w-6 shrink-0 flex flex-col justify-center items-center border border-gray-800 rounded-l overflow-hidden relative bg-[#0a1e16]">
                                      <span className="-rotate-90 absolute text-[9px] uppercase tracking-[0.25em] text-emerald-400 font-bold whitespace-nowrap opacity-90 shadow-sm drop-shadow-md">Defesa</span>
                                   </div>
                                   <div className="flex-1 border border-l-0 border-gray-800 rounded-r p-1.5 flex flex-col gap-0.5 bg-[#081014]">
                                      <StatRow label="Desarmes" value={player.TacklesAttempted} percentile={player.percentiles.TacklesAttempted} tooltip="Desarmes tentados durante os 90 minutos" />
                                      <StatRow label="Des Concl." value={player.TacklesWon} percentile={player.percentiles.TacklesWon} tooltip="Desarmes ganhos + Pressões concluídas" />
                                      <StatRow label="Press. Tent." value={player.PressuresAttempted} percentile={player.percentiles.PressuresAttempted} tooltip="Pressões tentadas sem a posse da bola" />
                                      <StatRow label="Press. Conc." value={player.PressuresWon} percentile={player.percentiles.PressuresWon} tooltip="Pressões concluídas" />
                                      <StatRow label="Cabeceios" value={player.HeadersAttempted} percentile={player.percentiles.HeadersAttempted} tooltip="Cabeceios disputados no ar" />
                                      <StatRow label="Cab Ganhos" value={player.HeadersWon} percentile={player.percentiles.HeadersWon} tooltip="Bolas aéreas ganhas via cabeceio" />
                                      <StatRow label="Interceptações" value={player.Interceptions} percentile={player.percentiles.Interceptions} tooltip="Interceptações de passes ou ações adversárias" />
                                   </div>
                                </div>

                                {/* POSSE */}
                                <div className="flex mb-1.5">
                                   <div className="w-6 shrink-0 flex flex-col justify-center items-center border border-gray-800 rounded-l overflow-hidden relative bg-[rgba(15,30,60,0.8)]">
                                      <span className="-rotate-90 absolute text-[9px] uppercase tracking-[0.25em] text-blue-300 font-bold whitespace-nowrap opacity-90 drop-shadow-md">Posse</span>
                                   </div>
                                   <div className="flex-1 border border-l-0 border-gray-800 rounded-r p-1.5 flex flex-col gap-0.5 bg-[#081014]">
                                      <StatRow label="Passes" value={player.PassesAttempted} percentile={player.percentiles.PassesAttempted} tooltip="Passes tentados ao longo do jogo" />
                                      <StatRow label="Pas Concl." value={player.PassesCompleted} percentile={player.percentiles.PassesCompleted} tooltip="Passes conseguidos" />
                                      <StatRow label="Dribles" value={player.Dribbles} percentile={player.percentiles.Dribbles} tooltip="Fintas e Dribles concluídos" />
                                      <StatRow label="Posse Perd." value={player.PossessionLost} percentile={player.percentiles.PeP} tooltip="Volume de Posses perdidas (Mínimo = Melhor)" />
                                      <StatRow label="Perd./90" value={player.PossessionLostPer90} percentile={player.percentiles.PossessionLost} tooltip="Posses perdidas a cada 90 minutos" />
                                   </div>
                                </div>

                                {/* ÚLTIMO TERÇO */}
                                <div className="flex mb-1">
                                   <div className="w-6 shrink-0 flex flex-col justify-center items-center border border-gray-800 rounded-l overflow-hidden relative bg-[#3f121d]">
                                      <span className="-rotate-90 absolute text-[9px] uppercase tracking-[0.25em] text-rose-300 font-bold whitespace-nowrap opacity-90 drop-shadow-md">Ataque</span>
                                   </div>
                                   <div className="flex-1 border border-l-0 border-gray-800 rounded-r p-1.5 flex flex-col gap-0.5 bg-[#081014]">
                                      <StatRow label="Passes Ch" value={player.KeyPasses} percentile={player.percentiles.KeyPasses} tooltip="Passe-Chave criativo e direcional para gol" />
                                      <StatRow label="Oport. Cla" value={player.OCG} percentile={player.percentiles.OCG} tooltip="Oportunidades Claras de Golo" />
                                      <StatRow label="Finaliz." value={player.Shots} percentile={player.percentiles.Shots} tooltip="Remates ou Finalizações desferidos" />
                                      <StatRow label="xA" value={player.ExpectedAssists} percentile={player.percentiles.ExpectedAssists} tooltip="Assistências Esperadas (xA)" />
                                      <StatRow label="Assistênc" value={player.Assists} percentile={player.percentiles.Assists} tooltip="Assistências concluídas para gol" />
                                      <StatRow label="xG" value={player.ExpectedGoals} percentile={player.percentiles.ExpectedGoals} tooltip="Gols Esperados (xG)" />
                                      <StatRow label="Gols" value={player.Goals} percentile={player.percentiles.Goals} tooltip="Gols marcados" />
                                   </div>
                                </div>
                                </>
                              )}
                           </div>
                       </div>
                     ))}
                   </div>
                 ) : (
                   /* VIEW DE TABELA */
                   <div className="overflow-x-auto rounded-xl border border-gray-700/50 bg-[#1f2229] shadow-lg scrollbar-hide h-[calc(100vh-200px)]">
                      <table className="w-full text-sm text-left">
                        <thead className="bg-[#181a20] text-xs text-gray-400 uppercase border-b border-gray-700 sticky top-0 z-40 shadow-sm">
                          <tr>
                            <th className="px-4 py-3 font-semibold sticky left-0 bg-[#181a20] z-50">P.</th>
                            {displayHeaders.map(colObj => {
                               const col = typeof colObj === 'string' ? colObj : colObj.id;
                               return (
                               <th 
                                 key={col} 
                                 onClick={() => requestSort(col)}
                                 className={`px-3 py-2 font-semibold min-w-[100px] whitespace-normal leading-tight text-center align-bottom cursor-pointer hover:text-white transition-colors border-x border-gray-800/50 select-none ${col.includes('Jogador') ? 'sticky left-[38px] bg-[#181a20] z-50 shadow-[2px_0_10px_rgba(0,0,0,0.5)] border-r-gray-700' : ''}`}
                               >
                                 <div className={`flex items-center justify-center gap-1 ${col.includes('Jogador') ? 'justify-start ml-2' : ''}`}>
                                   {col}
                                   {sortConfig.key === col && (
                                     <span className="text-accent">{sortConfig.direction === 'asc' ? '↑' : '↓'}</span>
                                   )}
                                 </div>
                               </th>
                               );
                            })}

                          </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-800/60">
                            {displayPlayers.map((player, i) => (
                              <tr key={player.uid + i} className="hover:bg-gray-800/80 cursor-pointer transition-colors" onClick={() => openPlayerModal(player)}>
                                <td className="px-4 py-3 font-bold text-center sticky left-0 z-10 text-gray-500 bg-[#1f2229] border-r border-gray-800/50">{i + 1}</td>
                            {displayHeaders.map(colObj => {
                               const col = typeof colObj === 'string' ? colObj : colObj.id;
                               return (
                               <td 
                                  key={col} 
                                  className={`px-3 py-2 text-gray-300 text-center whitespace-nowrap border-x border-gray-800/50 transition-colors ${col.includes('Jogador') ? 'sticky left-[38px] bg-[#1f2229] border-r-gray-700 z-10 shadow-[2px_0_10px_rgba(0,0,0,0.5)]' : ''}`}
                                  style={getCellStyle(col, player)}
                               >
                                  {formatCellValue(player[col], colObj)}
                               </td>
                               );
                            })}
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

      {isModalOpen && selectedPlayer && (
         <MoneyballPlayerModal 
            player={selectedPlayer} 
            onClose={() => setIsModalOpen(false)} 
         />
      )}
    </div>
  );
}
