import json

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

with open('src/data/index.json', 'r', encoding='utf-8') as f:
    items = json.load(f)

idx = 0
for item in items:
    if item.get('tipo', '') == 'video' and item.get('uploadedYoutubeUrl'):
        if idx < len(new_urls):
            print(f"Update: {item['titulo']} -> {new_urls[idx]}")
            item['uploadedYoutubeUrl'] = new_urls[idx]
            idx += 1

with open('src/data/index.json', 'w', encoding='utf-8') as f:
    json.dump(items, f, ensure_ascii=False, indent=2)
