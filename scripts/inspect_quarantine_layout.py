#!/usr/bin/env python3
"""Actual asset mask + source-authored layout preview; not Unity rendering/gameplay evidence."""
import argparse
import json
import re
from pathlib import Path
import validate_board_backgrounds as v

ROOT = Path(__file__).resolve().parents[1]

def layout():
    path = ROOT/'FungusToast.Unity/Assets/Configs/Toast Configs/ToastBoardMedium.asset'
    asset = v.load_unity_yaml(path)['MonoBehaviour']
    guid_map = v.build_sprite_guid_map(ROOT/'FungusToast.Unity/Assets/Sprites/UI/Bread Backgrounds')
    metadata = v.build_metadata_map(asset,guid_map)
    override = next(o for o in asset['boardBackgroundOverrides'] if o.get('minBoardWidth') == 120)
    settings = v.build_settings(override,metadata,guid_map,'quarantine','stage12',120,120,120,120)
    bounds = v.get_effective_safe_area(settings,120,120)
    tester = v.get_playable_outline_tester(settings)
    offsets = v.build_clip_budget_sample_offsets(v.PLAYABLE_SURFACE_TILE_SCALE,settings.max_tile_clip_fraction,settings.tile_clip_sample_resolution)
    blocked = v.build_outline_blocked_tile_ids(tester,bounds,120,120,settings.min_tile_coverage,offsets)
    source = (ROOT/'FungusToast.Core/Board/QuarantineRotLayout.cs').read_text()
    lane_source = source.split('CorridorLanes =',1)[1].split('};',1)[0]
    lanes = [tuple(map(int,m)) for m in re.findall(r'\((\d+), (\d+), (\d+), (\d+)\)',lane_source)]
    starts_source = source.split('AuthoredStarts =',1)[1].split('};',1)[0]
    starts = [tuple(map(int,m)) for m in re.findall(r'\((\d+), (\d+)\)',starts_source)]
    balance = (ROOT/'FungusToast.Core/Config/RotBalance.cs').read_text()
    def constant(name):return int(re.search(r'const int '+name+r' = (\d+);',balance).group(1))
    belt,pocket_x = constant('QuarantineBeltStartX'),constant('QuarantinePocketStartX')
    playable = set(range(120*120))-blocked
    corridor = {y*120+x for left,bottom,right,top in lanes for y in range(bottom,top+1) for x in range(left,right+1)}
    pocket = {i for i in playable if i%120 >= pocket_x}
    thin_x = constant('QuarantineOuterBeltStartX')
    bottom,top = constant('QuarantineBulgeBottomY'),constant('QuarantineBulgeTopY')
    rot = {i for i in playable if i%120 < pocket_x and (i%120 >= thin_x or (i%120 >= belt and bottom <= i//120 <= top))}-corridor
    assert corridor <= playable
    assert len(playable)==5011
    assert 0.14 <= len(pocket)/len(playable) <= 0.16
    return blocked,playable,rot,pocket,corridor,starts

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--output-dir',type=Path,default=ROOT/'TEMP/quarantine-layout')
    args=parser.parse_args()
    blocked,playable,rot,pocket,corridor,starts=layout()
    args.output_dir.mkdir(parents=True,exist_ok=True)
    (args.output_dir/'blocked-tiles.txt').write_text(','.join(map(str,sorted(blocked)))+'\n')
    stats=dict(playableTiles=len(playable),pocketTiles=len(pocket),pocketFraction=len(pocket)/len(playable),rotTiles=len(rot),corridorTiles=len(corridor),startingPositions=starts)
    (args.output_dir/'layout-stats.json').write_text(json.dumps(stats,indent=2)+'\n')
    from PIL import Image,ImageDraw
    scale=7
    image=Image.new('RGB',(120*scale,120*scale),(30,33,38));draw=ImageDraw.Draw(image)
    colors={'main':(213,186,141),'pocket':(148,186,135),'rot':(87,57,47),'corridor':(244,224,168)}
    for i in playable:
        x,y=i%120,i//120
        kind='rot' if i in rot else 'corridor' if i in corridor else 'pocket' if i in pocket else 'main'
        draw.rectangle((x*scale,(119-y)*scale,(x+1)*scale-1,(120-y)*scale-1),fill=colors[kind])
    for slot,(x,y) in enumerate(starts):
        cx,cy=(x+.5)*scale,(119.5-y)*scale
        draw.ellipse((cx-5,cy-5,cx+5,cy+5),fill=(244,248,252) if slot==0 else (240,139,100),outline=(20,20,20))
        draw.text((cx+7,cy-6),'H' if slot==0 else str(slot),fill=(255,255,255),stroke_width=1,stroke_fill=(0,0,0))
    draw.text((12,12),'CPU topology preview: green pocket / brown rot / pale narrow passage; H = human',fill=(235,235,235))
    image.save(args.output_dir/'quarantine-topology.png')
    print(json.dumps(stats))
    print('Mask exported from canonical background validator; preview is not a Unity capture.')

if __name__=='__main__':main()
