import requests
import time

last_seen = 0
print("Simulating OBS Overlay continuous polling...")

s = requests.Session()
while True:
    try:
        res = s.get(f"http://localhost:3000/api/reidamesa/overlay/poll?since={last_seen}", timeout=5)
        if res.status_code == 200:
            data = res.json()
            events = data.get('events', [])
            if events:
                print(f"[FOUND EVENTS] {events}")
                last_seen = max(e['id'] for e in events)
        else:
            print(f"Error HTTP {res.status_code}")
    except Exception as e:
        print(f"Connection error: {e}")
    time.sleep(2)
