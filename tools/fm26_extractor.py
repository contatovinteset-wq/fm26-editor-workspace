#!/usr/bin/env python3
"""
FM26 Extraction Tool
Extrai e organiza arquivos do jogo FM26
"""

import os
import sys
import json
import struct
import zipfile
import shutil
from pathlib import Path
from datetime import datetime

class FM26Extractor:
    def __init__(self, game_path, output_path):
        self.game_path = Path(game_path)
        self.output_path = Path(output_path)
        self.manifest = {
            "extracted_at": datetime.now().isoformat(),
            "game_path": str(game_path),
            "files": {}
        }
    
    def find_all_xmls(self):
        """Encontra todos os XMLs no diretório do jogo"""
        xmls = list(self.game_path.rglob("*.xml"))
        print(f"[XML] Encontrados {len(xmls)} arquivos XML")
        return xmls
    
    def find_all_jsons(self):
        """Encontra todos os JSONs"""
        jsons = list(self.game_path.rglob("*.json"))
        print(f"[JSON] Encontrados {len(jsons)} arquivos JSON")
        return jsons
    
    def find_all_configs(self):
        """Encontra arquivos de configuração"""
        configs = []
        for ext in ["*.cfg", "*.config", "*.ini", "*.properties"]:
            configs.extend(self.game_path.rglob(ext))
        print(f"[CONFIG] Encontrados {len(configs)} arquivos de config")
        return configs
    
    def find_asset_bundles(self):
        """Encontra todos os Asset Bundles"""
        bundles = list(self.game_path.rglob("*.bundle"))
        print(f"[BUNDLE] Encontrados {len(bundles)} Asset Bundles")
        
        # Organizar por tipo
        by_type = {}
        for bundle in bundles:
            name = bundle.stem
            # Extrair tipo do nome
            parts = name.split('-')
            if len(parts) >= 2:
                bundle_type = parts[0]
                if bundle_type not in by_type:
                    by_type[bundle_type] = []
                by_type[bundle_type].append(str(bundle))
        
        return bundles, by_type
    
    def find_fmf_files(self):
        """Encontra arquivos FMF"""
        fmfs = list(self.game_path.rglob("*.fmf"))
        print(f"[FMF] Encontrados {len(fmfs)} arquivos FMF")
        return fmfs
    
    def find_dll_assemblies(self):
        """Encontra DLLs principais do jogo"""
        dlls = []
        for dll in self.game_path.rglob("*.dll"):
            name = dll.name.lower()
            # Filtrar DLLs importantes
            if any(x in name for x in ['fm', 'si.', 'game_plugin', 'match', 'ui']):
                dlls.append(str(dll))
        print(f"[DLL] Encontrados {len(dlls)} DLLs relevantes")
        return dlls
    
    def extract_fmf_header(self, fmf_path):
        """Extrai header de arquivo FMF"""
        with open(fmf_path, 'rb') as f:
            magic = f.read(4)
            if magic == b'FMF\x00':
                version = struct.unpack('<I', f.read(4))[0]
                return {
                    "path": str(fmf_path),
                    "magic": "FMF",
                    "version": version,
                    "valid": True
                }
        return {"path": str(fmf_path), "valid": False}
    
    def scan_all(self):
        """Escaneia todo o diretório do jogo"""
        print("=" * 60)
        print("FM26 EXTRACTION TOOL - Scan Completo")
        print("=" * 60)
        
        results = {
            "xmls": [str(x) for x in self.find_all_xmls()],
            "jsons": [str(x) for x in self.find_all_jsons()],
            "configs": [str(x) for x in self.find_all_configs()],
            "fmfs": [str(x) for x in self.find_fmf_files()],
            "dlls": self.find_dll_assemblies()
        }
        
        bundles, bundles_by_type = self.find_asset_bundles()
        results["bundles"] = [str(x) for x in bundles]
        results["bundles_by_type"] = bundles_by_type
        
        # Salvar manifest
        manifest_path = self.output_path / "extraction_manifest.json"
        with open(manifest_path, 'w', encoding='utf-8') as f:
            json.dump(results, f, indent=2, ensure_ascii=False)
        
        print(f"\n[OK] Manifest salvo em: {manifest_path}")
        return results
    
    def extract_important_files(self):
        """Extrai arquivos importantes para análise"""
        extract_dir = self.output_path / "extracted"
        extract_dir.mkdir(parents=True, exist_ok=True)
        
        # Copiar XMLs importantes
        xml_dir = extract_dir / "xmls"
        xml_dir.mkdir(exist_ok=True)
        for xml in self.find_all_xmls():
            if xml.stat().st_size > 1000:  # Só arquivos > 1KB
                shutil.copy2(xml, xml_dir / xml.name)
        
        # Copiar JSONs importantes
        json_dir = extract_dir / "jsons"
        json_dir.mkdir(exist_ok=True)
        for json_file in self.find_all_jsons():
            shutil.copy2(json_file, json_dir / json_file.name)
        
        print(f"[OK] Arquivos extraídos para: {extract_dir}")

if __name__ == '__main__':
    game_path = "/data/.openclaw/workspace/fm26-game-files"
    output_path = "/data/.openclaw/workspace/fm26-editor-workspace"
    
    extractor = FM26Extractor(game_path, output_path)
    results = extractor.scan_all()
    
    print("\n" + "=" * 60)
    print("RESUMO")
    print("=" * 60)
    print(f"XMLs: {len(results['xmls'])}")
    print(f"JSONs: {len(results['jsons'])}")
    print(f"Configs: {len(results['configs'])}")
    print(f"FMFs: {len(results['fmfs'])}")
    print(f"DLLs: {len(results['dlls'])}")
    print(f"Bundles: {len(results['bundles'])}")
    print(f"\nTipos de Bundle:")
    for bt, bundles in results['bundles_by_type'].items():
        print(f"  {bt}: {len(bundles)} bundles")
