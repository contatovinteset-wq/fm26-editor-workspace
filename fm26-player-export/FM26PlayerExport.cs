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
            Log.LogInfo("[FM26Export] Plugin carregado!");
            Log.LogInfo("[FM26Export] F8 = Re-escanear UIDocuments");
            Log.LogInfo("[FM26Export] Ctrl+P = Exportar jogadores para CSV");
            
            AddComponent<ExportBehaviour>();
        }
    }
    
    public class ExportBehaviour : MonoBehaviour
    {
        private List<UIDocument> _uiDocuments = new List<UIDocument>();
        private bool _initialized = false;
        private int _frameCount = 0;
        
        public ExportBehaviour(IntPtr ptr) : base(ptr) { }
        
        private void Start()
        {
            Plugin.Log.LogInfo("[FM26Export] Behaviour iniciado");
        }
        
        private void Update()
        {
            _frameCount++;
            
            if (!_initialized && _frameCount > 300)
            {
                _initialized = true;
                ScanUIDocuments();
            }
            
            if (Keyboard.current == null) return;
            
            if (Keyboard.current.f8Key.wasPressedThisFrame)
            {
                Plugin.Log.LogInfo("[FM26Export] >>> F8 - Re-escaneando UIDocuments...");
                ScanUIDocuments();
            }
            
            bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
            bool p = Keyboard.current.pKey.wasPressedThisFrame;
            
            if (ctrl && p)
            {
                Plugin.Log.LogInfo("[FM26Export] >>> Ctrl+P - Iniciando exportação...");
                ExportPlayers();
            }
        }
        
        private void ScanUIDocuments()
        {
            _uiDocuments.Clear();
            
            var allDocs = FindObjectsOfType<UIDocument>();
            Plugin.Log.LogInfo($"[FM26Export] Encontrados {allDocs.Length} UIDocuments");
            
            foreach (var doc in allDocs)
            {
                if (doc.rootVisualElement != null)
                {
                    var rootName = doc.rootVisualElement.name;
                    Plugin.Log.LogInfo($"[FM26Export]   - {rootName}");
                    
                    if (rootName == "PanelManager-container")
                    {
                        _uiDocuments.Add(doc);
                        Plugin.Log.LogInfo($"[FM26Export]   ✓ PanelManager encontrado!");
                    }
                }
            }
            
            Plugin.Log.LogInfo($"[FM26Export] Total PanelManager documents: {_uiDocuments.Count}");
        }
        
        // Helper para evitar ambiguidade IL2CPP
        private VisualElement QSafe(VisualElement parent, string name)
        {
            return parent.Q(name, (string)null);
        }
        
        private UQueryBuilder<T> QuerySafe<T>(VisualElement parent) where T : VisualElement
        {
            return parent.Query<T>(null, (string)null);
        }
        
        private void ExportPlayers()
        {
            try
            {
                if (_uiDocuments.Count == 0)
                {
                    Plugin.Log.LogWarning("[FM26Export] Nenhum UIDocument escaneado. Aperte F8 primeiro.");
                    ScanUIDocuments();
                    
                    if (_uiDocuments.Count == 0)
                    {
                        Plugin.Log.LogError("[FM26Export] Ainda sem UIDocuments. Abra a tela de jogadores primeiro.");
                        return;
                    }
                }
                
                foreach (var doc in _uiDocuments)
                {
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    Plugin.Log.LogInfo($"[FM26Export] Processando root: {root.name}");
                    
                    // PASSO 1: Localizar tabela
                    var tables = QSafe(root, "tables");
                    if (tables == null)
                    {
                        Plugin.Log.LogWarning("[FM26Export] 'tables' não encontrado");
                        continue;
                    }
                    Plugin.Log.LogInfo($"[FM26Export] ✓ 'tables' encontrado");
                    
                    var tableContainer = QSafe(tables, "search-table-remapper");
                    if (tableContainer == null)
                    {
                        Plugin.Log.LogWarning("[FM26Export] 'search-table-remapper' não encontrado");
                        
                        Plugin.Log.LogInfo("[FM26Export] Filhos de 'tables':");
                        int childCount = 0;
                        foreach (var child in tables.Children())
                        {
                            childCount++;
                            Plugin.Log.LogInfo($"[FM26Export]   [{childCount}] {child.name} (type: {child.GetType().Name})");
                            if (childCount >= 20) break;
                        }
                        continue;
                    }
                    
                    Plugin.Log.LogInfo($"[FM26Export] ✓ 'search-table-remapper' encontrado");
                    
                    // Contar filhos
                    int rowCount = 0;
                    foreach (var _ in tableContainer.Children())
                        rowCount++;
                    
                    Plugin.Log.LogInfo($"[FM26Export] 'search-table-remapper' tem {rowCount} filhos");
                    
                    // PASSO 2: Ler linhas
                    var selectedRows = new List<VisualElement>();
                    int toggleSelected = 0;
                    int classSelected = 0;
                    
                    foreach (var row in tableContainer.Children())
                    {
                        bool isSelected = false;
                        
                        try
                        {
                            var toggle = QSafe(row, "") as Toggle;
                            if (toggle == null)
                            {
                                var toggles = QuerySafe<Toggle>(row).Build().ToList();
                                if (toggles.Count > 0) toggle = toggles[0];
                            }
                            if (toggle != null && toggle.value)
                            {
                                isSelected = true;
                                toggleSelected++;
                            }
                        }
                        catch { }
                        
                        if (!isSelected)
                        {
                            try
                            {
                                if (row.ClassListContains("selected") || row.ClassListContains("checked"))
                                {
                                    isSelected = true;
                                    classSelected++;
                                }
                            }
                            catch { }
                        }
                        
                        if (isSelected)
                        {
                            selectedRows.Add(row);
                        }
                    }
                    
                    Plugin.Log.LogInfo($"[FM26Export] Linhas com Toggle marcado: {toggleSelected}");
                    Plugin.Log.LogInfo($"[FM26Export] Linhas com classe 'selected': {classSelected}");
                    
                    if (selectedRows.Count == 0)
                    {
                        Plugin.Log.LogInfo("[FM26Export] Nenhuma linha selecionada - exportando TODAS");
                        foreach (var row in tableContainer.Children())
                        {
                            selectedRows.Add(row);
                        }
                    }
                    
                    // PASSO 3: Ler cabeçalho
                    var headers = new List<string>();
                    try
                    {
                        var headerSection = QSafe(root, "PersonSearchTableTopSection");
                        if (headerSection != null)
                        {
                            Plugin.Log.LogInfo("[FM26Export] ✓ 'PersonSearchTableTopSection' encontrado");
                            
                            var headerLabels = QuerySafe<Label>(headerSection).Build().ToList();
                            Plugin.Log.LogInfo($"[FM26Export] {headerLabels.Count} labels no header");
                            
                            foreach (var label in headerLabels)
                            {
                                var text = label.text?.Trim() ?? "";
                                if (!string.IsNullOrEmpty(text))
                                {
                                    headers.Add(EscapeCSV(text));
                                }
                            }
                        }
                        else
                        {
                            Plugin.Log.LogWarning("[FM26Export] 'PersonSearchTableTopSection' não encontrado");
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogError($"[FM26Export] Erro ao ler header: {ex.Message}");
                    }
                    
                    if (headers.Count == 0)
                    {
                        headers.Add("Dados");
                    }
                    
                    // PASSO 4: Montar CSV
                    var csv = new StringBuilder();
                    csv.AppendLine(string.Join(";", headers));
                    
                    int exportedCount = 0;
                    foreach (var row in selectedRows)
                    {
                        try
                        {
                            var values = new List<string>();
                            
                            var labels = QuerySafe<Label>(row).Build().ToList();
                            foreach (var label in labels)
                            {
                                var text = label.text?.Trim() ?? "";
                                if (!string.IsNullOrEmpty(text))
                                {
                                    values.Add(EscapeCSV(text));
                                }
                            }
                            
                            if (values.Count > 0)
                            {
                                csv.AppendLine(string.Join(";", values));
                                exportedCount++;
                            }
                        }
                        catch { }
                        
                        if (exportedCount >= 10000) break;
                    }
                    
                    // PASSO 5: Salvar CSV
                    string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string fm26Path = Path.Combine(docsPath, "Sports Interactive", "Football Manager 2026");
                    
                    if (!Directory.Exists(fm26Path))
                    {
                        Directory.CreateDirectory(fm26Path);
                    }
                    
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string filename = $"player_export_{timestamp}.csv";
                    string fullPath = Path.Combine(fm26Path, filename);
                    
                    File.WriteAllText(fullPath, csv.ToString(), Encoding.UTF8);
                    
                    Plugin.Log.LogInfo($"[FM26Export] ✅ Exportado {exportedCount} jogadores");
                    Plugin.Log.LogInfo($"[FM26Export] 📁 Arquivo: {fullPath}");
                    
                    return;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[FM26Export] ERRO: {ex.Message}");
                Plugin.Log.LogError($"[FM26Export] Stack: {ex.StackTrace}");
            }
        }
        
        private string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            
            value = value.Replace("\r", " ").Replace("\n", " ");
            
            if (value.Contains(";") || value.Contains("\""))
            {
                value = "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            
            return value;
        }
    }
}
