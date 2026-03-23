import json
import os
import re
from notebooklm_mcp import api_client

AUTH_FILE = os.path.expanduser("~/.notebooklm-mcp/auth.json")

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
        print(f"ERRO: Arquivo de autenticação não encontrado: {AUTH_FILE}")
        return

    with open(AUTH_FILE, "r") as f:
        auth_data = json.load(f)

    client = api_client.NotebookLMClient(
        cookies=auth_data.get('cookies', {}),
        csrf_token=auth_data.get('csrf_token', ''),
        session_id=auth_data.get('session_id', '')
    )

    print("Obtendo notebooks carregados na conta do NotebookLM...")
    notebooks = client.list_notebooks()
    
    # Criar mapeamento do título para ID e URL real
    title_to_yt = {}
    print("Extraindo fontes do youtube para seus cadernos...")
    for nb in notebooks:
        try:
            detailed = client.get_notebook(nb.id)
            yt_url = extract_youtube_url(detailed)
            title_to_yt[nb.title.strip()] = {
                "id": nb.id,
                "url": yt_url
            }
        except Exception as e:
            print(f"Erro ao obter {nb.title}: {e}")

    index_path = 'src/data/index.json'
    with open(index_path, 'r', encoding='utf-8') as f:
        items = json.load(f)

    updated = 0
    for item in items:
        if item.get('tipo', '') != 'video':
            continue
            
        title = item.get('titulo', '').strip()
        
        # Tentativa de matching exato
        mapped = title_to_yt.get(title)
        
        # Tentativa de matching parcial
        if not mapped:
            for k, v in title_to_yt.items():
                if k.lower() in title.lower() or title.lower() in k.lower():
                    mapped = v
                    break

        if mapped and mapped["url"]:
            item['originalYoutubeUrl'] = mapped["url"]
            # Para o CreatorName não temos exposto via API do Google de forma fáci,
            # então mantemos os mappings anteriores se existirem ou colocamos genérico
            # mas agora temos o link real.
            # O creatorName já foi preenchido anteriormente de forma manual, vamos focar na URL
            updated += 1
            print(f"Atualizado: {title} -> {mapped['url']}")
            item['id'] = mapped['id'] # Atualizar também com o ID fixo do notebook

    with open(index_path, 'w', encoding='utf-8') as f:
        json.dump(items, f, ensure_ascii=False, indent=2)

    print(f'Successfully updated {updated} items in index.json using the REAL NotebookLM API sources!!!')

if __name__ == "__main__":
    main()
