import os
import json
import time
import re
from notebooklm_mcp import api_client
from playwright.sync_api import sync_playwright

DOWNLOADS_DIR = "downloads"
INDEX_FILE = "index.json"

def sanitize_filename(name):
    return re.sub(r'[\\/*?:"<>|]', "", name).strip()

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
    print("--- Recuperando Lista Oficial de Cadernos via API Interna ---")
    
    auth_path = os.path.expanduser('~/.notebooklm-mcp/auth.json')
    if not os.path.exists(auth_path):
        print("Arquivo auth.json não encontrado. Rode `notebooklm-mcp-auth` no terminal primeiro.")
        return
        
    session_file = "notebooklm_session_final.json"
    if not os.path.exists(session_file):
        print(f"Sessão Playwright não encontrada ({session_file}). Sem ela não podemos baixar as mídias autenticadas.")
        return

    try:
        with open(auth_path, "r", encoding="utf-8") as f:
            auth_data = json.load(f)
    except Exception as e:
        print(f"Erro ao ler auth.json: {e}")
        return

    client = api_client.NotebookLMClient(
        cookies=auth_data.get('cookies', {}),
        csrf_token=auth_data.get('csrf_token', ''),
        session_id=auth_data.get('session_id', '')
    )

    notebooks = client.list_notebooks()
    ignored_keywords = ["introduction to", "bracket guide to"]
    my_notebooks = [nb for nb in notebooks if not any(kw in nb.title.lower() for kw in ignored_keywords)]

    print(f"Encontrados {len(my_notebooks)} cadernos reais da sua conta!\n")

    if not os.path.exists(DOWNLOADS_DIR):
        os.makedirs(DOWNLOADS_DIR)

    index_data = []
    if os.path.exists(INDEX_FILE):
        try:
            with open(INDEX_FILE, "r", encoding="utf-8") as f:
                index_data = json.load(f)
        except Exception:
            pass

    downloaded_ids = {item.get('id') for item in index_data}

    # Iniciar Playwright HEADLESS apenas para trafegar os cookies
    print("Inicializando Motor de Mídia Autenticada...")
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(storage_state=session_file)

        for idx, nb in enumerate(my_notebooks, start=1):
            notebook_id = nb.id
            title = nb.title
            print(f"[{idx}/{len(my_notebooks)}] Consultando API: {title}")
            
            if nb.url in downloaded_ids or notebook_id in downloaded_ids:
                print("-> Já baixado anteriormente. Pulando.")
                continue

            youtube_url = ""
            try:
                detailed = client.get_notebook(notebook_id)
                youtube_url = extract_youtube_url(detailed)
                if youtube_url:
                    print(f"-> URL do YouTube encontrada: {youtube_url}")
            except Exception as e:
                print(f"-> Falha ao recuperar detalhes do caderno: {e}")

            try:
                artifacts = client.poll_studio_status(notebook_id)
            except Exception as e:
                print(f"-> FALHA ao consultar API: {e}")
                continue

            found_media = False
            if artifacts:
                for artifact in artifacts:
                    atype = artifact.get("type")
                    if atype not in ["audio", "video"]:
                        continue
                        
                    status = artifact.get("status")
                    if status != "completed":
                        print(f"-> A mídia ({atype}) ainda está sendo gerada (status: {status}).")
                        continue
                        
                    url = artifact.get("video_url") or artifact.get("audio_url")
                    if not url:
                        continue
                        
                    found_media = True
                    ext = ".mp4" if "video" in atype else ".mp3"
                    safe_name = f"{idx:02d}_{sanitize_filename(title)}{ext}"
                    dest_path = os.path.join(DOWNLOADS_DIR, safe_name)
                    
                    print(f"-> Link Direto Encontrado! Extrato da nuvem (PW Stream): {safe_name}...")
                    
                    try:
                        # O Playwright faz o GET herdando os cookies maravilhosamente e burla os redirects infinitos
                        resp = context.request.get(url, timeout=120000)
                        
                        if not resp.ok:
                            print(f"-> Erro ao baixar via PW. Código HTTP: {resp.status}")
                            continue
                            
                        body = resp.body()
                        
                        if len(body) < 1000000 and b'accounts.google.com' in body[:500]:
                            print("-> FALHA: O link encaminhou para página de login (cookies expirados).")
                            continue
                            
                        with open(dest_path, 'wb') as f:
                            f.write(body)
                            
                        # Atualizar index a cada sucesso
                        index_data.append({
                            "id": notebook_id,
                            "titulo": title,
                            "arquivo": safe_name,
                            "tipo": atype,
                            "originalYoutubeUrl": youtube_url
                        })
                        
                        with open(INDEX_FILE, "w", encoding="utf-8") as f:
                            json.dump(index_data, f, indent=2, ensure_ascii=False)
                            
                        print(f"-> Salvo com sucesso! ({len(body) // 1024 // 1024} MB)")
                        
                    except Exception as e:
                        print(f"-> Erro crítico ao fazer o stream com Playwright: {e}")
                        
            if not found_media:
                print(f"-> ATENÇÃO: Nenhuma mídia (Áudio/Vídeo) concluída encontrada.")
                with open("download_log.txt", "a", encoding="utf-8") as lf:
                    lf.write(f"[{idx}] {title} - PENDENTE\\n")
                    
        browser.close()

    print("\nPROCESSO FINALIZADO!")

if __name__ == "__main__":
    main()
