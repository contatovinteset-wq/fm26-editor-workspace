// Função para tratar parse e cálculo de dados dos jogadores exportados no Moneyball
import { processAvancadosRow, getAvancadosHeaders } from './MoneyballAvancados.js';

// Converter valores monetários ou strings (como "€1.2M", "5,55", "85%") para número
const parseMoneyballNumber = (val) => {
  if (!val) return 0;
  if (typeof val === 'number') return val;
  
  let cleanStr = val.toString().trim();
  if (cleanStr === '-' || cleanStr === '') return 0;
  
  // Tratar porcentagens (85% => 85)
  if (cleanStr.includes('%')) {
     return parseFloat(cleanStr.replace('%', '').replace(',', '.')) || 0;
  }
  
  // Tratar salários/valores monetários (ex: 1,5M, 500m)
  if (cleanStr.includes('M') || cleanStr.includes('m') || cleanStr.includes('k')) {
     let numStr = cleanStr.replace(/[^\d.,]/g, '').replace(',', '.');
     let num = parseFloat(numStr) || 0;
     if(cleanStr.toLowerCase().includes('m')) num *= 1000000;
     if(cleanStr.toLowerCase().includes('k')) num *= 1000;
     return num;
  }
  
  // Substituir vírgula por ponto (Formato EUR/BR para Float)
  cleanStr = cleanStr.replace(',', '.');
  return parseFloat(cleanStr) || 0;
};

// Funções para pegar dados baseados em colunas pt-BR padrão do export
const extractStat = (player, colName) => parseMoneyballNumber(player[colName]);

const calcPer90 = (stat, minutes) => {
   if (!minutes || minutes === 0) return 0;
   return (stat / minutes) * 90;
}

