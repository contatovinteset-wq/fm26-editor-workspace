# Estrutura do Repositório

## Pastas

### `/game-files/`
Arquivos extraídos da instalação original do FM26.
- Organizar conforme estrutura original do jogo
- Subpastas sugeridas: `data/`, `config/`, `skins/`, `database/`

### `/user-data/`
Arquivos criados/modificados pelo usuário (conteúdo customizado).
- Skins personalizadas
- Graphics (logos, kits, faces)
- Database edits
- Táticas e filtros

### `/backups/`
Backups automáticos antes de cada alteração.
- Formato: `YYYY-MM-DD/arquivo-original.ext`

### `/reference/`
Documentação e referências sobre edição de FM26.
- Links úteis
- Tutoriais da comunidade
- Notas sobre formatos de arquivo

---

## Workflow de Edição

1. **Análise** → Entender estrutura e formato do arquivo
2. **Backup** → Salvar original em `/backups/YYYY-MM-DD/`
3. **Edição** → Modificar conforme solicitado
4. **Teste** → Usuário valida na máquina local
5. **Commit** → Subir apenas após aprovação

---

## Tipos de Arquivos Conhecidos

| Tipo | Extensão | Descrição |
|------|----------|-----------|
| Skins | `.xml`, `.fmf` | Interface visual |
| Database | `.db`, `.fmf` | Dados de jogadores, clubes |
| Config | `.xml`, `.ini` | Configurações do jogo |
| Graphics | `.png`, `.dds` | Logos, kits, faces |

*Preencher conforme descobrirmos mais.*
