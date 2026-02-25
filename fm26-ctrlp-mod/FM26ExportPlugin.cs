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
            
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            
            // Patch SI.Bindable.Bindings.Update (classe ativa)
            var bindingsType = Type.GetType("SI.Bindable.Bindings, SI.Bindable");
            if (bindingsType != null)
            {
                var updateMethod = bindingsType.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
                if (updateMethod != null)
                {
                    var patchMethod = typeof(Plugin).GetMethod("OnUpdate", BindingFlags.Static | BindingFlags.Public);
                    harmony.Patch(updateMethod, postfix: new HarmonyMethod(patchMethod));
                    Log.LogInfo("[Init] Patched SI.Bindable.Bindings.Update");
                }
            }
        }
        
        private static MethodInfo _exportMethod = null;
        private static Type _targetType = null;
        private static bool _initialized = false;
        
        public static void OnUpdate()
        {
            try
            {
                // Busca método na primeira chamada
                if (!_initialized)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            var name = assembly.GetName().Name;
                            if (name.StartsWith("System") || name.StartsWith("Mono") || name.StartsWith("mscorlib") || name.StartsWith("Il2Cpp"))
                                continue;
                            
                            foreach (var type in assembly.GetTypes())
                            {
                                var method = type.GetMethod("UpdateExportCurrentItemBinding",
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                
                                if (method != null)
                                {
                                    _targetType = type;
                                    _exportMethod = method;
                                    _initialized = true;
                                    Debug.Log($"[FM26CtrlP] ENCONTRADO: {type.FullName}");
                                    return;
                                }
                            }
                        }
                        catch { }
                    }
                    _initialized = true; // Marca como verificado mesmo se não encontrar
                }
                
                // Verifica Ctrl+P
                if (UnityEngine.Input.GetKey(KeyCode.LeftControl) && UnityEngine.Input.GetKeyDown(KeyCode.P))
                {
                    Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO!");
                    
                    if (_exportMethod != null && _targetType != null)
                    {
                        try
                        {
                            var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", new Type[] { typeof(Type) });
                            if (findMethod != null)
                            {
                                var objects = findMethod.Invoke(null, new object[] { _targetType }) as Il2CppSystem.Array;
                                if (objects != null)
                                {
                                    var count = objects.Length;
                                    Debug.Log($"[FM26CtrlP] {count} objetos encontrados");
                                    
                                    for (int i = 0; i < count; i++)
                                    {
                                        var obj = objects.GetValue(i);
                                        if (obj != null)
                                        {
                                            _exportMethod.Invoke(obj, new object[] { 0 });
                                            Debug.Log($"[FM26CtrlP] Exportado: {i + 1}/{count}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[FM26CtrlP] Erro no export: {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[FM26CtrlP] Método UpdateExportCurrentItemBinding não encontrado");
                    }
                }
                
                // F10 - Debug
                if (UnityEngine.Input.GetKeyDown(KeyCode.F10))
                {
                    Debug.Log("[FM26CtrlP] >>> F10 - Listando tipos com 'Export' no nome...");
                    int count = 0;
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            var name = assembly.GetName().Name;
                            if (name.StartsWith("System") || name.StartsWith("Mono")) continue;
                            
                            foreach (var type in assembly.GetTypes())
                            {
                                if (type.Name.Contains("Export") || type.Name.Contains("Carousel") || 
                                    type.Name.Contains("Table") || type.Name.Contains("View"))
                                {
                                    Debug.Log($"[FM26CtrlP] Tipo: {type.FullName}");
                                    count++;
                                    if (count > 20) return;
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
    }
}
