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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.31.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.31.0 CARREGADO!");
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
                    Log.LogInfo(">>> Ctrl+P - Buscar e exportar");
                    FindAndExport();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Buscar tipos Search/Database/Recruitment");
                    FindSearchTypes();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar elementos UI 'search' e 'database'");
                    FindSearchUIElements();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void FindSearchTypes()
        {
            try
            {
                var keywords = new[] { "search", "database", "recruitment", "shortlist", "scout" };
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                int count = 0;
                
                foreach (var asm in assemblies)
                {
                    var asmName = asm.GetName().Name;
                    if (!asmName.StartsWith("SI.") && !asmName.StartsWith("FM.")) continue;
                    
                    try
                    {
                        var types = asm.GetTypes();
                        foreach (var t in types)
                        {
                            var nameLower = t.Name.ToLower();
                            if (!keywords.Any(k => nameLower.Contains(k))) continue;
                            
                            // Verificar propriedades estáticas
                            var staticProps = t.GetProperties(BindingFlags.Static | BindingFlags.Public);
                            var staticFields = t.GetFields(BindingFlags.Static | BindingFlags.Public);
                            
                            string info = $"{t.Name} ({asmName})";
                            
                            // Verificar se tem dados
                            foreach (var p in staticProps)
                            {
                                try
                                {
                                    var val = p.GetValue(null);
                                    if (val is IEnumerable en && !(val is string))
                                    {
                                        int cnt = 0;
                                        foreach (var item in en) { cnt++; if (cnt >= 10) break; }
                                        if (cnt > 0)
                                        {
                                            info += $" ⭐ static {p.Name}: {cnt}+ itens";
                                        }
                                    }
                                }
                                catch { }
                            }
                            
                            Log.LogInfo($"[Type] {info}");
                            count++;
                            if (count >= 30) return;
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Type] Total: {count} tipos encontrados");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Type] Erro: {ex.Message}");
            }
        }
        
        private static void FindSearchUIElements()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Buscar elementos com nomes relevantes
                    var keywords = new[] { "search", "database", "recruitment", "shortlist", "scout", "player" };
                    var elements = new List<VisualElement>();
                    
                    FindElementsWithKeywords(root, elements, keywords, 0, 50);
                    
                    Log.LogInfo($"[UI] {elements.Count} elementos encontrados");
                    
                    foreach (var el in elements.Take(20))
                    {
                        string info = $"{el.name} ({el.GetType().Name}) [{el.childCount}]";
                        
                        // Verificar propriedades especiais
                        var type = el.GetType();
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        
                        foreach (var p in props)
                        {
                            var pName = p.Name.ToLower();
                            if (pName.Contains("data") || pName.Contains("source") || pName.Contains("item") || pName.Contains("list"))
                            {
                                try
                                {
                                    var val = p.GetValue(el);
                                    if (val != null)
                                    {
                                        info += $" | {p.Name}: {val.GetType().Name}";
                                        
                                        if (val is IEnumerable en && !(val is string))
                                        {
                                            int cnt = 0;
                                            foreach (var item in en) { cnt++; if (cnt >= 10) break; }
                                            if (cnt > 0) info += $" ({cnt}+)";
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        
                        Log.LogInfo($"[UI] {info}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[UI] Erro: {ex.Message}");
            }
        }
        
        private static void FindElementsWithKeywords(VisualElement element, List<VisualElement> results, string[] keywords, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            var nameLower = element.name.ToLower();
            if (keywords.Any(k => nameLower.Contains(k)))
            {
                results.Add(element);
            }
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindElementsWithKeywords(element[i], results, keywords, depth + 1, maxDepth);
            }
        }
        
        private static void FindAndExport()
        {
            try
            {
                // 1. Buscar em tipos estáticos
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                
                foreach (var asm in assemblies)
                {
                    var asmName = asm.GetName().Name;
                    if (!asmName.StartsWith("SI.") && !asmName.StartsWith("FM.")) continue;
                    
                    try
                    {
                        var types = asm.GetTypes();
                        foreach (var t in types)
                        {
                            var staticProps = t.GetProperties(BindingFlags.Static | BindingFlags.Public);
                            foreach (var p in staticProps)
                            {
                                try
                                {
                                    var val = p.GetValue(null);
                                    if (val is IEnumerable en && !(val is string))
                                    {
                                        var list = new List<object>();
                                        foreach (var item in en)
                                        {
                                            list.Add(item);
                                            if (list.Count >= 10000) break;
                                        }
                                        
                                        if (list.Count > 10)
                                        {
                                            var first = list[0];
                                            if (first != null)
                                            {
                                                var itemType = first.GetType();
                                                var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                                
                                                // Verificar se tem propriedades que parecem dados de jogador
                                                var propNames = props.Select(x => x.Name.ToLower()).ToList();
                                                bool hasPlayerData = propNames.Any(n => 
                                                    n.Contains("name") || n.Contains("age") || 
                                                    n.Contains("position") || n.Contains("club") ||
                                                    n.Contains("value") || n.Contains("nation"));
                                                
                                                if (hasPlayerData)
                                                {
                                                    Log.LogInfo($"[Export] {asmName}.{t.Name}.{p.Name}: {list.Count} itens");
                                                    ExportCsv(list);
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
                
                // 2. Buscar nos elementos UI
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    var found = FindEnumerableInUI(root, 0, 50);
                    if (found != null)
                    {
                        var list = new List<object>();
                        foreach (var item in found) { list.Add(item); if (list.Count >= 10000) break; }
                        
                        if (list.Count > 10)
                        {
                            Log.LogInfo($"[Export] UI: {list.Count} itens");
                            ExportCsv(list);
                            return;
                        }
                    }
                }
                
                Log.LogWarning("[Export] Nenhum dado encontrado");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static IEnumerable FindEnumerableInUI(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return null;
            
            var type = element.GetType();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var p in props)
            {
                try
                {
                    var val = p.GetValue(element);
                    if (val is IEnumerable en && !(val is string))
                    {
                        // Verificar se tem itens
                        var list = new List<object>();
                        foreach (var item in en) { list.Add(item); if (list.Count >= 20) break; }
                        if (list.Count > 10) return en;
                    }
                }
                catch { }
            }
            
            for (int i = 0; i < element.childCount; i++)
            {
                var found = FindEnumerableInUI(element[i], depth + 1, maxDepth);
                if (found != null) return found;
            }
            
            return null;
        }
        
        private static void ExportCsv(List<object> data)
        {
            try
            {
                var first = data[0];
                if (first == null) return;
                
                var type = first.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetIndexParameters().Length == 0 && p.Name.Length < 30)
                    .ToList();
                
                var csv = new System.Text.StringBuilder();
                csv.AppendLine(string.Join(";", props.Select(p => p.Name)));
                
                int count = 0;
                foreach (var item in data)
                {
                    if (item == null) continue;
                    
                    var values = props.Select(p =>
                    {
                        try
                        {
                            var val = p.GetValue(item);
                            return (val?.ToString() ?? "").Replace(";", ",").Replace("\n", " ");
                        }
                        catch { return ""; }
                    });
                    
                    csv.AppendLine(string.Join(";", values));
                    count++;
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                Log.LogInfo($"[CSV] ✅ {count} linhas: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[CSV] Erro: {ex.Message}");
            }
        }
    }
}
