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
    [BepInPlugin("com.koda.fm26.playerexport", "FM26 Player Export", "4.0.1")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("[FM26Export v4] Carregado! Atalhos: [F9 ou Ctrl+P] = exportar | [F8] = re-escanear");
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
                bool ctrlP = (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed) && Keyboard.current.pKey.wasPressedThisFrame;
                bool f9 = Keyboard.current.f9Key.wasPressedThisFrame;
                
                if (ctrlP || f9)
                {
                    Plugin.Log.LogInfo($"[FM26Export] Iniciando exportação via atalho: {(f9 ? "F9" : "Ctrl+P")}");
                    StartCapture();
                }
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
            // 1) Tentativa de ler via Tooltip (às vezes a UI esconde o número real e só plota a tooltip)
            string tip = null;
            try { tip = cell.tooltip; } catch {}
            if (!string.IsNullOrEmpty(tip) && double.TryParse(tip, out double _)) 
            {
                return tip;
            }
            
            // 2) Tentativa de buscar nas bindings atreladas via Reflection (m_bindingPath)
            try
            {
                string path = TryGetBindingPath(cell);
                if (!string.IsNullOrEmpty(path))
                {
                    // A propriedade no IL2CPP pode estar associada a "currentAbility" / "potentialAbility" etc
                    // Como não podemos invocar direto sem os tipos exatos, vamos logar apenas se for um binding relevante.
                    if (path.ToLower().Contains("ability")) {
                        // Se pudéssemos rodar Evaluate(), pegaríamos aqui. Por enquanto, caímos no log caso precisemos mapear.
                        Plugin.Log.LogInfo($"[FM26Export] Célula com binding '{path}' detectada no perfil.");
                    }
                }
            } catch {}

            // 3) Fallback Visual (contagem de CSS)
            int filled = 0, half = 0, total = 0;
            CountStars(cell, ref filled, ref half, ref total, 0);
            if (total == 0) return null;
            float val = filled + half * 0.5f;
            if (val <= 0) return string.Empty;
            return val.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace(".", ",");
        }



        private static string TryGetBindingPath(VisualElement el)
        {
            if (el == null) return null;
            try
            {
                var type = el.GetType();
                var prop = type.GetProperty("bindingPath", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (prop != null) { var v = prop.GetValue(el); if (v != null) return v.ToString(); }
                var field = type.GetField("bindingPath", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) { var v = field.GetValue(el); if (v != null) return v.ToString(); }
            } catch { }
            return null;
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
        private static List<string> ReadRow(VisualElement row, bool diag, List<string> headers)
        {
            var vals = new List<string>();
            if (row == null || row.childCount == 0) return vals;
            
            var sel = row.ElementAt(0);
            
            // FIX v3.1 (Ajuste para rodar a tabela do Plantel)
            // Na Base de Dados: sel tem vários filhos (as colunas).
            // No Plantel: sel tem 1 único filho, que é o container wrapper das colunas.
            if (sel.childCount == 1 && sel.ElementAt(0).childCount > 1)
                sel = sel.ElementAt(0);

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

                var pt = FindByName(root, "playertable") ?? FindByName(root, "client-object-viewer-table");
                if (pt == null) { Plugin.Log.LogWarning("[FM26Export] Nenhuma tabela de jogadores/plantel (playertable ou client-object-viewer-table) encontrada na UI."); return; }
                
                _captureView = FindByName(pt, "View");
                if (_captureView == null)
                {
                    Plugin.Log.LogWarning("[FM26Export] View nao encontrada");
                    return;
                }
                
                // Acha os headers
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
                
                var sv = _captureView.GetFirstAncestorOfType<ScrollView>();
                if (sv != null) sv.scrollOffset = Vector2.zero;

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
                    var vals = ReadRow(row, dodiag, _captureHeaders);
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

                var sv = _captureView.GetFirstAncestorOfType<ScrollView>();
                float currentY = sv != null ? sv.scrollOffset.y : 0;
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
                float ph = sv != null && sv.layout.height > 0 ? sv.layout.height : 600f;
                if (sv != null) sv.scrollOffset = new Vector2(0, currentY + ph);
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

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Sports Interactive", "Football Manager 26", "FM26PlayerExport by vinteset");
                string csvDir = Path.Combine(baseDir, "Exports CSV");
                string htmlDir = Path.Combine(baseDir, "Exports HTML");

                Directory.CreateDirectory(csvDir);
                Directory.CreateDirectory(htmlDir);

                // CSV EXPORT
                var csv = new StringBuilder();
                csv.AppendLine(string.Join(";", _captureHeaders));
                foreach (var row in _capturedRows) csv.AppendLine(string.Join(";", row.ConvertAll(Esc)));
                
                string csvFile = Path.Combine(csvDir, $"player_export_{timestamp}.csv");
                File.WriteAllText(csvFile, csv.ToString(), Encoding.UTF8);

                // HTML EXPORT
                var html = new StringBuilder();
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset=\"UTF-8\">");
                html.AppendLine("<style type =\"text/css\">");
                html.AppendLine("body,td,th { font-family: Verdana, Arial, Helvetica, sans-serif; font-size: 12px; }");
                html.AppendLine("th { padding: 5px; text-align: left; background-color: #EEEEEE; border: 1px solid #000000; font-weight: bold; }");
                html.AppendLine("td { padding: 4px; border: 1px solid #000000; }");
                html.AppendLine("table { border-collapse: collapse; width: 98%; margin: 20px auto; }");
                html.AppendLine("tr:nth-child(even) { background-color: #F9F9F9; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                html.AppendLine("<table border=\"1\">");

                // Headers HTML
                html.AppendLine("<tr>");
                foreach (var header in _captureHeaders)
                {
                    html.AppendLine($"\t<th>{header}</th>");
                }
                html.AppendLine("</tr>");

                // Rows HTML
                foreach (var row in _capturedRows)
                {
                    bool isEmpty = true;
                    foreach (var cell in row) if (!string.IsNullOrEmpty(cell)) { isEmpty = false; break; }
                    if (isEmpty) continue;

                    html.AppendLine("<tr>");
                    foreach (var cell in row)
                    {
                        html.AppendLine($"\t<td>{cell}</td>");
                    }
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</table></body></html>");
                string htmlFile = Path.Combine(htmlDir, $"moneyball_export_{timestamp}.html");
                File.WriteAllText(htmlFile, html.ToString(), Encoding.UTF8);

                Plugin.Log.LogInfo($"[FM26Export] ✅ {_capturedRows.Count} jogadores exportados.");
                Plugin.Log.LogInfo($"[FM26Export] CSV salvo em: {csvFile}");
                Plugin.Log.LogInfo($"[FM26Export] HTML salvo em: {htmlFile}");
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

        private static ScrollView BuscarScrollViewRecursivo(VisualElement el)
        {
            if (el == null) return null;
            var sv = el.TryCast<ScrollView>();
            if (sv != null) return sv;
            for (int i = 0; i < el.childCount; i++) { var r = BuscarScrollViewRecursivo(el.ElementAt(i)); if (r != null) return r; }
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
