import os
import json
import requests

AUTH_FILE = os.path.expanduser("~/.notebooklm-mcp/auth.json")
url = "https://lh3.googleusercontent.com/notebooklm/ANHLwAzbpssv_oIQVhOhiHgjE4Z9UQljlPFrH2SoLYylDi-y1hf0hGeRvqYRO0QyeVL4mjYlqR6Bxq_AZoVhhvaY6tbJs5HkSrAuFXvP2-3HqsvPT6Pcs3HOBNFE5Xuw-3d6gOMnyT3zo0ITHilg52-2Lfjs01cwryg=m22-dv"

def main():
    if not os.path.exists(AUTH_FILE):
        return
    with open(AUTH_FILE, "r") as f:
        auth_data = json.load(f)
    cookies = auth_data.get("cookies", {})
    
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
        "Accept": "video/webm,video/ogg,video/*;q=0.9,application/ogg;q=0.7,audio/*;q=0.6,*/*;q=0.5",
        "Accept-Language": "en-US,en;q=0.5",
        "Referer": "https://notebooklm.google.com/",
        "Sec-Fetch-Dest": "video",
        "Sec-Fetch-Mode": "no-cors",
        "Sec-Fetch-Site": "cross-site",
    }
    
    session = requests.Session()
    # Add cookies
    for k, v in cookies.items():
        session.cookies.set(k, v, domain=".google.com")
        
    print("Fetching video...")
    resp = session.get(url, headers=headers, allow_redirects=True, stream=True)
    print(f"Status: {resp.status_code}")
    print(f"Content-Type: {resp.headers.get('Content-Type')}")
    
    if "video" in resp.headers.get("Content-Type", ""):
        print("Success! Writing to file...")
        with open("test_vid_headers.mp4", "wb") as f:
            for chunk in resp.iter_content(chunk_size=8192):
                if chunk: f.write(chunk)
                break # only write a chunk
        print("Done.")
    else:
        print("Failed. Not a video.")
        print(resp.url)

if __name__ == "__main__":
    main()
