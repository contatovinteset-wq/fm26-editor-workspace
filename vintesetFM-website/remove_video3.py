import json

with open('src/data/index.json', 'r', encoding='utf-8') as f:
    items = json.load(f)

# The problematic video has the uploaded URL: https://youtu.be/KUUG27B6_as
for item in items:
    if item.get('uploadedYoutubeUrl') == "https://youtu.be/KUUG27B6_as":
        # Remove it from Telecurso display by nullifying these attributes
        item['uploadedYoutubeUrl'] = ""
        item['thumbUrl'] = ""
        print("Video '{0}' removido da vitrine do Telecurso.".format(item.get('titulo')))

with open('src/data/index.json', 'w', encoding='utf-8') as f:
    json.dump(items, f, ensure_ascii=False, indent=2)
