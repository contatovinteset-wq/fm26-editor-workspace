# FM26 Player Export Plugin

## Visão Geral

Plugin BepInEx IL2CPP para exportar jogadores selecionados da tela "Player Database" do Football Manager 26 para CSV.

## Hotkeys

| Tecla | Função |
|-------|--------|
| **Ctrl+P** | Inicia captura e exportação |
| **F8** | Re-escaneia UIDocuments |

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
4. Aguarda 3 frames para a UI atualizar
5. Repete até chegar no fim ou 300 tentativas

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

- Usa `TryCast<TextElement>()` para obter texto
- Remove tags HTML: `<color=#fff>Texto</color>` → `Texto`
- Escapa caracteres especiais para CSV

### 5. Deduplicação

Usa **chave única** baseada nas primeiras 3 colunas não vazias para evitar duplicados durante o scroll.

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
- Limite de 300 tentativas de scroll
- Deduplicação por chave única
- Strip de tags HTML para texto limpo

## Dependências

- BepInEx 6.0.0-be.738+
- UnityEngine.UIElementsModule
- UnityEngine.InputSystem

## Compatibilidade

- FM26 versão 26.1.3+
- Atualizações do jogo NÃO quebram o plugin (usa APIs públicas do Unity)
