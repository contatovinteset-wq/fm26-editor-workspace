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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.36.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        internal static object _bindingsInstance = null;
        internal static Type _bindingsType = null;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.36.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            // Buscar tipo Bindings em todos assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = asm.GetTypes();
                    foreach (var t in types)
                    {
                        if (t.Name == "Bindings" && t.Namespace == "")
                        {
                            _bindingsType = t;
                            Log.LogInfo($"[Init] Bindings encontrado: {t.FullName} em {asm.GetName().Name}");
                            
                            // Hook no Update
                            var harmony = new Harmony("com.koda.fm26.ctrlp");
                            var updateMethod = t.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
                            if (updateMethod != null)
                            {
                                var patchMethod = typeof(Plugin).GetMethod("OnUpdate", BindingFlags.Static | BindingFlags.Public);
                                harmony.Patch(updateMethod, postfix: new HarmonyMethod(patchMethod));
                                Log.LogInfo("[Init] Patched Bindings.Update");
                            }
                            break;
                        }
                    }
                }
                catch { }
            }
            
            if (_bindingsType == null)
            {
                Log.LogWarning("[Init] Bindings não encontrado, tentando via SI.Bindable.Bindings");
                
                var altType = Type.GetType("SI.Bindable.Bindings, SI.Bindable");
                if (altType != null)
                {
                    _bindingsType = altType;
                    Log.LogInfo($"[Init] Tipo alternativo: {altType.FullName}");
                    
                    var harmony = new Harmony("com.koda.fm26.ctrlp");
                    var updateMethod = altType.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
                    if (updateMethod != null)
                    {
                        var patchMethod = typeof(Plugin).GetMethod("OnUpdate", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(updateMethod, postfix: new HarmonyMethod(patchMethod));
                        Log.LogInfo("[Init] Patched SI.Bindable.Bindings.Update");
                    }
                }
            }
        }
        
        private static int _frameCount = 0;
        private static bool _initialized = false;
        
        public static void OnUpdate(object __instance)
        {
            try
            {
                // Capturar instância
                if (_bindingsInstance == null && __instance != null)
                {
                    _bindingsInstance = __instance;
                    Log.LogInfo($"[Hook] Bindings capturada!");
                }
                
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
                    Log.LogInfo(">>> Ctrl+P - Exportar via StreamedTable");
                    ExportViaStreamedTable();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Listar Bindings.DataSet");
                    ListBindingsDataSet();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Investigar StreamedTable");
                    InvestigateStreamedTable();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void ListBindingsDataSet()
        {
            try
            {
                if (_bindingsInstance == null)
                {
                    Log.LogWarning("[Bind] Bindings não capturada");
                    return;
                }
                
                var type = _bindingsInstance.GetType();
                Log.LogInfo($"[Bind] Tipo: {type.Name}");
                
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[Bind] {props.Length} propriedades:");
                
                foreach (var p in props)
                {
                    Log.LogInfo($"[Bind]   {p.Name}: {p.PropertyType.Name}");
                }
                
                // DataSet
                var dataSetProp = props.FirstOrDefault(p => p.Name == "DataSet");
                if (dataSetProp != null)
                {
                    var dataSet = dataSetProp.GetValue(_bindingsInstance);
                    if (dataSet is IEnumerable en)
                    {
                        int count = 0;
                        foreach (var _ in en) { count++; if (count >= 10000) break; }
                        Log.LogInfo($"[Bind] DataSet: {count} itens");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Bind] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigateStreamedTable()
        {
            try
            {
                // Buscar StreamedTable
                var streamedTableType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.Name == "StreamedTable");
                
                if (streamedTableType == null)
                {
                    Log.LogWarning("[ST] StreamedTable não encontrado");
                    return;
                }
                
                Log.LogInfo($"[ST] Tipo: {streamedTableType.FullName}");
                
                // Propriedades
                var props = streamedTableType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[ST] {props.Length} propriedades:");
                
                foreach (var p in props)
                {
                    Log.LogInfo($"[ST]   {p.Name}: {p.PropertyType.Name}");
                }
                
                // Campos
                var fields = streamedTableType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[ST] {fields.Length} campos públicos:");
                
                foreach (var f in fields)
                {
                    Log.LogInfo($"[ST]   {f.Name}: {f.FieldType.Name}");
                }
                
                // Buscar instâncias ativas
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.rootVisualElement == null) continue;
                    
                    var tables = new List<VisualElement>();
                    FindElementsOfType(doc.rootVisualElement, "StreamedTable", tables, 0, 30);
                    
                    if (tables.Count > 0)
                    {
                        Log.LogInfo($"[ST] {tables.Count} StreamedTables encontrados em {doc.name}");
                        
                        foreach (var table in tables.Take(3))
                        {
                            Log.LogInfo($"[ST]   {table.name} ({table.childCount} filhos)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[ST] Erro: {ex.Message}");
            }
        }
        
        private static void FindElementsOfType(VisualElement element, string typeName, List<VisualElement> results, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            if (element.GetType().Name == typeName)
            {
                results.Add(element);
            }
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindElementsOfType(element[i], typeName, results, depth + 1, maxDepth);
            }
        }
        
        private static void ExportViaStreamedTable()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.rootVisualElement == null) continue;
                    
                    var tables = new List<VisualElement>();
                    FindElementsOfType(doc.rootVisualElement, "StreamedTable", tables, 0, 40);
                    FindElementsOfType(doc.rootVisualElement, "StreamedListView", tables, 0, 40);
                    
                    foreach (var table in tables)
                    {
                        Log.LogInfo($"[Export] {table.GetType().Name}: {table.name}");
                        
                        // Verificar propriedades de dados
                        var type = table.GetType();
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        
                        foreach (var p in props)
                        {
                            var name = p.Name.ToLower();
                            if (!name.Contains("data") && !name.Contains("source") && 
                                !name.Contains("item") && !name.Contains("list")) continue;
                            
                            try
                            {
                                var val = p.GetValue(table);
                                if (val == null) continue;
                                
                                Log.LogInfo($"[Export]   {p.Name}: {val.GetType().Name}");
                                
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
                                        Log.LogInfo($"[Export]   ⭐ {list.Count} itens!");
                                        
                                        // Verificar tipo do primeiro item
                                        var first = list[0];
                                        if (first != null)
                                        {
                                            Log.LogInfo($"[Export]   Item tipo: {first.GetType().Name}");
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
                
                Log.LogWarning("[Export] Nenhum dado encontrado");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
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
