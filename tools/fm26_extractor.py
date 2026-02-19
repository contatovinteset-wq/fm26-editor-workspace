#!/usr/bin/env python3
"""
FM26 Extractor Tool - Ferramenta de extração automatizada
Extrai e analisa arquivos do Football Manager 26
"""

import os
import sys
import json
import struct
import zipfile
import subprocess
from pathlib import Path
from datetime import datetime

class FM26Extractor:
    def __init__(self, game_path: str, output_path: str):
        self.game_path = Path(game_path)
        self.output_path = Path(output_path)
        self.output_path.mkdir(parents=True, exist_ok=True)
        
    def find_fmf_files(self):
        """Localiza todos os arquivos FMF do jogo"""
        fmf_files = list(self.game_path.glob("**/*.fmf"))
        print(f"Encontrados {len(fmf_files)} arquivos FMF")
        return fmf_files
    
    def find_asset_bundles(self):
        """Localiza todos os Asset Bundles"""
        bundles = list(self.game_path.glob("**/*.bundle"))
        print(f"Encontrados {len(bundles)} Asset Bundles")
        return bundles
    
    def find_dlls(self):
        """Localiza DLLs importantes"""
        dlls = list(self.game_path.glob("**/*.dll"))
        important = [d for d in dlls if any(x in d.name.lower() for x in ['fm', 'si.', 'game_plugin'])]
        print(f"Encontradas {len(important)} DLLs importantes")
        return important
    
    def analyze_metadata(self):
        """Analisa global-metadata.dat"""
        metadata_path = self.game_path / "fm_Data/il2cpp_data/Metadata/global-metadata.dat"
        if not metadata_path.exists():
            print("Metadata não encontrado")
            return None
            
        with open(metadata_path, 'rb') as f:
            data = f.read()
        
        # Extrair strings legíveis
        strings = []
        current = b''
        for byte in data:
            if 32 <= byte < 127:
                current += bytes([byte])
            else:
                if len(current) > 10:
                    try:
                        s = current.decode('utf-8', errors='replace')
                        strings.append(s)
                    except:
                        pass
                current = b''
        
        # Filtrar por categorias importantes
        categories = {
            'injury': [],
            'transfer': [],
            'newgen': [],
            'match': [],
            'ui': [],
            'database': [],
            'config': []
        }
        
        keywords = {
            'injury': ['injury', 'lesion', 'fitness'],
            'transfer': ['transfer', 'value', 'wage', 'salary'],
            'newgen': ['newgen', 'regen', 'wonderkid'],
            'match': ['match', 'engine', 'tactic'],
            'ui': ['uipanel', 'uitheme', 'skin', 'uiview'],
            'database': ['database', 'playerid', 'clubid', 'teamid'],
            'config': ['config', 'setting', 'path']
        }
        
        for s in strings:
            s_lower = s.lower()
            for cat, kws in keywords.items():
                if any(kw in s_lower for kw in kws):
                    categories[cat].append(s)
        
        return categories
    
    def extract_bundle_strings(self, bundle_path: Path):
        """Extrai strings de um Asset Bundle"""
        with open(bundle_path, 'rb') as f:
            data = f.read()
        
        strings = []
        current = b''
        for i, byte in enumerate(data):
            if 32 <= byte < 127:
                current += bytes([byte])
            else:
                if len(current) > 8:
                    try:
                        s = current.decode('utf-8', errors='replace')
                        if s.isprintable():
                            strings.append(s)
                    except:
                        pass
                current = b''
        
        return strings
    
    def generate_report(self):
        """Gera relatório completo do jogo"""
        report = {
            'timestamp': datetime.now().isoformat(),
            'game_path': str(self.game_path),
            'files': {
                'fmf': len(self.find_fmf_files()),
                'bundles': len(self.find_asset_bundles()),
                'dlls': len(self.find_dlls())
            },
            'metadata_analysis': self.analyze_metadata()
        }
        
        # Salvar relatório
        report_path = self.output_path / 'extraction_report.json'
        with open(report_path, 'w', encoding='utf-8') as f:
            json.dump(report, f, indent=2, ensure_ascii=False)
        
        print(f"Relatório salvo em: {report_path}")
        return report

def main():
    game_path = "/data/.openclaw/workspace/fm26-game-files"
    output_path = "/data/.openclaw/workspace/fm26-editor-workspace/extraction-output"
    
    extractor = FM26Extractor(game_path, output_path)
    report = extractor.generate_report()
    
    print("\n=== RESUMO ===")
    for cat, items in report.get('metadata_analysis', {}).items():
        if isinstance(items, list):
            print(f"{cat}: {len(items)} referências")

if __name__ == '__main__':
    main()
