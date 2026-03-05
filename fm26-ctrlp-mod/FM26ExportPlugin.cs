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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.40.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.40.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            
            try
            {
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
            catch (Exception ex)
            {
                Log.LogError($"[Init] Erro no patch: {ex.Message}");
            }
        }
        
        // Instância capturada do Bindings
        private static object _bindingsInstance = null;
        private static int _frameCount = 0;
        private static bool _initialized = false;
        
        public static void OnUpdate(object __instance)
        {
            try
            {
                // Capturar instância
                if (_bindingsInstance == null && __instance != null)
                {
                    _bindingsInstance = __instance;
                    Log.LogInfo("[Hook] Bindings capturada!");
                }
                
                _frameCount++;
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    Log.LogInfo("[Init] Pronto!");
                }
                
                if (!_initialized) return;
                
                try
                {
                    if (Keyboard.current == null) return;
                }
                catch { return; }
                
                try
                {
                    if (Keyboard.current.f9Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F9 - Mostrar Bindings.DataSet");
                        ShowDataSet();
                    }
                }
                catch { }
                
                try
                {
                    if (Keyboard.current.f10Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F10 - Mostrar primeiro IReadOnlyData");
                        ShowFirstDataItem();
                    }
                }
                catch { }
                
                try
                {
                    bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                    bool p = Keyboard.current.pKey.wasPressedThisFrame;
                    
                    if (ctrl && p)
                    {
                        Log.LogInfo(">>> Ctrl+P - Exportar DataSet para CSV");
                        ExportDataSet();
                    }
                }
                catch { }
            }
            catch { }
        }
        
        private static void ShowDataSet()
        {
            try
            {
                if (_bindingsInstance == null)
                {
                    Log.LogWarning("[DS] Nenhuma instância de Bindings capturada");
                    return;
                }
                
                var type = _bindingsInstance.GetType();
                var dataSetProp = type.GetProperty("DataSet");
                
                if (dataSetProp == null)
                {
                    Log.LogWarning("[DS] Propriedade DataSet não encontrada");
                    return;
                }
                
                var dataSet = dataSetProp.GetValue(_bindingsInstance);
                if (dataSet == null)
                {
                    Log.LogWarning("[DS] DataSet é null");
                    return;
                }
                
                // Contar itens
                var countProp = dataSet.GetType().GetProperty("Count");
                if (countProp != null)
                {
                    var count = (int)countProp.GetValue(dataSet);
                    Log.LogInfo($"[DS] DataSet.Count: {count}");
                }
                
                // Tentar iterar
                var enumerable = dataSet as IEnumerable;
                if (enumerable != null)
                {
                    int i = 0;
                    foreach (var item in enumerable)
                    {
                        if (i < 3) // Mostrar primeiros 3
                        {
                            var itemType = item?.GetType().Name ?? "null";
                            Log.LogInfo($"[DS]   [{i}]: {itemType}");
                        }
                        i++;
                        if (i >= 1000) break; // Limite de segurança
                    }
                    Log.LogInfo($"[DS] Total iterado: {i}");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[DS] Erro: {ex.Message}");
            }
        }
        
        private static void ShowFirstDataItem()
        {
            try
            {
                if (_bindingsInstance == null)
                {
                    Log.LogWarning("[Item] Nenhuma instância de Bindings");
                    return;
                }
                
                var type = _bindingsInstance.GetType();
                var dataSetProp = type.GetProperty("DataSet");
                
                if (dataSetProp == null) return;
                
                var dataSet = dataSetProp.GetValue(_bindingsInstance) as IEnumerable;
                if (dataSet == null) return;
                
                foreach (var item in dataSet)
                {
                    if (item == null) continue;
                    
                    var itemType = item.GetType();
                    Log.LogInfo($"[Item] Tipo: {itemType.FullName}");
                    
                    // Listar propriedades
                    var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    Log.LogInfo($"[Item] {props.Length} propriedades:");
                    
                    foreach (var p in props.Take(20))
                    {
                        try
                        {
                            var val = p.GetValue(item);
                            var valStr = val?.ToString() ?? "null";
                            if (valStr.Length > 60) valStr = valStr.Substring(0, 60) + "...";
                            Log.LogInfo($"[Item]   {p.Name}: {valStr}");
                        }
                        catch { }
                    }
                    
                    return; // Só primeiro item
                }
                
                Log.LogWarning("[Item] DataSet vazio");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Item] Erro: {ex.Message}");
            }
        }
        
        private static void ExportDataSet()
        {
            try
            {
                if (_bindingsInstance == null)
                {
                    Log.LogWarning("[Export] Nenhuma instância de Bindings");
                    return;
                }
                
                var type = _bindingsInstance.GetType();
                var dataSetProp = type.GetProperty("DataSet");
                
                if (dataSetProp == null) return;
                
                var dataSet = dataSetProp.GetValue(_bindingsInstance) as IEnumerable;
                if (dataSet == null) return;
                
                var items = new List<object>();
                foreach (var item in dataSet)
                {
                    if (item != null) items.Add(item);
                    if (items.Count >= 50000) break;
                }
                
                if (items.Count == 0)
                {
                    Log.LogWarning("[Export] DataSet vazio");
                    return;
                }
                
                Log.LogInfo($"[Export] {items.Count} itens para exportar");
                
                // Descobrir propriedades do primeiro item
                var firstType = items[0].GetType();
                var props = firstType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetIndexParameters().Length == 0)
                    .ToList();
                
                var csv = new System.Text.StringBuilder();
                csv.AppendLine(string.Join(";", props.Select(p => p.Name)));
                
                int count = 0;
                foreach (var item in items)
                {
                    try
                    {
                        var values = props.Select(p =>
                        {
                            try
                            {
                                var val = p.GetValue(item);
                                var str = val?.ToString() ?? "";
                                return str.Replace(";", ",").Replace("\n", " ").Replace("\r", "");
                            }
                            catch { return ""; }
                        });
                        csv.AppendLine(string.Join(";", values));
                        count++;
                    }
                    catch { }
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                
                Log.LogInfo($"[Export] ✅ {count} linhas exportadas!");
                Log.LogInfo($"[Export] Arquivo: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
    }
}
