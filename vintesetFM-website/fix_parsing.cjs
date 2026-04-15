const fs = require('fs');

let text = fs.readFileSync('src/components/ferramentas/MoneyballAnalyzer.jsx', 'utf8');

const helper = `
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
`;

// Insert the helper at the start of getCellStyle
text = text.replace("const getCellStyle = (colName, player) => {", "const getCellStyle = (colName, player) => {\n" + helper);

// Replace parseFloat
text = text.replace("let val = parseFloat(player[colName]);", "let val = parseFMValue(player[colName], colName);");

// Remove the Idade hack
text = text.replace("if (colName === 'Idade') { computedMin = 16; computedMax = 33; computedP50 = 24; }", "// removed idade hack");

fs.writeFileSync('src/components/ferramentas/MoneyballAnalyzer.jsx', text, 'utf8');
console.log('Fixed parsing in MoneyballAnalyzer.jsx');
