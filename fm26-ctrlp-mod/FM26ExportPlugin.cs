using System;
using System.Reflection;
using System.Collections;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.6.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.6.0 CARREGADO!");
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
            _frameCount++;
            
            if (!_initialized && _frameCount == 300)
            {
                _initialized = true;
                Log.LogInfo("[Init] Plugin pronto");
            }
            
            if (!_initialized) return;
            if (Keyboard.current == null) return;
            
            bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
            bool p = Keyboard.current.pKey.wasPressedThisFrame;
            
            // Ctrl+P - EXPORTAR (seguro)
            if (ctrl && p)
            {
                Log.LogInfo(">>> Ctrl+P - EXPORTAR");
                SafeExport();
            }
            
            // F9 - Listar estrutura do Report
            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                Log.LogInfo(">>> F9 - Listar Report");
                ListReportStructure();
            }
            
            // F10 - Buscar elementos com dados
            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                Log.LogInfo(">>> F10 - Buscar dados");
                FindDataElements();
            }
        }
        
        // F9 - Listar estrutura do Report (seguro, só nomes)
        private static void ListReportStructure()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[Report] {uiDocs.Length} UIDocuments");
                
                foreach (var doc in uiDocs)
                {
                    if (doc?.name != "PanelManager") continue;
                    
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Procurar Report
                    for (int i = 0; i < root.childCount; i++)
                    {
                        var child = root[i];
                        if (child?.name == "Report")
                        {
                            Log.LogInfo($"[Report] Encontrado Report com {child.childCount} filhos");
                            
                            // Listar até 10 filhos diretos
                            for (int j = 0; j < child.childCount && j < 10; j++)
                            {
                                var sub = child[j];
                                if (sub != null)
                                {
                                    Log.LogInfo($"[Report]   [{j}] {sub.name} ({sub.GetType().Name})");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Report] Erro: {ex.Message}");
            }
        }
        
        // F10 - Buscar elementos com dados (seguro, só nomes)
        private static void FindDataElements()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                int found = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    found += ScanForData(root, 0, 8);
                }
                
                Log.LogInfo($"[FindData] Total encontrados: {found}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[FindData] Erro: {ex.Message}");
            }
        }
        
        private static int ScanForData(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return 0;
            
            int found = 0;
            
            try
            {
                string name = element.name ?? "";
                string typeName = element.GetType().Name ?? "";
                
                // Procurar por nomes relevantes
                bool hasData = name.Contains("Streamed") ||
                               name.Contains("Table") ||
                               name.Contains("List") ||
                               name.Contains("Player") ||
                               name.Contains("Squad");
                
                if (hasData)
                {
                    found++;
                    Log.LogInfo($"[FindData] {' ',depth*2}⭐ {name} ({typeName})");
                }
                
                // Recursão limitada
                for (int i = 0; i < element.childCount && i < 20; i++)
                {
                    found += ScanForData(element[i], depth + 1, maxDepth);
                }
            }
            catch { }
            
            return found;
        }
        
        // Ctrl+P - Exportação segura (sem crash)
        private static void SafeExport()
        {
            try
            {
                Log.LogInfo("[Export] Buscando dados...");
                
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc?.name != "PanelManager") continue;
                    
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Procurar Report
                    for (int i = 0; i < root.childCount; i++)
                    {
                        var child = root[i];
                        if (child?.name == "Report")
                        {
                            Log.LogInfo("[Export] Report encontrado");
                            
                            // Procurar sub-elemento com dados
                            for (int j = 0; j < child.childCount; j++)
                            {
                                var sub = child[j];
                                if (sub == null) continue;
                                
                                string subName = sub.name ?? "";
                                
                                // PlayerSearchReport, TeamSquadReport, etc.
                                if (subName.Contains("Report") || subName.Contains("Search") || subName.Contains("Squad"))
                                {
                                    Log.LogInfo($"[Export] Sub-elemento: {subName}");
                                    
                                    // Verificar se tem dataSource
                                    var ds = sub.GetType().GetProperty("dataSource");
                                    if (ds != null)
                                    {
                                        try
                                        {
                                            var value = ds.GetValue(sub);
                                            if (value != null)
                                            {
                                                Log.LogInfo($"[Export] dataSource: {value.GetType().Name}");
                                                
                                                // Se for lista, exportar
                                                if (value is IList list && list.Count > 0)
                                                {
                                                    Log.LogInfo($"[Export] ✅ Encontrado IList com {list.Count} itens!");
                                                    ExportListToCsv(list);
                                                    return;
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
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
        
        private static void ExportListToCsv(IList list)
        {
            try
            {
                if (list == null || list.Count == 0) return;
                
                var first = list[0];
                if (first == null) return;
                
                var type = first.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                // Preparar CSV
                var csv = new System.Text.StringBuilder();
                var headers = new System.Collections.Generic.List<string>();
                
                // Headers
                foreach (var prop in props)
                {
                    if (prop.GetIndexParameters().Length == 0 && prop.Name.Length < 30)
                    {
                        headers.Add(prop.Name);
                    }
                }
                csv.AppendLine(string.Join(";", headers));
                
                // Dados
                int count = 0;
                foreach (var item in list)
                {
                    if (item == null) continue;
                    
                    var values = new System.Collections.Generic.List<string>();
                    foreach (var prop in props)
                    {
                        if (prop.GetIndexParameters().Length > 0) continue;
                        if (!headers.Contains(prop.Name)) continue;
                        
                        try
                        {
                            var val = prop.GetValue(item);
                            string str = val?.ToString()?.Replace(";", ",") ?? "";
                            if (str.Length > 100) str = str.Substring(0, 100);
                            values.Add(str);
                        }
                        catch
                        {
                            values.Add("");
                        }
                    }
                    csv.AppendLine(string.Join(";", values));
                    count++;
                }
                
                // Salvar
                var path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                
                Log.LogInfo($"[Export] ✅ {count} linhas salvas em {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro CSV: {ex.Message}");
            }
        }
    }
}
