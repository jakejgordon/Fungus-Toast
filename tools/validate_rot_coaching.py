#!/usr/bin/env python3
"""Source-derived rot coaching checks; not Unity C#/UI compilation or playback."""
import ast
import itertools
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / 'FungusToast.Unity/Assets/Scripts/Unity'
catalog = (SCRIPTS / 'UI/Onboarding/NewPlayerTooltipCatalog.cs').read_text()
match = re.search(r'ShouldShowRotIntro\([^)]*\)\s*=>\s*(.*?);', catalog, re.S)
assert match, 'Rot coaching predicate missing'
expression = match.group(1).replace('NewPlayerTooltipCatalog.HasBeenSeen(NewPlayerTooltipId.RotIntro)', 'hasBeenSeen')
expression = expression.replace('&&', ' and ').replace('!', ' not ').strip()
expression = ' '.join(expression.split())
parsed = ast.parse(expression, mode='eval')
allowed = (ast.Expression, ast.BoolOp, ast.UnaryOp, ast.Compare, ast.Name, ast.Constant,
           ast.And, ast.Not, ast.Eq, ast.Gt, ast.Load)
assert all(isinstance(node, allowed) for node in ast.walk(parsed))
code = compile(parsed, '<source-derived rot coaching rule>', 'eval')
booleans = ['isNewGame','isCampaign','isFirstRotLevel','hasRot','isFastForwarding','hasBeenSeen']
count = 0
for bits in itertools.product((False,True), repeat=len(booleans)):
    for current_round in (0,1,2):
        for humans in (0,1):
            values = dict(zip(booleans,bits), currentRound=current_round, humanPlayerCount=humans)
            actual = eval(code, {'__builtins__': {}}, values)
            expected = (values['isNewGame'] and values['isCampaign'] and values['isFirstRotLevel']
                        and values['hasRot'] and current_round == 1 and humans > 0
                        and not values['isFastForwarding'] and not values['hasBeenSeen'])
            assert actual == expected, values
            count += 1

manager = (SCRIPTS / 'GameManager.cs').read_text()
flow = manager.split('var introBoard = Board;',1)[1].split('#endregion',1)[0]
assert flow.index('isRotIntroPendingOrVisible =') < flow.index('StartNextRound();') < flow.index('RotIntroCoachmark.Show(')
assert 'campaignController?.IsFirstRotLevel == true' in flow
assert 'applyStartingSporeEffects' in flow
assert 'ReferenceEquals(Board, introBoard)' in flow and 'introBoard.CurrentRound == 1' in flow
assert re.search(r'finally\s*{\s*isRotIntroPendingOrVisible = false;', flow)
assert 'while (isRotIntroPendingOrVisible) yield return null;' in manager
assert manager.count('isRotIntroPendingOrVisible = false;') >= 3
card = (SCRIPTS / 'UI/Onboarding/RotIntroCoachmark.cs').read_text()
assert 'card.Root != null && shouldRemainVisible()' in card
assert 'if (dismissed) NewPlayerTooltipCatalog.MarkSeen' in card
assert 'finally' in card and 'card.HideImmediate()' in card
controller = (SCRIPTS / 'Campaign/CampaignController.cs').read_text()
assert 'State.levelIndex == progression.levels.FindIndex(level => level != null && level.enableRotPatch)' in controller
asset = (ROOT/'FungusToast.Unity/Assets/Configs/Campaign/CampaignProgression.asset').read_text()
levels = re.split(r'\n  - levelIndex:', asset)[1:]
first = next((i for i,level in enumerate(levels) if 'enableRotPatch: 1' in level), None)
assert first is not None, 'Default campaign has no rot introduction stage'
print(f'PASS: {count} source-derived trigger cases; first-rot authored ordering; round-start sequencing, dismissal, cancellation and reset source checks.')
print(f'Default first rot stage: {first+1}. Unity compile/visual flow remains a manual Editor gate.')
