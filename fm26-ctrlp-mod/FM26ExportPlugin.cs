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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.29.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.29.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            
            // Hook no Bindings.Update para capturar dados
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
                
                // Hook no construtor de Bindings
                var ctor = bindingsType.GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                {
                    var ctorPatch = typeof(Plugin).GetMethod("OnBindingsCtor", BindingFlags.Static | BindingFlags.Public);
                    harmony.Patch(ctor, postfix: new HarmonyMethod(ctorPatch));
                    Log.LogInfo("[Init] Patched Bindings.ctor");
                }
            }
        }
        
        private static int _frameCount = 0;
        private static bool _initialized = false;
        private static List<object> _capturedBindings = new List<object>();
        
        public static void OnBindingsCtor(object __instance)
        {
            try
            {
                if (_capturedBindings.Count < 100)
                {
                    _capturedBindings.Add(__instance);
                }
            }
            catch { }
        }
        
        public static void OnUpdate()
        {
            try
            {
                _frameCount++;
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    Log.LogInfo("[Init] Pronto!");
                    Log.LogInfo($"[Init] Capturados {_capturedBindings.Count} bindings");
                }
                
                if (!_initialized || Keyboard.current == null) return;
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Exportar");
                    ExportFromCapturedBindings();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Listar assemblies");
                    ListAssemblies();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Investigar bindings capturados");
                    InvestigateBindings();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void ListAssemblies()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Log.LogInfo($"[Asm] {assemblies.Length} assemblies carregados");
                
                foreach (var asm in assemblies)
                {
                    var name = asm.GetName().Name;
                    if (name.StartsWith("SI.") || name.StartsWith("FM.") || name.Contains("Football"))
                    {
                        try
                        {
                            var types = asm.GetTypes();
                            Log.LogInfo($"[Asm] {name}: {types.Length} tipos");
                        }
                        catch
                        {
                            Log.LogInfo($"[Asm] {name}: erro ao listar tipos");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Asm] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigateBindings()
        {
            try
            {
                Log.LogInfo($"[Bind] {_capturedBindings.Count} bindings capturados");
                
                foreach (var binding in _capturedBindings.Take(10))
                {
                    try
                    {
                        var type = binding.GetType();
                        Log.LogInfo($"[Bind] {type.Name}");
                        
                        // Propriedades públicas
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var p in props.Take(10))
                        {
                            try
                            {
                                var val = p.GetValue(binding);
                                string valType = val?.GetType().Name ?? "null";
                                Log.LogInfo($"[Bind]   {p.Name}: {valType}");
                                
                                // Se for IEnumerable, mostrar count
                                if (val is IEnumerable en && !(val is string))
                                {
                                    int count = 0;
                                    foreach (var item in en)
                                    {
                                        count++;
                                        if (count >= 100) break;
                                    }
                                    if (count > 0)
                                    {
                                        Log.LogInfo($"[Bind]     → {count} itens!");
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"[Bind] Erro: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Bind] Erro: {ex.Message}");
            }
        }
        
        private static void ExportFromCapturedBindings()
        {
            try
            {
                foreach (var binding in _capturedBindings)
                {
                    try
                    {
                        var type = binding.GetType();
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        
                        foreach (var p in props)
                        {
                            try
                            {
                                var val = p.GetValue(binding);
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
                                        Log.LogInfo($"[Export] {type.Name}.{p.Name}: {list.Count} itens");
                                        ExportCsv(list);
                                        return;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                
                Log.LogWarning("[Export] Nenhum dado encontrado nos bindings");
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
