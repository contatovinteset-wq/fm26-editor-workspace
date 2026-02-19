# Análise do Sistema de UI do FM26

## Mudança de Tecnologia

### FM24 e anteriores:
- XML simples para skins
- Arquivos de configuração em texto
- Modificação direta de arquivos

### FM26 (Unity Engine):
- **Unity UI Toolkit** (novo sistema)
- Arquivos `.uxml` (UXML - Unity XML)
- Arquivos `.uss` (USS - Unity Style Sheets, similar a CSS)
- Asset Bundles para recursos

---

## Estrutura de UI no Metadata

### Classes encontradas:
```
FMUIProjectConfiguration
PhoneNavigationHeaderContractedStyleClass
CardSettings
PhoneDashboardHeaderSettings
TeamPath
StoreSelectionCardLoadEventContext
```

### Referências de UI:
- `UIPanel` - 3 ocorrências
- `UIView` - múltiplas ocorrências
- `UITheme` - referências de tema
- `Skin` - 82 ocorrências

---

## Arquivos de UI

### Localização:
1. **Asset Bundles:** `fm_Data/VietNorSteam/aa/StandaloneWindows64/`
   - Bundles com "ui" no nome
   - Bundles com "menu" no nome
   - Bundles com "skin" no nome

2. **globalgamemanagers.assets:**
   - Configurações globais de UI
   - Referências de assets

3. **resources.assets:**
   - Recursos carregados sob demanda
   - Texturas de UI

---

## Modificação de Skins

### Método 1: Modificar Bundle Existente
1. Extrair bundle com AssetStudio
2. Editar .uxml/.uss
3. Reempacotar com UABE
4. Substituir bundle original

### Método 2: Criar Nova Skin
1. Usar base de skin existente (MrTini23, Material Skin)
2. Modificar arquivos .uxml/.uss
3. Criar novo bundle
4. Colocar na pasta de skins do usuário

---

## Tutorial de Skinning (FMScout)

**Vídeo:** "HOW TO USE AND MAKE SKINS ON FM26!"
**URL:** https://www.youtube.com/watch?v=YBIGCXjkZvc

### Pontos principais:
- Skins ainda funcionam no FM26
- Limitações do novo sistema Unity
- Ferramentas recomendadas

---

## Ferramentas para UI

1. **AssetStudio** (extrair)
   - GUI para Windows
   - Exporta .uxml, .uss, texturas

2. **UABE** (editar)
   - Unity Asset Bundle Editor
   - Permite modificar e reempacotar

3. **Unity Editor** (avançado)
   - Criar bundles do zero
   - Testar skins em tempo real

---

## Próximos Passos

1. Instalar AssetStudio no Windows
2. Extrair UI bundle de interesse
3. Analisar estrutura .uxml
4. Criar skin de teste
5. Documentar processo completo
