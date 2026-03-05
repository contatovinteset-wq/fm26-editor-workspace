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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.32.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.32.0 CARREGADO!");
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
                    TryExportData();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Investigar PlayerDataPoint");
                    InvestigatePlayerDataPoint();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Investigar SearchResultReference");
                    InvestigateSearchResultReference();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigatePlayerDataPoint()
        {
            try
            {
                // Buscar tipo PlayerDataPoint
                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.Name == "PlayerDataPoint");
                
                if (type == null)
                {
                    Log.LogWarning("[PDP] Tipo não encontrado");
                    return;
                }
                
                Log.LogInfo($"[PDP] Tipo: {type.FullName}");
                
                // Propriedades de instância
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[PDP] {props.Length} propriedades de instância:");
                foreach (var p in props)
                {
                    Log.LogInfo($"[PDP]   {p.Name}: {p.PropertyType.Name}");
                }
                
                // Propriedades estáticas
                var staticProps = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
                Log.LogInfo($"[PDP] {staticProps.Length} propriedades estáticas:");
                foreach (var p in staticProps)
                {
                    Log.LogInfo($"[PDP]   static {p.Name}: {p.PropertyType.Name}");
                    
                    try
                    {
                        var val = p.GetValue(null);
                        if (val != null)
                        {
                            Log.LogInfo($"[PDP]     valor: {val.GetType().Name}");
                            
                            if (val is IEnumerable en && !(val is string))
                            {
                                int count = 0;
                                foreach (var item in en)
                                {
                                    count++;
                                    if (count >= 1000) break;
                                }
                                Log.LogInfo($"[PDP]     ⭐ {count} itens!");
                            }
                        }
                    }
                    catch { }
                }
                
                // Campos estáticos
                var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
                Log.LogInfo($"[PDP] {fields.Length} campos estáticos:");
                foreach (var f in fields)
                {
                    Log.LogInfo($"[PDP]   static {f.Name}: {f.FieldType.Name}");
                    
                    try
                    {
                        var val = f.GetValue(null);
                        if (val != null)
                        {
                            if (val is IEnumerable en && !(val is string))
                            {
                                int count = 0;
                                foreach (var item in en)
                                {
                                    count++;
                                    if (count >= 1000) break;
                                }
                                Log.LogInfo($"[PDP]     ⭐ {count} itens!");
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[PDP] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigateSearchResultReference()
        {
            try
            {
                // Buscar tipo SearchResultReference
                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.Name == "SearchResultReference");
                
                if (type == null)
                {
                    Log.LogWarning("[SRR] Tipo não encontrado");
                    return;
                }
                
                Log.LogInfo($"[SRR] Tipo: {type.FullName}");
                
                // Propriedades de instância
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[SRR] {props.Length} propriedades:");
                foreach (var p in props)
                {
                    Log.LogInfo($"[SRR]   {p.Name}: {p.PropertyType.Name}");
                }
                
                // Propriedades estáticas
                var staticProps = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
                foreach (var p in staticProps)
                {
                    Log.LogInfo($"[SRR]   static {p.Name}: {p.PropertyType.Name}");
                    
                    try
                    {
                        var val = p.GetValue(null);
                        if (val != null)
                        {
                            Log.LogInfo($"[SRR]     valor: {val.GetType().Name}");
                            
                            if (val is IEnumerable en && !(val is string))
                            {
                                int count = 0;
                                foreach (var item in en)
                                {
                                    count++;
                                    if (count >= 1000) break;
                                }
                                Log.LogInfo($"[SRR]     ⭐ {count} itens!");
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[SRR] Erro: {ex.Message}");
            }
        }
        
        private static void TryExportData()
        {
            try
            {
                // Buscar todos os objetos UnityEngine.Object que contenham dados de jogador
                var allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                
                Log.LogInfo($"[Export] {allObjects.Length} objetos Unity encontrados");
                
                int count = 0;
                foreach (var obj in allObjects)
                {
                    if (obj == null) continue;
                    
                    var type = obj.GetType();
                    var name = type.Name.ToLower();
                    
                    // Verificar se é um tipo relevante
                    if (name.Contains("playerdata") || name.Contains("searchresult") ||
                        name.Contains("squaddata") || name.Contains("rosterdata"))
                    {
                        Log.LogInfo($"[Export] Objeto: {type.Name}");
                        count++;
                        
                        // Verificar propriedades IEnumerable
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var p in props)
                        {
                            try
                            {
                                var val = p.GetValue(obj);
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
                                        Log.LogInfo($"[Export] ⭐⭐⭐ {p.Name}: {list.Count} itens!");
                                        ExportCsv(list);
                                        return;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                
                Log.LogInfo($"[Export] {count} objetos relevantes encontrados, mas sem dados IEnumerable");
                
                // Fallback: buscar SI.Bindable.CustomViewExportData
                var exportType = Type.GetType("SI.Bindable.CustomViewExportData, SI.Bindable");
                if (exportType != null)
                {
                    Log.LogInfo($"[Export] Investigando CustomViewExportData...");
                    
                    var staticProps = exportType.GetProperties(BindingFlags.Static | BindingFlags.Public);
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
                                
                                if (list.Count > 0)
                                {
                                    Log.LogInfo($"[Export] CustomViewExportData.{p.Name}: {list.Count} itens");
                                    ExportCsv(list);
                                    return;
                                }
                            }
                        }
                        catch { }
                    }
                }
                
                Log.LogWarning("[Export] Nenhum dado exportável encontrado");
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
