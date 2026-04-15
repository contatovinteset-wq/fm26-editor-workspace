import React from 'react';
import { X } from 'lucide-react';

const POLAR_METRICS = [
  // FINAL THIRD (Ataque) - Vermelho
  { key: 'Goals', label: 'Gols', color: '#FFD700' },
  { key: 'ExpectedGoals', label: 'Exp. Goals (xG)', color: '#FFD700' },
  { key: 'Shots', label: 'Finalizações', color: '#FFD700' },
  { key: 'Assists', label: 'Assistências', color: '#FFD700' },
  { key: 'ExpectedAssists', label: 'Exp. Assists (xA)', color: '#FFD700' },
  { key: 'KeyPasses', label: 'Passes Chave', color: '#FFD700' },
  
  // POSSESSION (Posse) - Amarelo
  { key: 'PassesAttempted', label: 'Passes Tentados', color: '#E2E8F0' },
  { key: 'ProgressivePasses', label: 'Passes Progressivos', color: '#E2E8F0' },
  { key: 'Dribbles', label: 'Dribles / Fintas', color: '#E2E8F0' },
  { key: 'PossessionLost', label: 'Posse Perdida', color: '#E2E8F0' },

  // DEFENDING (Defesa) - Azul Esverdeado
  { key: 'TackleWinRate', label: '% Desarmes', color: '#10B981' },
  { key: 'TacklesAttempted', label: 'Tent. Desarmes', color: '#10B981' },
  { key: 'Interceptions', label: 'Interceptações', color: '#10B981' },
  { key: 'Blocks', label: 'Bloqueios', color: '#10B981' },
  { key: 'HeaderWinRate', label: '% Aéreo', color: '#10B981' },
];

// MÉTRICAS POLARES ESPECÍFICAS DE GOLEIRO
const GK_POLAR_METRICS = [
  // 🧤 DEFESAS - Esmeralda
  { key: 'GK_SavesTotal', label: 'Def. Totais', color: '#10B981' },
  { key: 'GK_SavesSafe', label: 'Def. Seguras', color: '#10B981' },
  { key: 'GK_SavesTipped', label: 'Def. P. Dedos', color: '#10B981' },
  { key: 'GK_SavesParried', label: 'Def. Desviadas', color: '#10B981' },
  { key: 'GK_DifficultSavePct', label: '% Def Difíceis', color: '#10B981' },
  { key: 'GK_xGSaved', label: 'xG Defendidos', color: '#10B981' },
  { key: 'GK_PenFaced', label: 'Pên Enfrent.', color: '#10B981' },
  { key: 'GK_PenSaved', label: 'Pên Defend.', color: '#10B981' },
  
  // ⚡ AÇÕES - Azul
  { key: 'GK_SweepAttempts', label: 'Saídas 1v1 T.', color: '#3B82F6' },
  { key: 'GK_SweepSuccess', label: 'Saídas Suces.', color: '#3B82F6' },
  { key: 'GK_ActionsTried', label: 'Ações Tent.', color: '#3B82F6' },
  { key: 'GK_ActionsSuccess', label: 'Ações Suces.', color: '#3B82F6' },
  
  // 📐 PASSES - Roxo
  { key: 'GK_PassesAttempted', label: 'Passes Tent.', color: '#A78BFA' },
  { key: 'GK_PassesCompleted', label: 'Passes Compl.', color: '#A78BFA' },
];

const SVG_SIZE = 700;
const CENTER = SVG_SIZE / 2;
const MAX_RADIUS = 210;

const polarToCartesian = (centerX, centerY, radius, angleInDegrees) => {
  const angleInRadians = (angleInDegrees - 90) * Math.PI / 180.0;
  return {
    x: centerX + (radius * Math.cos(angleInRadians)),
    y: centerY + (radius * Math.sin(angleInRadians))
  };
};

const drawSlice = (radius, startAngle, endAngle) => {
  const start = polarToCartesian(CENTER, CENTER, radius, endAngle);
  const end = polarToCartesian(CENTER, CENTER, radius, startAngle);
  const largeArcFlag = endAngle - startAngle <= 180 ? "0" : "1";

  return [
    "M", CENTER, CENTER,
    "L", start.x, start.y, 
    "A", radius, radius, 0, largeArcFlag, 0, end.x, end.y,
    "Z"
  ].join(" ");
};

