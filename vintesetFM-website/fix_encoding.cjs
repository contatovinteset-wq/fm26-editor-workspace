const fs = require('fs');

let c = fs.readFileSync('src/components/ferramentas/MoneyballAvancados.js', 'utf-8');

const replacements = {
  '\ufffd': '', // remove standard replacement chars if we had another way, but wait...
  'PǸ preferido': 'Pé preferido',
  'Salǭrio': 'Salário',
  'MǸdia': 'Média',
  'ǭrea': 'área',
  'PǦnalti': 'Pênalti',
  'Pnaltis': 'Pênaltis',
  'PǦnaltis': 'Pênaltis',
  'Conclusǜo': 'Conclusão',
  'Cobranas': 'Cobranças',
  'Conversǜo': 'Conversão',
  'Finalizaes': 'Finalizações',
  'Finalizaǜo': 'Finalização',
  'nǜo': 'não',
  'AssistǦncias': 'Assistências',
  'Eficǭcia': 'Eficácia',
  'Aes': 'Ações',
  'Participaǜo': 'Participação',
  'ǧltimo': 'último',
  'tero': 'terço',
  'Desperdiada': 'Desperdiçada',
  'Classificaǜo': 'Classificação',
  'Classificao': 'Classificação',
  'Naǜo': 'Nação',
  'Distǽncia': 'Distância',
  'PǸ Direito': 'Pé Direito',
  'PSNALTI': 'PÊNALTI'
};

for (const [bad, good] of Object.entries(replacements)) {
  // Global replace
  c = c.split(bad).join(good);
}

fs.writeFileSync('src/components/ferramentas/MoneyballAvancados.js', c);
console.log("Characters fixed.");
