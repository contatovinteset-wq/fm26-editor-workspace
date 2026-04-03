using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers
{
    public class CalendarExportHandler : IExportHandler
    {
        private VisualElement _calendarRoot;
        private List<string> _headers = new List<string>();
        private List<List<string>> _rows = new List<List<string>>();

        public bool TryStartCapture(VisualElement root, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Tenta encontrar telas comuns de calendário/jogos
            _calendarRoot = UIUtils.FindByName(root, "fixtures_schedule");
            if (_calendarRoot == null)
                _calendarRoot = UIUtils.FindByName(root, "Calendar");
            if (_calendarRoot == null)
                _calendarRoot = UIUtils.FindByName(root, "team_fixtures");
                
            if (_calendarRoot == null)
            {
                errorMessage = "[FM26Export.Calendar] Tela de calendário (fixtures_schedule, team_fixtures ou Calendar) não encontrada.";
                return false;
            }

            var stList = new List<VisualElement>();
            FindAllByName(_calendarRoot, "StreamedTable", stList);

            VisualElement targetSt = null, targetView = null;
            // Pegar a maior/primeira StreamedTable visível
            foreach (var st in stList)
            {
                if (!IsElementVisible(st)) continue;
                var view = UIUtils.FindByName(st, "View");
                if (view != null && view.childCount > 0)
                {
                    targetSt = st; targetView = view; break;
                }
            }
            
            if (targetSt == null || targetView == null)
            {
                errorMessage = "[FM26Export.Calendar] Tabela não encontrada na tela de calendário.";
                return false;
            }

            var ch = UIUtils.FindByName(targetSt, "column-headers");
            var validIndices = new List<int>();
            _headers.Clear();
            if (ch != null)
            {
                for (int i = 0; i < ch.childCount; i++)
                {
                    string txt = UIUtils.CollectFirstText(ch.ElementAt(i));
                    if (string.IsNullOrEmpty(txt)) txt = UIUtils.CollectFirstTooltip(ch.ElementAt(i));
                    if (string.IsNullOrEmpty(txt)) txt = "Col" + i;

                    _headers.Add(UIUtils.Esc(txt.Trim()));
                    validIndices.Add(i);
                }
            }
            else {
                 _headers.Add("Dados"); validIndices.Add(0); 
            }

            _rows.Clear();
            for (int i = 0; i < targetView.childCount; i++)
            {
                var row = targetView.ElementAt(i);
                var vals = new List<string>();

                var sel = row;
                if (sel.childCount == 1 && sel.ElementAt(0).childCount > 1) sel = sel.ElementAt(0);

                for (int c = 0; c < validIndices.Count; c++)
                {
                    int colIdx = validIndices[c];
                    if (colIdx >= sel.childCount) break;
                    
                    var cell = sel.ElementAt(colIdx);
                    string val = UIUtils.CollectAllTextsJoined(cell) ?? "";
                    
                    if (string.IsNullOrEmpty(val)) {
                        string tip = UIUtils.CollectFirstTooltip(cell);
                        if (!string.IsNullOrEmpty(tip)) val = tip;
                    }

                    vals.Add(val.Trim());
                }
                
                bool isEmpty = true;
                foreach (var v in vals) if (!string.IsNullOrWhiteSpace(v.Replace("-",""))) { isEmpty = false; break; }
                if (!isEmpty) _rows.Add(vals);
            }

            return true;
        }

        public bool CaptureStep() { return true; }

        public void FinishCapture()
        {
            if (_rows.Count == 0) return;
            string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Sports Interactive", "Football Manager 26", "FM26PlayerExport by vinteset");
            string htmlDir = Path.Combine(baseDir, "Exports HTML");
            Directory.CreateDirectory(htmlDir);
            
            string timeSuffix = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string htmlFile = Path.Combine(htmlDir, $"calendario_{timeSuffix}.html");
            
            var html = new StringBuilder();
            html.AppendLine("<html><head><meta charset=\"UTF-8\">");
            html.AppendLine("<style type =\"text/css\">body,td,th { font-family: Verdana, Arial, Helvetica, sans-serif; font-size: 12px; } h2 { font-size: 16px; margin-top: 25px; font-weight: bold; } table { border-collapse: collapse; width: 100%; border: 1px solid #000; } th { padding: 5px; background-color: #EEE; border: 1px solid #000; text-align: left; } td { padding: 4px; border: 1px solid #000; } </style>");
            html.AppendLine("</head><body><h2>Resumo: Calendário</h2><br><table><tr>");
            
            foreach (var h in _headers) html.AppendLine($"<th>{h}</th>");
            html.AppendLine("</tr>");
            
            foreach (var r in _rows) {
                html.AppendLine("<tr>");
                foreach (var c in r) html.AppendLine($"<td>{c}</td>");
                html.AppendLine("</tr>");
            }
            html.AppendLine("</table></body></html>");
            File.WriteAllText(htmlFile, html.ToString(), Encoding.UTF8);
            Plugin.Log.LogInfo($"[FM26Export.Calendar] Exportou {_rows.Count} linhas de calendário para {htmlFile}");
        }

        public void Cleanup()
        {
            _calendarRoot = null;
            if (_headers != null) _headers.Clear();
            if (_rows != null) _rows.Clear();
        }

        private bool IsElementVisible(VisualElement el)
        {
            if (el == null) return false;
            try { if (el.resolvedStyle.display == DisplayStyle.None) return false; } catch { }
            if (el.parent != null && el.parent.name != "PanelManager-container") 
                return IsElementVisible(el.parent);
            return true;
        }

        private void FindAllByName(VisualElement root, string name, List<VisualElement> results)
        {
            if (root == null) return;
            if (root.name == name) results.Add(root);
            for (int i = 0; i < root.childCount; i++) FindAllByName(root.ElementAt(i), name, results);
        }
    }
}
