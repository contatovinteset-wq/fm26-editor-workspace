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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.41.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.41.0 CARREGADO!");
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
                
                try
                {
                    if (Keyboard.current == null) return;
                }
                catch { return; }
                
                try
                {
                    if (Keyboard.current.f9Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F9 pressionado");
                        ShowDataSet();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F9] Erro: {ex.Message}"); }
                
                try
                {
                    if (Keyboard.current.f10Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F10 pressionado");
                        ShowFirstDataItem();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F10] Erro: {ex.Message}"); }
                
                try
                {
                    bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                    bool p = Keyboard.current.pKey.wasPressedThisFrame;
                    
                    if (ctrl && p)
                    {
                        Log.LogInfo(">>> Ctrl+P pressionado");
                        ExportDataSet();
                    }
                }
                catch (Exception ex) { Log.LogError($"[CtrlP] Erro: {ex.Message}"); }
            }
            catch (Exception ex) { Log.LogError($"[OnUpdate] Erro: {ex.Message}"); }
        }
        
        private static void ShowDataSet()
        {
            try
            {
                Log.LogInfo("[DS] Iniciando...");
                
                if (_bindingsInstance == null)
                {
                    Log.LogWarning("[DS] _bindingsInstance é null!");
                    return;
                }
                
                Log.LogInfo("[DS] _bindingsInstance OK");
                
                var type = _bindingsInstance.GetType();
                Log.LogInfo($"[DS] Tipo: {type.FullName}");
                
                var dataSetProp = type.GetProperty("DataSet");
                if (dataSetProp == null)
                {
                    Log.LogWarning("[DS] Propriedade DataSet não encontrada");
                    
                    // Listar todas as propriedades
                    var allProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    Log.LogInfo($"[DS] Propriedades disponíveis: {string.Join(", ", allProps.Select(p => p.Name))}");
                    return;
                }
                
                Log.LogInfo("[DS] Propriedade DataSet encontrada, tentando GetValue...");
                
                object dataSet = null;
                try
                {
                    dataSet = dataSetProp.GetValue(_bindingsInstance);
                }
                catch (Exception ex)
                {
                    Log.LogError($"[DS] Erro ao obter DataSet: {ex.Message}");
                    return;
                }
                
                if (dataSet == null)
                {
                    Log.LogWarning("[DS] DataSet é null");
                    return;
                }
                
                Log.LogInfo($"[DS] DataSet OK! Tipo: {dataSet.GetType().FullName}");
                
                // Contar
                var countProp = dataSet.GetType().GetProperty("Count");
                if (countProp != null)
                {
                    try
                    {
                        var count = (int)countProp.GetValue(dataSet);
                        Log.LogInfo($"[DS] DataSet.Count: {count}");
                    }
                    catch (Exception ex) { Log.LogError($"[DS] Erro ao obter Count: {ex.Message}"); }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[DS] Erro geral: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static void ShowFirstDataItem()
        {
            try
            {
                Log.LogInfo("[Item] Iniciando...");
                
                if (_bindingsInstance == null)
                {
                    Log.LogWarning("[Item] _bindingsInstance é null!");
                    return;
                }
                
                var type = _bindingsInstance.GetType();
                var dataSetProp = type.GetProperty("DataSet");
                
                if (dataSetProp == null)
                {
                    Log.LogWarning("[Item] DataSet property não encontrada");
                    return;
                }
                
                var dataSet = dataSetProp.GetValue(_bindingsInstance);
                if (dataSet == null)
                {
                    Log.LogWarning("[Item] DataSet é null");
                    return;
                }
                
                // Tentar como IEnumerable
                var enumerable = dataSet as IEnumerable;
                if (enumerable == null)
                {
                    Log.LogWarning($"[Item] DataSet não é IEnumerable. Tipo: {dataSet.GetType().FullName}");
                    return;
                }
                
                Log.LogInfo("[Item] Iterando...");
                int i = 0;
                foreach (var item in enumerable)
                {
                    if (item == null)
                    {
                        Log.LogInfo($"[Item] [{i}] = null");
                        i++;
                        continue;
                    }
                    
                    var itemType = item.GetType();
                    Log.LogInfo($"[Item] [{i}] Tipo: {itemType.FullName}");
                    
                    // Propriedades
                    var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    Log.LogInfo($"[Item] {props.Length} propriedades");
                    
                    foreach (var p in props.Take(15))
                    {
                        try
                        {
                            var val = p.GetValue(item);
                            var valStr = val?.ToString() ?? "null";
                            if (valStr.Length > 50) valStr = valStr.Substring(0, 50) + "...";
                            Log.LogInfo($"[Item]   {p.Name}: {valStr}");
                        }
                        catch (Exception ex) { Log.LogInfo($"[Item]   {p.Name}: ERRO - {ex.Message}"); }
                    }
                    
                    i++;
                    if (i >= 3) break; // Primeiros 3
                }
                
                Log.LogInfo($"[Item] Total: {i} itens");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Item] Erro: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static void ExportDataSet()
        {
            try
            {
                Log.LogInfo("[Export] Iniciando...");
                
                if (_bindingsInstance == null)
                {
                    Log.LogWarning("[Export] _bindingsInstance é null!");
                    return;
                }
                
                var type = _bindingsInstance.GetType();
                var dataSetProp = type.GetProperty("DataSet");
                
                if (dataSetProp == null)
                {
                    Log.LogWarning("[Export] DataSet property não encontrada");
                    return;
                }
                
                var dataSet = dataSetProp.GetValue(_bindingsInstance);
                if (dataSet == null)
                {
                    Log.LogWarning("[Export] DataSet é null");
                    return;
                }
                
                var enumerable = dataSet as IEnumerable;
                if (enumerable == null)
                {
                    Log.LogWarning("[Export] DataSet não é IEnumerable");
                    return;
                }
                
                var items = new List<object>();
                foreach (var item in enumerable)
                {
                    if (item != null) items.Add(item);
                    if (items.Count >= 50000) break;
                }
                
                if (items.Count == 0)
                {
                    Log.LogWarning("[Export] DataSet vazio");
                    return;
                }
                
                Log.LogInfo($"[Export] {items.Count} itens");
                
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
                
                Log.LogInfo($"[Export] ✅ {count} linhas!");
                Log.LogInfo($"[Export] Arquivo: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
