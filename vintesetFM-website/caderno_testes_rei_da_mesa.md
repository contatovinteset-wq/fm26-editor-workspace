# 👑 Caderno de Testes e Operação - Rei da Mesa

Bem-vindo ao Guia Oficial de Operação do Rei da Mesa!
Este caderno de testes define o **fluxo operacional camada de administrador (Dono)** e estabelece um *Teste de Mesa* verificável passo a passo com base nos dados que você forneceu. Siga estes passos para homologar o painel antes de liberar na live.

---

## 🚦 Fluxo da Rodada (Caminho Feliz)

A rotina de operação de uma rodada no do "Rei da Mesa" acontece no `[Painel do Dono]` e deve seguir esta exata ordem de botões:

1. **Abrir Mercado**: Isso sinaliza para a aplicação que uma NOVA RODADA começou. Os Viewers podem montar/alterar as seleções deles. A sua rodada anterior é fechada definitivamente e uma nova é crida (Mantendo as escolhas para quem não for alterar).
2. **Importar Plantel da Rodada**: Você irá extrair e subir o arquivo `moneyball_export.html` (dados de todo o elenco). Atualizando todas as médias, idades e listagem dos jogadores para seus viewers selecionarem. 
   > *Dica: Faça isso LOGO DEPOIS de abrir o mercado.*
3. **... (O Jogo de FM acontece na live) ...**
4. **Fechar Mercado**: Antes do juiz apitar o início do jogo do seu time, você clica aqui para trancar. Os viewers podem continuar vendo o time deles atual, mas o "Salvar" das edições é travado.
5. **Importar Partida (Estatísticas do Jogo)**: Acabou o jogo! Extraia e mande o html `match_stats.html`.
6. **Magia Acontece**: O sistema tabula tudo, preenche o hall do Top 3, encontra O Bagre, rankeia os Manager viewers e atualiza o saldo da carteira da rapaziada!
7. **Repete tudo pro próximo jogo!**

---

## 📝 Teste de Mesa Oficial (Validando a Pontuação)

Com base nos HTMLs `moneyball_export_20260402_155333` e `match_stats_Hartle vs MFC_20260401_232250`, nós faremos agora a auditoria da nossa engine! O sistema precisa acusar EXATAMENTE os valores abaixo quando você importar as duas planilhas nestes testes hoje:

### 🏆 TOP 3 DA PARTIDA
1. **Slavi Spasov** (ATA) — **36.50 pts** ⚽⚽⚽ *(Hat-trick Monster!)*
2. **Zac Bell** (DEF) — **12.74 pts**
3. **Ben Worman** (MEI) — **11.52 pts**

### 🐟 O BAGRE DA RODADA
- **Bodhan Keogh** — **1.00 pts**

---

### 📊 Dissecando a Máquina (Exemplo)

Como nós extraímos a pontuação massiva do nosso Monstro Sagrado **Slavi Spasov (36.50 pts)**?

| Ação na Partida | Unidades | Fórmula / Peso | Total |
| :--- | :--- | :--- | :--- |
| **Tempo em Campo** | 90min | ( > 60m = +1.0 ) | +1.0 |
| **Gols Feitos** | 3 Gols | 3 x (8.0) | +24.0 |
| **xG (Gols Esperados)**| 2.21 xG | 2.21 x (2.0) | +4.42 |
| **xA (Ast. Esperada)** | 0.04 xA | 0.04 x (2.0) | +0.08 |
| **Chances Criadas** | 3 ChC | 3 x (2.0) | +6.0 |
| **Passes Decisivos** | 1 PasD | 1 x (1.0) | +1.0 |
| **Restante do Jogo** | 0 def/fintas | Sem desconto de falha | 0.0 |
| **PONTUAÇÃO FINAL** | | | **36.50 pts** |

Como a engine penalizou nosso querido bagre **Bodhan Keogh (1.00 pts)**?
- **Tempo**: 60min (+1.0 pt)
- **Interceptações**: 1 (+0.5 pt)
- **Faltas Cometidas**: 1 **(-0.5 pt)**
- **Restante do Jogo**: Zerado em todo o resto!
- **PONTUAÇÃO FINAL**: **1.00 pt** *(E o título oficial de Bagre da Rodada no site)*.

> [!WARNING]  
> Lembra da regra de Ouro do Esquadrão Viewer? Se o espectador escolheu o **Bodhan Keogh** no slot de (O Bagre) do time dele, a pontuação geral final do View ganha **+5.0 pts** como prêmio BINGO no final (O Total é Pontuação do ATA + MEI + DEF e + ou - 5 pts caso acertou ou errou o Bagre).

---

## ✅ Checklist do Testador (Passo a Passo de Validação)

Abra o site você mesmo e valide a ferramenta acompanhando essas etapas pontualmente:

1. [ ] **Entre como Administrador** e abra a nova tela "Painel do Dono" 
2. [ ] Clique em "Abrir Mercado". Teste ir na aba "Escale Agora" e ver que o botão "Confirmar Equipe" está habilitado para edição (desde que você logue com as configs ativas e tudo preenchido).
3. [ ] **Upload Plantel**: Vá no Painel e suba o seu arquivo `moneyball_export...` mais atual. 
4. [ ] Vá no site clicando em "Meu Esquadrão" no Painel inicial e verifique se as **Labels dos jogadores** exibem com os novos campos atualizados (Minutos, CM, etc) com base na sua ingestação do Plantel.
5. [ ] **Feche o Mercado** e verifique: Seu Dashboard vai deixar de dizer "Escale Agora" e vai mostrar os 4 rostinhos da sua seleção no canto com a tag *Ver Meu Time / Pontuação da Rodada*. Se clicar dentro do Campinho, o seu botão Confirmar Equipe no fim da tela estará desabilitado para travar mudanças.
6. [ ] **Roleplay Match Results**: Volte ao painel, vá no botão "Importar Partida" e suba o HTML `match_stats_Hartle vs MFC...`. O loading deve processar e carregar as engrenagens avisando sucesso.
7. [ ] Volte para a capa do `Rei da Mesa` (`/reidamesa`) – A magia da Tv deve acontecer: O topo do site mostrará os rostos (Slavi Spasov, Zac, e Ben no TOP 3, com o Bodhan afundado de vermelho no Bagre). E o bloco novo que criamos para o "PONTOS DA RODADA" na direita onde antes estava escrito Escale/Fechado vai pular a pontuação positiva da sua equipe combinada que você deixou logado.

*Se todos esse passos refletirem o esperado... o sistema de Game Fantasy está blindado e pronto para o público!*
