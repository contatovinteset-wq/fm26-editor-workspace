# Como Criar Skin Minimalista Brasil - FM Skin Builder

## Visão Geral

O **FM Skin Builder** é a ferramenta oficial para criar skins do FM26. Usa CSS para definir cores, ao invés de XML do FM24.

## Fluxo de Trabalho Correto

### 1. Instalar FM Skin Builder

**Download:** https://fmskinbuilder.com/downloads

- Windows: `.exe` installer
- macOS: `.dmg` 
- Linux: `.AppImage`

### 2. Usar Darkside como Base

A documentação recomenda usar a **Darkside Neutral Theme** como template inicial:
https://sortitoutsi.net/content/75885/darkside-neutral-theme-blue-and-kaiserslautern-legend-edition

### 3. Criar Projeto

1. Abrir FM Skin Builder
2. File > New Skin
3. Nome: "Minimalista Brasil"
4. Template: **Color Override** (para começar com cores)
5. Salvar em pasta do projeto

### 4. Editar Cores via CSS

Abrir `colours/base.uss` e definir a paleta:

```css
:root {
  /* Base - Cinza Escuro */
  --background-primary: #1A1A1F;
  --background-secondary: #2D2D35;
  --background-tertiary: #404048;
  
  /* Acentos - Azul */
  --primary: #0066CC;
  --primary-light: #3399FF;
  --primary-dark: #004C99;
  
  /* Destaques - Amarelo */
  --accent: #FFD700;
  --accent-light: #FFE44D;
  --accent-dark: #CCB000;
  
  /* Texto */
  --text-primary: #FFFFFF;
  --text-secondary: #B0B0B8;
  --text-muted: #6B6B75;
}
```

### 5. Build e Instalar

1. Clicar **Build** (F5)
2. Clicar **Install to Game**
3. Abrir FM26
4. Preferences > Interface > Selecionar "Minimalista Brasil"
5. Confirm

### 6. Customizar Backgrounds (Opcional)

Para adicionar backgrounds customizados (como a foto do Ronaldo):

1. Template: **Full Theme** (inclui pasta de assets)
2. Colocar imagens em `assets/backgrounds/`
3. Referenciar no CSS

## Diferenças FM24 vs FM26

| Aspecto | FM24 (XML) | FM26 (CSS) |
|---------|-----------|-----------|
| Formato | `<colour name="x">` | `--variable-name` |
| Cores | `value="RRGGBB"` | `#RRGGBB` |
| Arquivos | `.xml` | `.uss` ou `.css` |

## Próximos Passos

1. Baixar FM Skin Builder
2. Baixar Darkside Neutral Theme
3. Criar projeto "Minimalista Brasil"
4. Aplicar paleta CSS
5. Build e testar no FM26
6. Ajustar conforme feedback

## Recursos

- Documentação: https://fmskinbuilder.com/docs
- Downloads: https://fmskinbuilder.com/downloads
- Darkside Theme: https://sortitoutsi.net/content/75885/darkside-neutral-theme-blue-and-kaiserslautern-legend-edition
- CSS Variables Reference: https://fmskinbuilder.com/docs/reference/css-variables
