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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.20.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.20.0 CARREGADO!");
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
                
                if (!_initialized || Keyboard.current == null) return;
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Exportar");
                    ExportData();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Mapear hierarquia PROFUNDA PlayerSearchReport");
                    MapDeepHierarchy("PlayerSearchReport");
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar elementos 'StreamedTable'");
                    FindStreamedTables();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void MapDeepHierarchy(string reportName)
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    var report = FindElementByName(root, reportName, 0, 50);
                    if (report != null)
                    {
                        Log.LogInfo($"[Deep] === {reportName} === childCount: {report.childCount}");
                        
                        // Mapear com profundidade 12
                        MapElementDeep(report, 0, 12);
                    }
                    else
                    {
                        Log.LogWarning($"[Deep] {reportName} não encontrado");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Deep] Erro: {ex.Message}");
            }
        }
        
        private static int _totalElements = 0;
        
        private static void MapElementDeep(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            _totalElements++;
            if (_totalElements > 500) return; // Limite de segurança
            
            string indent = new string(' ', depth * 2);
            
            // Verificar dataSource
            string dsInfo = "";
            try
            {
                var dsProp = element.GetType().GetProperty("dataSource");
                if (dsProp != null)
                {
                    var ds = dsProp.GetValue(element);
                    if (ds != null)
                    {
                        dsInfo = $" [DS: {ds.GetType().Name}]";
                    }
                }
            }
            catch { }
            
            // Marcar elementos suspeitos
            string marker = "";
            string nameLower = element.name.ToLower();
            if (nameLower.Contains("table") || nameLower.Contains("list") || 
                nameLower.Contains("grid") || nameLower.Contains("row") ||
                nameLower.Contains("streamed") || nameLower.Contains("item"))
            {
                marker = " ⭐⭐⭐";
            }
            
            Log.LogInfo($"[Deep] {indent}{element.name} ({element.GetType().Name}) [{element.childCount}]{dsInfo}{marker}");
            
            // Recursão em TODOS os filhos
            for (int i = 0; i < element.childCount; i++)
            {
                MapElementDeep(element[i], depth + 1, maxDepth);
            }
        }
        
        private static void FindStreamedTables()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    Log.LogInfo($"[Table] Escaneando UIDocument: {doc.name}");
                    
                    // Buscar elementos com "StreamedTable" no nome ou tipo
                    var found = new List<VisualElement>();
                    FindElementsWithPattern(root, found, 0, 30, 
                        e => e.name.Contains("Streamed") || 
                             e.name.Contains("Table") ||
                             e.name.Contains("List") ||
                             e.name.Contains("Grid"));
                    
                    Log.LogInfo($"[Table] Encontrados: {found.Count}");
                    foreach (var e in found)
                    {
                        Log.LogInfo($"[Table] ⭐ {e.name} ({e.GetType().Name}) childCount={e.childCount}");
                        
                        // Explorar dataSource
                        try
                        {
                            var dsProp = e.GetType().GetProperty("dataSource");
                            if (dsProp != null)
                            {
                                var ds = dsProp.GetValue(e);
                                if (ds != null)
                                {
                                    Log.LogInfo($"[Table]   dataSource: {ds.GetType().FullName}");
                                    ExploreObjectDeep(ds, 0, 3);
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Table] Erro: {ex.Message}");
            }
        }
        
        private static void FindElementsWithPattern(VisualElement element, List<VisualElement> results, int depth, int maxDepth, Func<VisualElement, bool> predicate)
        {
            if (element == null || depth > maxDepth) return;
            
            if (predicate(element))
            {
                results.Add(element);
            }
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindElementsWithPattern(element[i], results, depth + 1, maxDepth, predicate);
            }
        }
        
        private static void ExploreObjectDeep(object obj, int depth, int maxDepth)
        {
            if (obj == null || depth > maxDepth) return;
            
            try
            {
                var type = obj.GetType();
                string indent = new string(' ', depth * 2 + 2);
                
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    if (p.Name.Length > 30) continue; // Ignorar nomes muito longos
                    
                    try
                    {
                        var val = p.GetValue(obj);
                        if (val == null) continue;
                        
                        if (val is IList list)
                        {
                            Log.LogInfo($"[Obj] {indent}{p.Name}: List<{list.Count}>");
                        }
                        else if (!p.PropertyType.IsPrimitive && p.PropertyType != typeof(string))
                        {
                            Log.LogInfo($"[Obj] {indent}{p.Name}: {p.PropertyType.Name}");
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static void ExportData()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Buscar qualquer elemento com dados
                    var found = new List<VisualElement>();
                    FindElementsWithPattern(root, found, 0, 30, e => true);
                    
                    foreach (var element in found)
                    {
                        try
                        {
                            var dsProp = element.GetType().GetProperty("dataSource");
                            if (dsProp == null) continue;
                            
                            var ds = dsProp.GetValue(element);
                            if (ds == null) continue;
                            
                            var list = FindListInObject(ds, 0, 5);
                            if (list != null && list.Count > 0)
                            {
                                Log.LogInfo($"[Export] ✅ Dados em {element.name}: {list.Count} itens");
                                ExportCsv(list);
                                return;
                            }
                        }
                        catch { }
                    }
                }
                
                Log.LogWarning("[Export] Nenhum dado encontrado.");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static IList FindListInObject(object obj, int depth, int maxDepth)
        {
            if (obj == null || depth > maxDepth) return null;
            
            try
            {
                var type = obj.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    
                    try
                    {
                        var val = p.GetValue(obj);
                        if (val is IList list && list.Count > 0)
                        {
                            return list;
                        }
                        else if (val != null && !p.PropertyType.IsPrimitive && p.PropertyType != typeof(string))
                        {
                            var found = FindListInObject(val, depth + 1, maxDepth);
                            if (found != null) return found;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            
            return null;
        }
        
        private static VisualElement FindElementByName(VisualElement element, string name, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return null;
            if (element.name == name) return element;
            
            for (int i = 0; i < element.childCount; i++)
            {
                var found = FindElementByName(element[i], name, depth + 1, maxDepth);
                if (found != null) return found;
            }
            return null;
        }
        
        private static void ExportCsv(IList data)
        {
            try
            {
                var first = data[0];
                if (first == null)
                {
                    Log.LogWarning("[CSV] Primeiro item é null");
                    return;
                }
                
                // Tenta extrair Item1 de ValueTuple
                var item1Prop = first.GetType().GetProperty("Item1");
                object targetObj = item1Prop != null ? item1Prop.GetValue(first) : first;
                
                var type = targetObj.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                var csv = new System.Text.StringBuilder();
                var headers = new List<string>();
                
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length == 0 && p.Name.Length < 30) headers.Add(p.Name);
                }
                csv.AppendLine(string.Join(";", headers));
                
                int count = 0;
                foreach (var item in data)
                {
                    if (item == null) continue;
                    object rowObj = item1Prop != null ? item1Prop.GetValue(item) : item;
                    
                    var values = new List<string>();
                    foreach (var p in props)
                    {
                        if (p.GetIndexParameters().Length > 0 || p.Name.Length >= 30) continue;
                        try
                        {
                            var val = p.GetValue(rowObj);
                            values.Add((val?.ToString() ?? "").Replace(";", ","));
                        }
                        catch { values.Add(""); }
                    }
                    csv.AppendLine(string.Join(";", values));
                    count++;
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                Log.LogInfo($"[CSV] ✅ {count} linhas em: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[CSV] Erro: {ex.Message}");
            }
        }
    }
}
