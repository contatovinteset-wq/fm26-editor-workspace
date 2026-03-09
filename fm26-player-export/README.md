# FM26 Player Export Plugin

## Visão Geral

Plugin BepInEx IL2CPP para exportar jogadores selecionados da tela "Player Database" do Football Manager 26 para CSV.

## Hotkeys

| Tecla | Função |
|-------|--------|
| **Ctrl+P** | Inicia captura e exportação |
| **F8** | Re-escaneia UIDocuments |

## Configurações de Performance

| Constante | Valor | Descrição |
|-----------|-------|-----------|
| `WAIT_FRAMES` | 4 | Frames aguardados após cada scroll |
| `MAX_SCROLL` | 500 | Máximo de tentativas de scroll |
| `MAX_ROWS` | 5000 | Limite de linhas por export |
| `ZERO_STEPS_MAX` | 3 | Passos sem captura antes de parar |

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
5. Repete até: chegar no fim, 500 tentativas, ou 3 passos sem novos dados

### 3. Estrutura de uma Linha

```
row (VisualElement)
└── [0] cell-selector
    ├── [0] Checkbox ← Ignorado (coluna 0)
    ├── [1] Nome     ← Capturado
    ├── [2] Clube    ← Capturado
    ├── [3] Valor    ← Capturado
    └── ...          ← Demais colunas
```

### 4. Extração de Texto

Múltiplas estratégias para capturar dados:

| Método | O que faz |
|--------|-----------|
| `GetText()` | Tenta TextElement → Label → tooltip |
| `CollectFirstText()` | Primeiro texto encontrado (recursivo) |
| `CollectAllTexts()` | Todos os textos de um elemento |
| `TryReadStars()` | Conta estrelas preenchidas/metade via CSS |

#### Célula do Jogador (coluna 1)
- Coleta **todos os textos** da célula
- Seleciona o **mais longo** (resolve nomes com imagens/flags)

#### Ratings em Estrelas
- Analisa classes CSS: `star`, `ability`, `rating`, `filled`, `half`
- Converte para valor numérico: 3.5 estrelas → "3,5"

### 5. Deduplicação

- Usa **hash do conteúdo completo da linha** (todas as colunas)
- Evita duplicados durante o scroll
- Mais robusto que usar só as primeiras 3 colunas

## Arquivo de Saída

**Caminho:** `Documents\Sports Interactive\Football Manager 2026\player_export_{timestamp}.csv`

**Formato:**
```csv
Nome;Clube;Valor;...
João Silva;Flamengo;€5M;...
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
    │ LOOP       │◄──────────┐
    └─────┬──────┘           │
          ▼                  │
    ┌────────────┐           │
    │ Captura    │           │
    │ selecionad │           │
    └─────┬──────┘           │
          ▼                  │
    ┌────────────┐           │
    │ Scroll     │           │
    │ +1 página  │           │
    └─────┬──────┘           │
          ▼                  │
    ┌────────────┐           │
    │ Wait 3     │           │
    │ frames     │───────────┘
    └─────┬──────┘  (não chegou no fim)
          │
          ▼ (fim ou 300 tentativas)
    ┌────────────┐
    │ Salva CSV  │
    └────────────┘
```

## Classes CSS Importantes

| Classe | Significado |
|--------|-------------|
| `virtualised-list__item--selected` | Linha selecionada pelo usuário |

## Tratamento de Erros

- Try-catch em todas as operações de UI
- Limite de 500 tentativas de scroll
- Limite de 5000 linhas por export
- **Proteção contra stall**: para se 3 passos consecutivos sem novos dados
- Strip de tags HTML para texto limpo
- Diagnóstico detalhado no primeiro passo (se `_diagLogged = false`)

## Dependências

- BepInEx 6.0.0-be.738+
- UnityEngine.UIElementsModule
- UnityEngine.InputSystem

## Compatibilidade

- FM26 versão 26.1.3+
- Atualizações do jogo NÃO quebram o plugin (usa APIs públicas do Unity)
