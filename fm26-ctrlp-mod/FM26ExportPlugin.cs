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
            
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            
            // Patch SI.Bindable.Bindings.Update
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
        private static bool _initialized = false;
        private static Type _sicarouselManagedType = null;
        private static MethodInfo _exportMethod = null;
        
        public static void OnUpdate()
        {
            _frameCount++;
            
            // Inicializa depois de 300 frames
            if (!_initialized && _frameCount == 300)
            {
                _initialized = true;
                InitializeTypes();
            }
            
            if (!_initialized) return;
            
            // Teclado disponível?
            if (Keyboard.current == null) return;
            
            // Ctrl+P - NOVO INPUT SYSTEM
            bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
            bool p = Keyboard.current.pKey.wasPressedThisFrame;
            
            if (ctrl && p)
            {
                Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO!");
                Log.LogInfo(">>> Ctrl+P PRESSIONADO!");
                DoExport();
            }
            
            // F10 - Debug: lista tipos
            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F10 - Debug tipos");
                LogTypes();
            }
        }
        
        private static void InitializeTypes()
        {
            try
            {
                // Usar tipo managed (não Il2CppSystem.Type)
                _sicarouselManagedType = Type.GetType("SI.Bindable.SICarousel, SI.Bindable");
                
                if (_sicarouselManagedType != null)
                {
                    Debug.Log($"[FM26CtrlP] Tipo managed obtido: {_sicarouselManagedType.FullName}");
                    Log.LogInfo($"[Init] Tipo managed obtido: {_sicarouselManagedType.FullName}");
                    
                    _exportMethod = _sicarouselManagedType.GetMethod("UpdateExportCurrentItemBinding",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (_exportMethod != null)
                    {
                        Debug.Log($"[FM26CtrlP] Método encontrado: {_exportMethod.Name}");
                        Log.LogInfo($"[Init] Método encontrado: {_exportMethod.Name}");
                    }
                    else
                    {
                        Debug.LogError("[FM26CtrlP] Método NÃO encontrado");
                    }
                }
                else
                {
                    Debug.LogError("[FM26CtrlP] Falha ao obter tipo managed");
                    Log.LogError("[Init] Falha ao obter tipo managed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro na inicialização: {ex.Message}");
                Log.LogError($"[Init] Erro: {ex.Message}");
            }
        }
        
        private static void DoExport()
        {
            if (_sicarouselManagedType == null)
            {
                Log.LogError("[Export] Tipo não inicializado");
                return;
            }
            
            if (_exportMethod == null)
            {
                Log.LogError("[Export] Método não inicializado");
                return;
            }
            
            try
            {
                Log.LogInfo("[Export] Buscando objetos com Resources.FindObjectsOfTypeAll...");
                
                // Resources.FindObjectsOfTypeAll funciona com qualquer tipo
                var objects = Resources.FindObjectsOfTypeAll(_sicarouselManagedType);
                
                if (objects == null || objects.Length == 0)
                {
                    Log.LogWarning("[Export] Nenhum carousel encontrado");
                    return;
                }
                
                Log.LogInfo($"[Export] {objects.Length} objetos encontrados");
                
                int success = 0;
                foreach (var obj in objects)
                {
                    if (obj == null) continue;
                    
                    try
                    {
                        // Tentar cast para MonoBehaviour para verificar se está ativo
                        var mono = obj as MonoBehaviour;
                        if (mono != null && mono.gameObject != null)
                        {
                            // Pular objetos hidden ou de sistema
                            if (mono.gameObject.hideFlags != HideFlags.None)
                                continue;
                            
                            Log.LogInfo($"[Export] Exportando: {mono.gameObject.name}");
                        }
                        
                        _exportMethod.Invoke(obj, new object[] { 0 });
                        success++;
                        Debug.Log($"[FM26CtrlP] Exportado com sucesso");
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"[Export] Erro: {ex.Message}");
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
            Debug.Log($"[FM26CtrlP] Total de tipos: {count}");
        }
    }
}
