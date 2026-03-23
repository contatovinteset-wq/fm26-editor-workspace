# Histórico das Sessões (Session-History)

Este arquivo é a memória viva do que já foi completado no projeto `fm26-editor-workspace`, baseado nos avanços e relatórios exportados anteriormente pelo Perplexity e por este agente.

## Funcionalidades Implementadas
- **Plugin 1: FM26PlayerExport**
  - Integração Completa para Exportação de CSV, contendo nomes, idades, CP (Current Potential), e traduções de estrelas.
  - O sistema lida ativamente com duplicatas através de geração de um Hash de Linha Inteira (`RowKey`) durante o scroll das tabelas.
  - Workflow de distribuição (CI/CD) gerido via Inno Setup 6, enviando um `.exe` direto pro usuário que embute as dependências com o BepInEx 6 automaticamente no regedit Steam do jogo.
- **Plugin 2: FM26TacticsDump**
  - Mapeador Dinâmico das abas de táticas UI (Táticas com e sem posse de bola).
  - Serialização dos sub-tópicos de instrução (ex: "Crossing Style", "Possession") em formato `.json` persistido.
  - Correções estruturais ativas para navegar corretamente pelo Virtual DOM da engine via `si-tile`.

## Bugs Resolvidos e Soluções 
- **Bug de Compilação IL2CPP Reference:**
  - *Problema:* Discrepâncias em `System.Reflection.Emit.ILGeneration` apontando versão 4.x contra 6.x.
  - *Solução:* Criado e mantido no `.csproj` um bypass de referências para a máquina local de forma pacífica através do `<Target Name="PostBuild">`. As referências em cache apontam para C:\Program Files\dotnet\packs\.
- **Loop de Interface e Duplicidade no CSV:**
  - *Problema:* ScrollViews longos regravavam o mesmo nome do jogador Múltiplas vezes.
  - *Solução:* Aplicação de um Memory Hash (`HashSet<string>`) comparando toda a linha raspada, garantindo deduplicação O(1) in-memory.
- **Instruções Inversas Out of Possession:**
  - *Problema:* A lógica falhava ao diferenciar comportamento da prancheta quando um time estava defendendo devido as sub-janelas carregadas apenas no DOM ativo.
  - *Solução:* Re-estruturação com classes CSS virtuais, isolando `StartCoroutine` atadas unicamente aos menus instanciados no momento. Versão elevada para 2.1.6 por estabilidade.

## Funcionalidades Pendentes (Priorizadas)
1. **[Crítica]** Mudar do Scraping Visual para o Hook de Runtime (`FM26TacticsTypeMap` via `0Harmony`). Isso provará definitivamente que ordens dadas geram peso estrito em `PlayerDecision.EvaluateCrossing()` e não são Placebos de UI sobrepostos por Atributos do jogador.
2. Injetar Captura de CA e PA exatos no `FM26PlayerExport`, atributos escondidos pelo renderizador da interface mas latentes na memória da base de dados (namespace `SI.*`).
3. Criar painéis In-Game (Custom UI Overlay) para habilitar cliques de Botão Exportar e Despejar via mouse ao invés do atalho restrito no `Keyboard.Update()`.
