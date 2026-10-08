#!/usr/bin/env python3
"""Authoring/source contracts only; not Unity C# compilation or save/UI execution."""
import re
import subprocess
import sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'scripts'))
import validate_board_backgrounds as v

asset_path=ROOT/'FungusToast.Unity/Assets/Configs/Campaign/CampaignProgression.asset'
asset=v.load_unity_yaml(asset_path)['MonoBehaviour']
levels=asset['levels']
old=subprocess.check_output(['git','show','HEAD:FungusToast.Unity/Assets/Configs/Campaign/CampaignProgression.asset'],cwd=ROOT,text=True)
old_order=list(map(int,re.findall(r'^  - levelIndex: (\d+)',old,re.M)))
assert [level['levelIndex'] for level in levels]==old_order,'Do not renumber active save stages'
stage=levels[11]
assert len(stage['variants'])==2
assert {variant['variantId'] for variant in stage['variants']}=={'flatbread-original','rot-quarantine'}
assert sorted(variant['rotLayout'] for variant in stage['variants'])==[0,2]
meta_paths=list((ROOT/'FungusToast.Unity/Assets/Configs/Board Presets').glob('*.asset.meta'))
def preset(guid):
    hits=[p.with_suffix('') for p in meta_paths if re.search(r'^guid: '+guid+r'$',p.read_text(),re.M)]
    assert len(hits)==1,(guid,hits)
    return v.load_unity_yaml(hits[0])['MonoBehaviour']
ids=[]
base=preset(stage['boardPreset']['guid'])
for variant in stage['variants']:
    board=preset(variant['boardPreset']['guid']);ids.append(board['presetId'])
    assert board['boardWidth']==board['boardHeight']==120
    assert board['aiPlayers']==base['aiPlayers'],'Keep stage12 opponents unchanged'
    assert variant['enableNutrientPatches']==stage['enableNutrientPatches']
    assert variant['allowedNutrientPatchTypes']==stage['allowedNutrientPatchTypes']
assert len(set(ids))==len(ids)
assert ids==['Campaign11','Campaign11_QuarantineCorridor']
source=ROOT/'FungusToast.Unity/Assets/Scripts/Unity'
controller=(source/'Campaign/CampaignController.cs').read_text()
assert 'State.levelVariantId = variant?.variantId ?? string.Empty;' in controller
assert 'State.levelVariantId = CurrentLevelVariant.variantId;' in controller
assert 'useSaved ? State.levelVariantId : null, useSaved ? State.boardPresetId : null' in controller
assert 'if (variant != null) return variant.boardPreset;' in controller
assert 'CurrentLevelVariant?.enableNutrientPatches' in controller and 'CurrentLevelVariant?.rotLayout' in controller
manager=(source/'GameManager.cs').read_text()
placement=manager.split('private void PlaceStartingSpores()',1)[1].split('public void StartGrowthPhase()',1)[0]
assert placement.index('QuarantineRotLayout.Build')<placement.index('StartingSporeUtility.PlaceStartingSpores')
assert 'Array.Clear(edgeOffsets' in placement
assert 'excludedTileIds: quarantineRotPlan' in placement
assert 'authoredPlan.StartingPositions[slot]' in placement
state=(source/'Campaign/CampaignState.cs').read_text()
assert 'public string levelVariantId;' in state
print('PASS: unchanged stage ordering; two complete distinct-ID stage12 variants; original AI/resource preservation; save identity and startup source contracts.')
print('Unity compile, actual JSON save/resume, variant titles and board/card rendering remain manual Editor gates.')
