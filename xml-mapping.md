# Mapeamento de XMLs do FM26

## Arquivos Extraídos (fmf-extracted/)

### 1. achievements.xml (66KB - 824 linhas)
**Estrutura:**
- Define conquistas do jogo (Steam/Epic/Consoles)
- Usa sistema de estatísticas (`statvalue`) para rastrear progresso

**Campos principais:**
- `name` - identificador da conquista
- `statvalue` - estatística associada
- `enabled` - se está ativa
- `display_name` - nome visível ao usuário

**Exemplo de mod possível:**
- Adicionar conquistas personalizadas
- Modificar requisitos de conquistas existentes

---

### 2. settings.xml (343 bytes)
**Estrutura:**
```xml
<record>
    <boolean id="preload" value="true"/>
    <boolean id="cache" value="true"/>
    <boolean id="dont_recurse" value="true"/>
</record>
```
**Função:** Controla como configurações são carregadas na memória

---

### 3. training.xml (109 bytes)
**Estrutura:**
```xml
<record>
    <boolean id="preload" value="false"/>
</record>
```
**Função:** Desabilita preload de dados de treino (carregados sob demanda)

**Possibilidade de mod:**
- Adicionar configurações de treino customizadas
- Modificar intensidade padrão

---

### 4. media.xml (1.4KB - 22 linhas)
**Estrutura:**
- Define respostas alternativas de coletivas de imprensa
- Usa sistema de tradução (`translation_id`)

**Campos:**
- `alt_answer_id` - ID da resposta alternativa
- `alt_answer_headline` - Título da resposta
- `alt_answer_string` - Texto completo

**Mod possível:**
- Adicionar novas respostas para coletivas
- Personalizar tom das respostas

---

### 5. store.xml (765 bytes - 15 linhas)
**Estrutura:**
- Gerencia itens da loja in-game (Editor, etc.)
- Mapeia IDs para diferentes plataformas

**Campos importantes:**
- `unlockable` - ID do item desbloqueável
- `offer_id` - ID da oferta na Epic Store
- `item_id` - ID do item específico
- `item_category` - categoria (editor, etc.)

**Exemplo:**
```
UNLOCKABLE_ID_INGAME_EDITOR = 17
```

---

### 6. leaderboards.xml (12KB - 289 linhas)
**Estrutura:**
- Define placares para diferentes plataformas
- Mapeia nomes internos para IDs de plataforma

**Plataformas suportadas:**
- Steam
- Epic Store
- MS Store
- Xbox Edition
- PlayStation

---

### 7. filters.xml (57 bytes)
```xml
<record>
    <boolean id="cache" value="true"/>
</record>
```
**Função:** Habilita cache de filtros de busca

---

### 8. languages.xml (4KB)
**Estrutura:**
- Arquivo binário com strings de idiomas
- Contém traduções em árabe (entre outros)

**Nota:** Formato não é XML puro, contém dados binários

---

### 9. profanity_filter.xml (326 bytes)
**Estrutura:**
```xml
<record>
    <boolean id="preload" value="false"/>
    <boolean id="cache" value="false"/>
    <list id="supported_types">
        <record type="Text Stream" value="true"/>
    </list>
</record>
```
**Função:** Configura filtro de palavrões como stream (não carregado na memória)

---

## Convenções de Estrutura

### Tags Principais
- `<record>` - Container principal de dados
- `<list id="items">` - Lista de itens
- `<string id="...">` - Campo de texto
- `<integer id="...">` - Campo numérico
- `<boolean id="...">` - Campo verdadeiro/falso
- `<translation id="...">` - Texto traduzível

### Atributos Comuns
- `id` - Identificador do campo
- `value` - Valor do campo
- `type` - Tipo de dado
- `translation_id` - Referência para sistema de tradução

---

## Próximos Passos

1. [ ] Extrair XMLs de configuração da Match Engine (dentro de Asset Bundles)
2. [ ] Mapear estrutura de arquivos .uxml (UI do Unity)
3. [ ] Localizar arquivos de database (jogadores, clubes, ligas)
4. [ ] Criar ferramenta para editar XMLs sem corromper estrutura
