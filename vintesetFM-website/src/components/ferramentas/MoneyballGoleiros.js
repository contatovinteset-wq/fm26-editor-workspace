// Auto-generated MoneyballGoleiros.js

const ROW = () => 1;
const IFERROR = (val, errVal) => {
  if (val === null || val === undefined) return errVal;
  if (typeof val === 'string' && val.includes('%')) val = parseFloat(val.replace('%',''))/100;
  if (isNaN(val) || !isFinite(val)) return errVal;
  return val;
};
const SUBSTITUTE = (text, oldText, newText) => {
  let res = String(text || '').split(oldText).join(newText);
  if (oldText === '.' && newText === ',') return res.replace(',', '.');
  return res;
};
const IF = (cond, t, f) => cond ? t : f;
const VALUE = (text) => parseFloat(String(text || '').replace(',', '.'));

export const processGoleirosRow = (row, index) => {
  const g = (r, key, k2) => {
      let v = r[key] !== undefined ? r[key] : (k2 && r[k2] !== undefined ? r[k2] : undefined);
      if (v === undefined) v = '';
      if (typeof v === 'string') {
          if (v.trim() === '-' || v.trim() === '') return '';
          let asNum = v.replace(',', '.').replace('%', '');
          let n = Number(asNum);
          if (!isNaN(n) && isFinite(n) && asNum.trim() !== '') return v.includes('%') ? n/100 : n;
      }
      return v;
  };
  const calc = {};
  calc['ROW'] = ROW(index);

  calc['CL'] = g(row, 'Inf');
  calc['CM'] = g(row, 'Nação', 'NAC');
  calc['CN'] = g(row, 'Clube', 'Equipe');
  calc['CO'] = g(row, 'Idade', 'Idade.1');
  calc['CP'] = g(row, 'Jogador', 'Jogador.1');
  calc['CQ'] = g(row, 'Valor Estimado', 'Valor Estimado.1');
  calc['CR'] = g(row, 'Pé Preferido', 'Pé preferido');
  calc['CS'] = g(row, 'Salário', 'Salário.1');
  calc['CT'] = g(row, 'Altura', 'Altura.1');
  calc['CU'] = g(row, 'Expira', 'Data final de contrato');
  calc['CV'] = g(row, 'HdJ', 'Man of the match');
  calc['CW'] = g(row, 'Minutos');
  calc['CX'] = g(row, 'Golos Sofridos', 'Gols Sofridos');
  calc['CY'] = g(row, 'Ds', 'Defesas Seguras');
  calc['CZ'] = g(row, 'Dft', 'Defesas Com a Ponta dos Dedos');
  calc['DA'] = g(row, 'Dfa', 'Defesas Desviadas');
  calc['DB'] = g(row, 'Press. tent.');
  calc['DC'] = g(row, 'Poss Con/90');
  calc['DD'] = g(row, 'Poss Perd/90');
  calc['DE'] = g(row, 'Sem golos sofridos', 'Clean Sheet');
  calc['DF'] = g(row, 'Amr', 'Cartões Amarelos');
  calc['DG'] = g(row, 'Cartões vermelhos', 'Cartões Vermelhos');
  calc['DH'] = g(row, 'Faltas Cometidas');
  calc['DI'] = g(row, 'Faltas Contra', 'Faltas Sofridas');
  calc['DJ'] = g(row, 'PeP', 'Passes em progressão');
  calc['DK'] = g(row, 'Passes Ch', 'Passes Decisivos');
  calc['DL'] = g(row, 'Pas A', 'Passes Tentados');
  calc['DM'] = g(row, 'Ps C', 'Passes completados');
  calc['DN'] = g(row, 'xGD', 'xG Defendidos');
  calc['DO'] = g(row, 'Press. conc.');
  calc['DP'] = g(row, 'EPG', 'Falhas');
  calc['DQ'] = g(row, 'T Desa', 'Tentativas de desarme');
  calc['DR'] = g(row, 'Des C', 'Desarmes completos');
  calc['DS'] = g(row, 'Crt D', 'Cortes decisivos');
  calc['DT'] = g(row, 'Pen. Enfrentados', 'Pênaltis enfrentados');
  calc['DU'] = g(row, 'Pen. Defendidos', 'Pênaltis Defendidos');
  calc['DV'] = g(row, 'Classificação', 'Nota média');

  Object.defineProperties(calc, {
    'A': { get: function() { return ROW()-1 + "º"; }, enumerable: true },
    'B': { get: function() { return this['CP']; }, enumerable: true },
    'C': { get: function() { return this['CM']; }, enumerable: true },
    'D': { get: function() { return this['CR']; }, enumerable: true },
    'E': { get: function() { return this['CN']; }, enumerable: true },
    'F': { get: function() { return this['CO']; }, enumerable: true },
    'G': { get: function() { return this['CQ']; }, enumerable: true },
    'H': { get: function() { return IFERROR(SUBSTITUTE(this['CT']," cm","")/100,0); }, enumerable: true },
    'I': { get: function() { return IF(this['CU']=="-", "Sem Data Final", this['CU']); }, enumerable: true },
    'J': { get: function() { return IFERROR(this['CS'],0); }, enumerable: true },
    'K': { get: function() { return IFERROR(0,0); }, enumerable: true }, // Média de jogos handled externally
    'L': { get: function() { return IFERROR(this['CW']/90,0); }, enumerable: true },
    'M': { get: function() { return this['CV']; }, enumerable: true },
    'N': { get: function() { return IFERROR((this['CW']/this['M']),5400); }, enumerable: true },
    'O': { get: function() { return IFERROR((this['M']*1)/this['L'],0); }, enumerable: true },
    'P': { get: function() { return this['DL']; }, enumerable: true },
    'Q': { get: function() { return this['DM']; }, enumerable: true },
    'R': { get: function() { return IFERROR((this['Q']/this['L']),0); }, enumerable: true },
    'S': { get: function() { return IFERROR(this['P']-this['Q'],0); }, enumerable: true },
    'T': { get: function() { return IFERROR(this['Q']-this['S'],0); }, enumerable: true },
    'U': { get: function() { return IFERROR(((this['P']-this['Q'])*1)/this['P'],0); }, enumerable: true },
    'V': { get: function() { return IFERROR((this['Q']*1)/this['P'],0); }, enumerable: true },
    'W': { get: function() { return this['DL']-this['DJ']; }, enumerable: true },
    'X': { get: function() { return IFERROR(this['W']/this['L'],0); }, enumerable: true },
    'Y': { get: function() { return this['DJ']; }, enumerable: true },
    'Z': { get: function() { return IFERROR(this['Y']/this['L'],0); }, enumerable: true },
    'AA': { get: function() { return IFERROR(this['Y']/this['W'],0); }, enumerable: true },
    'AB': { get: function() { return this['DK']; }, enumerable: true },
    'AC': { get: function() { return IFERROR(this['AB']/this['L'],0); }, enumerable: true },
    'AD': { get: function() { return this['CY']; }, enumerable: true },
    'AE': { get: function() { return IFERROR(this['AD']/this['L'],0); }, enumerable: true },
    'AF': { get: function() { return IFERROR((this['AD']*1)/this['AP'],0); }, enumerable: true },
    'AG': { get: function() { return this['CZ']; }, enumerable: true },
    'AH': { get: function() { return IFERROR(this['AG']/this['L'],0); }, enumerable: true },
    'AI': { get: function() { return IFERROR((this['AG']*1)/this['AP'],0); }, enumerable: true },
    'AJ': { get: function() { return this['DA']; }, enumerable: true },
    'AK': { get: function() { return IFERROR(this['AJ']/this['L'],0); }, enumerable: true }, // Fixed bug from /R2 to /L2
    'AL': { get: function() { return IFERROR((this['AJ']*1)/this['AP'],0); }, enumerable: true },
    'AM': { get: function() { return IFERROR((this['AN']*1)/this['L'],0); }, enumerable: true },
    'AN': { get: function() { return this['DE']; }, enumerable: true },
    'AO': { get: function() { return IFERROR(this['AN']/this['L'],0); }, enumerable: true },
    'AP': { get: function() { return IFERROR(this['CY']+this['CZ']+this['DA'],0); }, enumerable: true },
    'AQ': { get: function() { return IFERROR(this['AP']/this['L'],0); }, enumerable: true },
    'AR': { get: function() { return IFERROR((this['CZ']+this['DA']+this['CY']+this['CX']),0); }, enumerable: true },
    'AS': { get: function() { return IFERROR(this['AR']/this['L'],0); }, enumerable: true },
    'AT': { get: function() { return IFERROR(this['AP']/this['AR'],0); }, enumerable: true },
    'AU': { get: function() { return IFERROR(((this['CZ']+this['DA']+this['BI'])*1)/(this['AR']-this['CY']),0); }, enumerable: true },
    'AV': { get: function() { return IFERROR((this['BN']*1)/this['AS'],1); }, enumerable: true },
    'AW': { get: function() { return IFERROR((this['AG']*2+this['AJ']*1.5+this['AD']*1)/this['AR'],0); }, enumerable: true },
    'AX': { get: function() { return IFERROR(this['CW']/this['CX'],0); }, enumerable: true },
    'AY': { get: function() { return IFERROR((this['CX']+this['CY']+this['CZ']+this['DA'])/this['CX'],0); }, enumerable: true },
    'AZ': { get: function() { return IFERROR((this['CX']+this['CZ']+this['DA'])/this['CX'],0); }, enumerable: true },
    'BA': { get: function() { return IF(this['DN']=="-",0,VALUE(SUBSTITUTE(this['DN'],".",","))); }, enumerable: true },
    'BB': { get: function() { return IFERROR(this['BA']/this['L'],0); }, enumerable: true },
    'BC': { get: function() { return IFERROR(this['BA']-(this['BI']*0.79),-100); }, enumerable: true },
    'BD': { get: function() { return IFERROR(this['BC']/this['L'],0); }, enumerable: true },
    'BE': { get: function() { return IFERROR(this['BB']/this['AO'],this['BB']); }, enumerable: true },
    'BF': { get: function() { return IFERROR(this['BA']/(this['BA']+this['CX']),0); }, enumerable: true },
    'BG': { get: function() { return this['DT']; }, enumerable: true },
    'BH': { get: function() { return IFERROR(this['BG']/this['L'],0); }, enumerable: true },
    'BI': { get: function() { return this['DU']; }, enumerable: true },
    'BJ': { get: function() { return IFERROR(this['BI']/this['L'],0); }, enumerable: true },
    'BK': { get: function() { return IFERROR((this['BI']*1)/this['BG'],0); }, enumerable: true },
    'BL': { get: function() { return 0; }, enumerable: true }, // Requires SUM(CX:CX), handled via 0 or removed
    'BM': { get: function() { return this['CX']; }, enumerable: true },
    'BN': { get: function() { return IFERROR(this['BM']/this['L'],0); }, enumerable: true },
    'BO': { get: function() { return this['DP']; }, enumerable: true },
    'BP': { get: function() { return IFERROR(this['BO']/this['L'],0); }, enumerable: true },
    'BQ': { get: function() { return IFERROR(this['BR']*this['L'],0); }, enumerable: true },
    'BR': { get: function() { return this['DC']; }, enumerable: true },
    'BS': { get: function() { return IFERROR(this['BT']*this['L'],0); }, enumerable: true },
    'BT': { get: function() { return this['DD']; }, enumerable: true },
    'BU': { get: function() { return IFERROR((this['DB']+this['DQ']+(this['DS']*2.5)+this['DF']+this['DP']),0); }, enumerable: true },
    'BV': { get: function() { return IFERROR(this['BU']/this['L'],0); }, enumerable: true },
    'BW': { get: function() { return IFERROR((this['DO']+this['DR']+(this['DS']*2.5)),0); }, enumerable: true },
    'BX': { get: function() { return IFERROR(this['BW']/this['L'],0); }, enumerable: true },
    'BY': { get: function() { return IFERROR((this['DB']-this['DO'])+(this['DQ']-this['DR'])+(this['DS']*2.5)+(this['DF']),0); }, enumerable: true },
    'BZ': { get: function() { return IFERROR(this['BY']/this['L'],0); }, enumerable: true },
    'CA': { get: function() { return IFERROR((this['BW']*1)/this['BU'],0); }, enumerable: true },
    'CB': { get: function() { return this['DF']; }, enumerable: true },
    'CC': { get: function() { return this['DG']; }, enumerable: true },
    'CD': { get: function() { return IFERROR(this['CB']+this['CC'],0); }, enumerable: true },
    'CE': { get: function() { return this['DI']; }, enumerable: true },
    'CF': { get: function() { return this['DH']; }, enumerable: true },
    'CG': { get: function() { return IFERROR(this['CY']+this['CX']+this['CZ']+this['DA']+this['DT']+this['DL']+this['DF']+(this['DD']*1.33)+(this['DE']*2)+(this['DP']*2.5)+this['DQ']+this['DB'],0); }, enumerable: true },
    'CH': { get: function() { return IFERROR(this['CY']+this['CZ']+this['DA']+this['DU']+this['DM']+this['DR']+this['DO'],0); }, enumerable: true },
    'CI': { get: function() { return IFERROR((this['CH']*1)/this['CG'],0); }, enumerable: true },
    'CJ': { get: function() { return 0; }, enumerable: true }, // SUM(AR:AR)
    'CK': { get: function() { return IFERROR(VALUE(SUBSTITUTE(this['DV'], ".", ",")),0); }, enumerable: true },
  });

  return {
    'Col_A': calc['A'],
    'Jogador': calc['B'],
    'NAC': calc['C'],
    'Pé preferido': calc['D'],
    'Equipe': calc['E'],
    'Idade': calc['F'],
    'Valor estimado': calc['G'],
    'Altura': calc['H'],
    'Data final de contrato': calc['I'],
    'Salário': calc['J'],
    'Média de jogos': calc['K'],
    'Jogos completos': calc['L'],
    'Man of the match': calc['M'],
    'Minutos pra ser o homem do jogo': calc['N'],
    '% de vezes que foi eleito o Homem do Jogo': calc['O'],
    'Passes Tentados': calc['P'],
    'Passes completados': calc['Q'],
    'Passes C / 90': calc['R'],
    'Passes errados': calc['S'],
    'Passes certos - errados': calc['T'],
    '% passes errados': calc['U'],
    '% Passes certos': calc['V'],
    'Passes Curtos': calc['W'],
    'Passes Curtos /90': calc['X'],
    'Passes em progressão': calc['Y'],
    'Pass Progr /90': calc['Z'],
    '% Passes em progressão': calc['AA'],
    'Passes Decisivos': calc['AB'],
    'Pass D/90': calc['AC'],
    'Defesas Seguras': calc['AD'],
    'Defesas Seguras /90': calc['AE'],
    '% Def Seguras': calc['AF'],
    'Defesas Com a Ponta dos Dedos': calc['AG'],
    'Defesas com a ponta dos dedos /90': calc['AH'],
    '% Def com a ponta dos dedos': calc['AI'],
    'Defesas Desviadas': calc['AJ'],
    'Defesas Desviadas /90': calc['AK'],
    '% Def Desviadas': calc['AL'],
    '% de jogos sem sofrer gol': calc['AM'],
    'Clean Sheet': calc['AN'],
    'Clean Sheets/90': calc['AO'],
    'Defesas totais': calc['AP'],
    'Defesas totais / Jogo': calc['AQ'],
    'Bolas enfrentadas': calc['AR'],
    'Bolas enf /90': calc['AS'],
    'Proporção de Defesas vs Chutes': calc['AT'],
    '% Def Dificeis': calc['AU'],
    'Chances de Sofrer um gol/90': calc['AV'],
    'Índice de Defesas Críticas': calc['AW'],
    'Minutos pra sofrer um gol': calc['AX'],
    'Bolas enfrentadas pra sofrer um gol': calc['AY'],
    'Bolas dificeis enfrentadas pra sofrer 1 gol': calc['AZ'],
    'xG Defendidos': calc['BA'],
    'xG defendidos / 90': calc['BB'],
    'xG Defendidos sem pênalti': calc['BC'],
    'xG Defendidos sem pênalti /90': calc['BD'],
    'xPG Ratio': calc['BE'],
    'Expected Goals Prevented xGP': calc['BF'],
    'Pênaltis enfrentados': calc['BG'],
    'Pen Enf/90': calc['BH'],
    'Pênaltis Defendidos': calc['BI'],
    'Pen Def/90': calc['BJ'],
    '% Pênaltis defendidos': calc['BK'],
    '% Gols Sofridos comparado aos outros goleiros': calc['BL'],
    'Gols Sofridos': calc['BM'],
    'Sofridos / jogo': calc['BN'],
    'Falhas': calc['BO'],
    'Falhas/90': calc['BP'],
    'Posse Ganha Total': calc['BQ'],
    'Posse Ganha /90': calc['BR'],
    'Posse Perdida Total': calc['BS'],
    'Posse Perdida /90': calc['BT'],
    'Tentativas de Saída do gol pra 1v1': calc['BU'],
    'Tentativas de saída do gol /90': calc['BV'],
    'Saídas do gol com sucesso': calc['BW'],
    'Saídas do gol com sucesso /90': calc['BX'],
    'Saídas do gol falhas': calc['BY'],
    'Saídas do gol falhas /90': calc['BZ'],
    '% De Acerto nas Saídas do gol': calc['CA'],
    'Cartões Amarelos': calc['CB'],
    'Cartões Vermelhos': calc['CC'],
    'Total cartões': calc['CD'],
    'Faltas Sofridas': calc['CE'],
    'Faltas cometidas': calc['CF'],
    'Ações tentadas': calc['CG'],
    'Ações com sucesso': calc['CH'],
    '% Acerto do goleiro': calc['CI'],
    'Soma de todas as bolas enfrentadas': calc['CJ'],
    'Nota média': calc['CK']
  };
};