export default function MoneyballPlayerModal({ player, onClose }) {
  if (!player) return null;

  const isGK = player.isGoalkeeper;
  const metrics = isGK ? GK_POLAR_METRICS : POLAR_METRICS;
  const sliceAngle = 360 / metrics.length;

  const getAvatarUrl = (uid) => {
    return uid ? `https://sortitoutsi.b-cdn.net/uploads/face/face_${uid}.png` : '';
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm overflow-y-auto">
      {/* Container Principal */}
      <div className="bg-[#18232f] w-full max-w-6xl overflow-hidden rounded-xl shadow-2xl relative border border-gray-700/50 mt-10 mb-10 animation-fade-in">
        
        {/* Header - Tom Avermelhado */}
        <div className="bg-[#0b1016] bg-opacity-90 border-b-4 border-yellow-500 p-6 relative flex flex-col md:flex-row gap-6 md:items-center"
style={{ backgroundImage: "linear-gradient(to right, #0b1016, #141c27)" }}>
           <button onClick={(e) => { e.stopPropagation(); onClose(); }} className="absolute z-50 cursor-pointer top-4 right-4 text-white hover:text-white bg-black/40 hover:bg-black/80 px-2 py-2 rounded-full transition">
              <X className="w-6 h-6"/>
           </button>
           
           {/* Face */}
           <div className="w-28 h-28 shrink-0 bg-[#1f2229] rounded-lg overflow-hidden border-2 border-white/20 shadow-lg relative">
             <img src={getAvatarUrl(player.uid)} className="w-full h-full object-cover object-top" alt="Player Face" onError={(e) => { e.target.onerror = null; e.target.src = `https://ui-avatars.com/api/?name=${encodeURIComponent(player['Jogador'])}&background=181a20&color=fff&bold=true`; }} />
           </div>

           {/* Info */}
           <div className="flex-1 text-white">
              <h2 className="text-3xl font-black mb-1 drop-shadow-md tracking-tight uppercase">{player['Jogador'] || 'Unknown Player'}</h2>
              <div className="flex flex-col md:flex-row gap-1 md:gap-4 text-[13px] text-white/80 font-medium">
                 <div>
                    <p>{player['Idade'] ? `${player['Idade']} years old` : 'Idade N/D'}</p>
                    <p>{player['Altura'] || ''}</p>
                    <p>{player['Peso'] || ''}</p>
                    <p className="mt-1 font-bold text-white/90">Personalidade: {player['Personalidade'] || 'Equilibrado'}</p>
                 </div>
                 <div className="md:border-l md:border-white/20 md:pl-4">
                    <p className="font-bold text-yellow-400">{player['Clube'] || 'Livre'}</p>
                    <p className="text-gray-400">{player.Foot || player['Pé Preferido'] || 'Destro'}</p>
                    <p className="text-gray-400">{player.Wage || 'N/A'} - Exp. {(player.Expires || '').split(' ')[0] || 'N/A'}</p>
                    <p className="mt-1 font-bold text-accent">Valor: {player.Value || 'N/D'}</p>
                 </div>
              </div>
           </div>
        </div>

        {/* Corpo principal */}
        <div className="flex flex-col lg:flex-row p-8 text-gray-200 bg-[#0d1319]">
           {/* Explica��o do Gr�fico Polar */}
           {/* Lado Esquerdo - Estatísticas Base */}
           <div className="w-full lg:w-[280px] shrink-0 mb-8 lg:mb-0">
               <table className="w-full text-[11px] font-bold tracking-wider text-gray-400 uppercase">
                  <tbody>
                     <tr className="border-b border-gray-700/50">
                        <td className="py-3">Partidas</td>
                        <td className="py-3 text-right text-gray-200 font-medium text-sm">{player.Appearances || '0'}</td>
                     </tr>
                     <tr className="border-b border-gray-700/50">
                        <td className="py-3">Minutos Jogados</td>
                        <td className="py-3 text-right text-gray-200 font-medium text-sm">{player._rawMinutes || '0'}</td>
                     </tr>
                     {isGK ? (
                        <>
                        <tr className="border-b border-gray-700/50">
                           <td className="py-3">🧤 Defesas Totais</td>
                           <td className="py-3 text-right text-gray-200 font-medium text-sm">{player.GK_SavesTotal || '0'}</td>
                        </tr>
                        <tr className="border-b border-gray-700/50">
                           <td className="py-3">Clean Sheets</td>
                           <td className="py-3 text-right text-emerald-400 font-medium text-sm">{player.GK_CleanSheets || '0'}</td>
                        </tr>
                        <tr className="border-b border-gray-700/50">
                           <td className="py-3">Gols Sofridos</td>
                           <td className="py-3 text-right text-red-400 font-medium text-sm">{player.GK_GoalsConceded || '0'}</td>
                        </tr>
                        </>
                      ) : (
                        <>
                        <tr className="border-b border-gray-700/50">
                           <td className="py-3">Gols</td>
                           <td className="py-3 text-right text-gray-200 font-medium text-sm">{player.Goals || '0'}</td>
                        </tr>
                        <tr className="border-b border-gray-700/50">
                           <td className="py-3">Assistências</td>
                           <td className="py-3 text-right text-gray-200 font-medium text-sm">{player.Assist || '0'}</td>
                        </tr>
                        </>
                      )}
                     <tr className="">
                        <td className="py-3 text-accent font-black">Classificação Média</td>
                        <td className="py-3 text-right text-white font-black text-lg">{player._rawRating || '0.00'}</td>
                     </tr>
                  </tbody>
               </table>

               {/* Legenda do Gráfico Polar */}
               <div className="mt-8 p-5 bg-[#111822] border border-[#2a303c] rounded-lg shadow-md text-[13px] text-gray-400 leading-relaxed">
                  <h3 className="text-gray-200 font-bold mb-3 uppercase tracking-wider text-[12px] flex items-center gap-1.5">
                     <svg className="w-4 h-4 text-accent" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                     Entendendo o Gráfico
                  </h3>
                  <ul className="list-disc pl-4 space-y-2.5">
                     <li>O gráfico exibe os <strong className="text-gray-300">Percentis (0 a 100)</strong> comparando com os demais. Ex: um "90%" significa ser melhor que 90% dos jogadores importados.</li>
                     {isGK ? (

                       <>

                       <li><span className="text-[#10B981] font-black tracking-wide">ESMERALDA:</span> Defesas totais, seguras, desviadas, ponta dos dedos, % difíceis, xG defendidos e pênaltis.</li>

                       <li><span className="text-[#3B82F6] font-black tracking-wide">AZUL:</span> Ações do goleiro: saídas 1v1 e ações gerais.</li>

                       <li><span className="text-[#A78BFA] font-black tracking-wide">ROXO:</span> Distribuição com os pés: passes tentados e completados.</li>

                       </>

                     ) : (

                       <>

                       <li><span className="text-[#FFD700] font-black tracking-wide">DOURADO:</span> Métricas ofensivas, finalização e Último Terço.</li>

                       <li><span className="text-[#E2E8F0] font-black tracking-wide">PRATA:</span> Capacidade de progressão, passes e Construção.</li>

                       <li><span className="text-[#10B981] font-black tracking-wide">ESMERALDA:</span> Intensidade defensiva, desarmes e disputas físicas.</li>

                       </>

                     )}
                  </ul>
               </div>
           </div>

           {/* Lado Direito - Nightingale Rose Chart (Polar) */}
           <div className="flex-1 flex justify-center items-center relative">
               
               <div className="relative w-full h-[600px] flex justify-center items-center overflow-visible" style={{ minHeight: '600px' }}>
                  <svg width="100%" height="100%" viewBox={`0 0 ${SVG_SIZE} ${SVG_SIZE}`} preserveAspectRatio="xMidYMid meet" className="overflow-visible">
                     {/* Fundo do Radar (Círculos concêntricos) */}
                     <circle cx={CENTER} cy={CENTER} r={MAX_RADIUS} fill="none" stroke="#253545" strokeWidth="1" strokeDasharray="4 4" />
                     <circle cx={CENTER} cy={CENTER} r={MAX_RADIUS * 0.75} fill="none" stroke="#253545" strokeWidth="1" strokeDasharray="4 4" />
                     <circle cx={CENTER} cy={CENTER} r={MAX_RADIUS * 0.5} fill="none" stroke="#253545" strokeWidth="1" strokeDasharray="4 4" />
                     <circle cx={CENTER} cy={CENTER} r={MAX_RADIUS * 0.25} fill="none" stroke="#253545" strokeWidth="1" strokeDasharray="4 4" />

                     {/* Fatias do Gráfico */}
                     {metrics.map((metric, index) => {
                        const startAngle = index * sliceAngle;
                        const endAngle = (index + 1) * sliceAngle;
                        
                        // Pegar o percentil (0 a 100) e converter num raio (0 a MAX_RADIUS)
                        // Prevenir valores negativos ou nulos visuais muito absurdos
                        let percent = player.percentiles?.[metric.key] || 0;
                        if(percent < 5) percent = 5; // mínimo visual
                        if(percent > 100) percent = 100;
                        
                        const actualRadius = (percent / 100) * MAX_RADIUS;
                        
                        return (
                           <g key={metric.key}>
                              <path 
                                 d={drawSlice(actualRadius, startAngle, endAngle)} 
                                 fill={metric.color} 
                                 stroke="#18232f" 
                                 strokeWidth="2" 
                                 opacity="0.9"
                                 className="transition-all duration-500 origin-center hover:opacity-100 hover:brightness-110 cursor-pointer"
                              >
                                 <title>{metric.label}: {percent}%</title>
                              </path>
                           </g>
                        );
                     })}

                     {/* Linhas  Divisórias */}
                     {metrics.map((_, index) => {
                        const angle = index * sliceAngle;
                        const lineEnd = polarToCartesian(CENTER, CENTER, MAX_RADIUS, angle);
                        return (
                           <line 
                              key={`line-${index}`}
                              x1={CENTER} y1={CENTER} 
                              x2={lineEnd.x} y2={lineEnd.y} 
                              stroke="#18232f" strokeWidth="2" 
                           />
                        );
                     })}

                     {/* Botão Central "ALL" */}
                     <circle cx={CENTER} cy={CENTER} r="18" fill="#E2E8F0" stroke="#18232f" strokeWidth="3" />
                     <text x={CENTER} y={CENTER + 3} textAnchor="middle" fill="#18232f" fontSize="11" fontWeight="bold">ALL</text>

                     {/* Rótulos Radiais */}
                     {metrics.map((metric, index) => {
                        // Posicionar os textos fora do gráfico
                        const midAngle = (index + 0.5) * sliceAngle;
                        const textPos = polarToCartesian(CENTER, CENTER, MAX_RADIUS + 25, midAngle);
                        
                        // Pegar os valores raw da per/90
                        const isPercentageOnly = ['TackleWinRate', 'HeaderWinRate'].includes(metric.key);
                        const rawKey = isPercentageOnly ? metric.key : metric.key + 'Per90';
                        const val = isPercentageOnly ? Math.round(player[rawKey] || 0) + '%' : parseFloat(player[rawKey] || 0).toFixed(2);
                        const pct = player.percentiles?.[metric.key] || 0;

                        // Alinhamento inteligente baseado no lado
                        let anchor = 'middle';
                        if (textPos.x < CENTER - 20) anchor = 'end';
                        else if (textPos.x > CENTER + 20) anchor = 'start';

                        return (
                           <g key={`label-${index}`}>
                               {/* Dot indicator na frente do texto */}
                               <circle cx={anchor === 'start' ? textPos.x - 12 : anchor === 'end' ? textPos.x + 12 : textPos.x} cy={textPos.y - 12} r="5" fill={metric.color} />
                               <text 
                                  x={textPos.x} 
                                  y={textPos.y - 8} 
                                  textAnchor={anchor} 
                                  fill="#8ba3b8" 
                                  fontSize="12" 
                                  fontWeight="600"
                               >
                                  {metric.label}
                               </text>
                               <text 
                                  x={textPos.x} 
                                  y={textPos.y + 8} 
                                  textAnchor={anchor} 
                                  fill="#ffffff" 
                                  fontSize="14" 
                                  fontWeight="800"
                               >
                                  {val} <tspan fill="#94a3b8" fontSize="12" fontWeight="500">{pct}%</tspan>
                               </text>
                           </g>
                        );
                     })}
                  </svg>
               </div>

           </div>
        </div>

      </div>
    </div>
  );
}
