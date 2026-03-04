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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.28.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.28.0 CARREGADO!");
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
                    TryExport();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Buscar tipos com IList em SI.*");
                    FindTypesWithList();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar dados em BindingRemapper/tables");
                    FindBindingData();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void FindTypesWithList()
        {
            try
            {
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
                            // Verificar se tem propriedade com IList
                            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            foreach (var p in props)
                            {
                                if (typeof(IList).IsAssignableFrom(p.PropertyType))
                                {
                                    Log.LogInfo($"[Type] {t.Name}.{p.Name}: {p.PropertyType.Name} ({asmName})");
                                    count++;
                                    if (count >= 30) return;
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Type] Total: {count} tipos com IList");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Type] Erro: {ex.Message}");
            }
        }
        
        private static void FindBindingData()
        {
            try
            {
                // Buscar qualquer objeto que tenha "tables", "squad", "players" no nome
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
                            var nameLower = t.Name.ToLower();
                            if (!nameLower.Contains("table") && !nameLower.Contains("squad") && 
                                !nameLower.Contains("player") && !nameLower.Contains("list")) continue;
                            
                            // Propriedades estáticas
                            var staticProps = t.GetProperties(BindingFlags.Static | BindingFlags.Public);
                            foreach (var p in staticProps)
                            {
                                try
                                {
                                    var val = p.GetValue(null);
                                    if (val != null)
                                    {
                                        if (val is IList list && list.Count > 0)
                                        {
                                            Log.LogInfo($"[Bind] ⭐⭐⭐ {t.Name}.{p.Name}: List<{list.Count}>");
                                            ShowFirstItem(list);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
                
                // Buscar nos elementos UI também
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Buscar elementos com "tables" no nome
                    var tables = new List<VisualElement>();
                    FindElementsWithName(root, tables, "tables");
                    
                    foreach (var t in tables)
                    {
                        Log.LogInfo($"[Bind] UI: {t.name} ({t.childCount} filhos)");
                        
                        // Explorar filhos
                        for (int i = 0; i < t.childCount && i < 5; i++)
                        {
                            var child = t[i];
                            Log.LogInfo($"[Bind]   [{i}] {child.name} ({child.GetType().Name})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Bind] Erro: {ex.Message}");
            }
        }
        
        private static void ShowFirstItem(IList list)
        {
            try
            {
                if (list.Count == 0) return;
                var first = list[0];
                if (first == null) return;
                
                var type = first.GetType();
                Log.LogInfo($"[Bind]   Item[0]: {type.Name}");
                
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var p in props.Take(10))
                {
                    try
                    {
                        var val = p.GetValue(first);
                        string valStr = val?.ToString() ?? "null";
                        if (valStr.Length > 30) valStr = valStr.Substring(0, 30) + "...";
                        Log.LogInfo($"[Bind]     {p.Name}: {valStr}");
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static void FindElementsWithName(VisualElement element, List<VisualElement> results, string namePart)
        {
            if (element == null) return;
            if (element.name.ToLower().Contains(namePart)) results.Add(element);
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindElementsWithName(element[i], results, namePart);
            }
        }
        
        private static void TryExport()
        {
            try
            {
                // Buscar qualquer lista em qualquer lugar
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                
                foreach (var asm in assemblies)
                {
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
                                    if (val is IList list && list.Count > 5)
                                    {
                                        // Verificar se parece ter dados
                                        var first = list[0];
                                        if (first != null)
                                        {
                                            var props = first.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                            if (props.Length >= 3)
                                            {
                                                Log.LogInfo($"[Export] {t.Name}.{p.Name}: {list.Count} itens");
                                                ExportCsv(list);
                                                return;
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
                
                Log.LogWarning("[Export] Nenhum dado encontrado");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
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
                    
                    if (count >= 10000) break; // Limite
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
