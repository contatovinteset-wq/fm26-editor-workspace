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
            
            // F8 - Re-escanear
            if (Keyboard.current.f8Key.wasPressedThisFrame)
            {
                Plugin.Log.LogInfo("[FM26Export] >>> F8 - Re-escaneando UIDocuments...");
                ScanUIDocuments();
            }
            
            // Ctrl+P - Exportar
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
        
        // Método auxiliar para evitar ambiguidade
        private VisualElement FindElement(VisualElement parent, string name)
        {
            foreach (var child in parent.Children())
            {
                if (child.name == name)
                    return child;
            }
            return null;
        }
        
        // Método auxiliar para encontrar Labels
        private List<Label> FindLabels(VisualElement parent)
        {
            var labels = new List<Label>();
            FindLabelsRecursive(parent, labels);
            return labels;
        }
        
        private void FindLabelsRecursive(VisualElement element, List<Label> labels)
        {
            if (element is Label label)
            {
                labels.Add(label);
            }
            
            foreach (var child in element.Children())
            {
                FindLabelsRecursive(child, labels);
            }
        }
        
        // Método auxiliar para encontrar Toggle
        private Toggle FindToggle(VisualElement parent)
        {
            return FindToggleRecursive(parent);
        }
        
        private Toggle FindToggleRecursive(VisualElement element)
        {
            if (element is Toggle toggle)
                return toggle;
            
            foreach (var child in element.Children())
            {
                var found = FindToggleRecursive(child);
                if (found != null)
                    return found;
            }
            return null;
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
                    var tables = FindElement(root, "tables");
                    if (tables == null)
                    {
                        Plugin.Log.LogWarning("[FM26Export] 'tables' não encontrado");
                        continue;
                    }
                    Plugin.Log.LogInfo($"[FM26Export] ✓ 'tables' encontrado");
                    
                    var tableContainer = FindElement(tables, "search-table-remapper");
                    if (tableContainer == null)
                    {
                        Plugin.Log.LogWarning("[FM26Export] 'search-table-remapper' não encontrado");
                        
                        // Listar filhos de tables para debug
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
                    
                    // PASSO 1 CONCLUÍDO
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
                        
                        // Verificar Toggle
                        try
                        {
                            var toggle = FindToggle(row);
                            if (toggle != null && toggle.value)
                            {
                                isSelected = true;
                                toggleSelected++;
                            }
                        }
                        catch { }
                        
                        // Verificar classes CSS
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
                    
                    // Se nenhuma selecionada, pegar todas
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
                        var headerSection = FindElement(root, "PersonSearchTableTopSection");
                        if (headerSection != null)
                        {
                            Plugin.Log.LogInfo("[FM26Export] ✓ 'PersonSearchTableTopSection' encontrado");
                            
                            var headerLabels = FindLabels(headerSection);
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
                    
                    // Se não achou header, usar genérico
                    if (headers.Count == 0)
                    {
                        headers.Add("Dados");
                    }
                    
                    // PASSO 4: Montar CSV
                    var csv = new StringBuilder();
                    
                    // Header
                    csv.AppendLine(string.Join(";", headers));
                    
                    // Linhas
                    int exportedCount = 0;
                    foreach (var row in selectedRows)
                    {
                        try
                        {
                            var values = new List<string>();
                            
                            var labels = FindLabels(row);
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
                        
                        if (exportedCount >= 10000) break; // Limite de segurança
                    }
                    
                    // PASSO 5: Salvar CSV
                    string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string fm26Path = Path.Combine(docsPath, "Sports Interactive", "Football Manager 2026");
                    
                    // Criar diretório se não existir
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
                    
                    return; // Sucesso - sair do loop
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
            
            // Remover quebras de linha
            value = value.Replace("\r", " ").Replace("\n", " ");
            
            // Se tem ponto-e-vírgula ou aspas, envolver em aspas
            if (value.Contains(";") || value.Contains("\""))
            {
                value = "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            
            return value;
        }
    }
}
