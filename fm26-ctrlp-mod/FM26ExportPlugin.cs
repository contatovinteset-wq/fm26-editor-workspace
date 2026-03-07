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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.56.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.56.0");
            Log.LogInfo("Player Database -> Ctrl+P");
            Log.LogInfo("========================================");
            
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            
            try
            {
                var bindingsType = Type.GetType("SI.Bindable.Bindings, SI.Bindable");
                if (bindingsType != null)
                {
                    var updateMethod = bindingsType.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
                    if (updateMethod != null)
                    {
                        var patchMethod = typeof(Plugin).GetMethod("OnUpdate", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(updateMethod, postfix: new HarmonyMethod(patchMethod));
                        Log.LogInfo("[OK] Hook ativo");
                    }
                }
            }
            catch { }
        }
        
        private static object _bindingsInstance = null;
        private static int _frameCount = 0;
        private static bool _initialized = false;
        private static List<string[]> _tableData = null;
        
        public static void OnUpdate(object __instance)
        {
            try
            {
                if (_bindingsInstance == null && __instance != null) _bindingsInstance = __instance;
                
                _frameCount++;
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    Log.LogInfo("[OK] F9=Diagnóstico, F10=Buscar tabela, Ctrl+P=Exportar");
                }
                
                if (!_initialized) return;
                
                try { if (Keyboard.current == null) return; } catch { return; }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Diagnóstico UI");
                    DiagnoseUI();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar tabela na UI");
                    FindTableInUI();
                }
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Exportar");
                    Export();
                }
            }
            catch { }
        }
        
        private static void DiagnoseUI()
        {
            try
            {
                var docs = UnityEngine.Object.FindObjectsOfType<UIDocument>();
                Log.LogInfo($"[UI] {docs.Length} UIDocuments");
                
                foreach (var doc in docs)
                {
                    Log.LogInfo($"[UI] Document: {doc.name}");
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    Log.LogInfo($"[UI] Root: {root.GetType().Name}");
                    
                    // Procurar "Report" - provavelmente a tabela
                    void ScanElement(VisualElement el, int depth)
                    {
                        if (el == null || depth > 8) return;
                        
                        try
                        {
                            var name = el.name ?? "";
                            var type = el.GetType().Name;
                            
                            if (depth <= 3 || name.Contains("Report") || name.Contains("Table") || name.Contains("List") || name.Contains("Row") || name.Contains("Item"))
                            {
                                var prefix = new string(' ', depth * 2);
                                Log.LogInfo($"[UI] {prefix}{type} ({name}) childCount={el.childCount}");
                            }
                            
                            // Se for "Report", explorar mais
                            if (name == "Report" && depth < 5)
                            {
                                Log.LogInfo($"[UI] *** REPORT ENCONTRADO ***");
                                for (int i = 0; i < el.childCount && i < 20; i++)
                                {
                                    try
                                    {
                                        var child = el.ElementAt(i);
                                        Log.LogInfo($"[UI]   Report[{i}]: {child.GetType().Name} ({child.name})");
                                        
                                        // Explorar filhos do filho
                                        for (int j = 0; j < child.childCount && j < 10; j++)
                                        {
                                            var subchild = child.ElementAt(j);
                                            Log.LogInfo($"[UI]     [{j}]: {subchild.GetType().Name} ({subchild.name})");
                                        }
                                    }
                                    catch { }
                                }
                            }
                            
                            // Navegar filhos
                            for (int i = 0; i < el.childCount && i < 50; i++)
                            {
                                try { ScanElement(el.ElementAt(i), depth + 1); } catch { }
                            }
                        }
                        catch { }
                    }
                    
                    ScanElement(root, 0);
                }
            }
            catch (Exception ex) { Log.LogError($"[UI] {ex.Message}"); }
        }
        
        private static void FindTableInUI()
        {
            try
            {
                var docs = UnityEngine.Object.FindObjectsOfType<UIDocument>();
                
                foreach (var doc in docs)
                {
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Procurar "Report"
                    void FindReport(VisualElement el, int depth)
                    {
                        if (el == null || depth > 5) return;
                        
                        try
                        {
                            if (el.name == "Report")
                            {
                                Log.LogInfo($"[Tabela] Report encontrado!");
                                ExtractTableFromReport(el);
                                return;
                            }
                            
                            for (int i = 0; i < el.childCount && i < 30; i++)
                            {
                                try { FindReport(el.ElementAt(i), depth + 1); } catch { }
                            }
                        }
                        catch { }
                    }
                    
                    FindReport(root, 0);
                }
            }
            catch (Exception ex) { Log.LogError($"[Tabela] {ex.Message}"); }
        }
        
        private static void ExtractTableFromReport(VisualElement report)
        {
            try
            {
                _tableData = new List<string[]>();
                
                Log.LogInfo($"[Tabela] Report tem {report.childCount} filhos");
                
                // Estrutura esperada: Report -> Container -> Rows
                for (int i = 0; i < report.childCount && i < 100; i++)
                {
                    try
                    {
                        var container = report.ElementAt(i);
                        Log.LogInfo($"[Tabela] Container[{i}]: {container.GetType().Name} ({container.name}) childCount={container.childCount}");
                        
                        // Procurar linhas dentro do container
                        for (int j = 0; j < container.childCount && j < 100; j++)
                        {
                            try
                            {
                                var row = container.ElementAt(j);
                                var rowType = row.GetType().Name;
                                var rowName = row.name;
                                
                                // Extrair texto de cada elemento
                                var rowValues = new List<string>();
                                
                                void ExtractText(VisualElement el, int d)
                                {
                                    if (el == null || d > 4) return;
                                    
                                    try
                                    {
                                        // Se tem texto, extrair
                                        var textProp = el.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                                        if (textProp != null)
                                        {
                                            var text = textProp.GetValue(el)?.ToString() ?? "";
                                            if (!string.IsNullOrEmpty(text) && text.Length < 100)
                                            {
                                                rowValues.Add(text);
                                            }
                                        }
                                        
                                        // Navegar filhos
                                        for (int k = 0; k < el.childCount && k < 20; k++)
                                        {
                                            ExtractText(el.ElementAt(k), d + 1);
                                        }
                                    }
                                    catch { }
                                }
                                
                                ExtractText(row, 0);
                                
                                if (rowValues.Count > 0)
                                {
                                    _tableData.Add(rowValues.ToArray());
                                    if (_tableData.Count <= 5)
                                    {
                                        Log.LogInfo($"[Tabela] Linha {j}: {string.Join(" | ", rowValues.Take(10))}");
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Tabela] Total: {_tableData.Count} linhas extraídas");
            }
            catch (Exception ex) { Log.LogError($"[Tabela] {ex.Message}"); }
        }
        
        private static void Export()
        {
            try
            {
                if (_tableData == null || _tableData.Count == 0)
                {
                    Log.LogWarning("[Export] Nenhuma tabela. Aperte F10 primeiro.");
                    return;
                }
                
                // Encontrar máximo de colunas
                int maxCols = _tableData.Max(r => r.Length);
                
                var csv = new System.Text.StringBuilder();
                
                foreach (var row in _tableData)
                {
                    var values = row.Select(v => v.Replace(";", ",").Replace("\n", " ").Replace("\r", "")).ToList();
                    while (values.Count < maxCols) values.Add("");
                    csv.AppendLine(string.Join(";", values));
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Players_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                
                Log.LogInfo($"[Export] ✅ {_tableData.Count} linhas -> {path}");
            }
            catch (Exception ex) { Log.LogError($"[Export] {ex.Message}"); }
        }
    }
}
