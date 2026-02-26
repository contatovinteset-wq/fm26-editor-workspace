using System;
using System.Reflection;
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
        private static Il2CppSystem.Type _sicarouselType = null;
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
                
                _sicarouselType = Il2CppSystem.Type.GetType("SI.Bindable.SICarousel, SI.Bindable");
                
                if (_sicarouselType != null)
                {
                    Log.LogInfo($"[Init] Tipo Il2Cpp obtido: {_sicarouselType.FullName}");
                    
                    var managedType = Type.GetType("SI.Bindable.SICarousel, SI.Bindable");
                    if (managedType != null)
                    {
                        _exportMethod = managedType.GetMethod("UpdateExportCurrentItemBinding",
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
                }
                else
                {
                    Log.LogError("[Init] Falha ao obter Il2CppSystem.Type");
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
                if (_sicarouselType == null)
                {
                    Log.LogError("[Export] _sicarouselType é null");
                    return;
                }
                
                if (_exportMethod == null)
                {
                    Log.LogError("[Export] _exportMethod é null");
                    return;
                }
                
                Log.LogInfo("[Export] Buscando objetos...");
                var objects = UnityEngine.Object.FindObjectsOfType(_sicarouselType);
                
                if (objects == null)
                {
                    Log.LogWarning("[Export] FindObjectsOfType retornou null");
                    return;
                }
                
                var count = objects.Length;
                Log.LogInfo($"[Export] {count} objetos encontrados");
                
                if (count == 0)
                {
                    Log.LogWarning("[Export] Nenhum carousel ativo na cena");
                    return;
                }
                
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        var obj = objects[i];
                        if (obj == null)
                        {
                            Log.LogWarning($"[Export] Objeto {i} é null, pulando");
                            continue;
                        }
                        
                        Log.LogInfo($"[Export] Exportando {i + 1}/{count}: {obj.name}");
                        _exportMethod.Invoke(obj, new object[] { 0 });
                        Log.LogInfo($"[Export] OK: {obj.name}");
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"[Export] Erro no objeto {i}: {ex.Message}");
                    }
                }
                
                Log.LogInfo("[Export] >>> CONCLUÍDO!");
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
