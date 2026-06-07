namespace MarqueeLight;

public sealed class MainForm : Form
{
    private readonly LightEngine _engine;
    private readonly System.Windows.Forms.Timer _renderTimer;
    private DateTime _lastTick;
    private bool _animating = false;

    public MainForm(LightEngine engine)
    {
        _engine = engine;
        _lastTick = DateTime.UtcNow;

        var bounds = LightEngine.GetTotalBounds();
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.Fuchsia;
        TransparencyKey = Color.Fuchsia;
        Bounds = bounds;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;

        _renderTimer = new System.Windows.Forms.Timer { Interval = 30 };
        _renderTimer.Tick += OnRenderTick;
        _renderTimer.Start();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        int exStyle = NativeMethods.GetWindowLongW(Handle, NativeMethods.GWL_EXSTYLE);
        exStyle |= NativeMethods.WS_EX_TRANSPARENT
                   | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
        _ = NativeMethods.SetWindowLongW(Handle, NativeMethods.GWL_EXSTYLE, exStyle);

        // Re-assert topmost after extended styles are applied
        TopMost = true;
    }

    protected override bool ShowWithoutActivation => true;

    public bool IsAnimating => _animating;

    public void StartAnimation()
    {
        _animating = true;
        if (!_renderTimer.Enabled)
        {
            _lastTick = DateTime.UtcNow;
            _renderTimer.Start();
        }
    }

    public void StopAnimation()
    {
        _animating = false;
        _renderTimer.Stop();
    }

    private void OnRenderTick(object? sender, EventArgs e)
    {
        if (!_animating) return;

        var now = DateTime.UtcNow;
        float dt = (float)(now - _lastTick).TotalSeconds;
        _lastTick = now;
        if (dt > 0.1f) dt = 0.03f;

        _engine.Advance(dt);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!_animating) return;

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var segments = _engine.GetLightSegments();
        if (segments == null) return;

        foreach (var seg in segments)
        {
            using var brush = new SolidBrush(seg.Color);
            e.Graphics.FillRectangle(brush, seg.X, seg.Y, seg.Width, seg.Height);
        }
    }
}
