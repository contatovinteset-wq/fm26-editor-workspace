# Análise de Estrutura XML - FM26

## Arquivos Extraídos de FMF

Local: `fmf-extracted/`

### 1. achievements.xml (66KB)
**Função:** Define conquistas do jogo e estatísticas rastreadas

**Estrutura:**
```xml
<record>
  <string id="attributes" value="1.0" />
  <list id="items">
    <record>
      <string id="name" value="career_earnings" />
      <string id="statvalue" value="career_earnings" />
      <string id="enabled" value="Yes" />
    </record>
  </list>
</record>
```

**Campos importantes:**
- `career_earnings` - Ganhos da carreira
- `manager_of_the_month_awards` - Prêmios de técnico do mês
- `seasons_at_one_club` - Temporadas em um clube
- `consecutive_matches_unbeaten` - Jogos invicto consecutivos
- `consecutive_matches_won` - Vitórias consecutivas
- `players_in_team_of_week` - Jogadores no time da semana

**Potencial de modding:** BAIXO - Apenas estatísticas de conquistas

---

### 2. settings.xml (343 bytes)
**Função:** Configurações de carregamento de pastas

**Estrutura:**
```xml
<record>
  <boolean id="preload" value="true"/>
  <boolean id="cache" value="true"/>
  <boolean id="dont_recurse" value="true"/>
</record>
```

**Potencial de modding:** BAIXO - Apenas configuração de sistema

---

### 3. training.xml (109 bytes)
**Função:** Configuração de preload de treino

**Estrutura:**
```xml
<record>
  <boolean id="preload" value="false"/>
</record>
```

**Potencial de modding:** BAIXO - Apenas flag de preload

---

### 4. media.xml (1.4KB)
**Função:** Configuração de mídia e notícias

**Potencial de modding:** MÉDIO - Pode afetar notícias e cobertura midiática

---

### 5. filters.xml (57 bytes)
**Função:** Filtros de pesquisa

**Potencial de modding:** BAIXO

---

### 6. languages.xml (4KB)
**Função:** Idiomas disponíveis

**Potencial de modding:** BAIXO - Apenas lista de idiomas

---

### 7. leaderboards.xml (12KB)
**Função:** Tabelas de classificação online

**Potencial de modding:** BAIXO

---

### 8. profanity_filter.xml (326 bytes)
**Função:** Filtro de palavrões

**Potencial de modding:** MÉDIO - Pode ser editado para remover censura

---

### 9. store.xml (765 bytes)
**Função:** Configuração da loja in-game

**Potencial de modding:** BAIXO

---

## Conclusão

Os XMLs extraídos são **arquivos de sistema**, não contêm dados modificáveis do gameplay como:
- ❌ Taxa de lesões
- ❌ Progresso de newgens
- ❌ Valores de jogadores
- ❌ Configuração da Match Engine

**Próximos passos:**
1. Investigar Asset Bundles para configurações de gameplay
2. Analisar DLLs para encontrar constantes hardcoded
3. Procurar arquivos de configuração em outros formatos (JSON, .dat)
