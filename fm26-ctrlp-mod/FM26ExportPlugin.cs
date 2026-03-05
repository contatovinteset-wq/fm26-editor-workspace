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
                    Log.LogInfo(">>> Ctrl+P - Exportar da tela atual");
                    ExportFromCurrentScreen();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Mapear elementos com nomes relevantes");
                    MapCurrentScreenSafe();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar tipos C# com 'PlayerSearch'");
                    FindSearchTypes();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void MapCurrentScreenSafe()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Buscar elementos com nomes relevantes - APENAS nomes, sem acessar propriedades
                    var relevant = new List<VisualElement>();
                    FindRelevantElements(root, relevant, 0, 40);
                    
                    Log.LogInfo($"[Map] {relevant.Count} elementos relevantes encontrados");
                    
                    // Mostrar apenas os primeiros 20, sem verificar dados
                    foreach (var el in relevant.Take(20))
                    {
                        try
                        {
                            Log.LogInfo($"[Map] {el.name} ({el.childCount} filhos)");
                        }
                        catch { }
                    }
                    
                    // Agora verificar dados APENAS nos elementos com > 10 filhos (possíveis tabelas)
                    Log.LogInfo($"[Map] === Elementos com muitos filhos (possíveis tabelas) ===");
                    
                    var tables = relevant.Where(e => e.childCount > 10).Take(10);
                    foreach (var el in tables)
                    {
                        try
                        {
                            Log.LogInfo($"[Map] {el.name} ({el.childCount} filhos) - verificando dados...");
                            CheckForDataSafe(el);
                        }
                        catch (Exception ex)
                        {
                            Log.LogError($"[Map] Erro em {el.name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Map] Erro: {ex.Message}");
            }
        }
        
        private static void FindRelevantElements(VisualElement element, List<VisualElement> results, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                var name = element.name.ToLower();
                if (name.Contains("player") || name.Contains("search") || name.Contains("database") ||
                    name.Contains("squad") || name.Contains("roster") || name.Contains("table") ||
                    name.Contains("list") || name.Contains("result") || name.Contains("item") ||
                    name.Contains("row") || name.Contains("column"))
                {
                    results.Add(element);
                }
            }
            catch { }
            
            for (int i = 0; i < element.childCount; i++)
            {
                try
                {
                    FindRelevantElements(element[i], results, depth + 1, maxDepth);
                }
                catch { }
            }
        }
        
        private static void CheckForDataSafe(VisualElement element)
        {
            try
            {
                var type = element.GetType();
                
                // Apenas verificar se existe propriedade dataSource
                var dsProp = type.GetProperty("dataSource", BindingFlags.Public | BindingFlags.Instance);
                if (dsProp != null)
                {
                    try
                    {
                        var ds = dsProp.GetValue(element);
                        if (ds != null)
                        {
                            Log.LogInfo($"[Map]   dataSource: {ds.GetType().Name}");
                            
                            // Verificar se tem IEnumerable
                            if (ds is IEnumerable en && !(ds is string))
                            {
                                int count = 0;
                                foreach (var item in en)
                                {
                                    count++;
                                    if (count >= 500) break;
                                }
                                if (count > 0)
                                {
                                    Log.LogInfo($"[Map]   ⭐⭐⭐ {count} itens encontrados!");
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static void FindSearchTypes()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name.StartsWith("SI.") || a.GetName().Name.StartsWith("FM."))
                    .ToList();
                
                var found = new List<Type>();
                
                foreach (var asm in assemblies)
                {
                    try
                    {
                        var types = asm.GetTypes();
                        foreach (var t in types)
                        {
                            var name = t.Name.ToLower();
                            if (name.Contains("playersearch") || name.Contains("searchresult") ||
                                name.Contains("playerdata") || name.Contains("searchdata"))
                            {
                                found.Add(t);
                            }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Type] {found.Count} tipos encontrados");
                
                foreach (var t in found.Take(15))
                {
                    try
                    {
                        var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        Log.LogInfo($"[Type] {t.Name} [{props.Length} props]");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Type] Erro: {ex.Message}");
            }
        }
        
        private static void ExportFromCurrentScreen()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Buscar dados em toda a árvore
                    var data = FindDataInTree(root, 0, 50);
                    if (data != null && data.Count > 0)
                    {
                        Log.LogInfo($"[Export] Encontrado: {data.Count} itens");
                        ExportCsv(data);
                        return;
                    }
                }
                
                Log.LogWarning("[Export] Nenhum dado encontrado na tela atual");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static IList FindDataInTree(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return null;
            
            try
            {
                var type = element.GetType();
                var dsProp = type.GetProperty("dataSource", BindingFlags.Public | BindingFlags.Instance);
                
                if (dsProp != null)
                {
                    try
                    {
                        var val = dsProp.GetValue(element);
                        if (val is IEnumerable en && !(val is string))
                        {
                            var list = new List<object>();
                            foreach (var item in en)
                            {
                                list.Add(item);
                                if (list.Count >= 10000) break;
                            }
                            
                            if (list.Count > 5)
                            {
                                var first = list[0];
                                if (first != null)
                                {
                                    var itemProps = first.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                    if (itemProps.Length >= 3)
                                    {
                                        return list;
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            
            for (int i = 0; i < element.childCount; i++)
            {
                try
                {
                    var found = FindDataInTree(element[i], depth + 1, maxDepth);
                    if (found != null) return found;
                }
                catch { }
            }
            
            return null;
        }
        
        private static void ExportCsv(IList data)
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
