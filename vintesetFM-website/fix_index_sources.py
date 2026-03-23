import json
import os
import re
from notebooklm_mcp import api_client

AUTH_FILE = os.path.expanduser("~/.notebooklm-mcp/auth.json")

def extract_sources(notebook_data):
    # This will return a list of dicts: [{'title': '...', 'url': '...'}]
    sources = []
    if not notebook_data or not isinstance(notebook_data, list) or len(notebook_data) == 0:
        return sources
    if not isinstance(notebook_data[0], list) or len(notebook_data[0]) < 2:
        return sources
        
    for src in notebook_data[0][1]:
        if len(src) >= 3:
            src_title = src[1] if len(src) > 1 and isinstance(src[1], str) else "Unknown"
            
            src_url = ""
            if isinstance(src[2], list):
                metadata = src[2]
                for item in metadata:
                    if isinstance(item, list) and len(item) > 0 and isinstance(item[0], str):
                        if item[0].startswith("http"):
                            src_url = item[0]
                    elif isinstance(item, str) and item.startswith("http"):
                        src_url = item
                        
                # Some notebooks might have unstructured urls in other places
                if not src_url and len(metadata) > 7 and isinstance(metadata[7], list) and len(metadata[7]) > 0:
                    potential_url = metadata[7][0]
                    if isinstance(potential_url, str) and potential_url.startswith("http"):
                        src_url = potential_url

            sources.append({
                "title": src_title,
                "url": src_url,
                "isYoutube": "youtube" in src_url.lower() or "youtu.be" in src_url.lower() if src_url else False
            })
    return sources

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
    
    title_to_sources = {}
    
    print(f"Encontrados {len(notebooks)} cadernos. Extraindo fontes...")
    for idx, nb in enumerate(notebooks, 1):
        if not nb.title: continue
        print(f"[{idx}/{len(notebooks)}] Processando {nb.title} ...")
        try:
            detailed = client.get_notebook(nb.id)
            sources = extract_sources(detailed)
            title_to_sources[nb.title.strip()] = {
                "id": nb.id,
                "sources": sources
            }
        except Exception as e:
            print(f"Erro ao obter {nb.title}: {e}")

    with open('sources_map.json', 'w', encoding='utf-8') as f:
        json.dump(title_to_sources, f, ensure_ascii=False, indent=2)

    print("Mapeamento salvo em sources_map.json")

    # Update index.json exactly based on the FIRST valid URL found
    index_path = 'src/data/index.json'
    with open(index_path, 'r', encoding='utf-8') as f:
        items = json.load(f)

    updated = 0
    for item in items:
        if item.get('tipo', '') != 'video':
            continue
            
        title = item.get('titulo', '').strip()
        
        mapped = title_to_sources.get(title)
        if not mapped:
            for k, v in title_to_sources.items():
                if k.lower() in title.lower() or title.lower() in k.lower():
                    mapped = v
                    break

        if mapped and mapped.get("sources"):
            # find first valid url
            best_url = ""
            creator_name = "NotebookLM"
            
            for src in mapped["sources"]:
                if src["url"]:
                    best_url = src["url"]
                    if src["isYoutube"]:
                        # try to guess creator from the title if it contains it, or leave generic Let the user fix
                        # Often the youtube video title doesn't have the creator. Let's rely on what was previously set if it existed
                        pass
                    break
            
            if best_url:
                item['originalYoutubeUrl'] = best_url # We can rename it to just sourceUrl in the future
                item['id'] = mapped['id']
                print(f"Atualizado {title} -> {best_url}")
                updated += 1
                
    with open(index_path, 'w', encoding='utf-8') as f:
        json.dump(items, f, ensure_ascii=False, indent=2)

    print(f"Index.json atualizado com {updated} itens.")

if __name__ == "__main__":
    main()