export const getGoleirosHeaders = () => {
  return [
    { id: 'Jogador', type: 'text' },
    { id: 'NAC', type: 'text' },
    { id: 'Pé preferido', type: 'text' },
    { id: 'Equipe', type: 'text' },
    { id: 'Idade', type: 'number' },
    { id: 'Valor estimado', type: 'text' },
    { id: 'Altura', type: 'float' },
    { id: 'Data final de contrato', type: 'text' },
    { id: 'Salário', type: 'float' },
    { id: 'Média de jogos', type: 'number' },
    { id: 'Jogos completos', type: 'number' },
    { id: 'Man of the match', type: 'number' },
    { id: 'Minutos pra ser o homem do jogo', type: 'float' },
    { id: '% de vezes que foi eleito o Homem do Jogo', type: 'percentage' },
    { id: 'Passes Tentados', type: 'number' },
    { id: 'Passes completados', type: 'number' },
    { id: 'Passes C / 90', type: 'float' },
    { id: 'Passes errados', type: 'number' },
    { id: 'Passes certos - errados', type: 'number' },
    { id: '% passes errados', type: 'percentage' },
    { id: '% Passes certos', type: 'percentage' },
    { id: 'Passes Curtos', type: 'number' },
    { id: 'Passes Curtos /90', type: 'float' },
    { id: 'Passes em progressão', type: 'number' },
    { id: 'Pass Progr /90', type: 'float' },
    { id: '% Passes em progressão', type: 'percentage' },
    { id: 'Passes Decisivos', type: 'number' },
    { id: 'Pass D/90', type: 'float' },
    { id: 'Defesas Seguras', type: 'number' },
    { id: 'Defesas Seguras /90', type: 'float' },
    { id: '% Def Seguras', type: 'percentage' },
    { id: 'Defesas Com a Ponta dos Dedos', type: 'number' },
    { id: 'Defesas com a ponta dos dedos /90', type: 'float' },
    { id: '% Def com a ponta dos dedos', type: 'percentage' },
    { id: 'Defesas Desviadas', type: 'number' },
    { id: 'Defesas Desviadas /90', type: 'float' },
    { id: '% Def Desviadas', type: 'percentage' },
    { id: '% de jogos sem sofrer gol', type: 'percentage' },
    { id: 'Clean Sheet', type: 'number' },
    { id: 'Clean Sheets/90', type: 'float' },
    { id: 'Defesas totais', type: 'number' },
    { id: 'Defesas totais / Jogo', type: 'float' },
    { id: 'Bolas enfrentadas', type: 'number' },
    { id: 'Bolas enf /90', type: 'float' },
    { id: 'Proporção de Defesas vs Chutes', type: 'float' },
    { id: '% Def Dificeis', type: 'percentage' },
    { id: 'Chances de Sofrer um gol/90', type: 'float' },
    { id: 'Índice de Defesas Críticas', type: 'float' },
    { id: 'Minutos pra sofrer um gol', type: 'float' },
    { id: 'Bolas enfrentadas pra sofrer um gol', type: 'float' },
    { id: 'Bolas dificeis enfrentadas pra sofrer 1 gol', type: 'float' },
    { id: 'xG Defendidos', type: 'float' },
    { id: 'xG defendidos / 90', type: 'float' },
    { id: 'xG Defendidos sem pênalti', type: 'float' },
    { id: 'xG Defendidos sem pênalti /90', type: 'float' },
    { id: 'xPG Ratio', type: 'float' },
    { id: 'Expected Goals Prevented xGP', type: 'float' },
    { id: 'Pênaltis enfrentados', type: 'number' },
    { id: 'Pen Enf/90', type: 'float' },
    { id: 'Pênaltis Defendidos', type: 'number' },
    { id: 'Pen Def/90', type: 'float' },
    { id: '% Pênaltis defendidos', type: 'percentage' },
    { id: '% Gols Sofridos comparado aos outros goleiros', type: 'percentage' },
    { id: 'Gols Sofridos', type: 'number' },
    { id: 'Sofridos / jogo', type: 'float' },
    { id: 'Falhas', type: 'number' },
    { id: 'Falhas/90', type: 'float' },
    { id: 'Posse Ganha Total', type: 'number' },
    { id: 'Posse Ganha /90', type: 'float' },
    { id: 'Posse Perdida Total', type: 'number' },
    { id: 'Posse Perdida /90', type: 'float' },
    { id: 'Tentativas de Saída do gol pra 1v1', type: 'number' },
    { id: 'Tentativas de saída do gol /90', type: 'float' },
    { id: 'Saídas do gol com sucesso', type: 'number' },
    { id: 'Saídas do gol com sucesso /90', type: 'float' },
    { id: 'Saídas do gol falhas', type: 'number' },
    { id: 'Saídas do gol falhas /90', type: 'float' },
    { id: '% De Acerto nas Saídas do gol', type: 'percentage' },
    { id: 'Cartões Amarelos', type: 'number' },
    { id: 'Cartões Vermelhos', type: 'number' },
    { id: 'Total cartões', type: 'number' },
    { id: 'Faltas Sofridas', type: 'number' },
    { id: 'Faltas cometidas', type: 'number' },
    { id: 'Ações tentadas', type: 'float' },
    { id: 'Ações com sucesso', type: 'float' },
    { id: '% Acerto do goleiro', type: 'percentage' },
    { id: 'Soma de todas as bolas enfrentadas', type: 'number' },
    { id: 'Nota média', type: 'float' }
  ];
};
