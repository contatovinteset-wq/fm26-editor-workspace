# Contexto Técnico - FM26 Editor Workspace

Este documento reflete as decisões consolidadas de arquitetura, stack e convenções de código do projeto.

## Stack Completa
- **Linguagem:** C# (.NET 6.0)
- **Engine Alvo:** Unity 3D (Backend IL2CPP)
- **Modding Framework:** BepInEx 6+ (variante IL2CPP Interop)
- **UI Framework Nativo:** Unity `UIElements` (manipulação de VisualElements, ScrollViews, etc via scraping CSS-like, ex: classes `si-tile`)
- **Instalador:** Inno Setup 6 (Para deploy simples e inclusão nativa de dependências como winhttp.dll).

## Decisões de Arquitetura
- **Attachable Modding:** Os hooks lógicos não são agressivos no carregamento inicial (`BasePlugin`). Tudo é gerido instanciando um novo componente MonoBehaviour e embutindo no ciclo de vida em lote padrão da Unity (`Update()`).
- **Scraping Customizado do DOM:** O extraction visualiza a árvore do UI toolkit da Unity para raspar informações da UI usando classes ("si-tile", "ip-tiles") ou identificando subárvores.
- **Reflection / Hooks Futuros (FM26TacticsTypeMap):** O projeto está migrando as capturas passivas de UI para a injeção em bibliotecas Assembly nativas (ex: namespace `SI.*`, `PlayerDecision.EvaluateCrossing`) para rastrear o que a Engine simula em Runtime, comprovando falsos-positivos da interface gráfica.

## Convenções de Código
- Referências locais `<FM26Path>` no `.csproj` são mantidas para compilação fluída por desenvolvedores, mas descartadas durante o build release via Inno Setup.
- Acesso à memórias diretas com Casting Unity evitam interrupções no IL2CPP usando extensões como `.WrapToIl2Cpp()` em `StartCoroutine` e varrendo ponteiros literais em instâncias (`new Label(el.Pointer)`).
- Todo macro gerador via `PostBuild` já envia a `.dll` diretamente para a pasta de `plugins` do jogo localmente.

## Referências Críticas
- Unity/IL2CPP requer os Interops base, frequentemente localizados em:
  - `BepInEx.Core.dll`
  - `BepInEx.Unity.IL2CPP.dll`
  - `Il2CppInterop.Runtime.dll`
  - `0Harmony.dll`
- As DLLs moddadas cruciais do FM26: `UnityEngine.UIElementsModule`, `UnityEngine.CoreModule`.
