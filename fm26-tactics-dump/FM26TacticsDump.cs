using System;
using System.Collections;
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
    [BepInPlugin("com.vintesetfm.tactics_dump", "FM26 Tactics Dump", "2.0.0")]
    public class TacticsDumpPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("FM26 Tactics Dump v2.0.0");
            Log.LogInfo("F10 = Dump taticas (ambas abas) | F11 = Diag profundo");
            AddComponent<TacticsDumpBehaviour>();
        }
    }

    public class TacticsDumpBehaviour : MonoBehaviour
    {
        public TacticsDumpBehaviour(IntPtr ptr) : base(ptr) { }

        private List<UIDocument> _docs = new List<UIDocument>();

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.f10Key.wasPressedThisFrame) 
                StartCoroutine(DumpTacticsAsync());
            if (Keyboard.current.f11Key.wasPressedThisFrame) 
                DiagDeep();
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

        // ── F10: Dump completo com scroll e ambas abas ───────────────────
        [HideFromIl2Cpp]
        private IEnumerator DumpTacticsAsync()
        {
            TacticsDumpPlugin.Log.LogInfo("[TD] Iniciando dump completo...");
            
            var root = GetRoot();
            if (root == null) 
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] Sem PanelManager");
                yield break;
            }

            var planner = FindByName(root, "TacticalPlannerTool");
            if (planner == null)
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] TacticalPlannerTool não encontrado. Abra Taticas > Instrucoes a Equipa.");
                yield break;
            }

            var allTiles = new List<TileData>();
            string currentPossession = "";

            // Detectar aba ativa atual
            var ipButton = FindByName(planner, "IP");
            bool isIPActive = HasClass(ipButton, "buttongroup__button--active");
            currentPossession = isIPActive ? "ComPosse" : "SemPosse";
            
            TacticsDumpPlugin.Log.LogInfo($"[TD] Aba ativa: {currentPossession}");

            // Capturar aba atual
            yield return StartCoroutine(CaptureCurrentTab(planner, allTiles, currentPossession));

            // Trocar para outra aba e capturar
            string otherPossession = isIPActive ? "SemPosse" : "ComPosse";
            var targetButton = isIPActive ? FindByName(planner, "OOP") : FindByName(planner, "IP");
            
            if (targetButton != null)
            {
                TacticsDumpPlugin.Log.LogInfo($"[TD] Trocando para aba {otherPossession}...");
                
                // Simular clique
                using (var evt = NavigationSubmitEvent.GetPooled())
                {
                    evt.target = targetButton;
                    targetButton.SendEvent(evt);
                }
                
                // Aguardar renderização
                yield return new WaitForSeconds(0.5f);
                
                yield return StartCoroutine(CaptureCurrentTab(planner, allTiles, otherPossession));
                
                // Voltar para aba original
                using (var evt = NavigationSubmitEvent.GetPooled())
                {
                    evt.target = ipButton;
                    ipButton.SendEvent(evt);
                }
            }

            if (allTiles.Count == 0)
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] Nenhum tile capturado. Possível problema de timing.");
                yield break;
            }

            // Salvar JSON
            SaveFile(BuildJson(allTiles), "tactics_dump");
            TacticsDumpPlugin.Log.LogInfo($"[TD] Dump completo: {allTiles.Count} tiles capturados");
        }

        [HideFromIl2Cpp]
        private IEnumerator CaptureCurrentTab(VisualElement planner, List<TileData> allTiles, string possession)
        {
            var glecs = new List<VisualElement>();
            FindAllByName(planner, "GridLayoutElementContent", glecs, 0);
            
            TacticsDumpPlugin.Log.LogInfo($"[TD] {glecs.Count} GLECs encontrados na aba {possession}");

            foreach (var glec in glecs)
            {
                if (!IsInstructionGlec(glec)) continue;

                // Capturar tiles visíveis
                yield return StartCoroutine(CaptureTilesFromGlec(glec, allTiles, possession));
            }
        }

        [HideFromIl2Cpp]
        private IEnumerator CaptureTilesFromGlec(VisualElement glec, List<TileData> allTiles, string possession)
        {
            // Encontrar ScrollView pai
            var scrollView = FindParentScrollView(glec);
            
            if (scrollView != null)
            {
                // Scroll para o topo
                scrollView.scrollOffset = new Vector2(0, 0);
                yield return new WaitForSeconds(0.1f);
                
                // Capturar tiles visíveis no topo
                ExtractTilesFromGlec(glec, allTiles, possession);
                
                // Scroll gradual para capturar todos
                float scrollStep = 200f;
                float maxScroll = 5000f;
                float currentScroll = 0f;
                int previousCount = allTiles.Count;
                int noNewTilesCount = 0;
                
                while (currentScroll < maxScroll && noNewTilesCount < 3)
                {
                    currentScroll += scrollStep;
                    scrollView.scrollOffset = new Vector2(0, currentScroll);
                    yield return new WaitForSeconds(0.05f);
                    
                    int countBefore = allTiles.Count;
                    ExtractTilesFromGlec(glec, allTiles, possession);
                    
                    if (allTiles.Count == previousCount)
                    {
                        noNewTilesCount++;
                    }
                    else
                    {
                        noNewTilesCount = 0;
                        previousCount = allTiles.Count;
                    }
                }
                
                // Voltar scroll para início
                scrollView.scrollOffset = new Vector2(0, 0);
            }
            else
            {
                // Sem ScrollView, capturar direto
                ExtractTilesFromGlec(glec, allTiles, possession);
            }
        }

        [HideFromIl2Cpp]
        private void ExtractTilesFromGlec(VisualElement glec, List<TileData> allTiles, string possession)
        {
            var existingNames = new HashSet<string>();
            foreach (var t in allTiles) existingNames.Add(t.Name);

            for (int i = 0; i < glec.childCount; i++)
            {
                var siTile = glec[i];
                if (!HasClass(siTile, "si-tile")) continue;

                // Nome do tile
                string tileName = siTile.childCount > 0 ? siTile[0].name : $"Tile{i}";
                
                // Filtrar "Body" (painel principal)
                if (tileName == "Body") continue;
                
                // Dedup
                if (existingNames.Contains(tileName)) continue;
                existingNames.Add(tileName);

                // Valor selecionado
                string selectedValue = GetSelectedValue(siTile);
                
                if (!string.IsNullOrEmpty(selectedValue))
                {
                    allTiles.Add(new TileData 
                    { 
                        Name = tileName, 
                        Value = selectedValue,
                        Possession = possession
                    });
                    
                    TacticsDumpPlugin.Log.LogInfo($"[TD] + {tileName} = {selectedValue} [{possession}]");
                }
            }
        }

        [HideFromIl2Cpp]
        private string GetSelectedValue(VisualElement siTile)
        {
            // Procurar elemento com classe "name" dentro do tile
            var nameEl = FindByNameClass(siTile, "name", 0, 10);
            if (nameEl != null)
            {
                string txt = TryGetText(nameEl);
                if (!string.IsNullOrEmpty(txt)) return txt;
            }
            
            // Fallback: procurar primeiro texto
            var texts = new List<string>();
            CollectTextsDeep(siTile, texts, 0, 20);
            return texts.Count > 0 ? texts[0] : null;
        }

        [HideFromIl2Cpp]
        private VisualElement FindByNameClass(VisualElement root, string className, int depth, int maxDepth)
        {
            if (root == null || depth > maxDepth) return null;
            
            try
            {
                for (int c = 0; c < root.classList.Count; c++)
                {
                    if (root.classList[c] == className) return root;
                }
            } catch { }
            
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindByNameClass(root[i], className, depth + 1, maxDepth);
                if (found != null) return found;
            }
            
            return null;
        }

        [HideFromIl2Cpp]
        private ScrollView FindParentScrollView(VisualElement el)
        {
            var current = el.parent;
            int maxDepth = 20;
            int depth = 0;
            
            while (current != null && depth < maxDepth)
            {
                if (current is ScrollView sv) return sv;
                // Checar pela classe também
                if (HasClass(current, "unity-scroll-view") || HasClass(current, "siscrollview"))
                {
                    try { return current as ScrollView; } catch { }
                }
                current = current.parent;
                depth++;
            }
            
            return null;
        }

        // ── F11: Diagnóstico ───────────────────────────────────────────────
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
            sb.AppendLine($"# TacticsDump Diag v2.0.0 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# GLECs: {glecs.Count}");
            sb.AppendLine();

            for (int g = 0; g < glecs.Count; g++)
            {
                var glec = glecs[g];
                bool isInstruction = IsInstructionGlec(glec);
                sb.AppendLine($"=== GLEC[{g}] ch={glec.childCount} isInstruction={isInstruction} ===");
                DumpDeep(glec, sb, 0, 30);
                sb.AppendLine();
            }

            SaveFile(sb.ToString(), "tactics_diag", ".txt");
            TacticsDumpPlugin.Log.LogInfo($"[TD] Diag salvo: {glecs.Count} GLECs");
        }

        // ── Identifica GLEC de instrucao ───────────────────────────────────
        [HideFromIl2Cpp]
        private bool IsInstructionGlec(VisualElement glec)
        {
            if (glec.childCount == 0) return false;
            
            for (int i = 0; i < Math.Min(glec.childCount, 5); i++)
            {
                var child = glec[i];
                if (!HasClass(child, "si-tile")) continue;
                
                if (child.childCount > 0)
                {
                    string name = child[0].name ?? "";
                    // Nomes conhecidos de instruções
                    if (name == "PassingDirectness" || name == "Tempo" || name == "TimeWasting" ||
                        name == "AttackingTransition" || name == "TeamWidth" || name == "StoppageStrategy" ||
                        name == "CreativeFreedom" || name == "Pressing" || name == "PressureIntensity" ||
                        name == "DefensiveLine" || name == "LineOfEngagement" || name == "DefensiveWidth" ||
                        name == "TackleIntensity" || name == "OffsideTrap" || name == "PreventShortGKDist" ||
                        name == "PressingType" || name.Contains("Tile"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // ── Helpers ─────────────────────────────────────────────────────────
        [HideFromIl2Cpp]
        private bool HasClass(VisualElement el, string cls)
        {
            if (el == null) return false;
            try 
            { 
                for (int c = 0; c < el.classList.Count; c++) 
                    if (el.classList[c] == cls || el.classList[c].Contains(cls)) return true; 
            }
            catch { }
            return false;
        }

        [HideFromIl2Cpp]
        private string TryGetText(VisualElement el)
        {
            if (el == null) return null;
            
            // 1. Label
            try { var l = el.TryCast<Label>(); if (l?.text?.Length > 0) return l.text.Trim(); } catch { }
            // 2. TextElement
            try { var te = el.TryCast<TextElement>(); if (te?.text?.Length > 0) return te.text.Trim(); } catch { }
            // 3. Reflection
            try
            {
                var prop = el.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    string v = prop.GetValue(el) as string;
                    if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
                }
            } catch { }
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
        private VisualElement FindByName(VisualElement root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) 
            { 
                var r = FindByName(root[i], name); 
                if (r != null) return r; 
            }
            return null;
        }

        [HideFromIl2Cpp]
        private void FindAllByName(VisualElement root, string name, List<VisualElement> res, int depth)
        {
            if (root == null || depth > 60) return;
            if (root.name == name) res.Add(root);
            for (int i = 0; i < root.childCount; i++) 
                FindAllByName(root[i], name, res, depth + 1);
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

        // ── JSON / Save ─────────────────────────────────────────────────────
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
                sb.AppendLine($"    {{ \"name\": {JS(t.Name)}, \"value\": {JS(t.Value)}, \"possession\": {JS(t.Possession)} }}{(i < tiles.Count - 1 ? "," : "")}");
            }
            
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        [HideFromIl2Cpp]
        private void SaveFile(string content, string prefix, string ext = ".json")
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

        public class TileData 
        { 
            public string Name; 
            public string Value; 
            public string Possession;
        }
    }
}
