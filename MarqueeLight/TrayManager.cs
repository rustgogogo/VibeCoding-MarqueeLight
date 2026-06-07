using MarqueeLight.Models;

namespace MarqueeLight;

public sealed class TrayManager : IDisposable
{
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _menu;
    private readonly MainForm _form;
    private readonly LightEngine _engine;
    private bool _disposed;

    private readonly ToolStripMenuItem _menuRed;
    private readonly ToolStripMenuItem _menuYellow;
    private readonly ToolStripMenuItem _menuGreen;
    private readonly ToolStripMenuItem _menuBlue;
    private readonly ToolStripMenuItem _menuToggle;
    private ColorTheme _currentTheme;

    public TrayManager(MainForm form, LightEngine engine, AppConfig config)
    {
        _form = form;
        _engine = engine;
        _currentTheme = ColorTheme.All.TryGetValue(config.Color, out var t) ? t : ColorTheme.Red;

        _engine.SetTheme(_currentTheme);

        _trayIcon = new NotifyIcon
        {
            Text = "Marquee Light",
            Visible = true
        };

        _trayIcon.Icon = CreateColorIcon();

        _menu = new ContextMenuStrip();

        var colorHeader = new ToolStripMenuItem("颜色模式") { Enabled = false };
        _menu.Items.Add(colorHeader);

        _menuRed = new ToolStripMenuItem("红色调", null, OnColorClick) { Checked = _currentTheme == ColorTheme.Red };
        _menuYellow = new ToolStripMenuItem("黄色调", null, OnColorClick) { Checked = _currentTheme == ColorTheme.Yellow };
        _menuGreen = new ToolStripMenuItem("绿色调", null, OnColorClick) { Checked = _currentTheme == ColorTheme.Green };
        _menuBlue = new ToolStripMenuItem("蓝色调", null, OnColorClick) { Checked = _currentTheme == ColorTheme.Blue };

        _menu.Items.Add(_menuRed);
        _menu.Items.Add(_menuYellow);
        _menu.Items.Add(_menuGreen);
        _menu.Items.Add(_menuBlue);
        _menu.Items.Add(new ToolStripSeparator());
        _menuToggle = new ToolStripMenuItem("隐藏灯带", null, OnToggleClick) { Checked = true };
        _menu.Items.Add(_menuToggle);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem("退出", null, OnExitClick));

        _trayIcon.ContextMenuStrip = _menu;

        _trayIcon.DoubleClick += OnToggleClick;
    }

    private void OnColorClick(object? sender, EventArgs e)
    {
        if (sender == _menuRed) SetTheme(ColorTheme.Red);
        else if (sender == _menuYellow) SetTheme(ColorTheme.Yellow);
        else if (sender == _menuGreen) SetTheme(ColorTheme.Green);
        else if (sender == _menuBlue) SetTheme(ColorTheme.Blue);
    }

    private void SetTheme(ColorTheme theme)
    {
        _currentTheme = theme;
        _engine.SetTheme(theme);

        _menuRed.Checked = theme == ColorTheme.Red;
        _menuYellow.Checked = theme == ColorTheme.Yellow;
        _menuGreen.Checked = theme == ColorTheme.Green;
        _menuBlue.Checked = theme == ColorTheme.Blue;
    }

    private void OnToggleClick(object? sender, EventArgs e)
    {
        if (_form.IsAnimating)
        {
            _form.StopAnimation();
            _menuToggle.Text = "显示灯带";
            _trayIcon.Text = "Marquee Light (已停止)";
        }
        else
        {
            _form.StartAnimation();
            _menuToggle.Text = "隐藏灯带";
            _trayIcon.Text = "Marquee Light";
        }
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }

    private static Icon CreateColorIcon()
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.FillEllipse(Brushes.Red, 2, 5, 4, 4);
        g.FillEllipse(Brushes.Yellow, 6, 5, 4, 4);
        g.FillEllipse(Brushes.Green, 10, 5, 4, 4);
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _trayIcon?.Dispose();
            _menu?.Dispose();
            _disposed = true;
        }
    }
}
