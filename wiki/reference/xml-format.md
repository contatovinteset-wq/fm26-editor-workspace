# Formato XML do FM26

Este documento descreve a estrutura dos arquivos XML extraídos dos arquivos .fmf.

---

## Estrutura Base

Todos os arquivos XML do FM26 seguem uma estrutura comum:

```xml
<?xml version="1.0" encoding="utf-8"?>
<record>
  <!-- Conteúdo específico do arquivo -->
</record>
```

---

## Tipos de Dados

### string
Texto simples
```xml
<string id="name" value="achievement_beat_a_rival"/>
```

### integer
Número inteiro
```xml
<integer id="max_value" value="1"/>
```

### boolean
Valor booleano (true/false)
```xml
<boolean id="preload" value="true"/>
```

### translation
Texto traduzível (referencia strings do languages.fmf)
```xml
<translation id="display_name" translation_id="553361" type="use" value="'I Would Love It If We Beat Them'"/>
```

### list
Lista de registros
```xml
<list id="items">
  <record>...</record>
  <record>...</record>
</list>
```

### record
Registro aninhado
```xml
<record id="mappings">
  <string id="steamworks" value="achievement_beat_a_rival"/>
</record>
```

---

## Arquivos XML Documentados

### achievements.xml

**Localização:** `data/game_simulation/achievements.fmf`
**Tamanho extraído:** 66 KB

**Estrutura:**
```xml
<record>
  <list id="items">
    <record>
      <!-- Nome interno da conquista -->
      <string id="name" value="achievement_beat_a_rival"/>
      
      <!-- Nome exibido (traduzível) -->
      <translation id="display_name" translation_id="553361" type="use" value="'I Would Love It If We Beat Them'"/>
      
      <!-- Descrição (traduzível) -->
      <translation id="description" translation_id="553362" type="use" value="You beat a rival team"/>
      
      <!-- Habilitado? -->
      <string id="enabled" value="Yes"/>
      
      <!-- Valor máximo (1 = conquista simples) -->
      <integer id="max_value" value="1"/>
      
      <!-- Tipo: manager, player, etc -->
      <string id="type" value="manager"/>
      
      <!-- Mapeamentos por plataforma -->
      <record id="mappings">
        <string id="steamworks" value="achievement_beat_a_rival"/>
        <string id="epicstore" value="achievement_beat_a_rival"/>
        <string id="xboxlive" value="achievement_beat_a_rival"/>
        <string id="psn" value="1"/>
        <string id="googleplay" value="achievement_beat_a_rival"/>
      </record>
    </record>
  </list>
</record>
```

**Campos editáveis:**
| Campo | Tipo | Descrição | Editável |
|-------|------|-----------|----------|
| name | string | ID interno | ✅ |
| display_name | translation | Nome visível | ✅ |
| description | translation | Descrição | ✅ |
| enabled | string | Habilitado | ✅ |
| max_value | integer | Progresso máx | ✅ |
| type | string | Tipo | ✅ |
| mappings | record | IDs por plataforma | ✅ |

**Ideias de modificação:**
- Criar conquistas personalizadas
- Desabilitar conquistas indesejadas
- Modificar textos de conquistas
- Criar mods de "conquistas customizadas"

---

### store.xml

**Localização:** `data/game_simulation/store.fmf`
**Tamanho extraído:** 768 bytes

**Estrutura:**
```xml
<record>
  <list id="items">
    <record>
      <!-- ID do desbloqueável -->
      <string id="unlockable" value="17"/>  <!-- 17 = In-Game Editor -->
      
      <!-- IDs da Epic Games Store -->
      <string id="offer_id" value="7cba406da746429f8a868c5b10105c3f"/>
      <string id="item_id" value="b8ae39698d9a4d2c8e0b4eb8f271b643"/>
      
      <!-- Categoria -->
      <string id="item_category" value="editor"/>
    </record>
  </list>
</record>
```

**IDs de unlockables conhecidos:**
| ID | Item |
|----|------|
| 17 | In-Game Editor |

**⚠️ Nota:** Não é possível desbloquear o editor editando este arquivo. A verificação é feita server-side.

---

### media.xml

**Localização:** `data/game_simulation/media.fmf`
**Tamanho extraído:** 1.4 KB

**Estrutura:**
```xml
<record>
  <list id="items">
    <record>
      <string id="id" value="alternative_press_conference_answer"/>
      <string id="answer" value="..."/>
      <record id="conditions">
        <!-- Condições para esta resposta aparecer -->
      </record>
    </record>
  </list>
</record>
```

**Condições conhecidas:**
- `consortium` - Consórcio
- `club_ownership` - Propriedade do clube

---

### leaderboards.xml

**Localização:** `data/game_simulation/leaderboards.fmf`
**Tamanho extraído:** 12.6 KB

**Estrutura:**
```xml
<record>
  <list id="items">
    <record>
      <string id="name" value="fantasy_draft_overall"/>
      <string id="type" value="fantasy_draft"/>
      <record id="mappings">
        <string id="steamworks" value="fantasy_draft_overall"/>
        <string id="epicstore" value="fantasy_draft_overall"/>
      </record>
    </record>
  </list>
</record>
```

---

### settings.xml

**Localização:** `data/game_simulation/settings.fmf`
**Tamanho extraído:** 343 bytes

**Estrutura:**
```xml
<record>
  <boolean id="preload" value="true"/>
  <boolean id="cache" value="true"/>
  <boolean id="dont_recurse" value="true"/>
</record>
```

**Campos:**
| Campo | Descrição |
|-------|-----------|
| preload | Carregar na inicialização |
| cache | Usar cache |
| dont_recurse | Não processar recursivamente |

---

### filters.xml

**Localização:** `data/game_simulation/filters.fmf`
**Tamanho extraído:** 60 bytes

Contém configurações de cache de filtros de busca.

---

### training.xml

**Localização:** `data/game_simulation/training.fmf`
**Tamanho extraído:** 112 bytes

Contém configurações de preload de treinamento.

---

### profanity_filter.xml

**Localização:** `data/game_simulation/profanity_filter.fmf`
**Tamanho extraído:** 329 bytes

Configurações do filtro de palavrões.

---

## Como Editar

### 1. Extrair o FMF
```python
import zstandard as zstd

with open("achievements.fmf", "rb") as f:
    data = f.read()

zstd_data = data[26:]  # Pular header
dctx = zstd.ZstdDecompressor()
xml_content = dctx.decompress(zstd_data, max_output_size=10*1024*1024)

with open("achievements.xml", "wb") as f:
    f.write(xml_content)
```

### 2. Editar o XML
Use qualquer editor de texto (VSCode, Notepad++, etc.)

### 3. Reempacotar
```python
import zstandard as zstd

with open("achievements.xml", "rb") as f:
    xml_content = f.read()

cctx = zstd.ZstdCompressor()
compressed = cctx.compress(xml_content)

# Reconstruir header FMF
header = bytes([0x02, 0x01]) + b"fmf." + bytes(20)

with open("achievements.fmf", "wb") as f:
    f.write(header + compressed)
```

---

## Próximos Passos

- [ ] Documentar formato binário do languages.fmf
- [ ] Mapear todos os IDs de translation
- [ ] Criar ferramenta gráfica de edição
- [ ] Testar modificações no jogo

---

*Documentação gerada em 2026-02-19*
