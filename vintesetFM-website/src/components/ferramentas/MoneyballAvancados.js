// Auto-generated MoneyballAvancados.js via Spec JSON
// Excel Helpers
const PARSE_GAMES = (val) => {
  let str = String(val || '').trim();
  if (!str) return { total: 0, pctTitular: 0 };
  let hasSub = str.indexOf('(') !== -1;
  if (!hasSub) {
    let t = parseInt(str) || 0;
    return { total: t, pctTitular: t > 0 ? 1 : 0 };
  }
  let starts = parseInt(str.substring(0, str.indexOf('(')).trim()) || 0;
  let subs = parseInt(str.substring(str.indexOf('(') + 1, str.indexOf(')')).trim()) || 0;
  let total = starts + subs;
  return { total, pctTitular: total > 0 ? starts / total : 0 };
};
const ROW = () => 1; // Used for row counting placeholder
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
const FIND = (findText, text) => {
    let idx = String(text || '').indexOf(findText);
    if (idx === -1) throw new Error('not found');
    return idx + 1;
};
const LEFT = (text, num) => String(text || '').substring(0, num);
const MID = (text, start, num) => String(text || '').substring(start-1, start-1+num);
const ISERROR = (func) => { try { func(); return false; } catch { return true; } };
const TEXT = (val, format) => {
  if (format === '0%') return Math.round(val * 100) + '%';
  return String(val);
};

