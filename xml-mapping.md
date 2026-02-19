# Mapeamento de XMLs do FM26

## Arquivos Extraídos de FMF

### 1. achievements.xml (66KB)
**Função:** Define conquistas do Steam/Plataformas
**Estrutura:**
- Lista de stats que alimentam conquistas (career_earnings, manager_of_month, etc.)
- Definições de conquistas com display_name, description, icon
- Condições de unlock

**Modificável:** Sim - pode adicionar novas conquistas ou modificar condições

---

### 2. settings.xml (343B)
**Função:** Configurações de carregamento
**Estrutura:**
```xml
<boolean id="preload" value="true"/>
<boolean id="cache" value="true"/>
<boolean id="dont_recurse" value="true"/>
```
**Modificável:** Sim - afeta performance de carregamento

---

### 3. training.xml (109B)
**Função:** Configurações de carregamento de treino
**Modificável:** Sim - pode habilitar preload de treino

---

### 4. store.xml (765B)
**Função:** Itens da loja (In-Game Editor via Epic)
**Conteúdo importante:**
- `unlockable_id="17"` = In-Game Editor
- `offer_id` e `item_id` para integração Epic Store

---

### 5. media.xml (1.4KB)
**Função:** Respostas alternativas de coletivas de imprensa
**Estrutura:**
- `PRESS_CONFERENCE_ALTERNATIVE_ANSWER_ID`
- Textos de resposta com translation_id
- Regras de quando usar cada resposta

**Modificável:** Sim - pode adicionar novas respostas

---

### 6. filters.xml (57B)
**Função:** Configuração de cache de filtros
**Modificável:** Sim

---

### 7. languages.xml (4KB)
**Função:** Strings de localização (árabe neste caso)
**Conteúdo:** Textos traduzidos para competições, lesões, etc.

---

### 8. leaderboards.xml (12.5KB)
**Função:** Leaderboards Steam/Epic/PlayStation/Xbox
**Mapeia:** IDs de leaderboard por plataforma

---

### 9. profanity_filter.xml (326B)
**Função:** Configuração do filtro de palavrões
**Tipos suportados:** Text Stream

---

## XMLs em data/game_simulation/

### versus comps/config.xml
```xml
<boolean id="dont_scan" value="true"/>
```
**Função:** Marca diretório para não ser escaneado

### templates/config.xml
**Função:** Configuração de templates de save game

---

## Próximos Passos

1. **Extrair mais FMFs** - existem outros arquivos FMF no jogo com mais configurações
2. **Investigar pasta de configurações do usuário** - onde ficam saves, configs pessoais
3. **Mapear Asset Bundles** - extrair .uxml/.uss para UI
4. **Encontrar arquivos de Match Engine** - provavelmente em bundles ou configs

---

## Ferramentas Necessárias

- **AssetStudio** - extrair contents dos bundles
- **UABE** - editar e reempacotar bundles
- **FMF Extractor** - já temos, mas precisa automatizar
