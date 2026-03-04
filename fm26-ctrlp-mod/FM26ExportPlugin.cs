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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.23.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.23.0 CARREGADO!");
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
                    FindAndExportTable();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Subir hierarquia desde column-headers");
                    TraceAncestorsFromColumns();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar TODOS elementos com dataSource não-null");
                    FindAllWithDataSource();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void TraceAncestorsFromColumns()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Buscar column-headers com mais filhos (18 = tabela real)
                    var columnElements = new List<VisualElement>();
                    FindElementsWithPattern(root, columnElements, 0, 50, 
                        e => e.name == "column-headers" && e.childCount >= 10);
                    
                    foreach (var col in columnElements)
                    {
                        Log.LogInfo($"[Trace] === column-headers ({col.childCount} filhos) ===");
                        
                        // Subir na hierarquia
                        var current = col;
                        int level = 0;
                        
                        while (current != null && level < 20)
                        {
                            string dsInfo = GetDataSourceInfo(current);
                            Log.LogInfo($"[Trace] L{level}: {current.name} ({current.GetType().Name}) [{current.childCount}]{dsInfo}");
                            
                            // Se tem dataSource, explorar
                            if (dsInfo.Contains("DS:"))
                            {
                                ExploreDataSourceDeep(current, "  ");
                            }
                            
                            current = current.parent;
                            level++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Trace] Erro: {ex.Message}");
            }
        }
        
        private static void FindAllWithDataSource()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    var withDs = new List<(VisualElement el, object ds)>();
                    FindElementsWithDataSource(root, withDs, 0, 50);
                    
                    Log.LogInfo($"[DS] {withDs.Count} elementos com dataSource não-null");
                    
                    foreach (var (el, ds) in withDs)
                    {
                        Log.LogInfo($"[DS] {el.name}: {ds.GetType().Name}");
                        
                        // Verificar se tem lista
                        var props = ds.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var p in props)
                        {
                            if (p.GetIndexParameters().Length > 0) continue;
                            try
                            {
                                var val = p.GetValue(ds);
                                if (val is IList list && list.Count > 0)
                                {
                                    Log.LogInfo($"[DS]   ⭐ {p.Name}: List<{list.Count}>");
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[DS] Erro: {ex.Message}");
            }
        }
        
        private static void FindElementsWithDataSource(VisualElement element, List<(VisualElement, object)> results, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                var dsProp = element.GetType().GetProperty("dataSource");
                if (dsProp != null)
                {
                    var ds = dsProp.GetValue(element);
                    if (ds != null)
                    {
                        results.Add((element, ds));
                    }
                }
            }
            catch { }
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindElementsWithDataSource(element[i], results, depth + 1, maxDepth);
            }
        }
        
        private static string GetDataSourceInfo(VisualElement element)
        {
            try
            {
                var dsProp = element.GetType().GetProperty("dataSource");
                if (dsProp == null) return "";
                
                var ds = dsProp.GetValue(element);
                if (ds == null) return "";
                
                return $" [DS: {ds.GetType().Name}]";
            }
            catch { }
            return "";
        }
        
        private static void ExploreDataSourceDeep(VisualElement element, string indent)
        {
            try
            {
                var dsProp = element.GetType().GetProperty("dataSource");
                if (dsProp == null) return;
                
                var ds = dsProp.GetValue(element);
                if (ds == null) return;
                
                Log.LogInfo($"[DS] {indent}Tipo: {ds.GetType().FullName}");
                
                var props = ds.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    if (p.Name.Length > 30) continue;
                    
                    try
                    {
                        var val = p.GetValue(ds);
                        if (val == null) continue;
                        
                        if (val is IList list)
                        {
                            Log.LogInfo($"[DS] {indent}{p.Name}: List<{list.Count}> ⭐⭐⭐");
                            
                            if (list.Count > 0)
                            {
                                var first = list[0];
                                if (first != null)
                                {
                                    Log.LogInfo($"[DS] {indent}  Item[0]: {first.GetType().Name}");
                                    
                                    // Mostrar propriedades do primeiro item
                                    var itemProps = first.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                    int count = 0;
                                    foreach (var ip in itemProps)
                                    {
                                        if (ip.GetIndexParameters().Length > 0) continue;
                                        if (count++ > 10) break;
                                        try
                                        {
                                            var iv = ip.GetValue(first);
                                            Log.LogInfo($"[DS] {indent}    {ip.Name}: {iv?.GetType().Name ?? "null"}");
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                        else if (!p.PropertyType.IsPrimitive && p.PropertyType != typeof(string))
                        {
                            Log.LogInfo($"[DS] {indent}{p.Name}: {p.PropertyType.Name}");
                            
                            // Explorar sub-objeto
                            var subProps = val.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            foreach (var sp in subProps)
                            {
                                if (sp.GetIndexParameters().Length > 0) continue;
                                try
                                {
                                    var sv = sp.GetValue(val);
                                    if (sv is IList sl && sl.Count > 0)
                                    {
                                        Log.LogInfo($"[DS] {indent}  {sp.Name}: List<{sl.Count}> ⭐⭐");
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static void FindElementsWithPattern(VisualElement element, List<VisualElement> results, int depth, int maxDepth, Func<VisualElement, bool> predicate)
        {
            if (element == null || depth > maxDepth) return;
            
            if (predicate(element)) results.Add(element);
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindElementsWithPattern(element[i], results, depth + 1, maxDepth, predicate);
            }
        }
        
        private static void FindAndExportTable()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    var found = FindListInTree(root, 0, 50);
                    if (found != null)
                    {
                        Log.LogInfo($"[Export] ✅ {found.Count} itens");
                        ExportCsv(found);
                        return;
                    }
                }
                
                Log.LogWarning("[Export] Nenhum dado encontrado.");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static IList FindListInTree(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return null;
            
            try
            {
                var dsProp = element.GetType().GetProperty("dataSource");
                if (dsProp != null)
                {
                    var ds = dsProp.GetValue(element);
                    if (ds != null)
                    {
                        var list = FindListInObject(ds, 0, 5);
                        if (list != null && list.Count > 0) return list;
                    }
                }
            }
            catch { }
            
            for (int i = 0; i < element.childCount; i++)
            {
                var found = FindListInTree(element[i], depth + 1, maxDepth);
                if (found != null) return found;
            }
            
            return null;
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
                        if (val is IList list && list.Count > 0) return list;
                        
                        if (val != null && !p.PropertyType.IsPrimitive && p.PropertyType != typeof(string))
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
