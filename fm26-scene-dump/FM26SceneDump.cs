using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26SceneDump
{
    [BepInPlugin("com.koda.fm26.scenedump", "FM26 Scene Dump", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("[FM26Dump] Plugin carregado!");
            Log.LogInfo("[FM26Dump] F6 = Dump da cena para scene_dump.txt");
            
            AddComponent<SceneDumpBehaviour>();
        }
    }
    
    public class SceneDumpBehaviour : MonoBehaviour
    {
        private bool _dumpRequested = false;
        
        public void Update()
        {
            try
            {
                if (Keyboard.current == null) return;
                
                if (Keyboard.current.f6Key.wasPressedThisFrame && !_dumpRequested)
                {
                    _dumpRequested = true;
                    Plugin.Log.LogInfo("[FM26Dump] F6 pressionado - iniciando dump...");
                    DumpScene();
                    _dumpRequested = false;
                }
            }
            catch { }
        }
        
        private void DumpScene()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== FM26 Scene Dump ===");
            sb.AppendLine($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            
            // 1. Listar todos os GameObjects ativos
            Plugin.Log.LogInfo("[FM26Dump] Listando GameObjects ativos...");
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            sb.AppendLine($"=== GameObjects Ativos ({allObjects.Length}) ===");
            
            foreach (var obj in allObjects)
            {
                if (!obj.activeInHierarchy) continue;
                
                sb.AppendLine($"GameObject: {obj.name}");
                var components = obj.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    sb.AppendLine($"  Component: {comp.GetType().Name}");
                }
            }
            
            sb.AppendLine();
            
            // 2. Listar UIDocuments
            Plugin.Log.LogInfo("[FM26Dump] Buscando UIDocuments...");
            var uiDocs = UnityEngine.Object.FindObjectsOfType<UIDocument>();
            sb.AppendLine($"=== UIDocuments ({uiDocs.Length}) ===");
            
            foreach (var doc in uiDocs)
            {
                Plugin.Log.LogInfo($"[FM26Dump] UIDocument: {doc.name}");
                sb.AppendLine($"\n--- UIDocument: {doc.name} ---");
                
                var root = doc.rootVisualElement;
                if (root == null)
                {
                    sb.AppendLine("  (rootVisualElement null)");
                    continue;
                }
                
                DumpVisualElement(root, sb, 0);
            }
            
            // Salvar arquivo
            string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, "..", "..", "scene_dump.txt");
            try
            {
                System.IO.File.WriteAllText(path, sb.ToString());
                Plugin.Log.LogInfo($"[FM26Dump] ✅ Dump salvo em: {path}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[FM26Dump] Erro ao salvar: {ex.Message}");
            }
        }
        
        private void DumpVisualElement(VisualElement element, StringBuilder sb, int depth)
        {
            if (element == null) return;
            if (depth > 30) return; // Limite de profundidade
            
            string indent = new string(' ', depth * 2);
            var type = element.GetType().Name;
            var name = element.name ?? "(sem nome)";
            
            // Class list
            var classList = GetClassList(element);
            
            sb.AppendLine($"{indent}{type} '{name}' [{classList}]");
            Plugin.Log.LogInfo($"[FM26Dump] {indent}{type} '{name}'");
            
            // Se for ListView, mostrar info adicional
            if (type == "ListView" || type.Contains("List"))
            {
                DumpListView(element, sb, depth);
            }
            
            // Recursão nos filhos
            int childCount = element.childCount;
            for (int i = 0; i < childCount && i < 100; i++)
            {
                try
                {
                    var child = element.ElementAt(i);
                    DumpVisualElement(child, sb, depth + 1);
                }
                catch { }
            }
        }
        
        private void DumpListView(VisualElement element, StringBuilder sb, int depth)
        {
            string indent = new string(' ', (depth + 1) * 2);
            
            try
            {
                var type = element.GetType();
                
                // itemsSource property
                var itemsSourceProp = type.GetProperty("itemsSource");
                if (itemsSourceProp == null)
                {
                    sb.AppendLine($"{indent}(itemsSource não encontrado)");
                    return;
                }
                
                var itemsSource = itemsSourceProp.GetValue(element);
                if (itemsSource == null)
                {
                    sb.AppendLine($"{indent}(itemsSource null)");
                    return;
                }
                
                var itemsType = itemsSource.GetType();
                
                // Count property
                var countProp = itemsType.GetProperty("Count");
                int count = 0;
                if (countProp != null)
                {
                    count = (int)countProp.GetValue(itemsSource);
                    sb.AppendLine($"{indent}itemsSource.Count: {count}");
                }
                
                // Indexer
                var indexerProp = itemsType.GetProperty("Item");
                if (indexerProp == null)
                {
                    sb.AppendLine($"{indent}(sem indexer)");
                    return;
                }
                
                // Tipo do primeiro item
                if (count > 0)
                {
                    try
                    {
                        var firstItem = indexerProp.GetValue(itemsSource, new object[] { 0 });
                        if (firstItem != null)
                        {
                            sb.AppendLine($"{indent}Primeiro item tipo: {firstItem.GetType().FullName}");
                        }
                    }
                    catch { }
                }
                
                // Listar 3 primeiros itens
                int itemsToDump = Math.Min(count, 3);
                for (int i = 0; i < itemsToDump; i++)
                {
                    try
                    {
                        var item = indexerProp.GetValue(itemsSource, new object[] { i });
                        if (item == null) continue;
                        
                        sb.AppendLine($"{indent}Item[{i}]:");
                        DumpObjectFields(item, sb, depth + 2);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"{indent}Erro ListView: {ex.Message}");
            }
        }
        
        private void DumpObjectFields(object obj, StringBuilder sb, int depth)
        {
            if (obj == null) return;
            
            string indent = new string(' ', depth * 2);
            var type = obj.GetType();
            
            // Campos a ignorar
            string[] ignorePatterns = { "m_", "k__BackingField", "Il2Cpp", "Pointer", "GCHandle", "NativeObject" };
            
            // Campos públicos
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var f in fields)
            {
                try
                {
                    if (ignorePatterns.Any(p => f.Name.Contains(p))) continue;
                    
                    var val = f.GetValue(obj);
                    var valStr = FormatValue(val);
                    sb.AppendLine($"{indent}{f.Name}: {valStr}");
                }
                catch { }
            }
            
            // Propriedades públicas
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                try
                {
                    if (ignorePatterns.Any(pa => p.Name.Contains(pa))) continue;
                    if (p.GetIndexParameters().Length > 0) continue; // Ignorar indexers
                    
                    var val = p.GetValue(obj);
                    var valStr = FormatValue(val);
                    sb.AppendLine($"{indent}{p.Name}: {valStr}");
                }
                catch { }
            }
        }
        
        private string FormatValue(object val)
        {
            if (val == null) return "null";
            
            var type = val.GetType();
            
            if (type.IsPrimitive || type == typeof(string))
            {
                var str = val.ToString();
                if (str.Length > 100) str = str.Substring(0, 100) + "...";
                return str;
            }
            
            if (val is IList list)
            {
                return $"List({list.Count} items)";
            }
            
            return type.Name;
        }
        
        private string GetClassList(VisualElement element)
        {
            try
            {
                var type = element.GetType();
                var classListProp = type.GetProperty("classList");
                if (classListProp == null) return "";
                
                var classList = classListProp.GetValue(element);
                if (classList == null) return "";
                
                // Tentar converter para string
                var toString = classList.GetType().GetMethod("ToString", Type.EmptyTypes);
                if (toString != null)
                {
                    var str = toString.Invoke(classList, null)?.ToString() ?? "";
                    if (str.Length > 50) str = str.Substring(0, 50) + "...";
                    return str;
                }
                
                return classList.GetType().Name;
            }
            catch
            {
                return "";
            }
        }
    }
}
