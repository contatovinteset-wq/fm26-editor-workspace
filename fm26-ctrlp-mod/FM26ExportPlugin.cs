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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.7.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.7.0 CARREGADO!");
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
                
                // F9 - DUMP COMPLETO
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - DUMP COMPLETO");
                    DumpEverything();
                }
                
                // F10 - Buscar dataSource
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar dataSource");
                    FindAllDataSources();
                }
                
                // F12 - Diagnóstico simples
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
        
        // F9 - DUMP COMPLETO de toda a hierarquia
        private static void DumpEverything()
        {
            try
            {
                var report = FindReportElement();
                if (report == null)
                {
                    Log.LogWarning("[F9] Report não encontrado");
                    return;
                }
                
                Log.LogInfo("[F9] ===== DUMP COMPLETO =====");
                DumpElement(report, 0, 10);
                Log.LogInfo("[F9] ===== FIM DO DUMP =====");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F9] Erro: {ex.Message}");
            }
        }
        
        private static void DumpElement(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                string indent = new string(' ', depth * 2);
                string name = element.name ?? "(null)";
                string typeName = element.GetType().Name;
                int childCount = element.childCount;
                
                // Mostrar elemento
                Log.LogInfo($"[F9] {indent}{name} ({typeName}) [{childCount} filhos]");
                
                // Se tiver dataSource, mostrar
                try
                {
                    var dsProp = typeof(VisualElement).GetProperty("dataSource", BindingFlags.Public | BindingFlags.Instance);
                    if (dsProp != null)
                    {
                        var ds = dsProp.GetValue(element);
                        if (ds != null)
                        {
                            string dsType = ds.GetType().FullName;
                            if (ds is IList list)
                            {
                                Log.LogInfo($"[F9] {indent}  >>> dataSource: {dsType} ({list.Count} itens)");
                            }
                            else
                            {
                                Log.LogInfo($"[F9] {indent}  >>> dataSource: {dsType}");
                            }
                        }
                    }
                }
                catch { }
                
                // Mostrar TODAS as propriedades no nível 3-4 (mais fundo)
                if (depth >= 3 && depth <= 5)
                {
                    try
                    {
                        var props = element.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var prop in props)
                        {
                            if (prop.GetIndexParameters().Length > 0) continue;
                            if (prop.Name == "name" || prop.Name == "childCount") continue;
                            
                            try
                            {
                                var value = prop.GetValue(element);
                                if (value == null) continue;
                                
                                string valueStr = value.ToString();
                                if (valueStr.Length > 50) valueStr = valueStr.Substring(0, 50) + "...";
                                
                                // Filtrar propriedades interessantes
                                if (value is IList || 
                                    prop.Name.Contains("Data") || 
                                    prop.Name.Contains("List") ||
                                    prop.Name.Contains("Item") ||
                                    prop.Name.Contains("Source"))
                                {
                                    string listInfo = value is IList l ? $" ({l.Count} itens)" : "";
                                    Log.LogInfo($"[F9] {indent}  {prop.Name}: {valueStr}{listInfo}");
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                
                // Recursão nos filhos
                for (int i = 0; i < childCount && i < 50; i++)
                {
                    DumpElement(element[i], depth + 1, maxDepth);
                }
            }
            catch { }
        }
        
        // F10 - Buscar TODOS os dataSources
        private static void FindAllDataSources()
        {
            try
            {
                var report = FindReportElement();
                if (report == null)
                {
                    Log.LogWarning("[F10] Report não encontrado");
                    return;
                }
                
                int found = 0;
                FindDataSourcesRecursive(report, ref found, 0, 15);
                Log.LogInfo($"[F10] Total dataSources encontrados: {found}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F10] Erro: {ex.Message}");
            }
        }
        
        private static void FindDataSourcesRecursive(VisualElement element, ref int count, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
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
                            string name = element.name ?? "(null)";
                            string dsType = ds.GetType().FullName;
                            
                            if (ds is IList list)
                            {
                                Log.LogInfo($"[F10] ✅ {name}: {dsType} ({list.Count} itens)");
                                
                                // Se tem itens, mostrar o tipo do primeiro
                                if (list.Count > 0 && list[0] != null)
                                {
                                    Log.LogInfo($"[F10]    Primeiro item: {list[0].GetType().FullName}");
                                }
                            }
                            else
                            {
                                Log.LogInfo($"[F10] 📌 {name}: {dsType}");
                            }
                        }
                    }
                    catch { }
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 50; i++)
                {
                    FindDataSourcesRecursive(element[i], ref count, depth + 1, maxDepth);
                }
            }
            catch { }
        }
        
        // F12 - Diagnóstico simples
        private static void Diagnose()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[F12] {uiDocs.Length} UIDocuments");
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    Log.LogInfo($"[F12]   {doc.name}: {doc.rootVisualElement?.childCount ?? 0} filhos");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[F12] Erro: {ex.Message}");
            }
        }
        
        // Ctrl+P - Exportar
        private static void SafeExport()
        {
            try
            {
                Log.LogInfo("[Export] Buscando dados...");
                
                var report = FindReportElement();
                if (report == null)
                {
                    Log.LogWarning("[Export] Report não encontrado");
                    return;
                }
                
                // Procurar dataSource com dados
                IList foundData = null;
                string foundElement = "";
                FindDataForExport(report, ref foundData, ref foundElement, 0, 15);
                
                if (foundData != null && foundData.Count > 0)
                {
                    Log.LogInfo($"[Export] ✅ Encontrado {foundData.Count} itens em {foundElement}");
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
                    var ds = dsProp.GetValue(element);
                    if (ds is IList list && list.Count > 0)
                    {
                        foundData = list;
                        foundName = element.name ?? "(null)";
                        return;
                    }
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 50; i++)
                {
                    FindDataForExport(element[i], ref foundData, ref foundName, depth + 1, maxDepth);
                    if (foundData != null) return;
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
                        var child = root[i];
                        if (child?.name == "Report")
                        {
                            return child;
                        }
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
