---
name: fm26_player_database_export
description: >
  Use this skill whenever the user asks to extract, export, read or access
  the Player Database (lista de jogadores, player data, player attributes,
  CA, PA, stats) from Football Manager 2026 (FM26) to a CSV, Excel or any
  structured file. Covers IL2CPP reverse engineering, BepInEx plugin approach,
  pre-game .dat file approach, and in-game memory approach via FMRTE.
emoji: ⚽
tags: [fm26, football-manager, game-modding, player-database, csv-export, il2cpp, bepinex]
---

# FM26 Player Database Export

## Critical Context — FM26 Architecture

Football Manager 2026 runs on the **Unity engine** compiled with **IL2CPP** backend.
This means:
- The game code is NOT a standard .NET assembly readable via reflection
- All C# source code was compiled into a **native binary** called `GameAssembly.dll`
- Standard .NET plugins, Harmony patches or Mono-based BepInEx builds will NOT work
- You MUST use the **IL2CPP build of BepInEx** and IL2CPP-aware interop libraries
- Any plugin that tries `Assembly.GetTypes()` or standard reflection will find nothing

This is the root cause of failures when trying generic Unity plugin approaches.

---

## Game File Locations (Windows)

```
# Executable and core binaries
C:\Program Files (x86)\Steam\steamapps\common\Football Manager 2026\
  ├── Football Manager 2026.exe
  ├── GameAssembly.dll                          ← compiled IL2CPP native code
  └── Football Manager 2026_Data\
        └── il2cpp_data\
              └── Metadata\
                    └── global-metadata.dat     ← IL2CPP metadata (class/field names)

# Pre-game editor database (offline, no game running needed)
C:\Users\<USER>\Documents\Sports Interactive\Football Manager 2026\
  └── editor data\
        └── *.fmf  (or *.dat depending on version)

# Save files (in-game data, requires game running to read)
C:\Users\<USER>\Documents\Sports Interactive\Football Manager 2026\games\
```

---

## Method 1 — Dump the IL2CPP Classes (REQUIRED FIRST STEP for all methods)

This reveals the exact class names and memory offsets for the Player Database.

### Tools needed
- **Il2CppDumper** → https://github.com/Perfare/Il2CppDumper (releases page)

### Steps
```bash
# 1. Download Il2CppDumper and run it
Il2CppDumper.exe "GameAssembly.dll" "global-metadata.dat" "output_folder"

# 2. This generates in output_folder/:
#    - dump.cs          ← MOST IMPORTANT: all classes with field offsets
#    - DummyDll/        ← fake DLLs you can open in dnSpy/ILSpy
#    - script.json      ← for Ghidra/IDA Pro analysis
#    - stringliteral.json

# 3. Open dump.cs and search for these class names (use Ctrl+F):
#    - "PlayerData"
#    - "PersonRecord" 
#    - "PlayerRecord"
#    - "PersonInstance"
#    - "PlayerInstance"
#    - "DatabaseManager"
#    - "PlayerManager"
#    - "PersonManager"
#    - "SIEngine"
#    - "PlayerCache"
#    - "FMDatabase"
```

### What to look for in dump.cs
```csharp
// Example of what you will find - actual field offsets
public class PlayerData // TypeDefIndex: 4821
{
    // Fields
    public int currentAbility; // 0x10
    public int potentialAbility; // 0x14
    public PersonRecord person; // 0x18
    public int age; // 0x1C
    // ...
}
```
The `// 0x10` comments are the **memory offsets** — critical for reading data at runtime.

---

## Method 2 — BepInEx IL2CPP Plugin (In-Game, Runtime Export)

### Prerequisites
- BepInEx 6.x **IL2CPP build** (NOT the standard Mono build)
  → https://github.com/BepInEx/BepInEx/releases (look for `BepInEx_UnityIL2CPP_x64`)
- UnityExplorer for IL2CPP (to inspect live objects)
  → https://github.com/sinai-dev/UnityExplorer

