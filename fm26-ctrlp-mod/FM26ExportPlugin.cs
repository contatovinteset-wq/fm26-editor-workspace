using System;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public static int patchCount = 0;
        
        public override void Load()
        {
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v1.0.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            try
            {
                var harmony = new Harmony("com.koda.fm26.ctrlp");
                Log.LogInfo("[Init] Procurando classes para patchear...");
                
                int count = 0;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var name = asm.GetName().Name;
                        if (name.StartsWith("System") || name.StartsWith("Mono") || name.StartsWith("mscorlib") || name.StartsWith("Il2Cpp"))
                            continue;
                        
                        foreach (var type in asm.GetTypes())
                        {
                            try
                            {
                                var updateMethod = type.GetMethod("Update", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                if (updateMethod != null && updateMethod.GetParameters().Length == 0)
                                {
                                    // Patch todos os Updates que encontrar
                                    var patchMethod = typeof(Plugin).GetMethod("OnUpdate", BindingFlags.Static | BindingFlags.Public);
                                    harmony.Patch(updateMethod, postfix: new HarmonyMethod(patchMethod));
                                    count++;
                                    
                                    if (count <= 10)
                                        Log.LogInfo($"[Init] Patched: {type.FullName}.Update");
                                    
                                    if (count >= 50) break; // Limita a 50 patches
                                }
                            }
                            catch { }
                        }
                        if (count >= 50) break;
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Init] Total de patches: {count}");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"[Init] Erro: {ex.Message}");
                Log.LogError($"[Init] StackTrace: {ex.StackTrace}");
            }
        }
        
        public static void OnUpdate()
        {
            patchCount++;
            if (patchCount % 100 == 0) // Loga a cada 100 chamadas
            {
                Debug.Log($"[FM26CtrlP] OnUpdate chamado! Total: {patchCount}");
            }
        }
    }
}
