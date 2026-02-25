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
            
            // Busca método depois de 300 frames (5 segundos a 60fps)
            if (!_searched && _frameCount == 300)
            {
                _searched = true;
                FindExportMethod();
            }
            
            // Só processa depois de buscar
            if (!_searched) return;
            
            // Ctrl+P - SÓ LOGA POR ENQUANTO
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO! Frame: " + _frameCount);
                
                if (_exportMethod != null && _targetType != null)
                {
                    Debug.Log($"[FM26CtrlP] Método disponível: {_targetType.FullName}.{_exportMethod.Name}");
                    
                    // Tenta encontrar objetos
                    try
                    {
                        var objects = UnityEngine.Object.FindObjectsOfType(_targetType);
                        Debug.Log($"[FM26CtrlP] FindObjectsOfType retornou: {(objects != null ? objects.Length.ToString() : "null")}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FM26CtrlP] Erro FindObjectsOfType: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning("[FM26CtrlP] Método não encontrado ainda");
                }
            }
            
            // F10 - Debug
            if (Input.GetKeyDown(KeyCode.F10))
            {
                Debug.Log($"[FM26CtrlP] Frame: {_frameCount}, Método encontrado: {_targetType?.FullName ?? "null"}");
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
    }
}
