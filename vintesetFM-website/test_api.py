import json
import os
import requests
import sys

from notebooklm_mcp import api_client

print("Iniciando teste da API interna...")
try:
    auth_data = json.load(open(os.path.expanduser('~/.notebooklm-mcp/auth.json')))
    print(f"Auth loaded com {len(auth_data.get('cookies', {}))} cookies")
    client = api_client.NotebookLMClient(
        cookies=auth_data.get('cookies', {}),
        csrf_token=auth_data.get('csrf_token', ''),
        session_id=auth_data.get('session_id', '')
    )
    print("Cliente inicializado. Buscando artifacts do estúdio...")
    
    # "Guia de Bolas Paradas para Football Manager 2026"
    notebook_id = 'e4e71530-a4d2-411e-b9a3-b3d7bdf3074c' 
    artifacts = client.poll_studio_status(notebook_id)
    
    print("Sucesso! Resultado:")
    print(json.dumps(artifacts, indent=2))
except Exception as e:
    print(f"Erro: {e}")
