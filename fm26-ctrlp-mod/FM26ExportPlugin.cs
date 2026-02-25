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
        
        private static int _frameCount = 0;
        private static bool _searched = false;
        private static MethodInfo _exportMethod = null;
        private static Type _targetType = null;
        
        public static void OnUpdate()
        {
            _frameCount++;
            
            // Só busca método depois de 60 frames (deixa o jogo estabilizar)
            if (!_searched && _frameCount > 60)
            {
                _searched = true;
                FindExportMethod();
            }
            
            // Só processa input depois de buscar
            if (!_searched) return;
            
            try
            {
                // Ctrl+P
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
                {
                    Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO!");
                    TryExport();
                }
                
                // F10 - Debug
                if (Input.GetKeyDown(KeyCode.F10))
                {
                    Debug.Log("[FM26CtrlP] >>> F10 - Listando tipos...");
                    ListTypes();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro: {ex.Message}");
            }
        }
        
        private static void FindExportMethod()
        {
            try
            {
                Debug.Log("[FM26CtrlP] Buscando UpdateExportCurrentItemBinding...");
                
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var name = assembly.GetName().Name;
                        if (name.StartsWith("System") || name.StartsWith("Mono") || 
                            name.StartsWith("mscorlib") || name.StartsWith("Il2Cpp") ||
                            name.StartsWith("BepInEx") || name.StartsWith("0Harmony"))
                            continue;
                        
                        foreach (var type in assembly.GetTypes())
                        {
                            try
                            {
                                var method = type.GetMethod("UpdateExportCurrentItemBinding",
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                
                                if (method != null)
                                {
                                    _targetType = type;
                                    _exportMethod = method;
                                    Debug.Log($"[FM26CtrlP] ENCONTRADO: {type.FullName}.{method.Name}");
                                    return;
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                
                Debug.LogWarning("[FM26CtrlP] Método UpdateExportCurrentItemBinding não encontrado");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro na busca: {ex.Message}");
            }
        }
        
        private static void TryExport()
        {
            if (_exportMethod == null || _targetType == null)
            {
                Debug.LogWarning("[FM26CtrlP] Método de export não disponível");
                return;
            }
            
            try
            {
                var objects = UnityEngine.Object.FindObjectsOfType(_targetType);
                if (objects != null && objects.Length > 0)
                {
                    Debug.Log($"[FM26CtrlP] {objects.Length} objetos encontrados");
                    
                    foreach (var obj in objects)
                    {
                        if (obj != null)
                        {
                            _exportMethod.Invoke(obj, new object[] { 0 });
                        }
                    }
                    Debug.Log("[FM26CtrlP] Export concluído!");
                }
                else
                {
                    Debug.LogWarning("[FM26CtrlP] Nenhum objeto encontrado");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro no export: {ex.Message}");
            }
        }
        
        private static void ListTypes()
        {
            int count = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var name = assembly.GetName().Name;
                    if (name.StartsWith("System") || name.StartsWith("Mono")) continue;
                    
                    foreach (var type in assembly.GetTypes())
                    {
                        try
                        {
                            if (type.Name.Contains("Export") || type.Name.Contains("Carousel") || 
                                type.Name.Contains("Table") || type.Name.Contains("View"))
                            {
                                Debug.Log($"[FM26CtrlP] {type.FullName}");
                                count++;
                                if (count > 30) return;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
    }
}
