from PIL import Image

def process_image(input_path, output_path):
    img = Image.open(input_path).convert("RGBA")
    
    # We will do an iterative flood fill from the corners
    mask = Image.new('L', img.size, 0)
    width, height = img.size
    pixels = img.load()
    mask_pixels = mask.load()
    
    target_color = pixels[0, 0]
    tolerance = 45 # slightly higher tolerance for JPEG artifacts
    
    def color_distance(c1, c2):
        return sum(abs(a - b) for a, b in zip(c1[:3], c2[:3]))
        
    stack = [(0, 0), (width-1, 0), (0, height-1), (width-1, height-1)]
    visited = set()
    
    while stack:
        x, y = stack.pop()
        if (x, y) in visited:
            continue
        visited.add((x, y))
        
        if color_distance(pixels[x, y], target_color) <= tolerance:
            mask_pixels[x, y] = 255 # set to be removed
            
            if x > 0: stack.append((x-1, y))
            if x < width - 1: stack.append((x+1, y))
            if y > 0: stack.append((x, y-1))
            if y < height - 1: stack.append((x, y+1))

    # Apply the mask to make pixels transparent
    for y in range(height):
        for x in range(width):
            if mask_pixels[x, y] == 255:
                pixels[x, y] = (0, 0, 0, 0) # completely transparent
                
    img.save(output_path, "PNG")

import sys
if __name__ == '__main__':
    process_image(sys.argv[1], sys.argv[2])
