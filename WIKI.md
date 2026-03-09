# FM26 Development Wiki

> Conhecimento acumulado após 1+ mês de engenharia reversa do Football Manager 2026

## Índice

1. [Arquitetura do FM26](#arquitetura-do-fm26)
2. [IL2CPP - O que é e como lidar](#il2cpp---o-que-é-e-como-lidar)
3. [UI Toolkit - Estrutura da Interface](#ui-toolkit---estrutura-da-interface)
4. [Sistema de Bindings](#sistema-de-bindings)
5. [Tabelas Virtualizadas](#tabelas-virtualizadas)
6. [Classes CSS Importantes](#classes-css-importantes)
7. [Offsets e dump.cs](#offsets-e-dumpcs)
8. [Erros Comuns e Soluções](#erros-comuns-e-soluções)
9. [Plugin Funcional - FM26PlayerExport](#plugin-funcional---fm26playerexport)
10. [Checklist para Novos Plugins](#checklist-para-novos-plugins)

---

## Arquitetura do FM26

### Engine
- **Unity 2022.3.x** com backend **IL2CPP**
- Não é Mono/.NET padrão - código compilado para nativo

### Arquivos Principais

```
Football Manager 2026/
├── Football Manager 2026.exe      ← Launcher
├── GameAssembly.dll               ← TODO o código do jogo compilado (IL2CPP)
└── Football Manager 2026_Data/
    └── il2cpp_data/
        └── Metadata/
            └── global-metadata.dat  ← Nomes de classes, campos, métodos
```

### Implicações
- Reflexão .NET padrão **NÃO funciona**
- `Assembly.GetTypes()` retorna vazio ou erros
- `GetType()` retorna wrappers IL2CPP, não tipos reais
- Precisa de **BepInEx IL2CPP** (não Mono)

---

## IL2CPP - O que é e como lidar

### O Problema
IL2CPP compila C# para C++ nativo. O resultado:
- Sem metadados de tipo em runtime
- Campos privados inacessíveis via reflexão normal
- Overloads de métodos ambíguos

### Ferramenta Essencial: Il2CppDumper

```bash
Il2CppDumper.exe "GameAssembly.dll" "global-metadata.dat" "output/"
```

**Gera:**
- `dump.cs` - **CRUCIAL** - todas as classes com offsets de memória
- `DummyDll/` - DLLs falsas para ILSpy/dnSpy
- `script.json` - para Ghidra/IDA

### Exemplo de dump.cs

```csharp
// TypeDefIndex: 5519
public class Bindings {
    private readonly List<Bindings.Data> m_data; // 0x70 ← OFFSET!
}

// TypeDefIndex: 5506
internal class Bindings.Data {
    public Bindings.DataKey key;      // 0x10
    public List<ulong> interest;      // 0x18 ← ITENS ATIVOS NA UI
    public IDataHandler handler;      // 0x20
    private TypedValue m_value;       // 0x30 ← O VALOR
}
```

### Lições Aprendidas

1. **Offsets mudam entre versões** - re-rodar Il2CppDumper após updates
2. **TypeDefIndex é estável** - use para buscar classes
3. **Campos privados têm offsets** - acessíveis via memória, não reflexão

---

## UI Toolkit - Estrutura da Interface

### Hierarquia Principal

```
PanelManager-container (root UIDocument)
├── PanelManager
│   └── Report
│       ├── Title
│       ├── Header
│       ├── Body
│       │   └── PlayerSearchReport
│       │       └── playertable
│       │           ├── column-headers
│       │           └── [scroll-container]
│       │               └── ScrollView
│       │                   └── View  ← Linhas virtualizadas
│       └── Footer
```

### Elementos Importantes

| Elemento | Nome | Função |
|----------|------|--------|
| Root | `PanelManager-container` | Container principal |
| Tabela | `playertable` | Tabela de jogadores |
| Headers | `column-headers` | Cabeçalhos das colunas |
| Linhas | `View` (dentro de ScrollView) | Linhas dos dados |
| Checkbox | Primeiro filho de cada linha | Seleção (ignorar) |

### Como Navegar

```csharp
// Busca recursiva por nome
VisualElement FindByName(VisualElement el, string name)
{
    if (el == null) return null;
    if (el.name == name) return el;
    for (int i = 0; i < el.childCount; i++)
    {
        var r = FindByName(el.ElementAt(i), name);
        if (r != null) return r;
    }
    return null;
}

// Uso
var playertable = FindByName(root, "playertable");
```

---

## Sistema de Bindings

### Estrutura

```
SI.Bindable.Bindings
├── m_data: List<Bindings.Data>     (0x70) ← 4000+ itens
├── m_handlers: Dictionary          (45 handlers)
└── m_nodes: Dictionary             (4636 nodes)

Bindings.Data
├── key: DataKey                    (0x10)
├── interest: List<ulong>           (0x18) ← IDs ativos na UI
├── handler: IDataHandler           (0x20)
└── m_value: TypedValue             (0x30) ← VALOR REAL
```

### TypedValue

```csharp
public abstract class TypedValue
{
    public abstract string AsString();  // Funciona!
    public abstract object Get();       // Retorna valor real
    public abstract TVal Get<TVal>();
    public bool IsNull { get; }
    public DataType DataType { get; }
}
```

### Problema com Bindings
- `GetType()` retorna wrapper IL2CPPInterop
- Não consegue acessar `m_data` via reflexão normal
- **Solução**: Usar UI Toolkit diretamente, não Bindings

---

## Tabelas Virtualizadas

### O Problema
FM26 usa **virtualização** - só renderiza elementos visíveis na tela:
- 1000 jogadores na lista
- Apenas ~20 existem no DOM
- Scroll cria/destrói elementos dinamicamente

### Solução: Captura por Scroll

```csharp
// 1. Encontrar ScrollView
var scrollView = element.TryCast<ScrollView>();

// 2. Scroll para o topo
scrollView.scrollOffset = Vector2.zero;

// 3. Loop de captura
while (!atBottom && attempts < MAX)
{
    // Capturar linhas visíveis
    foreach (var row in view.Children())
    {
        if (row.ClassListContains("selected"))
            CaptureRow(row);
    }
    
    // Scroll para baixo
    scrollView.scrollOffset = new Vector2(0, currentY + pageHeight);
    
    // Aguardar frames para UI atualizar
    yield return null; // 3-4 frames
}
```

### Configurações Importantes

```csharp
WAIT_FRAMES    = 4;    // Frames após scroll
MAX_SCROLL     = 500;  // Limite de tentativas
MAX_ROWS       = 5000; // Limite de linhas
ZERO_STEPS_MAX = 3;    // Parar se não capturar nada
```

---

## Classes CSS Importantes

### Seleção

| Classe | Significado |
|--------|-------------|
| `virtualised-list__item--selected` | Linha selecionada pelo usuário |
| `selected` | Alternativa para seleção |
| `checked` | Checkbox marcado |

### Estrelas/Ratings

| Classe | Significado |
|--------|-------------|
| `star`, `ability`, `rating` | Elemento de estrela |
| `filled`, `active`, `full`, `on` | Estrela preenchida |
| `half` | Meia estrela |

### Leitura de Estrelas

```csharp
void CountStars(VisualElement el, ref int filled, ref int half, ref int total)
{
    for (int c = 0; c < el.classList.Count; c++)
    {
        string cls = el.classList[c].ToLower();
        if (cls.Contains("star")) isStar = true;
        if (cls.Contains("filled")) isFilled = true;
        if (cls.Contains("half")) isHalf = true;
    }
    
    if (isStar && el.childCount == 0) // folha = 1 estrela
    {
        total++;
        if (isHalf) half++;
        else if (isFilled) filled++;
    }
}
```

---

## Offsets e dump.cs

### Offsets Descobertos

```csharp
// Bindings
m_data: 0x70

// Bindings.Data
key:      0x10
interest: 0x18
handler:  0x20
m_value:  0x30

// List<T> padrão IL2CPP
_items: 0x10
_size:  0x18
```

### Classes Importantes no dump.cs

| Classe | TypeDefIndex | Uso |
|--------|--------------|-----|
| `Bindings` | 5519 | Sistema de binding |
| `Bindings.Data` | 5506 | Item de dados |
| `TypedValue` | 12778 | Valor tipado |
| `PersonReference` | 707 | Referência de pessoa |
| `PlayerReference` | - | Interface para player |
| `StreamedTable` | - | Tabela com dados |

---

## Erros Comuns e Soluções

### 1. Ambiguidade em Q() e Query<T>()

**Erro:**
```
Ambiguous match found for Q(VisualElement, string, Il2CppStringArray)
```

**Solução:**
```csharp
// Errado
parent.Q("name", null);

// Certo
parent.Q("name", (string)null);

// Ou criar helper
VisualElement QSafe(VisualElement parent, string name)
    => parent.Q(name, (string)null);
```

### 2. Crash em Recursão de UI

**Erro:**
```
Stack overflow ou crash ao iterar VisualElements
```

**Solução:**
```csharp
// SEMPRE limitar profundidade e filhos
void Traverse(VisualElement el, int depth = 0)
{
    if (depth > 10) return;           // Limite profundidade
    if (el.childCount > 100) return;  // Limite filhos
    
    try { /* operação */ } catch { }
    
    for (int i = 0; i < Math.Min(el.childCount, 20); i++)
        Traverse(el.ElementAt(i), depth + 1);
}
```

### 3. GetType() Retorna Wrapper

**Problema:**
```csharp
var type = element.GetType();
// type.Name = "Object" ou "Il2CppObject"
```

**Solução:**
```csharp
// Usar TryCast<T> em vez de GetType()
var label = element.TryCast<Label>();
if (label != null) { /* é Label */ }

var textElement = element.TryCast<TextElement>();
if (textElement != null) { /* é TextElement */ }
```

### 4. Query<Label>() Retorna Vazio

**Problema:**
Labels não são encontrados via Query

**Solução:**
```csharp
// Usar TryCast e recursão manual
string GetText(VisualElement el)
{
    var te = el.TryCast<TextElement>();
    if (te != null && !string.IsNullOrWhiteSpace(te.text))
        return te.text;
    
    var lb = el.TryCast<Label>();
    if (lb != null && !string.IsNullOrWhiteSpace(lb.text))
        return lb.text;
    
    return null;
}
```

### 5. Toggles Não Encontrados

**Problema:**
```csharp
Query<Toggle>().Build().ToList() // Retorna 0
```

**Motivo:**
FM26 usa checkboxes customizados, não Unity Toggle

**Solução:**
```csharp
// Detectar seleção via classe CSS
bool isSelected = row.ClassListContains("virtualised-list__item--selected");
```

---

## Plugin Funcional - FM26PlayerExport

### Características

- ✅ Exporta jogadores selecionados para CSV
- ✅ Captura estrelas/ratings via classes CSS
- ✅ Scroll automático para lista virtualizada
- ✅ Deduplicação robusta
- ✅ Proteção contra loops infinitos
- ✅ Compatível com atualizações do jogo

### Código Final

Disponível em:
```
fm26-editor-workspace/fm26-player-export/FM26PlayerExport.cs
```

### Hotkeys

| Tecla | Função |
|-------|--------|
| Ctrl+P | Exportar jogadores |
| F8 | Re-escanear UI |

### Output

```
Documents\Sports Interactive\Football Manager 2026\player_export_{timestamp}.csv
```

---

## Checklist para Novos Plugins

### Antes de Começar

- [ ] Confirmar que FM26 é IL2CPP (existe GameAssembly.dll)
- [ ] Instalar BepInEx **IL2CPP** (não Mono)
- [ ] Rodar Il2CppDumper e gerar dump.cs
- [ ] Buscar classes relevantes no dump.cs

### Durante Desenvolvimento

- [ ] Usar `TryCast<T>()` em vez de `GetType()`
- [ ] Limitar recursão (depth < 10, children < 20)
- [ ] Try-catch em CADA operação de UI
- [ ] Usar helper methods para evitar ambiguidade IL2CPP
- [ ] Aguardar frames após operações de scroll

### Teste

- [ ] Testar hotkeys
- [ ] Verificar logs em BepInEx\LogOutput.log
- [ ] Validar output CSV
- [ ] Testar após atualização do jogo

### Commit

- [ ] Documentar mudanças
- [ ] Atualizar versão
- [ ] Push para repositório

---

## Referências

- **Il2CppDumper**: https://github.com/Perfare/Il2CppDumper
- **BepInEx IL2CPP**: https://github.com/BepInEx/BepInEx
- **UnityExplorer**: https://github.com/sinai-dev/UnityExplorer
- **Repo do Projeto**: https://github.com/contatovinteset-wq/fm26-editor-workspace

---

## Histórico de Versões

| Versão | Data | Status |
|--------|------|--------|
| FM26CtrlPExport v2.61.0 | 2026-03-08 | Obsoleto (bindings approach) |
| FM26SceneDump v1.0.0 | 2026-03-07 | Funcional |
| FM26PlayerExport v1.0.0 | 2026-03-09 | **Funcionando** ✅ |

---

*Wiki criada após 1+ mês de engenharia reversa do FM26*
*Última atualização: 2026-03-09*
