# Análise de Asset Bundles - Football Manager 26

**Data:** 2026-02-18  
**Localização:** `/data/.openclaw/workspace/fm26-game-files/fm_Data/VietNorSteam/aa/StandaloneWindows64/`  
**Tamanho Total:** 1.9 GB

---

## 1. Formato dos Bundles

### Cabeçalho Identificado
```
Assinatura: UnityFS
Versão Unity: 5.x.x
Build: 6000.0.52f1-fm26-05f1
```

**Conclusão:** Os arquivos são **AssetBundles Unity** no formato UnityFS, criados com Unity 6000.x (versão muito recente). O sufixo `-fm26-05f1` indica customização específica da Sports Interactive.

---

## 2. Lista Completa de Bundles (37 arquivos)

### 2.1 Personagens Masculinos (914 MB)

| Arquivo | Tamanho | Conteúdo Estimado |
|---------|---------|-------------------|
| `art-characters-male-outfits_assets_all.bundle` | **361 MB** | Uniformes, kits, chuteiras, meias completos |
| `art-characters-male-skin_assets_all.bundle` | **206 MB** | Texturas de pele, tons de pele, variações étnicas |
| `art-characters-male-hair_assets_all.bundle` | **163 MB** | Modelos de cabelo com LODs (0-3), materiais |
| `art-characters-male-player_assets_all.bundle` | **58 MB** | Corpos de jogadores, meshes, rigs |
| `art-characters-male-facialhair_assets_all.bundle` | **31 MB** | Barbas, bigodes, pelos faciais |
| `art-characters-male-manager_assets_all.bundle` | **23 MB** | Modelos de técnicos/manager |
| `art-characters-male-common_assets_all.bundle` | **5.2 MB** | Assets compartilhados masculinos |
| `art-characters-male-accessories_assets_all.bundle` | **6.4 MB** | Acessórios (óculos, brincos, etc) |

**Assets Detectados:**
- `FootBoots`, `Socks`, `Jersey` (uniformes)
- `HairShort_Male_XX`, `HairLong_Male_XX`, `HairMedium_Male_XX`
- `PlayerFace`, `PlayerEyes`, `SkinTones`
- LODs: `_LOD0`, `_LOD1`, `_LOD2`, `_LOD3`
- `AnimTransforms`, `AnimBindPoses` (animações)

---

### 2.2 Personagens Femininos (697 MB)

| Arquivo | Tamanho | Conteúdo Estimado |
|---------|---------|-------------------|
| `art-characters-female-outfits_assets_all.bundle` | **356 MB** | Uniformes femininos completos |
| `art-characters-female-skin_assets_all.bundle` | **178 MB** | Texturas de pele femininas |
| `art-characters-female-player_assets_all.bundle` | **68 MB** | Corpos de jogadoras, meshes |
| `art-characters-female-hair-common_assets_all.bundle` | **64 MB** | Cabelos comuns femininos |
| `art-characters-female-hair_assets_all.bundle` | **52 MB** | Modelos de cabelo feminino |
| `art-characters-female-manager_assets_all.bundle` | **4.3 MB** | Modelos de técnicas |
| `art-characters-female-common_assets_all.bundle` | **7.7 MB** | Assets compartilhados femininos |
| `art-characters-female-accessories_assets_all.bundle` | **7.9 MB** | Acessórios femininos |
| `art-characters-female-facialhair_assets_all.bundle` | **772 KB** | Pelos faciais femininos |

---

### 2.3 Assets de Olhos (823 KB)

| Arquivo | Tamanho | Conteúdo |
|---------|---------|----------|
| `art-characters-eyes_assets_.bundle` | 247 KB | Texturas de olhos base |
| `art-characters-eyes_assets_female.bundle` | 322 KB | Olhos femininos |
| `art-characters-eyes_assets_male.bundle` | 261 KB | Olhos masculinos |

---

### 2.4 Ambientes (285 MB)

| Arquivo | Tamanho | Conteúdo |
|---------|---------|----------|
| `art-environments-cityassets_assets_all.bundle` | **185 MB** | Cidade, estádios, prédios |
| `art-environments-ambientocclusionmaps_assets_all.bundle` | **100 MB** | Mapas de oclusão ambiente |
| `art-environments-buildings_assets_all.bundle` | **12 MB** | Estruturas de prédios |

