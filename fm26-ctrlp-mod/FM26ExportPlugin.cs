using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v1.0.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            try
            {
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
            catch (Exception ex)
            {
                Log.LogError($"[Init] Erro: {ex.Message}");
            }
        }
        
        private static int _frameCount = 0;
        private static bool _initialized = false;
        private static Type _sicarouselManagedType = null;
        private static MethodInfo _exportMethod = null;
        
        public static void OnUpdate()
        {
            try
            {
                _frameCount++;
                
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    InitializeTypes();
                }
                
                if (!_initialized) return;
                if (Keyboard.current == null) return;
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO!");
                    Log.LogInfo(">>> Ctrl+P PRESSIONADO!");
                    DoExport();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Debug.Log("[FM26CtrlP] >>> F10 - Debug");
                    LogTypes();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] OnUpdate erro: {ex.Message}");
            }
        }
        
        private static void InitializeTypes()
        {
            try
            {
                Log.LogInfo("[Init] Procurando tipos...");
                
                // Usar tipo managed (não Il2CppSystem.Type)
                _sicarouselManagedType = Type.GetType("SI.Bindable.SICarousel, SI.Bindable");
                
                if (_sicarouselManagedType != null)
                {
                    Log.LogInfo($"[Init] Tipo managed obtido: {_sicarouselManagedType.FullName}");
                    
                    _exportMethod = _sicarouselManagedType.GetMethod("UpdateExportCurrentItemBinding",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (_exportMethod != null)
                    {
                        Log.LogInfo($"[Init] Método encontrado: {_exportMethod.Name}");
                    }
                    else
                    {
                        Log.LogWarning("[Init] Método NÃO encontrado");
                    }
                }
                else
                {
                    Log.LogError("[Init] Falha ao obter tipo managed");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Init] Erro: {ex.Message}");
            }
        }
        
        private static void DoExport()
        {
            Log.LogInfo("[Export] Iniciando...");
            
            try
            {
                if (_sicarouselManagedType == null)
                {
                    Log.LogError("[Export] _sicarouselManagedType é null");
                    return;
                }
                
                if (_exportMethod == null)
                {
                    Log.LogError("[Export] _exportMethod é null");
                    return;
                }
                
                Log.LogInfo("[Export] Buscando objetos com Resources.FindObjectsOfTypeAll...");
                
                // Usar Resources.FindObjectsOfTypeAll que funciona com qualquer tipo
                var allObjects = Resources.FindObjectsOfTypeAll(_sicarouselManagedType);
                
                if (allObjects == null || allObjects.Length == 0)
                {
                    Log.LogWarning("[Export] Nenhum carousel encontrado");
                    return;
                }
                
                Log.LogInfo($"[Export] {allObjects.Length} objetos encontrados");
                
                int success = 0;
                foreach (var obj in allObjects)
                {
                    try
                    {
                        if (obj == null) continue;
                        
                        var mono = obj as MonoBehaviour;
                        if (mono == null)
                        {
                            Log.LogWarning($"[Export] Objeto não é MonoBehaviour: {obj.GetType().Name}");
                            continue;
                        }
                        
                        // Pular objetos que estão no DontDestroyOnLoad ou são hidden
                        if (mono.gameObject.hideFlags != HideFlags.None)
                        {
                            Log.LogInfo($"[Export] Pulando objeto hidden: {mono.gameObject.name}");
                            continue;
                        }
                        
                        Log.LogInfo($"[Export] Exportando: {mono.gameObject.name}");
                        _exportMethod.Invoke(obj, new object[] { 0 });
                        success++;
                        Log.LogInfo($"[Export] OK: {mono.gameObject.name}");
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"[Export] Erro no objeto: {ex.Message}");
                    }
                }
                
                Log.LogInfo($"[Export] >>> CONCLUÍDO! {success} exportações");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro geral: {ex.Message}");
                Log.LogError($"[Export] Stack: {ex.StackTrace}");
            }
        }
        
        private static void LogTypes()
        {
            try
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
                            if (type.Name.Contains("Export") || 
                                type.Name.Contains("Carousel") || 
                                type.Name.Contains("Table"))
                            {
                                Debug.Log($"[FM26CtrlP] Tipo: {type.FullName}");
                                count++;
                                if (count > 30) return;
                            }
                        }
                    }
                    catch { }
                }
                Debug.Log($"[FM26CtrlP] Total: {count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] LogTypes erro: {ex.Message}");
            }
        }
    }
}
