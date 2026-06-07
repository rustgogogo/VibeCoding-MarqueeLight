using MarqueeLight.Models;

namespace MarqueeLight;

static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();

            var config = AppConfig.Load();

            var initialTheme = ColorTheme.All.TryGetValue(config.Color, out var t)
                ? t : ColorTheme.Red;
            var engine = new LightEngine(
                speed: (float)config.Speed,
                theme: initialTheme,
                opacity: (float)config.GlobalOpacity,
                lightLengthRatio: (float)config.LightLengthRatio,
                lightWidth: config.LightWidth);

            var form = new MainForm(engine);
            form.Show();

            using var httpServer = new HttpServer(config.HttpPort, engine, form);
            httpServer.Start();

            using var tray = new TrayManager(form, engine, config);

            Application.Run();
        }
        catch (Exception ex)
        {
            File.WriteAllText("marquee_error.log",
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n");
            throw;
        }
    }
}
