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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.30.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.30.0 CARREGADO!");
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
                    Log.LogInfo(">>> F9 - Buscar tipos Player* em SI.Bindable");
                    FindPlayerTypesInBindable();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Investigar CustomViewExportData");
                    InvestigateExportDataTypes();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void FindPlayerTypesInBindable()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "SI.Bindable");
                
                if (asm == null)
                {
                    Log.LogWarning("[Type] SI.Bindable não encontrado");
                    return;
                }
                
                var types = asm.GetTypes();
                Log.LogInfo($"[Type] {types.Length} tipos em SI.Bindable");
                
                // Buscar tipos com "Player" no nome
                var playerTypes = types.Where(t => 
                    t.Name.ToLower().Contains("player") || 
                    t.Name.ToLower().Contains("person") ||
                    t.Name.ToLower().Contains("squad")).Take(30);
                
                foreach (var t in playerTypes)
                {
                    string info = $"{t.Name}";
                    
                    // Verificar propriedades
                    var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    if (props.Length > 0)
                    {
                        info += $" [{props.Length} props]";
                        
                        // Verificar se tem dados
                        var dataProps = props.Where(p => 
                            p.Name.ToLower().Contains("name") ||
                            p.Name.ToLower().Contains("age") ||
                            p.Name.ToLower().Contains("id") ||
                            p.Name.ToLower().Contains("club"));
                        
                        if (dataProps.Any())
                        {
                            info += " ⭐";
                        }
                    }
                    
                    Log.LogInfo($"[Type] {info}");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Type] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigateExportDataTypes()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "SI.Bindable");
                
                if (asm == null) return;
                
                var types = asm.GetTypes();
                
                // Buscar tipos com "Export" ou "Data" no nome
                var exportTypes = types.Where(t => 
                    t.Name.Contains("Export") || 
                    t.Name.Contains("Data") ||
                    t.Name.Contains("Item") ||
                    t.Name.Contains("Row") ||
                    t.Name.Contains("Entry")).Take(30);
                
                Log.LogInfo($"[Export] Tipos com Export/Data/Item/Row/Entry:");
                
                foreach (var t in exportTypes)
                {
                    Log.LogInfo($"[Export] {t.Name}");
                    
                    // Propriedades estáticas
                    var staticProps = t.GetProperties(BindingFlags.Static | BindingFlags.Public);
                    foreach (var p in staticProps)
                    {
                        try
                        {
                            var val = p.GetValue(null);
                            if (val != null)
                            {
                                string valInfo = val.GetType().Name;
                                
                                // Contar se for enumerable
                                if (val is IEnumerable en && !(val is string))
                                {
                                    int count = 0;
                                    foreach (var item in en)
                                    {
                                        count++;
                                        if (count >= 1000) break;
                                    }
                                    if (count > 0)
                                    {
                                        Log.LogInfo($"[Export]   static {p.Name}: {count} itens ⭐⭐⭐");
                                    }
                                }
                                else
                                {
                                    Log.LogInfo($"[Export]   static {p.Name}: {valInfo}");
                                }
                            }
                        }
                        catch { }
                    }
                    
                    // Propriedades de instância
                    var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    if (props.Length > 0 && props.Length < 30)
                    {
                        Log.LogInfo($"[Export]   Props: {string.Join(", ", props.Take(10).Select(p => p.Name))}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void TryExport()
        {
            try
            {
                // Buscar em todos assemblies
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var asmName = asm.GetName().Name;
                    if (!asmName.StartsWith("SI.") && !asmName.StartsWith("FM.")) continue;
                    
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
                                                var itemType = first.GetType();
                                                var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                                
                                                // Verificar se tem dados úteis
                                                if (props.Length >= 3)
                                                {
                                                    Log.LogInfo($"[Export] {asmName}.{t.Name}.{p.Name}: {list.Count} itens");
                                                    ExportCsv(list);
                                                    return;
                                                }
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
