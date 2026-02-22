# Skin FM26 - Projeto de Personalização

## Objetivo

Criar uma skin personalizada para Football Manager 2026 inspirada nas skins **MrTini** e **Mustermann** (FM24), com foco em:

1. **Atributos como bolinhas coloridas** (como Mustermann FM24)
2. **UI mais limpa e amigável**
3. **Potencializar dados analíticos do jogo**

---

## Features Desejadas

### Atributos em Círculos Coloridos

Em vez de mostrar "Aceleração = 16", exibir um círculo colorido representando a faixa:

| Faixa | Cor | Significado |
|-------|-----|-------------|
| 1-5 | Vermelho | Muito ruim |
| 6-9 | Laranja | Ruim |
| 10-13 | Amarelo | Médio |
| 14-16 | Verde | Bom |
| 17-20 | Azul/Cyan | Excelente |

### UI Melhorada

- Layout mais limpo e moderno
- Melhor hierarquia visual
- Cores mais suaves
- Tipografia otimizada

---

## Estrutura de Pastas de uma Skin FM

```
skin-fm26/
├── panels/           # Painéis da interface (XML)
│   ├── player/       # Painéis de jogador (atributos, perfil, etc)
│   ├── team/         # Painéis de time
│   ├── match/        # Painéis de partida
│   ├── club/         # Painéis de clube
│   └── ...
├── settings/         # Configurações de cores e fontes
├── graphics/         # Imagens e ícones
├── fonts/            # Fontes personalizadas
├── classes/          # Classes CSS-like
├── styles/           # Estilos visuais
├── properties/       # Propriedades de objetos
├── sections/         # Seções da UI
└── skin_config.xml   # Configuração principal da skin
```

---

## Arquivos Chave para Modificar Atributos

### Painel de Atributos do Jogador

Localização: `panels/player/player attributes panel profile.xml`

Este arquivo controla como os atributos são exibidos no perfil do jogador.

### Estrutura XML de Atributos

```xml
<!-- Widget de valor do atributo -->
<record index="2" id="val" sort_disabled="true" column_alignment="centre" right="8">
  <record id="widget_info" class="attribute_label" 
          alignment="centre,can_scale" 
          colour="white" 
          use_attribute_colour_as_bg="true" 
          appearance="boxes/custom/attributes/paper"/>
</record>
```

### Cores dos Atributos

Definidas em: `settings/fm-color settings.xml` ou `_COLOURS/`

---

## Referências

### Skins de Referência

1. **MrTini Skin (FM24)** - Disponibilizada via Mega pelo usuário
2. **Mustermann Skin (FM24)** - Atributos em bolinhas coloridas
3. **FM24Light** - https://github.com/nyurch/FM24Light
   - Estrutura bem organizada
   - Atualizado recentemente (Fev 2026)

### Documentação FM

- Painéis são definidos em XML
- Cores podem ser customizadas via arquivos de settings
- A skin precisa estar em: `Documents/Sports Interactive/Football Manager 2026/skins/`

---

## Próximos Passos

1. [ ] Extrair e analisar a skin MrTini do Mega
2. [ ] Estudar a estrutura da skin Mustermann para atributos em bolinhas
3. [ ] Criar estrutura base da skin FM26
4. [ ] Implementar visualização de atributos em círculos
5. [ ] Testar no jogo
6. [ ] Documentar instalação

---

## Instalação da Skin

1. Copiar pasta da skin para: `Documents/Sports Interactive/Football Manager 2026/skins/`
2. No jogo: Preferences > Interface > Clear Cache
3. Selecionar a skin em Preferences > Interface > Skin
4. Aplicar mudanças

---

## Notas Técnicas

- FM26 usa a mesma estrutura de XML do FM24/FM25
- Os painéis de atributos ficam em `panels/player/`
- Cores são definidas via propriedades XML
- É possível usar imagens PNG para os círculos coloridos
