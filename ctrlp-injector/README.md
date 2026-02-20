# FM26 Ctrl+P Injector - Guia de Uso

## Compilação

### Opção 1: Visual Studio (Recomendado)
1. Crie um projeto "Dynamic-Link Library (DLL)" em C++
2. Adicione `injector.cpp` ao projeto
3. Compile em Release x64
4. Renomeie a saída para `version.dll`

### Opção 2: MinGW
```bash
x86_64-w64-mingw32-g++ -shared -o version.dll injector.cpp -luser32 -static
```

---

## Instalação

1. Copie `version.dll` para a pasta do jogo:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Football Manager 2026\
   ```

2. O jogo carregará automaticamente a DLL ao iniciar

---

## Debug

1. Baixe [DebugView](https://docs.microsoft.com/en-us/sysinternals/downloads/debugview) do Sysinternals

2. Execute como Administrador

3. Inicie o FM26

4. Procure por mensagens `[FM26-CtrlP]` no DebugView

---

## Encontrando o Offset da Função

O injector precisa do offset correto de `ExportCurrentItemToBinding` no `GameAssembly.dll`.

### Método 1: Cheat Engine

1. Abra o FM26
2. Abra Cheat Engine
3. Selecione o processo `fm.exe`
4. Busque por strings relacionadas a "Export"
5. Encontre referências à função
6. Calcule o offset: `endereço - base_do_GameAssembly.dll`

### Método 2: x64dbg

1. Abra x64dbg
2. Anexe ao processo `fm.exe`
3. Busque por strings "ExportCurrentItem" ou "PrintScreen"
4. Encontre a função
5. Anote o endereço e calcule o offset

### Método 3: Il2CppDumper

1. Rode Il2CppDumper com:
   - `GameAssembly.dll`
   - `global-metadata.dat`

2. Abra `dump.cs` e busque por `ExportCurrentItemToBinding`

3. Use o script `script.json` para mapear offsets

---

## Editando o Offset

No arquivo `injector.cpp`, altere:

```cpp
DWORD g_ExportOffset = 0xABCDEF; // Substitua pelo offset real
```

Recompile e teste novamente.

---

## Teste

1. Inicie o FM26
2. Vá para uma tela de jogadores
3. Selecione jogadores
4. Pressione Ctrl+P
5. Verifique o DebugView para logs

---

## Problemas Comuns

### DLL não carrega
- Verifique se o nome é `version.dll` ou `winmm.dll`
- Tente outro nome de DLL do sistema

### Hook não funciona
- Verifique se o jogo está rodando como Administrador
- Tente executar o injector com privilégios elevados

### Função não encontrada
- O offset está incorreto
- Use x64dbg para encontrar o endereço correto

---

## Próximos Passos

1. Determinar o offset exato da função
2. Testar a chamada da função
3. Ajustar parâmetros se necessário
4. Documentar a solução final

---

## Arquivos

- `injector.cpp` - Código fonte do injector
- `version.dll` - DLL compilada (após compilar)

---

**Autor:** Koda (OpenClaw Assistant)
**Data:** 2026-02-20
