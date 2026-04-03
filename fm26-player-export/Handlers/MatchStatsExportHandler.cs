using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers
{
    public class MatchStatsExportHandler : IExportHandler
    {
        private class ScrapedTab
        {
            public string TabName;
            public List<string> Headers;
            public List<List<string>> Rows;
        }

        private List<ScrapedTab> _accumulatedTabs = new List<ScrapedTab>();
        private string _matchContext = "";

        private VisualElement _matchStatsRoot;
        private int _currentTabIdx = 0;
        private float _nextStepTime = 0f;
        private float _timeoutTime = 0f;
        private string _lastTableHash = "";
        private string[] _tabNames = new string[] { "KeyStatistics", "Passing", "Attacking", "Defending", "Goalkeeping", "SetPieces" };
        private List<VisualElement> _tabElements = new List<VisualElement>();

        private bool IsElementVisible(VisualElement el)
        {
            if (el == null) return false;
            // Using try-catch because layout might not be fully valid
            try {
                if (el.resolvedStyle.display == DisplayStyle.None) return false;
            } catch { }
            if (el.parent != null && el.parent.name != "PanelManager-container") 
                return IsElementVisible(el.parent);
            return true;
        }

        public bool TryStartCapture(VisualElement root, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            _matchStatsRoot = UIUtils.FindByName(root, "MatchStatsStandAlone");
            if (_matchStatsRoot == null) return false;

            _tabElements.Clear();
            var panelRoot = _matchStatsRoot?.panel?.visualTree;
            foreach (var tName in _tabNames)
            {
                var el = UIUtils.FindByName(panelRoot, tName);
                if (el != null) _tabElements.Add(el);
            }

            if (_tabElements.Count < 2)
            {
                errorMessage = "[FM26Export.MatchStats] Abas (KeyStatistics, Passing...) não foram encontradas na memória da UI da Partida.";
                return false;
            }

            _accumulatedTabs.Clear();
            _currentTabIdx = 0;
            _nextStepTime = 0f;
            _lastTableHash = "";
            _contextHome = "";
            _contextAway = "";
            _matchContext = ObterContextoDaPartida();

            Plugin.Log.LogInfo($"[FM26Export.MatchStats] Macro automática iniciada! Abas encontradas: {_tabElements.Count}/6.");

            SafeClick(_tabElements[0]);
            _nextStepTime = Time.unscaledTime + 1.0f; 
            _timeoutTime = Time.unscaledTime + 5.0f;

            return true;
        }

        public bool CaptureStep()
        {
            if (Time.unscaledTime < _nextStepTime)
            {
                return false; 
            }

            if (_currentTabIdx >= _tabElements.Count)
            {
                return true;
            }

            var panelRoot = _matchStatsRoot?.panel?.visualTree;
            var stList = new List<VisualElement>();
            FindAllByName(panelRoot, "StreamedTable", stList);

            VisualElement targetSt = null, targetView = null;
            foreach (var st in stList)
            {
                if (!IsElementVisible(st)) continue;

                var view = UIUtils.FindByName(st, "View");
                if (view != null && view.childCount > 0)
                {
                    targetSt = st; targetView = view; break;
                }
            }

            if (targetSt != null && targetView != null)
            {
                var headers = new List<string>();
                var validIndices = new List<int>();
                var ch = UIUtils.FindByName(targetSt, "column-headers");
                if (ch != null)
                {
                    for (int i = 0; i < ch.childCount; i++)
                    {
                        string tip = UIUtils.CollectFirstTooltip(ch.ElementAt(i));
                        string txt = UIUtils.CollectFirstText(ch.ElementAt(i));
                        string finalTxt = !string.IsNullOrWhiteSpace(tip) ? tip : txt;
                        string rawLower = finalTxt != null ? finalTxt.ToLowerInvariant() : "";
                        
                        if (rawLower.Contains("condi") || rawLower.Contains("cora") || rawLower.Contains("fit") || rawLower.Contains("ção")) continue;

                        string hStr = finalTxt != null ? UIUtils.Esc(finalTxt.Trim()) : $"Col{i}";
                        if (hStr.ToLower().Contains("condi") || hStr.ToLower().Contains("condition")) continue;

                        headers.Add(hStr);
                        validIndices.Add(i);
                    }
                }
                if (headers.Count == 0) {
                    headers.Add("Dados");
                    validIndices.Add(0);
                }

                if (headers.Count > 0)
                {
                    string hStr = string.Join(" ", headers).ToLowerInvariant();
                    if (hStr.Contains("name") || hStr.Contains("time") || hStr.Contains("distance") || hStr.Contains("rating") || hStr.Contains("goals")) UIUtils.GameLang = "en";
                    else if (hStr.Contains("nombre") || hStr.Contains("pases") || hStr.Contains("goles") || hStr.Contains("asistencias") || hStr.Contains("calificación")) UIUtils.GameLang = "es";
                    else UIUtils.GameLang = "pt";
                }

                string myTabName = UIUtils.CollectFirstText(_tabElements[_currentTabIdx]);
                if (string.IsNullOrEmpty(myTabName)) myTabName = "Aba " + _currentTabIdx;

                var tabData = new ScrapedTab
                {
                    TabName = myTabName,
                    Headers = headers,
                    Rows = new List<List<string>>()
                };

                // Add Percentage prefix for Defensive headers
                for (int h = 0; h < tabData.Headers.Count; h++)
                {
                    string hStr = tabData.Headers[h];
                    if (hStr == "Desarmes Conseguidos" || hStr == "Tackles Won" || hStr == "Cabeceamentos Concluídos" || hStr == "Cabeceamentos Concluidos" || hStr == "Headers Won")
                    {
                        if (!hStr.StartsWith("%")) tabData.Headers[h] = "% " + hStr;
                    }
                }

                for (int i = 0; i < targetView.childCount; i++)
                {
                    var row = targetView.ElementAt(i);
                    var vals = ReadMatchRow(row, tabData.Headers, validIndices, i);
                    if (vals.Count > 0) tabData.Rows.Add(vals);
                }

                string currentHash = headers.Count > 0 ? string.Join("|", headers) : "";
                if (tabData.Rows.Count > 0) currentHash += string.Join("|", tabData.Rows[0]);
                
                // Anti-twin logic: Se bateu timeout, exporta assim mesmo e avança
                if (currentHash == _lastTableHash && Time.unscaledTime < _timeoutTime && _currentTabIdx > 0)
                {
                    // Tabela ainda está no 'cache' do render da UI, esperar!
                    _nextStepTime = Time.unscaledTime + 0.3f; // Checa de novo rápido
                    return false;
                }

                _lastTableHash = currentHash;
                _accumulatedTabs.Add(tabData);
                Plugin.Log.LogInfo($"[FM26Export.MatchStats] Lidas {tabData.Rows.Count} linhas de {tabData.TabName} [{_currentTabIdx+1}/{_tabElements.Count}]");
            }
            else
            {
                Plugin.Log.LogWarning($"[FM26Export.MatchStats] StreamedTable visível não encontrada na aba {_tabElements[_currentTabIdx].name}.");
            }

            _currentTabIdx++;
            if (_currentTabIdx < _tabElements.Count)
            {
                SafeClick(_tabElements[_currentTabIdx]);
                _nextStepTime = Time.unscaledTime + 0.8f; // Espera para a próxima aba carregar
                _timeoutTime = Time.unscaledTime + 5.0f; // Timeout resetado para aba nova
                return false; 
            }

            return true; 
        }

        private void SafeClick(VisualElement el)
        {
            if (el == null) return;
            try
            {
                el.Focus();

                var eDown = new Event() { type = EventType.MouseDown };
                var pd = PointerDownEvent.GetPooled(eDown); 
                pd.target = el.Cast<IEventHandler>(); 
                el.SendEvent(pd);
                
                var eUp = new Event() { type = EventType.MouseUp };
                var pu = PointerUpEvent.GetPooled(eUp); 
                pu.target = el.Cast<IEventHandler>(); 
                el.SendEvent(pu);
                
                var sub = NavigationSubmitEvent.GetPooled(); 
                sub.target = el.Cast<IEventHandler>(); 
                el.SendEvent(sub);
            }
            catch (Exception ex) { Plugin.Log.LogError($"[FM26Export] Erro safe_click: {ex.Message}"); }
        }

        public void FinishCapture()
        {
            try
            {
                if (_accumulatedTabs.Count == 0) 
                { 
                    return; 
                }

                string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Sports Interactive", "Football Manager 26", "FM26PlayerExport by vinteset");
                string htmlDir = Path.Combine(baseDir, "Exports HTML");
                Directory.CreateDirectory(htmlDir);

                string safeMatchContext = string.IsNullOrEmpty(_matchContext) ? "partida_dados" : string.Join("_", _matchContext.Split(Path.GetInvalidFileNameChars()));
                string timeSuffix = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string htmlFile = Path.Combine(htmlDir, $"match_stats_{safeMatchContext}_{timeSuffix}.html");

                var html = new StringBuilder();
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset=\"UTF-8\">");
                html.AppendLine("<style type =\"text/css\">");
                html.AppendLine("body,td,th { font-family: Verdana, Arial, Helvetica, sans-serif; font-size: 12px; }");
                html.AppendLine("h2 { font-size: 16px; margin-top: 25px; font-weight: bold; }");
                html.AppendLine("h3 { font-size: 14px; margin-top: 15px; border-bottom: 2px solid #000; padding-bottom: 4px; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 30px; }");
                html.AppendLine("th { padding: 5px; text-align: left; background-color: #EEEEEE; border: 1px solid #000000; font-weight: bold; }");
                html.AppendLine("td { padding: 4px; border: 1px solid #000000; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                
                string displayTitle = string.IsNullOrEmpty(_matchContext) ? "Estatísticas Unificadas" : $"Resumo: {_matchContext}";
                html.AppendLine($"<h2>{displayTitle}</h2>");

                int totalLinhasGlobais = 0;

                foreach (var tab in _accumulatedTabs)
                {
                    if (tab.Rows.Count == 0) continue;

                    html.AppendLine($"<h3>{tab.TabName}</h3>");
                    html.AppendLine("<table>");
                    html.AppendLine("<tr>");
                    foreach (var header in tab.Headers)
                    {
                        html.AppendLine($"\t<th>{header}</th>");
                    }
                    html.AppendLine("</tr>");

                    foreach (var row in tab.Rows)
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
                        totalLinhasGlobais++;
                    }
                    html.AppendLine("</table>");
                }

                html.AppendLine("</body></html>");
                File.WriteAllText(htmlFile, html.ToString(), Encoding.UTF8);

                Plugin.Log.LogInfo($"[FM26Export.MatchStats] Dossiê gravado: ({_accumulatedTabs.Count}/6) abas lidas, {totalLinhasGlobais} linhas exportadas.");
                Plugin.Log.LogInfo($"[FM26Export.MatchStats] Arquivo salvo em: {htmlFile}");
            }
            catch (Exception ex) { Plugin.Log.LogError($"[FM26Export.MatchStats] Erro Export HTML: {ex.Message}"); }
        }

        public void Cleanup()
        {
            _matchStatsRoot = null;
            if (_tabElements != null) _tabElements.Clear();
            if (_accumulatedTabs != null) _accumulatedTabs.Clear();
        }

        private List<string> ReadMatchRow(VisualElement row, List<string> actualHeaders, List<int> validIndices, int rowIdx)
        {
            var vals = new List<string>();
            if (row == null || row.childCount == 0) return vals;
            
            var sel = row;
            if (sel.childCount == 1 && sel.ElementAt(0).childCount > 1) 
            {
                sel = sel.ElementAt(0);
            }

            for (int i = 0; i < validIndices.Count; i++)
            {
                int colIdx = validIndices[i];
                if (colIdx >= sel.childCount) break;

                var cell = sel.ElementAt(colIdx);
                string hName = i < actualHeaders.Count ? actualHeaders[i].ToLower() : "";
                string val = "";
                
                bool isMinColumn = hName == "min" || hName == "min." || hName == "time" || hName.StartsWith("min") || hName.StartsWith("tem") || hName.StartsWith("tie");
                if (isMinColumn)
                {
                    string icns = UIUtils.LerIconesComoTexto(cell);
                    string txts = UIUtils.CollectAllTextsJoined(cell);
                    
                    // Lógica para Consertar o bug do Entra/Sai baseada na linha!
                    // Os primeiros 11 jogadores na tabela do FM (índice 0 a 10) são titulares.
                    // Logo, se sofrerem evento de substituição (sub), necessariamente eles 'Saíram'.
                    // Jogadores do índice 11 pra frente são reservas, se tiverem evento sub, eles 'Entraram'.
                    if (icns.Contains(UIUtils.GetTrans("Sub In")) || icns.Contains(UIUtils.GetTrans("Sub Out")) || icns.Contains("Entra") || icns.Contains("Sai") || icns.Contains("Sub"))
                    {
                        string target = rowIdx < 11 ? UIUtils.GetTrans("Sub Out") : UIUtils.GetTrans("Sub In");
                        
                        if (icns.Contains(UIUtils.GetTrans("Sub In"))) icns = icns.Replace(UIUtils.GetTrans("Sub In"), target);
                        else if (icns.Contains(UIUtils.GetTrans("Sub Out"))) icns = icns.Replace(UIUtils.GetTrans("Sub Out"), target);
                        else if (icns.Contains("Entra")) icns = icns.Replace("Entra", target);
                        else if (icns.Contains("Sai")) icns = icns.Replace("Sai", target);
                        else if (icns.Contains("Sub")) icns = icns.Replace("Sub", target);
                    }

                    string tempVal = "";
                    if (!string.IsNullOrEmpty(txts) && !string.IsNullOrEmpty(icns)) {
                        bool allTxtInIcns = true;
                        foreach (var word in txts.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries)) {
                            if (!icns.Contains(word)) {
                                allTxtInIcns = false; break;
                            }
                        }
                        if (allTxtInIcns) tempVal = icns;
                        else tempVal = txts + " (" + icns + ")";
                    }
                    else if (!string.IsNullOrEmpty(txts)) tempVal = txts;
                    else tempVal = icns;

                    // Remover lixo
                    tempVal = tempVal.Replace("Coração", "").Replace("Fadigado", "").Replace("  ", " ").Trim();
                    val = tempVal;
                }
                else
                {
                    val = UIUtils.CollectAllTextsJoined(cell) ?? string.Empty;
                    if (val.StartsWith("- ") && val.Length > 2) val = val.Substring(2).Trim();
                    if (val.EndsWith(" -") && val.Length > 2) val = val.Substring(0, val.Length - 2).Trim();
                    if (string.IsNullOrEmpty(val) || val == "-")
                    {
                        var stars = UIUtils.TryReadStars(cell);
                        if (stars != null) val = stars;
                    }
                }

                if (!string.IsNullOrEmpty(val) && val != "-" && (hName.Contains("desarmes") || hName.Contains("cabece") || hName.Contains("passe")))
                {
                    if (double.TryParse(val.Replace(",",".").Replace("%",""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
                    {
                        if (!val.Contains("%")) val += "%";
                    }
                }

                vals.Add(val);
                if (vals.Count >= actualHeaders.Count) break;
            }

            int minIdx = actualHeaders.FindIndex(h => 
                {
                    string l = h.ToLower();
                    return l == "min" || l == "min." || l == "time" || l.StartsWith("min") || l.StartsWith("tem") || l.StartsWith("tie");
                });
            if (minIdx >= 0 && minIdx < vals.Count)
            {
                // vals[minIdx] is already correctly loaded by "val = tempVal" logic above
                // If it is completely empty and they played, we can fallback to 90
                bool played = false;
                int distIdx = actualHeaders.FindIndex(h => h.ToLower().StartsWith("dist"));
                if (distIdx >= 0 && distIdx < vals.Count && !string.IsNullOrEmpty(vals[distIdx]))
                {
                    if (vals[distIdx] != "0,0 km" && vals[distIdx] != "0 km" && vals[distIdx] != "-") played = true;
                }
                else 
                {
                    int passIdx = actualHeaders.FindIndex(h => h.ToLower().Contains("pass"));
                    if (passIdx >= 0 && passIdx < vals.Count && !string.IsNullOrEmpty(vals[passIdx]))
                    {
                        if (vals[passIdx] != "0" && vals[passIdx] != "-") played = true;
                    }
                }

                if (played && string.IsNullOrEmpty(vals[minIdx].Replace("-", "").Trim()))
                {
                    vals[minIdx] = "90";
                }
                else if (vals[minIdx] == "-")
                {
                    vals[minIdx] = "";
                }
            }

            return vals;
        }

        private void FindAllByName(VisualElement root, string name, List<VisualElement> results)
        {
            if (root == null) return;
            if (root.name == name) results.Add(root);
            for (int i = 0; i < root.childCount; i++)
            {
                FindAllByName(root.ElementAt(i), name, results);
            }
        }

        private string _contextHome = "";
        private string _contextAway = "";

        private string ObterContextoDaPartida()
        {
            if (_matchStatsRoot == null) return "Partida";
            if (!string.IsNullOrEmpty(_contextHome) && !string.IsNullOrEmpty(_contextAway))
                return $"{_contextHome} vs {_contextAway}";

            string home = "Casa";
            string away = "Fora";
            string placar = "";

            try 
            {
                // Prioridade 1: screen_title (tem os nomes completos)
                var titleBar = UIUtils.FindByName(_matchStatsRoot?.panel?.visualTree, "screen_title");
                if (titleBar != null) {
                    string tTitle = UIUtils.CollectAllTextsJoined(titleBar);
                    if (!string.IsNullOrEmpty(tTitle)) {
                        string[] sep = new string[]{" vs ", " x ", " - "};
                        foreach(var s in sep) {
                            if (tTitle.Contains(s)) {
                                var splits = tTitle.Split(new string[]{s}, System.StringSplitOptions.None);
                                if (splits.Length == 2) {
                                    home = splits[0].Trim();
                                    away = splits[1].Trim();
                                    if (home.Contains(":")) home = home.Substring(home.IndexOf(":")+1).Trim();
                                    break;
                                }
                            }
                        }
                    }
                }

                // Prioridade 2: Fallback para as Badges (se screen_title falhar)
                if (home == "Casa" || home == "Estatísticas da Partida") {
                    var hb = UIUtils.FindByName(_matchStatsRoot, "HomeTeamBadge");
                    if (hb != null) {
                        string ht = UIUtils.CollectFirstTooltip(hb);
                        if (string.IsNullOrEmpty(ht)) ht = UIUtils.CollectAllTextsJoined(hb);
                        if (!string.IsNullOrEmpty(ht)) home = ht.Trim();
                    }
                }

                if (away == "Fora" || string.IsNullOrEmpty(away)) {
                    var ab = UIUtils.FindByName(_matchStatsRoot, "AwayTeamBadge");
                    if (ab != null) {
                        string at = UIUtils.CollectFirstTooltip(ab);
                        if (string.IsNullOrEmpty(at)) at = UIUtils.CollectAllTextsJoined(ab);
                        if (!string.IsNullOrEmpty(at)) away = at.Trim();
                    }
                }

                // Extrair Placar
                var frm = UIUtils.FindByName(_matchStatsRoot, "Teams frame");
                if (frm != null)
                {
                    var allTexts = new List<string>();
                    UIUtils.CollectAllTexts(frm, allTexts);
                    string joined = string.Join(" ", allTexts).Replace("  ", " ").Trim();
                    Plugin.Log.LogInfo($"[FM26Export] Teams frame raw text: '{joined}'");

                    var simpleMatch = System.Text.RegularExpressions.Regex.Match(joined, @"(\d+)\s*[-xX]\s*(\d+)");
                    if (simpleMatch.Success) placar = $" {simpleMatch.Groups[1].Value}x{simpleMatch.Groups[2].Value}";
                }

            } catch (Exception e) { Plugin.Log.LogError("Erro parser contexto: " + e.Message); }

            _contextHome = home;
            _contextAway = away;

            return $"{home}{placar} vs {away}";
        }

        /* unused: private string TraduzirAba(string unityName) ... */
    }
}
