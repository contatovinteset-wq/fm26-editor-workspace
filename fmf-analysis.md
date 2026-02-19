# FMF File Format Analysis

## Overview

FMF (Football Manager File) é o formato proprietário da Sports Interactive para arquivos de dados do Football Manager 26.

## Formato do Arquivo

```
┌─────────────────────────────────────────────┐
│ Header FMF (26 bytes)                       │
├─────────────────────────────────────────────┤
│ 0x00-0x01: Versão (02 01)                   │
│ 0x02-0x05: Magic "fmf." (66 6d 66 2e)       │
│ 0x06-0x19: Metadados (tamanho, flags)       │
├─────────────────────────────────────────────┤
│ Payload ZSTD (a partir do offset 26)        │
│ 0x1A: Magic ZSTD (28 b5 2f fd)              │
│ 0x1E: Frame header                          │
│ ...: Dados comprimidos                      │
└─────────────────────────────────────────────┘
```

## Arquivos Analisados

| Arquivo | Tamanho FMF | Tamanho XML | Conteúdo |
|---------|-------------|-------------|----------|
| achievements.fmf | 5.0 KB | 66.3 KB | Definições de conquistas Steam/Epic/Xbox |
| filters.fmf | 14 KB | 60 B | Config de cache de filtros |
| languages.fmf | 197 MB | 4 KB* | Strings de localização (formato binário) |
| leaderboards.fmf | 1.1 KB | 12.6 KB | Configurações de placares |
| media.fmf | 3.9 MB | 1.4 KB | Respostas alternativas de coletivas |
| profanity_filter.fmf | 3.4 KB | 329 B | Config de filtro de palavrões |
| settings.fmf | 3.0 KB | 343 B | Config de preload/cache |
| store.fmf | 1.4 KB | 768 B | Itens da loja (In-Game Editor) |
| training.fmf | 9.7 KB | 112 B | Config de preload de treino |

*Nota: languages.fmf contém strings em formato binário, não XML puro.

## Estrutura dos Arquivos XML

### achievements.xml
```xml
<record>
  <list id="items">
    <record>
      <string id="name" value="achievement_beat_a_rival"/>
      <translation id="display_name" translation_id="553361" type="use" value="'I Would Love It If We Beat Them'"/>
      <translation id="description" translation_id="553362" type="use" value="You beat a rival team"/>
      <string id="enabled" value="Yes"/>
      <integer id="max_value" value="1"/>
      <string id="type" value="manager"/>
      <record id="mappings">
        <string id="steamworks" value="achievement_beat_a_rival"/>
        <string id="epicstore" value="achievement_beat_a_rival"/>
      </record>
    </record>
  </list>
</record>
```

### store.xml
```xml
<record>
  <list id="items">
    <record>
      <string id="unlockable" value="17"/>  <!-- UNLOCKABLE_ID_INGAME_EDITOR -->
      <string id="offer_id" value="7cba406da746429f8a868c5b10105c3f"/>
      <string id="item_id" value="b8ae39698d9a4d2c8e0b4eb8f271b643"/>
      <string id="item_category" value="editor"/>
    </record>
  </list>
</record>
```

### settings.xml
```xml
<record>
  <boolean id="preload" value="true"/>
  <boolean id="cache" value="true"/>
  <boolean id="dont_recurse" value="true"/>
</record>
```

## Como Extrair FMF

```python
import zstandard as zstd

with open("arquivo.fmf", "rb") as f:
    data = f.read()

# Pular 26 bytes do header FMF
zstd_data = data[26:]

# Descomprimir com max_output_size
dctx = zstd.ZstdDecompressor()
result = dctx.decompress(zstd_data, max_output_size=10*1024*1024)
```

## Potencial para Mods

### Alta Viabilidade
- **achievements.xml**: Adicionar/modificar conquistas
- **store.xml**: Modificar itens da loja
- **leaderboards.xml**: Configurar placares

### Média Viabilidade
- **media.xml**: Modificar respostas de coletivas
- **settings.xml**: Configurações de preload

### Baixa Viabilidade
- **languages.fmf**: Formato binário complexo, requer ferramenta específica

## Arquivos Extraídos

Todos os arquivos foram extraídos para:
`/data/.openclaw/workspace/fm26-editor-workspace/fmf-extracted/`

---
*Análise realizada em 2026-02-19*
