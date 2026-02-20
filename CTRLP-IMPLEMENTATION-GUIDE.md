# Guia de Implementação: Ctrl+P no FM26

## Objetivo
Restaurar a funcionalidade de exportação de jogadores (Ctrl+P) no Football Manager 26.

---

## Passo 1: Preparar Ferramentas no Windows

### 1.1 Baixar Il2CppDumper
```
https://github.com/Perfare/Il2CppDumper/releases
```
Baixe `Il2CppDumper-win-x64-v6.x.x.zip`

### 1.2 Baixar dnSpy
```
https://github.com/dnSpy/dnSpy/releases
```
Baixe `dnSpy-net-win64.zip`

### 1.3 Localizar arquivos do FM26
```
C:\Program Files (x86)\Steam\steamapps\common\Football Manager 2026\fm_data\data\
```
Arquivos necessários:
- `game_plugin.dll` (ou similar - o maior arquivo .dll)
- `global-metadata.dat` (na pasta `il2cpp_data/Metadata/`)

---

## Passo 2: Extrair Assemblies IL2CPP

### 2.1 Executar Il2CppDumper
1. Abra `Il2CppDumper.exe`
2. Selecione `game_plugin.dll` (ou `GameAssembly.dll`)
3. Selecione `global-metadata.dat`
4. Escolha o modo "Auto" ou "Manual"
5. Aguarde a extração

### 2.2 Arquivos gerados
- `dump.cs` - Classes e métodos em C#
- `script.json` - Metadados estruturados
- `dll/` - DLLs extraídas

---

## Passo 3: Localizar ExportCurrentItemToBinding

### 3.1 Buscar no dump.cs
```bash
grep -n "ExportCurrentItemToBinding" dump.cs
grep -n "PrintScreen\|ctrlKeyboard" dump.cs
```

### 3.2 Buscar com dnSpy
1. Abra `dnSpy.exe`
2. Carregue `FM.UI.dll` da pasta `dll/`
3. Busca (Ctrl+Shift+K): `ExportCurrentItem`
4. Anote:
   - Namespace
   - Classe
   - Assinatura do método
   - Offset no assembly

---

## Passo 4: Criar DLL Injector

### 4.1 Estrutura do projeto
```
FM26-CtrlP-Patch/
├── injector/
│   ├── main.cpp
│   └── dllmain.cpp
├── patch/
│   └── hook.cpp
└── build.bat
```

### 4.2 Template do Injector (C++)
```cpp
// dllmain.cpp
#include <windows.h>
#include <dinput.h>

// Endereço da função ExportCurrentItemToBinding
// Será preenchido após análise
typedef void (*ExportFunc)(void* context);
ExportFunc g_ExportFunction = nullptr;

// Hook de teclado
HHOOK g_Hook = nullptr;

LRESULT CALLBACK KeyboardProc(int nCode, WPARAM wParam, LPARAM lParam) {
    if (nCode >= 0) {
        KBDLLHOOKSTRUCT* kbd = (KBDLLHOOKSTRUCT*)lParam;
        
        // Ctrl+P detectado
        if (wParam == WM_KEYDOWN && kbd->vkCode == 'P') {
            if (GetAsyncKeyState(VK_CONTROL) & 0x8000) {
                // Chamar função de exportação
                if (g_ExportFunction) {
                    g_ExportFunction(nullptr);
                }
                return 1; // Bloquear propagação
            }
        }
    }
    return CallNextHookEx(g_Hook, nCode, wParam, lParam);
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID reserved) {
    switch (reason) {
        case DLL_PROCESS_ATTACH:
            // 1. Encontrar endereço da função
            // g_ExportFunction = (ExportFunc)FindExportFunction();
            
            // 2. Instalar hook de teclado
            g_Hook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardProc, hModule, 0);
            break;
            
        case DLL_PROCESS_DETACH:
            if (g_Hook) UnhookWindowsHookEx(g_Hook);
            break;
    }
    return TRUE;
}
```

### 4.3 Método de injeção
**Opção A: DLL Proxy**
1. Criar `version.dll` ou `winmm.dll`
2. Colocar na pasta do jogo
3. Jogo carrega automaticamente

**Opção B: Injector externo**
1. Usar `ProcessHacker` ou `Xenos Injector`
2. Injetar após o jogo iniciar

---

## Passo 5: Testar

### 5.1 Checklist
- [ ] DLL compilada
- [ ] Injector funcionando
- [ ] Hook de teclado ativo
- [ ] Ctrl+P chama a função

### 5.2 Debug
- Usar `OutputDebugString` para log
- Verificar com `DebugView` do Sysinternals

---

## Próximos Passos

1. **Executar Il2CppDumper** na sua máquina Windows
2. **Mandar o arquivo dump.cs** para eu analisar
3. **Identificar a assinatura exata** da função
4. **Calcular o offset** no assembly
5. **Compilar o injector**

---

## Contato

Atualizado: 2026-02-20
Autor: Koda (OpenClaw Assistant)
