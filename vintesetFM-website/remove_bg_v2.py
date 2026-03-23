from PIL import Image

def process_image(input_path, output_path):
    img = Image.open(input_path).convert("RGBA")
    width, height = img.size
    pixels = img.load()
    
    # Target color from top-left pixel
    target_color = pixels[0, 0]
    tolerance = 20 # Keep tolerance reasonable to avoid eating the white car
    
    def color_distance(c1, c2):
        return sum(abs(a - b) for a, b in zip(c1[:3], c2[:3]))
        
    # Mask to keep track of processed and transparent pixels
    mask = [[False]*height for _ in range(width)]
    
    # Start flood fill from the 4 corners
    stack = [(0, 0), (width-1, 0), (0, height-1), (width-1, height-1)]
    for sx, sy in stack:
        mask[sx][sy] = True
        
    while stack:
        x, y = stack.pop()
        
        # Check 4 neighbors
        for nx, ny in [(x-1, y), (x+1, y), (x, y-1), (x, y+1)]:
            if 0 <= nx < width and 0 <= ny < height and not mask[nx][ny]:
                c = pixels[nx, ny]
                if color_distance(c, target_color) <= tolerance:
                    mask[nx][ny] = True
                    stack.append((nx, ny))

    # Apply transparency
    for x in range(width):
        for y in range(height):
            if mask[x][y]:
                pixels[x, y] = (0, 0, 0, 0)
                
    # Crop the image to the bounding box of non-transparent pixels to make the car larger
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)
        
    img.save(output_path, "PNG")

import sys
if __name__ == '__main__':
    process_image(sys.argv[1], sys.argv[2])
