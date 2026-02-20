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

## 🏃 Jogadores Brasileiros

### Estrelas Atuais
| ID | Nome | Clube | Posição | Valor |
|----|------|-------|---------|-------|
| - | *Coletar via FM Live Editor* | - | - | - |

### Prospects/Newgens
| ID | Nome | Clube | Posição | Potencial |
|----|------|-------|---------|-----------|
| - | *Coletar jogadores gerados* | - | - | - |

### Ídolos Históricos (se no jogo)
| ID | Nome | Clube Base | Posição |
|----|------|------------|---------|
| - | *Coletar lendas* | - | - |

---

## 📊 Estrutura de Dados por Jogador

```
PlayerID: {
  name: "Nome Completo",
  short_name: "Apelido",
  club_id: 319,
  position: "ST",
  nationality: "Brasil",
  age: 25,
  value: 50000000,
  wage: 150000,
  ca: 150,  // Current Ability
  pa: 170   // Potential Ability
}
```

---

## 🎯 Prioridades de Coleta

### Top 50 Brasileiros Ativos
1. **Atacantes**: Endrick, Vini Jr, Raphinha, Rodrygo, Gabriel Barbosa
2. **Meias**: Bruno Guimarães, Lucas Paquetá, Raphinha
3. **Zagueiros**: Marquinhos, Militão, Gabriel Magalhães
4. **Laterais**: Danilo, Alex Sandro, Renan Lodi
5. **Goleiros**: Alisson, Ederson

### Prospects (Newgens)
- Buscar jogadores com PA alto em clubes brasileiros
- Registrar para acompanhar desenvolvimento

### IDs Especiais
- Newgens gerados pelo jogo (FA automatizar)
- Jogadores sem clube
- Base de jovens

---

## 💡 Dica: Padrão de Nomes

Clubes sem licença usam siglas de 3 letras:
- COR = Corinthians
- FLA = Flamengo
- PAL = Palmeiras
- GRE = Grêmio
- INT = Internacional

Essas siglas podem ajudar a identificar clubes ao buscar no FM Live Editor.

---

## 🔧 Como Coletar IDs de Jogadores

### Método 1: FM Live Editor 26
1. Abrir painel do editor (tecla configurada)
2. Buscar jogador pelo nome
3. Anotar: `ID | Nome | Clube | Posição`

### Método 2: Tela do Jogador
1. Entrar na página do jogador
2. FM Live Editor mostra o ID no topo
3. Screenshot para processar depois

### Método 3: Exportar Lista
1. Criar shortlist no jogo
2. FM Live Editor pode exportar para CSV
3. Processar CSV para extrair IDs

---

## 📝 Template para Enviar IDs

```
JOGADORES:
ID | Nome | Clube | Posição
123456 | Vini Jr | RMA | LW
789012 | Endrick | PAL | ST
...

CLUBES:
ID | Sigla | Nome Real
319 | COR | Corinthians
...

COMPETIÇÕES:
ID | Nome Jogo | Nome Real
102423 | Série A | Brasileirão
```
