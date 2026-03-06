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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.44.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.44.0 CARREGADO!");
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
                        Log.LogInfo(">>> F9 - Explorar TODAS as propriedades do Bindings");
                        ExploreBindings();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F9] {ex.Message}"); }
                
                try
                {
                    if (Keyboard.current.f10Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F10 - Explorar m_data");
                        ExploreMData();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F10] {ex.Message}"); }
                
                try
                {
                    bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                    bool p = Keyboard.current.pKey.wasPressedThisFrame;
                    
                    if (ctrl && p)
                    {
                        Log.LogInfo(">>> Ctrl+P - Debug m_handlers");
                        ExploreHandlers();
                    }
                }
                catch (Exception ex) { Log.LogError($"[CtrlP] {ex.Message}"); }
            }
            catch { }
        }
        
        private static void ExploreBindings()
        {
            try
            {
                if (_bindingsInstance == null) { Log.LogWarning("[Exp] Null"); return; }
                
                var type = _bindingsInstance.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                
                Log.LogInfo($"[Exp] {props.Length} propriedades:");
                
                foreach (var p in props)
                {
                    try
                    {
                        var val = p.GetValue(_bindingsInstance);
                        var valType = val?.GetType().Name ?? "null";
                        
                        // Se for lista/dict, mostrar count
                        string extra = "";
                        if (val != null)
                        {
                            var countProp = val.GetType().GetProperty("Count");
                            if (countProp != null)
                            {
                                try
                                {
                                    var count = countProp.GetValue(val);
                                    extra = $" (Count={count})";
                                }
                                catch { }
                            }
                        }
                        
                        Log.LogInfo($"[Exp]   {p.Name}: {valType}{extra}");
                    }
                    catch { }
                }
            }
            catch (Exception ex) { Log.LogError($"[Exp] {ex.Message}"); }
        }
        
        private static void ExploreMData()
        {
            try
            {
                if (_bindingsInstance == null) { Log.LogWarning("[Data] Null"); return; }
                
                var type = _bindingsInstance.GetType();
                var mDataProp = type.GetProperty("m_data", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                
                if (mDataProp == null)
                {
                    // Tentar campo
                    var mDataField = type.GetField("m_data", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    if (mDataField == null)
                    {
                        Log.LogWarning("[Data] m_data não encontrado");
                        return;
                    }
                    
                    var mData = mDataField.GetValue(_bindingsInstance);
                    if (mData == null) { Log.LogWarning("[Data] null"); return; }
                    
                    ProcessMData(mData);
                }
                else
                {
                    var mData = mDataProp.GetValue(_bindingsInstance);
                    if (mData == null) { Log.LogWarning("[Data] null"); return; }
                    
                    ProcessMData(mData);
                }
            }
            catch (Exception ex) { Log.LogError($"[Data] {ex.Message}"); }
        }
        
        private static void ProcessMData(object mData)
        {
            try
            {
                var listType = mData.GetType();
                Log.LogInfo($"[Data] Tipo: {listType.FullName}");
                
                // Count
                var countProp = listType.GetProperty("Count");
                if (countProp != null)
                {
                    var count = (int)countProp.GetValue(mData);
                    Log.LogInfo($"[Data] Count: {count}");
                }
                
                // Indexer
                var indexer = listType.GetProperty("Item");
                if (indexer == null)
                {
                    Log.LogWarning("[Data] Sem indexer");
                    return;
                }
                
                // Listar primeiros itens
                int count2 = countProp != null ? (int)countProp.GetValue(mData) : 10;
                int max = Math.Min(count2, 10);
                
                Log.LogInfo($"[Data] Primeiros {max} itens:");
                
                for (int i = 0; i < max; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) { Log.LogInfo($"[Data]   [{i}] = null"); continue; }
                        
                        var itemType = item.GetType();
                        Log.LogInfo($"[Data]   [{i}] {itemType.Name}");
                        
                        // Propriedades do item
                        var itemProps = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var ip in itemProps.Take(5))
                        {
                            try
                            {
                                var v = ip.GetValue(item);
                                var vs = v?.ToString() ?? "null";
                                if (vs.Length > 40) vs = vs.Substring(0, 40) + "...";
                                Log.LogInfo($"[Data]       {ip.Name}: {vs}");
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex) { Log.LogInfo($"[Data]   [{i}] ERRO: {ex.Message}"); }
                }
            }
            catch (Exception ex) { Log.LogError($"[Data] {ex.Message}"); }
        }
        
        private static void ExploreHandlers()
        {
            try
            {
                if (_bindingsInstance == null) { Log.LogWarning("[Hand] Null"); return; }
                
                var type = _bindingsInstance.GetType();
                
                // m_handlers
                var handlersField = type.GetField("m_handlers", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (handlersField == null)
                {
                    handlersField = type.GetField("_handlers", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                }
                
                if (handlersField == null)
                {
                    // Tentar propriedade
                    var handlersProp = type.GetProperty("m_handlers", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    if (handlersProp == null)
                    {
                        Log.LogWarning("[Hand] m_handlers não encontrado");
                        
                        // Listar campos disponíveis
                        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        Log.LogInfo($"[Hand] {fields.Length} campos:");
                        foreach (var f in fields.Take(20))
                        {
                            Log.LogInfo($"[Hand]   {f.Name}: {f.FieldType.Name}");
                        }
                        return;
                    }
                    
                    var handlers = handlersProp.GetValue(_bindingsInstance);
                    if (handlers != null)
                    {
                        Log.LogInfo($"[Hand] Tipo: {handlers.GetType().FullName}");
                    }
                }
                else
                {
                    var handlers = handlersField.GetValue(_bindingsInstance);
                    if (handlers == null) { Log.LogWarning("[Hand] null"); return; }
                    
                    Log.LogInfo($"[Hand] Tipo: {handlers.GetType().FullName}");
                    
                    // Se for Dictionary
                    var countProp = handlers.GetType().GetProperty("Count");
                    if (countProp != null)
                    {
                        var count = countProp.GetValue(handlers);
                        Log.LogInfo($"[Hand] Count: {count}");
                    }
                }
            }
            catch (Exception ex) { Log.LogError($"[Hand] {ex.Message}"); }
        }
    }
}
