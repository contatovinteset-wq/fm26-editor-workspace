# Mapeamento de IDs - FM26 Brasil

**Fonte:** Dados coletados em jogo pelo usuário
**Data:** 20/02/2026

---

## 🇧🇷 Competições Brasileiras

| ID | Nome no Jogo | Nome Real | Divisão |
|----|--------------|-----------|---------|
| 102423 | Série A | Campeonato Brasileiro Série A | 1ª |
| - | Série B | Campeonato Brasileiro Série B | 2ª |
| - | Série C | Campeonato Brasileiro Série C | 3ª |
| - | Série D | Campeonato Brasileiro Série D | 4ª |
| - | Copa do Brasil | Copa do Brasil | - |
| - | Libertadores | CONMEBOL Libertadores | - |
| - | Sul-Americana | CONMEBOL Sul-Americana | - |

---

## 🏟️ Clubes Brasileiros

| ID | Nome no Jogo | Nome Real | Cidade | Estado |
|----|--------------|-----------|--------|--------|
| 319 | COR | Corinthians | São Paulo | SP |

> **Nota:** Clubes sem licença aparecem com nomes abreviados (3 letras)

---

## 🔗 Estrutura de Relacionamentos

```
Competition: 102423 (Série A)
    │
    ├── Teams (20 clubes)
    │   ├── 319 (COR/Corinthians)
    │   ├── ??? (FLA/Flamengo)
    │   ├── ??? (PAL/Palmeiras)
    │   └── ...
    │
    └── Sub-competitions
        ├── ??? (Paulista - estadual)
        ├── ??? (Carioca - estadual)
        └── ...
```

---

## 📋 Como Coletar Mais IDs

### Método 1: FM Live Editor 26
1. Abrir o jogo com FM Live Editor ativo
2. Navegar até o clube/competição
3. O ID aparece no painel do editor

### Método 2: F12 (Debug Mode)
1. Alguns saves mostram IDs ao pressionar F12
2. Verificar se funciona no FM26

### Método 3: Arquivos de Save
1. Os saves (.fm) contêm todos os IDs
2. Usar FM Save Editor para extrair

---

## 🎯 Próximos IDs a Coletar

### Prioridade Alta - Série A
- [ ] Flamengo (FLA)
- [ ] Palmeiras (PAL)
- [ ] São Paulo (SAO)
- [ ] Santos (SAN)
- [ ] Grêmio (GRE)
- [ ] Internacional (INT)
- [ ] Atlético-MG (ATL)
- [ ] Fluminense (FLU)
- [ ] Botafogo (BOT)
- [ ] Vasco (VAS)

### Prioridade Média - Série B
- [ ] Listar todos os 20 clubes

### Prioridade Baixa - Estaduais
- [ ] Paulista (competição + clubes)
- [ ] Carioca (competição + clubes)
- [ ] Gaúcho, Mineiro, etc.

---

## 💡 Dica: Padrão de Nomes

Clubes sem licença usam siglas de 3 letras:
- COR = Corinthians
- FLA = Flamengo
- PAL = Palmeiras
- GRE = Grêmio
- INT = Internacional

Essas siglas podem ajudar a identificar clubes ao buscar no FM Live Editor.
