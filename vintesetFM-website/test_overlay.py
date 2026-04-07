import requests
import time

s = requests.Session()

# Login as admin to get cookies if needed? No, /api/reidamesa/overlay/poll is public
last_seen = 0
print("Polling API...")

# Poll 1
res = s.get(f"http://localhost:3000/api/reidamesa/overlay/poll?since={last_seen}")
print(f"Poll 1: {res.json()}")

# Emulate user triggering an event 
# We don't have login easily, wait, I can just use a test script to trigger the event directly on the backend

