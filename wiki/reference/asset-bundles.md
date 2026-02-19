# Asset Bundles - Referência Completa

Lista detalhada de todos os Asset Bundles do FM26 e seu conteúdo.

---

## Localização

```
[FM26]/fm_Data/VietNorSteam/aa/StandaloneWindows64/
```

**Tamanho total:** 1.9 GB
**Quantidade:** 37 bundles

---

## Lista Completa

### 👔 Personagens Masculinos (914 MB)

| Bundle | Tamanho | Conteúdo | Editável |
|--------|---------|----------|----------|
| art-characters-male-outfits_assets_all.bundle | 361 MB | Uniformes, kits, chuteiras, meias | ⭐⭐⭐⭐⭐ |
| art-characters-male-skin_assets_all.bundle | 206 MB | Texturas de pele, tons de pele | ⭐⭐⭐ |
| art-characters-male-hair_assets_all.bundle | 163 MB | Modelos de cabelo com LODs | ⭐⭐⭐⭐ |
| art-characters-male-player_assets_all.bundle | 58 MB | Corpos, meshes, rigs | ⭐⭐ |
| art-characters-male-facialhair_assets_all.bundle | 31 MB | Barbas, bigodes | ⭐⭐⭐⭐ |
| art-characters-male-manager_assets_all.bundle | 23 MB | Modelos de técnicos | ⭐⭐⭐ |
| art-characters-male-accessories_assets_all.bundle | 6.4 MB | Óculos, brincos, relógios | ⭐⭐⭐⭐⭐ |
| art-characters-male-common_assets_all.bundle | 5.2 MB | Assets compartilhados | ⭐⭐ |

**Assets identificados:**
- `FootBoots` - Chuteiras
- `Socks` - Meias
- `Jersey` - Camisas
- `HairShort_Male_XX` - Cabelos curtos
- `HairLong_Male_XX` - Cabelos longos
- `PlayerFace` - Faces de jogadores
- `SkinTones` - Tons de pele

---

### 👩 Personagens Femininos (697 MB)

| Bundle | Tamanho | Conteúdo | Editável |
|--------|---------|----------|----------|
| art-characters-female-outfits_assets_all.bundle | 356 MB | Uniformes femininos | ⭐⭐⭐⭐⭐ |
| art-characters-female-skin_assets_all.bundle | 178 MB | Texturas de pele | ⭐⭐⭐ |
| art-characters-female-player_assets_all.bundle | 68 MB | Corpos de jogadoras | ⭐⭐ |
| art-characters-female-hair-common_assets_all.bundle | 64 MB | Cabelos comuns | ⭐⭐⭐⭐ |
| art-characters-female-hair_assets_all.bundle | 52 MB | Modelos de cabelo | ⭐⭐⭐⭐ |
| art-characters-female-manager_assets_all.bundle | 4.3 MB | Modelos de técnicas | ⭐⭐⭐ |
| art-characters-female-accessories_assets_all.bundle | 7.9 MB | Acessórios | ⭐⭐⭐⭐⭐ |
| art-characters-female-common_assets_all.bundle | 7.7 MB | Assets compartilhados | ⭐⭐ |
| art-characters-female-facialhair_assets_all.bundle | 772 KB | Pelos faciais | ⭐⭐⭐ |

---

### 👁️ Olhos (823 KB)

| Bundle | Tamanho | Conteúdo |
|--------|---------|----------|
| art-characters-eyes_assets_male.bundle | 261 KB | Olhos masculinos |
| art-characters-eyes_assets_female.bundle | 322 KB | Olhos femininos |
| art-characters-eyes_assets_.bundle | 247 KB | Olhos base |

---

### 🏙️ Ambientes (285 MB)

| Bundle | Tamanho | Conteúdo |
|--------|---------|----------|
| art-environments-cityassets_assets_all.bundle | 185 MB | Cidade, estádios, prédios |
| art-environments-ambientocclusionmaps_assets_all.bundle | 100 MB | Mapas de oclusão |
| art-environments-buildings_assets_all.bundle | 12 MB | Estruturas |

---

### 🔧 Compartilhados (53 MB)

