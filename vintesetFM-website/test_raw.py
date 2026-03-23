import os
import json
import requests

AUTH_FILE = os.path.expanduser("~/.notebooklm-mcp/auth.json")

def get_cookie_string():
    if os.path.exists(AUTH_FILE):
        with open(AUTH_FILE, "r") as f:
            data = json.load(f)
            cookies = data.get("cookies", {})
            return "; ".join([f"{k}={v}" for k, v in cookies.items()])
    return ""

cookie = get_cookie_string()
headers = {
    "Cookie": cookie,
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
    "Accept": "application/json"
}

res = requests.get("https://notebooklm.google.com/api/v1/notebooklm/notebooks", headers=headers)
print("Status:", res.status_code)
try:
    data = res.json()
    print("Notebooks:", len(data.get("notebooks", [])))
except Exception as e:
    print("Not JSON:", res.text[:200])
