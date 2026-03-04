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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.31.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.31.0 CARREGADO!");
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
                    Log.LogInfo(">>> F9 - Buscar tipos PlayerSearch/PlayerDatabase");
                    FindPlayerSearchTypes();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Listar TODOS tipos SI.Bindable (primeiros 100)");
                    ListAllBindableTypes();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void FindPlayerSearchTypes()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "SI.Bindable");
                
                if (asm == null)
                {
                    Log.LogWarning("[Search] SI.Bindable não encontrado");
                    return;
                }
                
                var types = asm.GetTypes();
                
                // Buscar tipos relacionados a Player Search/Database
                var searchTypes = types.Where(t => 
                {
                    var name = t.Name.ToLower();
                    return name.Contains("playersearch") || 
                           name.Contains("playerdatabase") ||
                           name.Contains("playerlist") ||
                           name.Contains("searchresult") ||
                           name.Contains("playerrow") ||
                           name.Contains("playeritem") ||
                           name.Contains("personsearch") ||
                           name.Contains("squadlist");
                }).ToList();
                
                Log.LogInfo($"[Search] {searchTypes.Count} tipos encontrados");
                
                foreach (var t in searchTypes.Take(30))
                {
                    var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    var staticProps = t.GetProperties(BindingFlags.Static | BindingFlags.Public);
                    
                    string info = $"{t.Name} [{props.Length} props, {staticProps.Count()} static]";
                    
                    // Verificar se tem propriedades com dados de jogador
                    var hasPlayerData = props.Any(p => 
                    {
                        var name = p.Name.ToLower();
                        return name.Contains("name") || name.Contains("age") || 
                               name.Contains("position") || name.Contains("club") ||
                               name.Contains("nation") || name.Contains("value");
                    });
                    
                    if (hasPlayerData) info += " ⭐";
                    
                    Log.LogInfo($"[Search] {info}");
                    
                    // Verificar propriedades estáticas para dados
                    foreach (var sp in staticProps)
                    {
                        try
                        {
                            var val = sp.GetValue(null);
                            if (val != null)
                            {
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
                                        Log.LogInfo($"[Search]   static {sp.Name}: {count} itens ⭐⭐⭐");
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Search] Erro: {ex.Message}");
            }
        }
        
        private static void ListAllBindableTypes()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "SI.Bindable");
                
                if (asm == null) return;
                
                var types = asm.GetTypes();
                Log.LogInfo($"[List] {types.Length} tipos em SI.Bindable:");
                
                // Mostrar todos os tipos com "Data" no nome
                var dataTypes = types.Where(t => t.Name.Contains("Data")).Take(50);
                foreach (var t in dataTypes)
                {
                    Log.LogInfo($"[List] {t.Name}");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[List] Erro: {ex.Message}");
            }
        }
        
        private static void TryExport()
        {
            try
            {
                // Buscar em todos assemblies por IEnumerable estático
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
                                                
                                                // Verificar se tem dados de jogador
                                                var propNames = props.Select(x => x.Name.ToLower()).ToList();
                                                bool hasPlayerData = propNames.Any(n => 
                                                    n.Contains("name") || n.Contains("age") || 
                                                    n.Contains("position") || n.Contains("club"));
                                                
                                                if (hasPlayerData)
                                                {
                                                    Log.LogInfo($"[Export] {asmName}.{t.Name}.{p.Name}: {list.Count} jogadores ⭐⭐⭐");
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
                
                Log.LogWarning("[Export] Nenhum dado de jogador encontrado");
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
