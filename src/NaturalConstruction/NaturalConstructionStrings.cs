using static global::STRINGS.UI;

namespace ONIUtilityTweaks.NaturalConstruction
{
    internal static class STRINGS
    {
        internal static class BUILDINGS
        {
            internal static class PREFABS
            {
                internal static class OUT_NATURALTILE
                {
                    public static LocString NAME = FormatAsLink(
                        "Natural Tile", nameof(OUT_NATURALTILE));
                    public static LocString DESC =
                        "A constructed tile that behaves like natural terrain.";
                    public static LocString EFFECT =
                        "Creates a natural-looking solid tile from the selected material. " +
                        "Utilities can be built through it without digging it up, and Pips " +
                        "can still plant suitable seeds on it.";
                }

                internal static class OUT_NATURALBACKWALL
                {
                    public static LocString NAME = FormatAsLink(
                        "Natural Backwall", nameof(OUT_NATURALBACKWALL));
                    public static LocString DESC =
                        "A backwall made from a selected natural material.";
                    public static LocString EFFECT =
                        "Creates a natural backwall from the selected material.";
                }
            }
        }
    }
}
