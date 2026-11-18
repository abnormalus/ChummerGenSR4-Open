"""Render the code-owned dossier icon with only Python's standard library."""
from pathlib import Path
import struct
import zlib

size = 1024
pixels = bytearray(size * size * 3)
for y in range(size):
    for x in range(size):
        t = y / size
        i = (y * size + x) * 3
        pixels[i:i+3] = bytes((int(9-4*t), int(101-58*t), int(116-61*t)))

def rectangle(x0, y0, x1, y1, color, radius=0):
    for y in range(y0, y1):
        for x in range(x0, x1):
            dx = max(x0 + radius - x, 0, x - (x1 - radius - 1))
            dy = max(y0 + radius - y, 0, y - (y1 - radius - 1))
            if dx*dx + dy*dy <= radius*radius:
                i = (y * size + x) * 3
                pixels[i:i+3] = bytes(color)

white = (245, 252, 253)
teal = (10, 74, 86)
rectangle(218, 150, 806, 874, white, 45)
rectangle(260, 207, 764, 550, teal, 22)
# A compact stencil alphabet remains readable at home-screen icon size.
glyphs = {'S': ['11111','10000','10000','11111','00001','00001','11111'],
          'R': ['11110','10001','10001','11110','10100','10010','10001'],
          '4': ['10010','10010','10010','11111','00010','00010','00010']}
scale = 25
for letter, left in zip('SR4', [290, 452, 614]):
    for y, row in enumerate(glyphs[letter]):
        for x, filled in enumerate(row):
            if filled == '1': rectangle(left+x*scale, 290+y*scale, left+(x+1)*scale, 290+(y+1)*scale, white)
for top, width in [(618, 470), (690, 470), (762, 300)]:
    rectangle(275, top, 275+width, top+23, teal, 10)

def chunk(kind, data):
    return struct.pack('!I', len(data)) + kind + data + struct.pack('!I', zlib.crc32(kind+data))
raw = b''.join(b'\0'+pixels[y*size*3:(y+1)*size*3] for y in range(size))
png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('!2I5B', size,size,8,2,0,0,0)) + chunk(b'IDAT', zlib.compress(raw,9)) + chunk(b'IEND', b'')
path = Path(__file__).resolve().parents[1] / 'App/Assets.xcassets/AppIcon.appiconset/AppIcon.png'
path.write_bytes(png)
print(path)
