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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.22.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.22.0 CARREGADO!");
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
                    Log.LogInfo(">>> F9 - Buscar TABELA (column-headers/column-footers)");
                    FindTableColumns();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Investigar elemento específico");
                    InvestigateSpecificElement();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void FindTableColumns()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Buscar elementos com "column" no nome
                    var columnElements = new List<VisualElement>();
                    FindElementsWithPattern(root, columnElements, 0, 50, 
                        e => e.name.Contains("column") || e.name.Contains("Column"));
                    
                    Log.LogInfo($"[Table] Encontrados {columnElements.Count} elementos com 'column'");
                    
                    // Para cada elemento column, buscar o pai e explorar
                    foreach (var col in columnElements)
                    {
                        Log.LogInfo($"[Table] === {col.name} ({col.childCount} filhos) ===");
                        
                        // Explorar pai
                        var parent = col.parent;
                        if (parent != null)
                        {
                            Log.LogInfo($"[Table] Pai: {parent.name} ({parent.childCount} filhos)");
                            
                            // Verificar dataSource do pai
                            ExploreDataSourceDeep(parent, "  ");
                            
                            // Mostrar TODOS os filhos do pai
                            Log.LogInfo($"[Table] Filhos do pai:");
                            for (int i = 0; i < parent.childCount && i < 25; i++)
                            {
                                var sibling = parent[i];
                                string dsInfo = GetDataSourceSummary(sibling);
                                Log.LogInfo($"[Table]   [{i}] {sibling.name} ({sibling.GetType().Name}) [{sibling.childCount}]{dsInfo}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Table] Erro: {ex.Message}");
            }
        }
        
        private static void ExploreDataSourceDeep(VisualElement element, string indent)
        {
            try
            {
                var dsProp = element.GetType().GetProperty("dataSource");
                if (dsProp == null) return;
                
                var ds = dsProp.GetValue(element);
                if (ds == null)
                {
                    Log.LogInfo($"[DS] {indent}dataSource: null");
                    return;
                }
                
                Log.LogInfo($"[DS] {indent}dataSource: {ds.GetType().FullName}");
                
                // Explorar todas as propriedades do dataSource
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
                            Log.LogInfo($"[DS] {indent}  {p.Name}: List<{list.Count}> ⭐⭐⭐");
                            
                            // Se encontrou lista, explorar primeiro item
                            if (list.Count > 0)
                            {
                                var first = list[0];
                                if (first != null)
                                {
                                    Log.LogInfo($"[DS] {indent}    Primeiro: {first.GetType().Name}");
                                    ShowProperties(first, indent + "    ");
                                }
                            }
                        }
                        else if (!p.PropertyType.IsPrimitive && p.PropertyType != typeof(string))
                        {
                            // Explorar sub-objeto
                            Log.LogInfo($"[DS] {indent}  {p.Name}: {p.PropertyType.Name}");
                            ExploreSubObject(val, indent + "    ", 1);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static void ExploreSubObject(object obj, string indent, int depth)
        {
            if (obj == null || depth > 3) return;
            
            try
            {
                var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    if (p.Name.Length > 25) continue;
                    
                    try
                    {
                        var val = p.GetValue(obj);
                        if (val is IList list && list.Count > 0)
                        {
                            Log.LogInfo($"[DS] {indent}{p.Name}: List<{list.Count}> ⭐⭐");
                        }
                        else if (val != null && !p.PropertyType.IsPrimitive && p.PropertyType != typeof(string) && depth < 3)
                        {
                            Log.LogInfo($"[DS] {indent}{p.Name}: {p.PropertyType.Name}");
                            ExploreSubObject(val, indent + "  ", depth + 1);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static void ShowProperties(object obj, string indent)
        {
            if (obj == null) return;
            
            try
            {
                var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                int count = 0;
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    if (count++ > 15) break;
                    
                    try
                    {
                        var val = p.GetValue(obj);
                        Log.LogInfo($"[DS] {indent}{p.Name}: {val?.GetType().Name ?? "null"}");
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static string GetDataSourceSummary(VisualElement element)
        {
            try
            {
                var dsProp = element.GetType().GetProperty("dataSource");
                if (dsProp == null) return "";
                
                var ds = dsProp.GetValue(element);
                if (ds == null) return "";
                
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
                            return $" [DS: List<{list.Count}>]";
                        }
                    }
                    catch { }
                }
                
                return $" [DS: {ds.GetType().Name}]";
            }
            catch { }
            return "";
        }
        
        private static void InvestigateSpecificElement()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Buscar elementos com dataSource
                    var withDs = new List<VisualElement>();
                    FindElementsWithDataSource(root, withDs, 0, 40);
                    
                    Log.LogInfo($"[Inv] {withDs.Count} elementos com dataSource");
                    
                    foreach (var el in withDs)
                    {
                        var dsProp = el.GetType().GetProperty("dataSource");
                        if (dsProp == null) continue;
                        
                        var ds = dsProp.GetValue(el);
                        if (ds == null) continue;
                        
                        // Verificar se tem lista
                        var hasList = false;
                        var listInfo = "";
                        
                        var props = ds.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var p in props)
                        {
                            if (p.GetIndexParameters().Length > 0) continue;
                            try
                            {
                                var val = p.GetValue(ds);
                                if (val is IList list && list.Count > 0)
                                {
                                    hasList = true;
                                    listInfo = $"List<{list.Count}>";
                                    break;
                                }
                            }
                            catch { }
                        }
                        
                        if (hasList)
                        {
                            Log.LogInfo($"[Inv] ⭐⭐⭐ {el.name}: {listInfo}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Inv] Erro: {ex.Message}");
            }
        }
        
        private static void FindElementsWithDataSource(VisualElement element, List<VisualElement> results, int depth, int maxDepth)
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
                        results.Add(element);
                    }
                }
            }
            catch { }
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindElementsWithDataSource(element[i], results, depth + 1, maxDepth);
            }
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
                    
                    // Buscar qualquer elemento com dataSource que contenha lista
                    var found = FindListInTree(root, 0, 45);
                    if (found != null)
                    {
                        Log.LogInfo($"[Export] ✅ Encontrado: {found.Count} itens");
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
            
            // Verificar dataSource
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
            
            // Recursão
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
