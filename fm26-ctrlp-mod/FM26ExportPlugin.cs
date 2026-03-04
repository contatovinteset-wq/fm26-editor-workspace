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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.24.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.24.0 CARREGADO!");
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
                    Log.LogInfo(">>> F9 - Investigar PlayerTable (propriedades e campos)");
                    InvestigatePlayerTable();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar TODOS elementos com 'PlayerTable' ou 'playertable'");
                    FindPlayerTables();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigatePlayerTable()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Buscar PlayerTable
                    var playerTables = new List<VisualElement>();
                    FindElementsWithPattern(root, playerTables, 0, 50, 
                        e => e.name == "PlayerTable" || e.name == "playertable");
                    
                    Log.LogInfo($"[PT] Encontrados {playerTables.Count} elementos PlayerTable");
                    
                    foreach (var pt in playerTables)
                    {
                        Log.LogInfo($"[PT] === {pt.name} ({pt.childCount} filhos) ===");
                        
                        var type = pt.GetType();
                        Log.LogInfo($"[PT] Tipo real: {type.FullName}");
                        
                        // TODAS as propriedades
                        Log.LogInfo($"[PT] --- PROPRIEDADES ---");
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        foreach (var p in props)
                        {
                            if (p.Name.Length > 35) continue;
                            try
                            {
                                var val = p.GetValue(pt);
                                string valType = val?.GetType().Name ?? "null";
                                string valStr = val?.ToString() ?? "null";
                                if (valStr.Length > 50) valStr = valStr.Substring(0, 50) + "...";
                                
                                // Destacar possíveis dados
                                bool highlight = p.Name.ToLower().Contains("item") ||
                                                p.Name.ToLower().Contains("row") ||
                                                p.Name.ToLower().Contains("data") ||
                                                p.Name.ToLower().Contains("source") ||
                                                p.Name.ToLower().Contains("list") ||
                                                p.Name.ToLower().Contains("bind");
                                
                                Log.LogInfo($"[PT]   {(highlight ? "⭐ " : "  ")}{p.Name}: {valType} = {valStr}");
                                
                                // Se é lista, mostrar count
                                if (val is IList list)
                                {
                                    Log.LogInfo($"[PT]     → IList com {list.Count} itens!");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.LogInfo($"[PT]   {p.Name}: ERRO - {ex.Message}");
                            }
                        }
                        
                        // TODOS os campos
                        Log.LogInfo($"[PT] --- CAMPOS ---");
                        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        foreach (var f in fields)
                        {
                            if (f.Name.Length > 35) continue;
                            if (f.Name.StartsWith("<") || f.Name.StartsWith("k__BackingField")) continue;
                            try
                            {
                                var val = f.GetValue(pt);
                                string valType = val?.GetType().Name ?? "null";
                                string valStr = val?.ToString() ?? "null";
                                if (valStr.Length > 50) valStr = valStr.Substring(0, 50) + "...";
                                
                                bool highlight = f.Name.ToLower().Contains("item") ||
                                                f.Name.ToLower().Contains("row") ||
                                                f.Name.ToLower().Contains("data") ||
                                                f.Name.ToLower().Contains("source") ||
                                                f.Name.ToLower().Contains("list") ||
                                                f.Name.ToLower().Contains("bind");
                                
                                Log.LogInfo($"[PT]   {(highlight ? "⭐ " : "  ")}{f.Name}: {valType} = {valStr}");
                                
                                if (val is IList list)
                                {
                                    Log.LogInfo($"[PT]     → IList com {list.Count} itens!");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.LogInfo($"[PT]   {f.Name}: ERRO - {ex.Message}");
                            }
                        }
                        
                        // Filhos
                        Log.LogInfo($"[PT] --- FILHOS ({pt.childCount}) ---");
                        for (int i = 0; i < pt.childCount && i < 10; i++)
                        {
                            var child = pt[i];
                            Log.LogInfo($"[PT]   [{i}] {child.name} ({child.GetType().Name}) [{child.childCount}]");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[PT] Erro: {ex.Message}");
            }
        }
        
        private static void FindPlayerTables()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Buscar todos os elementos com "player" e "table" no nome
                    var allElements = new List<VisualElement>();
                    FindAllElements(root, allElements, 0, 50);
                    
                    var playerRelated = allElements.Where(e => 
                        e.name.ToLower().Contains("player") || 
                        e.name.ToLower().Contains("table") ||
                        e.name.ToLower().Contains("squad") ||
                        e.name.ToLower().Contains("list")).ToList();
                    
                    Log.LogInfo($"[Find] {playerRelated.Count} elementos relacionados a player/table");
                    
                    foreach (var el in playerRelated.Take(30))
                    {
                        // Verificar se tem IList em alguma propriedade
                        var type = el.GetType();
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        bool hasList = false;
                        string listInfo = "";
                        
                        foreach (var p in props)
                        {
                            try
                            {
                                var val = p.GetValue(el);
                                if (val is IList list && list.Count > 0)
                                {
                                    hasList = true;
                                    listInfo = $" → {p.Name}: List<{list.Count}>";
                                    break;
                                }
                            }
                            catch { }
                        }
                        
                        Log.LogInfo($"[Find] {el.name} ({el.childCount}){(hasList ? listInfo : "")}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Find] Erro: {ex.Message}");
            }
        }
        
        private static void FindAllElements(VisualElement element, List<VisualElement> results, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            results.Add(element);
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindAllElements(element[i], results, depth + 1, maxDepth);
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
                    
                    // Buscar qualquer elemento com lista
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
            
            var type = element.GetType();
            
            // Verificar propriedades
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var p in props)
            {
                try
                {
                    var val = p.GetValue(element);
                    if (val is IList list && list.Count > 0) return list;
                    
                    // Explorar sub-objeto
                    if (val != null && !p.PropertyType.IsPrimitive && p.PropertyType != typeof(string))
                    {
                        var subList = FindListInObject(val, 0, 3);
                        if (subList != null) return subList;
                    }
                }
                catch { }
            }
            
            // Verificar campos
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                if (f.Name.StartsWith("<")) continue;
                try
                {
                    var val = f.GetValue(element);
                    if (val is IList list && list.Count > 0) return list;
                    
                    if (val != null && !f.FieldType.IsPrimitive && f.FieldType != typeof(string))
                    {
                        var subList = FindListInObject(val, 0, 3);
                        if (subList != null) return subList;
                    }
                }
                catch { }
            }
            
            // Recursão nos filhos
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
