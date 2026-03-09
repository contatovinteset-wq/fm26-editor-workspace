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
        // ── Configurações de performance ──────────────────────
        private const int WAIT_FRAMES    = 4;    // frames aguardados após cada scroll
        private const int MAX_SCROLL     = 500;  // segurança contra loop infinito
        private const int MAX_ROWS       = 5000; // limite de linhas por export
        private const int ZERO_STEPS_MAX = 3;    // passos consecutivos sem captura antes de parar

        private List<UIDocument> _docs    = new List<UIDocument>();
        private int  _frame   = 0;
        private bool _ready   = false;
        private bool _capturing = false;
        private int  _captureWait = 0;
        private ScrollView    _captureScrollView;
        private VisualElement _captureView;
        private List<string>  _captureHeaders;
        private List<List<string>> _capturedRows;
        private HashSet<string>    _seenKeys;
        private float _lastScrollY;
        private int   _scrollAttempts;
        private int   _zeroSteps;
        private bool  _diagLogged;

        public ExportBehaviour(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            _frame++;
            if (!_ready && _frame > 300) { _ready = true; Scan(); }
            if (Keyboard.current == null) return;
            if (Keyboard.current.f8Key.wasPressedThisFrame) { Scan(); _diagLogged = false; }
            if (!_capturing)
            {
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                if (ctrl && Keyboard.current.pKey.wasPressedThisFrame) StartCapture();
            }
            else
            {
                if (_captureWait > 0) { _captureWait--; return; }
                CaptureStep();
            }
        }

        // ── Leitura de texto ──────────────────────────────────

        private static string GetText(VisualElement el)
        {
            if (el == null) return null;
            try { var te = el.TryCast<TextElement>(); if (te != null && !string.IsNullOrWhiteSpace(te.text)) return StripHtml(te.text.Trim()); } catch { }
            try { var lb = el.TryCast<Label>();       if (lb != null && !string.IsNullOrWhiteSpace(lb.text)) return StripHtml(lb.text.Trim()); } catch { }
            try { var tip = el.tooltip;               if (!string.IsNullOrWhiteSpace(tip)) return StripHtml(tip.Trim()); } catch { }
            return null;
        }

        private static string StripHtml(string s)
            => string.IsNullOrEmpty(s) ? s : Regex.Replace(s, "<[^>]+>", string.Empty).Trim();

        private static string CollectFirstText(VisualElement el, int d = 0)
        {
            if (el == null || d > 20) return null;
            var t = GetText(el);
            if (t != null) return t;
            for (int i = 0; i < el.childCount; i++) { var r = CollectFirstText(el.ElementAt(i), d+1); if (r != null) return r; }
            return null;
        }

        private static void CollectAllTexts(VisualElement el, List<string> out_, int d = 0)
        {
            if (el == null || d > 20) return;
            var t = GetText(el); if (t != null) out_.Add(t);
            for (int i = 0; i < el.childCount; i++) CollectAllTexts(el.ElementAt(i), out_, d+1);
        }

        // ── Estrelas ─────────────────────────────────────────
        // Conta estrelas preenchidas/metade/vazias pelo nome das classes CSS
        private static string TryReadStars(VisualElement cell)
        {
            int filled = 0, half = 0, total = 0;
            CountStars(cell, ref filled, ref half, ref total, 0);
            if (total == 0) return null;
            float val = filled + half * 0.5f;
            // Retorna vazio se todas as estrelas estão vazias (sem rating atribuído)
            if (val <= 0) return string.Empty;
            return val.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace(".", ",");
        }

        private static void CountStars(VisualElement el, ref int filled, ref int half, ref int total, int d)
        {
            if (el == null || d > 12) return;
            try
            {
                bool isStar = false, isFilled = false, isHalf = false;
                for (int c = 0; c < el.classList.Count; c++)
                {
                    string cls = el.classList[c].ToLower();
                    if (cls.Contains("star") || cls.Contains("ability") || cls.Contains("rating")) isStar = true;
                    if (cls.Contains("filled") || cls.Contains("active") || cls.Contains("full") || cls.Contains("on")) isFilled = true;
                    if (cls.Contains("half")) isHalf = true;
                }
                if (isStar && el.childCount == 0) // folha = 1 estrela
                {
                    total++;
                    if (isHalf)   half++;
                    else if (isFilled) filled++;
                }
            }
            catch { }
            for (int i = 0; i < el.childCount; i++) CountStars(el.ElementAt(i), ref filled, ref half, ref total, d+1);
        }

        // ── Diagnóstico ───────────────────────────────────────
        private static string DiagCell(VisualElement el, int d = 0)
        {
            if (el == null || d > 6) return string.Empty;
            var clsSb = new StringBuilder();
            try { for (int c = 0; c < el.classList.Count; c++) { if (c>0) clsSb.Append(','); clsSb.Append(el.classList[c]); } } catch { }
            string t = GetText(el) ?? string.Empty;
            var sb = new StringBuilder();
            sb.Append($"{new string('-',d)}{el.GetType().Name}[cls={clsSb},ch={el.childCount},txt={t}] ");
            for (int i = 0; i < el.childCount; i++) sb.Append(DiagCell(el.ElementAt(i), d+1));
            return sb.ToString();
        }

        // ── Leitura de linha ──────────────────────────────────
        private static List<string> ReadRow(VisualElement row, bool diag)
        {
            var vals = new List<string>();
            if (row == null || row.childCount == 0) return vals;
            var sel = row.ElementAt(0);
            for (int c = 1; c < sel.childCount; c++) // c=1 pula checkbox
            {
                var cell = sel.ElementAt(c);
                string val;

                if (c == 1) // célula do jogador: pega texto mais longo
                {
                    var txts = new List<string>();
                    CollectAllTexts(cell, txts);
                    if (diag) Plugin.Log.LogInfo($"[FM26Export] Cel[1] DIAG: {DiagCell(cell)}");
                    val = string.Empty;
                    foreach (var tx in txts) if (tx.Length > val.Length) val = tx;
                }
                else
                {
                    val = CollectFirstText(cell) ?? string.Empty;
                    // Se vazio, tenta ler como estrelas
                    if (string.IsNullOrEmpty(val))
                    {
                        var stars = TryReadStars(cell);
                        if (stars != null) val = stars;
                    }
                }
                vals.Add(val);
            }
            return vals;
        }

        // ── Chave de deduplicação (hash do conteúdo completo da linha) ──
        private static string RowKey(List<string> vals)
        {
            if (vals == null || vals.Count == 0) return string.Empty;
            // Usa todas as colunas para evitar falsos positivos em jogadores com mesmo nome/clube
            return string.Join("|", vals);
        }

        // ── Scan / Capture ────────────────────────────────────

        private void Scan()
        {
            _docs.Clear();
            var all = FindObjectsOfType<UIDocument>();
            Plugin.Log.LogInfo($"[FM26Export] {all.Length} UIDocuments");
            foreach (var doc in all)
                if (doc.rootVisualElement?.name == "PanelManager-container")
                    _docs.Add(doc);
            Plugin.Log.LogInfo($"[FM26Export] PanelManagers: {_docs.Count}");
        }

        private void StartCapture()
        {
            try
            {
                if (_docs.Count == 0) Scan();
                if (_docs.Count == 0) { Plugin.Log.LogError("[FM26Export] Sem UIDocument."); return; }

                VisualElement root = null;
                foreach (var doc in _docs) if (doc.rootVisualElement != null) { root = doc.rootVisualElement; break; }
                if (root == null) return;

                var pt = FindByName(root, "playertable");
                if (pt == null) { Plugin.Log.LogWarning("[FM26Export] playertable nao encontrado"); return; }
                if (pt.childCount < 2) return;
                var svEl = pt.ElementAt(1).childCount > 0 ? pt.ElementAt(1).ElementAt(0) : null;
                if (svEl == null) return;

                _captureScrollView = svEl.TryCast<ScrollView>();
                if (_captureScrollView == null) { Plugin.Log.LogWarning("[FM26Export] ScrollView nao encontrado"); return; }

                _captureView = FindByName(_captureScrollView, "View");
                if (_captureView == null) { Plugin.Log.LogWarning("[FM26Export] View nao encontrada"); return; }

                _captureHeaders = new List<string>();
                var ch = FindByName(pt, "column-headers");
                if (ch != null)
                    for (int i = 1; i < ch.childCount; i++)
                    {
                        var txt = CollectFirstText(ch.ElementAt(i));
                        _captureHeaders.Add(txt != null ? Esc(txt) : $"Col{i}");
                    }
                if (_captureHeaders.Count == 0) _captureHeaders.Add("Dados");
                Plugin.Log.LogInfo($"[FM26Export] Headers ({_captureHeaders.Count}): {string.Join(" | ", _captureHeaders)}");

                _capturedRows   = new List<List<string>>();
                _seenKeys       = new HashSet<string>();
                _scrollAttempts = 0;
                _zeroSteps      = 0;
                _lastScrollY    = -1f;
                _diagLogged     = false;
                _captureScrollView.scrollOffset = Vector2.zero;
                _captureWait = WAIT_FRAMES;
                _capturing = true;
                Plugin.Log.LogInfo("[FM26Export] Captura iniciada...");
            }
            catch (Exception ex) { Plugin.Log.LogError($"[FM26Export] Erro StartCapture: {ex.Message}"); }
        }

        private void CaptureStep()
        {
            try
            {
                int newCount = 0;
                for (int i = 0; i < _captureView.childCount; i++)
                {
                    var row = _captureView.ElementAt(i);
                    bool sel = false;
                    try { sel = row.ClassListContains("virtualised-list__item--selected"); } catch { }
                    if (!sel) continue;

                    bool dodiag = !_diagLogged && _scrollAttempts == 0 && newCount == 0;
                    var vals = ReadRow(row, dodiag);
                    if (dodiag) _diagLogged = true;
                    if (vals.Count == 0) continue;

                    string key = RowKey(vals);
                    if (string.IsNullOrEmpty(key) || _seenKeys.Contains(key)) continue;
                    _seenKeys.Add(key);
                    _capturedRows.Add(vals);
                    newCount++;

                    if (_capturedRows.Count >= MAX_ROWS)
                    {
                        Plugin.Log.LogWarning($"[FM26Export] Limite de {MAX_ROWS} linhas atingido — finalizando.");
                        FinishCapture(); return;
                    }
                }

                float currentY = _captureScrollView.scrollOffset.y;
                _scrollAttempts++;
                bool atBottom = Math.Abs(currentY - _lastScrollY) < 0.5f && _lastScrollY >= 0;

                // Contador de passos sem novidades (proteção contra loop)
                if (newCount == 0) _zeroSteps++; else _zeroSteps = 0;
                bool stalled = _zeroSteps >= ZERO_STEPS_MAX;

                Plugin.Log.LogInfo($"[FM26Export] Step {_scrollAttempts}: +{newCount} | total={_capturedRows.Count} | scrollY={currentY:F0} | fim={atBottom} | stall={_zeroSteps}/{ZERO_STEPS_MAX}");

                if (atBottom || _scrollAttempts >= MAX_SCROLL || stalled)
                {
                    if (stalled && !atBottom) Plugin.Log.LogWarning("[FM26Export] Parado por falta de novos dados.");
                    FinishCapture(); return;
                }

                _lastScrollY = currentY;
                float ph = _captureScrollView.layout.height;
                if (ph <= 0) ph = 600f;
                _captureScrollView.scrollOffset = new Vector2(0, currentY + ph);
                _captureWait = WAIT_FRAMES;
            }
            catch (Exception ex) { Plugin.Log.LogError($"[FM26Export] Erro CaptureStep: {ex.Message}"); _capturing = false; }
        }

        private void FinishCapture()
        {
            _capturing = false;
            try
            {
                if (_capturedRows.Count == 0) { Plugin.Log.LogWarning("[FM26Export] Nenhum dado."); return; }
                var csv = new StringBuilder();
                csv.AppendLine(string.Join(";", _captureHeaders));
                foreach (var row in _capturedRows) csv.AppendLine(string.Join(";", row.ConvertAll(Esc)));
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Sports Interactive", "Football Manager 2026");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                string file = Path.Combine(path, $"player_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                File.WriteAllText(file, csv.ToString(), Encoding.UTF8);
                Plugin.Log.LogInfo($"[FM26Export] ✅ {_capturedRows.Count} jogadores exportados -> {file}");
            }
            catch (Exception ex) { Plugin.Log.LogError($"[FM26Export] Erro FinishCapture: {ex.Message}"); }
        }

        // ── Utilitários ───────────────────────────────────────

        private static VisualElement FindByName(VisualElement el, string name)
        {
            if (el == null) return null;
            if (el.name == name) return el;
            for (int i = 0; i < el.childCount; i++) { var r = FindByName(el.ElementAt(i), name); if (r != null) return r; }
            return null;
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
