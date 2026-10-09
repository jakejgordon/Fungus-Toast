#!/usr/bin/env python3
"""Source contracts only; does not compile or execute Unity."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
source=root/'FungusToast.Unity/Assets/Scripts/Unity'
ui=(source/'UI/MutationTree/UI_MutationManager.cs').read_text()
flow=ui.split('private IEnumerator ResolveChemotacticBeaconUpgrade',1)[1].split('private void EnsureSoundEffectAudioSource',1)[0]
hover=flow.split('SetHoverPreviewCallback',1)[1]
assert 'Blocked by rot: this Beacon cannot reach the marker.' in ui
assert 'The marker is reachable, but spiral growth stops at rot.' in ui
assert 'projection.BlockingRotTileId.HasValue' in hover
assert 'projection.MarkerBlockedByRot ? BeaconRotLineWarning : BeaconRotSpiralWarning' in hover
assert 'UIStyleTokens.State.Warning' in hover
assert 'prompt = $' in hover, 'Replace the banner text rather than overflow its fixed height'
assert 'selection == null || !selection.HasActiveSelection' in hover
clear=hover.split('if (tileId < 0)',1)[1].split('var projection',1)[0]
assert 'ClearChemotacticBeaconPreview' in clear and 'ShowSelectionPrompt(BeaconSelectionPrompt' in clear
assert '"Cancel (Esc)", () => selection.CancelSelection()' in hover
selection=(source/'UI/TileSelectionController.cs').read_text()
assert '_hoverPreviewCallback?.Invoke(-1);' in selection and '_hoverPreviewCallback = null;' in selection
assert 'GameManager.Instance.HideSelectionPrompt();' in selection
print('PASS: contextual rot line/spiral warnings, semantic color, clear-hover restore, cancel wiring and inactive-selection cleanup source contracts.')
print('Unity compile, banner sizing/contrast and interactive hover/cancel/confirm/menu behavior remain manual gates.')