| Bundle | Tamanho | Conteúdo |
|--------|---------|----------|
| art-characters-tests_assets_all.bundle | 16 MB | Assets de teste |
| art-characters-kitresources_assets_all.bundle | 8.1 MB | Recursos de kits |
| art-characters-shared-manager_assets_all.bundle | 9.6 MB | Assets de manager |
| art-characters-shared-player_assets_all.bundle | 5.8 MB | Assets de jogador |
| art-characters_assets_outfits.bundle | 16 MB | Referências de outfits |
| art-characters_assets_hair.bundle | 3.1 MB | Referências de cabelo |
| art-common_assets_all.bundle | 481 KB | Assets comuns |
| art-characters-generic_assets_all.bundle | 1.8 MB | Assets genéricos |
| art-characters-2d_assets_all.bundle | 62 KB | Elementos 2D |
| art-characters-3d_assets_all.bundle | 6.3 KB | Referências 3D |

---

## Tipos de Assets

### Texture2D

Texturas 2D (imagens).

**Formatos suportados:**
- PNG, TGA, JPEG, BMP
- DXT/BC (comprimidas)
- ASTC (mobile)

**Extração:**
```python
# AssetStudio ou UnityPy
for obj in env.objects:
    if obj.type.name == "Texture2D":
        data = obj.read()
        data.image.save("texture.png")
```

---

### Mesh

Modelos 3D.

**Formatos de exportação:**
- OBJ (simples)
- FBX (com animações)
- glTF (moderno)

**Extração:**
```python
for obj in env.objects:
    if obj.type.name == "Mesh":
        data = obj.read()
        # Exportar como OBJ
```

---

### Material

Materiais e shaders.

**Propriedades comuns:**
- `_MainTex` - Textura principal
- `_NormalMap` - Mapa normal
- `_Metallic` - Metalicidade
- `_Roughness` - Rugosidade

---

### Animator

Animações.

**Conteúdo:**
- AnimationClips
- AnimatorController
- Avatar

---

## Como Extrair

### Com AssetStudio (GUI)

1. Baixar AssetStudio: https://github.com/Perfare/AssetStudio/releases
2. Abrir `File → Load file`
3. Selecionar o bundle
4. Navegar pelos assets
5. `Export → Export selected assets`

### Com UnityPy (Python)

```python
import UnityPy

# Carregar bundle
env = UnityPy.load("art-characters-male-outfits_assets_all.bundle")

# Listar todos os assets
for obj in env.objects:
    print(f"{obj.type.name}: {obj.read().name if hasattr(obj.read(), 'name') else 'N/A'}")

# Extrair texturas
for obj in env.objects:
    if obj.type.name == "Texture2D":
        data = obj.read()
        data.image.save(f"output/{data.name}.png")

# Extrair modelos
for obj in env.objects:
    if obj.type.name == "Mesh":
        data = obj.read()
        # Salvar como OBJ ou FBX
```

---

## Como Editar

### Texturas (Fácil)

1. Extrair PNG do bundle
2. Editar no Photoshop/GIMP
3. Reinjectar com UABE ou AssetStudio

```python
# Reinjectar textura
import UnityPy

env = UnityPy.load("bundle_path")
for obj in env.objects:
    if obj.type.name == "Texture2D" and obj.read().name == "target_texture":
        # Carregar nova textura
        with open("new_texture.png", "rb") as f:
            new_data = f.read()
        # Substituir
        obj.read().set_image(new_data)
        obj.read().save()

# Salvar bundle modificado
with open("modified_bundle", "wb") as f:
    f.write(env.file.save())
```

### Modelos 3D (Médio)

1. Extrair mesh como FBX/OBJ
2. Editar no Blender
3. Exportar com mesmo nome/estrutura
4. Reinjectar

### Materiais (Médio)

1. Extrair material
2. Editar propriedades
3. Reinjectar

---

## LODs (Levels of Detail)

O FM26 usa sistema de LODs para performance:

| LOD | Distância | Uso |
|-----|-----------|-----|
| LOD0 | Próximo | Qualidade máxima |
| LOD1 | Médio | Qualidade média |
| LOD2 | Distante | Qualidade baixa |
| LOD3 | Muito distante | Qualidade mínima |

**Ao editar modelos, mantenha todos os LODs consistentes!**

---

## Nomenclatura

Padrão de nomes encontrados:

```
[tipo]_[categoria]_[variante]_[LOD]

Exemplos:
- HairShort_Male_01_LOD0
- FootBoots_Nike_Mercurial_LOD1
- Jersey_Home_United_LOD2
```

---

## Próximos Passos

- [Tutorial: Extrair Texturas](../tutorials/texture-extraction.md)
- [Tutorial: Substituir Uniformes](../tutorials/kit-replacement.md)

---

*Referência gerada em 2026-02-19*
