import json
import os
import requests
import urllib.parse
import re

auth_data = json.load(open(os.path.expanduser('~/.notebooklm-mcp/auth.json')))
cookies = auth_data.get('cookies', {})
session_id = auth_data.get('session_id', '')

headers = {
    'Content-Type': 'application/x-www-form-urlencoded;charset=utf-8',
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
}

def clean_json_response(text: str):
    text = re.sub(r'^\)]}\'\n', '', text)
    lines = text.split('\n')
    for line in lines:
        try:
            parsed = json.loads(line)
            if isinstance(parsed, list) and len(parsed) > 0:
                if isinstance(parsed[0], list) and len(parsed[0]) >= 3:
                    return json.loads(parsed[0][2])
        except:
            pass
    return None

def call_rpc(rpc_id, params_json, source_path="/"):
    rpc_call = [ [ rpc_id, params_json, None, "generic" ] ]
    f_req = json.dumps([rpc_call])
    payload = f"f.req={urllib.parse.quote(f_req)}"
    if session_id:
        payload += f"&at={session_id}"
    
    url = f"https://notebooklm.google.com/_/LabsTailwindUi/data/batchexecute?rpcids={rpc_id}&source-path={urllib.parse.quote(source_path)}"
    resp = requests.post(url, headers=headers, cookies=cookies, data=payload, timeout=10)
    print("HTTP Status:", resp.status_code)
    return clean_json_response(resp.text)

print("Listando Notebooks...")
try:
    data = call_rpc("wXbhsf", json.dumps([None, None, None, None, None]))
    if not data:
        print("Erro: não retornou JSON. Provavel fim de sessão.")
        exit(1)
        
    notebooks_data = data[1] if len(data) > 1 else []
    for nb in notebooks_data[:3]:
        nb_id = nb[0]
        nb_title = nb[1]
        print(f"\nNotebook: {nb_title} ({nb_id})")
        
        # Get Notebook details
        nb_data = call_rpc("rLM1Ne", json.dumps([nb_id]), f"/notebook/{nb_id}")
        if nb_data:
            sources_data = nb_data[1] if isinstance(nb_data, list) and len(nb_data) > 1 else []
            for src in sources_data:
                if isinstance(src, list) and len(src) >= 3:
                    src_title = src[1] if len(src) > 1 else "Unknown"
                    metadata = src[2] if len(src) > 2 else []
                    src_url = None
                    if isinstance(metadata, list) and len(metadata) > 7:
                        url_info = metadata[7]
                        if isinstance(url_info, list) and len(url_info) > 0:
                            src_url = url_info[0]
                    print(f"  -> Fonte: {src_title}")
                    if src_url:
                        print(f"     URL Original: {src_url}")
except Exception as e:
    print("ERRO EXCEPTION:", e)
