const fs = require('fs');
let text = fs.readFileSync('src/components/ferramentas/MoneyballAnalyzer.jsx', 'utf8');

// The new header content
const newHeader = `                           {/* HEADER UNIFICADO (Mustermann Mod) */}
                           <div className="flex border-b border-[#2a303c] bg-[#141820] items-center justify-between px-3 py-2">
                               <div className="flex flex-col items-center justify-center -mt-0.5">
                                   <span className="text-blue-300 font-black text-xl leading-none">{player._rawMinutes || 0}</span>
                                   <span className="text-[8px] text-gray-400 uppercase font-black tracking-[0.2em] mt-1">Minutos</span>
                               </div>
                               
                               <div className="flex items-center gap-1.5">
                                   <FootIcon pe={player.Foot || player['Pé Preferido'] || player['Pé']} />
                                   
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
                                     <span className={(player._notaIA >= 50 ? 'text-accent ' : 'text-gray-300 ') + 'font-black text-[13px] leading-none'}>{player._notaIA >= 50 ? '🌟 ' : ''}{player._notaIA}</span>
                                   </div>
                               </div>
                           </div>\n`;

let lines = text.split(/\r?\n/);

// Find indices
let startIdx = lines.findIndex(l => l.includes('{/* MIDDLE INFO (Tags) */}'));
let endIdx = lines.findIndex(l => l.includes('{/* STATS AREA */}'));

if (startIdx !== -1 && endIdx !== -1) {
    // Delete from startIdx up to endIdx - 1
    lines.splice(startIdx, endIdx - startIdx, newHeader);
}

fs.writeFileSync('src/components/ferramentas/MoneyballAnalyzer.jsx', lines.join('\n'));
console.log('Analyzer Header Patched.');

// Patch Logic
let mlText = fs.readFileSync('src/components/ferramentas/MoneyballLogic.js', 'utf8');

const regexMax = /Math\.max\(\.\.\.players\.map\((p => p\.[A-Za-z0-9_]+)\),\ 0\.01\)/g;

mlText = mlText.replace(regexMax, function(match, arrowFunc) {
   return "Math.max(...(players.filter(x => !x._rawMinutes || x._rawMinutes >= 270).length >= 5 ? players.filter(x => !x._rawMinutes || x._rawMinutes >= 270).map(" + arrowFunc + ") : players.map(" + arrowFunc + ")), 0.01)";
});

fs.writeFileSync('src/components/ferramentas/MoneyballLogic.js', mlText);
console.log('Logic Max Values Patched.');
