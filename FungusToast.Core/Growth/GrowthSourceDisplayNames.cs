namespace FungusToast.Core.Growth
{
    public static class GrowthSourceDisplayNames
    {
        public static string GetDisplayName(GrowthSource source) => source switch
        {
            GrowthSource.InitialSpore => "Initial Spore",
            GrowthSource.HyphalOutgrowth => "Hyphal Outgrowth",
            GrowthSource.TendrilOutgrowth => "Tendril Outgrowth",
            GrowthSource.RegenerativeHyphae => "Regenerative Hyphae",
            GrowthSource.NecrotoxicConversion => "Necrotoxic Conversion",
            GrowthSource.HyphalSurge => "Hyphal Surge",
            GrowthSource.JettingMycelium => "Jetting Mycelium",
            GrowthSource.HyphalVectoring => "Chemotactic Beacon",
            GrowthSource.ChemotacticBeacon => "Chemotactic Beacon",
            GrowthSource.SurgicalInoculation => "Surgical Inoculation",
            GrowthSource.Necrosporulation => "Necrosporulation",
            GrowthSource.NecrophyticBloom => "Necrophytic Bloom",
            GrowthSource.NecrohyphalInfiltration => "Necrohyphal Infiltration",
            GrowthSource.CreepingMold => "Creeping Mold",
            GrowthSource.CatabolicRebirth => "Catabolic Rebirth",
            GrowthSource.Ballistospore => "Ballistospore",
            GrowthSource.MycotoxinTracer => "Mycotoxin Tracers",
            GrowthSource.DistalSpore => "Distal Spore",
            GrowthSource.Manual => "Manual",
            GrowthSource.Unknown => "Unknown",
            GrowthSource.SporicidalBloom => "Sporicidal Bloom",
            GrowthSource.MimeticResilience => "Mimetic Resilience",
            GrowthSource.CytolyticBurst => "Cytolytic Burst",
            GrowthSource.PutrefactiveCascade => "Putrefactive Cascade",
            GrowthSource.ChitinFortification => "Chitin Fortification",
            GrowthSource.CornerConduit => "Corner Conduit",
            GrowthSource.AggressotropicConduit => "Aggressotropic Conduit",
            GrowthSource.AegisHyphae => "Aegis Hyphae",
            GrowthSource.CrustalCallus => "Crustal Callus",
            GrowthSource.SporeSalvo => "Spore Salvo",
            GrowthSource.VesicleBurst => "Vesicle Burst",
            GrowthSource.HyphalBridge => "Hyphal Bridge",
            GrowthSource.HyphalDraw => "Hyphal Draw",
            GrowthSource.SeptalAlarm => "Septal Alarm",
            GrowthSource.SporemealPatch => "Sporemeal Patch",
            GrowthSource.ConidiaAscent => "Conidia Ascent",
            _ => source.ToString()
        };
    }
}