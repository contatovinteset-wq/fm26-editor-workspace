import json
import requests
import re

new_urls = [
    "https://youtu.be/PfRsDFNLFOc",
    "https://youtu.be/kqTnZPgy6-o",
    "https://youtu.be/KUUG27B6_as",
    "https://youtu.be/NKc2ZaBCOPI",
    "https://youtu.be/9umqtxtKKWI",
    "https://youtu.be/S7PiXXQxrek",
    "https://youtu.be/MTCAs_BXzTk",
    "https://youtu.be/Ii46kz6oSWU",
    "https://youtu.be/dVUr0pUHhZI",
    "https://youtu.be/-VNZY7LF99A"
]

yt_mapping = {}

print("Buscando titles no Youtube...")
for url in new_urls:
    try:
        resp = requests.get(url, timeout=10)
        match = re.search(r'<title>(.*?)</title>', resp.text)
        if match:
            title = match.group(1).replace(" - YouTube", "").strip()
            yt_mapping[url] = title
            print(f"{url} -> {title}")
        else:
            print(f"{url} -> Nao encontrou titulo")
    except Exception as e:
        print(f"Erro no {url}: {e}")

with open('yt_titles.json', 'w', encoding='utf-8') as f:
    json.dump(yt_mapping, f, ensure_ascii=False, indent=2)
