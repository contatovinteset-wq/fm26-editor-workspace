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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.53.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.53.0 CARREGADO!");
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
                
                // F9 - Explora FM.UI.PersonReference
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Explorar PersonReference");
                    ExplorePersonReferences();
                }
                
                // F10 - Explora List<TypedValue> aninhados em detalhe
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Explorar List<TypedValue> detalhado");
                    ExploreNestedListsDetailed();
                }
                
                // Ctrl+P - Exporta jogadores em formato tabela
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Exportar jogadores");
                    ExportPlayers();
                }
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
        
        private static string CallAsString(object typedValue)
        {
            try
            {
                var method = typedValue.GetType().GetMethod("AsString");
                return method?.Invoke(typedValue, null)?.ToString() ?? "";
            }
            catch { return ""; }
        }
        
        private static object CallGet(object typedValue)
        {
            try
            {
                var method = typedValue.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "Get" && !m.IsGenericMethod && m.GetParameters().Length == 0);
                return method?.Invoke(typedValue, null);
            }
            catch { return null; }
        }
        
        private static object TryUnbox(object il2cppObject, Type targetType)
        {
            try
            {
                var unboxMethod = il2cppObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "Unbox" && m.IsGenericMethod);
                
                if (unboxMethod != null)
                {
                    var genericMethod = unboxMethod.MakeGenericMethod(targetType);
                    return genericMethod.Invoke(il2cppObject, null);
                }
            }
            catch { }
            
            return null;
        }
        
        private static void ExplorePersonReferences()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) { Log.LogWarning("[Person] m_data null"); return; }
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) return;
                
                int total = (int)countProp.GetValue(mData);
                int personCount = 0;
                
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var mValueProp = item.GetType().GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (mValueProp == null) continue;
                        
                        var mValue = mValueProp.GetValue(item);
                        if (mValue == null) continue;
                        
                        var asString = CallAsString(mValue);
                        if (asString != "FM.UI.PersonReference") continue;
                        
                        personCount++;
                        
                        if (personCount <= 5) // Explorar os primeiros 5
                        {
                            Log.LogInfo($"[Person] === PersonReference #{personCount} (índice {i}) ===");
                            
                            // Get() para pegar o objeto
                            var obj = CallGet(mValue);
                            if (obj != null)
                            {
                                Log.LogInfo($"[Person] Get() tipo: {obj.GetType().FullName}");
                                
                                // Propriedades do objeto
                                var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                Log.LogInfo($"[Person] {props.Length} propriedades");
                                
                                foreach (var p in props.Take(15))
                                {
                                    try
                                    {
                                        var v = p.GetValue(obj);
                                        var vs = v?.ToString() ?? "null";
                                        if (vs.Length > 50) vs = vs.Substring(0, 50) + "...";
                                        Log.LogInfo($"[Person]   {p.Name}: {vs}");
                                    }
                                    catch (Exception ex) { Log.LogInfo($"[Person]   {p.Name}: ERRO - {ex.Message}"); }
                                }
                                
                                // Campos
                                var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                                Log.LogInfo($"[Person] {fields.Length} campos públicos");
                                
                                foreach (var f in fields.Take(10))
                                {
                                    try
                                    {
                                        var v = f.GetValue(obj);
                                        var vs = v?.ToString() ?? "null";
                                        if (vs.Length > 50) vs = vs.Substring(0, 50) + "...";
                                        Log.LogInfo($"[Person]   {f.Name}: {vs}");
                                    }
                                    catch { }
                                }
                            }
                            else
                            {
                                Log.LogWarning("[Person] Get() retornou null");
                            }
                            
                            // DataKey associado
                            try
                            {
                                var keyProp = item.GetType().GetProperty("key", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                                if (keyProp != null)
                                {
                                    var key = keyProp.GetValue(item);
                                    if (key != null)
                                    {
                                        Log.LogInfo($"[Person] DataKey tipo: {key.GetType().Name}");
                                        var keyProps = key.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                        foreach (var kp in keyProps.Take(5))
                                        {
                                            try
                                            {
                                                var kv = kp.GetValue(key);
                                                Log.LogInfo($"[Person]   Key.{kp.Name}: {kv}");
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Person] Total: {personCount} PersonReferences");
            }
            catch (Exception ex) { Log.LogError($"[Person] {ex.Message}"); }
        }
        
        private static void ExploreNestedListsDetailed()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) return;
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) return;
                
                int total = (int)countProp.GetValue(mData);
                int listCount = 0;
                
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var mValueProp = item.GetType().GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (mValueProp == null) continue;
                        
                        var mValue = mValueProp.GetValue(item);
                        if (mValue == null) continue;
                        
                        var asString = CallAsString(mValue);
                        if (!asString.Contains("List`1[SI.Core.TypedValue]")) continue;
                        
                        listCount++;
                        
                        if (listCount <= 3) // Explorar as primeiras 3 listas
                        {
                            Log.LogInfo($"[List] === Lista #{listCount} (índice {i}) ===");
                            
                            var obj = CallGet(mValue);
                            if (obj == null) { Log.LogWarning("[List] Get() null"); continue; }
                            
                            // Tentar acessar como lista
                            var objType = obj.GetType();
                            var countPropList = objType.GetProperty("Count");
                            var indexerList = objType.GetProperty("Item");
                            
                            if (countPropList != null && indexerList != null)
                            {
                                int count = (int)countPropList.GetValue(obj);
                                Log.LogInfo($"[List] Tamanho: {count} itens");
                                
                                // Explorar todos os itens da lista
                                for (int j = 0; j < Math.Min(count, 20); j++)
                                {
                                    try
                                    {
                                        var inner = indexerList.GetValue(obj, new object[] { j });
                                        if (inner == null) { Log.LogInfo($"[List]   [{j}]: null"); continue; }
                                        
                                        var innerAsString = CallAsString(inner);
                                        Log.LogInfo($"[List]   [{j}]: {innerAsString}");
                                        
                                        // Se for PersonReference, explorar mais
                                        if (innerAsString == "FM.UI.PersonReference" || innerAsString.StartsWith("FM.UI."))
                                        {
                                            var innerObj = CallGet(inner);
                                            if (innerObj != null)
                                            {
                                                var innerProps = innerObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                                foreach (var ip in innerProps.Take(3))
                                                {
                                                    try
                                                    {
                                                        var ipv = ip.GetValue(innerObj);
                                                        Log.LogInfo($"[List]       {ip.Name}: {ipv}");
                                                    }
                                                    catch { }
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                            else
                            {
                                Log.LogWarning("[List] Sem Count/Indexer");
                            }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[List] Total: {listCount} List<TypedValue>");
            }
            catch (Exception ex) { Log.LogError($"[List] {ex.Message}"); }
        }
        
        private static void ExportPlayers()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) return;
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) return;
                
                int total = (int)countProp.GetValue(mData);
                
                var players = new List<Dictionary<string, string>>();
                
                // Buscar List<TypedValue> que podem conter jogadores
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var mValueProp = item.GetType().GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (mValueProp == null) continue;
                        
                        var mValue = mValueProp.GetValue(item);
                        if (mValue == null) continue;
                        
                        var asString = CallAsString(mValue);
                        
                        // Se for List<TypedValue>, explorar
                        if (asString.Contains("List`1[SI.Core.TypedValue]"))
                        {
                            var obj = CallGet(mValue);
                            if (obj == null) continue;
                            
                            var objType = obj.GetType();
                            var countPropList = objType.GetProperty("Count");
                            var indexerList = objType.GetProperty("Item");
                            
                            if (countPropList == null || indexerList == null) continue;
                            
                            int count = (int)countPropList.GetValue(obj);
                            
                            // Se a lista tem entre 5 e 50 itens, pode ser uma linha de jogador
                            if (count >= 5 && count <= 50)
                            {
                                var row = new Dictionary<string, string>();
                                
                                for (int j = 0; j < count; j++)
                                {
                                    try
                                    {
                                        var inner = indexerList.GetValue(obj, new object[] { j });
                                        if (inner == null) continue;
                                        
                                        var innerAsString = CallAsString(inner);
                                        row[$"col_{j}"] = innerAsString;
                                    }
                                    catch { }
                                }
                                
                                if (row.Count > 0) players.Add(row);
                            }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Export] {players.Count} linhas de dados encontradas");
                
                if (players.Count == 0)
                {
                    Log.LogWarning("[Export] Nenhum dado estruturado encontrado");
                    return;
                }
                
                // Encontrar todas as colunas
                var allCols = new HashSet<string>();
                foreach (var row in players)
                {
                    foreach (var key in row.Keys) allCols.Add(key);
                }
                
                var sortedCols = allCols.OrderBy(c => int.Parse(c.Replace("col_", ""))).ToList();
                
                // Exportar CSV
                var csv = new System.Text.StringBuilder();
                csv.AppendLine(string.Join(";", sortedCols));
                
                foreach (var row in players)
                {
                    var values = sortedCols.Select(c => 
                    {
                        if (row.TryGetValue(c, out var v))
                        {
                            return v.Replace(";", ",").Replace("\n", " ").Replace("\r", "");
                        }
                        return "";
                    });
                    csv.AppendLine(string.Join(";", values));
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Players_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                
                Log.LogInfo($"[Export] ✅ {players.Count} linhas exportadas");
                Log.LogInfo($"[Export] Arquivo: {path}");
            }
            catch (Exception ex) { Log.LogError($"[Export] {ex.Message}"); }
        }
    }
}
