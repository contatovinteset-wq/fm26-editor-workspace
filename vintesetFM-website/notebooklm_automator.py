import os
import json
import time
import re
from playwright.sync_api import sync_playwright
from notebooklm_mcp.api_client import NotebookLMClient

SESSION_FILE = "notebooklm_session.json"
DOWNLOADS_DIR = "downloads"
INDEX_FILE = "index.json"

def get_notebooks():
    AUTH_FILE = os.path.expanduser("~/.notebooklm-mcp/auth.json")
    if not os.path.exists(AUTH_FILE):
        print("ERRO: ~/.notebooklm-mcp/auth.json não encontrado.")
        print("Por favor, rode o comando `notebooklm-mcp-auth` no terminal para gerar a autenticação primária.")
        return []
        
    with open(AUTH_FILE, "r") as f:
        auth_data = json.load(f)
        
    client = NotebookLMClient(cookies=auth_data.get("cookies", {}))
    return client.list_notebooks()

def sanitize_filename(name):
    return re.sub(r'[\\/*?:"<>|]', "", name).replace(" ", "_").lower()[0:100]

def main():
    os.makedirs(DOWNLOADS_DIR, exist_ok=True)
    SESSION_FILE = "notebooklm_session_final.json"
    
    # 1. PEGAR A LISTA OFICIAL VIA MCP (Funciona perfeitamente!)
    AUTH_FILE = os.path.expanduser("~/.notebooklm-mcp/auth.json")
    if not os.path.exists(AUTH_FILE):
        print("ERRO: auth.json não encontrado. Rode `notebooklm-mcp-auth` no terminal primeiro.")
        return
        
    print("\n--- Recuperando Lista Oficial de Cadernos ---")
    try:
        notebooks = get_notebooks()
        notebooks = [nb for nb in notebooks if nb.id and hasattr(nb, 'title') and nb.title]
        print(f"Encontrados {len(notebooks)} cadernos reais da sua conta (Criados + Compartilhados)!")
    except Exception as e:
        print(f"Erro ao obter lista de cadernos pela API interna: {e}")
        return
        
    if len(notebooks) == 0:
        print("Nenhum caderno encontrado. Encerrando.")
        return

    # 2. INICIAR PLAYWRIGHT PARA O NAVEGADOR
    with sync_playwright() as p:
        browser = p.chromium.launch(
            headless=False,
            args=["--disable-blink-features=AutomationControlled"],
            ignore_default_args=["--enable-automation"]
        )
        
        if os.path.exists(SESSION_FILE):
            print("Sessão do Navegador encontrada. Carregando...")
            context = browser.new_context(
                storage_state=SESSION_FILE,
                accept_downloads=True,
                user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
            )
        else:
            print("Sessão do Navegador NÃO encontrada. Preparando Login interativo.")
            context = browser.new_context(
                accept_downloads=True,
                user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
            )
            
        page = context.new_page()
        
        # Faz p login manual 1 vez se não existir
        if not os.path.exists(SESSION_FILE):
            page.goto("https://notebooklm.google.com")
            print("=========================================================")
            print(" Faça o Login no Google NA JANELA QUE ABRIR.")
            print(" Chegue até a tela inicial do NotebookLM.")
            print("=========================================================")
            input("PRESSIONE ENTER AQUI NO TERMINAL QUANDO ESTIVER LOGADO...")
            context.storage_state(path=SESSION_FILE)
            print("Sessão do Chrome salva com sucesso!")

        index_data = []
        if os.path.exists(INDEX_FILE):
            with open(INDEX_FILE, "r", encoding="utf-8") as f:
                index_data = json.load(f)
                
        for idx, nb in enumerate(notebooks, 1):
            url = f"https://notebooklm.google.com/notebook/{nb.id}"
            raw_title = nb.title
            try:
                print(f"\n[{idx}/{len(notebooks)}] Acessando: {raw_title}")
                
                # Ignorar cadernos de exemplo do Google
                if "Introduction to" in raw_title or "Bracket Guide" in raw_title or "Google I/O" in raw_title:
                    print("-> Caderno de exemplo do Google. Pulando.")
                    continue
                
                # Check if already processed
                if any(m.get("id") == url for m in index_data):
                    print("-> Já processado no index.json. Pulando.")
                    continue
                    
                page.goto(url, wait_until="domcontentloaded", timeout=20000)
                time.sleep(5) # allow UI React components to settle
                
                # Open Studio Drawer if not open (Studio, Estúdio, Visão geral)
                studio_btn = page.get_by_text(re.compile("Studio|Estúdio|Visão", re.IGNORECASE))
                if studio_btn.count() > 0 and studio_btn.first.is_visible():
                    studio_btn.first.click()
                    time.sleep(2)
                elif page.get_by_label(re.compile("Studio|Estúdio|Visão", re.IGNORECASE)).count() > 0:
                    page.get_by_label(re.compile("Studio|Estúdio|Visão", re.IGNORECASE)).first.click()
                    time.sleep(2)
                    
                time.sleep(3)
                
                # NOVO FLUXO ABSOLUTAMENTE SEGURO (Y > 80)
                download_regex = re.compile("download|baixar|fazer|transferir|salvar", re.IGNORECASE)
                download_found = False
                target_btn = None
                
                # Pegar todos os botões da página
                all_buttons = page.locator('button, [role="button"]')
                
                # Vamos tentar clicar apenas em botões que sejam ícones (SVG) ou play
                for i in range(all_buttons.count()):
                    btn = all_buttons.nth(i)
                    if not btn.is_visible():
                        continue
                        
                    box = btn.bounding_box()
                    if not box:
                        continue
                        
                    # IGNORAR O CABEÇALHO (Y < 80px) -> Ignora Perfil, Configurações, Criar Notebook
                    if box['y'] < 80:
                        continue
                        
                    # IGNORAR FOTOS DE PERFIL (img)
                    if btn.locator('img').count() > 0:
                        continue
                        
                    # Filtrar pelo texto (se tiver texto longo que não seja play, pula)
                    text = btn.inner_text().strip().lower()
                    if text and len(text) > 3 and not any(kw in text for kw in ["play", "reproduz", "opç", "mais"]):
                        continue
                        
                    # Precisamos garantir que seja um ícone util (Play ou Kebab)
                    if btn.locator('svg').count() == 0:
                        continue
                        
                    # Clicar!
                    try:
                        btn.click(timeout=1000)
                        time.sleep(1.5)
                    except: pass
                    
                    # Verificar se o download apareceu no DOM
                    d_btns = page.get_by_role("button", name=download_regex)
                    if d_btns.count() == 0:
                        d_btns = page.get_by_text(download_regex)
                        
                    if d_btns.count() > 0:
                        for j in range(d_btns.count()):
                            if d_btns.nth(j).is_visible():
                                target_btn = d_btns.nth(j)
                                download_found = True
                                break
                                
                    if download_found:
                        break
                        
                if download_found and target_btn:
                    print(f"-> Ouro encontrado! Iniciando interceptação de download...")
                    
                    try:
                        with page.expect_download(timeout=60000) as download_info:
                            target_btn.click(timeout=3000)
                        
                        download = download_info.value
                        orig_name = download.suggested_filename
                        ext = ".mp4" if "mp4" in orig_name.lower() or "video" in orig_name.lower() else ".mp3"
                        
                        safe_name = f"{idx:02d}_{sanitize_filename(raw_title)}{ext}"
                        dest_path = os.path.join(DOWNLOADS_DIR, safe_name)
                        
                        print(f"-> Salvando arquivo como: {safe_name}...")
                        download.save_as(dest_path)
                        
                        index_data.append({
                            "id": url,
                            "titulo": raw_title,
                            "arquivo": safe_name,
                            "tipo": "video" if ext == ".mp4" else "audio"
                        })
                        
                        with open(INDEX_FILE, "w", encoding="utf-8") as f:
                            json.dump(index_data, f, indent=2, ensure_ascii=False)
                            
                        print("-> Sucesso absoluto!")
                    except Exception as e:
                        print(f"-> Falha ao acionar o download: {e}")
                        with open("download_log.txt", "a", encoding="utf-8") as lf:
                            lf.write(f"[{idx}] {raw_title} - ERRO DOWNLOAD: {str(e)}\n")
                    
                else:
                    print("-> ATENÇÃO: Nenhum botão de download visível após tentar abrir todas as opções para este caderno.")
                    with open("download_log.txt", "a", encoding="utf-8") as lf:
                        lf.write(f"[{idx}] {raw_title} - PENDENTE (Nenhum video/audio encontrado)\n")
            except Exception as e:
                print(f"-> FALHA na iteração do caderno {url}: {e}")
                with open("download_log.txt", "a", encoding="utf-8") as lf:
                    lf.write(f"[{idx}] {url} - ERRO: {str(e)}\n")
                    
            time.sleep(2) # Respiro entre cadastros
            
        print("\nVarredura finalizada!")
        browser.close()

if __name__ == "__main__":
    main()
