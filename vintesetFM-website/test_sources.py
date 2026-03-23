import json
import os
from notebooklm_mcp import api_client

auth_data = json.load(open(os.path.expanduser('~/.notebooklm-mcp/auth.json')))
client = api_client.NotebookLMClient(cookies=auth_data.get('cookies', {}), session_id=auth_data.get('session_id', ''))
notebooks = client.list_notebooks()
for nb in notebooks[:3]:
    sources = client.list_sources(nb.id)
    print(f'Notebook: {nb.title}')
    for s in sources:
        print(f'  Source title: {s.title}')
        print(f'  Source URL: {getattr(s, "url", "No URL")}')
        print(f'  Source type: {s.source_type}')
