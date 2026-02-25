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
        public override void Load()
        {
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v1.0.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            try
            {
                // Usa Harmony para patchear método do jogo
                var harmony = new Harmony("com.koda.fm26.ctrlp");
                
                // Procura uma classe do jogo para patchear
                // Por enquanto, vamos apenas logar que estamos tentando
                Log.LogInfo("[Init] Procurando classe para patchear...");
                
                // Tenta encontrar qualquer classe com Update
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var name = asm.GetName().Name;
                        if (name.StartsWith("System") || name.StartsWith("Mono") || name.StartsWith("mscorlib") || name.StartsWith("Il2Cpp"))
                            continue;
                        
                        foreach (var type in asm.GetTypes())
                        {
                            var updateMethod = type.GetMethod("Update", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (updateMethod != null && updateMethod.GetParameters().Length == 0)
                            {
                                Log.LogInfo($"[Init] Encontrado: {type.FullName}.Update");
                                
                                // Patch o Update
                                var patchMethod = typeof(Plugin).GetMethod("OnUpdate", BindingFlags.Static | BindingFlags.Public);
                                harmony.Patch(updateMethod, postfix: new HarmonyMethod(patchMethod));
                                
                                Log.LogInfo("[Init] Harmony patch aplicado!");
                                return;
                            }
                        }
                    }
                    catch { }
                }
                
                Log.LogWarning("[Init] Não encontrou classe para patchear");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"[Init] Erro: {ex.Message}");
                Log.LogError($"[Init] StackTrace: {ex.StackTrace}");
            }
        }
        
        public static void OnUpdate()
        {
            Debug.Log("[FM26CtrlP] OnUpdate chamado!");
        }
    }
}