### Installation
```bash
# 1. Extract BepInEx IL2CPP into FM26 root folder
# Result:
# Football Manager 2026\
#   ├── BepInEx\
#   │     ├── plugins\       ← your .dll plugin goes here
#   │     └── config\
#   └── doorstop_config.ini  ← auto-created by BepInEx

# 2. Run FM26 once to let BepInEx generate its folder structure
# Check BepInEx\LogOutput.log for "BepInEx loaded" confirmation
```

### Plugin Template to Export Players (C#)
```csharp
using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using System.IO;
using System.Text;
using UnhollowerRuntimeLib;
using UnityEngine;

[BepInPlugin("com.yourname.fm26playerexport", "FM26 Player Exporter", "1.0.0")]
public class PlayerExporterPlugin : BasePlugin
{
    public override void Load()
    {
        Log.LogInfo("FM26 Player Exporter loaded");

        // Add a keyboard hook to trigger export (F9 key)
        AddComponent<PlayerExporterBehaviour>();
    }
}

public class PlayerExporterBehaviour : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ExportPlayers();
        }
    }

    void ExportPlayers()
    {
        // STEP 1: Find the game manager — use the class names found in dump.cs
        // Replace "PlayerManager" with the actual class name from your dump.cs
        var managerType = Il2CppType.From(typeof(PlayerManager)); // adjust class name
        var manager = GameObject.FindObjectOfType(managerType);

        if (manager == null)
        {
            Debug.LogError("PlayerManager not found — check class name in dump.cs");
            return;
        }

        // STEP 2: Access the player list — field name from dump.cs
        // Common field names: players, allPlayers, playerList, database
        var playerList = manager.players; // adjust field name from dump.cs

        var sb = new StringBuilder();
        sb.AppendLine("Name,Age,CurrentAbility,PotentialAbility,Club,Nationality");

        foreach (var player in playerList)
        {
            // STEP 3: Read fields using names from dump.cs
            sb.AppendLine($"{player.name},{player.age},{player.currentAbility},{player.potentialAbility},{player.club},{player.nationality}");
        }

        string outputPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
            "Sports Interactive", "Football Manager 2026", "player_export.csv"
        );

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"Exported to: {outputPath}");
    }
}
```

**IMPORTANT**: Before writing this plugin, always check dump.cs first for:
1. The exact class name of the player/person manager
2. The exact field names and their types
3. Whether the list is `Il2CppSystem.Collections.Generic.List<T>` or a native array

---

## Method 3 — Pre-Game Database (.dat / .fmf files, No Game Running)

This reads the offline editor database directly from disk.

### Open-Source Reference
The **FMM26 Pre-Game Database Editor** reads these files and is open source:
→ Search GitHub for: `nyongrand/fmm-editor`
→ The `people` table in the editor corresponds to the Player Database

### File Format Notes
- FM26 database files are **custom binary format** (not SQLite, not XML)
- They contain compressed/encoded records for People, Clubs, Competitions, Nations
- The open-source editor source code is the best reference for parsing offsets
- Look for the `PeopleRepository` or `PersonReader` class in that codebase

### Python skeleton to parse once you find offsets
```python
import struct
import csv

def read_fm26_database(dat_file_path, output_csv):
    with open(dat_file_path, "rb") as f:
        data = f.read()

    # Offsets below are PLACEHOLDERS — get real offsets from FMM26 editor source
    HEADER_SIZE = 0x40        # adjust after reading source
    RECORD_SIZE = 0x200       # adjust based on person record struct
    NAME_OFFSET = 0x08        # offset within each record
    CA_OFFSET   = 0x80        # current ability offset
    PA_OFFSET   = 0x84        # potential ability offset

    records = []
    pos = HEADER_SIZE
    while pos + RECORD_SIZE <= len(data):
        name = data[pos + NAME_OFFSET:pos + NAME_OFFSET + 64].decode("utf-8", errors="ignore").rstrip("\x00")
        ca   = struct.unpack_from("<h", data, pos + CA_OFFSET)[0]
        pa   = struct.unpack_from("<h", data, pos + PA_OFFSET)[0]
        records.append({"name": name, "ca": ca, "pa": pa})
        pos += RECORD_SIZE

    with open(output_csv, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=["name", "ca", "pa"])
        w.writeheader()
        w.writerows(records)

    print(f"Exported {len(records)} records to {output_csv}")

# Usage:
# read_fm26_database(
#     r"C:\Users\USER\Documents\Sports Interactive\Football Manager 2026\editor data\fm2026.fmf",
#     "players.csv"
# )
```

