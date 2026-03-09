using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        public ExportBehaviour(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            _frame++;
            if (!_ready && _frame > 300) { _ready = true; Scan(); }
            if (Keyboard.current == null) return;
            if (Keyboard.current.f8Key.wasPressedThisFrame) Scan();
            bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
            if (ctrl && Keyboard.current.pKey.wasPressedThisFrame) Export();
        }

        // ── Helpers (sem Q()/Query<T>()) ──

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

        private static void CollectLabels(VisualElement el, List<string> result, int depth = 0)
        {
            if (el == null || depth > 15) return;
            if (el is Label lbl)
            {
                try
                {
                    var t = lbl.text?.Trim() ?? string.Empty;
                    if (t.Length > 0) result.Add(t);
                }
                catch { }
                return;
            }
            for (int i = 0; i < el.childCount; i++)
                CollectLabels(el.ElementAt(i), result, depth + 1);
        }

        // ─────────────────────────────────

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

        private void Export()
        {
            try
            {
                if (_docs.Count == 0) Scan();
                if (_docs.Count == 0)
                {
                    Plugin.Log.LogError("[FM26Export] Sem UIDocument. Abra a Player Database e aperte F8.");
                    return;
                }

                foreach (var doc in _docs)
                {
                    var root = doc.rootVisualElement;
                    if (root == null) continue;

                    // Caminho confirmado pelo dump:
                    // playertable > child[1] > child[0] > View (virtualised-list__view) > linhas
                    var playertable = FindByName(root, "playertable");
                    if (playertable == null)
                    {
                        Plugin.Log.LogWarning("[FM26Export] 'playertable' nao encontrado");
                        continue;
                    }
                    Plugin.Log.LogInfo($"[FM26Export] 'playertable' encontrado, filhos: {playertable.childCount}");

                    // child[1] = container sem nome com o scroll virtualizado
                    if (playertable.childCount < 2)
                    {
                        Plugin.Log.LogWarning($"[FM26Export] playertable tem apenas {playertable.childCount} filhos");
                        continue;
                    }
                    var scrollContainer = playertable.ElementAt(1);

                    // child[0] do scrollContainer = o scroll view virtualizado
                    if (scrollContainer.childCount < 1) continue;
                    var scrollView = scrollContainer.ElementAt(0);

                    // Dentro do scrollView, procura 'View' (virtualised-list__view)
                    var view = FindByName(scrollView, "View");
                    if (view == null)
                    {
                        Plugin.Log.LogWarning("[FM26Export] 'View' nao encontrado dentro do scroll");
                        continue;
                    }
                    Plugin.Log.LogInfo($"[FM26Export] 'View' encontrado, {view.childCount} linhas visiveis");

                    // Headers: column-headers > panes > Labels
                    var headers = new List<string>();
                    var colHeaders = FindByName(playertable, "column-headers");
                    if (colHeaders != null)
                    {
                        for (int i = 0; i < colHeaders.childCount; i++)
                        {
                            var pane = colHeaders.ElementAt(i);
                            var cellLabels = new List<string>();
                            CollectLabels(pane, cellLabels);
                            headers.Add(cellLabels.Count > 0 ? Esc(cellLabels[0]) : $"Col{i}");
                        }
                        Plugin.Log.LogInfo($"[FM26Export] {headers.Count} colunas no header");
                    }
                    else
                    {
                        Plugin.Log.LogWarning("[FM26Export] 'column-headers' nao encontrado - usando genericos");
                    }
                    if (headers.Count == 0) headers.Add("Dados");

                    // Linhas: filtrar por classe virtualised-list__item--selected
                    var selectedRows = new List<VisualElement>();
                    int totalRows = 0;

                    for (int i = 0; i < view.childCount; i++)
                    {
                        var row = view.ElementAt(i);
                        totalRows++;
                        bool sel = false;
                        try { sel = row.ClassListContains("virtualised-list__item--selected"); } catch { }
                        if (sel) selectedRows.Add(row);
                    }

                    Plugin.Log.LogInfo($"[FM26Export] Linhas visiveis: {totalRows} | Selecionadas: {selectedRows.Count}");

                    if (selectedRows.Count == 0)
                    {
                        Plugin.Log.LogInfo("[FM26Export] Nenhuma selecionada - exportando TODAS visiveis");
                        for (int i = 0; i < view.childCount; i++)
                            selectedRows.Add(view.ElementAt(i));
                    }

                    // Montar CSV
                    // Estrutura da linha: row > child[0] (cell-selector) > N filhos (streamed-table__cell) > Labels
                    var csv = new StringBuilder();
                    csv.AppendLine(string.Join(";", headers));

                    int count = 0;
                    foreach (var row in selectedRows)
                    {
                        try
                        {
                            if (row.childCount == 0) continue;
                            var cellSelector = row.ElementAt(0); // streamed-table-cell-selector
                            var vals = new List<string>();

                            for (int c = 0; c < cellSelector.childCount; c++)
                            {
                                var cell = cellSelector.ElementAt(c); // streamed-table__cell
                                var cellLabels = new List<string>();
                                CollectLabels(cell, cellLabels);
                                vals.Add(cellLabels.Count > 0 ? Esc(cellLabels[0]) : string.Empty);
                            }

                            if (vals.Count > 0)
                            {
                                csv.AppendLine(string.Join(";", vals));
                                count++;
                            }
                        }
                        catch { }
                        if (count >= 10000) break;
                    }

                    string path = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Sports Interactive", "Football Manager 2026");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    string file = Path.Combine(path, $"player_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                    File.WriteAllText(file, csv.ToString(), Encoding.UTF8);
                    Plugin.Log.LogInfo($"[FM26Export] {count} jogadores exportados -> {file}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[FM26Export] ERRO: {ex.Message}\n{ex.StackTrace}");
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
