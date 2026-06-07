namespace MarqueeLight.Models;

/// <summary>
/// Defines a color theme with primary color (center/max brightness)
/// and secondary color (edges/gradient). The lerp provides smooth
/// transitions between the two.
/// </summary>
public sealed class ColorTheme
{
    public string Name { get; init; } = "";
    public Color Primary { get; init; }
    public Color Secondary { get; init; }

    private ColorTheme() { }

    public static readonly ColorTheme Red = new()
    {
        Name = "red",
        Primary = Color.FromArgb(0xFF, 0x22, 0x00),
        Secondary = Color.FromArgb(0xFF, 0x88, 0x00)
    };

    public static readonly ColorTheme Yellow = new()
    {
        Name = "yellow",
        Primary = Color.FromArgb(0xFF, 0xF0, 0x00),
        Secondary = Color.FromArgb(0xFF, 0xC8, 0x00)
    };

    public static readonly ColorTheme Green = new()
    {
        Name = "green",
        Primary = Color.FromArgb(0xFF, 0x00, 0xFF, 0x44),
        Secondary = Color.FromArgb(0xFF, 0x66, 0xFF, 0xAA)
    };

    public static readonly ColorTheme Blue = new()
    {
        Name = "blue",
        Primary = Color.FromArgb(0xFF, 0x00, 0xAA, 0xFF),
        Secondary = Color.FromArgb(0xFF, 0x00, 0x66, 0xCC)
    };

    public static readonly Dictionary<string, ColorTheme> All = new(StringComparer.OrdinalIgnoreCase)
    {
        ["red"] = Red,
        ["yellow"] = Yellow,
        ["green"] = Green,
        ["blue"] = Blue,
    };

    /// <summary>
    /// Linear interpolate between Primary (t=0) and Secondary (t=1).
    /// </summary>
    public Color Lerp(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            0xFF,
            (int)(Primary.R + (Secondary.R - Primary.R) * t),
            (int)(Primary.G + (Secondary.G - Primary.G) * t),
            (int)(Primary.B + (Secondary.B - Primary.B) * t));
    }
}
