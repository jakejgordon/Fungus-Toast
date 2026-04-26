using System;
using System.Collections.Generic;
using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;

namespace FungusToast.Core.Persistence;

[Serializable]
public sealed class RoundStartRuntimeSnapshot
{
    public int BoardWidth;
    public int BoardHeight;
    public int CurrentRound;
    public int CurrentGrowthCycle;
    public bool NecrophyticBloomActivated;
    public List<PlayerRuntimeSnapshot> Players = new();
    public List<FungalCellSnapshot> Cells = new();
    public List<NutrientPatchSnapshot> NutrientPatches = new();
    public List<ChemobeaconMarkerSnapshot> Chemobeacons = new();
    public List<int> PendingHypervariationDraftPlayerIds = new();
    public MycovariantPoolRuntimeSnapshot? MycovariantPool;
}

[Serializable]
public sealed class PlayerRuntimeSnapshot
{
    public int PlayerId;
    public string PlayerName = string.Empty;
    public PlayerTypeEnum PlayerType;
    public AITypeEnum AIType;
    public int MutationPoints;
    public bool IsActive;
    public int Score;
    public bool WantsToBankPointsThisTurn;
    public bool IsLastAiMycovariantDrafterForCurrentDraft;
    public int BaseMutationPointIncome;
    public bool HasStartingTileId;
    public int StartingTileIdValue;
    public string? MutationStrategyName;
    public List<int> ControlledTileIds = new();
    public List<PlayerMutationSnapshot> Mutations = new();
    public List<PlayerMycovariantSnapshot> Mycovariants = new();
    public List<PlayerAdaptationSnapshot> Adaptations = new();
    public List<ActiveSurgeSnapshot> ActiveSurges = new();

    public int? StartingTileId
    {
        get => HasStartingTileId ? StartingTileIdValue : null;
        set
        {
            HasStartingTileId = value.HasValue;
            StartingTileIdValue = value.GetValueOrDefault();
        }
    }
}

[Serializable]
public sealed class PlayerMutationSnapshot
{
    public int MutationId;
    public int CurrentLevel;
    public bool HasFirstUpgradeRound;
    public int FirstUpgradeRoundValue;
    public bool HasPrereqMetRound;
    public int PrereqMetRoundValue;

    public int? FirstUpgradeRound
    {
        get => HasFirstUpgradeRound ? FirstUpgradeRoundValue : null;
        set
        {
            HasFirstUpgradeRound = value.HasValue;
            FirstUpgradeRoundValue = value.GetValueOrDefault();
        }
    }

    public int? PrereqMetRound
    {
        get => HasPrereqMetRound ? PrereqMetRoundValue : null;
        set
        {
            HasPrereqMetRound = value.HasValue;
            PrereqMetRoundValue = value.GetValueOrDefault();
        }
    }
}

[Serializable]
public sealed class PlayerMycovariantSnapshot
{
    public int MycovariantId;
    public bool HasTriggered;
    public bool HasAIScoreAtDraft;
    public float AIScoreAtDraftValue;
    public List<MycovariantEffectCountSnapshot> EffectCounts = new();

    public float? AIScoreAtDraft
    {
        get => HasAIScoreAtDraft ? AIScoreAtDraftValue : null;
        set
        {
            HasAIScoreAtDraft = value.HasValue;
            AIScoreAtDraftValue = value.GetValueOrDefault();
        }
    }
}

[Serializable]
public sealed class MycovariantEffectCountSnapshot
{
    public MycovariantEffectType EffectType;
    public int Count;
}

[Serializable]
public sealed class PlayerAdaptationSnapshot
{
    public string AdaptationId = string.Empty;
    public bool HasTriggered;
    public bool HasRuntimeValue;
    public int RuntimeValue;
}

[Serializable]
public sealed class ActiveSurgeSnapshot
{
    public int MutationId;
    public int Level;
    public int TurnsRemaining;
}

[Serializable]
public sealed class FungalCellSnapshot
{
    public int OriginalOwnerPlayerId;
    public bool HasOwnerPlayerId;
    public int OwnerPlayerIdValue;
    public int TileId;
    public int BirthRound;
    public FungalCellType CellType;
    public int GrowthCycleAge;
    public int ToxinExpirationAge;
    public bool IsNewlyGrown;
    public bool IsDying;
    public bool IsReceivingToxinDrop;
    public bool HasCauseOfDeath;
    public DeathReason CauseOfDeathValue;
    public bool HasSourceOfGrowth;
    public GrowthSource SourceOfGrowthValue;
    public bool HasLastOwnerPlayerId;
    public int LastOwnerPlayerIdValue;
    public int ReclaimCount;
    public bool IsResistant;
    public string? ResistanceSource;

    public int? OwnerPlayerId
    {
        get => HasOwnerPlayerId ? OwnerPlayerIdValue : null;
        set
        {
            HasOwnerPlayerId = value.HasValue;
            OwnerPlayerIdValue = value.GetValueOrDefault();
        }
    }

    public DeathReason? CauseOfDeath
    {
        get => HasCauseOfDeath ? CauseOfDeathValue : null;
        set
        {
            HasCauseOfDeath = value.HasValue;
            CauseOfDeathValue = value.GetValueOrDefault();
        }
    }

    public GrowthSource? SourceOfGrowth
    {
        get => HasSourceOfGrowth ? SourceOfGrowthValue : null;
        set
        {
            HasSourceOfGrowth = value.HasValue;
            SourceOfGrowthValue = value.GetValueOrDefault();
        }
    }

    public int? LastOwnerPlayerId
    {
        get => HasLastOwnerPlayerId ? LastOwnerPlayerIdValue : null;
        set
        {
            HasLastOwnerPlayerId = value.HasValue;
            LastOwnerPlayerIdValue = value.GetValueOrDefault();
        }
    }
}

[Serializable]
public sealed class NutrientPatchSnapshot
{
    public int TileId;
    public int ClusterId;
    public int ClusterTileCount;
    public NutrientPatchSource Source;
    public NutrientPatchType PatchType;
    public string DisplayName = string.Empty;
    public string Description = string.Empty;
    public NutrientRewardType RewardType;
    public int RewardAmount;
}

[Serializable]
public sealed class ChemobeaconMarkerSnapshot
{
    public int PlayerId;
    public int MutationId;
    public int TileId;
    public int TurnsRemaining;
}

[Serializable]
public sealed class MycovariantPoolRuntimeSnapshot
{
    public List<int> AvailablePoolIds = new();
    public List<int> UniversalPoolIds = new();
    public List<int> DraftedNonUniversalIds = new();
    public List<int> TemporarilyRemovedMycovariantIds = new();
}