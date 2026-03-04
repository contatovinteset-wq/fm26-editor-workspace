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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.26.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.26.0 CARREGADO!");
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
                    TryExportFromTypes();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Buscar tipos com 'Player' ou 'Squad' no nome");
                    FindPlayerTypes();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar instâncias de tipos de dados");
                    FindDataInstances();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void FindPlayerTypes()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var types = new List<Type>();
                
                foreach (var asm in assemblies)
                {
                    try
                    {
                        var asmTypes = asm.GetTypes();
                        foreach (var t in asmTypes)
                        {
                            var name = t.Name.ToLower();
                            if ((name.Contains("player") || name.Contains("squad") || name.Contains("person")) 
                                && !name.Contains("element") 
                                && !name.Contains("visual")
                                && !name.Contains("panel"))
                            {
                                types.Add(t);
                            }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Type] {types.Count} tipos encontrados");
                
                // Mostrar os primeiros 30
                foreach (var t in types.Take(30))
                {
                    string info = $"{t.Name}";
                    
                    // Verificar se é uma classe com dados (tem propriedades)
                    var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    if (props.Length > 0)
                    {
                        info += $" [{props.Length} props]";
                        
                        // Verificar se tem lista
                        foreach (var p in props)
                        {
                            if (typeof(IList).IsAssignableFrom(p.PropertyType))
                            {
                                info += " ⭐ IList!";
                                break;
                            }
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
        
        private static void FindDataInstances()
        {
            try
            {
                // Buscar tipos que parecem conter dados
                var targetTypes = new[] { "PlayerSearchResult", "PlayerList", "SquadData", "PlayerItem", "PersonList" };
                
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                
                foreach (var asm in assemblies)
                {
                    try
                    {
                        var types = asm.GetTypes();
                        foreach (var t in types)
                        {
                            var name = t.Name;
                            if (!targetTypes.Any(n => name.Contains(n))) continue;
                            
                            Log.LogInfo($"[Inst] Tipo: {t.FullName}");
                            
                            // Tentar encontrar instância estática
                            var props = t.GetProperties(BindingFlags.Static | BindingFlags.Public);
                            foreach (var p in props)
                            {
                                try
                                {
                                    var val = p.GetValue(null);
                                    if (val != null)
                                    {
                                        if (val is IList list && list.Count > 0)
                                        {
                                            Log.LogInfo($"[Inst] ⭐ {p.Name}: List<{list.Count}>");
                                        }
                                        else
                                        {
                                            Log.LogInfo($"[Inst] {p.Name}: {val.GetType().Name}");
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Inst] Erro: {ex.Message}");
            }
        }
        
        private static void TryExportFromTypes()
        {
            try
            {
                // Buscar qualquer tipo que tenha uma lista de objetos
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                
                foreach (var asm in assemblies)
                {
                    try
                    {
                        var types = asm.GetTypes();
                        foreach (var t in types)
                        {
                            // Verificar propriedades estáticas com IList
                            var props = t.GetProperties(BindingFlags.Static | BindingFlags.Public);
                            foreach (var p in props)
                            {
                                try
                                {
                                    var val = p.GetValue(null);
                                    if (val is IList list && list.Count > 5)
                                    {
                                        // Verificar se o primeiro item parece ter dados de jogador
                                        var first = list[0];
                                        if (first != null)
                                        {
                                            var itemType = first.GetType();
                                            var itemProps = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                            
                                            // Procurar propriedades comuns de jogador
                                            var propNames = itemProps.Select(x => x.Name.ToLower()).ToList();
                                            if (propNames.Any(n => n.Contains("name") || n.Contains("age") || n.Contains("club")))
                                            {
                                                Log.LogInfo($"[Export] Encontrado: {t.Name}.{p.Name}: List<{list.Count}>");
                                                ExportCsv(list);
                                                return;
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
                
                Log.LogWarning("[Export] Nenhum dado encontrado nos tipos.");
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
                if (first == null)
                {
                    Log.LogWarning("[CSV] Primeiro item é null");
                    return;
                }
                
                var type = first.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                var csv = new System.Text.StringBuilder();
                var headers = new List<string>();
                
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length == 0 && p.Name.Length < 30) headers.Add(p.Name);
                }
                csv.AppendLine(string.Join(";", headers));
                
                int count = 0;
                foreach (var item in data)
                {
                    if (item == null) continue;
                    
                    var values = new List<string>();
                    foreach (var p in props)
                    {
                        if (p.GetIndexParameters().Length > 0 || p.Name.Length >= 30) continue;
                        try
                        {
                            var val = p.GetValue(item);
                            values.Add((val?.ToString() ?? "").Replace(";", ","));
                        }
                        catch { values.Add(""); }
                    }
                    csv.AppendLine(string.Join(";", values));
                    count++;
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                Log.LogInfo($"[CSV] ✅ {count} linhas em: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[CSV] Erro: {ex.Message}");
            }
        }
    }
}