export const processMoneyballHtml = (html, positionSelected) => {
   const parser = new DOMParser();
   const doc = parser.parseFromString(html, 'text/html');
   const table = doc.querySelector('table');
   
   if (!table) throw new Error("A tabela não foi encontrada no arquivo enviado.");
   
   const isGoalkeeper = positionSelected === 'Goleiros';
   
   const rows = Array.from(table.querySelectorAll('tr'));
   const originalHeaders = Array.from(rows[0].querySelectorAll('th')).map(th => th.innerText.trim());
   
   let players = [];
   
   // Loop de linhas
   for (let i = 1; i < rows.length; i++) {
     const tds = Array.from(rows[i].querySelectorAll('td'));
     if (tds.length === 0) continue;
     
     const p = {};
     originalHeaders.forEach((header, index) => {
        if(tds[index]) p[header] = tds[index].innerText.trim();
     });
     
     // GERAR METRICAS DA PLANILHA PRIMEIRO (pular para goleiros)
     if (!isGoalkeeper) {
       const advancedP = processAvancadosRow(p, i);
       Object.assign(p, advancedP);
     }
     
     // 1. Limpeza Base
     const minutos = extractStat(p, 'Minutos') || extractStat(p, 'Min');
     const notaMedia = extractStat(p, 'Classificação') || extractStat(p, 'Clas. Méd') || extractStat(p, 'Nota média');
     
     p._rawMinutes = minutos;
     p._rawRating = notaMedia;
     
     // Extrair UID da foto se presente na tabela HTML ou diretamente da coluna de ID
     p.uid = p['Inf']?.match(/face_([0-9]*)/)?.[1] || p['ID Único'] || p['Unique ID'] || null; 
     
     if (isGoalkeeper) {
       // ====== EXTRAÇÃO ESPECÍFICA DE GOLEIRO ======
       p.isGoalkeeper = true;
       p['Jogos completos'] = minutos > 0 ? minutos / 90 : 0;

       // 🧤 DEFESAS
       p.GK_SavesTotal = extractStat(p, 'Defesas totais') || extractStat(p, 'Def') || extractStat(p, 'Defesas');
       p.GK_SavesTotalPer90 = calcPer90(p.GK_SavesTotal, minutos);
       p.GK_SavesSafe = extractStat(p, 'Defesas Seguras') || extractStat(p, 'D Seg') || extractStat(p, 'Def Seg');
       p.GK_SavesSafePer90 = calcPer90(p.GK_SavesSafe, minutos);
       p.GK_SavesTipped = extractStat(p, 'Defesas Com a Ponta dos Dedos') || extractStat(p, 'DPdD') || extractStat(p, 'D PdD') || extractStat(p, 'Def PD');
       p.GK_SavesTippedPer90 = calcPer90(p.GK_SavesTipped, minutos);
       p.GK_SavesParried = extractStat(p, 'Defesas Desviadas') || extractStat(p, 'D Desv') || extractStat(p, 'Def Desv');
       p.GK_SavesParriedPer90 = calcPer90(p.GK_SavesParried, minutos);
       p.GK_DifficultSavePct = extractStat(p, '% Def Dificeis') || extractStat(p, '% Def Difíceis');
       p.GK_xGSaved = extractStat(p, 'xG Defendidos') || extractStat(p, 'xGD') || extractStat(p, 'xG Def');
       p.GK_xGSavedPer90 = calcPer90(p.GK_xGSaved, minutos);
       p.GK_PenFaced = extractStat(p, 'Pênaltis enfrentados') || extractStat(p, 'Pên Enf') || extractStat(p, 'Pen Enf');
       p.GK_PenSaved = extractStat(p, 'Pênaltis Defendidos') || extractStat(p, 'Pên Def') || extractStat(p, 'Pen Def');

       // ⚡ AÇÕES
       p.GK_SweepAttempts = extractStat(p, 'Tentativas de Saída do gol pra 1v1') || extractStat(p, 'T Saída') || extractStat(p, 'Saídas T') || extractStat(p, 'Saídas 1v1');
       p.GK_SweepAttemptsPer90 = calcPer90(p.GK_SweepAttempts, minutos);
       p.GK_SweepSuccess = extractStat(p, 'Saídas do gol com sucesso') || extractStat(p, 'Saídas C') || extractStat(p, 'Saídas S');
       p.GK_SweepSuccessPer90 = calcPer90(p.GK_SweepSuccess, minutos);
       p.GK_ActionsTried = extractStat(p, 'Ações tentadas') || extractStat(p, 'Ações T');
       p.GK_ActionsTriedPer90 = calcPer90(p.GK_ActionsTried, minutos);
       p.GK_ActionsSuccess = extractStat(p, 'Ações com sucesso') || extractStat(p, 'Ações C');
       p.GK_ActionsSuccessPer90 = calcPer90(p.GK_ActionsSuccess, minutos);

       // 📐 PASSES
       p.GK_PassesAttempted = extractStat(p, 'Passes Tentados') || extractStat(p, 'Pas A') || extractStat(p, 'Passes A');
       p.GK_PassesAttemptedPer90 = calcPer90(p.GK_PassesAttempted, minutos);
       p.GK_PassesCompleted = extractStat(p, 'Passes completados') || extractStat(p, 'Ps C') || extractStat(p, 'Passes C');
       p.GK_PassesCompletedPer90 = calcPer90(p.GK_PassesCompleted, minutos);

       // EXTRAS para badges do card
       p.GK_CleanSheets = extractStat(p, 'Clean Sheet') || extractStat(p, 'J Limpos') || extractStat(p, 'Jogos Limpos') || extractStat(p, 'CS');
       p.GK_GoalsConceded = extractStat(p, 'Gols Sofridos') || extractStat(p, 'GS') || extractStat(p, 'Gols Sof');
       p.GK_SavePct = extractStat(p, '% Acerto do goleiro') || extractStat(p, '% Defesas') || extractStat(p, '% Def');
       p.GK_ConcededPerGame = extractStat(p, 'Sofridos / jogo') || extractStat(p, 'GS/Jogo');

     } else {
       // 2. Extração para Polar Chart (FINAL THIRD)
       p.Goals = extractStat(p, 'Golos');
       p.GoalsPer90 = calcPer90(p.Goals, minutos);
       p.ExpectedGoals = extractStat(p, 'xG SP') || extractStat(p, 'xG');
       p.ExpectedGoalsPer90 = calcPer90(p.ExpectedGoals, minutos);
       p.Shots = extractStat(p, 'Remates');
       p.ShotsPer90 = calcPer90(p.Shots, minutos);
       p.Assists = extractStat(p, 'Assist.');
       p.AssistsPer90 = calcPer90(p.Assists, minutos);
       p.ExpectedAssists = extractStat(p, 'xA');
       p.ExpectedAssistsPer90 = calcPer90(p.ExpectedAssists, minutos);
       p.KeyPasses = extractStat(p, 'Passes Ch');
       p.KeyPassesPer90 = calcPer90(p.KeyPasses, minutos);
       
       // CRIAÇÃO E POSSE (POSSESSION)
       p.Dribbles = extractStat(p, 'Fnt');
       p.DribblesPer90 = calcPer90(p.Dribbles, minutos);
       p.PossessionLost = (p['Posse Perdida'] !== undefined ? p['Posse Perdida'] : null) ?? extractStat(p, 'PeP') ?? extractStat(p, 'Posse Desperdiçada');
       const possLost90 = (p['Posse Perdida /90'] !== undefined ? p['Posse Perdida /90'] : null) ?? extractStat(p, 'Poss Perd/90');
       p.PossessionLostPer90 = possLost90 ? possLost90 : calcPer90(p.PossessionLost, minutos); // INVERSO
       p.ProgressivePasses = extractStat(p, 'Psg P') || p.KeyPasses; // Aproximação se não tiver Passe Prog
       p.ProgressivePassesPer90 = calcPer90(p.ProgressivePasses, minutos);
       p.PassesAttempted = extractStat(p, 'Pas A');
       p.PassesAttemptedPer90 = calcPer90(p.PassesAttempted, minutos);
       p.PassesCompleted = extractStat(p, 'Ps C');
       p.PassesCompletedPer90 = calcPer90(p.PassesCompleted, minutos);
       p.PeP = p.PossessionLost;
       p.PePPer90 = p.PossessionLostPer90;
       // DEFESA (DEFENDING)
        p.HeadersAttempted = extractStat(p, "Cab A");
        p.HeadersAttemptedPer90 = calcPer90(p.HeadersAttempted, minutos);
        p.HeadersWon = extractStat(p, "Cabs");
        p.HeadersWonPer90 = calcPer90(p.HeadersWon, minutos);
        p.HeaderWinRate = p.HeadersAttempted > 0 ? Math.round((p.HeadersWon / p.HeadersAttempted) * 100) : 0;
        
        p.TacklesAttempted = extractStat(p, "T Desa");
        p.TacklesAttemptedPer90 = calcPer90(p.TacklesAttempted, minutos);
        p.TacklesWon = extractStat(p, "Des C");
        p.TacklesWonPer90 = calcPer90(p.TacklesWon, minutos);
        p.TackleWinRate = p.TacklesAttempted > 0 ? Math.round((p.TacklesWon / p.TacklesAttempted) * 100) : 0;
        
        p.PressuresAttempted = extractStat(p, "Press. tent.");
        p.PressuresAttemptedPer90 = calcPer90(p.PressuresAttempted, minutos);
        p.PressuresWon = extractStat(p, "Press. conc.");
        p.PressuresWonPer90 = calcPer90(p.PressuresWon, minutos);
        p.PressureWinRate = p.PressuresAttempted > 0 ? Math.round((p.PressuresWon / p.PressuresAttempted) * 100) : 0;
        
        p.Clearances = extractStat(p, "Crt");
        p.ClearancesPer90 = calcPer90(p.Clearances, minutos);
        
        p.Interceptions = extractStat(p, "Interceptações") || extractStat(p, "DF") || 0;
        p.InterceptionsPer90 = calcPer90(p.Interceptions, minutos);
        
        p.DistancePer90 = extractStat(p, "Dist / 90") || extractStat(p, "DK") || 0;
        
        p.OCG = extractStat(p, "OCG");
        p.OCGPer90 = calcPer90(p.OCG, minutos);
     }

     // INFOS EXTRAS PARA O MODAL
     p.YellowCards = extractStat(p, 'Amr');
     p.RedCards = extractStat(p, 'Cartões vermelhos') || extractStat(p, 'Vrm');
     p.Appearances = p['Presenças'] || p['Pres'];
     p.Age = p['Idade'];
     p.Height = p['Altura'] || p['Altura.1'];
     p.Wage = p['Salário'] || p['Salário.1'];
     p.Value = p['Valor Estimado'] || p['Valor Estimado.1'] || p['Valor'];
     p.Expires = p['Expira'] || p['Data Final do contrato'];
     p.Nation = p['Nação'] || p['Nacionalidade'] || p['NAC'];
     p.Club = p['Clube'] || p['Equipe'];
     p.Position = p['Posição'] || p['Pos'];
     p.Foot = p['Pé Preferido'] || p['Pé preferido'] || p['Pé'] || p['PÉ'];
     p.Jogador = p['Jogador'] || p['Jogador.1'];
     
     if(p['Jogador']) {
        players.push(p);
     }
   }
   
   // Calculates global "Média de jogos", which is the average of "Jogos completos" across the dataset
   const totalJogosCompletos = players.reduce((sum, p) => sum + (parseFloat(p['Jogos completos']) || 0), 0);
   const mediaJogosGlobal = players.length > 0 ? totalJogosCompletos / players.length : 0;
   players.forEach(p => p['Média de jogos'] = mediaJogosGlobal);

   // ====== PERCENTIS DE GOLEIRO ====== 
   if (isGoalkeeper) {
     const gkPer90Metrics = [
       'GK_SavesTotal', 'GK_SavesSafe', 'GK_SavesTipped', 'GK_SavesParried',
       'GK_xGSaved', 'GK_SweepAttempts', 'GK_SweepSuccess',
       'GK_ActionsTried', 'GK_ActionsSuccess',
       'GK_PassesAttempted', 'GK_PassesCompleted'
     ];
     const gkDirectMetrics = ['GK_DifficultSavePct', 'GK_SavePct'];

     const gkMaxValues = {};
     const qualifiedGKs = players.filter(p => !p._rawMinutes || p._rawMinutes >= 270);
     const refGKs = qualifiedGKs.length >= 3 ? qualifiedGKs : players;

     gkPer90Metrics.forEach(key => {
       const per90Key = key + 'Per90';
       gkMaxValues[per90Key] = Math.max(...refGKs.map(p => p[per90Key] || 0), 0.01);
     });
     gkDirectMetrics.forEach(key => {
       gkMaxValues[key] = Math.max(...refGKs.map(p => p[key] || 0), 0.01);
     });
     // Pênaltis usam totais (frequência é aleatória, não faz sentido per90)
     gkMaxValues['GK_PenFaced'] = Math.max(...refGKs.map(p => p.GK_PenFaced || 0), 0.01);
     gkMaxValues['GK_PenSaved'] = Math.max(...refGKs.map(p => p.GK_PenSaved || 0), 0.01);

     players = players.map(p => {
       p.percentiles = {};
       gkPer90Metrics.forEach(key => {
         const per90Key = key + 'Per90';
         const max = gkMaxValues[per90Key];
         const val = p[per90Key] || 0;
         p.percentiles[key] = Math.round(max > 0 ? (val / max) * 100 : 0);
       });
       gkDirectMetrics.forEach(key => {
         const max = gkMaxValues[key];
         const val = p[key] || 0;
         p.percentiles[key] = Math.round(max > 0 ? (val / max) * 100 : 0);
       });
       p.percentiles['GK_PenFaced'] = Math.round(gkMaxValues['GK_PenFaced'] > 0 ? ((p.GK_PenFaced || 0) / gkMaxValues['GK_PenFaced']) * 100 : 0);
       p.percentiles['GK_PenSaved'] = Math.round(gkMaxValues['GK_PenSaved'] > 0 ? ((p.GK_PenSaved || 0) / gkMaxValues['GK_PenSaved']) * 100 : 0);

       // Nota IA do Goleiro (Defesas 50%, Ações 25%, Passes 25%)
       const defAvg = (
         (p.percentiles.GK_SavesTotal || 0) +
         (p.percentiles.GK_DifficultSavePct || 0) +
         (p.percentiles.GK_xGSaved || 0)
       ) / 3;
       const actAvg = (
         (p.percentiles.GK_SweepSuccess || 0) +
         (p.percentiles.GK_ActionsSuccess || 0)
       ) / 2;
       const passAvg = p.percentiles.GK_PassesCompleted || 0;
       p._notaIA = (defAvg * 0.5 + actAvg * 0.25 + passAvg * 0.25).toFixed(1);

       return p;
     });

     players = players.sort((a, b) => b._notaIA - a._notaIA);

     // Headers para a tabela: usar colunas do HTML do goleiro
     const gkHeaders = originalHeaders
       .filter(h => h !== 'Inf' && h !== 'ID Único' && h !== 'Unique ID')
       .map(h => ({ id: h, type: 'float' }));

     return { players, originalHeaders: gkHeaders, originalHtmlCols: originalHeaders, maxValues: gkMaxValues };
   }
   
   // 3. Cálculos de Percentis do Dataset Baseado no melhor do elenco importado
   const maxValues = {
      GoalsPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.GoalsPer90) : players.map(p => p.GoalsPer90)), 0.01),
      ExpectedGoalsPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.ExpectedGoalsPer90) : players.map(p => p.ExpectedGoalsPer90)), 0.01),
      ShotsPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.ShotsPer90) : players.map(p => p.ShotsPer90)), 0.01),
      AssistsPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.AssistsPer90) : players.map(p => p.AssistsPer90)), 0.01),
      ExpectedAssistsPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.ExpectedAssistsPer90) : players.map(p => p.ExpectedAssistsPer90)), 0.01),
      KeyPassesPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.KeyPassesPer90) : players.map(p => p.KeyPassesPer90)), 0.01),
      
      DribblesPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.DribblesPer90) : players.map(p => p.DribblesPer90)), 0.01),
      PossessionLostPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.PossessionLostPer90) : players.map(p => p.PossessionLostPer90)), 0.01),
      PePPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.PePPer90) : players.map(p => p.PePPer90)), 0.01),
      PassesAttemptedPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.PassesAttemptedPer90) : players.map(p => p.PassesAttemptedPer90)), 0.01),
      PassesCompletedPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.PassesCompletedPer90) : players.map(p => p.PassesCompletedPer90)), 0.01),
      
      HeadersAttemptedPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.HeadersAttemptedPer90) : players.map(p => p.HeadersAttemptedPer90)), 0.01),
      HeadersWonPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.HeadersWonPer90) : players.map(p => p.HeadersWonPer90)), 0.01),
      TacklesAttemptedPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.TacklesAttemptedPer90) : players.map(p => p.TacklesAttemptedPer90)), 0.01),
      TacklesWonPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.TacklesWonPer90) : players.map(p => p.TacklesWonPer90)), 0.01),
      PressuresAttemptedPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.PressuresAttemptedPer90) : players.map(p => p.PressuresAttemptedPer90)), 0.01),
      PressuresWonPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.PressuresWonPer90) : players.map(p => p.PressuresWonPer90)), 0.01),
      ClearancesPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.ClearancesPer90) : players.map(p => p.ClearancesPer90)), 0.01),
      InterceptionsPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.InterceptionsPer90) : players.map(p => p.InterceptionsPer90)), 0.01),
      DistancePer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.DistancePer90) : players.map(p => p.DistancePer90)), 0.01),
      OCGPer90: Math.max(...(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270).length >= 5 ? Object.values(players.filter(p => !p._rawMinutes || p._rawMinutes >= 270)).map(p => p.OCGPer90) : players.map(p => p.OCGPer90)), 0.01),
    };
      
    players = players.map(p => {
       const inverseMetrics = ['PossessionLostPer90', 'PePPer90'];
       
       p.percentiles = {};
       Object.keys(maxValues).forEach(key => {
          const max = maxValues[key];
          const val = p[key] || 0;
          let calculatedPct = max > 0 ? (val / max) * 100 : 0;
          
          if (inverseMetrics.includes(key)) {
             calculatedPct = max > 0 ? (1 - (val / max)) * 100 : 0;
             // Ensure it doesn't go below 0 if somehow val > max (which shouldn't happen, but just in case)
             calculatedPct = Math.max(0, calculatedPct);
          }
          
          p.percentiles[key.replace('Per90', '')] = Math.round(calculatedPct);
       });
       
       // Nota I.A. Global
       p._notaIA = ((p.percentiles.Goals + p.percentiles.PassesAttempted + p.percentiles.TacklesAttempted) / 3).toFixed(1);

      const metricLabels = {
         Goals: 'Gols', ExpectedGoals: 'xG', Shots: 'Finaliz.', Assists: 'Assist', ExpectedAssists: 'xA', KeyPasses: 'Passes Chave',
         PossessionWon: 'Posse Rec.', PassesAttempted: 'Passes',
         ProgressivePasses: 'Passes Prog.', Dribbles: 'Dribles'
      };

      p.topStats = Object.keys(metricLabels)
         .filter(k => p.percentiles[k] > 0)
         .map(k => ({ label: metricLabels[k], pct: p.percentiles[k] }))
         .sort((a,b) => b.pct - a.pct)
         .slice(0, 3);
         
      return p;
   });
   
   players = players.sort((a,b) => b._notaIA - a._notaIA);
   return { players, originalHeaders: getAvancadosHeaders(), originalHtmlCols: originalHeaders, maxValues };
};
