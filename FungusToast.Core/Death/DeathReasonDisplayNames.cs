using FungusToast.Core.Formatting;

namespace FungusToast.Core.Death
{
    public static class DeathReasonDisplayNames
    {
        public static string GetDisplayName(DeathReason reason) => reason switch
        {
            DeathReason.Age => "Old Age",
            DeathReason.Randomness => "Random Death",
            DeathReason.PutrefactiveMycotoxin => "Putrefactive Mycotoxin",
            DeathReason.SporicidalBloom => "Sporicidal Bloom",
            DeathReason.MycotoxinPotentiation => "Mycotoxin Potentiation",
            DeathReason.HyphalVectoring => "Chemotactic Beacon",
            DeathReason.JettingMycelium => "Jetting Mycelium",
            DeathReason.Infested => "Infested",
            DeathReason.Poisoned => "Poisoned",
            DeathReason.MycotoxicLash => "Mycotoxic Lash",
            DeathReason.PutrefactiveCascade => "Putrefactive Cascade",
            DeathReason.PutrefactiveCascadePoison => "Putrefactive Cascade Poison",
            DeathReason.CytolyticBurst => "Cytolytic Burst",
            DeathReason.Unknown => "Unknown",
            _ => DisplayNameHumanizer.HumanizeIdentifier(reason.ToString())
        };
    }
}
