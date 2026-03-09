# FM26 Player Export Plugin - Documentação

## Visão Geral

Plugin BepInEx para Football Manager 26 que exporta dados de jogadores da tela "Player Database" para CSV.

---

## Como Funciona

### 1. Descoberta da UI

O plugin navega pela hierarquia de elementos visuais do Unity UI Toolkit:

```
PanelManager-container
└── playertable
    ├── column-headers     ← Cabeçalhos das colunas
    └── [scroll-container]
        └── ScrollView
            └── View       ← Linhas dos jogadores (virtualizadas)
```

### 2. Lista Virtualizada

O FM26 usa uma **virtualised-list** - somente linhas visíveis são renderizadas. O plugin:

1. Detecta o `ScrollView`
2. Scrolla página por página
3. Captura linhas marcadas com classe CSS `virtualised-list__item--selected`
4. Deduplica usando chave única (primeiras 3 colunas)

### 3. Extração de Texto

```csharp
// Cada linha tem estrutura:
row → [cellSelector] → [célula1, célula2, ...]
                       ↑ pula coluna 0 (checkbox)

// Célula → TextElement → text
```

- Remove tags HTML: `<color=#fff>Nome</color>` → `Nome`
- Escapa vírgulas e ponto-e-vírgula para CSV

---

## Estrutura do Código

```
FM26PlayerExport/
├── Plugin.cs           ← Entry point BepInEx
├── ExportBehaviour.cs  ← Lógica principal
│   ├── FindByName()    ← Busca recursiva por nome
│   ├── GetText()       ← Extrai texto de TextElement
│   ├── StripHtml()     ← Remove tags HTML
│   ├── ReadRow()       ← Lê valores de uma linha
│   ├── StartCapture()  ← Inicia captura
│   ├── CaptureStep()   ← Captura + scroll
│   └── FinishCapture() ← Salva CSV
└── FM26PlayerExport.csproj
```

---

## Hotkeys

| Tecla | Função |
|-------|--------|
| **Ctrl+P** | Inicia exportação (percorre toda a lista) |
| **F8** | Re-escaneia UIDocuments |

---

## Saída

**Caminho:** `Documents\Sports Interactive\Football Manager 2026\player_export_YYYYMMDD_HHMMSS.csv`

**Formato:**
```csv
Nome;Idade;Posição;Clube;...
João Silva;25;MC;Flamengo;...
```

---

## Classes CSS Importantes

| Classe | Significado |
|--------|-------------|
| `virtualised-list__item--selected` | Linha selecionada pelo usuário |
| `virtualised-list` | Container de lista virtualizada |

---

## Dependências

```xml
<Reference Include="BepInEx.Core" />
<Reference Include="BepInEx.Unity.IL2CPP" />
<Reference Include="UnityEngine.CoreModule" />
<Reference Include="UnityEngine.UIElementsModule" />
<Reference Include="UnityEngine.InputSystemModule" />
```

---

## Limitações

1. **Apenas linhas selecionadas** são exportadas
2. **Máximo de 300 scrolls** por captura (segurança)
3. **Aguarda 3 frames** entre cada scroll (sincronização)

---

## Debug

Use o plugin `FM26TableDump` (F7) para analizar a estrutura da UI:

```
FM26TableDump/
├── F7 = Dump completo da árvore
├── Profundidade: 6 níveis (geral) / 10 níveis (detalhado)
└── Saída: Documents\Sports Interactive\Football Manager 2026\table_dump_*.txt
```
