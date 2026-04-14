# Lógica do Moneyball Analítico (VintesetFM)

Este documento detalha o raciocínio matemático e de interface por trás do cálculo e renderização das barras de percentil no Dashboard do Moneyball (`MoneyballAnalyzer.jsx` e `MoneyballLogic.js`).

## 1. O Cálculo dos Percentis no Sistema (MoneyballLogic.js)

O sistema de geração das notas de cada jogador e das estatísticas não utiliza dados absolutos, e sim uma **escala local baseada em estatística relativa**. Tudo no Moneyball Analítico do FM é focado em comparar o jogador ao melhor jogador da pool (base de dados importada). A matemática ocorre em 3 etapas precisas:

### Etapa 1: Normalização Padrão "Por 90 Minutos" (Per 90)
Um erro grave de analistas é comparar dados brutos absolutos entre jogadores com tempo de jogo diferentes (ex: comparar o número de interceptações de um zagueiro com 5 partidas jogadas contra as interceptações de outro com 30 partidas). 

Para nivelar isso na base, o nosso script de processamento em `MoneyballLogic.js` extrai o valor puro importado da tabela HTML gerada pelo plugin, e o converte numa média **"Per 90"**.
* Exemplo: Se um jogador realizou 10 Desarmes em 450 minutos contabilizados: `(10 / 450) * 90 = 2.0`. A intensidade real dele é parametrizada em **2.0 Desarmes por partida completa simulada de 90m**, o que nivela a régua de comparação entre todo o elenco.

### Etapa 2: Descobrindo o Teto (Benchmark Local)
Após processar todos os jogadores do HTML da pool, o script escaneia as colunas estatísticas rodando funções matemáticas de máximo (`Math.max()`).
Isso detecta automaticamente **o valor do líder** real em cada critério dentro dos limites listados.
* Exemplo: Se o maior valor de desarmes bem sucedidos em toda a tabela base que você exportou do FM e jogou no HTML for de 5.0 tentativas por 90minutos, o sistema eleva esse "5.0" assumindo ele matematicamente como nosso **100% definitivo** de teto/benchmark dessa respectiva variável do dataset.

### Etapa 3: O Cálculo Relativo e o Retorno Final
A contagem em percentual que surge graficamente sob as barras do jogador não é fixa, nascendo de uma operação relativa da métrica específica daquele atleta baseada em divisão pelo benchmark limite (estatística do líder do quesito).
* Se o atacante alvo (Joãozinho) teve índice de `4.1` bolas roubadas p/90, e a nossa IA registrou o teto absoluto base/benchmark como `5.0`: `(4.1 / 5.0) * 100 = 82%` de score relativo calculado.

> **Importante:** Os percentis visuais **NÃO indicam "taxa de acertos in-game"** (isso é uma coluna secundária externa). O percentil de `82%` visual indica: o quão bem e perto o jogador perfomou ou quão "parecido estatisticamente" ele está comparado contra O MELHOR JOGADOR do seu dataset selecionado nesta métrica respectiva.

---

## 2. A Construção Visual da Interface e Gráfico (PercentileBar)

O componente visual reage em tempo real a essa lógica matemática utilizando faixas de cor guiadas por um threshold (limiar):

1. **A Geometria dos 10 Blocos (Gomos da UI):** 
   A dimensão base visual é tratada mapeando iterativamente a manipulação de um Array de tamanho fixo 10 (`length: 10`). A utilização do design separado em pequenos blocos (em oposição a uma barra sólida) cria nativamente essa imersão e estética tática de um "Painel Técnico e Esportivo" de pranchetas de Staff do Football Manager.

2. **Engenharia do Preenchimento Animado/Desenho:** 
   Uma vez obtido o percentil superior (`82%`), a barra divide o valor estrito por 100 e realiza mutiplicação pela limitação dos blocos predefinidos: `0.82 * 10 = 8.2`. Executando a função JavaScript de arredondamento (`Math.round()` no iterador react), o ambiente normalizará a renderização para preencher logicamente em exatos `8` blocos o painel da UI na linha dessa estatística pro Joãozinho.

3. **Gatilhos Condicionais e Alertas Visuais Baseados no Risco:**
   A colorimetria do HUD reage estritamente na escala dos threshold gerados pelo valor puro escalonado:
   * **Score < 25:** Preenchimento instanciado como **Vermelho Escuro** (`#d84841`). Acionamento de alerta visual, indicando fraqueza abissal ao padrão.
   * **Score entre 25 a 49:** Cores quentes/alerta em instanciamento **Laranja**, sinal de preocupação técnica do treinador ou área de desenvolvimento precária, medíocre.
   * **Score entre 50 a 74:** Escala quente **Amarela**, aponta o mediano, sólida atuação em parâmetro de normalidade sem maiores surpresas. Jogador seguro nesse papel.
   * **Score >= 75:** Threshold superado/Destivado, ativando estado **Esmeralda / Verde FM** (`#15af59`). Representa rápida identificação tática pela diretoria indicando ser uma competência de status "Elite/Masterclass" para tal variável em seu respectivo plantel (Jogador Forte).

Todo processo de re-draw é autônomo baseado no import do DOM injetado da exportação do usuário, e auto balanceia se você trocar HTMLs num evento ou novo file.
