---
name: inserir-telecurso
description: Skill para adicionar e mapear corretamente novos vídeos do projeto VintesetFM Telecurso, padronizando thumbnails, fontes originais e categorias visuais.
---

# Inserção de Novos Vídeos no Telecurso (VintesetFM)

Sempre que o usuário enviar novos links de vídeos do YouTube (da conta oficial `Telecurso27`) para serem adicionados, você MANTÉM e RESPEITA as seguintes regras de negócio estipuladas da plataforma:

## Regras e Estratégias de UI (Frontend - `Telecurso.jsx`)
O frontend já foi refatorado para ser dinâmico e inteligente. **Não altere sua estrutura a não ser que o usuário solicite expressamente**:
- **Thumbnails**: A thumb da vitrine SEMPRE vai extrair automaticamente a imagem em HQ do `uploadedYoutubeUrl` (o vídeo hospedado pelo usuário). Isso garante que estamos mostrando a "lousa" gerada pelo NotebookLM, e nunca usamos a thumb do vídeo original (por direitos autorais).
- **Player Blindado**: O código mascara via CSS absoluto o cabeçalho do iframe do YouTube, escondendo Nome do Canal, Avatar e Avatar do YouTube para não dar aspecto de "gambiarra" ou expor as contas raiz.
- **Identificação da Fonte Mestra**: O botão lateral é inteligente porque detecta propriedades da origem (`originalYoutubeUrl`). Se for vídeo do YT, ele mostra "Assistir Vídeo Original" 📺. Se foi um portal ou blog de texto, mostra "Ler Artigo Original" 🌐. O termo de UI adota o "Menos é Máis" (livre de textos redundantes).

## Fluxo de Inserção de Novo Vídeo

Durante a ingestão no arquivo banco de dados (`index.json`):

1. **Correspondência Exata**: Extraia o nome exato dos links do YouTube fornecidos pelo usuário lendo a tag `<title>` das páginas ou use a ordem sequencial caso eles informem o ID/nome. Verifique incisivamente se o vídeo (no Youtube) de fato corrobora com o título, para evitar a falha de puxar o NotebookLM / Assunto errado.
2. **Atualização do `uploadedYoutubeUrl`**: Povoar a respectiva chave `uploadedYoutubeUrl` no `index.json` apenas com links gerados da conta oficial.
3. **Mapeamento da Origem Real (`originalYoutubeUrl`)**: Garanta que o objeto original possua no `index.json` a chave `originalYoutubeUrl` mapeada com exatidão da fonte que alimentou o "Audio Overview / Masterclass" — seja link do YT da gringa ou URL de fórum gringo. Use os scripts de extração do Google NotebookLM API se necessário.
4. **Categorias Inteligentes e Não Hardcodadas**: O frontend (`Telecurso.jsx`: `getCategory()`) classificará o card na UI por padrão:
    - `Tática` (exclusivo para títulos envolvendo "Relacionismo")
    - `Tutorial` (envolvendo palavras como "Guia", "Tutorial", "Dica")
    - `Análise Dinâmica` (para o restante de perfis de jogadores, mecânicas, análises de times como Anfield, etc)
    - -> Deixe o frontend cuidar da categoria lendo o `item.titulo`.

## Passos para Executar
Ao receber o comando:
1. Receba os links.
2. Através de python, bata o título do YouTube com a chave `titulo` correta dentro do `index.json`.
3. Popule o `uploadedYoutubeUrl`.
4. Valide se há inconsistências de thumbnail/titles consultando de volta com o usuário se parecer incorreto.
5. Inicie o Server / React.
