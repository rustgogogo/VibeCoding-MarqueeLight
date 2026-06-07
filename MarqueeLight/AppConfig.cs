using System.Reflection;
using System.Text.Json;

namespace MarqueeLight;

public sealed class AppConfig
{
    public string Color { get; set; } = "red";
    public double Speed { get; set; } = 0.5;
    public int LightWidth { get; set; } = 8;
    public double GlobalOpacity { get; set; } = 0.85;
    public double LightLengthRatio { get; set; } = 0.125;
    public int HttpPort { get; set; } = 50080;

    public static AppConfig Load()
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("appsettings.json"));
        if (name == null) return new AppConfig();

        using var stream = asm.GetManifestResourceStream(name);
        if (stream == null) return new AppConfig();

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }
}
