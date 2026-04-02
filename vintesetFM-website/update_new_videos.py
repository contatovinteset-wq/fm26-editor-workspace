import json
import urllib.request
import re

urls = [
    "https://youtu.be/5qvk3AFWwqY",
    "https://youtu.be/pqKhCyZ_Z00",
    "https://youtu.be/HxJ8J4IwsU4",
    "https://youtu.be/HwwnNWNQ1NM",
    "https://youtu.be/cGEIb1hPlZI",
    "https://youtu.be/DhT-GMQ4WO0",
    "https://youtu.be/SgzdOF11Mbs",
    "https://youtu.be/buKzI3Gp8t8",
    "https://youtu.be/5Ty_IDTLJ9o"
]

def get_yt_title(url):
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    try:
        html = urllib.request.urlopen(req).read().decode('utf-8')
        match = re.search(r'<title>(.*?)</title>', html)
        if match:
            title = match.group(1).replace(' - YouTube', '').strip()
            title = title.replace('&#39;', "'").replace('&amp;', '&').replace('&quot;', '"')
            return title
    except Exception as e:
        print(f"Error fetching {url}: {e}")
    return None

def normalize(t):
    t = re.sub(r'^\d+\s*', '', t)
    return t.lower().replace(':', '').replace('-', '').replace(' ', '')

with open('src/data/index.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

for url in urls:
    title = get_yt_title(url)
    print(f"URL: {url}")
    print(f"Title: {title}")
    if title:
        matched = False
        for item in data:
            if 'titulo' in item and normalize(item['titulo']) == normalize(title):
                item['uploadedYoutubeUrl'] = url
                matched = True
                print(" -> Matched and updated!")
                break
        if not matched:
            print(" -> NO MATCH FOUND!")

with open('src/data/index.json', 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2, ensure_ascii=False)
