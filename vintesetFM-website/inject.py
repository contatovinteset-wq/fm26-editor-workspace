import json
import os

urls = [
    'https://youtu.be/v6-Ur2bLwlA',
    'https://youtu.be/ayZTzvvgvlQ',
    'https://youtu.be/GQjIaDshtHM',
    'https://youtu.be/_bxjDgtSZ1s',
    'https://youtu.be/b3PUdn8OLpk',
    'https://youtu.be/xgnEp7C7Vk8',
    'https://youtu.be/lKcnWY088-I',
    'https://youtu.be/A71sGZWTBt4',
    'https://youtu.be/nMDFdJUKmdg',
    'https://youtu.be/kPvCqq59tcg'
]

json_path = 'index.json'
with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

url_idx = 0
for item in data:
    if item.get('tipo', '') == 'video' and url_idx < len(urls):
        item['uploadedYoutubeUrl'] = urls[url_idx]
        url_idx += 1

with open(json_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2, ensure_ascii=False)

print(f'Successfully injected {url_idx} URLs into {json_path}.')
