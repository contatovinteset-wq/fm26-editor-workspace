# DLLs e Assemblies - FM26

## Visão Geral

O Football Manager 26 usa **Unity IL2CPP** para compilar o código C# em código nativo. Isso significa que o código-fonte não está acessível diretamente, mas podemos analisar os metadados.

---

## Arquitetura IL2CPP

```
┌─────────────────────────────────────────────────────────────┐
│                    CÓDIGO-FONTE ORIGINAL                     │
│                    (C# - não disponível)                     │
├─────────────────────────────────────────────────────────────┤
│                        COMPILAÇÃO                             │
│  C# → IL (Intermediate Language) → C++ → Código Nativo      │
├─────────────────────────────────────────────────────────────┤
│                      ARQUIVOS FINAIS                          │
│  ├── GameAssembly.dll (código nativo)                       │
│  ├── global-metadata.dat (metadados)                        │
│  └── il2cpp.usym (símbolos de debug)                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Arquivos Principais

### global-metadata.dat

**Localização:** `fm_Data/il2cpp_data/Metadata/global-metadata.dat`
**Tamanho:** 15 MB

**Conteúdo:**
- Definições de classes C#
- Nomes de métodos e campos
- Assinaturas de funções
- Strings literais
- Informações de tipos

**Como extrair:**
```bash
# Usar Il2CppDumper
Il2CppDumper.exe GameAssembly.dll global-metadata.dat output/
```

**Saída:**
- `dump.cs` - Definições de classes em C#
- `script.json` - Mapeamentos de endereços
- `stringliteral.json` - Strings literais

---

### GameAssembly.dll

**Conteúdo:** Código nativo compilado do jogo

**Análise:**
- Não é possível descompilar para C# original
- Pode ser analisado com IDA/Ghidra (assembly)
- Funções podem ser identificadas via metadados

---

### il2cpp.usym

**Tamanho:** 36 MB
**Conteúdo:** Símbolos de depuração

---

## Assemblies Identificados

### Principais

| Assembly | Função |
|----------|--------|
| **FMGame** | Núcleo do jogo, lógica principal |
| **FM.UI** | Interface do usuário (UI Toolkit) |
| **FM.Match** | Motor de partida (match engine) |
| **FM.Graphics** | Gráficos 3D, renderização |
| **FM.Input** | Sistema de entrada |
| **FM.Performance** | Otimização e performance |

### Sports Interactive

| Assembly | Função |
|----------|--------|
| **SI.Core** | Núcleo da Sports Interactive |
| **SI.Bindable** | Sistema de data binding |
| **SI.UI** | Componentes UI proprietários |
| **SI.Match** | Lógica de partida |

### Unity

| Assembly | Função |
|----------|--------|
| **UnityEngine.CoreModule** | Núcleo do Unity |
| **UnityEngine.UI** | Sistema de UI |
| **UnityEngine.UIModule** | Módulos UI |
| **Unity.TextMeshPro** | Renderização de texto |

---

## Sistema de UI (FM.UI.dll)

### UI Toolkit

O FM26 usa **UI Toolkit** (Unity UIElements) para a interface.

**Características:**
- Baseado em USS (similar a CSS)
- UXML para layout (similar a HTML)
- Data binding com SI.Bindable

**Arquivos relacionados:**
- Painéis de UI estão compilados nos DLLs
- Estilos podem estar em Asset Bundles
- Não é possível editar diretamente

### Data Binding

O sistema SI.Bindable conecta dados a elementos UI.

```csharp
// Exemplo hipotético da estrutura
public class PlayerData : Bindable
{
    public string Name { get; set; }
    public int Overall { get; set; }
}
```

---

## Análise com Il2CppDumper

### Instalação

```bash
# Baixar de https://github.com/Perfare/Il2CppDumper/releases
# Windows: Il2CppDumper-win-x64.zip
# Linux: Il2CppDumper-linux-x64.tar.gz
```

### Uso

```bash
# Executar
./Il2CppDumper GameAssembly.dll global-metadata.dat output/

# Saída:
# - dump.cs (definições de classes)
# - script.json (endereços)
# - stringliteral.json (strings)
```

### Exemplo de Saída (dump.cs)

```csharp
// Interesse para modding de UI
public class UIManager : MonoBehaviour
{
    public void ShowPanel(string panelName);
    public void HidePanel(string panelName);
    // ...
}

public class DataExporter
{
    public void ExportPlayerData();
    public void ExportMatchHistory();
    // Função que precisamos restaurar?
}
```

---

## Procurando Funcionalidades Específicas

### Exportação de Dados (Ctrl+P)

**Objetivo:** Encontrar e restaurar função de exportar dados

**Passos:**
1. Extrair dump.cs com Il2CppDumper
2. Buscar por palavras-chave:
   - `Export`
   - `SaveData`
   - `PrintScreen`
   - `Hotkey`
   - `Keyboard`

**Classes prováveis:**
```csharp
// Hipotético
public class KeyboardManager
{
    public bool IsKeyPressed(KeyCode key);
    public void HandleHotkey(KeyCode key);
}

public class DataExporter
{
    public void ExportToCSV();
    public void ExportToJSON();
}
```

---

## Sistema de Skins

### Onde estão as definições de UI?

**Possíveis localizações:**
1. Compiladas em `FM.UI.dll`
2. Asset Bundles específicos
3. Arquivos .uss/.uxml compilados

**Classes relevantes:**
```csharp
public class SkinManager
{
    public void LoadSkin(string path);
    public void ApplyTheme(Theme theme);
}

public class PanelDefinition
{
    public string Name;
    public UXMLDocument Layout;
    public USSDocument Styles;
}
```

---

## Modificação de DLLs

### ⚠️ Limitações

1. **Não é possível editar diretamente** - código é nativo
2. **Patching** - modificar bytes específicos
3. **Hooking** - interceptar chamadas de função

### Ferramentas

| Ferramenta | Uso |
|------------|-----|
| **BepInEx** | Framework de mods |
| **MelonLoader** | Loader de mods |
| **Harmony** | Patching em runtime |
| **dnSpy** | Debug (apenas para análise) |

### Exemplo de Patch com Harmony

```csharp
// Patch hipotético para restaurar export
[HarmonyPatch(typeof(DataExporter))]
[HarmonyPatch("ExportPlayerData")]
class ExportPatch
{
    static bool Prefix()
    {
        // Código customizado antes da função original
        return true; // true = executar original, false = pular
    }
}
```

---

## Próximos Passos

### Curto Prazo
1. [ ] Executar Il2CppDumper no FM26
2. [ ] Analisar dump.cs por classes relevantes
3. [ ] Identificar sistema de hotkeys
4. [ ] Mapear estrutura de UI

### Médio Prazo
5. [ ] Criar BepInEx plugin para logging
6. [ ] Investigar sistema de skins
7. [ ] Documentar todas as classes de UI

### Longo Prazo
8. [ ] Criar mod para restaurar exportação de dados
9. [ ] Criar editor de skins visual
10. [ ] Documentar API interna

---

## Referências

- [Il2CppDumper](https://github.com/Perfare/Il2CppDumper)
- [BepInEx](https://github.com/BepInEx/BepInEx)
- [Harmony](https://github.com/pardeike/Harmony)
- [Unity IL2CPP](https://docs.unity3d.com/Manual/ScriptingBackendsIl2cpp.html)

---

*Documentação gerada em 2026-02-19*
