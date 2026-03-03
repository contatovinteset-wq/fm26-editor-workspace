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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.16.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.16.0 CARREGADO!");
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
                    Log.LogInfo("[Init] Pronto para v2.16.0!");
                }
                
                if (!_initialized || Keyboard.current == null) return;
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - EXPORTAÇÃO BRUTA");
                    BruteForceExport();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Investigar PlayerSearchReport");
                    InvestigateReport("PlayerSearchReport");
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Investigar TeamSquadReport");
                    InvestigateReport("TeamSquadReport");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigateReport(string reportName)
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    var report = FindElementByName(root, reportName, 0, 30);
                    if (report != null)
                    {
                        Log.LogInfo($"[Diag] ⭐ Encontrou {reportName}!");
                        DumpElementDetails(report);
                    }
                    else
                    {
                        Log.LogWarning($"[Diag] {reportName} não encontrado no PanelManager");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Diag] Erro: {ex.Message}");
            }
        }
        
        private static VisualElement FindElementByName(VisualElement element, string name, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return null;
            if (element.name == name) return element;
            
            for (int i = 0; i < element.childCount; i++)
            {
                var found = FindElementByName(element[i], name, depth + 1, maxDepth);
                if (found != null) return found;
            }
            return null;
        }
        
        private static void DumpElementDetails(VisualElement element)
        {
            try
            {
                var type = element.GetType();
                Log.LogInfo($"[Dump] Nome: {element.name}, Tipo: {type.FullName}");
                
                // Tentar ler m_rows via propriedade ou campo em todos os descendentes
                Log.LogInfo("[Dump] Buscando m_rows nos filhos...");
                int foundCount = 0;
                SearchMRowsRecursive(element, ref foundCount, 0, 10);
                Log.LogInfo($"[Dump] Fim da busca. Encontrados: {foundCount}");
            }
            catch (Exception ex) { Log.LogError($"[Dump] Erro: {ex.Message}"); }
        }
        
        private static void SearchMRowsRecursive(VisualElement element, ref int count, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            var type = element.GetType();
            var mRowsProp = type.GetProperty("m_rows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (mRowsProp != null)
            {
                count++;
                Log.LogInfo($"[Found] ⭐ {element.name} [{type.Name}] tem m_rows!");
                try {
                    var val = mRowsProp.GetValue(element) as IList;
                    Log.LogInfo($"[Found]    Itens: {val?.Count ?? 0}");
                } catch {}
            }
            
            for (int i = 0; i < element.childCount; i++)
                SearchMRowsRecursive(element[i], ref count, depth + 1, maxDepth);
        }
        
        private static void BruteForceExport()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                IList dataToExport = null;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Estratégia v2.16.0: Buscar especificamente nos Reports
                    string[] targets = { "PlayerSearchReport", "TeamSquadReport", "ReportBody", "Body" };
                    foreach (var targetName in targets)
                    {
                        var target = FindElementByName(root, targetName, 0, 30);
                        if (target != null)
                        {
                            Log.LogInfo($"[Export] Escaneando {targetName}...");
                            dataToExport = FindFirstPopulatedRows(target, 0, 15);
                            if (dataToExport != null) break;
                        }
                    }
                    if (dataToExport != null) break;
                }
                
                if (dataToExport != null)
                {
                    Log.LogInfo($"[Export] ✅ Dados capturados! Total: {dataToExport.Count}");
                    ExportCsv(dataToExport);
                }
                else
                {
                    Log.LogWarning("[Export] Nada encontrado. A tabela pode não estar usando 'm_rows' ou está protegida.");
                }
            }
            catch (Exception ex) { Log.LogError($"[Export] Critico: {ex.Message}"); }
        }
        
        private static IList FindFirstPopulatedRows(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return null;
            
            var prop = element.GetType().GetProperty("m_rows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null)
            {
                var val = prop.GetValue(element) as IList;
                if (val != null && val.Count > 0) return val;
            }
            
            for (int i = 0; i < element.childCount; i++)
            {
                var found = FindFirstPopulatedRows(element[i], depth + 1, maxDepth);
                if (found != null) return found;
            }
            return null;
        }
        
        private static void ExportCsv(IList data)
        {
            try
            {
                var first = data[0];
                if (first == null) return;
                
                // Tenta extrair BindingRoot de ValueTuple (Comum em StreamedTable)
                object targetObj = first;
                var tupleProp = first.GetType().GetProperty("Item1");
                if (tupleProp != null) targetObj = tupleProp.GetValue(first);
                
                var type = targetObj.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                var csv = new System.Text.StringBuilder();
                List<string> headers = new List<string>();
                foreach (var p in props) if (p.GetIndexParameters().Length == 0) headers.Add(p.Name);
                csv.AppendLine(string.Join(";", headers));
                
                foreach (var item in data)
                {
                    if (item == null) continue;
                    object rowObj = item;
                    if (tupleProp != null) rowObj = tupleProp.GetValue(item);
                    
                    List<string> values = new List<string>();
                    foreach (var p in props)
                    {
                        if (p.GetIndexParameters().Length > 0) continue;
                        try {
                            var val = p.GetValue(rowObj);
                            values.Add((val?.ToString() ?? "").Replace(";", ","));
                        } catch { values.Add(""); }
                    }
                    csv.AppendLine(string.Join(";", values));
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Brute_{DateTime.Now:yyyyMMdd_HHmm}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                Log.LogInfo($"[Export] Salvo em: {path}");
            }
            catch (Exception ex) { Log.LogError($"[CSV] Erro: {ex.Message}"); }
        }
    }
}
