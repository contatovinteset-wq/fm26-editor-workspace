using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.39.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.39.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            
            try
            {
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
                Log.LogError($"[Init] Erro no patch: {ex.Message}");
            }
        }
        
        private static int _frameCount = 0;
        private static bool _initialized = false;
        
        public static void OnUpdate()
        {
            try
            {
                _frameCount++;
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    Log.LogInfo("[Init] Pronto!");
                }
                
                if (!_initialized) return;
                
                try
                {
                    if (Keyboard.current == null) return;
                }
                catch { return; }
                
                try
                {
                    if (Keyboard.current.f9Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F9 - Scan seguro de UI");
                        SafeScanUI();
                    }
                }
                catch { }
                
                try
                {
                    if (Keyboard.current.f10Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F10 - Buscar Bindings.DataSet");
                        FindBindingsDataSet();
                    }
                }
                catch { }
            }
            catch { }
        }
        
        /// <summary>
        /// Scan SEGURO da UI - profundidade limitada, try-catch em tudo
        /// </summary>
        private static void SafeScanUI()
        {
            try
            {
                UIDocument[] uiDocs = null;
                try { uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>(); }
                catch { Log.LogWarning("[Scan] Erro ao buscar UIDocuments"); return; }
                
                if (uiDocs == null) { Log.LogWarning("[Scan] uiDocs null"); return; }
                
                Log.LogInfo($"[Scan] {uiDocs.Length} UIDocuments encontrados");
                
                int totalElements = 0;
                int maxElements = 100; // LIMITE DE SEGURANÇA
                
                foreach (var doc in uiDocs)
                {
                    if (totalElements >= maxElements) break;
                    if (doc == null) continue;
                    
                    string docName = "unknown";
                    try { docName = doc.name; } catch { }
                    Log.LogInfo($"[Scan] Doc: {docName}");
                    
                    VisualElement root = null;
                    try { root = doc.rootVisualElement; } catch { }
                    if (root == null) continue;
                    
                    // Apenas 2 níveis de profundidade, 10 filhos por nível
                    try
                    {
                        for (int i = 0; i < root.childCount && i < 10; i++)
                        {
                            if (totalElements >= maxElements) break;
                            
                            VisualElement child = null;
                            try { child = root[i]; }
                            catch { continue; }
                            
                            if (child == null) continue;
                            totalElements++;
                            
                            // Logar tipo e nome
                            string typeName = "unknown";
                            string elemName = "unknown";
                            try { typeName = child.GetType().Name; } catch { }
                            try { elemName = child.name; } catch { }
                            
                            Log.LogInfo($"[Scan]   {typeName}: {elemName}");
                            
                            // Segundo nível
                            try
                            {
                                for (int j = 0; j < child.childCount && j < 5; j++)
                                {
                                    VisualElement grandchild = null;
                                    try { grandchild = child[j]; }
                                    catch { continue; }
                                    
                                    if (grandchild == null) continue;
                                    totalElements++;
                                    
                                    string gcType = "unknown";
                                    string gcName = "unknown";
                                    try { gcType = grandchild.GetType().Name; } catch { }
                                    try { gcName = grandchild.name; } catch { }
                                    
                                    Log.LogInfo($"[Scan]     {gcType}: {gcName}");
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Scan] Total: {totalElements} elementos escaneados");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Scan] Erro geral: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Buscar Bindings.DataSet via hook estático
        /// </summary>
        private static void FindBindingsDataSet()
        {
            try
            {
                // O tipo Bindings está em SI.Bindable
                var bindingsType = Type.GetType("SI.Bindable.Bindings, SI.Bindable");
                if (bindingsType == null)
                {
                    Log.LogWarning("[Bind] Tipo Bindings não encontrado");
                    
                    // Tentar buscar em todos assemblies
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            var types = asm.GetTypes();
                            foreach (var t in types)
                            {
                                if (t.Name == "Bindings")
                                {
                                    Log.LogInfo($"[Bind] Encontrado em: {asm.GetName().Name}");
                                }
                            }
                        }
                        catch { }
                    }
                    return;
                }
                
                Log.LogInfo($"[Bind] Tipo encontrado: {bindingsType.FullName}");
                
                // Listar propriedades públicas
                var props = bindingsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[Bind] {props.Length} propriedades públicas:");
                
                foreach (var p in props.Take(20))
                {
                    try
                    {
                        Log.LogInfo($"[Bind]   {p.Name}: {p.PropertyType.Name}");
                    }
                    catch { }
                }
                
                // Buscar DataSet especificamente
                var dataSetProp = bindingsType.GetProperty("DataSet");
                if (dataSetProp != null)
                {
                    Log.LogInfo($"[Bind] DataSet encontrado! Tipo: {dataSetProp.PropertyType.FullName}");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Bind] Erro: {ex.Message}");
            }
        }
    }
}
