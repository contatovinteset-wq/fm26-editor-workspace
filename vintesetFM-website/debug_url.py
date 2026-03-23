import os
import json
from notebooklm_mcp.api_client import NotebookLMClient

AUTH_FILE = os.path.expanduser("~/.notebooklm-mcp/auth.json")

def main():
    if not os.path.exists(AUTH_FILE):
        print(f"ERRO: Arquivo de autenticação não encontrado em {AUTH_FILE}")
        return

    with open(AUTH_FILE, "r") as f:
        auth_data = json.load(f)
    
    cookies = auth_data.get("cookies", {})
    client = NotebookLMClient(cookies=cookies)
        
    notebooks = client.list_notebooks()
    print(f"Total: {len(notebooks)} cadernos.")
    
    for i, nb in enumerate(notebooks, 1):
        if nb.title == "O Novo Raumdeuter: Recriando a Função no FM26" or "Raumdeuter" in nb.title:
            artifacts = client.poll_studio_status(nb.id)
            for art in artifacts:
                if art.get("type") == "video" and art.get("status") == "completed":
                    video_url = art.get("video_url", "")
                    print(f"Found Video URL for {nb.title}:")
                    print(video_url)
                    return
        
if __name__ == "__main__":
    main()