**Assets Detectados:**
- `CampusBuildings_XX`, `OfficeBuilding_XX`
- `Building` meshes e materiais

---

### 2.5 Assets Compartilhados e Outros (53 MB)

| Arquivo | Tamanho | Conteúdo |
|---------|---------|----------|
| `art-characters_assets_outfits.bundle` | 16 MB | Referências de outfits |
| `art-characters-tests_assets_all.bundle` | 16 MB | Assets de teste |
| `art-characters-kitresources_assets_all.bundle` | 8.1 MB | Recursos de kits (mascotes, braçadeiras) |
| `art-characters-shared-manager_assets_all.bundle` | 9.6 MB | Assets compartilhados de manager |
| `art-characters-shared-player_assets_all.bundle` | 5.8 MB | Assets compartilhados de jogador |
| `art-characters_assets_hair.bundle` | 3.1 MB | Referências de cabelo |
| `art-characters-generic_assets_all.bundle` | 1.8 MB | Assets genéricos |
| `art-common_assets_all.bundle` | 481 KB | Assets comuns gerais |
| `art-characters-2d_assets_all.bundle` | 62 KB | Elementos 2D |
| `art-characters-3d_assets_all.bundle` | 6.3 KB | Referências 3D |
| `art-characters_assets_common.bundle` | 4.5 KB | Manifest comum |
| `art-characters_assets_male.bundle` | 27 KB | Manifest masculino |
| `art-characters_assets_female.bundle` | 27 KB | Manifest feminino |

---

## 3. Tipos de Assets Identificados

### 3.1 Geometria 3D
- **Meshes:** SkinnedMeshRenderer, Mesh
- **LODs:** Níveis de detalhe 0-3
- **Rigs:** Esqueletos para animação

### 3.2 Texturas
- **Formato:** Provavelmente BC7/DXT (compressed)
- **Tipos:** Albedo, Normal, Roughness, AO
- **Dimensões:** Variadas (detectado m_Width/m_Height)

### 3.3 Materiais
- **Shaders:** Custom da SI
- **Propriedades:** _MainTex, _NormalMap, etc.

### 3.4 Animações
- **Tipos:** AnimTransforms, AnimBindPoses
- **Aplicação:** Cabelos animados, expressões

---

## 4. Compatibilidade com Ferramentas de Extração

### AssetStudio
| Aspecto | Status |
|---------|--------|
| Formato UnityFS | ✅ Suportado |
| Versão Unity 6000.x | ⚠️ Pode requerer versão recente |
| Extração de texturas | ✅ Possível |
| Extração de meshes | ✅ Possível |
| Descompactação | ⚠️ Pode ter compressão LZ4/HC |

**Recomendação:** Usar AssetStudio versão mais recente (v0.17+ ou build nightly)

### UABE (Unity Assets Bundle Extractor)
| Aspecto | Status |
|---------|--------|
| Formato UnityFS | ✅ Suportado |
| Edição de assets | ✅ Possível |
| Substituição de texturas | ✅ Possível |
| Versão Unity 6000.x | ⚠️ Pode ter limitações |

**Recomendação:** UABE 3.0+ ou AABBETA para Unity 2022+

### UnityPy (Python)
```python
# Exemplo de uso
import UnityPy

env = UnityPy.load("bundle_path")
for obj in env.objects:
    if obj.type.name == "Texture2D":
        data = obj.read()
        data.image.save("output.png")
```

**Status:** ⚠️ Pode requerer atualização para Unity 6000

---

## 5. Potencial para Modificação/Substituição

### 5.1 Uniformes e Kits ⭐⭐⭐⭐⭐ (ALTO)

**Bundles alvo:**
- `art-characters-male-outfits_assets_all.bundle` (361 MB)
- `art-characters-female-outfits_assets_all.bundle` (356 MB)

**Conteúdo:**
- Chuteiras (FootBoots)
- Meias (Socks)
- Camisas/Jerseys
- Shorts

