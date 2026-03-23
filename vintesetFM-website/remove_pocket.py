from PIL import Image
import sys

def process(img_path):
    img = Image.open(img_path).convert("RGBA")
    w, h = img.size
    pixels = img.load()
    
    cx = w // 2
    seed_point = None
    
    # 1. Look for the pocket by scanning UP from the bottom at the middle of the image
    padding_bottom = h - 1
    for y in range(padding_bottom, int(h * 0.5), -1):
        r, g, b, a = pixels[cx, y]
        # Look for the first pixel that is opaque and bright (the white/grey pocket)
        if a > 100:
            brightness = (r + g + b) / 3
            if brightness > 150: # It's a bright pixel!
                seed_point = (cx, y)
                break
                
    if not seed_point:
        print("No suitable pocket found.")
        sys.exit(0)
        
    print(f"Found pocket seed at {seed_point}")
    target_color = pixels[seed_point]
    
    # 2. Flood fill this pocket and make it transparent
    tolerance = 50
    def dist(c1, c2):
        return sum(abs(a-b) for a,b in zip(c1[:3], c2[:3]))
        
    stack = [seed_point]
    visited = set()
    
    while stack:
        x, y = stack.pop()
        if (x, y) in visited:
            continue
        visited.add((x, y))
        
        c = pixels[x, y]
        # If it's not already transparent and color is similar to the pocket color
        if c[3] > 0 and dist(c, target_color) <= tolerance:
            pixels[x, y] = (0, 0, 0, 0) # set transparent
            
            if x > 0: stack.append((x-1, y))
            if x < w - 1: stack.append((x+1, y))
            if y > 0: stack.append((x, y-1))
            if y < h - 1: stack.append((x, y+1))
            
    img.save(img_path, "PNG")
    print("Pocket removed successfully.")

if __name__ == '__main__':
    process(sys.argv[1])
