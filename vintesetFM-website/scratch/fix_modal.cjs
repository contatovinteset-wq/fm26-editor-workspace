const fs = require('fs');
const path = 'src/components/ferramentas/MoneyballPlayerModal.jsx';
let c = fs.readFileSync(path, 'utf8');

// 1. Replace legend: find the 3 <li> lines (DOURADO, PRATA, ESMERALDA) and wrap in conditional
const legendRegex = /(\s*)<li><span className="text-\[#FFD700\] font-black tracking-wide">DOURADO:<\/span>[^<]*<\/li>\n(\s*)<li><span className="text-\[#E2E8F0\] font-black tracking-wide">PRATA:<\/span>[^<]*<\/li>\n(\s*)<li><span className="text-\[#10B981\] font-black tracking-wide">ESMERALDA:<\/span>[^<]*<\/li>/;

const legendMatch = c.match(legendRegex);
if (legendMatch) {
  const indent = legendMatch[1]; // capture the indentation
  const newLegend = `${indent}{isGK ? (
${indent}  <>
${indent}  <li><span className="text-[#10B981] font-black tracking-wide">ESMERALDA:</span> Defesas totais, seguras, desviadas, ponta dos dedos, % difíceis, xG defendidos e pênaltis.</li>
${indent}  <li><span className="text-[#3B82F6] font-black tracking-wide">AZUL:</span> Ações do goleiro: saídas 1v1 e ações gerais.</li>
${indent}  <li><span className="text-[#A78BFA] font-black tracking-wide">ROXO:</span> Distribuição com os pés: passes tentados e completados.</li>
${indent}  </>
${indent}) : (
${indent}  <>
${indent}  <li><span className="text-[#FFD700] font-black tracking-wide">DOURADO:</span> Métricas ofensivas, finalização e Último Terço.</li>
${indent}  <li><span className="text-[#E2E8F0] font-black tracking-wide">PRATA:</span> Capacidade de progressão, passes e Construção.</li>
${indent}  <li><span className="text-[#10B981] font-black tracking-wide">ESMERALDA:</span> Intensidade defensiva, desarmes e disputas físicas.</li>
${indent}  </>
${indent})}`;
  c = c.replace(legendRegex, newLegend);
  console.log('Legend replaced!');
} else {
  console.log('Legend regex NOT matched');
}

// 2. Replace POLAR_METRICS.map with metrics.map
const polar1 = c.split('POLAR_METRICS.map').length - 1;
c = c.replace(/POLAR_METRICS\.map/g, 'metrics.map');
console.log(`Replaced ${polar1} occurrences of POLAR_METRICS.map`);

// 3. Replace POLAR_METRICS.length with metrics.length (if any remain)
const polar2 = c.split('POLAR_METRICS.length').length - 1;
c = c.replace(/POLAR_METRICS\.length/g, 'metrics.length');
console.log(`Replaced ${polar2} occurrences of POLAR_METRICS.length`);

fs.writeFileSync(path, c, 'utf8');
console.log('File saved!');
