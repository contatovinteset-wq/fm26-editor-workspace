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
                    Log.LogInfo(">>> Ctrl+P - Investigar CustomViewExportData e exportar");
                    InvestigateAndExport();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Propriedades do CustomViewExportData");
                    ShowCustomViewExportDataProps();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void ShowCustomViewExportDataProps()
        {
            try
            {
                var type = Type.GetType("SI.Bindable.CustomViewExportData, SI.Bindable");
                if (type == null)
                {
                    Log.LogWarning("[CVE] Tipo não encontrado");
                    return;
                }
                
                Log.LogInfo($"[CVE] Tipo: {type.FullName}");
                
                // Todas as propriedades de instância
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[CVE] {props.Length} propriedades de instância:");
                
                foreach (var p in props)
                {
                    Log.LogInfo($"[CVE]   {p.Name}: {p.PropertyType.Name}");
                }
                
                // Propriedades estáticas
                var staticProps = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
                Log.LogInfo($"[CVE] {staticProps.Length} propriedades estáticas:");
                
                foreach (var p in staticProps)
                {
                    Log.LogInfo($"[CVE]   static {p.Name}: {p.PropertyType.Name}");
                    
                    try
                    {
                        var val = p.GetValue(null);
                        if (val != null)
                        {
                            Log.LogInfo($"[CVE]     = {val.GetType().Name}");
                            
                            if (val is IEnumerable en && !(val is string))
                            {
                                int count = 0;
                                foreach (var item in en)
                                {
                                    count++;
                                    if (count >= 100) break;
                                }
                                Log.LogInfo($"[CVE]     → {count} itens!");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogInfo($"[CVE]     Erro: {ex.Message}");
                    }
                }
                
                // Campos
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                Log.LogInfo($"[CVE] {fields.Length} campos estáticos:");
                
                foreach (var f in fields)
                {
                    Log.LogInfo($"[CVE]   static {f.Name}: {f.FieldType.Name}");
                    
                    try
                    {
                        var val = f.GetValue(null);
                        if (val != null)
                        {
                            Log.LogInfo($"[CVE]     = {val.GetType().Name}");
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[CVE] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigateAndExport()
        {
            try
            {
                var type = Type.GetType("SI.Bindable.CustomViewExportData, SI.Bindable");
                if (type == null)
                {
                    Log.LogWarning("[Export] CustomViewExportData não encontrado");
                    return;
                }
                
                // Buscar qualquer propriedade/campo estático com IEnumerable
                var staticProps = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
                var staticFields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
                
                foreach (var p in staticProps)
                {
                    try
                    {
                        var val = p.GetValue(null);
                        if (val is IEnumerable en && !(val is string))
                        {
                            var list = en.Cast<object>().Take(10000).ToList();
                            if (list.Count > 0)
                            {
                                Log.LogInfo($"[Export] {p.Name}: {list.Count} itens");
                                ExportCsv(list);
                                return;
                            }
                        }
                    }
                    catch { }
                }
                
                foreach (var f in staticFields)
                {
                    try
                    {
                        var val = f.GetValue(null);
                        if (val is IEnumerable en && !(val is string))
                        {
                            var list = en.Cast<object>().Take(10000).ToList();
                            if (list.Count > 0)
                            {
                                Log.LogInfo($"[Export] {f.Name}: {list.Count} itens");
                                ExportCsv(list);
                                return;
                            }
                        }
                    }
                    catch { }
                }
                
                // Se não tem dados estáticos, criar instância e verificar
                try
                {
                    var instance = Activator.CreateInstance(type);
                    if (instance != null)
                    {
                        Log.LogInfo($"[Export] Instância criada: {instance.GetType().Name}");
                        
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var p in props)
                        {
                            try
                            {
                                var val = p.GetValue(instance);
                                if (val is IEnumerable en && !(val is string))
                                {
                                    var list = en.Cast<object>().Take(10000).ToList();
                                    if (list.Count > 0)
                                    {
                                        Log.LogInfo($"[Export] {p.Name}: {list.Count} itens");
                                        ExportCsv(list);
                                        return;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.LogInfo($"[Export] Erro ao criar instância: {ex.Message}");
                }
                
                Log.LogWarning("[Export] Nenhum dado encontrado em CustomViewExportData");
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
