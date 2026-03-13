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
    [BepInPlugin("com.vintesetfm.tactics_dump", "FM26 Tactics Dump", "2.1.0")]
    public class TacticsDumpPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("FM26 Tactics Dump v2.1.0");
            Log.LogInfo("F10 = Dump taticas (ambas abas) | F11 = Diag profundo");
            AddComponent<TacticsDumpBehaviour>();
        }
    }

    public class TacticsDumpBehaviour : MonoBehaviour
    {
        public TacticsDumpBehaviour(IntPtr ptr) : base(ptr) { }

        private List<UIDocument> _docs = new List<UIDocument>();
        private int _frameCounter = 0;
        private bool _dumpInProgress = false;
        private int _dumpPhase = 0;
        private List<TileData> _pendingTiles = new List<TileData>();
        private string _pendingPossession = "";
        private VisualElement _pendingTargetButton;
        private bool _wasIPActive;

        private void Update()
        {
            if (Keyboard.current == null) return;
            
            // Processar fases do dump
            if (_dumpInProgress)
            {
                ProcessDumpPhase();
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

        // ── F10: Dump com multi-frame ─────────────────────────────────────
        [HideFromIl2Cpp]
        private void StartDump()
        {
            TacticsDumpPlugin.Log.LogInfo("[TD] Iniciando dump completo...");
            
            var root = GetRoot();
            if (root == null) 
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] Sem PanelManager");
                return;
            }

            var planner = FindByName(root, "TacticalPlannerTool");
            if (planner == null)
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] TacticalPlannerTool não encontrado. Abra Taticas > Instrucoes a Equipa.");
                return;
            }

            _pendingTiles.Clear();
            _dumpPhase = 1;
            _dumpInProgress = true;
            _frameCounter = 0;
            
            // Detectar aba ativa
            var ipButton = FindByName(planner, "IP");
            _wasIPActive = HasClass(ipButton, "buttongroup__button--active");
            _pendingPossession = _wasIPActive ? "ComPosse" : "SemPosse";
            
            TacticsDumpPlugin.Log.LogInfo($"[TD] Fase 1: Capturando {_pendingPossession}...");
        }

        [HideFromIl2Cpp]
        private void ProcessDumpPhase()
        {
            _frameCounter++;
            
            switch (_dumpPhase)
            {
                case 1: // Capturar primeira aba
                    if (_frameCounter >= 2)
                    {
                        var root = GetRoot();
                        var planner = FindByName(root, "TacticalPlannerTool");
                        CaptureCurrentTab(planner, _pendingTiles, _pendingPossession);
                        
                        // Preparar para trocar aba
                        _pendingTargetButton = _wasIPActive ? FindByName(planner, "OOP") : FindByName(planner, "IP");
                        
                        if (_pendingTargetButton != null)
                        {
                            _dumpPhase = 2;
                            _frameCounter = 0;
                            _pendingPossession = _wasIPActive ? "SemPosse" : "ComPosse";
                            
                            // Simular clique
                            SimulateClick(_pendingTargetButton);
                            TacticsDumpPlugin.Log.LogInfo($"[TD] Fase 2: Trocando para {_pendingPossession}...");
                        }
                        else
                        {
                            // Sem troca de aba, finalizar
                            FinishDump();
                        }
                    }
                    break;
                    
                case 2: // Aguardar troca de aba
                    if (_frameCounter >= 30) // ~0.5s
                    {
                        var root = GetRoot();
                        var planner = FindByName(root, "TacticalPlannerTool");
                        CaptureCurrentTab(planner, _pendingTiles, _pendingPossession);
                        
                        // Voltar para aba original
                        var originalButton = _wasIPActive ? FindByName(planner, "IP") : FindByName(planner, "OOP");
                        if (originalButton != null)
                            SimulateClick(originalButton);
                        
                        FinishDump();
                    }
                    break;
            }
        }

        [HideFromIl2Cpp]
        private void FinishDump()
        {
            _dumpInProgress = false;
            
            if (_pendingTiles.Count == 0)
            {
                TacticsDumpPlugin.Log.LogWarning("[TD] Nenhum tile capturado.");
                return;
            }

            SaveFile(BuildJson(_pendingTiles), "tactics_dump");
            TacticsDumpPlugin.Log.LogInfo($"[TD] Dump completo: {_pendingTiles.Count} tiles capturados");
        }

        [HideFromIl2Cpp]
        private void SimulateClick(VisualElement element)
        {
            if (element == null) return;
            
            try
            {
                // Usar reflexão para invocar clickable
                var clickableProp = element.GetType().GetProperty("clickable");
                if (clickableProp != null)
                {
                    var clickable = clickableProp.GetValue(element);
                    if (clickable != null)
                    {
                        var invokeMethod = clickable.GetType().GetMethod("Invoke");
                        if (invokeMethod != null)
                        {
                            invokeMethod.Invoke(clickable, new object[] { element, null });
                            TacticsDumpPlugin.Log.LogInfo("[TD] Clique simulado via clickable.Invoke");
                            return;
                        }
                    }
                }
                
                // Fallback: SendEvent via reflexão
                var sendEventMethod = element.GetType().GetMethod("SendEvent", BindingFlags.Public | BindingFlags.Instance);
                if (sendEventMethod != null)
                {
                    var getPooledMethod = typeof(ClickEvent).GetMethod("GetPooled", BindingFlags.Public | BindingFlags.Static);
                    if (getPooledMethod != null)
                    {
                        var evt = getPooledMethod.Invoke(null, new object[0]);
                        if (evt != null)
                        {
                            sendEventMethod.Invoke(element, new object[] { evt });
                            TacticsDumpPlugin.Log.LogInfo("[TD] Clique simulado via SendEvent");
                            return;
                        }
                    }
                }
                
                TacticsDumpPlugin.Log.LogWarning("[TD] Não consegui simular clique");
            }
            catch (Exception ex)
            {
                TacticsDumpPlugin.Log.LogError($"[TD] Erro ao simular clique: {ex.Message}");
            }
        }

        [HideFromIl2Cpp]
        private void CaptureCurrentTab(VisualElement planner, List<TileData> tiles, string possession)
        {
            var glecs = new List<VisualElement>();
            FindAllByName(planner, "GridLayoutElementContent", glecs, 0);
            
            TacticsDumpPlugin.Log.LogInfo($"[TD] {glecs.Count} GLECs encontrados na aba {possession}");

            foreach (var glec in glecs)
            {
                if (!IsInstructionGlec(glec)) continue;
                ExtractTilesFromGlec(glec, tiles, possession);
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

                string tileName = siTile.childCount > 0 ? siTile[0].name : $"Tile{i}";
                
                if (tileName == "Body") continue;
                if (existingNames.Contains(tileName)) continue;
                existingNames.Add(tileName);

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
            var nameEl = FindByNameClass(siTile, "name", 0, 10);
            if (nameEl != null)
            {
                string txt = TryGetText(nameEl);
                if (!string.IsNullOrEmpty(txt)) return txt;
            }
            
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
            sb.AppendLine($"# TacticsDump Diag v2.1.0 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
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
