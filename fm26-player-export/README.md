# FM26 Player Export Plugin

## Visão Geral

Plugin BepInEx IL2CPP para exportar jogadores selecionados da tela "Player Database" do Football Manager 26 para CSV.

## Hotkeys

| Tecla | Função |
|-------|--------|
| **Ctrl+P** | Inicia captura e exportação |
| **F8** | Re-escaneia UIDocuments |

## Configurações (constantes)

```csharp
WAIT_FRAMES    = 4;    // frames aguardados após cada scroll
MAX_SCROLL     = 500;  // segurança contra loop infinito
MAX_ROWS       = 5000; // limite de linhas por export
ZERO_STEPS_MAX = 3;    // passos sem captura antes de parar
```

## Como Funciona

### 1. Localização da Tabela

```
PanelManager-container (root)
└── playertable
    ├── column-headers  ← Cabeçalhos das colunas
    └── [scroll-container]
        └── ScrollView
            └── View    ← Linhas dos jogadores (virtualizadas)
```

### 2. Captura por Scroll

A tabela do FM26 é **virtualizada** - só renderiza linhas visíveis. O plugin:

1. Detecta linhas com classe `virtualised-list__item--selected`
2. Captura dados das linhas visíveis
3. **Scrola automaticamente** para baixo
4. Aguarda 4 frames para a UI atualizar
5. Repete até:
   - Chegar no fim da lista
   - 500 tentativas de scroll
   - 3 passos consecutivos sem novos dados
   - 5000 linhas capturadas

### 3. Estrutura de uma Linha

```
row (VisualElement)
└── [0] cell-selector
    ├── [0] Checkbox ← Ignorado (coluna 0)
    ├── [1] Nome/Clube ← Texto mais longo capturado
    ├── [2] Coluna    ← Texto ou estrelas
    ├── [3] Coluna    ← Texto ou estrelas
    └── ...           ← Demais colunas
```

### 4. Extração de Dados

#### Texto
- Usa `TryCast<TextElement>()` e `TryCast<Label>()`
- Remove tags HTML: `<color=#fff>Texto</color>` → `Texto`
- Fallback: usa `tooltip` se texto vazio

#### Estrelas (Ratings)
- Conta estrelas pelas classes CSS:
  - `star`, `ability`, `rating` → identificador
  - `filled`, `active`, `full`, `on` → preenchida
  - `half` → meia estrela
- Retorna valor decimal: `3,5` para 3 estrelas e meia
- Ignora se todas vazias (sem rating atribuído)

#### Célula do Jogador (coluna 1)
- Coleta TODOS os textos da célula
- Seleciona o **mais longo** (nome + clube juntos)

### 5. Deduplicação

- Chave = hash de **TODAS as colunas** da linha
- Evita falsos positivos em jogadores com mesmo nome/clube

### 6. Proteções

| Proteção | Valor | Ação |
|----------|-------|------|
| Loop infinito | 500 scrolls | Para captura |
| Limite de linhas | 5000 | Para captura |
| Stalled | 3 passos sem novos dados | Para captura |
| Profundidade recursão | 20 níveis | Ignora além |

## Arquivo de Saída

**Caminho:** `Documents\Sports Interactive\Football Manager 2026\player_export_{timestamp}.csv`

**Formato:**
```csv
Nome;Clube;Valor;Rating;...
João Silva;Flamengo;€5M;4,5;...
```

## Fluxo de Execução

```
┌─────────────────┐
│  Ctrl+P         │
└────────┬────────┘
         ▼
┌─────────────────┐
│ Localiza tabela │
│ "playertable"   │
└────────┬────────┘
         ▼
┌─────────────────┐
│ Pega headers    │
│ (pula col 0)    │
└────────┬────────┘
         ▼
┌─────────────────┐
│ Inicia captura  │
│ scroll=0        │
└────────┬────────┘
         ▼
    ┌────────────┐
    │ LOOP       │◄──────────────┐
    └─────┬──────┘               │
          ▼                      │
    ┌────────────┐               │
    │ Captura    │               │
    │ selecionad │               │
    │ + estrelas │               │
    └─────┬──────┘               │
          ▼                      │
    ┌────────────┐               │
    │ Deduplica  │               │
    │ (hash full)│               │
    └─────┬──────┘               │
          ▼                      │
    ┌────────────┐               │
    │ Scroll     │               │
    │ +1 página  │               │
    └─────┬──────┘               │
          ▼                      │
    ┌────────────┐               │
    │ Wait 4     │               │
    │ frames     │───────────────┘
    └─────┬──────┘  (continua)
          │
          ▼ (fim/stall/limite)
    ┌────────────┐
    │ Salva CSV  │
    └────────────┘
```

## Classes CSS Importantes

| Classe | Significado |
|--------|-------------|
| `virtualised-list__item--selected` | Linha selecionada pelo usuário |
| `star`, `ability`, `rating` | Elemento de estrela |
| `filled`, `active`, `full`, `on` | Estrela preenchida |
| `half` | Meia estrela |

## Tratamento de Erros

- Try-catch em todas as operações de UI
- Limite de profundidade 20 em recursões
- Limite de 5000 linhas por export
- Proteção contra stalled (3 passos sem novos dados)

## Dependências

- BepInEx 6.0.0-be.738+
- UnityEngine.UIElementsModule
- UnityEngine.InputSystem

## Compatibilidade

- FM26 versão 26.1.3+
- Atualizações do jogo NÃO quebram o plugin (usa APIs públicas do Unity)
