import os
import json
import httpx
from notebooklm_mcp import api_client

print('Iniciando teste de download...')
auth_data = json.load(open(os.path.expanduser('~/.notebooklm-mcp/auth.json')))
cookies = auth_data.get('cookies', {})

jar = httpx.Cookies()
for k, v in cookies.items():
    jar.set(k, v, domain=".google.com")

headers = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
    "Accept-Language": "en-US,en;q=0.5",
    "Referer": "https://notebooklm.google.com/",
    "Sec-Fetch-Dest": "document",
    "Sec-Fetch-Mode": "navigate",
    "Sec-Fetch-Site": "cross-site",
}

client = api_client.NotebookLMClient(cookies=cookies, session_id=auth_data.get('session_id', ''))
artifacts = client.poll_studio_status('e4e71530-a4d2-411e-b9a3-b3d7bdf3074c')
url = None
if artifacts:
    for a in artifacts:
        if a.get('video_url'):
            url = a['video_url']
            break

if url is None:
    print('Nenhum URL encontrado')
    exit(1)

print(f"URL: {url[:60]}...")

with httpx.Client(cookies=jar, headers=headers, follow_redirects=True, timeout=120) as hc:
    r = hc.get(url)
    with open('teste.mp4', 'wb') as f:
        f.write(r.content)

size = os.path.getsize('teste.mp4')
print(f"Tamanho: {size} bytes")

with open('teste.mp4', 'r', encoding='utf-8', errors='ignore') as f:
    text = f.read(200)
    print("Início:", text)
