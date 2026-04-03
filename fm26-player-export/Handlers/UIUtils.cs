using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers
{
    public static class UIUtils
    {
        public static string GameLang = "pt";

        public static string GetTrans(string key)
        {
            if (GameLang == "en") {
                if (key == "Amarelo") return "Yellow";
                if (key == "Vermelho") return "Red";
                if (key == "Sub In") return "Sub In";
                if (key == "Sub Out") return "Sub Out";
                if (key == "Lesão") return "Injured";
            } else if (GameLang == "es") {
                if (key == "Amarelo") return "Amarilla";
                if (key == "Vermelho") return "Roja";
                if (key == "Sub In") return "Entra";
                if (key == "Sub Out") return "Sale";
                if (key == "Lesão") return "Lesión";
            } else { // default as PT or fallback
                if (key == "Sub In") return "Entra";
                if (key == "Sub Out") return "Sai";
            }
            return key;
        }

        public static string GetText(VisualElement el)
        {
            if (el == null) return null;
            try { var te = el.TryCast<TextElement>(); if (te != null && !string.IsNullOrWhiteSpace(te.text)) return StripHtml(te.text.Trim()); } catch { }
            try { var lb = el.TryCast<Label>();       if (lb != null && !string.IsNullOrWhiteSpace(lb.text)) return StripHtml(lb.text.Trim()); } catch { }
            try { var tip = el.tooltip;               if (!string.IsNullOrWhiteSpace(tip)) return StripHtml(tip.Trim()); } catch { }
            return null;
        }

        public static string StripHtml(string s)
            => string.IsNullOrEmpty(s) ? s : Regex.Replace(s, "<[^>]+>", string.Empty).Trim();

        public static string CollectFirstText(VisualElement el, int d = 0)
        {
            if (el == null || d > 20) return null;
            var t = GetText(el);
            if (t != null) return t;
            for (int i = 0; i < el.childCount; i++) { var r = CollectFirstText(el.ElementAt(i), d+1); if (r != null) return r; }
            return null;
        }
        
        public static string CollectFirstTooltip(VisualElement el, int d = 0)
        {
            if (el == null || d > 20) return null;
            try { var tip = el.tooltip; if (!string.IsNullOrWhiteSpace(tip)) return StripHtml(tip.Trim()); } catch { }
            for (int i = 0; i < el.childCount; i++) { var r = CollectFirstTooltip(el.ElementAt(i), d+1); if (r != null) return r; }
            return null;
        }

        public static string CollectAllTextsJoined(VisualElement el, int d = 0)
        {
            if (el == null || d > 20) return "";
            var list = new List<string>();
            CollectAllTexts(el, list, 0);
            return string.Join(" ", list).Trim();
        }

        public static void CollectAllTexts(VisualElement el, List<string> out_, int d = 0)
        {
            if (el == null || d > 20) return;
            var t = GetText(el);
            if (t != null && !out_.Contains(t)) out_.Add(t);
            try { var tip = el.tooltip; if (!string.IsNullOrWhiteSpace(tip)) { var ts = StripHtml(tip.Trim()); if (!out_.Contains(ts)) out_.Add(ts); } } catch { }
            for (int i = 0; i < el.childCount; i++) CollectAllTexts(el.ElementAt(i), out_, d+1);
        }



        public static string TryReadStars(VisualElement cell)
        {
            try { string tip = cell.tooltip; if (!string.IsNullOrEmpty(tip) && double.TryParse(tip, out double _)) return tip; } catch {}
            int filled = 0, half = 0, total = 0;
            CountStars(cell, ref filled, ref half, ref total, 0);
            if (total == 0) return null;
            float val = filled + half * 0.5f;
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
                if (isStar && el.childCount == 0)
                {
                    total++;
                    if (isHalf) half++;
                    else if (isFilled) filled++;
                }
            } catch { }
            for (int i = 0; i < el.childCount; i++) CountStars(el.ElementAt(i), ref filled, ref half, ref total, d+1);
        }

        public static string RowKey(List<string> vals)
        {
            if (vals == null || vals.Count == 0) return string.Empty;
            return string.Join("|", vals);
        }

        public static VisualElement FindByName(VisualElement el, string name)
        {
            if (el == null) return null;
            if (el.name == name) return el;
            for (int i = 0; i < el.childCount; i++) { var r = FindByName(el.ElementAt(i), name); if (r != null) return r; }
            return null;
        }

        public static string Esc(string v)
        {
            if (string.IsNullOrEmpty(v)) return string.Empty;
            v = v.Replace("\r", " ").Replace("\n", " ");
            string q = new string(new char[]{ (char)34 });
            if (v.Contains(";") || v.Contains(q)) v = q + v.Replace(q, q + q) + q;
            return v;
        }

        public static string LerIconesComoTexto(VisualElement el, int d = 0)
        {
            if (el == null || d > 6) return "";
            var res = new List<string>();
            try {
                for (int c = 0; c < el.classList.Count; c++)
                {
                    string cls = el.classList[c].ToLower();
                    if (cls.Contains("yellow")) res.Add(GetTrans("Amarelo"));
                    if (cls.Contains("red")) res.Add(GetTrans("Vermelho"));
                    if (cls.Contains("sub") && !cls.Contains("subject")) {
                        if (cls.Contains("on") || cls.Contains("in")) res.Add(GetTrans("Sub In"));
                        else if (cls.Contains("off") || cls.Contains("out")) res.Add(GetTrans("Sub Out"));
                        else res.Add("Sub");
                    }
                    if (cls.Contains("injur")) res.Add(GetTrans("Lesão"));
                    if (cls.Contains("condition") || cls.Contains("heart") || cls.Contains("sharpness")) res.Add("Coração");
                    if (cls.Contains("fatigue") || cls.Contains("tired")) res.Add("Fadigado");
                }
                string tip = el.tooltip;
                if (!string.IsNullOrWhiteSpace(tip) && res.Count > 0) {
                     string t = StripHtml(tip);
                     if (t.Length < 40) {
                         // Evita sobrescrever uma substituição validada com um tooltip genérico ("Entra")
                         string lastRes = res[res.Count - 1];
                         if (lastRes != GetTrans("Sub In") && lastRes != GetTrans("Sub Out")) 
                         {
                             res[res.Count - 1] = t;
                         }
                     }
                }
            } catch {}
            for(int i=0; i<el.childCount;i++){
                string sub = LerIconesComoTexto(el.ElementAt(i), d+1);
                if (!string.IsNullOrEmpty(sub)) {
                    foreach(var s in sub.Split(new string[]{" | "}, System.StringSplitOptions.RemoveEmptyEntries)) 
                        if (!res.Contains(s)) res.Add(s);
                }
            }
            return string.Join(" | ", res);
        }

        public static string DiagCell(VisualElement el, int d = 0)
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
    }
}
