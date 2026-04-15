const fs = require('fs');

let text = fs.readFileSync('src/components/ferramentas/MoneyballAvancados.js', 'utf8');

// 1. Fix 100%-calc['BH']
text = text.replace("100%-calc['BH']", "1-calc['BH']");

// 2. Extract AX and AY
const ax_ay = `  // Col 50 (AX) : Pênaltis batidos
  calc['AX'] = g(row, 'Pens');
  // Col 51 (AY) : Pênaltis marcados
  calc['AY'] = g(row, 'Pens M', 'Pen M');
`;
text = text.replace(ax_ay, "");

// 3. Extract CF to CN
const regex = /  \/\/ Col 84 \(CF\)[\s\S]*?calc\['CN'\].*?;\n/;
const match = text.match(regex);
let cf_cn = "";
if (match) {
    cf_cn = match[0];
    text = text.replace(cf_cn, "");
}

// 4. Insert after Z
const zLine = "  calc['Z'] = IFERROR(calc['X']+calc['Y'], 0);\n";
text = text.replace(zLine, zLine + "\n" + ax_ay + cf_cn);

fs.writeFileSync('src/components/ferramentas/MoneyballAvancados.js', text, 'utf8');
console.log("Fixed dependencies!");
