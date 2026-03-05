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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.33.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.33.0 CARREGADO!");
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
                    Log.LogInfo(">>> F9 - Buscar tipos Player em SI.Bindable");
                    FindPlayerTypesInBindable();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar tipos Person em FM.Core");
                    FindPersonTypesInCore();
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
                    Log.LogWarning("[Bind] Assembly não encontrado");
                    return;
                }
                
                var types = asm.GetTypes();
                Log.LogInfo($"[Bind] {types.Length} tipos em SI.Bindable");
                
                // Buscar tipos com Player, Person, Squad, Roster no nome
                var relevant = types.Where(t => 
                {
                    var name = t.Name.ToLower();
                    return name.Contains("player") || name.Contains("person") || 
                           name.Contains("squad") || name.Contains("roster") ||
                           name.Contains("athlete") || name.Contains("footballer");
                }).ToList();
                
                Log.LogInfo($"[Bind] {relevant.Count} tipos relevantes:");
                
                foreach (var t in relevant.Take(30))
                {
                    var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    Log.LogInfo($"[Bind] {t.Name} [{props.Length} props]");
                    
                    // Mostrar propriedades interessantes
                    var interesting = props.Where(p => 
                    {
                        var name = p.Name.ToLower();
                        return name.Contains("name") || name.Contains("age") || 
                               name.Contains("club") || name.Contains("team") ||
                               name.Contains("position") || name.Contains("value") ||
                               name.Contains("id") || name.Contains("nation");
                    });
                    
                    foreach (var p in interesting.Take(8))
                    {
                        Log.LogInfo($"[Bind]   {p.Name}: {p.PropertyType.Name}");
                    }
                    
                    // Verificar propriedades estáticas com dados
                    var staticProps = t.GetProperties(BindingFlags.Static | BindingFlags.Public);
                    foreach (var p in staticProps)
                    {
                        try
                        {
                            var val = p.GetValue(null);
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
                                    if (count > 0)
                                    {
                                        Log.LogInfo($"[Bind]   ⭐ static {p.Name}: {count} itens!");
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
                Log.LogError($"[Bind] Erro: {ex.Message}");
            }
        }
        
        private static void FindPersonTypesInCore()
        {
            try
            {
                // Buscar em todos assemblies SI.* e FM.*
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => 
                    {
                        var name = a.GetName().Name;
                        return name.StartsWith("SI.") || name.StartsWith("FM.");
                    })
                    .ToList();
                
                var allTypes = new List<Type>();
                foreach (var asm in assemblies)
                {
                    try
                    {
                        allTypes.AddRange(asm.GetTypes());
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Core] {allTypes.Count} tipos em SI.* + FM.*");
                
                // Buscar tipos que PARECEM dados de jogador (não UI, não Match)
                var playerDataTypes = allTypes.Where(t => 
                {
                    var name = t.Name.ToLower();
                    var ns = t.Namespace?.ToLower() ?? "";
                    
                    // Ignorar UI e Match
                    if (ns.Contains(".ui") || ns.Contains(".match")) return false;
                    
                    // Mas deve ter Player, Person, Footballer no nome
                    return name.Contains("player") || name.Contains("person") ||
                           name.Contains("footballer") || name.Contains("athlete") ||
                           name.Contains("squad") || name.Contains("roster");
                }).ToList();
                
                Log.LogInfo($"[Core] {playerDataTypes.Count} tipos de dados (não-UI, não-Match):");
                
                foreach (var t in playerDataTypes.Take(25))
                {
                    var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    var name = t.Name;
                    var ns = t.Namespace ?? "?";
                    
                    // Filtrar apenas tipos com propriedades interessantes
                    var hasName = props.Any(p => p.Name.ToLower().Contains("name"));
                    var hasAge = props.Any(p => p.Name.ToLower().Contains("age"));
                    var hasClub = props.Any(p => p.Name.ToLower().Contains("club") || p.Name.ToLower().Contains("team"));
                    
                    var marker = (hasName || hasAge || hasClub) ? "⭐" : "";
                    
                    Log.LogInfo($"[Core] {name} [{props.Length} props] {ns} {marker}");
                    
                    if (hasName || hasAge || hasClub)
                    {
                        var interesting = props.Where(p => 
                        {
                            var n = p.Name.ToLower();
                            return n.Contains("name") || n.Contains("age") || 
                                   n.Contains("club") || n.Contains("team") ||
                                   n.Contains("position") || n.Contains("value") ||
                                   n.Contains("nation") || n.Contains("id");
                        }).Take(10);
                        
                        foreach (var p in interesting)
                        {
                            Log.LogInfo($"[Core]   {p.Name}: {p.PropertyType.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Core] Erro: {ex.Message}");
            }
        }
        
        private static void TryExportData()
        {
            try
            {
                // Buscar todos os objetos e verificar tipos relevantes
                var allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                
                Log.LogInfo($"[Export] {allObjects.Length} objetos Unity");
                
                // Tipos relevantes que podem ter dados
                var relevantTypeNames = new[] { 
                    "player", "person", "squad", "roster", 
                    "footballer", "athlete", "search", "database"
                };
                
                foreach (var obj in allObjects)
                {
                    if (obj == null) continue;
                    
                    var type = obj.GetType();
                    var typeName = type.Name.ToLower();
                    var ns = type.Namespace?.ToLower() ?? "";
                    
                    // Ignorar UI e Match
                    if (ns.Contains(".ui") || ns.Contains(".match")) continue;
                    
                    // Verificar se é relevante
                    if (!relevantTypeNames.Any(n => typeName.Contains(n))) continue;
                    
                    Log.LogInfo($"[Export] Objeto: {type.Name} ({type.Namespace})");
                    
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
                
                Log.LogWarning("[Export] Nenhum dado encontrado");
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
