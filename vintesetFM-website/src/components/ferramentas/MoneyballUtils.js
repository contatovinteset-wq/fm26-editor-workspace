function strToFloat(str) {
  if (!str) return 0;
  let s = str.trim();
  const lastComma = s.lastIndexOf(',');
  const lastPoint = s.lastIndexOf('.');

  if (lastComma > lastPoint && lastComma !== -1) {
      s = s.replace(/\./g, '').replace(',', '.');
  } else if (lastPoint > lastComma && lastPoint !== -1) {
      s = s.replace(/,/g, '');
  } else if (lastComma !== -1) {
      s = s.replace(',', '.');
  }
  return parseFloat(s) || 0;
}

export const parseValue = (val) => {
  if (val === undefined || val === null) return "";
  if (typeof val === 'number') return val;
  
  let cleanStr = val.toString().trim();
  if (cleanStr === '') return 0;
  if (cleanStr === '-') return '-';

  // Porcentagens (ex: "85%", "85.5 %", "-5%")
  if (/^-?[\d.,]+\s*%$/.test(cleanStr)) {
      return strToFloat(cleanStr.replace('%', ''));
  }
  
  // Valores monetários, números grandes, ou números puros
  // Exemplos suportados: "R$ 17.75M", "£ 50k", "15,5 mil", "R$ 10.000 p/mês", "1,50", "20"
  // Não dará match em "30/06/2026" (datas) ou "186 cm" (unidades não suportadas aqui) ou "Shrewsbury"
  const match = cleanStr.match(/^[^\d-]*(-?[\d.,]+)(?:\s*(k|m|mil|mi|b|bi))?(?:\s*(?:\/|p\/)[a-zãõéêíá]+)?$/i);
  
  if (match) {
      let num = strToFloat(match[1]);
      if (match[2]) {
          const suffix = match[2].toLowerCase();
          if (suffix === 'k' || suffix === 'mil') num *= 1000;
          else if (suffix === 'm' || suffix === 'mi') num *= 1000000;
          else if (suffix === 'b' || suffix === 'bi') num *= 1000000000;
      }
      return num;
  }
  
  // Caso não separe adequadamente (e.g., textos puros, datas e medidas "cm"), retorna original.
  return val;
};
