import requests

url = "https://lh3.googleusercontent.com/notebooklm/ANHLwAzbpssv_oIQVhOhiHgjE4Z9UQljlPFrH2SoLYylDi-y1hf0hGeRvqYRO0QyeVL4mjYlqR6Bxq_AZoVhhvaY6tbJs5HkSrAuFXvP2-3HqsvPT6Pcs3HOBNFE5Xuw-3d6gOMnyT3zo0ITHilg52-2Lfjs01cwryg=m22-dv"

resp = requests.get(url, allow_redirects=True)
print(f"Status Code: {resp.status_code}")
print(f"Headers: {resp.headers}")
if resp.status_code == 200:
    out = "test_vid.mp4"
    with open(out, "wb") as f:
        f.write(resp.content[:1024*1024]) # first MB
    print(f"Saved first MB to {out}")
else:
    print(f"Failed: {resp.text[:200]}")
