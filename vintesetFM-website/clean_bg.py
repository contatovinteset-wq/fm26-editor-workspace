from PIL import Image
import sys
from collections import Counter

def process(in_p, out_p):
    img = Image.open(in_p).convert("RGBA")
    w, h = img.size
    pixels = img.load()
    
    # 1. FIND BACKGROUND COLOR
    # Sample pixels 5 pixels away from the edge to avoid 1px AI-generated frames
    border_pixels = []
    for x in range(w):
        border_pixels.append(pixels[x, 5])
        border_pixels.append(pixels[x, h-6])
    for y in range(h):
        border_pixels.append(pixels[5, y])
        border_pixels.append(pixels[w-6, y])
        
    # Most common colors among the border sample
    bg_color = Counter(border_pixels).most_common(1)[0][0]
    
    # 2. FLOOD FILL
    mask = [[False]*h for _ in range(w)]
    stack = []
    dist_tolerance = 60
    
    def dist(c1, c2): 
        return sum(abs(a-b) for a,b in zip(c1[:3], c2[:3]))
    
    # Seed the boundaries
    for x in range(w):
        if dist(pixels[x, 0], bg_color) <= dist_tolerance:
            stack.append((x, 0))
            mask[x][0] = True
        if dist(pixels[x, h-1], bg_color) <= dist_tolerance:
            stack.append((x, h-1))
            mask[x][h-1] = True
            
    for y in range(h):
        if dist(pixels[0, y], bg_color) <= dist_tolerance:
            stack.append((0, y))
            mask[0][y] = True
        if dist(pixels[w-1, y], bg_color) <= dist_tolerance:
            stack.append((w-1, y))
            mask[w-1][y] = True
            
    while stack:
        x, y = stack.pop()
        for nx, ny in [(x-1,y), (x+1,y), (x,y-1), (x,y+1)]:
            if 0 <= nx < w and 0 <= ny < h and not mask[nx][ny]:
                if dist(pixels[nx, ny], bg_color) <= dist_tolerance:
                    mask[nx][ny] = True
                    stack.append((nx, ny))
                    
    # 3. APPLY TRANSPARENCY
    for x in range(w):
        for y in range(h):
            if mask[x][y]:
                pixels[x, y] = (0,0,0,0)
                
    # 4. CROP TO CONTENT
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)
        
    # 5. SAVE
    img.save(out_p, "PNG")

if __name__ == '__main__':
    process(sys.argv[1], sys.argv[2])
