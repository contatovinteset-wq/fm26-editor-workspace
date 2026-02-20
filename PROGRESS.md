# Relatório de Progresso - FM26-Editor

**Data:** 20/02/2026 - 01:00 GMT-3

---

## ✅ Tarefas Concluídas

### 1. Mapeamento de XMLs Extraídos
**Arquivo:** `xml-structure-analysis.md`

| XML | Tamanho | Função | Potencial |
|-----|---------|--------|-----------|
| achievements.xml | 66KB | Conquistas | BAIXO |
| training.xml | 109B | Preload | BAIXO |
| settings.xml | 343B | Config sistema | BAIXO |
| media.xml | 1.4KB | Notícias | MÉDIO |
| profanity_filter.xml | 326B | Censura | MÉDIO |

**Conclusão:** XMLs são de sistema, não contêm dados de gameplay.

---

### 2. Análise do Metadata IL2CPP
**Arquivo:** `global-metadata.dat` (15MB)

**Referências encontradas:**
- Injury: 116 refs (MajorInjury, MinorInjury, InjuryTime)
- Transfer: 695 refs (TransferValue, Wage, Salary)
- Newgen: 25 refs (NewGenPortraitManager, AllowNewgenFaceGeneration)
- MatchEngine: 2 refs (GetMatchEngineCoordPercentage, m_matchEngineVersion)
- UI: 82 refs (FM.UI classes)

**Assemblies identificados:**
- FM.GameConfig.dll ← Configurações do jogo
- FM.UI.dll ← Interface
- FM.Match.dll ← Motor de partida
- SI.Core.dll ← Núcleo

---

### 3. Estrutura de Arquivos do Jogo

```
fm26-game-files/ (2.8GB)
├── fm_Data/
│   ├── il2cpp_data/
│   │   ├── Metadata/global-metadata.dat (15MB) ← Metadados
│   │   └── il2cpp.usym (36MB) ← Símbolos
│   ├── VietNorSteam/aa/StandaloneWindows64/
│   │   └── *.bundle (37+ Asset Bundles)
│   └── Resources/x86_64/
│       └── game_plugin.dll (423MB) ← Código compilado
└── dotnet/ ← Runtime .NET
```

---

## 🔍 Descobertas Importantes

### Sistema de UI (FM.UI)
O FM26 usa **Unity UI Toolkit**:
- Arquivos: `.uxml` (estrutura) e `.uss` (estilo)
- Classes principais: FM.UI.Widgets, FM.UI.TacticInstructions
- Skins funcionam mas com limitações

### Export de Dados
Encontrei referências a:
- `ExportTrainingSchedule` - Exportar treinos
- `BindableExportPaths` - Paths de exportação
- `CustomViewExportData` - Dados de views customizadas
- `SaveScreenshotToDisk` - Screenshots

**Possibilidade:** Ctrl+P pode ser reativado via hook ou modificação.

### Match Engine
- `MatchConfigurationGroup` - Grupo de config
- `GetMatchEngineCoordPercentage` - Coordenadas
- Configs provavelmente em Asset Bundles

---

## 📋 Próximos Passos

1. **Extrair Asset Bundles** com AssetStudio
   - Prioridade: bundles de UI e config
   - Formato: Unity Addressables

2. **Analisar FM.GameConfig.dll**
   - Extrair constantes de gameplay
   - Mapear estruturas de dados

3. **Mapear IDs**
   - ClubIDs brasileiros
   - PlayerIDs conhecidos
   - CompetitionIDs

4. **Database brasileira**
   - Pesquisar mods existentes
   - Estruturar projeto no Pre-Game Editor

---

## 🛠️ Ferramentas Criadas

| Arquivo | Função |
|---------|--------|
| tools/analyze_metadata.py | Analisa global-metadata.dat |
| tools/fm26_extractor.py | Extração automatizada |
| tools/extract_bundle_info.py | Info de Asset Bundles |
| tools/Il2CppDumper/ | Engenharia reversa |

---

## 📊 Status Geral

- [x] Mapear XMLs extraídos
- [x] Documentar estrutura
- [x] Criar ferramentas básicas
- [x] Análise profunda do metadata (21K+ refs)
- [ ] Investigar exportação (Ctrl+P) - **30%**
- [ ] Analisar UI detalhadamente
- [x] Extrair Asset Bundles (parcial - OOM)
- [x] Mapear IDs (iniciado: Série A, Corinthians)
- [ ] Criar database brasileira

---

## 🆕 Novas Descobertas (20/02)

### Config Hunter Analysis
- **21.637 linhas** de análise gerada
- **Injury**: 69 referências (EventPlayerInjured*, MajorInjury, MinorInjury)
- **Transfer**: 695+ referências (TransferValue, Wage, ClubBudgetAdjustment)
- **Newgen**: 64 referências (NewGenPortraitData, AllowNewgenFaceGeneration)
- **Match**: 2100+ referências (MatchPerformance, m_matchMaterialPreset)
- **Training**: 900+ referências ($intensityVariation, TrainingSchedule)
- **Finance**: 46 referências (ClubBudgetAdjustment, ClubFinances)

### IDs Mapeados (via usuário em jogo)
- **102423** = Série A (Brasileirão)
- **319** = COR (Corinthians)

### Ctrl+P Investigation
- Funções de exportação ainda existem no código
- `ExportCurrentItemToBinding` - principal função
- `Initialize_ctrlKeyboardprintScreen` - Print Screen pode funcionar
- Possibilidade de restaurar via mod de UI ou hook

---

## 📁 Arquivos Principais

```
fm26-editor-workspace/
├── WIKI.html                    # Manual completo
├── PROGRESS.md                  # Este arquivo
├── ID-MAPPING-BRASIL.md        # Mapeamento IDs BR
├── id-registry.json             # Registro de IDs
├── config-analysis.txt          # 21K+ refs de config
├── ctrlp-deep-investigation.md  # Análise Ctrl+P
├── tools/
│   ├── config_hunter.py         # Busca configs
│   ├── extract_ui.py            # Extrai UI
│   ├── register_id.py           # Registra IDs
│   └── ...
└── extracted-ui/
    └── MonoBehaviour/           # 66 JSONs extraídos
```
