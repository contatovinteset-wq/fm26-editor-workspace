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

namespace FM26TacticsDump
{
    [BepInPlugin("com.vintesetfm.tactics_dump", "FM26 Tactics Dump", "1.5.0")]
    public class TacticsDumpPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("FM26 Tactics Dump v1.5.0");
            Log.LogInfo("F10 = Dump taticas  |  F11 = Diag profundo do GLEC");
            AddComponent<TacticsDumpBehaviour>();
        }
    }

    public class TacticsDumpBehaviour : MonoBehaviour
    {
        public TacticsDumpBehaviour(IntPtr ptr) : base(ptr) { }

        private List<UIDocument> _docs = new List<UIDocument>();
        private int _frame = 0;

        private void Update()
        {
            _frame++;
            if (Keyboard.current == null) return;
            if (Keyboard.current.f10Key.wasPressedThisFrame) DumpTactics();
            if (Keyboard.current.f11Key.wasPressedThisFrame) DiagDeep();
        }

        [HideFromIl2Cpp]
        private VisualElement GetRoot()
        {
            _docs.Clear();
            foreach (var doc in FindObjectsOfType<UIDocument>())
                if (doc.rootVisualElement?.name == "PanelManager-container")
                    _docs.Add(doc);
            return _docs.Count > 0 ? _docs[0].rootVisualElement : null;
        }

        // ── F11: Diag profundo dos GLECs de instrucao ─────────────────────
        [HideFromIl2Cpp]
        private void DiagDeep()
        {
            var root = GetRoot();
            if (root == null) return;

            var planner = FindByName(root, "TacticalPlannerTool");
            if (planner == null) { TacticsDumpPlugin.Log.LogWarning("[TD] Abra Taticas > Instrucoes a Equipa"); return; }

            var glecs = new List<VisualElement>();
            FindAllByName(planner, "GridLayoutElementContent", glecs, 0);

            var sb = new StringBuilder();
            sb.AppendLine($"# TacticsDump Diag v1.5.0 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# GLECs em TacticalPlannerTool: {glecs.Count}");
            sb.AppendLine();

            for (int g = 0; g < glecs.Count; g++)
            {
                var glec = glecs[g];
                bool isInstruction = IsInstructionGlec(glec);
                sb.AppendLine($"=== GLEC[{g}] ch={glec.childCount} isInstruction={isInstruction} ===");
                // Vai ate profundidade 40 a partir do GLEC
                DumpDeep(glec, sb, 0, 40);
                sb.AppendLine();
            }

            SaveFile(sb.ToString(), "tactics_diag", ".txt");
            TacticsDumpPlugin.Log.LogInfo($"[TD] Diag: {glecs.Count} GLECs. Abrindo o popup de detalhe de uma instrucao aumenta info no GLEC[detail].");
        }

        // ── F10: Dump principal ──────────────────────────────────────────
        [HideFromIl2Cpp]
        private void DumpTactics()
        {
            TacticsDumpPlugin.Log.LogInfo("[TD] F10 iniciando...");
            var root = GetRoot();
            if (root == null) { TacticsDumpPlugin.Log.LogWarning("[TD] Sem PanelManager"); return; }

            var planner = FindByName(root, "TacticalPlannerTool");
            if (planner == null)
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] TacticalPlannerTool nao encontrado. Abra Taticas > Instrucoes a Equipa.");
                return;
            }

            var glecs = new List<VisualElement>();
            FindAllByName(planner, "GridLayoutElementContent", glecs, 0);
            TacticsDumpPlugin.Log.LogInfo($"[TD] {glecs.Count} GLECs encontrados");

            var tiles  = new List<TileData>();
            var shouts = new List<string>();

            foreach (var glec in glecs)
            {
                if (!IsInstructionGlec(glec)) continue;

                TacticsDumpPlugin.Log.LogInfo($"[TD] Instrucao GLEC ch={glec.childCount}");

                for (int i = 0; i < glec.childCount; i++)
                {
                    var siTile = glec[i];
                    if (!HasClass(siTile, "si-tile")) continue;

                    // Nome do tile = nome do primeiro filho (ex: "PassingDirectness")
                    string tileId = siTile.childCount > 0 ? siTile[0].name : $"Tile{i}";
                    if (string.IsNullOrEmpty(tileId)) tileId = $"Tile{i}";

                    // Coleta textos profundos (ate 40 niveis)
                    var texts = new List<string>();
                    CollectTextsDeep(siTile, texts, 0, 40);

                    TacticsDumpPlugin.Log.LogInfo($"[TD]   [{i}] id='{tileId}' texts={texts.Count}: [{string.Join(" | ", texts.GetRange(0, Math.Min(texts.Count, 5)))}]");

                    var tile = new TileData { Name = tileId };
                    for (int t = 0; t < texts.Count; t++)
                        tile.Options.Add(new OptionData { Value = texts[t], IsSelected = false });

                    // Tenta marcar o selecionado via classe CSS
                    var selectedEls = new List<VisualElement>();
                    FindByClassContains(siTile, "active",    selectedEls, 0);
                    FindByClassContains(siTile, "selected",  selectedEls, 0);
                    FindByClassContains(siTile, "--on",      selectedEls, 0);
                    FindByClassContains(siTile, "current",   selectedEls, 0);
                    if (selectedEls.Count > 0)
                    {
                        foreach (var opt in tile.Options) opt.IsSelected = false;
                        foreach (var sel in selectedEls)
                        {
                            string t2 = GetFirstText(sel);
                            if (!string.IsNullOrEmpty(t2) && t2 != tileId)
                            {
                                var m = tile.Options.Find(o => o.Value == t2);
                                if (m != null) m.IsSelected = true;
                                else tile.Options.Add(new OptionData { Value = t2, IsSelected = true });
                            }
                        }
                    }

                    tiles.Add(tile);
                }
            }

            // Shouts
            var speakEl = FindByName(planner, "SpeakToContainerExpect");
            if (speakEl != null) CollectTextsDeep(speakEl, shouts, 0, 40);

            TacticsDumpPlugin.Log.LogInfo($"[TD] Tiles={tiles.Count} | Shouts={shouts.Count}");

            if (tiles.Count == 0)
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] tiles=0. Possivel causa:");
                TacticsDumpPlugin.Log.LogWarning("[TD]   - Feche o popup de detalhe (X ou seta voltar)");
                TacticsDumpPlugin.Log.LogWarning("[TD]   - Abra a aba 'Com Posse de Bola' OU 'Sem Posse de Bola'");
                TacticsDumpPlugin.Log.LogWarning("[TD]   - Pressione F11 para diag profundo");
                DiagDeep();
            }

            SaveFile(BuildJson(tiles, shouts), "tactics_dump", ".json");
        }

        // ── Identifica GLEC de instrucao tatica ──────────────────────────
        // Um GLEC e de instrucao se algum si-tile contem 'TeamInstructionTileTemplate'
        [HideFromIl2Cpp]
        private bool IsInstructionGlec(VisualElement glec)
        {
            if (glec.childCount == 0) return false;
            for (int i = 0; i < Math.Min(glec.childCount, 3); i++)
            {
                var child = glec[i];
                if (HasClass(child, "si-tile"))
                {
                    if (FindByName(child, "TeamInstructionTileTemplate") != null) return true;
                    // Tambem aceita si-tile cujo filho se chama algo reconhecivel de instrucao
                    if (child.childCount > 0)
                    {
                        string n = child[0].name ?? "";
                        if (n == "PassingDirectness" || n == "Tempo" || n == "TimeWasting" ||
                            n == "AttackingTransition" || n == "TeamWidth" || n == "StoppageStrategy" ||
                            n == "CreativeFreedom" || n == "Pressing" || n == "PressureIntensity" ||
                            n == "Mentality" || n == "Finta" || n == "Paciencia" || n.Contains("Tile") ||
                            n == "TeamInstructionOptionListBlock" || n == "TacticsEditCardOptionDetails" ||
                            n.StartsWith("Portal")) return true;
                    }
                }
            }
            return false;
        }

        // ── Extracao de texto multi-estrategia ──────────────────────────
        // Estrategia 1: TryCast<Label>         → Unity Label nativo
        // Estrategia 2: TryCast<TextElement>   → base class de Label/Button
        // Estrategia 3: Reflection text prop   → sitext / custom elements FM26
        [HideFromIl2Cpp]
        private string TryGetText(VisualElement el)
        {
            // 1. Label
            try { var l = el.TryCast<Label>(); if (l?.text?.Length > 0) return l.text.Trim(); } catch { }
            // 2. TextElement (base de Label, Button, etc.)
            try { var te = el.TryCast<TextElement>(); if (te?.text?.Length > 0) return te.text.Trim(); } catch { }
            // 3. Reflection: funciona para SIText e outros custom types com prop 'text'
            try
            {
                var prop = el.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    string v = prop.GetValue(el) as string;
                    if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
                }
            }
            catch { }
            return null;
        }

        [HideFromIl2Cpp]
        private void CollectTextsDeep(VisualElement el, List<string> result, int depth, int maxDepth)
        {
            if (el == null || depth > maxDepth) return;
            string t = TryGetText(el);
            if (!string.IsNullOrEmpty(t) && !result.Contains(t))
                result.Add(t);
            for (int i = 0; i < el.childCount; i++)
                CollectTextsDeep(el[i], result, depth + 1, maxDepth);
        }

        [HideFromIl2Cpp]
        private string GetFirstText(VisualElement el)
        {
            string t = TryGetText(el);
            if (!string.IsNullOrEmpty(t)) return t;
            for (int i = 0; i < el.childCount; i++)
            {
                string r = GetFirstText(el[i]);
                if (!string.IsNullOrEmpty(r)) return r;
            }
            return null;
        }

        // ── Helpers ──────────────────────────────────────────────────────
        [HideFromIl2Cpp]
        private bool HasClass(VisualElement el, string cls)
        {
            try { for (int c = 0; c < el.classList.Count; c++) if (el.classList[c] == cls) return true; }
            catch { }
            return false;
        }

        [HideFromIl2Cpp]
        private void FindByClassContains(VisualElement root, string frag, List<VisualElement> res, int depth)
        {
            if (root == null || depth > 20) return;
            try { for (int c = 0; c < root.classList.Count; c++) if (root.classList[c].Contains(frag)) { res.Add(root); break; } }
            catch { }
            for (int i = 0; i < root.childCount; i++) FindByClassContains(root[i], frag, res, depth + 1);
        }

        [HideFromIl2Cpp]
        private VisualElement FindByName(VisualElement root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { var r = FindByName(root[i], name); if (r != null) return r; }
            return null;
        }

        [HideFromIl2Cpp]
        private void FindAllByName(VisualElement root, string name, List<VisualElement> res, int depth)
        {
            if (root == null || depth > 60) return;
            if (root.name == name) res.Add(root);
            for (int i = 0; i < root.childCount; i++) FindAllByName(root[i], name, res, depth + 1);
        }

        [HideFromIl2Cpp]
        private void DumpDeep(VisualElement el, StringBuilder sb, int depth, int maxDepth)
        {
            if (el == null || depth > maxDepth) return;
            var classes = new List<string>();
            try { for (int c = 0; c < el.classList.Count; c++) classes.Add(el.classList[c]); } catch { }
            string txt = TryGetText(el);
            string txtStr = !string.IsNullOrEmpty(txt) ? $" \"{Trunc(txt, 60)}\"" : "";
            string cls = classes.Count > 0 ? $" [{string.Join(", ", classes)}]" : "";
            sb.AppendLine($"{new string(' ', depth * 2)}{el.GetType().Name} name={el.name}{cls}{txtStr}  ch={el.childCount}");
            for (int i = 0; i < el.childCount; i++) DumpDeep(el[i], sb, depth + 1, maxDepth);
        }

        // ── JSON / Save ───────────────────────────────────────────────────
        [HideFromIl2Cpp]
        private string BuildJson(List<TileData> tiles, List<string> shouts)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-ddTHH:mm:ss}\",");
            sb.AppendLine("  \"tiles\": [");
            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i];
                sb.Append($"    {{ \"name\": {JS(t.Name)}, \"options\": [");
                for (int j = 0; j < t.Options.Count; j++)
                {
                    sb.Append($"{{\"value\": {JS(t.Options[j].Value)}, \"selected\": {t.Options[j].IsSelected.ToString().ToLower()}}}");
                    if (j < t.Options.Count - 1) sb.Append(", ");
                }
                sb.Append("] }");
                sb.AppendLine(i < tiles.Count - 1 ? "," : "");
            }
            sb.AppendLine("  ],");
            sb.AppendLine("  \"shouts\": [");
            for (int i = 0; i < shouts.Count; i++)
            {
                sb.Append($"    {JS(shouts[i])}");
                sb.AppendLine(i < shouts.Count - 1 ? "," : "");
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        [HideFromIl2Cpp]
        private void SaveFile(string content, string prefix, string ext)
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Sports Interactive", "Football Manager 2026");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
            File.WriteAllText(path, content, Encoding.UTF8);
            TacticsDumpPlugin.Log.LogInfo($"[TD] Salvo: {path}");
        }

        private static string JS(string s)
            => s == null ? "null" : "\"" + s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n","\\n").Replace("\r","\\r") + "\"";
        private static string Trunc(string s, int max)
            => string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s.Substring(0, max) + "...";

        public class TileData { public string Name; public List<OptionData> Options = new List<OptionData>(); }
        public class OptionData { public string Value; public bool IsSelected; }
    }
}
