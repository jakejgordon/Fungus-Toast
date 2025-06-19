namespace FungusToast.Core.Mycovariants
{
    public enum MycovariantType
    {
        Passive,
        Triggered,         // Requires board event like edge contact or cell death
        Directional,       // Requires target tile and/or direction
        AreaEffect,        // Targets a board region (e.g., Sporulation Vortex)
        Economy            // Provides bonus MP or similar
    }
}
