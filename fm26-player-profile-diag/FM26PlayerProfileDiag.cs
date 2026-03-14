using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26PlayerProfileDiag
{
    [BepInPlugin("com.vintesetfm.player_profile_diag", "FM26 Player Profile Diagnostic", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Player Profile Diagnostic v1.0.0");
            Log.LogInfo("========================================");
            Log.LogInfo("F10 = Diagnóstico COMPLETO da tela atual");
            Log.LogInfo("F11 = Diagnóstico de BINDINGS (dados)");
            Log.LogInfo("F12 = Diagnóstico de TOOLTIPS");
            Log.LogInfo("Abra o perfil de um jogador e pressione F10");
            AddComponent<ProfileDiagBehaviour>();
        }
    }

    public class ProfileDiagBehaviour : MonoBehaviour
    {
        public ProfileDiagBehaviour(IntPtr ptr) : base(ptr) { }

        private List<UIDocument> _docs = new List<UIDocument>();

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.f10Key.wasPressedThisFrame) DiagFull();
            if (Keyboard.current.f11Key.wasPressedThisFrame) DiagBindings();
            if (Keyboard.current.f12Key.wasPressedThisFrame) DiagTooltips();
        }

        // ── F10: Diagnóstico Completo ─────────────────────────────────────
        [HideFromIl2Cpp]
        private void DiagFull()
        {
            Plugin.Log.LogInfo("[PPD] === DIAGNÓSTICO COMPLETO ===");
            
            var root = GetRoot();
            if (root == null) { Plugin.Log.LogWarning("[PPD] Sem PanelManager"); return; }

            var sb = new StringBuilder();
            sb.AppendLine($"# FM26 Player Profile Diag - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# Root: {root.name}");
            sb.AppendLine();

            // Dump completo da árvore
            DumpTree(root, sb, 0, 50);
            
            // Procurar por elementos específicos
            sb.AppendLine();
            sb.AppendLine("=== ELEMENTOS ENCONTRADOS ===");
            
            FindAndDump(root, "PlayerProfile", sb);
            FindAndDump(root, "player", sb);
            FindAndDump(root, "ability", sb);
            FindAndDump(root, "potential", sb);
            FindAndDump(root, "rating", sb);
            FindAndDump(root, "attribute", sb);
            
            SaveFile(sb.ToString(), "profile_full_diag", ".txt");
            Plugin.Log.LogInfo($"[PPD] Diagnóstico completo salvo");
        }

        // ── F11: Diagnóstico de Bindings ───────────────────────────────────
        [HideFromIl2Cpp]
        private void DiagBindings()
        {
            Plugin.Log.LogInfo("[PPD] === DIAGNÓSTICO DE BINDINGS ===");
            
            var root = GetRoot();
            if (root == null) { Plugin.Log.LogWarning("[PPD] Sem PanelManager"); return; }

            var sb = new StringBuilder();
            sb.AppendLine($"# FM26 Bindings Diag - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // Procurar todos os elementos com binding
            var boundElements = new List<VisualElement>();
            FindBoundElements(root, boundElements, 0);
            
            sb.AppendLine($"# Elementos com binding: {boundElements.Count}");
            sb.AppendLine();
            
            foreach (var el in boundElements)
            {
                DumpElementBindings(el, sb);
            }
            
            SaveFile(sb.ToString(), "profile_bindings_diag", ".txt");
            Plugin.Log.LogInfo($"[PPD] Bindings salvos: {boundElements.Count} elementos");
        }

        // ── F12: Diagnóstico de Tooltips ───────────────────────────────────
        [HideFromIl2Cpp]
        private void DiagTooltips()
        {
            Plugin.Log.LogInfo("[PPD] === DIAGNÓSTICO DE TOOLTIPS ===");
            
            var root = GetRoot();
            if (root == null) { Plugin.Log.LogWarning("[PPD] Sem PanelManager"); return; }

            var sb = new StringBuilder();
            sb.AppendLine($"# FM26 Tooltips Diag - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // Procurar todos os elementos com tooltip
            var tooltipElements = new List<VisualElement>();
            FindTooltipElements(root, tooltipElements, 0);
            
            sb.AppendLine($"# Elementos com tooltip: {tooltipElements.Count}");
            sb.AppendLine();
            
            foreach (var el in tooltipElements)
            {
                try
                {
                    string tooltip = el.tooltip;
                    string name = el.name;
                    string typeName = el.GetType().Name;
                    
                    // Tentar pegar texto do elemento
                    string text = TryGetText(el);
                    
                    sb.AppendLine($"[{typeName}] name=\"{name}\"");
                    sb.AppendLine($"  tooltip: \"{tooltip}\"");
                    if (!string.IsNullOrEmpty(text) && text != tooltip)
                        sb.AppendLine($"  text: \"{text}\"");
                    sb.AppendLine();
                }
                catch { }
            }
            
            SaveFile(sb.ToString(), "profile_tooltips_diag", ".txt");
            Plugin.Log.LogInfo($"[PPD] Tooltips salvos: {tooltipElements.Count} elementos");
        }

        // ── Helpers ─────────────────────────────────────────────────────────
        [HideFromIl2Cpp]
        private VisualElement GetRoot()
        {
            _docs.Clear();
            foreach (var doc in FindObjectsOfType<UIDocument>())
                if (doc.rootVisualElement?.name == "PanelManager-container")
                    _docs.Add(doc);
            return _docs.Count > 0 ? _docs[0].rootVisualElement : null;
        }

        [HideFromIl2Cpp]
        private void DumpTree(VisualElement el, StringBuilder sb, int depth, int maxDepth)
        {
            if (el == null || depth > maxDepth) return;
            
            try
            {
                string indent = new string(' ', depth * 2);
                string typeName = el.GetType().Name;
                string name = el.name ?? "";
                
                // Classes
                var classes = new List<string>();
                try { for (int c = 0; c < el.classList.Count; c++) classes.Add(el.classList[c]); } catch { }
                string classStr = classes.Count > 0 ? $" [{string.Join(", ", classes)}]" : "";
                
                // Texto
                string text = TryGetText(el);
                string textStr = !string.IsNullOrEmpty(text) ? $" text=\"{Trunc(text, 40)}\"" : "";
                
                // Tooltip
                string tooltip = null;
                try { tooltip = el.tooltip; } catch { }
                string tooltipStr = !string.IsNullOrEmpty(tooltip) ? $" tooltip=\"{Trunc(tooltip, 40)}\"" : "";
                
                // Binding path
                string bindingPath = TryGetBindingPath(el);
                string bindingStr = !string.IsNullOrEmpty(bindingPath) ? $" binding=\"{bindingPath}\"" : "";
                
                sb.AppendLine($"{indent}{typeName} name=\"{name}\"{classStr}{textStr}{tooltipStr}{bindingStr} ch={el.childCount}");
                
                for (int i = 0; i < el.childCount; i++)
                    DumpTree(el[i], sb, depth + 1, maxDepth);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"{new string(' ', depth * 2)}ERROR: {ex.Message}");
            }
        }

        [HideFromIl2Cpp]
        private void FindAndDump(VisualElement root, string searchTerm, StringBuilder sb)
        {
            var found = new List<VisualElement>();
            FindByNameContaining(root, searchTerm, found, 0);
            
            if (found.Count > 0)
            {
                sb.AppendLine($"\n=== '{searchTerm}' ({found.Count} encontrados) ===");
                foreach (var el in found)
                {
                    DumpElementDeep(el, sb, 0, 5);
                }
            }
        }

        [HideFromIl2Cpp]
        private void FindByNameContaining(VisualElement el, string term, List<VisualElement> results, int depth)
        {
            if (el == null || depth > 60) return;
            
            try
            {
                if (!string.IsNullOrEmpty(el.name) && el.name.ToLower().Contains(term.ToLower()))
                    results.Add(el);
                
                // Também procurar nas classes
                for (int c = 0; c < el.classList.Count; c++)
                {
                    if (el.classList[c].ToLower().Contains(term.ToLower()))
                    {
                        results.Add(el);
                        break;
                    }
                }
            }
            catch { }
            
            for (int i = 0; i < el.childCount; i++)
                FindByNameContaining(el[i], term, results, depth + 1);
        }

        [HideFromIl2Cpp]
        private void FindBoundElements(VisualElement el, List<VisualElement> results, int depth)
        {
            if (el == null || depth > 60) return;
            
            try
            {
                // Verificar se tem binding
                var bindingPath = TryGetBindingPath(el);
                if (!string.IsNullOrEmpty(bindingPath))
                    results.Add(el);
            }
            catch { }
            
            for (int i = 0; i < el.childCount; i++)
                FindBoundElements(el[i], results, depth + 1);
        }

        [HideFromIl2Cpp]
        private void FindTooltipElements(VisualElement el, List<VisualElement> results, int depth)
        {
            if (el == null || depth > 60) return;
            
            try
            {
                var tooltip = el.tooltip;
                if (!string.IsNullOrEmpty(tooltip))
                    results.Add(el);
            }
            catch { }
            
            for (int i = 0; i < el.childCount; i++)
                FindTooltipElements(el[i], results, depth + 1);
        }

        [HideFromIl2Cpp]
        private void DumpElementDeep(VisualElement el, StringBuilder sb, int depth, int maxDepth)
        {
            if (el == null || depth > maxDepth) return;
            
            string indent = new string(' ', depth * 2);
            string typeName = el.GetType().Name;
            string name = el.name ?? "";
            
            var classes = new List<string>();
            try { for (int c = 0; c < el.classList.Count; c++) classes.Add(el.classList[c]); } catch { }
            string classStr = classes.Count > 0 ? $" [{string.Join(", ", classes)}]" : "";
            
            string text = TryGetText(el);
            string textStr = !string.IsNullOrEmpty(text) ? $" text=\"{text}\"" : "";
            
            string tooltip = null;
            try { tooltip = el.tooltip; } catch { }
            string tooltipStr = !string.IsNullOrEmpty(tooltip) ? $" tooltip=\"{Trunc(tooltip, 60)}\"" : "";
            
            string bindingPath = TryGetBindingPath(el);
            string bindingStr = !string.IsNullOrEmpty(bindingPath) ? $" binding=\"{bindingPath}\"" : "";
            
            sb.AppendLine($"{indent}{typeName} name=\"{name}\"{classStr}{textStr}{tooltipStr}{bindingStr}");
            
            for (int i = 0; i < el.childCount; i++)
                DumpElementDeep(el[i], sb, depth + 1, maxDepth);
        }

        [HideFromIl2Cpp]
        private void DumpElementBindings(VisualElement el, StringBuilder sb)
        {
            try
            {
                sb.AppendLine($"=== {el.GetType().Name} name=\"{el.name}\" ===");
                
                // BindingPath
                var bindingPath = TryGetBindingPath(el);
                if (!string.IsNullOrEmpty(bindingPath))
                    sb.AppendLine($"  bindingPath: {bindingPath}");
                
                // Tentar pegar todas as propriedades de binding via reflexão
                var type = el.GetType();
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                foreach (var field in fields)
                {
                    string fieldName = field.Name.ToLower();
                    if (fieldName.Contains("binding") || fieldName.Contains("path") || fieldName.Contains("data"))
                    {
                        try
                        {
                            var value = field.GetValue(el);
                            if (value != null)
                            {
                                string valueStr = value.ToString();
                                if (!string.IsNullOrEmpty(valueStr) && valueStr != "null")
                                    sb.AppendLine($"  {field.Name}: {valueStr}");
                            }
                        }
                        catch { }
                    }
                }
                
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  ERROR: {ex.Message}");
            }
        }

        [HideFromIl2Cpp]
        private string TryGetText(VisualElement el)
        {
            if (el == null) return null;
            
            try { var te = el.TryCast<TextElement>(); if (te != null && !string.IsNullOrWhiteSpace(te.text)) return te.text.Trim(); } catch { }
            try { var lb = el.TryCast<Label>(); if (lb != null && !string.IsNullOrWhiteSpace(lb.text)) return lb.text.Trim(); } catch { }
            
            return null;
        }

        [HideFromIl2Cpp]
        private string TryGetBindingPath(VisualElement el)
        {
            if (el == null) return null;
            
            try
            {
                // IBindableElement tem bindingPath
                var type = el.GetType();
                
                // bindingPath property
                var prop = type.GetProperty("bindingPath", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    var value = prop.GetValue(el);
                    if (value != null)
                        return value.ToString();
                }
                
                // bindingPath field
                var field = type.GetField("bindingPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    var value = field.GetValue(el);
                    if (value != null)
                        return value.ToString();
                }
                
                // m_bindingPath
                var mField = type.GetField("m_bindingPath", BindingFlags.NonPublic | BindingFlags.Instance);
                if (mField != null)
                {
                    var value = mField.GetValue(el);
                    if (value != null)
                        return value.ToString();
                }
            }
            catch { }
            
            return null;
        }

        [HideFromIl2Cpp]
        private void SaveFile(string content, string prefix, string ext = ".txt")
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Sports Interactive", "Football Manager 2026");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
            File.WriteAllText(path, content, Encoding.UTF8);
            Plugin.Log.LogInfo($"[PPD] Salvo: {path}");
        }

        private static string Trunc(string s, int max)
            => string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}
