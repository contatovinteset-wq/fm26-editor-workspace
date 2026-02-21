# Instalação do FM26 Ctrl+P Export Mod

## Pré-requisitos

1. **BepInEx-IL2CPP** instalado no Football Manager 2026
   - Download: https://github.com/BepInEx/BepInEx/releases
   - Versão recomendada: 6.0.0-pre.1 ou superior (IL2CPP)

2. **.NET 6.0 SDK** (para compilar o mod)
   - Download: https://dotnet.microsoft.com/download

## Instalação do BepInEx

1. Baixe `BepInEx-Unity-IL2CPP-win-x64-6.0.0-pre.1.zip` (ou versão similar)
2. Extraia o conteúdo na pasta raiz do FM26
3. Execute o jogo uma vez para gerar os arquivos de configuração
4. Feche o jogo

## Instalação do Mod

### Opção 1: Baixar DLL pronta (se disponível)
1. Copie `FM26ExportMod.dll` para:
   ```
   FM26\BepInEx\plugins\
   ```

### Opção 2: Compilar do código-fonte

1. Clone/baixe este repositório
2. Abra terminal/prompt na pasta do projeto
3. Execute:
   ```bash
   # Linux/Mac
   ./build.sh
   
   # Windows
   build.bat
   ```
4. Copie o arquivo gerado:
   ```
   output/FM26ExportMod.dll
   ```
   Para:
   ```
   FM26\BepInEx\plugins\
   ```

## Teste

1. Inicie o Football Manager 2026
2. Vá para qualquer tela com uma tabela (ex: elenco, jogadores)
3. Pressione **Ctrl+P**
4. Verifique se o arquivo foi exportado para:
   ```
   Documentos\Sports Interactive\Football Manager 2026\
   ```

## Desinstalação

Remova o arquivo:
```
FM26\BepInEx\plugins\FM26ExportMod.dll
```

## Troubleshooting

### O mod não carrega
- Verifique se o BepInEx está instalado corretamente
- Confira o arquivo `BepInEx\LogOutput.log` por erros

### Ctrl+P não funciona
- Verifique se o foco está em uma tabela/carousel
- Algumas telas podem não ter a funcionalidade de exportação ativa

### Erro de compilação
- Certifique-se de ter o .NET 6.0 SDK instalado
- Atualize as referências de DLL se necessário
