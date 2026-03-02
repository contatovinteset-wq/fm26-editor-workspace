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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.6.1")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.6.1 CARREGADO!");
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
                
                // F9 - Investigar Report
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Investigar Report");
                    InvestigateReport();
                }
                
                // F10 - Buscar tabelas
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar tabelas");
                    FindTables();
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
        
        // F9 - Investigar estrutura do Report
        private static void InvestigateReport()
        {
            try
            {
                var report = FindReportElement();
                if (report == null)
                {
                    Log.LogWarning("[F9] Report não encontrado");
                    return;
                }
                
                Log.LogInfo($"[F9] Report com {report.childCount} filhos");
                
                for (int i = 0; i < report.childCount; i++)
                {
                    var child = report[i];
                    if (child == null) continue;
                    
                    string name = child.name ?? "(sem nome)";
                    Log.LogInfo($"[F9] [{i}] {name}");
                    
                    // Se for Body, explorar mais
                    if (name == "Body")
                    {
                        Log.LogInfo($"[F9]   -> Body tem {child.childCount} filhos:");
                        for (int j = 0; j < child.childCount && j < 20; j++)
                        {
                            var bodyChild = child[j];
                            if (bodyChild == null) continue;
                            
                            string bName = bodyChild.name ?? "(sem nome)";
                            string bType = bodyChild.GetType().Name;
                            int bChildren = bodyChild.childCount;
                            
                            Log.LogInfo($"[F9]     [{j}] {bName} ({bType}) - {bChildren} filhos");
                            
                            // Mostrar netos do Body
                            if (bChildren > 0 && bChildren < 50)
                            {
                                for (int k = 0; k < Math.Min(10, bChildren); k++)
                                {
                                    var grandChild = bodyChild[k];
                                    if (grandChild != null)
                                    {
                                        string gName = grandChild.name ?? "(sem nome)";
                                        string gType = grandChild.GetType().Name;
                                        Log.LogInfo($"[F9]       [{k}] {gName} ({gType})");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[F9] Erro: {ex.Message}");
            }
        }
        
        // F10 - Buscar tabelas
        private static void FindTables()
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
                FindTablesRecursive(report, ref found, 0, 6);
                Log.LogInfo($"[F10] Total de elementos 'Streamed' encontrados: {found}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F10] Erro: {ex.Message}");
            }
        }
        
        private static void FindTablesRecursive(VisualElement element, ref int count, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                string typeName = element.GetType().FullName ?? "";
                string elementName = element.name ?? "";
                
                // Verificar se parece uma tabela/lista de dados
                bool isStreamed = typeName.Contains("Streamed");
                bool isTable = elementName.Contains("Table") || elementName.Contains("List");
                
                if (isStreamed || isTable)
                {
                    count++;
                    Log.LogInfo($"[F10] ⭐ {elementName} ({typeName})");
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 30; i++)
                {
                    FindTablesRecursive(element[i], ref count, depth + 1, maxDepth);
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
                
                // Procurar elemento com dados
                IList foundData = null;
                FindDataRecursive(report, ref foundData, 0, 8);
                
                if (foundData != null && foundData.Count > 0)
                {
                    Log.LogInfo($"[Export] Encontrado {foundData.Count} itens");
                    ExportToCsv(foundData);
                }
                else
                {
                    Log.LogWarning("[Export] Nenhum dado encontrado. Tente F9 para ver a estrutura.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void FindDataRecursive(VisualElement element, ref IList foundData, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth || foundData != null) return;
            
            try
            {
                var type = element.GetType();
                string typeName = type.FullName ?? "";
                string elementName = element.name ?? "";
                
                // Logar todos os elementos no nível 2-3
                if (depth >= 2 && depth <= 4)
                {
                    Log.LogInfo($"[Export] {' ', depth * 2}Verificando: {elementName} ({typeName})");
                }
                
                // Tentar pegar dataSource de QUALQUER elemento
                try
                {
                    var dsProp = typeof(VisualElement).GetProperty("dataSource", BindingFlags.Public | BindingFlags.Instance);
                    if (dsProp != null)
                    {
                        var ds = dsProp.GetValue(element);
                        if (ds != null)
                        {
                            Log.LogInfo($"[Export] {' ', depth * 2}  dataSource: {ds.GetType().FullName}");
                            
                            if (ds is IList list && list.Count > 0)
                            {
                                foundData = list;
                                Log.LogInfo($"[Export] ✅ Encontrado {list.Count} itens em {elementName}!");
                                return;
                            }
                        }
                    }
                }
                catch { }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 30; i++)
                {
                    FindDataRecursive(element[i], ref foundData, depth + 1, maxDepth);
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
