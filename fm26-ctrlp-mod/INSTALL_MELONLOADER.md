# FM26 Ctrl+P Export Mod - Guia de Instalação (MelonLoader)

## Problema com BepInEx

O FM26 usa **Unity 6** (versão 6000.0.52f1) com **metadata version 31**. O BepInEx atual (mesmo a versão 6 preview) não suporta essa versão do Unity.

**Erro:**
```
Unsupported metadata version found! We support 23-29, got 31
```

## Solução: MelonLoader

MelonLoader tem suporte mais recente para Unity 6.

---

## Passo 1: Instalar MelonLoader

1. Baixe a versão mais recente:
   ```
   https://github.com/LavaGang/MelonLoader/releases
   ```
   
   Procure por: `MelonLoader.x64.zip` ou `MelonLoader.Installer.exe`

2. **Opção A - Usando o Installer (recomendado):**
   - Execute `MelonLoader.Installer.exe`
   - Selecione o executável do FM26: `FM26.exe`
   - Clique em Install

3. **Opção B - Manual:**
   - Extraia o conteúdo do ZIP na raiz do FM26
   - Estrutura:
     ```
     Football Manager 26/
     ├── FM26.exe
     ├── MelonLoader/
     ├── Mods/
     ├── UserLibs/
     ├── version.dll
     └── DoORqSToP.dll
     ```

4. **Teste:**
   - Abra o jogo
   - Deve aparecer um console do MelonLoader
   - Feche o jogo

---

## Passo 2: Compilar o Mod

### Requisitos
- .NET 6.0 SDK ou superior

### Compilar
```bash
cd fm26-ctrlp-mod
dotnet restore FM26ExportMod_MelonLoader.csproj
dotnet build FM26ExportMod_MelonLoader.csproj -c Release
```

---

## Passo 3: Instalar o Mod

1. Copie o DLL compilado:
   ```
   De: bin/Release/net6.0/FM26ExportMod.dll
   Para: [Pasta do FM26]/Mods/FM26ExportMod.dll
   ```

2. Certifique-se que a pasta `Mods/` existe

---

## Passo 4: Testar

1. Abra o jogo
2. O MelonLoader deve carregar o mod
3. No console, deve aparecer:
   ```
   [FM26ExportMod] FM26 Ctrl+P Export Mod
   [FM26ExportMod] Versão: 1.0.0 (MelonLoader)
   ```
4. Em jogo, selecione um jogador/tabela
5. Pressione **Ctrl+P**
6. Verifique o console para mensagens de sucesso/erro

---

## Troubleshooting

### "MelonLoader não abre"
- Verifique se tem o Visual C++ Redistributable instalado
- Baixe: https://aka.ms/vs/17/release/vc_redist.x64.exe

### "Mod não carrega"
- Verifique se o DLL está na pasta `Mods/`
- Verifique o console do MelonLoader para erros

### "Ctrl+P não funciona"
- Verifique o console para mensagens de erro
- O mod precisa encontrar o tipo `SICarousel` no jogo
- Pode ser que a estrutura do FM26 tenha mudado

---

## Logs

Os logs do MelonLoader ficam em:
```
[Pasta do FM26]/MelonLoader/latest.log
```

---

## Estrutura Final

```
Football Manager 26/
├── FM26.exe
├── version.dll
├── DoORqSToP.dll
├── MelonLoader/
│   └── [arquivos do MelonLoader]
├── Mods/
│   └── FM26ExportMod.dll  ← NOSSO MOD
├── UserLibs/
└── [outros arquivos do jogo]
```
