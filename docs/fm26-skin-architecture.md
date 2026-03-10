# FM26 Skin Architecture - Descoberta Técnica

**Data**: 2026-03-10  
**Fonte**: Análise Darkside v4.3

---

## 🔍 Descoberta Principal

**FM26 NÃO usa XML/UXML para skins!**

Ao contrário de versões antigas (FM20-FM23), o FM26 usa **Unity UI Toolkit** com:
- **MonoBehaviours** (C# compilado) para estrutura de UI
- **StyleSheets** compilados (não editáveis via texto)
- **Asset Bundles** binários

---

## 📦 Anatomia dos Bundles

### Tipos de Assets por Bundle

| Bundle | Tamanho | Assets Principais | Modificável? |
|--------|---------|-------------------|--------------|
| `ui-styles_assets_default.bundle` | 746 KB | **33 MonoBehaviours** (estilos) | ❌ Difícil |
| `ui-backgrounds_assets_common.bundle` | 129 MB | **70 Texture2D** | ✅ Sim |
| `ui-fonts_assets_production.bundle` | 19 MB | 11 Fonts, 26 Texturas | ✅ Sim |
| `ui-tableviews_assets_all.bundle` | 191 KB | **800 MonoBehaviours** (tabelas) | ❌ Difícil |
| `ui-textures_assets_all.bundle` | 4.8 MB | Texturas variadas | ✅ Sim |
| `ui-widgets_assets_all.bundle` | 1.5 MB | Sprites de UI | ✅ Sim |

---

## 🎨 O Que as Skins Modificam?

### Darkside v4.3 (skin profissional)

Com base na análise, Darkside modifica **apenas**:

1. **Texturas de fundo** (`ui-backgrounds`)
   - Wallpapers de menu
   - Fundos de painéis
   - Gradientes

2. **Fontes** (possivelmente)
   - Texturas de atlas de fontes
   - Não as fontes em si

3. **Sprites de UI** (pequenos)
   - Ícones
   - Checkboxes/radio buttons
   - Elementos decorativos

**NÃO modifica**:
- ❌ Estrutura de painéis (hardcoded)
- ❌ Cores de texto/backgrounds (no MonoBehaviour)
- ❌ Layout de atributos
- ❌ Posicionamento de elementos

---

## 🚧 Limitações Técnicas

### Por que não há XMLs?

FM26 migrou para **Unity UI Toolkit moderno**:
- UI definida em **C# + USS** (Unity Style Sheets)
- USS compilado em **StyleSheet MonoBehaviours**
- Não há arquivos `.uss` ou `.uxml` editáveis nos bundles

### Por que MonoBehaviours são difíceis?

- Formato binário proprietário Unity
- Requer **AssetRipper/AssetStudio** para editar
- Re-serialização pode corromper assets
- Mudanças mínimas são arriscadas

---

## 💡 Estratégias Viáveis para Skins

### Opção 1: Substituição de Texturas (SEGURO)
✅ **Viável** | 🟢 Baixo risco | ⚡ Resultado visual limitado

**O que fazer:**
1. Extrair texturas de backgrounds
2. Modificar no Photoshop/Gimp
3. Re-empacotar no bundle
4. Testar

**Resultado**: Fundos/wallpapers customizados, paleta visual diferente

---

### Opção 2: Modificação de USS via Hex Edit (ARRISCADO)
⚠️ **Complexo** | 🔴 Alto risco | ⚡⚡ Resultado visual significativo

**O que fazer:**
1. Extrair StyleSheet MonoBehaviours
2. Identificar offsets de cores/valores no binário
3. Editar valores hex diretamente
4. Re-empacotar e testar extensivamente

**Resultado**: Cores de UI, tamanhos de fonte, espaçamentos

**Problemas:**
- Fácil corromper o asset
- Offsets mudam entre versões FM
- Difícil de manter

---

### Opção 3: Injeção de DLL (MOD COMPLETO)
🔥 **Muito Complexo** | 🔴🔴 Risco de ban/incompatibilidade | ⚡⚡⚡ Controle total

**O que fazer:**
1. Criar plugin BepInEx/MelonLoader
2. Hookar sistema de UI em runtime
3. Sobrescrever estilos programaticamente

**Resultado**: Controle total, atributos coloridos possíveis

**Problemas:**
- Pode violar TOS
- Complexidade extrema
- Quebra a cada atualização

---

## 🎯 Recomendação para Skin Brasileira

Dado as limitações técnicas, sugiro **abordagem híbrida**:

### Fase 1: Skin de Texturas (2-3 dias)
✅ Criar backgrounds temáticos brasileiros
✅ Modificar wallpapers de menu
✅ Customizar sprites pequenos (se possível)

**Output**: Skin visual distinta, instalação simples

### Fase 2: Investigar Fonts (1-2 dias)
🔍 Testar substituição de fontes
🔍 Ver se afeta legibilidade/estilo

**Output**: Tipografia brasileira (se viável)

### Fase 3: USS Hex Editing (experimental)
⚠️ Tentar modificar cores via hex edit
⚠️ Documentar offsets para manutenção

**Output**: Paleta de cores customizada (se estável)

---

## 🛠️ Ferramentas Necessárias

### Para Opção 1 (Texturas):
- ✅ UnityPy (já temos)
- ✅ Gimp/Photoshop
- ✅ Python scripts (já temos)

### Para Opção 2 (USS Hex):
- AssetStudio GUI (para visualizar StyleSheets)
- HxD ou outro hex editor
- Script Python para re-empacotamento

### Para Opção 3 (DLL):
- BepInEx framework
- dnSpy (decompilador .NET)
- C# IDE (Visual Studio/Rider)
- Conhecimento avançado de IL2CPP hooking

---

## 📚 Referências Técnicas

### Asset Bundles
- UnityPy: https://github.com/K0lb3/UnityPy
- AssetStudio: https://github.com/Perfare/AssetStudio

### UI Toolkit
- Unity Docs: https://docs.unity3d.com/Manual/UIElements.html
- USS Reference: https://docs.unity3d.com/Manual/UIE-USS.html

### Modding
- BepInEx: https://github.com/BepInEx/BepInEx
- IL2CPP Unhollower: https://github.com/knah/Il2CppAssemblyUnhollower

---

## 🔬 Próximos Experimentos

1. [ ] Extrair e visualizar backgrounds do Darkside
2. [ ] Comparar MrTini vs Darkside (diferenças de texturas)
3. [ ] Tentar re-empacotar bundle modificado
4. [ ] Testar no FM26 (validar processo)
5. [ ] Documentar pipeline funcional

---

## ⚠️ Conclusão Realista

**Não será possível fazer "atributos em bolinhas" sem mod de DLL.**

A melhor skin possível com segurança é:
- Backgrounds brasileiros elegantes
- Paleta visual coesa
- Possivelmente fontes customizadas

Isso ainda é **suficiente para ser a primeira skin BR** e se destacar visualmente! 🇧🇷

---

_Atualizado: 2026-03-10 18:52 GMT-3_
