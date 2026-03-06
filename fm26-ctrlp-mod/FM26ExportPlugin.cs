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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.45.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.45.0 CARREGADO!");
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
                        Log.LogInfo(">>> F9 - Contar itens com m_value não-null");
                        CountNonEmptyValues();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F9] {ex.Message}"); }
                
                try
                {
                    if (Keyboard.current.f10Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F10 - Explorar TypedValue");
                        ExploreTypedValue();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F10] {ex.Message}"); }
                
                try
                {
                    bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                    bool p = Keyboard.current.pKey.wasPressedThisFrame;
                    
                    if (ctrl && p)
                    {
                        Log.LogInfo(">>> Ctrl+P - Exportar TypedValues");
                        ExportTypedValues();
                    }
                }
                catch (Exception ex) { Log.LogError($"[CtrlP] {ex.Message}"); }
            }
            catch { }
        }
        
        private static object GetMData()
        {
            if (_bindingsInstance == null) return null;
            
            var type = _bindingsInstance.GetType();
            var mDataProp = type.GetProperty("m_data", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            
            if (mDataProp != null) return mDataProp.GetValue(_bindingsInstance);
            
            var mDataField = type.GetField("m_data", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            return mDataField?.GetValue(_bindingsInstance);
        }
        
        private static void CountNonEmptyValues()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) { Log.LogWarning("[Count] m_data null"); return; }
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) { Log.LogWarning("[Count] Sem Count/Indexer"); return; }
                
                int total = (int)countProp.GetValue(mData);
                int withValue = 0;
                int withHandler = 0;
                
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var itemType = item.GetType();
                        var mValueProp = itemType.GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        var handlerProp = itemType.GetProperty("handler", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        
                        if (mValueProp != null)
                        {
                            var val = mValueProp.GetValue(item);
                            if (val != null) withValue++;
                        }
                        
                        if (handlerProp != null)
                        {
                            var handler = handlerProp.GetValue(item);
                            if (handler != null) withHandler++;
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Count] Total: {total}");
                Log.LogInfo($"[Count] Com m_value: {withValue}");
                Log.LogInfo($"[Count] Com handler: {withHandler}");
            }
            catch (Exception ex) { Log.LogError($"[Count] {ex.Message}"); }
        }
        
        private static void ExploreTypedValue()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) { Log.LogWarning("[TV] m_data null"); return; }
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) return;
                
                int total = (int)countProp.GetValue(mData);
                int found = 0;
                
                for (int i = 0; i < total && found < 5; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var itemType = item.GetType();
                        var mValueProp = itemType.GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        
                        if (mValueProp == null) continue;
                        
                        var val = mValueProp.GetValue(item);
                        if (val == null) continue;
                        
                        found++;
                        
                        var valType = val.GetType();
                        Log.LogInfo($"[TV] Item [{i}] - Tipo: {valType.FullName}");
                        
                        // Propriedades do TypedValue
                        var props = valType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        Log.LogInfo($"[TV] {props.Length} propriedades:");
                        
                        foreach (var p in props)
                        {
                            try
                            {
                                var v = p.GetValue(val);
                                var vs = v?.ToString() ?? "null";
                                if (vs.Length > 60) vs = vs.Substring(0, 60) + "...";
                                Log.LogInfo($"[TV]   {p.Name}: {vs}");
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                
                if (found == 0) Log.LogWarning("[TV] Nenhum TypedValue encontrado");
            }
            catch (Exception ex) { Log.LogError($"[TV] {ex.Message}"); }
        }
        
        private static void ExportTypedValues()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) { Log.LogWarning("[Export] m_data null"); return; }
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) return;
                
                int total = (int)countProp.GetValue(mData);
                
                // Coletar todos os TypedValues
                var values = new List<object>();
                
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var itemType = item.GetType();
                        var mValueProp = itemType.GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        
                        if (mValueProp == null) continue;
                        
                        var val = mValueProp.GetValue(item);
                        if (val != null) values.Add(val);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Export] {values.Count} TypedValues");
                
                if (values.Count == 0) { Log.LogWarning("[Export] Nenhum valor"); return; }
                
                // Descobrir propriedades
                var firstType = values[0].GetType();
                var props = firstType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetIndexParameters().Length == 0)
                    .ToList();
                
                var csv = new System.Text.StringBuilder();
                csv.AppendLine(string.Join(";", props.Select(p => p.Name)));
                
                int exported = 0;
                foreach (var val in values)
                {
                    try
                    {
                        var row = props.Select(p =>
                        {
                            try
                            {
                                var v = p.GetValue(val);
                                var s = v?.ToString() ?? "";
                                return s.Replace(";", ",").Replace("\n", " ").Replace("\r", "");
                            }
                            catch { return ""; }
                        });
                        
                        csv.AppendLine(string.Join(";", row));
                        exported++;
                    }
                    catch { }
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                
                Log.LogInfo($"[Export] ✅ {exported} linhas!");
                Log.LogInfo($"[Export] {path}");
            }
            catch (Exception ex) { Log.LogError($"[Export] {ex.Message}"); }
        }
    }
}
