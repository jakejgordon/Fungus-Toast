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

card=(source/'UI/Testing/DevelopmentTestingCardController.cs').read_text()
assert '"Campaign Level Option"' in card
assert 'campaignVariants.Count > 1' in card
refresh=card.split('private void RefreshCampaignVariantOptions()',1)[1].split('private void OnCampaignVariantChanged',1)[0]
assert 'campaignVariants.AddRange(spec.variants)' in refresh
assert 'if (index < 0) index = 0;' in refresh
assert 'SetValueWithoutNotify(index)' in refresh
change=card.split('private void OnCampaignLevelChanged(int index)',1)[1].split('private void OnForcedMoldinessRewardChanged',1)[0]
assert change.index('selectedCampaignVariantId = string.Empty;') < change.index('RefreshCampaignVariantOptions();')
assert 'selectedCampaignVariantId = configuration.CampaignVariantId;' in card
assert 'configuration.ForcedStartingAdaptationIds, configuration.CampaignVariantId' in card
assert 'testingForcedCampaignVariantId = forcedCampaignVariantId ?? string.Empty;' in manager
endgame=(source/'UI/UI_EndGamePanel.cs').read_text()
assert 'manager?.TestingForcedCampaignVariantId ?? string.Empty' in endgame
print('PASS: testing picker source contracts: authored order, first-option default, stage reset, configuration reload, startup/endgame routing.')

# Level 8 uses the same complete-variant/save/picker machinery.
stage8 = levels[7]
assert [v['variantId'] for v in stage8['variants']] == ['country-original','rotten-heart']
assert [v['rotLayout'] for v in stage8['variants']] == [0,3]
original8 = preset(stage8['boardPreset']['guid'])
for variant in stage8['variants']:
    board = preset(variant['boardPreset']['guid'])
    assert board['boardWidth'] == board['boardHeight'] == 90
    for field in ['aiPlayers','pooledAiPlayerCount','aiStrategyPool','poolAdaptationOverrides']:
        assert board[field] == original8[field], field
    assert variant['enableNutrientPatches'] == stage8['enableNutrientPatches']
    assert variant['allowedNutrientPatchTypes'] == stage8['allowedNutrientPatchTypes']
assert [preset(v['boardPreset']['guid'])['presetId'] for v in stage8['variants']] == ['Campaign7','Campaign7_RottenHeart']
assert placement.index('CentralRotLayout.Place(Board)') < placement.index('StartingSporeUtility.PlaceStartingSpores')
assert 'RotLayoutKind.CentralIsland ? "Campaign7" : preset.presetId' in manager
print('PASS: level8 distinct stable IDs, original pooled AI/resources, terrain-first island placement and original human start metadata retained.')
