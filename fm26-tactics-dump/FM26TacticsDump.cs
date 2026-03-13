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
    [BepInPlugin("com.vintesetfm.tactics_dump", "FM26 Tactics Dump", "2.2.0")]
    public class TacticsDumpPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("FM26 Tactics Dump v2.2.0");
            Log.LogInfo("F10 = Dump taticas da aba ATUAL (com scroll automatico)");
            Log.LogInfo("F11 = Diagnostico profundo");
            AddComponent<TacticsDumpBehaviour>();
        }
    }

    public class TacticsDumpBehaviour : MonoBehaviour
    {
        public TacticsDumpBehaviour(IntPtr ptr) : base(ptr) { }

        private List<UIDocument> _docs = new List<UIDocument>();
        private bool _dumpInProgress = false;
        private int _scrollFrame = 0;
        private List<TileData> _capturedTiles = new List<TileData>();
        private HashSet<string> _capturedNames = new HashSet<string>();
        private VisualElement _currentGlec;
        private ScrollView _currentScrollView;
        private float _currentScrollPos = 0f;
        private string _currentPossession = "";
        private int _noNewTilesCount = 0;
        private int _phase = 0;

        private void Update()
        {
            if (Keyboard.current == null) return;
            
            if (_dumpInProgress)
            {
                ProcessScroll();
                return;
            }
            
            if (Keyboard.current.f10Key.wasPressedThisFrame) 
                StartDump();
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

        // ── F10: Dump com scroll automatico ─────────────────────────────────
        [HideFromIl2Cpp]
        private void StartDump()
        {
            TacticsDumpPlugin.Log.LogInfo("[TD] === Iniciando dump com scroll ===");
            
            var root = GetRoot();
            if (root == null) 
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] Sem PanelManager");
                return;
            }

            var planner = FindByName(root, "TacticalPlannerTool");
            if (planner == null)
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] Abra Taticas > Instrucoes a Equipa");
                return;
            }

            // Detectar aba ativa
            var ipButton = FindByName(planner, "IP");
            bool isIPActive = HasClass(ipButton, "buttongroup__button--active");
            _currentPossession = isIPActive ? "ComPosse" : "SemPosse";
            
            TacticsDumpPlugin.Log.LogInfo($"[TD] Aba ativa: {_currentPossession}");
            
            // Encontrar GLECs de instrucoes
            var glecs = new List<VisualElement>();
            FindAllByName(planner, "GridLayoutElementContent", glecs, 0);
            
            _currentGlec = null;
            _currentScrollView = null;
            
            foreach (var glec in glecs)
            {
                if (IsInstructionGlec(glec))
                {
                    _currentGlec = glec;
                    _currentScrollView = FindParentScrollView(glec);
                    break;
                }
            }
            
            if (_currentGlec == null)
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] Nenhum GLEC de instrucao encontrado");
                return;
            }
            
            TacticsDumpPlugin.Log.LogInfo($"[TD] GLEC encontrado com {_currentGlec.childCount} filhos");
            TacticsDumpPlugin.Log.LogInfo($"[TD] ScrollView: {(_currentScrollView != null ? "sim" : "nao")}");
            
            // Iniciar captura
            _capturedTiles.Clear();
            _capturedNames.Clear();
            _scrollFrame = 0;
            _currentScrollPos = 0f;
            _noNewTilesCount = 0;
            _phase = 0; // 0 = topo, 1 = scroll, 2 = fim
            _dumpInProgress = true;
            
            // Capturar tiles no topo
            CaptureVisibleTiles();
        }

        [HideFromIl2Cpp]
        private void ProcessScroll()
        {
            _scrollFrame++;
            
            if (_phase == 0 && _scrollFrame >= 5)
            {
                // Fase 0: topo capturado, iniciar scroll
                if (_currentScrollView != null)
                {
                    _phase = 1;
                    _scrollFrame = 0;
                    TacticsDumpPlugin.Log.LogInfo("[TD] Iniciando scroll...");
                }
                else
                {
                    // Sem scroll, finalizar
                    FinishDump();
                }
            }
            else if (_phase == 1)
            {
                // Fase 1: scroll gradual
                if (_scrollFrame % 3 == 0) // a cada 3 frames (~50ms)
                {
                    _currentScrollPos += 150f;
                    
                    try
                    {
                        _currentScrollView.scrollOffset = new Vector2(0, _currentScrollPos);
                    }
                    catch { }
                    
                    int countBefore = _capturedTiles.Count;
                    CaptureVisibleTiles();
                    
                    if (_capturedTiles.Count == countBefore)
                    {
                        _noNewTilesCount++;
                        if (_noNewTilesCount >= 5)
                        {
                            TacticsDumpPlugin.Log.LogInfo($"[TD] Scroll finalizado - sem novos tiles por {_noNewTilesCount} iteracoes");
                            _phase = 2;
                        }
                    }
                    else
                    {
                        _noNewTilesCount = 0;
                    }
                    
                    if (_currentScrollPos > 10000f)
                    {
                        TacticsDumpPlugin.Log.LogInfo("[TD] Scroll chegou ao limite");
                        _phase = 2;
                    }
                }
            }
            else if (_phase == 2)
            {
                FinishDump();
            }
        }

        [HideFromIl2Cpp]
        private void CaptureVisibleTiles()
        {
            if (_currentGlec == null) return;
            
            for (int i = 0; i < _currentGlec.childCount; i++)
            {
                var siTile = _currentGlec[i];
                if (!HasClass(siTile, "si-tile")) continue;

                string tileName = siTile.childCount > 0 ? siTile[0].name : $"Tile{i}";
                
                if (tileName == "Body") continue;
                if (_capturedNames.Contains(tileName)) continue;

                string selectedValue = GetSelectedValue(siTile);
                
                if (!string.IsNullOrEmpty(selectedValue))
                {
                    _capturedNames.Add(tileName);
                    _capturedTiles.Add(new TileData 
                    { 
                        Name = tileName, 
                        Value = selectedValue,
                        Possession = _currentPossession
                    });
                    
                    TacticsDumpPlugin.Log.LogInfo($"[TD] + {tileName} = {selectedValue}");
                }
            }
        }

        [HideFromIl2Cpp]
        private void FinishDump()
        {
            _dumpInProgress = false;
            
            // Voltar scroll para inicio
            if (_currentScrollView != null)
            {
                try { _currentScrollView.scrollOffset = new Vector2(0, 0); } catch { }
            }
            
            if (_capturedTiles.Count == 0)
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] Nenhum tile capturado");
                return;
            }

            SaveFile(BuildJson(_capturedTiles), "tactics_dump");
            TacticsDumpPlugin.Log.LogInfo($"[TD] === Dump completo: {_capturedTiles.Count} tiles [{_currentPossession}] ===");
        }

        [HideFromIl2Cpp]
        private string GetSelectedValue(VisualElement siTile)
        {
            // Procurar elemento com classe "name"
            var nameEl = FindByClass(siTile, "name", 0, 15);
            if (nameEl != null)
            {
                string txt = TryGetText(nameEl);
                if (!string.IsNullOrEmpty(txt)) return txt;
            }
            
            // Fallback: procurar dentro de name-style-setter
            var nameStyleSetter = FindByName(siTile, "name-style-setter");
            if (nameStyleSetter != null)
            {
                var nameInSetter = FindByClass(nameStyleSetter, "name", 0, 5);
                if (nameInSetter != null)
                {
                    string txt = TryGetText(nameInSetter);
                    if (!string.IsNullOrEmpty(txt)) return txt;
                }
            }
            
            return null;
        }

        [HideFromIl2Cpp]
        private VisualElement FindByClass(VisualElement root, string className, int depth, int maxDepth)
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
                var found = FindByClass(root[i], className, depth + 1, maxDepth);
                if (found != null) return found;
            }
            
            return null;
        }

        [HideFromIl2Cpp]
        private ScrollView FindParentScrollView(VisualElement el)
        {
            var current = el.parent;
            int depth = 0;
            
            while (current != null && depth < 30)
            {
                if (current is ScrollView sv) return sv;
                current = current.parent;
                depth++;
            }
            
            return null;
        }

        // ── F11: Diagnostico ───────────────────────────────────────────────
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
            sb.AppendLine($"# TacticsDump Diag v2.2.0 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# GLECs: {glecs.Count}");
            sb.AppendLine();

            for (int g = 0; g < glecs.Count; g++)
            {
                var glec = glecs[g];
                bool isInstruction = IsInstructionGlec(glec);
                sb.AppendLine($"=== GLEC[{g}] ch={glec.childCount} isInstruction={isInstruction} ===");
                DumpDeep(glec, sb, 0, 35);
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
                    // Nomes de instrucoes conhecidas
                    string[] known = {
                        "PassingDirectness", "Tempo", "TimeWasting", "AttackingTransition",
                        "TeamWidth", "StoppageStrategy", "CreativeFreedom", "Pressing",
                        "PressureIntensity", "DefensiveLine", "LineOfEngagement", "DefensiveWidth",
                        "TackleIntensity", "OffsideTrap", "PreventShortGKDist", "PressingType"
                    };
                    
                    foreach (var k in known)
                        if (name == k) return true;
                        
                    if (name.Contains("Tile") && name != "Body") return true;
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
                    if (el.classList[c] == cls) return true; 
            }
            catch { }
            return false;
        }

        [HideFromIl2Cpp]
        private string TryGetText(VisualElement el)
        {
            if (el == null) return null;
            
            try { var l = el.TryCast<Label>(); if (l?.text?.Length > 0) return l.text.Trim(); } catch { }
            try { var te = el.TryCast<TextElement>(); if (te?.text?.Length > 0) return te.text.Trim(); } catch { }
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
            string txtStr = !string.IsNullOrEmpty(txt) ? $" \"{Trunc(txt, 50)}\"" : "";
            string cls = classes.Count > 0 ? $" [{string.Join(", ", classes)}]" : "";
            sb.AppendLine($"{new string(' ', depth * 2)}{el.GetType().Name} name={el.name}{cls}{txtStr} ch={el.childCount}");
            for (int i = 0; i < el.childCount; i++) DumpDeep(el[i], sb, depth + 1, maxDepth);
        }

        // ── JSON / Save ─────────────────────────────────────────────────────
        [HideFromIl2Cpp]
        private string BuildJson(List<TileData> tiles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-ddTHH:mm:ss}\",");
            sb.AppendLine($"  \"possession\": \"{_currentPossession}\",");
            sb.AppendLine("  \"tiles\": [");
            
            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i];
                sb.AppendLine($"    {{ \"name\": {JS(t.Name)}, \"value\": {JS(t.Value)} }}{(i < tiles.Count - 1 ? "," : "")}");
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
