# Plano de Desenvolvimento - Skin FM26 Brasileira

**Data início**: 2026-03-10  
**Status**: 🔄 Em progresso  
**Objetivo**: Criar a primeira skin brasileira para FM26

---

## 📦 Materiais Disponíveis

### ✅ Já temos:
1. **Darkside v4.3** (bundles atualizados para FM26)
   - Skin profissional baseada em modificação de bundles
   - 242MB de assets compilados
   - Compatível com última atualização do FM26
   
2. **MrTini v1.2** (bundles anteriores)
   - Skin popular da comunidade
   - 133MB de assets
   
3. **Extrações anteriores**
   - `mrtini-extracted/`: assets da MrTini
   - Ferramentas Python prontas (UnityPy)

### 🔄 Em andamento:
- Extração dos bundles Darkside v4.3

---

## 🎯 Estratégia de Desenvolvimento

### Fase 1: Análise e Engenharia Reversa (ATUAL)
- [x] Download Darkside v4.3
- [ ] Extrair bundles Darkside com UnityPy
- [ ] Mapear estrutura de UXML/USS
- [ ] Identificar assets modificáveis (estilos, cores, tipografia)
- [ ] Documentar sistema de UI do FM26

**Output**: Documentação técnica completa da estrutura de skins FM26

### Fase 2: Prototipagem
- [ ] Criar modificações básicas (cores, fontes)
- [ ] Testar pipeline de bundle → modificação → recompilação
- [ ] Validar funcionamento no FM26
- [ ] Iterar até obter processo estável

**Output**: Pipeline funcional de modificação de bundles

### Fase 3: Design Brasileiro
- [ ] Definir paleta de cores (verde/amarelo ou moderna?)
- [ ] Escolher tipografia adequada
- [ ] Criar elementos visuais brasileiros sutis
- [ ] Projetar sistema de atributos em bolinhas coloridas

**Output**: Documento de design visual

### Fase 4: Implementação Core
- [ ] Modificar estilos base (ui-styles_assets_default.bundle)
- [ ] Customizar cores e tipografia
- [ ] Implementar visualização de atributos
- [ ] Ajustar painéis principais (jogador, time, partida)

**Output**: Skin funcional v0.1

### Fase 5: Refinamento
- [ ] Testes extensivos no jogo
- [ ] Ajustes de legibilidade
- [ ] Polimento visual
- [ ] Otimização de performance

**Output**: Skin v1.0 pronta para release

### Fase 6: Distribuição
- [ ] Documentação de instalação
- [ ] Screenshots/vídeos demonstrativos
- [ ] Publicação em fóruns FM
- [ ] GitHub release

---

## 🛠️ Ferramentas Necessárias

### Já disponíveis:
- ✅ UnityPy (extração de bundles)
- ✅ Python 3 + scripts customizados
- ✅ Assets de referência (MrTini, Darkside)

### A instalar:
- [ ] AssetBundle Browser (Unity Editor)
- [ ] Gimp/Photoshop (edição de texturas)
- [ ] Editor de texto avançado (VS Code)

---

## 📊 Arquivos-Chave dos Bundles

Com base nos bundles Darkside:

| Bundle | Tamanho | Conteúdo | Prioridade |
|--------|---------|----------|------------|
| `ui-styles_assets_default.bundle` | 746 KB | **Estilos principais** | 🔴 CRÍTICO |
| `ui-styles_assets_match.bundle` | 18 KB | Estilos de partida | 🟡 Médio |
| `ui-fonts_assets_production.bundle` | 19 MB | Fontes | 🟡 Médio |
| `ui-backgrounds_assets_common.bundle` | 129 MB | Fundos | 🟢 Baixo |
| `ui-tableviews_assets_all.bundle` | 191 KB | **Tabelas/listas** | 🔴 CRÍTICO |
| `ui-widgets_assets_all.bundle` | 1.5 MB | **Widgets UI** | 🔴 CRÍTICO |
| `ui-textures_assets_all.bundle` | 4.8 MB | Texturas | 🟡 Médio |
| `ui-iconspriteatlases_assets_2x.bundle` | 5 MB | Ícones | 🟢 Baixo |

**Foco inicial**: Bundles CRÍTICOS (estilos, tabelas, widgets)

---

## 📝 Próximos Passos Imediatos

1. ✅ Baixar Darkside v4.3
2. 🔄 Extrair bundles prioritários
3. ⏳ Analisar UXML/USS extraídos
4. ⏳ Mapear sistema de cores/estilos
5. ⏳ Criar documento técnico de referência

---

## 🎨 Conceito Visual (Rascunho)

### Opção 1: Brasileiro Tradicional
- Verde/Amarelo sutis
- Elementos tropicais discretos
- Tipografia clean e moderna

### Opção 2: Moderno Brasileiro
- Paleta neutra (cinza/branco)
- Acentos em verde Brasil (#009c3b)
- Minimalista, foco em dados

### Opção 3: Profissional CBF
- Cores oficiais CBF
- Visual corporativo
- Inspirado em transmissões de TV

**Decisão**: Aguardar análise técnica antes de definir

---

## 📚 Referências

- **FM-Base**: https://fm-base.co.uk/resources/categories/skins.3/
- **FMScout Skins**: https://www.fmscout.com/c-fm26-skins.html
- **Darkside GitHub**: (se houver)
- **UnityPy Docs**: https://github.com/K0lb3/UnityPy

---

## ⚠️ Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Bundles corrompidos após modificação | Alto | Backup sempre, testar pequenas mudanças |
| Incompatibilidade com atualizações FM | Médio | Versionamento claro, testar em cada patch |
| Performance degradada | Baixo | Otimizar texturas, evitar assets pesados |
| DMCA/copyright | Baixo | Usar apenas modificações, não redistribuir assets originais |

---

## 📅 Timeline Estimado

- **Fase 1**: 2-3 dias (análise técnica)
- **Fase 2**: 2-3 dias (prototipagem)
- **Fase 3**: 1-2 dias (design)
- **Fase 4**: 5-7 dias (implementação)
- **Fase 5**: 3-5 dias (refinamento)
- **Fase 6**: 1-2 dias (distribuição)

**Total**: ~3 semanas (trabalho part-time)

---

_Última atualização: 2026-03-10 18:48 GMT-3_
