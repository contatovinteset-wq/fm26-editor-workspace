import os
import json
import requests
import time
import re
from notebooklm_mcp.api_client import NotebookLMClient

AUTH_FILE = os.path.expanduser("~/.notebooklm-mcp/auth.json")
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
VIDEOS_DIR = os.path.join(BASE_DIR, "public", "videos")
OUT_JSON = os.path.join(BASE_DIR, "videos_mapping.json")

def sanitize_filename(name):
    return re.sub(r'[\\/*?:"<>|]', "", name).replace(" ", "_").lower()

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
        print(f"ERRO: Arquivo de autenticação não encontrado")
        return

    os.makedirs(VIDEOS_DIR, exist_ok=True)
    mapping = []
    
    # Se já existir do download anterior, vamos carregar e continuar de onde parou
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
    print(f"Total: {len(notebooks)} cadernos. Iniciando Download Local...")
    
    for i, nb in enumerate(notebooks, 1):
        if not nb.title:
            continue
            
        print(f"[{i}/{len(notebooks)}] Processando: {nb.title}")
        
        # Check if already processed
        if any(m["id"] == nb.id for m in mapping):
            print(" -> Já baixado. Pulando.")
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
                
            # O download
            safe_name = sanitize_filename(nb.title) + ".mp4"
            dest_path = os.path.join(VIDEOS_DIR, safe_name)
            
            print(f" -> Baixando vídeo do NotebookLM para {safe_name}...")
            # Manual redirect handling para não perder o cabeçalho Cookie do python requests
            header_cookie = "; ".join(f"{k}={v}" for k, v in cookies.items())
            req = requests.get(video_url, headers={"Cookie": header_cookie}, allow_redirects=False, timeout=30)
            
            if req.status_code in (301, 302, 303, 307, 308):
                redirect_url = req.headers['Location']
                req = requests.get(redirect_url, headers={"Cookie": header_cookie}, stream=True, timeout=30)
            else:
                req = requests.get(video_url, headers={"Cookie": header_cookie}, stream=True, timeout=30)
                
            req.raise_for_status()
            
            # Checar se não é uma página HTML de login que nos enganou
            if "text/html" in req.headers.get("Content-Type", ""):
                print(" -> ERRO: Google retornou página de Login ao invés do vídeo (Cooke expirado para download).")
                continue
                
            with open(dest_path, "wb") as vd:
                for chunk in req.iter_content(chunk_size=8192):
                    if chunk:
                        vd.write(chunk)
                        
            print(" -> Download Concluído!")
            
            mapping.append({
                "id": nb.id,
                "title": nb.title,
                "youtubeUrl": youtube_url,
                "localVideoUrl": f"/videos/{safe_name}",
            })
            
            # Salvar progresso
            with open(OUT_JSON, "w", encoding="utf-8") as f:
                json.dump(mapping, f, indent=2, ensure_ascii=False)
                
        except Exception as e:
            print(f" -> [✖] Erro processando caderno: {e}")
            
        time.sleep(3)
        
if __name__ == "__main__":
    main()
