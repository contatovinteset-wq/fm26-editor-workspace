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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.43.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.43.0 CARREGADO!");
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
            catch (Exception ex) { Log.LogError($"[Init] Erro: {ex.Message}"); }
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
                        Log.LogInfo(">>> F9 - Mostrar todos os métodos/props do DataSet");
                        ShowDataSetDetails();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F9] {ex.Message}"); }
                
                try
                {
                    if (Keyboard.current.f10Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F10 - Mostrar primeiros 20 itens");
                        ShowFirstItems();
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
            catch { }
        }
        
        private static void ShowDataSetDetails()
        {
            try
            {
                if (_bindingsInstance == null) { Log.LogWarning("[DS] Null"); return; }
                
                var type = _bindingsInstance.GetType();
                var dataSetProp = type.GetProperty("DataSet");
                if (dataSetProp == null) { Log.LogWarning("[DS] Prop null"); return; }
                
                var dataSet = dataSetProp.GetValue(_bindingsInstance);
                if (dataSet == null) { Log.LogWarning("[DS] Value null"); return; }
                
                var dsType = dataSet.GetType();
                Log.LogInfo($"[DS] Tipo: {dsType.FullName}");
                
                // Listar TODOS os métodos
                var methods = dsType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[DS] {methods.Length} métodos:");
                foreach (var m in methods.Take(30))
                {
                    Log.LogInfo($"[DS]   {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.Name))})");
                }
                
                // Listar TODAS as propriedades
                var props = dsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[DS] {props.Length} propriedades:");
                foreach (var p in props)
                {
                    Log.LogInfo($"[DS]   {p.Name}: {p.PropertyType.Name}");
                }
                
                // Interfaces
                var interfaces = dsType.GetInterfaces();
                Log.LogInfo($"[DS] {interfaces.Length} interfaces:");
                foreach (var i in interfaces)
                {
                    Log.LogInfo($"[DS]   {i.Name}");
                }
            }
            catch (Exception ex) { Log.LogError($"[DS] {ex.Message}"); }
        }
        
        private static void ShowFirstItems()
        {
            try
            {
                if (_bindingsInstance == null) { Log.LogWarning("[Item] Null"); return; }
                
                var type = _bindingsInstance.GetType();
                var dataSetProp = type.GetProperty("DataSet");
                if (dataSetProp == null) { Log.LogWarning("[Item] Prop null"); return; }
                
                var dataSet = dataSetProp.GetValue(_bindingsInstance);
                if (dataSet == null) { Log.LogWarning("[Item] Value null"); return; }
                
                var dsType = dataSet.GetType();
                
                // Tentar obter Count
                int count = 0;
                var countProp = dsType.GetProperty("Count");
                if (countProp != null)
                {
                    count = (int)countProp.GetValue(dataSet);
                    Log.LogInfo($"[Item] Count: {count}");
                }
                
                // Tentar indexer
                var indexer = dsType.GetProperty("Item");
                if (indexer == null)
                {
                    Log.LogWarning("[Item] Sem indexer");
                    return;
                }
                
                // Mostrar primeiros 20
                int maxItems = count > 0 ? Math.Min(count, 20) : 20;
                int found = 0;
                
                for (int i = 0; i < maxItems && found < 20; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(dataSet, new object[] { i });
                        if (item == null) continue;
                        
                        found++;
                        
                        // Pegar Key e Value
                        var itemType = item.GetType();
                        var keyProp = itemType.GetProperty("Key");
                        var valueProp = itemType.GetProperty("Value");
                        
                        var key = keyProp?.GetValue(item);
                        var value = valueProp?.GetValue(item);
                        
                        var keyStr = key?.ToString() ?? "null";
                        var valueStr = value?.ToString() ?? "null";
                        
                        if (valueStr.Length > 40) valueStr = valueStr.Substring(0, 40) + "...";
                        
                        Log.LogInfo($"[Item] [{i}] Key={keyStr}, Value={valueStr}");
                        
                        // Se Value não é null, mostrar tipo
                        if (value != null)
                        {
                            var valueType = value.GetType();
                            Log.LogInfo($"[Item]     ValueType: {valueType.Name}");
                            
                            // Se for TypedValue, mostrar propriedades
                            if (valueType.Name.Contains("TypedValue"))
                            {
                                var tvProps = valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                foreach (var p in tvProps.Take(5))
                                {
                                    try
                                    {
                                        var v = p.GetValue(value);
                                        var vs = v?.ToString() ?? "null";
                                        if (vs.Length > 30) vs = vs.Substring(0, 30) + "...";
                                        Log.LogInfo($"[Item]       {p.Name}: {vs}");
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Item] Encontrados: {found}");
            }
            catch (Exception ex) { Log.LogError($"[Item] {ex.Message}"); }
        }
        
        private static void ExportData()
        {
            try
            {
                if (_bindingsInstance == null) { Log.LogWarning("[Export] Null"); return; }
                
                var type = _bindingsInstance.GetType();
                var dataSetProp = type.GetProperty("DataSet");
                if (dataSetProp == null) { Log.LogWarning("[Export] Prop null"); return; }
                
                var dataSet = dataSetProp.GetValue(_bindingsInstance);
                if (dataSet == null) { Log.LogWarning("[Export] Value null"); return; }
                
                var dsType = dataSet.GetType();
                
                // Count
                var countProp = dsType.GetProperty("Count");
                if (countProp == null) { Log.LogWarning("[Export] Sem Count"); return; }
                
                int count = (int)countProp.GetValue(dataSet);
                Log.LogInfo($"[Export] Count: {count}");
                
                if (count == 0) { Log.LogWarning("[Export] Empty"); return; }
                
                // Indexer
                var indexer = dsType.GetProperty("Item");
                if (indexer == null) { Log.LogWarning("[Export] Sem indexer"); return; }
                
                // Coletar todos os itens
                var items = new List<object>();
                for (int i = 0; i < count && i < 50000; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(dataSet, new object[] { i });
                        if (item != null) items.Add(item);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Export] {items.Count} itens coletados");
                
                if (items.Count == 0) { Log.LogWarning("[Export] Nenhum item"); return; }
                
                // Descobrir estrutura do primeiro item
                var first = items[0];
                var firstType = first.GetType();
                Log.LogInfo($"[Export] Tipo: {firstType.Name}");
                
                // IReadOnlyData tem Key e Value
                var keyProp = firstType.GetProperty("Key");
                var valueProp = firstType.GetProperty("Value");
                
                if (keyProp == null || valueProp == null)
                {
                    Log.LogWarning("[Export] Sem Key/Value");
                    return;
                }
                
                // Verificar primeiro item com Value não-null
                object firstWithValue = null;
                foreach (var item in items)
                {
                    try
                    {
                        var v = valueProp.GetValue(item);
                        if (v != null) { firstWithValue = item; break; }
                    }
                    catch { }
                }
                
                if (firstWithValue == null)
                {
                    Log.LogWarning("[Export] Todos os Values são null");
                    return;
                }
                
                var val = valueProp.GetValue(firstWithValue);
                var valType = val.GetType();
                Log.LogInfo($"[Export] ValueType: {valType.Name}");
                
                // Propriedades do Value
                var valProps = valType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetIndexParameters().Length == 0)
                    .ToList();
                
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Key;" + string.Join(";", valProps.Select(p => p.Name)));
                
                int exported = 0;
                foreach (var item in items)
                {
                    try
                    {
                        var k = keyProp.GetValue(item);
                        var v = valueProp.GetValue(item);
                        
                        if (v == null) continue;
                        
                        var keyStr = k?.ToString() ?? "";
                        
                        var valStrs = valProps.Select(p =>
                        {
                            try
                            {
                                var x = p.GetValue(v);
                                var s = x?.ToString() ?? "";
                                return s.Replace(";", ",").Replace("\n", " ").Replace("\r", "");
                            }
                            catch { return ""; }
                        });
                        
                        csv.AppendLine(keyStr + ";" + string.Join(";", valStrs));
                        exported++;
                    }
                    catch { }
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                
                Log.LogInfo($"[Export] ✅ {exported} linhas!");
                Log.LogInfo($"[Export] {path}");
            }
            catch (Exception ex) { Log.LogError($"[Export] {ex.Message}\n{ex.StackTrace}"); }
        }
    }
}
