import re

with open('src/components/ferramentas/MoneyballAvancados.js', 'r', encoding='utf-8') as f:
    code = f.read()

# Add g function
helper = """const ISERROR = (val) => val === undefined || val === null || isNaN(val) || val === 0;
const TEXT = (val, format) => val;
const g = (row, ...keys) => {
  for (const k of keys) {
    if (row[k] !== undefined && row[k] !== "") return n(row[k]);
  }
  return 0;
};
"""
code = code.replace("const TEXT = (val, format) => val;", helper)

# Replace n(row['Golos']) with g(row, 'Golos', 'Gols')
code = re.sub(r"n\(row\['Golos'\]\)", r"g(row, 'Golos', 'Gols', 'Gols DS')", code)
code = re.sub(r"n\(row\['Assist\.'\]\)", r"g(row, 'Assist.', 'Assistências')", code)
code = re.sub(r"n\(row\['Golos marcados de fora da área'\]\)", r"g(row, 'Golos marcados de fora da área', 'Gols de fora da área')", code)
code = re.sub(r"n\(row\['Golos DS'\]\)", r"g(row, 'Golos DS', 'Gols na carreira', 'Gols DS')", code)
code = re.sub(r"n\(row\['Jogos DS'\]\)", r"g(row, 'Jogos DS', 'Jogos totais na carreira')", code)
code = re.sub(r"n\(row\['HdJ'\]\)", r"g(row, 'HdJ', 'Homem do Jogo')", code)
code = re.sub(r"n\(row\['xG'\]\)", r"g(row, 'xG')", code)
code = re.sub(r"n\(row\['xA'\]\)", r"g(row, 'xA')", code)
code = re.sub(r"n\(row\['Pens'\]\)", r"g(row, 'Pens')", code)
code = re.sub(r"n\(row\['Pens M'\]\)", r"g(row, 'Pens M', 'Pen M')", code)
code = re.sub(r"n\(row\['Minutos'\]\)", r"g(row, 'Minutos')", code)
code = re.sub(r"n\(row\['Altura'\]\)", r"g(row, 'Altura', 'Altura.1')", code)
code = re.sub(r"n\(row\['Idade'\]\)", r"g(row, 'Idade')", code)
code = re.sub(r"n\(row\['Salário'\]\)", r"g(row, 'Salário')", code)
code = re.sub(r"n\(row\['Valor Estimado'\]\)", r"g(row, 'Valor Estimado', 'Valor')", code)

with open('src/components/ferramentas/MoneyballAvancados.js', 'w', encoding='utf-8') as f:
    f.write(code)
print("Updated MoneyballAvancados.js")
