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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.57.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.57.0");
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
                    Log.LogInfo("[OK] F9=Explorar PlayerSearchReport, Ctrl+P=Exportar");
                }
                
                if (!_initialized) return;
                
                try { if (Keyboard.current == null) return; } catch { return; }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Explorar PlayerSearchReport");
                    ExplorePlayerSearchReport();
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
        
        private static void ExplorePlayerSearchReport()
        {
            try
            {
                var docs = UnityEngine.Object.FindObjectsOfType<UIDocument>();
                
                foreach (var doc in docs)
                {
                    if (doc.name != "PanelManager") continue;
                    
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Encontrar Report -> Body -> PlayerSearchReport
                    VisualElement report = null;
                    VisualElement reportBody = null;
                    VisualElement playerSearchReport = null;
                    
                    for (int i = 0; i < root.childCount; i++)
                    {
                        var child = root.ElementAt(i);
                        if (child.name == "Report")
                        {
                            report = child;
                            break;
                        }
                    }
                    
                    if (report == null) { Log.LogWarning("[Explore] Report não encontrado"); return; }
                    
                    // Procurar Body dentro do Report
                    for (int i = 0; i < report.childCount; i++)
                    {
                        var child = report.ElementAt(i);
                        if (child.name == "Body")
                        {
                            reportBody = child;
                            break;
                        }
                    }
                    
                    if (reportBody == null) { Log.LogWarning("[Explore] Body não encontrado"); return; }
                    
                    // Procurar PlayerSearchReport dentro do Body
                    for (int i = 0; i < reportBody.childCount; i++)
                    {
                        var child = reportBody.ElementAt(i);
                        if (child.name == "PlayerSearchReport")
                        {
                            playerSearchReport = child;
                            break;
                        }
                    }
                    
                    if (playerSearchReport == null) { Log.LogWarning("[Explore] PlayerSearchReport não encontrado"); return; }
                    
                    Log.LogInfo($"[Explore] PlayerSearchReport encontrado! childCount={playerSearchReport.childCount}");
                    
                    // Explorar profundamente
                    _tableData = new List<string[]>();
                    
                    void DeepScan(VisualElement el, int depth, List<string> currentRow)
                    {
                        if (el == null || depth > 15) return;
                        
                        try
                        {
                            var type = el.GetType();
                            var name = el.name ?? "";
                            
                            // Tentar extrair texto
                            var textProp = type.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                            if (textProp != null)
                            {
                                try
                                {
                                    var text = textProp.GetValue(el)?.ToString() ?? "";
                                    if (!string.IsNullOrWhiteSpace(text) && text.Length < 200)
                                    {
                                        currentRow.Add(text.Trim());
                                    }
                                }
                                catch { }
                            }
                            
                            // Verificar se é uma "linha" (baseado no nome ou tipo)
                            var isRow = name.Contains("Row") || name.Contains("Item") || name.Contains("Line") || type.Name.Contains("Row");
                            
                            // Se tem filhos, explorar
                            if (el.childCount > 0)
                            {
                                // Se parece ser uma linha, criar nova lista para os filhos
                                if (isRow && currentRow.Count > 0)
                                {
                                    // Salvar linha anterior se tiver dados
                                    if (currentRow.Count >= 3)
                                    {
                                        _tableData.Add(currentRow.ToArray());
                                        Log.LogInfo($"[Explore] Linha salva: {string.Join(" | ", currentRow.Take(5))}...");
                                    }
                                    currentRow = new List<string>();
                                }
                                
                                for (int i = 0; i < el.childCount && i < 100; i++)
                                {
                                    DeepScan(el.ElementAt(i), depth + 1, currentRow);
                                }
                            }
                        }
                        catch { }
                    }
                    
                    DeepScan(playerSearchReport, 0, new List<string>());
                    
                    Log.LogInfo($"[Explore] Total extraído: {_tableData.Count} linhas");
                    
                    // Se não encontrou linhas, mostrar estrutura
                    if (_tableData.Count == 0)
                    {
                        Log.LogInfo("[Explore] Extraindo TODOS os textos...");
                        _tableData.Clear();
                        
                        void ExtractAllText(VisualElement el, int depth)
                        {
                            if (el == null || depth > 20) return;
                            
                            try
                            {
                                var type = el.GetType();
                                var textProp = type.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                                
                                if (textProp != null)
                                {
                                    var text = textProp.GetValue(el)?.ToString() ?? "";
                                    if (!string.IsNullOrWhiteSpace(text) && text.Length < 100)
                                    {
                                        var prefix = new string(' ', depth * 2);
                                        Log.LogInfo($"[TXT] {prefix}{text.Trim()}");
                                    }
                                }
                                
                                for (int i = 0; i < el.childCount && i < 200; i++)
                                {
                                    ExtractAllText(el.ElementAt(i), depth + 1);
                                }
                            }
                            catch { }
                        }
                        
                        ExtractAllText(playerSearchReport, 0);
                    }
                }
            }
            catch (Exception ex) { Log.LogError($"[Explore] {ex.Message}"); }
        }
        
        private static void Export()
        {
            try
            {
                if (_tableData == null || _tableData.Count == 0)
                {
                    Log.LogWarning("[Export] Nenhuma tabela. Aperte F9 primeiro.");
                    return;
                }
                
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