export const processAvancadosRow = (row, index) => {
  const n = (val) => { let v = parseFloat(val); return isNaN(v) ? 0 : v; };
  const g = (r, key, k2) => {
      let ptMap = { 'Golos marcados': 'Gols', 'Golos marcados de fora da área': 'Gols de fora da área', 'Remates': 'Finalizações', 'Fnt': 'Dribles', 'Sofridas': 'Faltas sofridas', 'Compr.': 'Comprimento' };
      let v = r[key] !== undefined ? r[key] : (k2 && r[k2] !== undefined ? r[k2] : undefined);
      if (v === undefined && ptMap[key] !== undefined && r[ptMap[key]] !== undefined) v = r[ptMap[key]];
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

  // Original Mappings based strictly on user output
  calc['B'] = g(row, 'Jogador', 'Nome');
  calc['EK'] = g(row, 'Inf'); // Input
  calc['EL'] = g(row, 'Nação'); // Input
  calc['EM'] = g(row, 'Jogador'); // Input
  calc['EN'] = g(row, 'Idade'); // Input
  calc['EO'] = g(row, 'Clube'); // Input
  calc['EP'] = g(row, 'Altura'); // Input
  calc['EQ'] = g(row, 'Pé Preferido'); // Input
  calc['ER'] = g(row, 'Valor Estimado'); // Input
  calc['ES'] = g(row, 'Salário'); // Input
  calc['ET'] = g(row, 'Expira'); // Input
  calc['EU'] = g(row, 'Minutos'); // Input
  calc['EV'] = g(row, 'Presenças'); // Input
  calc['EW'] = g(row, 'HdJ'); // Input
  calc['EX'] = g(row, 'Golos'); // Input
  calc['EY'] = g(row, 'Assist.'); // Input
  calc['EZ'] = g(row, 'OCG'); // Input
  calc['FA'] = g(row, 'Golos DS'); // Input
  calc['FB'] = g(row, 'Jogos DS'); // Input
  calc['FC'] = g(row, 'Poss Perd/90'); // Input
  calc['FD'] = g(row, 'xG'); // Input
  calc['FE'] = g(row, 'xA'); // Input
  calc['FF'] = g(row, 'Faltas Cometidas'); // Input
  calc['FG'] = g(row, 'Faltas Contra'); // Input
  calc['FH'] = g(row, 'Pas A'); // Input
  calc['FI'] = g(row, 'Ps C'); // Input
  calc['FJ'] = g(row, 'Passes Ch'); // Input
  calc['FK'] = g(row, 'PeP'); // Input
  calc['FL'] = g(row, 'Fnt'); // Input
  calc['FM'] = g(row, 'Remates'); // Input
  calc['FN'] = g(row, 'Rem %'); // Input
  calc['FO'] = g(row, 'Cab A'); // Input
  calc['FP'] = g(row, 'Cabs'); // Input
  calc['FQ'] = g(row, 'Press. tent.'); // Input
  calc['FR'] = g(row, 'Press. conc.'); // Input
  calc['FS'] = g(row, 'T Desa'); // Input
  calc['FT'] = g(row, 'Des C'); // Input
  calc['FU'] = g(row, 'Crt'); // Input
  calc['FV'] = g(row, 'CT-JA'); // Input
  calc['FW'] = g(row, 'CC-JA'); // Input
  calc['FX'] = g(row, 'Cr T'); // Input
  calc['FY'] = g(row, 'Cr C'); // Input
  calc['FZ'] = g(row, 'Golos marcados de fora da área'); // Input
  calc['GA'] = g(row, 'Remates de fora da área em cada 90 minutes'); // Input
  calc['GB'] = g(row, 'Remates em livres'); // Input
  calc['GC'] = g(row, 'Fj'); // Input
  calc['GD'] = g(row, 'Pens'); // Input
  calc['GE'] = g(row, 'Pens M'); // Input
  calc['GF'] = g(row, 'Sprints/90'); // Input
  calc['GG'] = g(row, 'Distância'); // Input
  calc['GH'] = g(row, 'Classificação'); // Input

  // Computed Values
  Object.defineProperties(calc, {
    'A': { get: function() { return ROW()-1 + "º"; }, enumerable: true }, // Col_A
    'B': { get: function() { return this['EM']; }, enumerable: true }, // Jogador
    'C': { get: function() { return this['EL']; }, enumerable: true }, // NAC
    'D': { get: function() { return this['EQ']; }, enumerable: true }, // Pé preferido
    'E': { get: function() { return this['EO']; }, enumerable: true }, // Equipe
    'F': { get: function() { return this['EP']; }, enumerable: true }, // Altura
    'G': { get: function() { return IF(this['ET']=="-", "Sem Data Final", this['ET']); }, enumerable: true }, // Data Final do contrato
    'H': { get: function() { return this['EN']; }, enumerable: true }, // Idade
    'I': { get: function() { return this['ES']; }, enumerable: true }, // Salário
    'J': { get: function() { return this['ER']; }, enumerable: true }, // Valor Estimado
    'K': { get: function() { return IFERROR(0 /* skip average logic */,0); }, enumerable: true }, // Média de jogos
    'L': { get: function() { return IFERROR(this['EU']/90,0); }, enumerable: true }, // Jogos completos
    'M': { get: function() { return PARSE_GAMES(this['EV']).total; }, enumerable: true }, // Jogos Totais
    'N': { get: function() { return IFERROR(this['EU']/this['M'],0); }, enumerable: true }, // Minutos por partida
    'O': { get: function() { return PARSE_GAMES(this['EV']).pctTitular; }, enumerable: true }, // Jogos como Titular
    'P': { get: function() { return this['FA']; }, enumerable: true }, // Gols na carreira
    'Q': { get: function() { return IFERROR(this['FA']/this['FB'],0); }, enumerable: true }, // Média de gols em toda a Carreira
    'R': { get: function() { return IFERROR(this['EX']/this['L'],0); }, enumerable: true }, // Média gols / partida
    'S': { get: function() { return IFERROR((this['X']+this['Y'])/this['L'],0); }, enumerable: true }, // Média gols + ass / partida
    'T': { get: function() { return IFERROR((this['Y'])/this['L'],0); }, enumerable: true }, // Ass / 90
    'U': { get: function() { return this['EW']; }, enumerable: true }, // Man of the match
    'V': { get: function() { return IFERROR(this['EU']/this['U'],5000); }, enumerable: true }, // Minutos pra ser o homem do jogo
    'W': { get: function() { return IFERROR(this['U']/this['L'],0); }, enumerable: true }, // % de vezes que foi eleito o Homem do Jogo
    'X': { get: function() { return this['EX']; }, enumerable: true }, // Gols
    'Y': { get: function() { return this['EY']; }, enumerable: true }, // Assist
    'Z': { get: function() { return IFERROR(this['X']+this['Y'],0); }, enumerable: true }, // Gols + Ass
    'AA': { get: function() { return this['EX']-this['GD']-this['FZ']; }, enumerable: true }, // Gols de dentro da área
    'AB': { get: function() { return this['FZ']; }, enumerable: true }, // Gols de fora da área
    'AC': { get: function() { return this['AY']; }, enumerable: true }, // Gols de Penaltis
    'AD': { get: function() { return IFERROR(this['EX']-this['GE'],0); }, enumerable: true }, // Gols Sem Pênalti
    'AE': { get: function() { return IFERROR((this['X']-this['AC'])/this['X'],0); }, enumerable: true }, // % Gols (sem penalti)
    'AF': { get: function() { return IFERROR(this['Y']/this['Z'],0); }, enumerable: true }, // % Ass
    'AG': { get: function() { return IFERROR((this['AY']*1)/this['Z'],0); }, enumerable: true }, // % Pênaltis
    'AH': { get: function() { return IFERROR(this['X']/this['L'],0); }, enumerable: true }, // Gols /90
    'AI': { get: function() { return IFERROR(this['Y']/this['L'],0); }, enumerable: true }, // Assist /90
    'AJ': { get: function() { return IFERROR((this['EX']+this['EY'])/this['L'],0); }, enumerable: true }, // Gols + Assist /90
    'AK': { get: function() { return IFERROR(this['AD']/this['L'],0); }, enumerable: true }, // Gols Sem Pênalti /90
    'AL': { get: function() { return IFERROR(this['EX']-this['FZ']-this['GD'],0); }, enumerable: true }, // Gols de dentro da área
    'AM': { get: function() { return IFERROR(this['AL']/this['L'],0); }, enumerable: true }, // Gols de dentro da área /90
    'AN': { get: function() { return this['FZ']; }, enumerable: true }, // Gols de fora da área
    'AO': { get: function() { return IFERROR(this['AN']/this['L'],0); }, enumerable: true }, // Gols de fora da área /90
    'AP': { get: function() { return this['GA']; }, enumerable: true }, // Chutes de fora da área /90
    'AQ': { get: function() { return IFERROR((this['AO']*1)/this['AP'],0); }, enumerable: true }, // % Conclusão dos chutes de fora da área
    'AR': { get: function() { return this['FX']-this['FV']; }, enumerable: true }, // Tentativas de  Criar  uma chance em Bola Parada
    'AS': { get: function() { return this['AR']/this['L']; }, enumerable: true }, // Tentativas/90
    'AT': { get: function() { return this['FY']-this['FW']; }, enumerable: true }, // Chances Criadas em  Bolas Paradas
    'AU': { get: function() { return this['AT']/this['L']; }, enumerable: true }, // Chances C /90
    'AV': { get: function() { return IFERROR(this['AT']/this['AR'], 0); }, enumerable: true }, // % Aproveitamento das Tentativas de Criar chance em  BP
    'AW': { get: function() { return this['GB']; }, enumerable: true }, // Cobranças de falta (Diretas)
    'AX': { get: function() { return this['GD']; }, enumerable: true }, // Pênaltis batidos
    'AY': { get: function() { return this['GE']; }, enumerable: true }, // Pênaltis marcados
    'AZ': { get: function() { return IFERROR(this['GD']-this['GE'],0); }, enumerable: true }, // Pênaltis perdidos
    'BA': { get: function() { return IFERROR(this['AY']/this['L'],0); }, enumerable: true }, // Gols de pen/90
    'BB': { get: function() { return IFERROR((this['AY']*1)/this['GD'],0.00001); }, enumerable: true }, // % Conversão de pênalti
    'BC': { get: function() { return this['FO']; }, enumerable: true }, // Cabeceios disputados
    'BD': { get: function() { return this['FP']; }, enumerable: true }, // Ganhos
    'BE': { get: function() { return IFERROR(this['BC']/this['L'],0); }, enumerable: true }, // Cabs Disputados /90
    'BF': { get: function() { return IFERROR(this['BD']/this['L'],0); }, enumerable: true }, // Ganhos /90
    'BG': { get: function() { return IFERROR(this['FO']-this['FP'],0); }, enumerable: true }, // Perdidos
    'BH': { get: function() { return IFERROR((this['BD']*1)/this['BC'],0); }, enumerable: true }, // % Cabs ganhos
    'BI': { get: function() { return IFERROR(1-this['BH'],0); }, enumerable: true }, // % Cabs perdidos
    'BJ': { get: function() { return this['GC']; }, enumerable: true }, // Impedimentos
    'BK': { get: function() { return IFERROR(this['GC']/this['L'],0); }, enumerable: true }, // Impedimentos / 90
    'BL': { get: function() { return this['FM']; }, enumerable: true }, // Finalizações
    'BM': { get: function() { return IFERROR(this['BL']/this['L'],0); }, enumerable: true }, // Finalizações /90
    'BN': { get: function() { return this['FN']; }, enumerable: true }, // Finalizações no Gol
    'BO': { get: function() { return IFERROR(this['BN']/this['L'],0); }, enumerable: true }, // Finalizações no gol/90
    'BP': { get: function() { return IFERROR((this['FN']*1)/this['FM'],0); }, enumerable: true }, // % Finalizações que foram no gol
    'BQ': { get: function() { return IFERROR((this['FM']-this['GD'])/(this['EX']-this['GE']),0); }, enumerable: true }, // Finalizações pra um gol
    'BR': { get: function() { return IFERROR((this['FN']-this['GD'])/(this['EX']-this['GE']),0); }, enumerable: true }, // Finalizações certas pra um gol
    'BS': { get: function() { return IFERROR((this['FM']+this['FO']-this['GD'])/(this['EX']-this['GE']),0); }, enumerable: true }, // Finalização ou Cabeceio pra um gol
    'BT': { get: function() { return IFERROR((this['EX']-this['GE'])/(this['FM']-this['GD']),0); }, enumerable: true }, // Finalizações que se converteram em gols
    'BU': { get: function() { return IFERROR(((this['X']-this['AY'])/this['CH'])*(this['AD']/this['L']),0); }, enumerable: true }, // GPI (Goal Probability Index)
    'BV': { get: function() { return IFERROR(this['CF']/(this['L']),0); }, enumerable: true }, // xG / Jogo
    'BW': { get: function() { return IFERROR(this['EX']/this['L'],0); }, enumerable: true }, // Gols convertidos /90
    'BX': { get: function() { return IFERROR(this['BW']-this['BV'],0); }, enumerable: true }, // Over xG / Under xG per 90
    'BY': { get: function() { return IFERROR(this['CH']/(this['FM']-this['GD']),0); }, enumerable: true }, // xG / chute
    'BZ': { get: function() { return IFERROR(this['EU']/this['FM'],0); }, enumerable: true }, // Minutos pra tentar uma finalização
    'CA': { get: function() { return IFERROR(this['EU']/this['FN'],0); }, enumerable: true }, // Minutos pra acertar uma finalização no gol
    'CB': { get: function() { return IFERROR(this['EU']/this['EX'],this['L']*90); }, enumerable: true }, // Minutos pra MARCAR um gol
    'CC': { get: function() { return IFERROR(this['EU']/this['Z'],300); }, enumerable: true }, // Minutos pra PARTICIPAR de um gol
    'CD': { get: function() { return IFERROR(this['EX']-this['CF'],0); }, enumerable: true }, // Gols não esperados
    'CE': { get: function() { return IFERROR((this['EX']-this['GE'])-this['CH'],0); }, enumerable: true }, // Gols não esperados SEM PÊNALTI
    'CF': { get: function() { return SUBSTITUTE(this['FD'],".","," )*1; }, enumerable: true }, // Gols esperados (xG)
    'CG': { get: function() { return IFERROR(this['CF']/this['L'],0); }, enumerable: true }, // xG /90
    'CH': { get: function() { return IFERROR(this['CF']-(this['GD']*0.79),0); }, enumerable: true }, // xG (Sem pênaltis)
    'CI': { get: function() { return IFERROR((this['CH']/this['L']),0); }, enumerable: true }, // xG (Sem pênaltis) /90
    'CJ': { get: function() { return SUBSTITUTE(this['FE'],".","," )*1; }, enumerable: true }, // Assistências Esperadas (xA)
    'CK': { get: function() { return IFERROR(this['CJ']/this['L'],0); }, enumerable: true }, // xA /90
    'CL': { get: function() { return IFERROR(this['CJ']+this['CH'],0); }, enumerable: true }, // xA + xG sem pen
    'CM': { get: function() { return IFERROR(this['CL']/this['L'],0); }, enumerable: true }, // xA + xG /90
    'CN': { get: function() { return this['EX']/(this['EX']+this['CF']); }, enumerable: true }, // xG Conclusion
    'CO': { get: function() { return this['FJ']; }, enumerable: true }, // Passes Decisivos
    'CP': { get: function() { return IFERROR(this['CO']/this['L'],0); }, enumerable: true }, // Pass D /90
    'CQ': { get: function() { return this['CJ']; }, enumerable: true }, // xA (assistências esperadas)
    'CR': { get: function() { return IFERROR(this['CJ']-this['EY'],0); }, enumerable: true }, // Chances criadas e não aproveitadas pela equipe / 90
    'CS': { get: function() { return this['FV']; }, enumerable: true }, // Cruzamentos Tentados
    'CT': { get: function() { return this['CS']/this['L']; }, enumerable: true }, // Cruzamentos T /90
    'CU': { get: function() { return this['FW']; }, enumerable: true }, // Cruzamentos Conseguidos
    'CV': { get: function() { return this['CU']/this['L']; }, enumerable: true }, // Cruzamentos C/90
    'CW': { get: function() { return IFERROR(this['CU']/this['CS'],0); }, enumerable: true }, // Cruzamentos
    'CX': { get: function() { return this['FL']; }, enumerable: true }, // Fintas
    'CY': { get: function() { return IFERROR(this['FL']/this['L'],0); }, enumerable: true }, // Fintas/90
    'CZ': { get: function() { return IFERROR((((this['DJ']*1000)/(this['EU']*60))*3600)/1000,0); }, enumerable: true }, // Velocidade Média (em km/h)
    'DA': { get: function() { return this['FQ']+this['FS']+this['FF']; }, enumerable: true }, // Desarme + Pressões Tentadas
    'DB': { get: function() { return IFERROR(this['DA']/this['L'],0); }, enumerable: true }, // Des + Pres T /90
    'DC': { get: function() { return IFERROR(this['FT']+this['FR'],0); }, enumerable: true }, // Desarme + Pressões Concluídas
    'DD': { get: function() { return IFERROR(this['DC']/this['L'],0); }, enumerable: true }, // Des + Pres C /90
    'DE': { get: function() { return IFERROR((this['DC']*1)/this['DA'], 0); }, enumerable: true }, // % Des + Pressões concluídas
    'DF': { get: function() { return this['FU']; }, enumerable: true }, // Interceptações
    'DG': { get: function() { return IFERROR(this['DF']/this['L'],0); }, enumerable: true }, // Int/90
    'DH': { get: function() { return this['FG']; }, enumerable: true }, // Faltas Sofridas
    'DI': { get: function() { return IFERROR(this['DH']/this['L'],0); }, enumerable: true }, // Faltas Sof/90
    'DJ': { get: function() { return VALUE(SUBSTITUTE(this['GG'], " km", "")); }, enumerable: true }, // Distância
    'DK': { get: function() { return IFERROR(this['DJ']/this['L'],0); }, enumerable: true }, // Dist / 90
    'DL': { get: function() { return IFERROR((this['GF']*this['L']),0); }, enumerable: true }, // Sprints de alta intensidade
    'DM': { get: function() { return this['GF']; }, enumerable: true }, // Sprints de alta intensidade/90
    'DN': { get: function() { return (this['FO']+this['FM']+this['GC']+this['FF']+this['FG']+this['FL']+this['FJ']+this['GD']+this['CF']); }, enumerable: true }, // Lances ofensivos tentados
    'DO': { get: function() { return IFERROR(this['DN']/this['L'],0); }, enumerable: true }, // Lances ofensivos / 90
    'DP': { get: function() { return (this['FN']+this['FG']+this['FL']+this['FJ']+this['GE']+this['EX']+this['EY']); }, enumerable: true }, // Lances ofensivos conseguidos
    'DQ': { get: function() { return IFERROR(this['DP']/this['L'],0); }, enumerable: true }, // Lances ofensivos conseguidos / 90
    'DR': { get: function() { return this['DS']*this['L']; }, enumerable: true }, // Posse Perdida
    'DS': { get: function() { return this['FC']; }, enumerable: true }, // Posse Perdida /90
    'DT': { get: function() { return IFERROR(this['DP']/this['DN'],0); }, enumerable: true }, // Eficácia ofensiva
    'DU': { get: function() { return IFERROR(this['FN']+this['CJ']+this['EZ']+this['FJ']+this['FZ']+this['FW']+(this['EX']*0.5)+(this['EY']*0.5),0); }, enumerable: true }, // Ações que geraram finalizações ao gol
    'DV': { get: function() { return IFERROR(this['DU']/this['L'],0); }, enumerable: true }, // Ações que geraram finalizações ao gol /90
    'DW': { get: function() { return IFERROR((this['GD']+this['FO']+this['FM']+this['FL']+this['FH']+this['FT'])/this['L'],0); }, enumerable: true }, // Participação do jogador a cada 90 minutos
    'DX': { get: function() { return IFERROR(this['FV']+this['FM']+this['FL']+this['FJ'],0); }, enumerable: true }, // Ações com Bola Tentadas
    'DY': { get: function() { return this['DX']/this['L']; }, enumerable: true }, // Ações com Bola T/90
    'DZ': { get: function() { return IFERROR(this['FW']+this['FN']+this['FL']+this['FJ'],0); }, enumerable: true }, // Ações com Bola (Completadas)
    'EA': { get: function() { return IFERROR(this['DZ']/this['L'],0); }, enumerable: true }, // Ações com Bola Comp /90
    'EB': { get: function() { return IFERROR(this['DZ']/this['DX'],0); }, enumerable: true }, // % Sucesso de ações com bola
    'EC': { get: function() { return IFERROR((this['FL']+this['FM']+(this['EZ']-this['GD'])+this['FJ']+this['DA']+this['FW']),0); }, enumerable: true }, // Ações no úúltimo terço 
    'ED': { get: function() { return IFERROR(this['EC']/this['L'],0); }, enumerable: true }, // Ações no úúltimo terço / 90
    'EE': { get: function() { return IFERROR((this['FM']+this['EZ']+this['FJ']),0); }, enumerable: true }, // Tentativas de marcar um gol (finalização e oportunidades criadas)
    'EF': { get: function() { return IFERROR(this['EE']/this['L'],0); }, enumerable: true }, // Tentativas de marcar um gol  /90
    'EG': { get: function() { return IFERROR((this['FM']-this['FN'])+(this['FH']-this['FI']),0); }, enumerable: true }, // Posse Desperdiçada
    'EH': { get: function() { return IFERROR(this['EG']/this['L'],0); }, enumerable: true }, // Posse Desperdiçada /90
    'EI': { get: function() { return this['FC']; }, enumerable: true }, // Posse perdida /90
    'EJ': { get: function() { return IFERROR(VALUE(SUBSTITUTE(this['GH'], ".", ",")),0); }, enumerable: true }, // Nota média
  });

  return {
    'Col_A': calc['A'],
    'Jogador': calc['B'],
    'NAC': calc['C'],
    'Pé preferido': calc['D'],
    'Equipe': calc['E'],
    'Altura': calc['F'],
    'Data Final do contrato': calc['G'],
    'Idade': calc['H'],
    'Salário': calc['I'],
    'Valor Estimado': calc['J'],
    'Média de jogos': calc['K'],
    'Jogos completos': calc['L'],
    'Jogos Totais': calc['M'],
    'Minutos por partida': calc['N'],
    'Jogos como Titular': calc['O'],
    'Gols na carreira': calc['P'],
    'Média de gols em toda a Carreira': calc['Q'],
    'Média gols / partida': calc['R'],
    'Média gols + ass / partida': calc['S'],
    'Ass / 90': calc['T'],
    'Man of the match': calc['U'],
    'Minutos pra ser o homem do jogo': calc['V'],
    '% de vezes que foi eleito o Homem do Jogo': calc['W'],
    'Gols': calc['X'],
    'Assist': calc['Y'],
    'Gols + Ass': calc['Z'],
    'Gols de dentro da área': calc['AA'],
    'Gols de fora da área': calc['AB'],
    'Gols de Penaltis': calc['AC'],
    'Gols Sem Pênalti': calc['AD'],
    '% Gols (sem penalti)': calc['AE'],
    '% Ass': calc['AF'],
    '% Pênaltis': calc['AG'],
    'Gols /90': calc['AH'],
    'Assist /90': calc['AI'],
    'Gols + Assist /90': calc['AJ'],
    'Gols Sem Pênalti /90': calc['AK'],
    'Gols de dentro da área': calc['AL'],
    'Gols de dentro da área /90': calc['AM'],
    'Gols de fora da área': calc['AN'],
    'Gols de fora da área /90': calc['AO'],
    'Chutes de fora da área /90': calc['AP'],
    '% Conclusão dos chutes de fora da área': calc['AQ'],
    'Tentativas de  Criar  uma chance em Bola Parada': calc['AR'],
    'Tentativas/90': calc['AS'],
    'Chances Criadas em  Bolas Paradas': calc['AT'],
    'Chances C /90': calc['AU'],
    '% Aproveitamento das Tentativas de Criar chance em  BP': calc['AV'],
    'Cobranças de falta (Diretas)': calc['AW'],
    'Pênaltis batidos': calc['AX'],
    'Pênaltis marcados': calc['AY'],
    'Pênaltis perdidos': calc['AZ'],
    'Gols de pen/90': calc['BA'],
    '% Conversão de pênalti': calc['BB'],
    'Cabeceios disputados': calc['BC'],
    'Ganhos': calc['BD'],
    'Cabs Disputados /90': calc['BE'],
    'Ganhos /90': calc['BF'],
    'Perdidos': calc['BG'],
    '% Cabs ganhos': calc['BH'],
    '% Cabs perdidos': calc['BI'],
    'Impedimentos': calc['BJ'],
    'Impedimentos / 90': calc['BK'],
    'Finalizações': calc['BL'],
    'Finalizações /90': calc['BM'],
    'Finalizações no Gol': calc['BN'],
    'Finalizações no gol/90': calc['BO'],
    '% Finalizações que foram no gol': calc['BP'],
    'Finalizações pra um gol': calc['BQ'],
    'Finalizações certas pra um gol': calc['BR'],
    'Finalização ou Cabeceio pra um gol': calc['BS'],
    'Finalizações que se converteram em gols': calc['BT'],
    'GPI (Goal Probability Index)': calc['BU'],
    'xG / Jogo': calc['BV'],
    'Gols convertidos /90': calc['BW'],
    'Over xG / Under xG per 90': calc['BX'],
    'xG / chute': calc['BY'],
    'Minutos pra tentar uma finalização': calc['BZ'],
    'Minutos pra acertar uma finalização no gol': calc['CA'],
    'Minutos pra MARCAR um gol': calc['CB'],
    'Minutos pra PARTICIPAR de um gol': calc['CC'],
    'Gols não esperados': calc['CD'],
    'Gols não esperados SEM PÊNALTI': calc['CE'],
    'Gols esperados (xG)': calc['CF'],
    'xG /90': calc['CG'],
    'xG (Sem pênaltis)': calc['CH'],
    'xG (Sem pênaltis) /90': calc['CI'],
    'Assistências Esperadas (xA)': calc['CJ'],
    'xA /90': calc['CK'],
    'xA + xG sem pen': calc['CL'],
    'xA + xG /90': calc['CM'],
    'xG Conclusion': calc['CN'],
    'Passes Decisivos': calc['CO'],
    'Pass D /90': calc['CP'],
    'xA (assistências esperadas)': calc['CQ'],
    'Chances criadas e não aproveitadas pela equipe / 90': calc['CR'],
    'Cruzamentos Tentados': calc['CS'],
    'Cruzamentos T /90': calc['CT'],
    'Cruzamentos Conseguidos': calc['CU'],
    'Cruzamentos C/90': calc['CV'],
    'Cruzamentos': calc['CW'],
    'Fintas': calc['CX'],
    'Fintas/90': calc['CY'],
    'Velocidade Média (em km/h)': calc['CZ'],
    'Desarme + Pressões Tentadas': calc['DA'],
    'Des + Pres T /90': calc['DB'],
    'Desarme + Pressões Concluídas': calc['DC'],
    'Des + Pres C /90': calc['DD'],
    '% Des + Pressões concluídas': calc['DE'],
    'Interceptações': calc['DF'],
    'Int/90': calc['DG'],
    'Faltas Sofridas': calc['DH'],
    'Faltas Sof/90': calc['DI'],
    'Distância': calc['DJ'],
    'Dist / 90': calc['DK'],
    'Sprints de alta intensidade': calc['DL'],
    'Sprints de alta intensidade/90': calc['DM'],
    'Lances ofensivos tentados': calc['DN'],
    'Lances ofensivos / 90': calc['DO'],
    'Lances ofensivos conseguidos': calc['DP'],
    'Lances ofensivos conseguidos / 90': calc['DQ'],
    'Posse Perdida': calc['DR'],
    'Posse Perdida /90': calc['DS'],
    'Eficácia ofensiva': calc['DT'],
    'Ações que geraram finalizações ao gol': calc['DU'],
    'Ações que geraram finalizações ao gol /90': calc['DV'],
    'Participação do jogador a cada 90 minutos': calc['DW'],
    'Ações com Bola Tentadas': calc['DX'],
    'Ações com Bola T/90': calc['DY'],
    'Ações com Bola (Completadas)': calc['DZ'],
    'Ações com Bola Comp /90': calc['EA'],
    '% Sucesso de ações com bola': calc['EB'],
    'Ações no úúltimo terço ': calc['EC'],
    'Ações no úúltimo terço / 90': calc['ED'],
    'Tentativas de marcar um gol (finalização e oportunidades criadas)': calc['EE'],
    'Tentativas de marcar um gol  /90': calc['EF'],
    'Posse Desperdiçada': calc['EG'],
    'Posse Desperdiçada /90': calc['EH'],
    'Posse perdida /90': calc['EI'],
    'Nota média': calc['EJ'],
  };
};

export const getAvancadosHeaders = () => {
  return [
    { id: 'Jogador', type: 'text' },
    { id: 'NAC', type: 'text' },
    { id: 'Pé preferido', type: 'text' },
    { id: 'Equipe', type: 'text' },
    { id: 'Altura', type: 'float' },
    { id: 'Data Final do contrato', type: 'text' },
    { id: 'Idade', type: 'text' },
    { id: 'Salário', type: 'float' },
    { id: 'Valor Estimado', type: 'text' },
    { id: 'Média de jogos', type: 'number' },
    { id: 'Jogos completos', type: 'number' },
    { id: 'Jogos Totais', type: 'number' },
    { id: 'Minutos por partida', type: 'number' },
    { id: 'Jogos como Titular', type: 'percentage' },
    { id: 'Gols na carreira', type: 'text' },
    { id: 'Média de gols em toda a Carreira', type: 'float' },
    { id: 'Média gols / partida', type: 'float' },
    { id: 'Média gols + ass / partida', type: 'float' },
    { id: 'Ass / 90', type: 'float' },
    { id: 'Man of the match', type: 'number' },
    { id: 'Minutos pra ser o homem do jogo', type: 'float' },
    { id: '% de vezes que foi eleito o Homem do Jogo', type: 'percentage' },
    { id: 'Gols', type: 'text' },
    { id: 'Assist', type: 'text' },
    { id: 'Gols + Ass', type: 'text' },
    { id: 'Gols de dentro da área', type: 'text' },
    { id: 'Gols de fora da área', type: 'text' },
    { id: 'Gols de Penaltis', type: 'number' },
    { id: 'Gols Sem Pênalti', type: 'number' },
    { id: '% Gols (sem penalti)', type: 'percentage' },
    { id: '% Ass', type: 'percentage' },
    { id: '% Pênaltis', type: 'percentage' },
    { id: 'Gols /90', type: 'float' },
    { id: 'Assist /90', type: 'float' },
    { id: 'Gols + Assist /90', type: 'float' },
    { id: 'Gols Sem Pênalti /90', type: 'float' },
    { id: 'Gols de dentro da área', type: 'number' },
    { id: 'Gols de dentro da área /90', type: 'float' },
    { id: 'Gols de fora da área', type: 'number' },
    { id: 'Gols de fora da área /90', type: 'float' },
    { id: 'Chutes de fora da área /90', type: 'float' },
    { id: '% Conclusão dos chutes de fora da área', type: 'percentage' },
    { id: 'Tentativas de  Criar  uma chance em Bola Parada', type: 'number' },
    { id: 'Tentativas/90', type: 'float' },
    { id: 'Chances Criadas em  Bolas Paradas', type: 'number' },
    { id: 'Chances C /90', type: 'float' },
    { id: '% Aproveitamento das Tentativas de Criar chance em  BP', type: 'percentage' },
    { id: 'Cobranças de falta (Diretas)', type: 'number' },
    { id: 'Pênaltis batidos', type: 'number' },
    { id: 'Pênaltis marcados', type: 'number' },
    { id: 'Pênaltis perdidos', type: 'number' },
    { id: 'Gols de pen/90', type: 'float' },
    { id: '% Conversão de pênalti', type: 'percentage' },
    { id: 'Cabeceios disputados', type: 'text' },
    { id: 'Ganhos', type: 'text' },
    { id: 'Cabs Disputados /90', type: 'float' },
    { id: 'Ganhos /90', type: 'float' },
    { id: 'Perdidos', type: 'text' },
    { id: '% Cabs ganhos', type: 'percentage' },
    { id: '% Cabs perdidos', type: 'percentage' },
    { id: 'Impedimentos', type: 'text' },
    { id: 'Impedimentos / 90', type: 'float' },
    { id: 'Finalizações', type: 'number' },
    { id: 'Finalizações /90', type: 'float' },
    { id: 'Finalizações no Gol', type: 'number' },
    { id: 'Finalizações no gol/90', type: 'float' },
    { id: '% Finalizações que foram no gol', type: 'percentage' },
    { id: 'Finalizações pra um gol', type: 'float' },
    { id: 'Finalizações certas pra um gol', type: 'float' },
    { id: 'Finalização ou Cabeceio pra um gol', type: 'float' },
    { id: 'Finalizações que se converteram em gols', type: 'percentage' },
    { id: 'GPI (Goal Probability Index)', type: 'float' },
    { id: 'xG / Jogo', type: 'float' },
    { id: 'Gols convertidos /90', type: 'float' },
    { id: 'Over xG / Under xG per 90', type: 'float' },
    { id: 'xG / chute', type: 'float' },
    { id: 'Minutos pra tentar uma finalização', type: 'float' },
    { id: 'Minutos pra acertar uma finalização no gol', type: 'float' },
    { id: 'Minutos pra MARCAR um gol', type: 'float' },
    { id: 'Minutos pra PARTICIPAR de um gol', type: 'float' },
    { id: 'Gols não esperados', type: 'float' },
    { id: 'Gols não esperados SEM PÊNALTI', type: 'float' },
    { id: 'Gols esperados (xG)', type: 'float' },
    { id: 'xG /90', type: 'float' },
    { id: 'xG (Sem pênaltis)', type: 'float' },
    { id: 'xG (Sem pênaltis) /90', type: 'float' },
    { id: 'Assistências Esperadas (xA)', type: 'float' },
    { id: 'xA /90', type: 'float' },
    { id: 'xA + xG sem pen', type: 'float' },
    { id: 'xA + xG /90', type: 'float' },
    { id: 'xG Conclusion', type: 'percentage' },
    { id: 'Passes Decisivos', type: 'number' },
    { id: 'Pass D /90', type: 'float' },
    { id: 'xA (assistências esperadas)', type: 'float' },
    { id: 'Chances criadas e não aproveitadas pela equipe / 90', type: 'float' },
    { id: 'Cruzamentos Tentados', type: 'number' },
    { id: 'Cruzamentos T /90', type: 'float' },
    { id: 'Cruzamentos Conseguidos', type: 'number' },
    { id: 'Cruzamentos C/90', type: 'float' },
    { id: 'Cruzamentos', type: 'percentage' },
    { id: 'Fintas', type: 'number' },
    { id: 'Fintas/90', type: 'float' },
    { id: 'Velocidade Média (em km/h)', type: 'float' },
    { id: 'Desarme + Pressões Tentadas', type: 'text' },
    { id: 'Des + Pres T /90', type: 'float' },
    { id: 'Desarme + Pressões Concluídas', type: 'text' },
    { id: 'Des + Pres C /90', type: 'float' },
    { id: '% Des + Pressões concluídas', type: 'percentage' },
    { id: 'Interceptações', type: 'number' },
    { id: 'Int/90', type: 'float' },
    { id: 'Faltas Sofridas', type: 'number' },
    { id: 'Faltas Sof/90', type: 'float' },
    { id: 'Distância', type: 'float' },
    { id: 'Dist / 90', type: 'float' },
    { id: 'Sprints de alta intensidade', type: 'number' },
    { id: 'Sprints de alta intensidade/90', type: 'float' },
    { id: 'Lances ofensivos tentados', type: 'number' },
    { id: 'Lances ofensivos / 90', type: 'float' },
    { id: 'Lances ofensivos conseguidos', type: 'number' },
    { id: 'Lances ofensivos conseguidos / 90', type: 'float' },
    { id: 'Posse Perdida', type: 'number' },
    { id: 'Posse Perdida /90', type: 'float' },
    { id: 'Eficácia ofensiva', type: 'percentage' },
    { id: 'Ações que geraram finalizações ao gol', type: 'number' },
    { id: 'Ações que geraram finalizações ao gol /90', type: 'float' },
    { id: 'Participação do jogador a cada 90 minutos', type: 'float' },
    { id: 'Ações com Bola Tentadas', type: 'number' },
    { id: 'Ações com Bola T/90', type: 'float' },
    { id: 'Ações com Bola (Completadas)', type: 'number' },
    { id: 'Ações com Bola Comp /90', type: 'float' },
    { id: '% Sucesso de ações com bola', type: 'percentage' },
    { id: 'Ações no úúltimo terço ', type: 'number' },
    { id: 'Ações no úúltimo terço / 90', type: 'float' },
    { id: 'Tentativas de marcar um gol (finalização e oportunidades criadas)', type: 'number' },
    { id: 'Tentativas de marcar um gol  /90', type: 'float' },
    { id: 'Posse Desperdiçada', type: 'number' },
    { id: 'Posse Desperdiçada /90', type: 'float' },
    { id: 'Posse perdida /90', type: 'float' },
    { id: 'Nota média', type: 'float' },
  ];
};