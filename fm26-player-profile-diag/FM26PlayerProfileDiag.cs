using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26PlayerProfileDiag
{
    [BepInPlugin("com.vintesetfm.player_profile_diag", "FM26 Player Profile Diagnostic", "3.0.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;

        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("============================================");
            Log.LogInfo("FM26 Player Profile Diagnostic v3.0.0");
            Log.LogInfo("============================================");
            Log.LogInfo("[F11] = Dump IL2CPP Bindings + UIElement DataSource scan");
            Log.LogInfo("[F10] = Dump apenas UIElements / dataSource");
            Log.LogInfo("[F8]  = Re-escanear UIDocuments");

            // Hookeia o Bindings.Update para capturar o pointer nativo
            try
            {
                var bindingsType = Type.GetType("SI.Bindable.Bindings, SI.Bindable");
                if (bindingsType != null)
                {
                    var updateMethod = bindingsType.GetMethod("Update",
                        BindingFlags.Public | BindingFlags.Instance);
                    if (updateMethod != null)
                    {
                        var harmony = new Harmony("com.vintesetfm.player_profile_diag");
                        var patchMethod = typeof(Plugin).GetMethod("OnBindingsUpdate",
                            BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(updateMethod, postfix: new HarmonyMethod(patchMethod));
                        Log.LogInfo("[PPD] Hook no Bindings.Update ativo!");
                    }
                    else Log.LogWarning("[PPD] Bindings.Update nao encontrado");
                }
                else Log.LogWarning("[PPD] SI.Bindable.Bindings nao encontrado");
            }
            catch (Exception ex) { Log.LogError($"[PPD] Erro ao hookear: {ex.Message}"); }

            AddComponent<DiagBehaviour>();
        }

        internal static IntPtr _bindingsPtr = IntPtr.Zero;
        internal static bool _dumpBindingsRequested = false;

        public static void OnBindingsUpdate(object __instance)
        {
            if (__instance == null) return;
            try
            {
                if (_bindingsPtr == IntPtr.Zero)
                {
                    IntPtr extracted = IntPtr.Zero;
                    if (__instance is Il2CppObjectBase il2Base)
                        extracted = il2Base.Pointer;
                    else
                    {
                        // Tenta extrair via campo pooledPtr (IL2CPP wrapper interno)
                        var pf = __instance.GetType().GetField("pooledPtr",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            ?? __instance.GetType().GetField("Pointer",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (pf?.GetValue(__instance) is IntPtr p) extracted = p;
                    }
                    if (extracted != IntPtr.Zero)
                    {
                        _bindingsPtr = extracted;
                        Plugin.Log.LogInfo($"[PPD] Bindings capturado! Ptr=0x{_bindingsPtr:X}");
                    }
                }
            }
            catch { }

            if (!_dumpBindingsRequested) return;
            _dumpBindingsRequested = false;
            DumpBindingsIl2Cpp();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // DUMP A: IL2CPP Bindings via pointer nativo
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private static void DumpBindingsIl2Cpp()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# FM26 IL2CPP Bindings Dump v3 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# Bindings Ptr: 0x{_bindingsPtr:X}");
            var matches = new List<string>();

            if (_bindingsPtr == IntPtr.Zero)
            {
                sb.AppendLine("ERRO: Pointer nao capturado. Abra perfil de jogador e tente F11.");
                WriteFile(sb, matches, "bindings"); return;
            }

            try
            {
                IntPtr klass = IL2CPP.il2cpp_object_get_class(_bindingsPtr);
                string klassName = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(klass));
                string klassNs   = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_namespace(klass));
                sb.AppendLine($"# Classe: {klassNs}.{klassName}");
                sb.AppendLine();

                // Procurar campos de lista candidatos pelo nome
                var listCandidates = new[] {
                    "m_data", "_data", "data", "items", "_items", "m_items",
                    "bindings", "_bindings", "m_bindings", "list", "_list", "m_list" };

                sb.AppendLine("=== CAMPOS CANDIDATOS DE LISTA ===");
                foreach (var cname in listCandidates)
                {
                    IntPtr fPtr = IL2CPP.il2cpp_class_get_field_from_name(klass, cname);
                    if (fPtr == IntPtr.Zero) continue;

                    uint off = IL2CPP.il2cpp_field_get_offset(fPtr);
                    IntPtr childPtr = Marshal.ReadIntPtr(_bindingsPtr + (int)off);
                    sb.AppendLine($"  CAMPO '{cname}' encontrado! offset={off} | childPtr=0x{childPtr:X}");
                    matches.Add($"Campo lista: '{cname}' offset={off} ptr=0x{childPtr:X}");

                    if (childPtr == IntPtr.Zero) { sb.AppendLine("  (null)"); continue; }

                    // Identificar tipo do objeto filho
                    try
                    {
                        IntPtr ck = IL2CPP.il2cpp_object_get_class(childPtr);
                        string cn = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(ck));
                        sb.AppendLine($"  Tipo filho: {cn}");

                        // Tentar ler _size ou Count do child
                        int listSize = 0;
                        foreach (var sname in new[] { "_size", "count", "_count", "Count", "size" })
                        {
                            IntPtr sf = IL2CPP.il2cpp_class_get_field_from_name(ck, sname);
                            if (sf == IntPtr.Zero) continue;
                            uint so = IL2CPP.il2cpp_field_get_offset(sf);
                            listSize = Marshal.ReadInt32(childPtr + (int)so);
                            sb.AppendLine($"  {sname} = {listSize}");
                            matches.Add($"  Lista '{cname}'.{sname} = {listSize}");
                            break;
                        }

                        if (listSize > 0)
                        {
                            // Tentar recuperar o array interno "_items" da lista
                            IntPtr itemsField = IL2CPP.il2cpp_class_get_field_from_name(ck, "_items");
                            if (itemsField != IntPtr.Zero)
                            {
                                uint itemsOff = IL2CPP.il2cpp_field_get_offset(itemsField);
                                IntPtr itemsArrayPtr = Marshal.ReadIntPtr(childPtr + (int)itemsOff);
                                
                                if (itemsArrayPtr != IntPtr.Zero)
                                {
                                    sb.AppendLine($"  Percorrendo array _items... (primeiros 500 para evitar travamento)");
                                    
                                    // Num array IL2CPP: itemsArrayPtr + 0x10 = _bounds/length (se nao for string), e dados começam em 0x20
                                    // Os dados de uma List<T> com reference types sao array de ponteiros
                                    for (int i = 0; i < Math.Min(listSize, 500); i++)
                                    {
                                        // offset de array Il2Cpp: header(16 ou 32 bytes) + indice * ponteiroSize
                                        // em 64 bits: headerGeral = 32 bytes de offset pros elementos
                                        int arrayHeaderSize = 32; 
                                        int ptrSize = 8;
                                        IntPtr itemPtr = Marshal.ReadIntPtr(itemsArrayPtr + arrayHeaderSize + (i * ptrSize));
                                        
                                        // Se for ponteiro valido, analisamos
                                        if (itemPtr != IntPtr.Zero)
                                        {
                                            try
                                            {
                                                IntPtr itemKlass = IL2CPP.il2cpp_object_get_class(itemPtr);
                                                DumpPlayerFields(itemPtr, itemKlass, sb, matches, $"    [{i}] ");
                                            } catch { }
                                        }
                                    }
                                }
                            }
                        }

                        // Procurar campos de CA/PA nos filhos do tipo
                        DumpPlayerFields(childPtr, ck, sb, matches, "  ");
                    }
                    catch (Exception ex) { sb.AppendLine($"  Erro ao inspecionar filho: {ex.Message}"); }
                }

                // Procurar campos de CA/PA direto no Bindings (por precaução)
                sb.AppendLine();
                sb.AppendLine("=== CAMPOS DIRETOS DE ABILITY NO BINDINGS ===");
                DumpPlayerFields(_bindingsPtr, klass, sb, matches, "");

                // DUMP MEMORIA RAW DO BINDINGS
                DumpRawMemory(_bindingsPtr, "Bindings", sb, matches);

                // Campo pai
                IntPtr parent = IL2CPP.il2cpp_class_get_parent(klass);
                if (parent != IntPtr.Zero)
                {
                    string pn = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(parent));
                    sb.AppendLine($"  Classe pai: {pn}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"ERRO FATAL: {ex.Message}\n{ex.StackTrace}");
            }

            WriteFile(sb, matches, "bindings");
        }

        // Tenta ler os campos CA/PA conhecidos de um objeto IL2CPP
        private static void DumpPlayerFields(IntPtr objPtr, IntPtr klass,
            StringBuilder sb, List<string> matches, string indent)
        {
            // Campos que esperamos existir conforme o JSON do FMRTE26
            var abilityFields = new[] {
                "CA", "PA", "RCA",
                "currentAbility", "potentialAbility",
                "CurrentAbility", "PotentialAbility",
                "ca", "pa", "rca",
                "m_currentAbility", "m_potentialAbility",
                "ActualRating", "PotentialRating" };

            foreach (var fname in abilityFields)
            {
                try
                {
                    IntPtr fPtr = IL2CPP.il2cpp_class_get_field_from_name(klass, fname);
                    if (fPtr == IntPtr.Zero) continue;

                    uint off = IL2CPP.il2cpp_field_get_offset(fPtr);
                    int ival = Marshal.ReadInt32(objPtr + (int)off);
                    sb.AppendLine($"{indent}⭐ CAMPO '{fname}' = {ival}  (offset={off})");
                    matches.Add($"⭐ FIELD {fname}={ival} @ offset={off}");
                    Plugin.Log.LogInfo($"[PPD] ⭐ FOUND {fname}={ival}");
                }
                catch { }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // DUMP B: UIElement DataSource scan
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        internal static void DumpUIDataSources(List<UIDocument> docs)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# FM26 UIElement DataSource Scan v3 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# Buscando campos: CA, PA, RCA, currentAbility, potentialAbility...");
            sb.AppendLine();
            var matches = new List<string>();
            int scanned = 0;

            foreach (var doc in docs)
            {
                try
                {
                    var root = doc.rootVisualElement;
                    if (root != null) ScanElement(root, sb, matches, ref scanned, 0);
                }
                catch { }
            }

            sb.AppendLine();
            sb.AppendLine($"# Escaneados: {scanned} elementos | Matches: {matches.Count}");
            sb.AppendLine();
            sb.AppendLine("=== MATCHES ===");
            if (matches.Count == 0) sb.AppendLine("(nenhum)");
            else foreach (var m in matches) sb.AppendLine(m);

            WriteFile(sb, matches, "ui_scan");
        }

        private static readonly string[] _dsFieldNames = {
            "dataSource", "m_DataSource", "_dataSource", "DataSource" };
        private static readonly string[] _abilityFieldNames = {
            "CA", "PA", "RCA", "currentAbility", "potentialAbility",
            "CurrentAbility", "PotentialAbility", "ca", "pa",
            "m_currentAbility", "m_potentialAbility" };

        private static void ScanElement(VisualElement el, StringBuilder sb,
            List<string> matches, ref int scanned, int depth)
        {
            if (el == null || depth > 25) return;
            scanned++;

            try
            {
                IntPtr elPtr = el.Pointer;
                IntPtr elKlass = IL2CPP.il2cpp_object_get_class(elPtr);

                // Tentar dataSource direto pela IL2CPP (qualquer dos nomes candidatos)
                foreach (var dsName in _dsFieldNames)
                {
                    IntPtr dsField = IL2CPP.il2cpp_class_get_field_from_name(elKlass, dsName);
                    if (dsField == IntPtr.Zero) continue;

                    uint dsOff = IL2CPP.il2cpp_field_get_offset(dsField);
                    IntPtr dsPtr = Marshal.ReadIntPtr(elPtr + (int)dsOff);
                    if (dsPtr == IntPtr.Zero) continue;

                    IntPtr dsKlass = IL2CPP.il2cpp_object_get_class(dsPtr);
                    string dsClassName = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(dsKlass));
                    string dsNs = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_namespace(dsKlass));
                    string elName = el.name ?? "?";

                    sb.AppendLine($"[DATASOURCE] el='{elName}' depth={depth} class={dsNs}.{dsClassName} ptr=0x{dsPtr:X}");

                    // Procurar campos de ability no dataSource
                    foreach (var fname in _abilityFieldNames)
                    {
                        try
                        {
                            IntPtr ff = IL2CPP.il2cpp_class_get_field_from_name(dsKlass, fname);
                            if (ff == IntPtr.Zero) continue;
                            uint fOff = IL2CPP.il2cpp_field_get_offset(ff);
                            int ival = Marshal.ReadInt32(dsPtr + (int)fOff);
                            string entry = $"  ⭐ {fname}={ival}  (el='{elName}')";
                            sb.AppendLine(entry);
                            matches.Add(entry);
                            Plugin.Log.LogInfo($"[PPD] {entry}");
                        }
                        catch { }
                    }

                    // DUMP MEMÓRIA RAW DO DATASOURCE (Para achar Offset CA/PA = 193/197)
                    DumpRawMemory(dsPtr, $"DataSource[{dsClassName}]", sb, matches);
                    break; // encontrou um dataSource, não precisa testar os outros nomes
                }

                // Também tentar dataSource como propriedade Unity (IL2CPP wrapper)
                try
                {
                    var ds = el.dataSource;
                    if (ds != null)
                    {
                        string elName = el.name ?? "?";
                        string dstName = ds.GetType().Name;
                        sb.AppendLine($"[DATASOURCE-PROP] el='{elName}' depth={depth} type={dstName}");

                        // Enumerar via reflection .NET do wrapper
                        foreach (var prop in ds.GetType().GetProperties(
                            BindingFlags.Public | BindingFlags.Instance))
                        {
                            try
                            {
                                var v = prop.GetValue(ds);
                                string pname = prop.Name;
                                if (v != null && (pname == "CA" || pname == "PA" || pname == "RCA" ||
                                    pname.ToLower().Contains("ability")))
                                {
                                    string entry = $"  ⭐ PROP {pname}={v}  (el='{elName}')";
                                    sb.AppendLine(entry);
                                    matches.Add(entry);
                                    Plugin.Log.LogInfo($"[PPD] {entry}");
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
            catch { }

            for (int i = 0; i < el.childCount; i++)
                ScanElement(el.ElementAt(i), sb, matches, ref scanned, depth + 1);
        }

        private static void WriteFile(StringBuilder sb, List<string> matches, string suffix)
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Sports Interactive", "Football Manager 2026");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"ppd_{suffix}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Plugin.Log.LogInfo($"[PPD] ✅ Salvo: {path} | {matches.Count} matches");
            foreach (var m in matches) Plugin.Log.LogInfo($"[PPD] {m}");
        }

        private static void DumpRawMemory(IntPtr ptr, string prefix, StringBuilder sb, List<string> matches)
        {
            sb.AppendLine($"\n--- INICIANDO DUMP DE MEMORIA RAW (PTR: 0x{ptr:X}) [{prefix}] ---");
            int rangeBytes = 8192; // 8 KB
            for(int offset = 0; offset < rangeBytes; offset += 2)
            {
                try {
                    short val16 = Marshal.ReadInt16(ptr + offset);
                    int val32 = 0;
                    if (offset % 4 == 0) // Ler Int32 apenas em offsets alinhados a 4 bytes
                        val32 = Marshal.ReadInt32(ptr + offset);

                    // Ignorar 0 e negativos para não poluir
                    if (val16 > 0 && val16 <= 200) {
                        if (val16 == 193 || val16 == 197 || val32 == 193 || val32 == 197) {
                            string msg = $"🎯 BINGO! Valor {val16} (16bit) / {val32} (32bit) encontrado no offset 0x{offset:X} ({offset}) de {prefix}!";
                            sb.AppendLine(msg);
                            matches.Add(msg);
                        }
                    }
                } catch { } // ignora memory access violation
            }
            sb.AppendLine($"--- FIM DUMP DE MEMORIA RAW [{prefix}] ---\n");
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MonoBehaviour: Update loop + input
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public class DiagBehaviour : MonoBehaviour
    {
        private List<UIDocument> _docs = new List<UIDocument>();
        private int _frame = 0;

        public DiagBehaviour(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            _frame++;
            if (_frame == 300) ScanDocs();

            if (Keyboard.current == null) return;

            if (Keyboard.current.f8Key.wasPressedThisFrame)
            {
                ScanDocs();
                Plugin.Log.LogInfo($"[PPD] F8: {_docs.Count} UIDocuments");
            }

            if (Keyboard.current.f11Key.wasPressedThisFrame)
            {
                Plugin.Log.LogInfo("[PPD] F11: Dump Bindings IL2CPP + UI DataSource scan...");
                ScanDocs();
                Plugin._dumpBindingsRequested = true;
                Plugin.DumpUIDataSources(_docs);
            }

            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                Plugin.Log.LogInfo("[PPD] F10: UI DataSource scan...");
                ScanDocs();
                Plugin.DumpUIDataSources(_docs);
            }
        }

        private void ScanDocs()
        {
            _docs.Clear();
            var all = FindObjectsOfType<UIDocument>();
            foreach (var doc in all)
            {
                if (doc.rootVisualElement != null)
                {
                    Plugin.Log.LogInfo($"[PPD] Encontrado UIDocument com root: {doc.rootVisualElement.name}");
                    _docs.Add(doc);
                }
            }
            Plugin.Log.LogInfo($"[PPD] UIDocuments escaneados: {_docs.Count}");
        }
    }
}
