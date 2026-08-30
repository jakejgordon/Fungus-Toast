namespace FungusToast.Simulation.Models
{
    public partial class SimulationTrackingContext
    {
        private readonly Dictionary<int, int> aeratedFrontierAttempts = new();
        private readonly Dictionary<int, int> aeratedFrontierBonusGrowths = new();

        public void RecordAeratedFrontierAttempt(int playerId)
        {
            aeratedFrontierAttempts[playerId] = GetAeratedFrontierAttempts(playerId) + 1;
        }

        public void RecordAeratedFrontierBonusGrowth(int playerId)
        {
            aeratedFrontierBonusGrowths[playerId] = GetAeratedFrontierBonusGrowths(playerId) + 1;
        }

        public int GetAeratedFrontierAttempts(int playerId)
            => aeratedFrontierAttempts.TryGetValue(playerId, out int value) ? value : 0;

        public int GetAeratedFrontierBonusGrowths(int playerId)
            => aeratedFrontierBonusGrowths.TryGetValue(playerId, out int value) ? value : 0;

        private readonly Dictionary<int, int> filamentOverdriveTriggers = new();
        private readonly Dictionary<int, int> filamentOverdriveBonusCells = new();

        public void RecordFilamentOverdrive(int playerId, int bonusCellsCreated)
        {
            filamentOverdriveTriggers[playerId] = GetFilamentOverdriveTriggers(playerId) + 1;
            filamentOverdriveBonusCells[playerId] = GetFilamentOverdriveBonusCells(playerId) + bonusCellsCreated;
        }

        public int GetFilamentOverdriveTriggers(int playerId)
            => filamentOverdriveTriggers.TryGetValue(playerId, out int value) ? value : 0;

        public int GetFilamentOverdriveBonusCells(int playerId)
            => filamentOverdriveBonusCells.TryGetValue(playerId, out int value) ? value : 0;

        private readonly Dictionary<int, int> crustwardTropismAttempts = new();
        private readonly Dictionary<int, int> crustwardTropismBonusGrowths = new();
        private readonly Dictionary<int, int> crustwardTropismAutomaticGrowths = new();

        public void RecordCrustwardTropismAttempt(int playerId)
        {
            crustwardTropismAttempts[playerId] = GetCrustwardTropismAttempts(playerId) + 1;
        }

        public void RecordCrustwardTropismBonusGrowth(int playerId)
        {
            crustwardTropismBonusGrowths[playerId] = GetCrustwardTropismBonusGrowths(playerId) + 1;
        }

        public void RecordCrustwardTropismAutomaticGrowth(int playerId)
        {
            crustwardTropismAutomaticGrowths[playerId] = GetCrustwardTropismAutomaticGrowths(playerId) + 1;
        }

        public int GetCrustwardTropismAttempts(int playerId)
            => crustwardTropismAttempts.TryGetValue(playerId, out int value) ? value : 0;

        public int GetCrustwardTropismBonusGrowths(int playerId)
            => crustwardTropismBonusGrowths.TryGetValue(playerId, out int value) ? value : 0;

        public int GetCrustwardTropismAutomaticGrowths(int playerId)
            => crustwardTropismAutomaticGrowths.TryGetValue(playerId, out int value) ? value : 0;

        private readonly Dictionary<int, int> detritalEnzymesAttempts = new();
        private readonly Dictionary<int, int> detritalEnzymesBonusGrowths = new();
        private readonly Dictionary<int, int> detritalEnzymesDenseDeadMatterAttempts = new();
        private readonly Dictionary<int, int> detritalEnzymesDenseDeadMatterBonusGrowths = new();

        public void RecordDetritalEnzymesAttempt(int playerId)
            => detritalEnzymesAttempts[playerId] = GetDetritalEnzymesAttempts(playerId) + 1;

        public void RecordDetritalEnzymesBonusGrowth(int playerId)
            => detritalEnzymesBonusGrowths[playerId] = GetDetritalEnzymesBonusGrowths(playerId) + 1;

        public void RecordDetritalEnzymesDenseDeadMatterAttempt(int playerId)
            => detritalEnzymesDenseDeadMatterAttempts[playerId] = GetDetritalEnzymesDenseDeadMatterAttempts(playerId) + 1;

        public void RecordDetritalEnzymesDenseDeadMatterBonusGrowth(int playerId)
            => detritalEnzymesDenseDeadMatterBonusGrowths[playerId] = GetDetritalEnzymesDenseDeadMatterBonusGrowths(playerId) + 1;

        public int GetDetritalEnzymesAttempts(int playerId)
            => detritalEnzymesAttempts.TryGetValue(playerId, out int value) ? value : 0;

        public int GetDetritalEnzymesBonusGrowths(int playerId)
            => detritalEnzymesBonusGrowths.TryGetValue(playerId, out int value) ? value : 0;

        public int GetDetritalEnzymesDenseDeadMatterAttempts(int playerId)
            => detritalEnzymesDenseDeadMatterAttempts.TryGetValue(playerId, out int value) ? value : 0;

        public int GetDetritalEnzymesDenseDeadMatterBonusGrowths(int playerId)
            => detritalEnzymesDenseDeadMatterBonusGrowths.TryGetValue(playerId, out int value) ? value : 0;

        private readonly Dictionary<int, int> toxinMarginAttempts = new();
        private readonly Dictionary<int, int> toxinMarginBonusGrowths = new();

        public void RecordToxinMarginAttempt(int playerId)
            => toxinMarginAttempts[playerId] = GetToxinMarginAttempts(playerId) + 1;

        public void RecordToxinMarginBonusGrowth(int playerId)
            => toxinMarginBonusGrowths[playerId] = GetToxinMarginBonusGrowths(playerId) + 1;

        public int GetToxinMarginAttempts(int playerId)
            => toxinMarginAttempts.TryGetValue(playerId, out int value) ? value : 0;

        public int GetToxinMarginBonusGrowths(int playerId)
            => toxinMarginBonusGrowths.TryGetValue(playerId, out int value) ? value : 0;

        private readonly Dictionary<int, int> toxinborneSeedingRelocations = new();
        private readonly Dictionary<int, int> toxinborneSeedingCarriedCellLandings = new();
        private readonly Dictionary<int, int> toxinborneSeedingAttempts = new();
        private readonly Dictionary<int, int> toxinborneSeedingBonusGrowths = new();

        public void RecordToxinborneSeedingAttempt(int playerId)
            => toxinborneSeedingAttempts[playerId] = GetToxinborneSeedingAttempts(playerId) + 1;

        public void RecordToxinborneSeedingBonusGrowth(int playerId)
            => toxinborneSeedingBonusGrowths[playerId] = GetToxinborneSeedingBonusGrowths(playerId) + 1;

        public void RecordToxinborneSeeding(int playerId, bool toxinRelocated, bool carriedCellLanded)
        {
            if (toxinRelocated)
            {
                toxinborneSeedingRelocations[playerId] = GetToxinborneSeedingRelocations(playerId) + 1;
            }
            if (carriedCellLanded)
            {
                toxinborneSeedingCarriedCellLandings[playerId] = GetToxinborneSeedingCarriedCellLandings(playerId) + 1;
            }
        }

        public int GetToxinborneSeedingRelocations(int playerId)
            => toxinborneSeedingRelocations.TryGetValue(playerId, out int value) ? value : 0;

        public int GetToxinborneSeedingCarriedCellLandings(int playerId)
            => toxinborneSeedingCarriedCellLandings.TryGetValue(playerId, out int value) ? value : 0;

        public int GetToxinborneSeedingAttempts(int playerId)
            => toxinborneSeedingAttempts.TryGetValue(playerId, out int value) ? value : 0;

        public int GetToxinborneSeedingBonusGrowths(int playerId)
            => toxinborneSeedingBonusGrowths.TryGetValue(playerId, out int value) ? value : 0;

        private readonly Dictionary<int, int> latentPolymorphismInterest = new();

        public void RecordLatentPolymorphismInterest(int playerId, int bonusPoints)
            => latentPolymorphismInterest[playerId] = GetLatentPolymorphismInterest(playerId) + bonusPoints;

        public int GetLatentPolymorphismInterest(int playerId)
            => latentPolymorphismInterest.TryGetValue(playerId, out int value) ? value : 0;

        // ────────────────
        // Perimeter Proliferator Growths
        // ────────────────
        private readonly Dictionary<int, int> perimeterProliferatorGrowths = new();
        public void RecordPerimeterProliferatorGrowth(int playerId)
        {
            if (!perimeterProliferatorGrowths.ContainsKey(playerId))
                perimeterProliferatorGrowths[playerId] = 0;
            perimeterProliferatorGrowths[playerId]++;
        }
        public int GetPerimeterProliferatorGrowths(int playerId)
            => perimeterProliferatorGrowths.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllPerimeterProliferatorGrowths() => new(perimeterProliferatorGrowths);

        // ────────────────
        // Hyphal Resistance Transfer
        // ────────────────
        private readonly Dictionary<int, int> hyphalResistanceTransfers = new();
        public void RecordHyphalResistanceTransfer(int playerId, int count)
        {
            if (!hyphalResistanceTransfers.ContainsKey(playerId))
                hyphalResistanceTransfers[playerId] = 0;
            hyphalResistanceTransfers[playerId] += count;
        }
        public int GetHyphalResistanceTransfers(int playerId)
            => hyphalResistanceTransfers.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllHyphalResistanceTransfers() => new(hyphalResistanceTransfers);

        public void RecordSeptalAlarmResistance(int playerId, int count)
        {
        }

        // ────────────────
        // Enduring Toxaphores Extended Cycles
        // ────────────────
        private readonly Dictionary<int, int> enduringToxaphoresExtendedCycles = new();
        public void RecordEnduringToxaphoresExtendedCycles(int playerId, int cycles)
        {
            if (!enduringToxaphoresExtendedCycles.ContainsKey(playerId))
                enduringToxaphoresExtendedCycles[playerId] = 0;
            enduringToxaphoresExtendedCycles[playerId] += cycles;
        }
        public int GetEnduringToxaphoresExtendedCycles(int playerId)
            => enduringToxaphoresExtendedCycles.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllEnduringToxaphoresExtendedCycles() => new(enduringToxaphoresExtendedCycles);

        // ────────────────
        // Enduring Toxaphores Existing Extensions
        // ────────────────
        private readonly Dictionary<int, int> enduringToxaphoresExistingExtensions = new();
        public void RecordEnduringToxaphoresExistingExtensions(int playerId, int cycles)
        {
            if (!enduringToxaphoresExistingExtensions.ContainsKey(playerId))
                enduringToxaphoresExistingExtensions[playerId] = 0;
            enduringToxaphoresExistingExtensions[playerId] += cycles;
        }
        public int GetEnduringToxaphoresExistingExtensions(int playerId)
            => enduringToxaphoresExistingExtensions.TryGetValue(playerId, out var val) ? val : 0;
        public Dictionary<int, int> GetAllEnduringToxaphoresExistingExtensions() => new(enduringToxaphoresExistingExtensions);
    }
}