---

## Method 4 — Memory Reading via ReadProcessMemory (No BepInEx)

For external tools (like FMRTE does), read the game process memory directly.

```python
# Requires: pip install pymem
import pymem
import pymem.process
import csv

def export_via_memory():
    pm = pymem.Pymem("fm.exe")  # attach to running FM26 process

    # 1. Get base address of GameAssembly.dll
    module = pymem.process.module_from_name(pm.process_handle, "GameAssembly.dll")
    base_addr = module.lpBaseOfDll

    # 2. Use pointer chain from dump.cs to navigate to player list
    # These offsets MUST come from Il2CppDumper dump.cs analysis
    # Example pointer chain (PLACEHOLDER - get real values from dump.cs):
    player_manager_offset = 0x05D3B4A0   # static field offset from dump.cs
    player_list_offset    = 0x18          # field offset within PlayerManager
    player_count_offset   = 0x18          # List<T>.size offset (standard IL2CPP)
    player_array_offset   = 0x10          # List<T>.items offset (standard IL2CPP)

    # Read static instance pointer
    manager_ptr = pm.read_longlong(base_addr + player_manager_offset)
    list_ptr    = pm.read_longlong(manager_ptr + player_list_offset)
    count       = pm.read_int(list_ptr + player_count_offset)
    array_ptr   = pm.read_longlong(list_ptr + player_array_offset)

    records = []
    for i in range(count):
        player_ptr = pm.read_longlong(array_ptr + 0x20 + (i * 8))
        # Read fields at their offsets from dump.cs:
        ca = pm.read_short(player_ptr + 0x10)   # replace 0x10 with real CA offset
        pa = pm.read_short(player_ptr + 0x14)   # replace 0x14 with real PA offset
        records.append({"index": i, "ca": ca, "pa": pa})

    with open("players_memory.csv", "w", newline="") as f:
        w = csv.DictWriter(f, fieldnames=["index", "ca", "pa"])
        w.writeheader()
        w.writerows(records)

    print(f"Exported {len(records)} players")

export_via_memory()
```

---

## Diagnostic Checklist (When the Player Table Cannot Be Found)

When a plugin or script cannot locate the player database, run through this checklist:

1. **Confirm FM26 is IL2CPP** → Check if `GameAssembly.dll` exists in game folder
2. **Run Il2CppDumper** → Do NOT skip this step; it reveals exact class names
3. **Search dump.cs for these keywords**: `Player`, `Person`, `Staff`, `Manager`, `Database`, `Cache`, `Repository`, `Registry`
4. **Check if using wrong BepInEx build** → Must be `UnityIL2CPP` build, not Mono
5. **Verify game version** → dump.cs is version-specific; re-run after each game patch
6. **Use UnityExplorer first** → Inspect live GameObjects in-game to visually find the manager holding the player list before writing code
7. **Check static fields in dump.cs** → Player managers are often static singletons; look for `static` fields at class level

---

## Quick Reference — Key Terms

| Term | Meaning |
|---|---|
| IL2CPP | Unity backend that compiles C# to native code — no .NET reflection |
| GameAssembly.dll | The compiled native binary containing all game logic |
| global-metadata.dat | Metadata file with class/field/method names for IL2CPP |
| dump.cs | Output of Il2CppDumper — shows all classes with memory offsets |
| BepInEx (IL2CPP) | Plugin loader framework compatible with IL2CPP Unity games |
| UnityExplorer | Runtime inspector plugin — browse live game objects and fields |
| FMRTE | Third-party FM editor that reads FM process memory externally |
| .fmf / .dat | FM26 binary database files (pre-game editor format) |
| CA / PA | Current Ability / Potential Ability — key player attributes |
