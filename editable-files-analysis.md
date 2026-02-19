# Análise Completa - Arquivos Editáveis FM26

## 📊 Resumo Executivo

Total de arquivos analisados: **~150 arquivos** entre XML, FMF, JSON e asset bundles.

---

## 🔥 MAIS RELEVANTES PARA MODS

### 1. **achievements.xml** (66 KB) - ⭐⭐⭐⭐⭐
**Caminho:** `data/game_simulation/achievements.fmf`

**O que contém:**
- Todas as conquistas do jogo (Steam, Epic, Xbox, PlayStation)
- Nomes, descrições, condições
- Mapeamentos de IDs para cada plataforma

**Potencial de modificação:**
- ✅ Adicionar conquistas personalizadas
- ✅ Modificar textos de conquistas existentes
- ✅ Habilitar/desabilitar conquistas
- ✅ Criar mods de "conquistas customizadas"

**Exemplo editável:**
```xml
<record>
  <string id="name" value="achievement_beat_a_rival"/>
  <translation id="display_name" value="'I Would Love It If We Beat Them'"/>
  <string id="enabled" value="Yes"/>  <!-- Mudar para "No" para desabilitar -->
  <integer id="max_value" value="1"/>
  <string id="type" value="manager"/>
</record>
```

---

### 2. **Versus Competições** (96 KB total) - ⭐⭐⭐⭐⭐
**Arquivos:** `head_2_head.xml`, `knockout.xml`, `league_2_rounds.xml`, `group_stage_and_knockout.xml`

**O que contém:**
- Regras de competições multiplayer
- Formatos de mata-mata, ligas, grupos
- Datas, horários, regras de jogo

**Potencial de modificação:**
- ✅ Criar novos formatos de torneio
- ✅ Modificar regras (prorrogação, pênaltis)
- ✅ Ajustar número de times
- ✅ Personalizar calendário

**Exemplo editável:**
```xml
<record>
  <string id="type" value="cup"/>
  <string id="number_matches" value="16"/>
  <string id="match_rules" value="extra_time,pen"/>  <!-- Adicionar/remover regras -->
  <string id="time" value="1500"/>  <!-- Horário das partidas -->
</record>
```

---

### 3. **leaderboards.xml** (12.6 KB) - ⭐⭐⭐⭐
**Caminho:** `data/game_simulation/leaderboards.fmf`

**O que contém:**
- Placares online (Steam, Epic, Xbox, PS, Google Play)
- Rankings de Fantasy Draft, simulações, etc.

**Potencial de modificação:**
- ✅ Adicionar novos placares
- ✅ Modificar mapeamentos de plataforma

---

### 4. **media.xml** (1.4 KB) - ⭐⭐⭐
**Caminho:** `data/game_simulation/media.fmf`

**O que contém:**
- Respostas alternativas de coletivas de imprensa
- Condições para cada resposta

**Potencial de modificação:**
- ✅ Adicionar novas respostas
- ✅ Modificar condições (ex: consórcio, propriedade do clube)

---

### 5. **store.xml** (768 bytes) - ⭐⭐⭐
**Caminho:** `data/game_simulation/store.fmf`

**O que contém:**
- Item do In-Game Editor (ID: 17)
- IDs da Epic Games Store

**Potencial:**
- ⚠️ Não permite desbloquear editor (verificação é server-side)
- ✅ Interessante para entender como a loja funciona

---

## 📁 OUTROS ARQUIVOS (Menos Relevantes)

### Configurações Básicas
| Arquivo | Tamanho | Função | Relevância |
|---------|---------|--------|------------|
| settings.xml | 343 B | Preload/cache | ⭐ |
| filters.xml | 60 B | Cache de filtros | ⭐ |
| training.xml | 112 B | Preload de treino | ⭐ |
| profanity_filter.xml | 329 B | Filtro de palavrões | ⭐ |
| templates/*.xml | 60-354 B | Config de pastas | ⭐ |

### Arquivos de Sistema (Não Editáveis)
- `languages.fmf` (197 MB) - Strings de localização em formato binário
- `ScriptingAssemblies.json` - Lista de DLLs do Unity
- `RuntimeInitializeOnLoads.json` - Inicialização de módulos

---

## 🎮 ASSET BUNDLES (1.9 GB)

**Maiores bundles:**
1. `art-characters-male-outfits_assets_all.bundle` - 361 MB (uniformes masculinos)
2. `art-characters-female-outfits_assets_all.bundle` - 356 MB (uniformes femininos)
3. `art-characters-male-skin_assets_all.bundle` - 206 MB (peles masculinas)

**Potencial de modificação:**
- ✅ Uniformes/kits - ALTA viabilidade, baixa dificuldade
- ✅ Cabelos - MÉDIA-ALTA viabilidade
- ✅ Faces/peles - MÉDIA viabilidade
- ✅ Acessórios - ALTA viabilidade

**Ferramentas necessárias:**
- AssetStudio (extrair assets)
- UABE (editar assets)
- Blender/Photoshop (editar modelos/texturas)

---

## 🚫 O QUE NÃO ENCONTRAMOS

1. **Arquivos de skin/UI** - A interface FM26 está dentro dos DLLs compilados (il2cpp), não em XML
2. **Ctrl+P (exportação de dados)** - Funcionalidade removida no código compilado
3. **Configurações de jogabilidade** - Estão nos DLLs, não em arquivos editáveis

---

## ✅ RECOMENDAÇÕES PARA MODS

### Fáceis de Começar:
1. **Criar novos formatos de torneio** - Editar XML de versus comps
2. **Modificar conquistas** - Editar achievements.xml
3. **Editar uniformes** - Usar AssetStudio nos bundles

### Médio:
4. **Adicionar respostas de coletiva** - Editar media.xml
5. **Novos placares** - Editar leaderboards.xml

### Avançado:
6. **Editar texturas de uniformes** - Extrair bundles, editar, reempacotar
7. **Criar ferramenta de descompressão FMF** - Python com zstandard

---

## 📋 Próximos Passos

1. [ ] Testar modificação de achievements.xml
2. [ ] Criar novo formato de torneio em versus comps
3. [ ] Explorar edição de asset bundles com AssetStudio
4. [ ] Documentar processo de reempacotamento FMF

---
*Análise gerada em 2026-02-19*
