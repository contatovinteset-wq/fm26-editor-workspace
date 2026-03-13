# Busca automatica pelo FM26 em todos os discos
Write-Host "Buscando Football Manager 26..." -ForegroundColor Cyan

$found = $null

# 1. Tenta caminhos Steam comuns
$steamPaths = @(
    "C:\Program Files (x86)\Steam\steamapps\common\Football Manager 26",
    "C:\Program Files\Steam\steamapps\common\Football Manager 26",
    "D:\Steam\steamapps\common\Football Manager 26",
    "D:\SteamLibrary\steamapps\common\Football Manager 26",
    "E:\Steam\steamapps\common\Football Manager 26",
    "E:\SteamLibrary\steamapps\common\Football Manager 26",
    "E:\Football Manager 26",
    "F:\Steam\steamapps\common\Football Manager 26",
    "F:\SteamLibrary\steamapps\common\Football Manager 26",
    "F:\Football Manager 26"
)

foreach ($p in $steamPaths) {
    if (Test-Path "$p\BepInEx\core\BepInEx.Core.dll") {
        $found = $p + "\"
        break
    }
}

# 2. Se nao achou, busca pelo registro do Steam
if (-not $found) {
    try {
        $steamReg = Get-ItemProperty "HKCU:\Software\Valve\Steam" -ErrorAction SilentlyContinue
        if ($steamReg) {
            $steamBase = $steamReg.SteamPath -replace "/", "\"
            $candidate = "$steamBase\steamapps\common\Football Manager 26"
            if (Test-Path "$candidate\BepInEx\core\BepInEx.Core.dll") {
                $found = $candidate + "\"
            }
        }
    } catch {}
}

# 3. Se ainda nao achou, busca em todos os drives
if (-not $found) {
    Write-Host "Buscando em todos os drives (pode demorar um pouco)..." -ForegroundColor Yellow
    $drives = Get-PSDrive -PSProvider FileSystem | Select-Object -ExpandProperty Root
    foreach ($drive in $drives) {
        $candidates = @(
            "${drive}Football Manager 26",
            "${drive}Steam\steamapps\common\Football Manager 26",
            "${drive}SteamLibrary\steamapps\common\Football Manager 26",
            "${drive}Games\Football Manager 26"
        )
        foreach ($c in $candidates) {
            if (Test-Path "$c\BepInEx\core\BepInEx.Core.dll") {
                $found = $c + "\"
                break
            }
        }
        if ($found) { break }
    }
}

if ($found) {
    Write-Host ""
    Write-Host "ENCONTRADO: $found" -ForegroundColor Green
    Write-Host ""
    Write-Host "Cole esta linha no FM26TacticsDump.csproj:" -ForegroundColor Yellow
    Write-Host "    <FM26Path>$found</FM26Path>" -ForegroundColor White
    Write-Host ""
    Write-Host "Verificando DLLs..." -ForegroundColor Cyan
    $dlls = @(
        "BepInEx\core\BepInEx.Core.dll",
        "BepInEx\core\BepInEx.Unity.IL2CPP.dll",
        "BepInEx\core\Il2CppInterop.Runtime.dll",
        "BepInEx\core\0Harmony.dll",
        "BepInEx\interop\Il2Cppmscorlib.dll",
        "BepInEx\interop\UnityEngine.CoreModule.dll",
        "BepInEx\interop\UnityEngine.UIElementsModule.dll",
        "BepInEx\interop\Unity.InputSystem.dll"
    )
    foreach ($dll in $dlls) {
        $full = Join-Path $found $dll
        if (Test-Path $full) { Write-Host "  OK  $dll" -ForegroundColor Green }
        else                 { Write-Host "  XXX $dll" -ForegroundColor Red }
    }
} else {
    Write-Host ""
    Write-Host "NAO ENCONTRADO automaticamente." -ForegroundColor Red
    Write-Host "Execute o comando abaixo para localizar manualmente:" -ForegroundColor Yellow
    Write-Host '  Get-ChildItem -Path C:\,D:\,E:\,F:\ -Filter "BepInEx.Core.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object FullName' -ForegroundColor White
}
