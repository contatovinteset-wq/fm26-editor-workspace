using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using BepInEx.Logging;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26TacticsDump
{
    [BepInPlugin("com.vintesetfm.tactics_dump", "FM26 Tactics Dump", "2.1.6")]
    public class TacticsDumpPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("FM26 Tactics Dump v2.1.6");
            Log.LogInfo("ABRA O MODAL de Instrucoes e pressione F10");
            AddComponent<TacticsDumpBehaviour>();
        }
    }

    public class TacticsDumpBehaviour : MonoBehaviour
    {
        public TacticsDumpBehaviour(IntPtr ptr) : base(ptr) { }

        private static readonly HashSet<string> IgnoredNames = new HashSet<string>
            { "Body", "Placeholder", "GridElementControls" };

        private readonly List<UIDocument> _docs = new List<UIDocument>();

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.f10Key.wasPressedThisFrame)
                StartCoroutine(DumpTacticsAsync().WrapToIl2Cpp());
        }

        [HideFromIl2Cpp]
        private VisualElement GetRoot()
        {
            _docs.Clear();
            try { foreach (var d in FindObjectsOfType<UIDocument>())
                      if (d.rootVisualElement?.name == "PanelManager-container") _docs.Add(d); }
            catch { }
            return _docs.Count > 0 ? _docs[0].rootVisualElement : null;
        }

        // ── F10 ──────────────────────────────────────────────────────────
        [HideFromIl2Cpp]
        private IEnumerator DumpTacticsAsync()
        {
            TacticsDumpPlugin.Log.LogInfo("[TD] v2.1.6 iniciando...");

            // Busca o root COMPLETO do PanelManager (inclui Overlay onde fica o modal)
            VisualElement root = null;
            try { root = GetRoot(); } catch { }
            if (root == null) { TacticsDumpPlugin.Log.LogWarning("[TD] PanelManager nao encontrado."); yield break; }

            // Busca ScrollViews no root INTEIRO — nao apenas no TacticalPlannerTool
            var scrollViews = new List<VisualElement>();
            try { FindAllScrollViews(root, scrollViews, 0); } catch { }
            TacticsDumpPlugin.Log.LogInfo($"[TD] {scrollViews.Count} ScrollView(s) no PanelManager inteiro");

            var allTiles = new List<TileData>();
            var globalSeen = new HashSet<string>();

            foreach (var sv in scrollViews)
            {
                // Loga qual layer este ScrollView pertence
                string parentChain = GetAncestorChain(sv, 5);
                int siTileCount = CountDescendantsWithClass(sv, "si-tile", 0, 8);
                TacticsDumpPlugin.Log.LogInfo($"[TD] SV: si-tiles={siTileCount} | chain=[{parentChain}]");

                if (siTileCount == 0) continue;

                yield return StartCoroutine(ScrollAndCapture(sv, allTiles, globalSeen).WrapToIl2Cpp());
            }

            // Captura estatica de si-tiles fora de ScrollViews
            try { CaptureStaticSiTiles(root, allTiles, globalSeen); } catch { }

            if (allTiles.Count == 0)
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] Nenhum tile capturado.");
                TacticsDumpPlugin.Log.LogWarning("[TD] Abra o modal de Instrucoes a Equipa antes de pressionar F10!");
                yield break;
            }

            int ip = 0, oop = 0, gen = 0;
            foreach (var t in allTiles)
            { if (t.Possession == "ComPosse") ip++; else if (t.Possession == "SemPosse") oop++; else gen++; }
            TacticsDumpPlugin.Log.LogInfo($"[TD] Total: {allTiles.Count} tiles | ComPosse={ip} SemPosse={oop} Geral={gen}");

            string label = ip > 0 && oop > 0 ? "Ambas" : ip > 0 ? "ComPosse" : oop > 0 ? "SemPosse" : "Geral";
            try { SaveFile(BuildJson(allTiles), $"tactics_dump_{label}"); }
            catch (Exception ex) { TacticsDumpPlugin.Log.LogError($"[TD] Erro ao salvar: {ex.Message}"); }
        }

        // ── ScrollView: scroll completo ───────────────────────────────────
        [HideFromIl2Cpp]
        private IEnumerator ScrollAndCapture(VisualElement svEl, List<TileData> allTiles, HashSet<string> globalSeen)
        {
            ScrollView sv = null;
            try { sv = new ScrollView(svEl.Pointer); } catch { yield break; }

            float contentH = 10000f;
            try { var cc = sv.contentContainer; if (cc != null) contentH = Math.Max(cc.layout.height, 300f); } catch { }
            TacticsDumpPlugin.Log.LogInfo($"[TD] Varrendo ScrollView contentH={contentH:0}");

            try { sv.scrollOffset = new Vector2(0, 0); } catch { yield break; }
            yield return new WaitForSeconds(0.2f);

            float step = 180f, pos = 0f, lastActual = -1f;
            int stuckCount = 0;

            while (pos <= contentH + step)
            {
                try { CaptureSiTilesUnder(svEl, allTiles, globalSeen); } catch { }

                pos += step;
                try { sv.scrollOffset = new Vector2(0, pos); } catch { break; }
                yield return new WaitForSeconds(0.07f);

                float actual = 0f;
                try { actual = sv.scrollOffset.y; } catch { }
                if (pos > step * 2 && Math.Abs(actual - lastActual) < 5f)
                { stuckCount++; if (stuckCount >= 3) break; }
                else stuckCount = 0;
                lastActual = actual;
            }

            try { CaptureSiTilesUnder(svEl, allTiles, globalSeen); } catch { }
            try { sv.scrollOffset = new Vector2(0, 0); } catch { }
            TacticsDumpPlugin.Log.LogInfo($"[TD] ScrollView concluido. Total tiles ate agora: {allTiles.Count}");
        }

        // ── Captura si-tiles visiveis ─────────────────────────────────────
        [HideFromIl2Cpp]
        private void CaptureSiTilesUnder(VisualElement root, List<TileData> allTiles, HashSet<string> globalSeen)
        {
            var tiles = new List<VisualElement>();
            try { FindAllByClass(root, "si-tile", tiles, 0, 60); } catch { return; }

            foreach (var siTile in tiles)
            {
                try
                {
                    string tileName = siTile.childCount > 0 ? siTile[0].name : "";
                    if (string.IsNullOrEmpty(tileName) || IgnoredNames.Contains(tileName)) continue;

                    string possession = DeterminePossession(siTile);
                    string key = $"{tileName}|{possession}";
                    if (globalSeen.Contains(key)) continue;
                    globalSeen.Add(key);

                    string val = GetSelectedValue(siTile);
                    if (!string.IsNullOrEmpty(val))
                    {
                        allTiles.Add(new TileData { Name = tileName, Value = val, Possession = possession });
                        TacticsDumpPlugin.Log.LogInfo($"[TD] + {tileName} = {val} [{possession}]");
                    }
                    else
                    {
                        string chain = GetAncestorChain(siTile, 5);
                        TacticsDumpPlugin.Log.LogInfo($"[TD] ? {tileName} sem valor | {chain}");
                    }
                }
                catch { }
            }
        }

        // ── Captura si-tiles fora de ScrollViews ──────────────────────────
        [HideFromIl2Cpp]
        private void CaptureStaticSiTiles(VisualElement root, List<TileData> allTiles, HashSet<string> globalSeen)
        {
            var tiles = new List<VisualElement>();
            try { FindAllByClass(root, "si-tile", tiles, 0, 80); } catch { return; }
            int added = 0;
            foreach (var siTile in tiles)
            {
                try
                {
                    if (HasAncestorWithScrollView(siTile)) continue;
                    string tileName = siTile.childCount > 0 ? siTile[0].name : "";
                    if (string.IsNullOrEmpty(tileName) || IgnoredNames.Contains(tileName)) continue;
                    string possession = DeterminePossession(siTile);
                    string key = $"{tileName}|{possession}";
                    if (globalSeen.Contains(key)) continue;
                    globalSeen.Add(key);
                    string val = GetSelectedValue(siTile);
                    if (!string.IsNullOrEmpty(val))
                    { allTiles.Add(new TileData { Name = tileName, Value = val, Possession = possession }); added++; }
                }
                catch { }
            }
            if (added > 0) TacticsDumpPlugin.Log.LogInfo($"[TD] {added} tile(s) estaticos capturados");
        }

        // ── Posse por ancestor chain ──────────────────────────────────────
        [HideFromIl2Cpp]
        private string DeterminePossession(VisualElement el)
        {
            var cur = el.parent; int d = 0;
            while (cur != null && d < 60)
            {
                try
                {
                    string n = (cur.name ?? "").ToLower();
                    if (n == "ip-tiles" || n.Contains("in-possession") || n.Contains("inpossession")
                        || n == "ip_tiles" || n == "compossedebola") return "ComPosse";
                    if (n == "oop-tiles" || n.Contains("out-of-possession") || n.Contains("outofpossession")
                        || n == "oop_tiles" || n == "sempossedabola") return "SemPosse";
                }
                catch { }
                cur = cur.parent; d++;
            }
            return "Geral";
        }

        [HideFromIl2Cpp]
        private string GetAncestorChain(VisualElement el, int max)
        {
            var names = new List<string>();
            var cur = el.parent; int d = 0;
            while (cur != null && d < max)
            {
                try { string n = cur.name; if (!string.IsNullOrEmpty(n)) names.Add(n); } catch { }
                cur = cur.parent; d++;
            }
            return string.Join(" > ", names);
        }

        [HideFromIl2Cpp]
        private bool HasAncestorWithScrollView(VisualElement el)
        {
            var cur = el.parent; int d = 0;
            while (cur != null && d < 30)
            {
                try { if (HasClass(cur, "unity-scroll-view") || HasClass(cur, "siscrollview")) return true; } catch { }
                cur = cur.parent; d++;
            }
            return false;
        }

        [HideFromIl2Cpp]
        private int CountDescendantsWithClass(VisualElement root, string cls, int depth, int maxDepth)
        {
            if (root == null || depth > maxDepth) return 0;
            int count = 0;
            try { if (HasClass(root, cls)) count++; } catch { }
            for (int i = 0; i < root.childCount; i++)
                try { count += CountDescendantsWithClass(root[i], cls, depth + 1, maxDepth); } catch { }
            return count;
        }

        // ── Encontra ScrollViews ──────────────────────────────────────────
        [HideFromIl2Cpp]
        private void FindAllScrollViews(VisualElement root, List<VisualElement> res, int depth)
        {
            if (root == null || depth > 70) return;
            try
            {
                if (HasClass(root, "unity-scroll-view") || HasClass(root, "siscrollview"))
                { res.Add(root); return; }
            }
            catch { }
            for (int i = 0; i < root.childCount; i++)
                try { FindAllScrollViews(root[i], res, depth + 1); } catch { }
        }

        // ── Extração de valor ─────────────────────────────────────────────
        [HideFromIl2Cpp]
        private string GetSelectedValue(VisualElement siTile)
        {
            try { var n = FindByClass(siTile, "name", 0, 15); if (n != null) { string t = TryGetText(n); if (!string.IsNullOrEmpty(t)) return t; } } catch { }
            try { string s = FindFirstSitext(siTile); if (!string.IsNullOrEmpty(s)) return s; } catch { }
            try { string ic = GetValueFromIconClass(siTile); if (!string.IsNullOrEmpty(ic)) return ic; } catch { }
            return null;
        }

        [HideFromIl2Cpp]
        private string FindFirstSitext(VisualElement root)
        {
            if (root == null) return null;
            try { if (HasClass(root, "sitext")) { string t = TryGetText(root); if (!string.IsNullOrEmpty(t)) return t; } } catch { }
            for (int i = 0; i < root.childCount; i++)
                try { string r = FindFirstSitext(root[i]); if (!string.IsNullOrEmpty(r)) return r; } catch { }
            return null;
        }

        [HideFromIl2Cpp]
        private string GetValueFromIconClass(VisualElement siTile)
        {
            VisualElement icon = null;
            try { icon = FindByName(siTile, "Icon"); } catch { }
            if (icon == null) return null;
            try
            {
                for (int c = 0; c < icon.classList.Count; c++)
                {
                    string cls = icon.classList[c];
                    if (!cls.StartsWith("tactic_icons-")) continue;
                    int vi = cls.LastIndexOf("-var-");
                    if (vi >= 0) { string r = cls.Substring(vi + 5).Trim('-'); if (!string.IsNullOrEmpty(r) && r != "null") return r; }
                    int ti = cls.LastIndexOf("-type-");
                    if (ti >= 0) { string r = cls.Substring(ti + 6).Trim('-'); if (!string.IsNullOrEmpty(r)) return r; }
                }
            }
            catch { }
            return null;
        }

        [HideFromIl2Cpp]
        private string TryGetText(VisualElement el)
        {
            if (el == null) return null;
            try { var l = new Label(el.Pointer); if (l?.text?.Length > 0) return l.text.Trim(); } catch { }
            try { var te = new TextElement(el.Pointer); if (te?.text?.Length > 0) return te.text.Trim(); } catch { }
            try { var p = el.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                  if (p != null) { string v = p.GetValue(el) as string; if (!string.IsNullOrWhiteSpace(v)) return v.Trim(); } } catch { }
            return null;
        }

        // ── Helpers ───────────────────────────────────────────────────────
        [HideFromIl2Cpp]
        private bool HasClass(VisualElement el, string cls)
        {
            if (el == null) return false;
            try { for (int c = 0; c < el.classList.Count; c++) if (el.classList[c].Contains(cls)) return true; } catch { }
            return false;
        }

        [HideFromIl2Cpp]
        private VisualElement FindByClass(VisualElement root, string cls, int depth, int maxDepth)
        {
            if (root == null || depth > maxDepth) return null;
            try { for (int c = 0; c < root.classList.Count; c++) if (root.classList[c] == cls) return root; } catch { }
            for (int i = 0; i < root.childCount; i++)
                try { var f = FindByClass(root[i], cls, depth + 1, maxDepth); if (f != null) return f; } catch { }
            return null;
        }

        [HideFromIl2Cpp]
        private void FindAllByClass(VisualElement root, string cls, List<VisualElement> res, int depth, int maxDepth)
        {
            if (root == null || depth > maxDepth) return;
            try { if (HasClass(root, cls)) res.Add(root); } catch { }
            for (int i = 0; i < root.childCount; i++)
                try { FindAllByClass(root[i], cls, res, depth + 1, maxDepth); } catch { }
        }

        [HideFromIl2Cpp]
        private VisualElement FindByName(VisualElement root, string name)
        {
            if (root == null) return null;
            try { if (root.name == name) return root; } catch { return null; }
            for (int i = 0; i < root.childCount; i++)
                try { var r = FindByName(root[i], name); if (r != null) return r; } catch { }
            return null;
        }

        // ── JSON / Save ───────────────────────────────────────────────────
        [HideFromIl2Cpp]
        private string BuildJson(List<TileData> tiles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-ddTHH:mm:ss}\",");
            sb.AppendLine("  \"tiles\": [");
            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i];
                string comma = i < tiles.Count - 1 ? "," : "";
                sb.AppendLine($"    {{ \"name\": {JS(t.Name)}, \"value\": {JS(t.Value)}, \"possession\": {JS(t.Possession)} }}{comma}");
            }
            sb.AppendLine("  ]"); sb.AppendLine("}");
            return sb.ToString();
        }

        [HideFromIl2Cpp]
        private void SaveFile(string content, string prefix)
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(dir, $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(path, content, Encoding.UTF8);
            TacticsDumpPlugin.Log.LogInfo($"[TD] Salvo: {path}");
        }

        private static string JS(string s)
            => s == null ? "null" : "\"" + s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n","\\n").Replace("\r","\\r") + "\"";

        public class TileData { public string Name; public string Value; public string Possession; }
    }
}
