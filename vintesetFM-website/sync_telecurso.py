import os
import json
import requests
import time
import re
from notebooklm_mcp.api_client import NotebookLMClient

AUTH_FILE = os.path.expanduser("~/.notebooklm-mcp/auth.json")
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
OUT_JSON = os.path.join(BASE_DIR, "telecurso_videos_sync.json")

# WEBHOOK_URL: You must deploy your n8n workflow and paste the Test/Production Webhook URL here.
WEBHOOK_URL = os.getenv("N8N_WEBHOOK_URL", "http://localhost:5678/webhook-test/notebooklm-to-gdrive")

def sanitize_filename(name):
    return re.sub(r'[\\/*?:"<>|]', "", name).replace(" ", "_").lower()[0:100]

def extract_youtube_url(notebook_data):
    if not notebook_data or not isinstance(notebook_data, list) or len(notebook_data) == 0:
        return ""
    if not isinstance(notebook_data[0], list) or len(notebook_data[0]) < 2:
        return ""
        
    for src in notebook_data[0][1]:
        if len(src) > 2 and isinstance(src[2], list):
            metadata = src[2]
            for item in metadata:
                if isinstance(item, list) and len(item) > 0 and isinstance(item[0], str):
                    if "youtube" in item[0] or "youtu.be" in item[0]:
                        return item[0]
                elif isinstance(item, str) and ("youtube" in item or "youtu.be" in item):
                    return item
    return ""

def main():
    if not os.path.exists(AUTH_FILE):
        print(f"ERRO: Arquivo de autenticação não encontrado em {AUTH_FILE}")
        return

    mapping = []
    
    # Se já existir progresso anterior
    if os.path.exists(OUT_JSON):
        with open(OUT_JSON, "r", encoding="utf-8") as f:
            mapping = json.load(f)

    with open(AUTH_FILE, "r") as f:
        auth_data = json.load(f)
    
    cookies = auth_data.get("cookies", {})
    try:
        client = NotebookLMClient(cookies=cookies)
    except Exception as e:
        print(f"Falha ao autenticar no NotebookLM: {e}")
        return
        
    notebooks = client.list_notebooks()
    print(f"Total: {len(notebooks)} cadernos. Iniciando Sincronização...")
    
    for i, nb in enumerate(notebooks, 1):
        if not nb.title:
            continue
            
        print(f"[{i}/{len(notebooks)}] Processando: {nb.title}")
        
        # Check if already processed
        if any(m["id"] == nb.id for m in mapping):
            print(" -> Já processado. Pulando.")
            continue
        
        try:
            detailed = client.get_notebook(nb.id)
            youtube_url = extract_youtube_url(detailed)
            
            if not youtube_url:
                print(" -> Sem link reconhecível do youtube.")
                time.sleep(1.5)
                continue
                
            time.sleep(1)
            artifacts = client.poll_studio_status(nb.id)
            video_url = ""
            for art in artifacts:
                if art.get("type") == "video" and art.get("status") == "completed":
                    video_url = art.get("video_url", "")
                    break
                    
            if not video_url:
                print(" -> Vídeo não está pronto no Studio.")
                time.sleep(2)
                continue
                
            safe_name = sanitize_filename(nb.title)
            
            print(f" -> Enviando para n8n...")
            header_cookie = "; ".join(f"{k}={v}" for k, v in cookies.items())
            
            payload = {
                "videoUrl": video_url,
                "cookie": header_cookie,
                "fileName": safe_name
            }
            
            resp = requests.post(WEBHOOK_URL, json=payload, timeout=60)
            if resp.status_code == 200:
                resp_data = resp.json()
                drive_id = resp_data.get("driveId")
                print(f" -> Sucesso! Drive ID: {drive_id}")
                
                mapping.append({
                    "id": nb.id,
                    "title": nb.title,
                    "youtubeUrl": youtube_url,
                    "driveId": drive_id,
                })
                
                # Salvar progresso
                with open(OUT_JSON, "w", encoding="utf-8") as f:
                    json.dump(mapping, f, indent=2, ensure_ascii=False)
            else:
                print(f" -> Erro no n8n: {resp.status_code} - {resp.text}")
                
        except Exception as e:
            print(f" -> [✖] Erro processando caderno: {e}")
            
        time.sleep(2)
        
    print("\nProcessamento Finalizado!")
    print(f"Mapeamento salvo em: {OUT_JSON}")
        
if __name__ == "__main__":
    main()
