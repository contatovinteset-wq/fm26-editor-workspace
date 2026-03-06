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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.50.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.50.0 CARREGADO!");
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
                        Log.LogInfo(">>> F9 - Listar TODOS os métodos do TypedValue");
                        ListAllMethods();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F9] {ex.Message}"); }
                
                try
                {
                    if (Keyboard.current.f10Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F10 - Buscar checkboxes selecionados na UI");
                        FindCheckboxes();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F10] {ex.Message}"); }
                
                try
                {
                    bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                    bool p = Keyboard.current.pKey.wasPressedThisFrame;
                    
                    if (ctrl && p)
                    {
                        Log.LogInfo(">>> Ctrl+P - Exportar jogadores selecionados");
                        ExportSelected();
                    }
                }
                catch (Exception ex) { Log.LogError($"[CtrlP] {ex.Message}"); }
            }
            catch { }
        }
        
        private static void ListAllMethods()
        {
            try
            {
                var typedValueType = Type.GetType("SI.Core.TypedValue, SI.Core");
                if (typedValueType == null) { Log.LogWarning("[Meth] Tipo não encontrado"); return; }
                
                Log.LogInfo($"[Meth] Tipo: {typedValueType.FullName}");
                
                var methods = typedValueType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[Meth] {methods.Length} métodos:");
                
                foreach (var m in methods)
                {
                    var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Log.LogInfo($"[Meth]   {m.ReturnType.Name} {m.Name}({pars})");
                }
                
                var fields = typedValueType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                Log.LogInfo($"[Meth] {fields.Length} campos:");
                
                foreach (var f in fields)
                {
                    Log.LogInfo($"[Meth]   {f.FieldType.Name} {f.Name}");
                }
            }
            catch (Exception ex) { Log.LogError($"[Meth] {ex.Message}"); }
        }
        
        private static List<Toggle> GetAllToggles(VisualElement root)
        {
            var toggles = new List<Toggle>();
            try
            {
                // Navegar manualmente pela árvore
                void FindToggles(VisualElement element, int depth)
                {
                    if (element == null || depth > 10) return;
                    
                    try
                    {
                        if (element is Toggle toggle)
                        {
                            toggles.Add(toggle);
                        }
                    }
                    catch { }
                    
                    // Filhos
                    try
                    {
                        int childCount = element.childCount;
                        for (int i = 0; i < childCount && toggles.Count < 1000; i++)
                        {
                            try
                            {
                                var child = element.ElementAt(i);
                                FindToggles(child, depth + 1);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                
                FindToggles(root, 0);
            }
            catch { }
            
            return toggles;
        }
        
        private static void FindCheckboxes()
        {
            try
            {
                var uiDocs = UnityEngine.Object.FindObjectsOfType<UIDocument>();
                Log.LogInfo($"[UI] {uiDocs.Length} UIDocuments");
                
                foreach (var doc in uiDocs)
                {
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        Log.LogInfo($"[UI] Document: {doc.name}");
                        
                        var toggles = GetAllToggles(root);
                        Log.LogInfo($"[UI] {toggles.Count} Toggles");
                        
                        int selected = 0;
                        int count = 0;
                        
                        foreach (var toggle in toggles)
                        {
                            if (count >= 20) break;
                            count++;
                            
                            try
                            {
                                if (toggle.value)
                                {
                                    selected++;
                                    Log.LogInfo($"[UI] Toggle selecionado: {toggle.name}");
                                    
                                    var parent = toggle.parent;
                                    int depth = 0;
                                    while (parent != null && depth < 3)
                                    {
                                        Log.LogInfo($"[UI]   Parent[{depth}]: {parent.GetType().Name} '{parent.name}'");
                                        parent = parent.parent;
                                        depth++;
                                    }
                                }
                            }
                            catch { }
                        }
                        
                        Log.LogInfo($"[UI] {selected} selecionados (de {count} verificados)");
                    }
                    catch { }
                }
            }
            catch (Exception ex) { Log.LogError($"[UI] {ex.Message}"); }
        }
        
        private static void ExportSelected()
        {
            try
            {
                var uiDocs = UnityEngine.Object.FindObjectsOfType<UIDocument>();
                
                int totalSelected = 0;
                var selectedRows = new List<VisualElement>();
                
                foreach (var doc in uiDocs)
                {
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        var toggles = GetAllToggles(root);
                        
                        foreach (var toggle in toggles)
                        {
                            try
                            {
                                if (toggle.value)
                                {
                                    totalSelected++;
                                    
                                    var parent = toggle.parent;
                                    int depth = 0;
                                    while (parent != null && depth < 5)
                                    {
                                        var parentType = parent.GetType().Name;
                                        if (parentType.Contains("Row") || parentType.Contains("Item") || parentType.Contains("Cell"))
                                        {
                                            selectedRows.Add(parent);
                                            break;
                                        }
                                        parent = parent.parent;
                                        depth++;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Export] {totalSelected} checkboxes selecionados");
                Log.LogInfo($"[Export] {selectedRows.Count} linhas encontradas");
                
                if (selectedRows.Count == 0)
                {
                    Log.LogWarning("[Export] Nenhuma linha encontrada. Tentando m_data...");
                    ExportFromMData();
                    return;
                }
                
                if (selectedRows.Count > 0)
                {
                    var firstRow = selectedRows[0];
                    Log.LogInfo($"[Export] Primeira linha: {firstRow.GetType().Name}");
                    
                    var props = firstRow.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    Log.LogInfo($"[Export] {props.Length} propriedades:");
                    
                    foreach (var p in props)
                    {
                        try
                        {
                            var v = p.GetValue(firstRow);
                            var vs = v?.ToString() ?? "null";
                            if (vs.Length > 40) vs = vs.Substring(0, 40) + "...";
                            Log.LogInfo($"[Export]   {p.Name}: {vs}");
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex) { Log.LogError($"[Export] {ex.Message}"); }
        }
        
        private static void ExportFromMData()
        {
            try
            {
                if (_bindingsInstance == null) { Log.LogWarning("[Data] Bindings null"); return; }
                
                var type = _bindingsInstance.GetType();
                var mDataProp = type.GetProperty("m_data", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                var mDataField = type.GetField("m_data", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                
                object mData = null;
                if (mDataProp != null) mData = mDataProp.GetValue(_bindingsInstance);
                else if (mDataField != null) mData = mDataField.GetValue(_bindingsInstance);
                
                if (mData == null) { Log.LogWarning("[Data] m_data null"); return; }
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) { Log.LogWarning("[Data] Sem Count/Indexer"); return; }
                
                int total = (int)countProp.GetValue(mData);
                Log.LogInfo($"[Data] Total: {total} itens");
                
                if (total > 0)
                {
                    var item = indexer.GetValue(mData, new object[] { 0 });
                    if (item != null)
                    {
                        var itemType = item.GetType();
                        Log.LogInfo($"[Data] Item tipo: {itemType.Name}");
                        
                        var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        Log.LogInfo($"[Data] {props.Length} propriedades:");
                        
                        foreach (var p in props)
                        {
                            try
                            {
                                var v = p.GetValue(item);
                                var vt = v?.GetType().Name ?? "null";
                                Log.LogInfo($"[Data]   {p.Name}: {vt}");
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex) { Log.LogError($"[Data] {ex.Message}"); }
        }
    }
}
