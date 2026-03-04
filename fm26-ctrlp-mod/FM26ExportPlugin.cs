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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.27.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.27.0 CARREGADO!");
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
        private static Type _playerType = null;
        
        public static void OnUpdate()
        {
            try
            {
                _frameCount++;
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    Log.LogInfo("[Init] Pronto!");
                    
                    // Cache do tipo Player
                    _playerType = FindPlayerType();
                    if (_playerType != null)
                    {
                        Log.LogInfo($"[Init] Tipo Player encontrado: {_playerType.FullName}");
                    }
                }
                
                if (!_initialized || Keyboard.current == null) return;
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Exportar jogadores");
                    TryExportPlayers();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Mostrar propriedades do tipo Player");
                    ShowPlayerProperties();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar instâncias de Player");
                    FindPlayerInstances();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static Type FindPlayerType()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var asm in assemblies)
            {
                try
                {
                    var types = asm.GetTypes();
                    foreach (var t in types)
                    {
                        // Buscar tipo Player com muitas propriedades (49)
                        if (t.Name == "Player" && !t.Namespace.Contains("UnityEngine"))
                        {
                            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            if (props.Length >= 40)
                            {
                                return t;
                            }
                        }
                    }
                }
                catch { }
            }
            return null;
        }
        
        private static void ShowPlayerProperties()
        {
            if (_playerType == null)
            {
                Log.LogWarning("[Props] Tipo Player não encontrado");
                return;
            }
            
            try
            {
                Log.LogInfo($"[Props] Tipo: {_playerType.FullName}");
                Log.LogInfo($"[Props] Assembly: {_playerType.Assembly.GetName().Name}");
                
                var props = _playerType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[Props] {props.Length} propriedades públicas:");
                
                foreach (var p in props.Take(30))
                {
                    string info = $"  {p.Name}: {p.PropertyType.Name}";
                    
                    // Destacar propriedades interessantes
                    var name = p.Name.ToLower();
                    if (name.Contains("name") || name.Contains("age") || name.Contains("club") || 
                        name.Contains("position") || name.Contains("nation") || name.Contains("value"))
                    {
                        info += " ⭐";
                    }
                    
                    Log.LogInfo($"[Props] {info}");
                }
                
                // Verificar métodos estáticos
                var staticMethods = _playerType.GetMethods(BindingFlags.Static | BindingFlags.Public);
                Log.LogInfo($"[Props] {staticMethods.Length} métodos estáticos:");
                foreach (var m in staticMethods.Take(10))
                {
                    Log.LogInfo($"[Props]   static {m.ReturnType.Name} {m.Name}()");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Props] Erro: {ex.Message}");
            }
        }
        
        private static void FindPlayerInstances()
        {
            if (_playerType == null)
            {
                Log.LogWarning("[Inst] Tipo Player não encontrado");
                return;
            }
            
            try
            {
                Log.LogInfo("[Inst] Buscando instâncias de Player...");
                
                // 1. Propriedades estáticas
                var staticProps = _playerType.GetProperties(BindingFlags.Static | BindingFlags.Public);
                foreach (var p in staticProps)
                {
                    try
                    {
                        var val = p.GetValue(null);
                        if (val != null)
                        {
                            if (val is IList list)
                            {
                                Log.LogInfo($"[Inst] ⭐⭐⭐ {p.Name}: List<{list.Count}>");
                            }
                            else if (val.GetType().Name == "Player")
                            {
                                Log.LogInfo($"[Inst] ⭐ {p.Name}: Player instance");
                            }
                            else
                            {
                                Log.LogInfo($"[Inst] {p.Name}: {val.GetType().Name}");
                            }
                        }
                    }
                    catch { }
                }
                
                // 2. Buscar em outros tipos
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    try
                    {
                        var types = asm.GetTypes();
                        foreach (var t in types)
                        {
                            // Tipos com nomes suspeitos
                            var name = t.Name.ToLower();
                            if (!name.Contains("squad") && !name.Contains("team") && !name.Contains("roster")) continue;
                            
                            var staticProps2 = t.GetProperties(BindingFlags.Static | BindingFlags.Public);
                            foreach (var p in staticProps2)
                            {
                                try
                                {
                                    var val = p.GetValue(null);
                                    if (val is IList list && list.Count > 0)
                                    {
                                        // Verificar se é lista de Player
                                        var first = list[0];
                                        if (first != null && first.GetType().Name == "Player")
                                        {
                                            Log.LogInfo($"[Inst] ⭐⭐⭐ {t.Name}.{p.Name}: List<Player> ({list.Count} itens)");
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
                
                // 3. Buscar UnityEngine.Object.FindObjectsOfType para Player (MonoBehaviour)
                try
                {
                    var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", Type.EmptyTypes);
                    if (findMethod != null)
                    {
                        var genericMethod = findMethod.MakeGenericMethod(_playerType);
                        var players = genericMethod.Invoke(null, null) as Array;
                        if (players != null && players.Length > 0)
                        {
                            Log.LogInfo($"[Inst] ⭐⭐⭐ FindObjectOfType: {players.Length} Player objects");
                        }
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Inst] Erro: {ex.Message}");
            }
        }
        
        private static void TryExportPlayers()
        {
            if (_playerType == null)
            {
                Log.LogWarning("[Export] Tipo Player não encontrado");
                return;
            }
            
            try
            {
                // Buscar qualquer lista de Player
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                
                foreach (var asm in assemblies)
                {
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
                                    if (val is IList list && list.Count > 0)
                                    {
                                        var first = list[0];
                                        if (first != null && first.GetType().Name == "Player")
                                        {
                                            Log.LogInfo($"[Export] Encontrado: {t.Name}.{p.Name}: {list.Count} jogadores");
                                            ExportCsv(list);
                                            return;
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
                
                Log.LogWarning("[Export] Nenhuma lista de Player encontrada");
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
                
                // Headers
                csv.AppendLine(string.Join(";", props.Select(p => p.Name)));
                
                // Data
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
                Log.LogInfo($"[CSV] ✅ {count} jogadores exportados: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[CSV] Erro: {ex.Message}");
            }
        }
    }
}
