# Execute este script para confirmar que os DLLs existem no caminho certo
$fm26path = "E:\Football Manager 26\"
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
    $full = Join-Path $fm26path $dll
    if (Test-Path $full) { Write-Host "OK  $dll" -ForegroundColor Green }
    else                 { Write-Host "XXX $dll  <- NAO ENCONTRADO" -ForegroundColor Red }
}
