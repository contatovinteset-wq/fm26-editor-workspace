using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.9.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.9.0 CARREGADO!");
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
                if (Keyboard.current == null) return;
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                // Ctrl+P - EXPORTAR
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - EXPORTAR");
                    SafeExport();
                }
                
                // F9 - DUMP SIMPLES (só nome e filhos)
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - DUMP HIERARQUIA");
                    DumpHierarchy();
                }
                
                // F10 - Buscar dataSource em TODOS os UIDocuments
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar dataSource GLOBAL");
                    FindAllDataSourcesGlobal();
                }
                
                // F11 - Dump PROFUNDO do primeiro UIDocument
                if (Keyboard.current.f11Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F11 - Dump PROFUNDO");
                    DumpDeep();
                }
                
                // F12 - Diagnóstico
                if (Keyboard.current.f12Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F12 - Diagnóstico");
                    Diagnose();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        // F9 - DUMP de TODOS os UIDocuments
        private static void DumpHierarchy()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[F9] ===== {uiDocs.Length} UIDocuments =====");
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        Log.LogInfo($"[F9] === {doc.name} ===");
                        DumpElementSimple(root, 0, 8);
                    }
                    catch { }
                }
                Log.LogInfo("[F9] ===== FIM =====");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F9] Erro: {ex.Message}");
            }
        }
        
        private static void DumpElementSimple(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                string indent = new string(' ', depth * 2);
                string name = element.name ?? "(null)";
                int childCount = element.childCount;
                
                // Só mostrar nome e quantidade de filhos
                Log.LogInfo($"[F9] {indent}{name} [{childCount}]");
                
                // Recursão
                for (int i = 0; i < childCount && i < 100; i++)
                {
                    try
                    {
                        DumpElementSimple(element[i], depth + 1, maxDepth);
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        // F10 - Buscar TODOS os dataSources em TODOS os UIDocuments
        private static void FindAllDataSourcesGlobal()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[F10] Buscando em {uiDocs.Length} UIDocuments...");
                
                int total = 0;
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        int found = 0;
                        FindDataSourcesRecursive(root, ref found, 0, 20, doc.name);
                        total += found;
                    }
                    catch { }
                }
                Log.LogInfo($"[F10] Total dataSources encontrados: {total}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F10] Erro: {ex.Message}");
            }
        }
        
        private static void FindDataSourcesRecursive(VisualElement element, ref int count, int depth, int maxDepth, string docName)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                string name = element.name ?? "(null)";
                
                // Verificar dataSource
                var dsProp = typeof(VisualElement).GetProperty("dataSource", BindingFlags.Public | BindingFlags.Instance);
                if (dsProp != null)
                {
                    try
                    {
                        var ds = dsProp.GetValue(element);
                        if (ds != null)
                        {
                            count++;
                            string dsType = ds.GetType().Name;
                            
                            if (ds is IList list)
                            {
                                Log.LogInfo($"[F10] ✅ [{docName}] {name}: {dsType} ({list.Count} itens)");
                            }
                            else
                            {
                                Log.LogInfo($"[F10] 📌 [{docName}] {name}: {dsType}");
                            }
                        }
                    }
                    catch { }
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 100; i++)
                {
                    try
                    {
                        FindDataSourcesRecursive(element[i], ref count, depth + 1, maxDepth, docName);
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        // F11 - Dump PROFUNDO (até 20 níveis)
        private static void DumpDeep()
        {
            try
            {
                var report = FindReportElement();
                if (report == null)
                {
                    Log.LogWarning("[F11] Report não encontrado");
                    return;
                }
                
                Log.LogInfo("[F11] ===== DUMP PROFUNDO =====");
                DumpElementDeep(report, 0, 20);
                Log.LogInfo("[F11] ===== FIM =====");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F11] Erro: {ex.Message}");
            }
        }
        
        private static void DumpElementDeep(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                string indent = new string(' ', Math.Min(depth, 40));
                string name = element.name ?? "(null)";
                int childCount = element.childCount;
                
                Log.LogInfo($"[F11] {indent}{name} [{childCount}]");
                
                // Recursão mais profunda
                for (int i = 0; i < childCount && i < 200; i++)
                {
                    try
                    {
                        DumpElementDeep(element[i], depth + 1, maxDepth);
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        // F12 - Diagnóstico
        private static void Diagnose()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[F12] {uiDocs.Length} UIDocuments");
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        Log.LogInfo($"[F12]   {doc.name}: {doc.rootVisualElement?.childCount ?? 0} filhos");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[F12] Erro: {ex.Message}");
            }
        }
        
        // Ctrl+P - Exportar (busca em TODOS os UIDocuments)
        private static void SafeExport()
        {
            try
            {
                Log.LogInfo("[Export] Buscando dados em TODOS os UIDocuments...");
                
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[Export] {uiDocs.Length} UIDocuments encontrados");
                
                IList foundData = null;
                string foundElement = "";
                string foundDoc = "";
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        FindDataForExport(root, ref foundData, ref foundElement, 0, 20);
                        
                        if (foundData != null)
                        {
                            foundDoc = doc.name;
                            break;
                        }
                    }
                    catch { }
                }
                
                if (foundData != null && foundData.Count > 0)
                {
                    Log.LogInfo($"[Export] ✅ Encontrado {foundData.Count} itens em [{foundDoc}] {foundElement}");
                    ExportToCsv(foundData);
                }
                else
                {
                    Log.LogWarning("[Export] Nenhum dado encontrado. Use F10 para ver dataSources.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void FindDataForExport(VisualElement element, ref IList foundData, ref string foundName, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth || foundData != null) return;
            
            try
            {
                var dsProp = typeof(VisualElement).GetProperty("dataSource", BindingFlags.Public | BindingFlags.Instance);
                if (dsProp != null)
                {
                    try
                    {
                        var ds = dsProp.GetValue(element);
                        if (ds is IList list && list.Count > 0)
                        {
                            foundData = list;
                            foundName = element.name ?? "(null)";
                            return;
                        }
                    }
                    catch { }
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 100; i++)
                {
                    try
                    {
                        FindDataForExport(element[i], ref foundData, ref foundName, depth + 1, maxDepth);
                        if (foundData != null) return;
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static VisualElement FindReportElement()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    for (int i = 0; i < root.childCount; i++)
                    {
                        try
                        {
                            var child = root[i];
                            if (child?.name == "Report")
                            {
                                return child;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
            
            return null;
        }
        
        private static void ExportToCsv(IList data)
        {
            try
            {
                if (data == null || data.Count == 0)
                {
                    Log.LogWarning("[Export] Lista vazia");
                    return;
                }
                
                var firstItem = data[0];
                if (firstItem == null)
                {
                    Log.LogError("[Export] Primeiro item é null");
                    return;
                }
                
                var type = firstItem.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                var csv = new System.Text.StringBuilder();
                var headers = new List<string>();
                
                foreach (var prop in props)
                {
                    if (prop.GetIndexParameters().Length == 0)
                    {
                        headers.Add(prop.Name);
                    }
                }
                csv.AppendLine(string.Join(";", headers));
                
                int rowCount = 0;
                foreach (var item in data)
                {
                    if (item == null) continue;
                    
                    var values = new List<string>();
                    foreach (var prop in props)
                    {
                        if (prop.GetIndexParameters().Length > 0) continue;
                        
                        try
                        {
                            var value = prop.GetValue(item);
                            string str = (value?.ToString() ?? "").Replace(";", ",").Replace("\n", " ").Replace("\r", "");
                            values.Add(str);
                        }
                        catch
                        {
                            values.Add("");
                        }
                    }
                    csv.AppendLine(string.Join(";", values));
                    rowCount++;
                }
                
                var path = System.IO.Path.Combine(
                    BepInEx.Paths.PluginPath, 
                    $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                );
                
                System.IO.File.WriteAllText(path, csv.ToString());
                Log.LogInfo($"[Export] ✅ {rowCount} linhas salvas em: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro CSV: {ex.Message}");
            }
        }
    }
}
