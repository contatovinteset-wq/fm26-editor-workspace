# Análise do Sistema de UI - FM26

## Visão Geral

O FM26 utiliza **Unity UI Toolkit** (não mais o sistema XML antigo do FM24).

### Assembly Principal: FM.UI.dll

#### Namespaces identificados:
- `FM.UI.TacticInstructions` - Instruções táticas
- `FM.UI.SetPiecesPitchWidget` - Widget de bola parada
- `FM.UI.MatchSimulation` - Simulação de partida
- `FM.UI.Bodymovin` - Animações Lottie
- `FM.UI.VisualFunctions` - Funções visuais
- `FM.UI.PromoBanner` - Banners promocionais
- `FM.UI.Widgets` - Widgets de UI
- `FM.UI.Widgets.DebugDisplays` - Exibição de debug

---

## Sistema de Elementos Visuais

### Classes principais:
```
VisualElement → Elemento base da UI
PlayerVisualElementTemplate → Template de jogador
NavigationVisualElementsLimiter → Limitador de navegação
```

### Métodos importantes:
```
- SetVisualElementVisibility() → Controla visibilidade
- FocusFirstNavigatableChildFromVisualElement() → Foco em elementos
- ScrollToCurrentFocusedVisualElement() → Scroll para elemento
- RegisterNavigatableVisualElement() → Registro de navegação
```

---

## Sistema de Exportação (Ctrl+P)

### Status: FUNCIONALIDADE REMOVIDA

### Classes encontradas:
```
- BindableExportData → Dados de exportação vinculáveis
- CustomViewExportData → Dados de view customizada
- ExportCurrentViewLabel → Label de exportação atual
- CreateExportDataFromCustomView() → Criar dados de view customizada
```

### Análise:
O sistema de exportação existe no código, mas a funcionalidade foi desabilitada. Possíveis razões:
1. Removido por decisão de design
2. Substituído por outro sistema
3. Ainda presente mas sem binding de tecla

### Como reativar (teoria):
1. Hook no método `ExportCurrentItemToBinding`
2. Injetar binding de tecla Ctrl+P
3. Ou usar FM Live Editor para acessar funcionalidade

---

## Arquivos de Skin/UI

### Localização:
Os arquivos de UI estão nos **Asset Bundles**:
```
fm_Data/VietNorSteam/aa/StandaloneWindows64/
```

### Formato:
- `.uxml` - Definição de estrutura (como HTML)
- `.uss` - Estilos (como CSS)
- `.bundle` - Pacotes Unity compilados

### Para modificar skins:
1. Extrair Asset Bundle com AssetStudio
2. Editar arquivos .uxml/.uss
3. Reempacotar com UABE
4. Testar no jogo

---

## Próximos Passos para UI Modding

1. **Instalar AssetStudio** para extrair UI dos bundles
2. **Identificar bundle de UI** principal (provavelmente contém "ui" no nome)
3. **Mapear estrutura de arquivos .uxml**
4. **Criar skin de teste** modificando cores/fontes
5. **Documentar processo** para outros mods

---

## Ferramentas Recomendadas

| Ferramenta | Uso |
|-----------|-----|
| AssetStudio | Extrair assets de bundles |
| UABE | Editar e reempacotar bundles |
| dnSpy | Analisar DLLs .NET |
| Il2CppDumper | Extrair código de IL2CPP |
