using System;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26TableDump
{
    [BepInPlugin("com.koda.fm26.tabledump", "FM26 Table Dump", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("[TableDump] F7 = Dump da sub-arvore de search-table-remapper");
            AddComponent<DumpBehaviour>();
        }
    }

    public class DumpBehaviour : MonoBehaviour
    {
        public DumpBehaviour(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.f7Key.wasPressedThisFrame) DoDump();
        }

        private static VisualElement FindByName(VisualElement el, string name)
        {
            if (el == null) return null;
            if (el.name == name) return el;
            for (int i = 0; i < el.childCount; i++)
            {
                var r = FindByName(el.ElementAt(i), name);
                if (r != null) return r;
            }
            return null;
        }

        private static void DumpTree(VisualElement el, StringBuilder sb, int depth, int maxDepth)
        {
            if (el == null || depth > maxDepth) return;
            string indent = new string(' ', depth * 2);
            string type = el.GetType().Name;
            string name = el.name ?? string.Empty;
            string classes = string.Empty;
            try
            {
                var sb2 = new StringBuilder();
                for (int c = 0; c < el.classList.Count; c++)
                {
                    if (c > 0) sb2.Append(',');
                    sb2.Append(el.classList[c]);
                }
                classes = sb2.ToString();
            }
            catch { }

            // Se for Label, captura o texto
            string extra = string.Empty;
            if (el is Label lbl)
            {
                try { extra = $" TEXT=\"{lbl.text}\""; } catch { }
            }
            if (el is Toggle tgl)
            {
                try { extra = $" CHECKED={tgl.value}"; } catch { }
            }

            sb.AppendLine($"{indent}[{depth}] {type} name='{name}' classes='{classes}'{extra} children={el.childCount}");

            for (int i = 0; i < el.childCount; i++)
                DumpTree(el.ElementAt(i), sb, depth + 1, maxDepth);
        }

        private void DoDump()
        {
            try
            {
                var all = FindObjectsOfType<UIDocument>();
                VisualElement tableContainer = null;
                VisualElement headerSection = null;

                foreach (var doc in all)
                {
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    if (root.name != "PanelManager-container") continue;

                    Plugin.Log.LogInfo("[TableDump] PanelManager encontrado");

                    var tables = FindByName(root, "tables");
                    if (tables == null)
                    {
                        Plugin.Log.LogWarning("[TableDump] 'tables' nao encontrado");
                        return;
                    }

                    tableContainer = FindByName(tables, "search-table-remapper");
                    if (tableContainer == null)
                    {
                        Plugin.Log.LogWarning("[TableDump] 'search-table-remapper' nao encontrado");
                        return;
                    }

                    headerSection = FindByName(root, "PersonSearchTableTopSection");
                    break;
                }

                if (tableContainer == null)
                {
                    Plugin.Log.LogError("[TableDump] Nao encontrou search-table-remapper. Abra a Player Database primeiro.");
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("=== FM26 Table Dump ===");
                sb.AppendLine($"Data: {DateTime.Now}");
                sb.AppendLine($"search-table-remapper: {tableContainer.childCount} filhos diretos");
                sb.AppendLine("");

                // Dump do header
                sb.AppendLine("=== PersonSearchTableTopSection ===");
                if (headerSection != null)
                    DumpTree(headerSection, sb, 0, 4);
                else
                    sb.AppendLine("(nao encontrado)");
                sb.AppendLine("");

                // Dump completo do search-table-remapper (até 6 níveis)
                sb.AppendLine("=== search-table-remapper (completo, 6 niveis) ===");
                DumpTree(tableContainer, sb, 0, 6);
                sb.AppendLine("");

                // Dump detalhado dos primeiros 3 filhos diretos (até 10 níveis)
                sb.AppendLine("=== Primeiros 3 filhos diretos (10 niveis) ===");
                int max3 = Math.Min(3, tableContainer.childCount);
                for (int i = 0; i < max3; i++)
                {
                    sb.AppendLine($"--- Filho [{i}] ---");
                    DumpTree(tableContainer.ElementAt(i), sb, 0, 10);
                }

                // Salvar
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Sports Interactive", "Football Manager 2026");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                string file = Path.Combine(path, $"table_dump_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
                Plugin.Log.LogInfo($"[TableDump] Salvo em: {file}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[TableDump] ERRO: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
