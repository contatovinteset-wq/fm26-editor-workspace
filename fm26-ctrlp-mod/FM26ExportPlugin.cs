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
        private static bool _initialized = false;
        private static MethodInfo _exportMethod = null;
        private static Type _sicarouselType = null;
        
        public static void OnUpdate()
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
                LogAllTypes();
            }
        }
        
        private static void InitializeTypes()
        {
            try
            {
                _sicarouselType = Type.GetType("SI.Bindable.SICarousel, SI.Bindable");
                
                if (_sicarouselType != null)
                {
                    Debug.Log($"[FM26CtrlP] Tipo encontrado: {_sicarouselType.FullName}");
                    Log.LogInfo($"[Init] Tipo: {_sicarouselType.FullName}");
                    
                    _exportMethod = _sicarouselType.GetMethod("UpdateExportCurrentItemBinding",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (_exportMethod != null)
                    {
                        Debug.Log($"[FM26CtrlP] Método encontrado: {_exportMethod.Name}");
                        Log.LogInfo($"[Init] Método: {_exportMethod.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Init erro: {ex.Message}");
            }
        }
        
        private static void DoExport()
        {
            if (_exportMethod == null || _sicarouselType == null)
            {
                Log.LogError("[Export] Tipos não inicializados");
                return;
            }
            
            try
            {
                Log.LogInfo("[Export] Buscando objetos...");
                
                // Nova abordagem: buscar todos os GameObjects e verificar componentes
                var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                Log.LogInfo($"[Export] {allGameObjects.Length} GameObjects encontrados");
                
                int found = 0;
                int exported = 0;
                
                foreach (var go in allGameObjects)
                {
                    if (go == null || go.hideFlags != HideFlags.None) continue;
                    
                    // Buscar todos os componentes do GameObject
                    var components = go.GetComponents<Component>();
                    if (components == null) continue;
                    
                    foreach (var comp in components)
                    {
                        if (comp == null) continue;
                        
                        // Verificar se o tipo do componente herda de SICarousel ou tem o método
                        var compType = comp.GetType();
                        if (compType == _sicarouselType || compType.IsSubclassOf(_sicarouselType) || 
                            compType.Name.Contains("Carousel"))
                        {
                            found++;
                            Debug.Log($"[FM26CtrlP] Encontrado: {compType.Name} em {go.name}");
                            Log.LogInfo($"[Export] Encontrado: {compType.Name} em {go.name}");
                            
                            try
                            {
                                // Verificar se tem o método
                                var method = compType.GetMethod("UpdateExportCurrentItemBinding",
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                
                                if (method != null)
                                {
                                    method.Invoke(comp, new object[] { 0 });
                                    exported++;
                                    Debug.Log($"[FM26CtrlP] Exportado: {go.name}");
                                    Log.LogInfo($"[Export] Exportado: {go.name}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"[FM26CtrlP] Erro ao exportar {go.name}: {ex.Message}");
                            }
                        }
                    }
                }
                
                Log.LogInfo($"[Export] Encontrados: {found}, Exportados: {exported}");
                
                if (exported == 0)
                {
                    Log.LogWarning("[Export] Nenhum carousel exportado. Tentando alternativa...");
                    TryAlternativeExport();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void TryAlternativeExport()
        {
            // Alternativa: buscar por nome do método em qualquer componente
            try
            {
                var allMono = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
                Log.LogInfo($"[Alt] {allMono.Length} MonoBehaviours encontrados");
                
                foreach (var mono in allMono)
                {
                    if (mono == null) continue;
                    if (mono.gameObject != null && mono.gameObject.hideFlags != HideFlags.None) continue;
                    
                    var type = mono.GetType();
                    var method = type.GetMethod("UpdateExportCurrentItemBinding",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (method != null)
                    {
                        Debug.Log($"[FM26CtrlP] Método encontrado em: {type.Name}");
                        Log.LogInfo($"[Alt] Método em: {type.Name}");
                        
                        try
                        {
                            method.Invoke(mono, new object[] { 0 });
                            Debug.Log($"[FM26CtrlP] Exportado via alternativa!");
                            Log.LogInfo($"[Alt] Exportado!");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[FM26CtrlP] Erro alt: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Alt] Erro: {ex.Message}");
            }
        }
        
        private static void LogAllTypes()
        {
            int count = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var name = assembly.GetName().Name;
                    if (!name.StartsWith("SI.") && !name.StartsWith("FM.")) continue;
                    
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains("Carousel") || type.Name.Contains("Export"))
                        {
                            Debug.Log($"[FM26CtrlP] {type.FullName}");
                            count++;
                        }
                    }
                }
                catch { }
            }
            Debug.Log($"[FM26CtrlP] Total: {count}");
        }
    }
}
