import os
import json

downloads_dir = 'downloads'
index_file = 'index.json'

if os.path.exists(index_file):
    with open(index_file, 'r', encoding='utf-8') as f:
        data = json.load(f)
else:
    data = []

new_data = [item for item in data if item.get('tipo') == 'video' and item['arquivo'].endswith('.mp4')]

with open(index_file, 'w', encoding='utf-8') as f:
    json.dump(new_data, f, indent=2, ensure_ascii=False)

count = 0
if os.path.exists(downloads_dir):
    for f in os.listdir(downloads_dir):
        if f.endswith('.mp3'):
            os.remove(os.path.join(downloads_dir, f))
            count += 1

print(f'{count} arquivos MP3 removidos com sucesso. index.json atualizado.')