**Viabilidade:** **ALTA** - Texturas 2D podem ser substituídas facilmente. Modelos 3D requerem mais trabalho.

---

### 5.2 Cabelos ⭐⭐⭐⭐ (MÉDIO-ALTO)

**Bundles alvo:**
- `art-characters-male-hair_assets_all.bundle` (163 MB)
- `art-characters-female-hair_assets_all.bundle` (52 MB)
- `art-characters-female-hair-common_assets_all.bundle` (64 MB)
- `art-characters-male-facialhair_assets_all.bundle` (31 MB)

**Conteúdo:**
- Modelos de cabelo com LODs
- Texturas de cabelo
- Animações (AnimTransforms)

**Viabilidade:** **MÉDIA** - Exige conhecimento de modelagem 3D e sistema de LODs do Unity.

---

### 5.3 Faces/Peles ⭐⭐⭐ (MÉDIO)

**Bundles alvo:**
- `art-characters-male-skin_assets_all.bundle` (206 MB)
- `art-characters-female-skin_assets_all.bundle` (178 MB)
- `art-characters-male-player_assets_all.bundle` (58 MB)
- `art-characters-female-player_assets_all.bundle` (68 MB)
- `art-characters-eyes_assets_*.bundle` (823 KB total)

**Conteúdo:**
- Texturas de pele (SkinTones)
- Geometria facial (PlayerFace)
- Olhos (PlayerEyes)

**Viabilidade:** **MÉDIA** - Faces usam sistema complexo com blendshapes/morphs. Substituição de texturas é simples, geometria é complexa.

---

### 5.4 Acessórios ⭐⭐⭐⭐ (ALTO)

**Bundles alvo:**
- `art-characters-male-accessories_assets_all.bundle` (6.4 MB)
- `art-characters-female-accessories_assets_all.bundle` (7.9 MB)

**Conteúdo:**
- Óculos, brincos, relógios, etc.

**Viabilidade:** **ALTA** - Assets pequenos e modulares.

---

## 6. Fluxo de Trabalho Recomendado

### Para Extração:
```
1. Instalar AssetStudio (Windows) ou compilar para Linux
2. Carregar bundle
3. Exportar assets por tipo (Texture2D, Mesh, Material)
4. Converter formatos conforme necessário
```

### Para Substituição:
```
1. Extrair asset original
2. Editar (Photoshop para texturas, Blender para modelos)
3. Usar UABE ou AssetStudio para reinjetar
4. Rebuild do bundle
5. Testar no jogo
```

---

## 7. Limitações e Considerações

### 7.1 Técnicas
- **Versão Unity 6000:** Muito recente, ferramentas podem não suportar totalmente
- **Compressão:** Provavelmente LZ4 ou LZ4HC (comum em mobile/consoles)
- **Dependências:** Assets podem ter referências cruzadas

### 7.2 Legais
- ⚠️ Modificação de assets é violação dos termos de uso
- Distribuição de mods pode infrigir direitos autorais
- Uso pessoal sob risco próprio

### 7.3 Práticas
- Sempre faça backup dos bundles originais
- Teste em ambiente isolado primeiro
- Documente todas as alterações

---

## 8. Próximos Passos

1. **Instalar AssetStudio** ou UABE para testar extração
2. **Analisar estrutura interna** de um bundle pequeno (ex: eyes)
3. **Testar substituição** de textura simples
4. **Documentar formato** dos assets específicos (nomes, GUIDs)
5. **Criar pipeline** de build automatizada

---

## 9. Resumo Executivo

| Categoria | Total | Modificável | Dificuldade |
|-----------|-------|-------------|-------------|
| Uniformes/Kits | 717 MB | ✅ Sim | Baixa (texturas) |
| Cabelos | 310 MB | ✅ Sim | Média |
| Peles/Faces | 510 MB | ⚠️ Parcial | Alta |
| Acessórios | 14.3 MB | ✅ Sim | Baixa |
| Ambientes | 297 MB | ✅ Sim | Média |
| **TOTAL** | **1.9 GB** | | |

---

*Documento gerado automaticamente pela análise de Asset Bundles do FM26*
