#!/usr/bin/env python3
"""CPU reference checks for rot edge continuity; NOT Unity/shader compilation.
Run: python3 tools/validate_rot_surface.py [--preview TEMP/rot-surface.png]
Preview optionally uses Pillow; default checks use only the standard library.
"""
import argparse
import itertools
import math
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SHADER = ROOT / 'FungusToast.Unity/Assets/Resources/Rot/RotSurface.shader'
SOURCE = SHADER.read_text()

def setting(name):
    return float(re.search(r'^\s*' + re.escape(name) + r'\s*\(.*\)\s*=\s*([\d.]+)', SOURCE, re.M).group(1))

RADIUS = setting('_CornerRadius')
INSET = setting('_EdgeInset')
ROUGHNESS = setting('_EdgeRoughness')
FEATHER = setting('_EdgeFeather')

def noise(x, y):
    def hashed(a, b):
        n = math.sin(a * 127.1 + b * 311.7) * 43758.5453
        return n - math.floor(n)
    ix, iy = math.floor(x), math.floor(y)
    fx, fy = x - ix, y - iy
    fx, fy = fx*fx*(3-2*fx), fy*fy*(3-2*fy)
    a = hashed(ix, iy)*(1-fx) + hashed(ix+1, iy)*fx
    b = hashed(ix, iy+1)*(1-fx) + hashed(ix+1, iy+1)*fx
    return a*(1-fy) + b*fy

def alpha(cells, x, y, u, v):
    left, right, bottom, top = [(x+dx, y+dy) not in cells for dx, dy in [(-1,0),(1,0),(0,-1),(0,1)]]
    d = min(u if left else 2, 1-u if right else 2, v if bottom else 2, 1-v if top else 2)
    for cx, cy, ex, ey in [(0,0,left,bottom),(1,0,right,bottom),(0,1,left,top),(1,1,right,top)]:
        if (x+(1 if cx else -1), y+(1 if cy else -1)) not in cells:
            d = min(d, math.hypot(u-cx, v-cy))
        if ex and ey and abs(u-cx) < RADIUS and abs(v-cy) < RADIUS:
            center_x = 1-RADIUS if cx else RADIUS
            center_y = 1-RADIUS if cy else RADIUS
            d = min(d, RADIUS-math.hypot(u-center_x, v-center_y))
    inset = INSET + ROUGHNESS * noise((x+u)*7, (y+v)*7)
    t = min(1, max(0, (d-inset)/FEATHER))
    return t*t*(3-2*t)

def checks():
    # Exhaust every surrounding arrangement for two horizontal neighbors, then transpose.
    fixed = {(1,1),(2,1)}
    optional = [(x,y) for x in range(4) for y in range(3) if (x,y) not in fixed]
    comparisons = 0
    for bits in itertools.product((False,True), repeat=len(optional)):
        cells = fixed | {p for p,b in zip(optional,bits) if b}
        for t in (0, .02, .06, .1, .25, .5, .75, .9, .94, .98, 1):
            assert abs(alpha(cells,1,1,1,t)-alpha(cells,2,1,0,t)) < 1e-9, (cells,t,'horizontal')
            transposed = {(y,x) for x,y in cells}
            assert abs(alpha(transposed,1,1,t,1)-alpha(transposed,1,2,t,0)) < 1e-9, (cells,t,'vertical')
            comparisons += 2
    single = {(0,0)}
    assert alpha(single,0,0,.5,.5) == 1
    assert alpha(single,0,0,0,0) == 0
    assert alpha(single,0,0,0,.5) == 0
    filled = {(x,y) for x in range(-1,2) for y in range(-1,2)}
    for u,v in itertools.product((0,.5,1), repeat=2):
        assert alpha(filled,0,0,u,v) == 1
    assert 0 < setting('_PulseAmplitude') <= .03
    assert setting('_PulsePeriod') >= 8
    assert 0 < setting('_DriftAmplitude') <= .02
    assert setting('_DriftPeriod') >= 20
    silhouette = SOURCE.split('float alpha =',1)[1].split(';',1)[0]
    assert '_Time' not in silhouette
    assert 'mesh.SetUVs(3, missingDiagonals)' in (ROOT/'FungusToast.Unity/Assets/Scripts/Unity/Grid/Helpers/RotSurfaceRenderer.cs').read_text()
    print(f'PASS: {comparisons} shared-edge alpha comparisons; isolated/interior coverage; stationary silhouette; restrained animation defaults.')
    print('CPU reference only: Unity C# and GPU shader compile/render still require Editor validation.')

def preview(path):
    from PIL import Image
    size = 32
    cells = {(x,y) for x in range(18) for y in range(8)
             if (x < 7 and 1 <= y <= 5 + (x%3==0)) or (4 <= x <= 13 and 3 <= y <= 4) or (11 <= x <= 16 and 2 <= y <= 5)}
    art = [Image.open(ROOT/f'FungusToast.Unity/Assets/Sprites/Tiles/Rot/rot_{i:02}.png').convert('RGBA') for i in range(1,6)]
    result = Image.new('RGB',(18*size,8*size),(217,197,151))
    for px in range(result.width):
        for py in range(result.height):
            bx, by = (px+.5)/size, 8-(py+.5)/size
            x,y = math.floor(bx), math.floor(by)
            if (x,y) not in cells: continue
            opacity = alpha(cells,x,y,bx-x,by-y)
            def mirrored(t): return .18 + .64*(1-abs((t*.5%1)*2-1))
            p = bx*.85 + setting('_TextureWarp')*(noise(bx*.7,by*.7)-.5)
            q = by*.85 + setting('_TextureWarp')*(noise(bx*.7+41.3,by*.7+41.3)-.5)
            phases = [(p,q),(-q+.37,p+.61),(p+.73,q+.19),(q+.23,-p+.83),(p+.57,q+.47)]
            variant = noise(bx*.23+13.7,by*.23+13.7)*4
            accum = [0.,0.,0.]; weight = 0.
            for i,image in enumerate(art):
                w = max(0,1-abs(variant-i))
                u,v = (mirrored(t) for t in phases[i])
                sample = image.getpixel((int(u*(image.width-1)),int((1-v)*(image.height-1))))
                w *= sample[3]/255
                weight += w
                for channel in range(3): accum[channel] += sample[channel]*w
            background = (217,197,151)
            color = tuple(round(accum[c]/max(weight,.001)*opacity + background[c]*(1-opacity)) for c in range(3))
            result.putpixel((px,py),color)
    path = Path(path)
    path.parent.mkdir(parents=True,exist_ok=True)
    result.save(path)
    print(f'CPU approximation preview (not an in-game capture): {path}')

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--preview')
    args = parser.parse_args()
    checks()
    if args.preview: preview(args.preview)
