import os
import json
import urllib.request
import urllib.parse
import time
import ssl

ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE

api_key = None
with open('.env', 'r', encoding='utf-8') as f:
    for line in f:
        if line.startswith('YOUTUBE_API_KEY='):
            api_key = line.strip().split('=', 1)[1].strip('"\'')
            break

if not api_key:
    print("CHAVE YOUTUBE NÃO ENCONTRADA")
    exit(1)

def search_youtube(query):
    try:
        url = f'https://www.googleapis.com/youtube/v3/search?part=snippet&q={urllib.parse.quote(query)}&type=video&maxResults=1&key={api_key}'
        req = urllib.request.Request(url)
        with urllib.request.urlopen(req, context=ctx) as response:
            resp = json.loads(response.read().decode())
            if 'items' in resp and len(resp['items']) > 0:
                item = resp['items'][0]
                return {
                    'videoId': item['id']['videoId'],
                    'channelTitle': item['snippet']['channelTitle'],
                    'thumbnail': item['snippet']['thumbnails']['high']['url']
                }
    except Exception as e:
        print(f"Erro na busca: {e}")
    return None

if os.path.exists('index.json'):
    with open('index.json', 'r', encoding='utf-8') as f:
        data = json.load(f)
else:
    data = []

count = 0
for row in data:
    if 'originalYoutubeUrl' not in row:
        title = row.get("titulo", "")
        print(f"Buscando YT para: {title}")
        yt = search_youtube(title)
        if yt:
            row['originalYoutubeUrl'] = f"https://www.youtube.com/watch?v={yt['videoId']}"
            row['creatorName'] = yt['channelTitle']
            row['thumbUrl'] = yt['thumbnail']
            print(f"-> Encontrado: {yt['channelTitle']} | {row['originalYoutubeUrl']}")
            count += 1
        time.sleep(1)

with open('index.json', 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2, ensure_ascii=False)

print(f"\\nEnriquecimento do YT concluído! {count} vídeos atualizados.")
