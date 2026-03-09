using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26PlayerExport
{
    [BepInPlugin("com.koda.fm26.playerexport", "FM26 Player Export", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("[FM26Export] Carregado! Ctrl+P = exportar | F8 = re-escanear");
            AddComponent<ExportBehaviour>();
        }
    }

    public class ExportBehaviour : MonoBehaviour
    {
        private List<UIDocument> _docs = new List<UIDocument>();
        private int _frame = 0;
        private bool _ready = false;

        // Estado da captura por scroll
        private bool _capturing = false;
        private int _captureWait = 0;
        private ScrollView _captureScrollView;
        private VisualElement _captureView;
        private List<string> _captureHeaders;
        private List<List<string>> _capturedRows;
        private HashSet<string> _seenKeys;
        private float _lastScrollY;
        private int _scrollAttempts;
        private const int MAX_SCROLL_ATTEMPTS = 300;
        private const int WAIT_FRAMES = 3;

        public ExportBehaviour(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            _frame++;
            if (!_ready && _frame > 300) { _ready = true; Scan(); }
            if (Keyboard.current == null) return;
            if (Keyboard.current.f8Key.wasPressedThisFrame) Scan();

            if (!_capturing)
            {
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                if (ctrl && Keyboard.current.pKey.wasPressedThisFrame) StartCapture();
            }
            else
            {
                // Aguardar frames para o virtualised-list atualizar após o scroll
                if (_captureWait > 0) { _captureWait--; return; }
                CaptureStep();
            }
        }

        // ── Helpers ──────────────────────────────────────────────

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

        private static string GetText(VisualElement el)
        {
            if (el == null) return null;
            try
            {
                var te = el.TryCast<TextElement>();
                if (te != null)
                {
                    var t = te.text;
                    if (!string.IsNullOrWhiteSpace(t)) return StripHtml(t.Trim());
                }
            }
            catch { }
            return null;
        }

        // Remove tags HTML: <color=#fff>Texto</color> → Texto
        private static string StripHtml(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return Regex.Replace(s, "<[^>]+>", string.Empty).Trim();
        }

        private static string CollectFirstText(VisualElement el, int depth = 0)
        {
            if (el == null || depth > 15) return null;
            var t = GetText(el);
            if (t != null) return t;
            for (int i = 0; i < el.childCount; i++)
            {
                var r = CollectFirstText(el.ElementAt(i), depth + 1);
                if (r != null) return r;
            }
            return null;
        }

        // Lê os valores de uma linha (pula a primeira célula = checkbox)
        private static List<string> ReadRow(VisualElement row)
        {
            var vals = new List<string>();
            if (row == null || row.childCount == 0) return vals;
            var cellSelector = row.ElementAt(0);
            // Começa em 1 para pular a coluna de checkbox
            for (int c = 1; c < cellSelector.childCount; c++)
            {
                var txt = CollectFirstText(cellSelector.ElementAt(c));
                vals.Add(txt ?? string.Empty);
            }
            return vals;
        }

        // ─────────────────────────────────────────────────────────

        private void Scan()
        {
            _docs.Clear();
            var all = FindObjectsOfType<UIDocument>();
            Plugin.Log.LogInfo($"[FM26Export] {all.Length} UIDocuments");
            foreach (var doc in all)
                if (doc.rootVisualElement != null && doc.rootVisualElement.name == "PanelManager-container")
                    _docs.Add(doc);
            Plugin.Log.LogInfo($"[FM26Export] PanelManagers: {_docs.Count}");
        }

        private void StartCapture()
        {
            try
            {
                if (_docs.Count == 0) Scan();
                if (_docs.Count == 0)
                {
                    Plugin.Log.LogError("[FM26Export] Sem UIDocument. Abra a Player Database e aperte F8.");
                    return;
                }

                VisualElement root = null;
                foreach (var doc in _docs)
                    if (doc.rootVisualElement != null) { root = doc.rootVisualElement; break; }
                if (root == null) return;

                var playertable = FindByName(root, "playertable");
                if (playertable == null) { Plugin.Log.LogWarning("[FM26Export] 'playertable' nao encontrado"); return; }

                if (playertable.childCount < 2) return;
                var scrollContainer = playertable.ElementAt(1);
                if (scrollContainer.childCount < 1) return;

                var scrollViewEl = scrollContainer.ElementAt(0);
                _captureScrollView = scrollViewEl.TryCast<ScrollView>();
                if (_captureScrollView == null) { Plugin.Log.LogWarning("[FM26Export] ScrollView nao encontrado"); return; }

                _captureView = FindByName(_captureScrollView, "View");
                if (_captureView == null) { Plugin.Log.LogWarning("[FM26Export] 'View' nao encontrado"); return; }

                // Headers: pular coluna 0 (checkbox)
                _captureHeaders = new List<string>();
                var colHeaders = FindByName(playertable, "column-headers");
                if (colHeaders != null)
                {
                    for (int i = 1; i < colHeaders.childCount; i++) // i=1 pula checkbox
                    {
                        var txt = CollectFirstText(colHeaders.ElementAt(i));
                        _captureHeaders.Add(txt != null ? Esc(txt) : $"Col{i}");
                    }
                }
                if (_captureHeaders.Count == 0) _captureHeaders.Add("Dados");
                Plugin.Log.LogInfo($"[FM26Export] Headers: {string.Join(" | ", _captureHeaders)}");

                // Inicializar estado
                _capturedRows = new List<List<string>>();
                _seenKeys = new HashSet<string>();
                _scrollAttempts = 0;
                _lastScrollY = -1f;

                // Scroll para o topo
                _captureScrollView.scrollOffset = Vector2.zero;
                _captureWait = WAIT_FRAMES;
                _capturing = true;
                Plugin.Log.LogInfo("[FM26Export] Captura iniciada - percorrendo lista...");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[FM26Export] Erro ao iniciar captura: {ex.Message}");
            }
        }

        private void CaptureStep()
        {
            try
            {
                // Capturar linhas selecionadas visíveis agora
                int newThisStep = 0;
                for (int i = 0; i < _captureView.childCount; i++)
                {
                    var row = _captureView.ElementAt(i);
                    bool sel = false;
                    try { sel = row.ClassListContains("virtualised-list__item--selected"); } catch { }
                    if (!sel) continue;

                    var vals = ReadRow(row);
                    if (vals.Count == 0) continue;

                    // Chave única: join das primeiras 3 colunas não vazias
                    string key = string.Join("|", vals.GetRange(0, Math.Min(3, vals.Count)));
                    if (string.IsNullOrEmpty(key) || _seenKeys.Contains(key)) continue;

                    _seenKeys.Add(key);
                    _capturedRows.Add(vals);
                    newThisStep++;
                }

                float currentY = _captureScrollView.scrollOffset.y;
                _scrollAttempts++;

                bool atBottom = Math.Abs(currentY - _lastScrollY) < 0.5f && _lastScrollY >= 0;
                bool limitHit = _scrollAttempts >= MAX_SCROLL_ATTEMPTS;

                Plugin.Log.LogInfo($"[FM26Export] Step {_scrollAttempts}: +{newThisStep} novos, total={_capturedRows.Count}, scrollY={currentY:F0}, atBottom={atBottom}");

                if (atBottom || limitHit)
                {
                    FinishCapture();
                    return;
                }

                // Scroll para baixo uma página
                _lastScrollY = currentY;
                float pageHeight = _captureScrollView.layout.height;
                if (pageHeight <= 0) pageHeight = 600f; // fallback
                _captureScrollView.scrollOffset = new Vector2(0, currentY + pageHeight);
                _captureWait = WAIT_FRAMES;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[FM26Export] Erro no CaptureStep: {ex.Message}");
                _capturing = false;
            }
        }

        private void FinishCapture()
        {
            _capturing = false;
            try
            {
                if (_capturedRows.Count == 0)
                {
                    Plugin.Log.LogWarning("[FM26Export] Nenhum dado capturado.");
                    return;
                }

                var csv = new StringBuilder();
                csv.AppendLine(string.Join(";", _captureHeaders));
                foreach (var row in _capturedRows)
                    csv.AppendLine(string.Join(";", row.ConvertAll(Esc)));

                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Sports Interactive", "Football Manager 2026");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                string file = Path.Combine(path, $"player_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                File.WriteAllText(file, csv.ToString(), Encoding.UTF8);
                Plugin.Log.LogInfo($"[FM26Export] {_capturedRows.Count} jogadores exportados -> {file}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[FM26Export] Erro ao salvar: {ex.Message}");
            }
        }

        private static string Esc(string v)
        {
            if (string.IsNullOrEmpty(v)) return string.Empty;
            v = v.Replace("\r", " ").Replace("\n", " ");
            string q = new string(new char[]{ (char)34 });
            if (v.Contains(";") || v.Contains(q)) v = q + v.Replace(q, q + q) + q;
            return v;
        }
    }
}
