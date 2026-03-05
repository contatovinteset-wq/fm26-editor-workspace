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

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.42.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.42.0 CARREGADO!");
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
        
        private static object _bindingsInstance = null;
        private static int _frameCount = 0;
        private static bool _initialized = false;
        
        public static void OnUpdate(object __instance)
        {
            try
            {
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
                
                try { if (Keyboard.current == null) return; }
                catch { return; }
                
                try
                {
                    if (Keyboard.current.f9Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F9 - Mostrar DataSet.Count");
                        ShowDataSetCount();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F9] {ex.Message}"); }
                
                try
                {
                    if (Keyboard.current.f10Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F10 - Mostrar primeiro item");
                        ShowFirstItem();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F10] {ex.Message}"); }
                
                try
                {
                    bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                    bool p = Keyboard.current.pKey.wasPressedThisFrame;
                    
                    if (ctrl && p)
                    {
                        Log.LogInfo(">>> Ctrl+P - Exportar");
                        ExportData();
                    }
                }
                catch (Exception ex) { Log.LogError($"[CtrlP] {ex.Message}"); }
            }
            catch (Exception ex) { Log.LogError($"[OnUpdate] {ex.Message}"); }
        }
        
        private static void ShowDataSetCount()
        {
            try
            {
                if (_bindingsInstance == null) { Log.LogWarning("[DS] Null instance"); return; }
                
                var type = _bindingsInstance.GetType();
                var dataSetProp = type.GetProperty("DataSet");
                if (dataSetProp == null) { Log.LogWarning("[DS] DataSet prop null"); return; }
                
                var dataSet = dataSetProp.GetValue(_bindingsInstance);
                if (dataSet == null) { Log.LogWarning("[DS] DataSet null"); return; }
                
                // Usar reflexão para Count
                var dsType = dataSet.GetType();
                var countProp = dsType.GetProperty("Count");
                
                if (countProp != null)
                {
                    var count = (int)countProp.GetValue(dataSet);
                    Log.LogInfo($"[DS] Count: {count}");
                }
                else
                {
                    Log.LogWarning("[DS] Count property not found");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[DS] {ex.Message}");
            }
        }
        
        private static void ShowFirstItem()
        {
            try
            {
                if (_bindingsInstance == null) { Log.LogWarning("[Item] Null instance"); return; }
                
                var type = _bindingsInstance.GetType();
                var dataSetProp = type.GetProperty("DataSet");
                if (dataSetProp == null) { Log.LogWarning("[Item] DataSet prop null"); return; }
                
                var dataSet = dataSetProp.GetValue(_bindingsInstance);
                if (dataSet == null) { Log.LogWarning("[Item] DataSet null"); return; }
                
                var dsType = dataSet.GetType();
                
                // Usar indexer via reflexão
                var indexer = dsType.GetProperty("Item");
                if (indexer == null)
                {
                    Log.LogWarning("[Item] Indexer not found");
                    return;
                }
                
                // Pegar primeiro item
                var item = indexer.GetValue(dataSet, new object[] { 0 });
                if (item == null)
                {
                    Log.LogWarning("[Item] First item is null");
                    return;
                }
                
                var itemType = item.GetType();
                Log.LogInfo($"[Item] Tipo: {itemType.FullName}");
                
                // Propriedades
                var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[Item] {props.Length} propriedades:");
                
                foreach (var p in props.Take(20))
                {
                    try
                    {
                        var val = p.GetValue(item);
                        var valStr = val?.ToString() ?? "null";
                        if (valStr.Length > 50) valStr = valStr.Substring(0, 50) + "...";
                        Log.LogInfo($"[Item]   {p.Name}: {valStr}");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Item] {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static void ExportData()
        {
            try
            {
                if (_bindingsInstance == null) { Log.LogWarning("[Export] Null instance"); return; }
                
                var type = _bindingsInstance.GetType();
                var dataSetProp = type.GetProperty("DataSet");
                if (dataSetProp == null) { Log.LogWarning("[Export] DataSet prop null"); return; }
                
                var dataSet = dataSetProp.GetValue(_bindingsInstance);
                if (dataSet == null) { Log.LogWarning("[Export] DataSet null"); return; }
                
                var dsType = dataSet.GetType();
                var countProp = dsType.GetProperty("Count");
                var indexer = dsType.GetProperty("Item");
                
                if (countProp == null || indexer == null)
                {
                    Log.LogWarning("[Export] Count or Indexer not found");
                    return;
                }
                
                var count = (int)countProp.GetValue(dataSet);
                Log.LogInfo($"[Export] {count} itens");
                
                if (count == 0)
                {
                    Log.LogWarning("[Export] Empty");
                    return;
                }
                
                // Pegar primeiro item para descobrir propriedades
                var firstItem = indexer.GetValue(dataSet, new object[] { 0 });
                if (firstItem == null) { Log.LogWarning("[Export] First item null"); return; }
                
                var itemType = firstItem.GetType();
                var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetIndexParameters().Length == 0)
                    .ToList();
                
                var csv = new System.Text.StringBuilder();
                csv.AppendLine(string.Join(";", props.Select(p => p.Name)));
                
                int exported = 0;
                int maxItems = Math.Min(count, 50000);
                
                for (int i = 0; i < maxItems; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(dataSet, new object[] { i });
                        if (item == null) continue;
                        
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
                        exported++;
                    }
                    catch { }
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                
                Log.LogInfo($"[Export] ✅ {exported} linhas!");
                Log.LogInfo($"[Export] {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
