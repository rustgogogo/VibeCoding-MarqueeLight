using MarqueeLight.Models;

namespace MarqueeLight;

public enum LightMode { Steady, Marquee, Blink }

public sealed class LightEngine
{
    private readonly int _totalPerimeter;
    private readonly float _lightLengthRatio;
    private readonly int _lightWidth;
    private float _headPosition;
    private float _speed;
    private ColorTheme _theme;
    private float _opacity;
    private LightMode _mode;

    private readonly ScreenEdge[] _edges;
    private readonly Rectangle _totalBounds;

    private static readonly Color TransparentColor = Color.FromArgb(255, 0, 255);
    private const int SubStripSize = 2;
    private const float BlinkFreq = 1.5f;

    public LightEngine(float speed, ColorTheme theme, float opacity,
                       float lightLengthRatio = 0.125f, int lightWidth = 8)
    {
        _speed = speed;
        _theme = theme;
        _opacity = Math.Clamp(opacity, 0f, 1f);
        _lightLengthRatio = lightLengthRatio;
        _lightWidth = lightWidth;
        _mode = LightMode.Marquee;

        _edges = BuildEdges(out _totalPerimeter, out _totalBounds);
        _headPosition = 0f;
    }

    public ColorTheme CurrentTheme => _theme;
    public void SetTheme(ColorTheme theme) => _theme = theme;
    public float Speed { get => _speed; set => _speed = value; }
    public float Opacity { get => _opacity; set => _opacity = Math.Clamp(value, 0f, 1f); }
    public LightMode Mode { get => _mode; set => _mode = value; }

    public static Rectangle GetTotalBounds()
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var s in Screen.AllScreens)
        {
            var b = s.Bounds;
            if (b.Left < minX) minX = b.Left;
            if (b.Top < minY) minY = b.Top;
            if (b.Right > maxX) maxX = b.Right;
            if (b.Bottom > maxY) maxY = b.Bottom;
        }
        return Rectangle.FromLTRB(minX, minY, maxX, maxY);
    }

    public void Advance(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        if (_mode == LightMode.Marquee)
        {
            _headPosition += _speed * _totalPerimeter * deltaTime;
            while (_headPosition >= _totalPerimeter)
                _headPosition -= _totalPerimeter;
        }
        else if (_mode == LightMode.Blink)
        {
            _headPosition += BlinkFreq * deltaTime;
            while (_headPosition >= 2f) _headPosition -= 2f;
        }
    }

    public List<LightSegment> GetLightSegments()
    {
        float halfWidth = _lightWidth / 2f;
        int subCount = _lightWidth / SubStripSize;
        float offsetX = _totalBounds.Left;
        float offsetY = _totalBounds.Top;

        if (_mode == LightMode.Steady)
            return GetSteadySegments(offsetX, offsetY, halfWidth, subCount);
        if (_mode == LightMode.Blink)
            return GetBlinkSegments(offsetX, offsetY, halfWidth, subCount);
        return GetMarqueeSegments(offsetX, offsetY, halfWidth, subCount);
    }

    private List<LightSegment> GetMarqueeSegments(
        float offsetX, float offsetY, float halfWidth, int subCount)
    {
        var result = new List<LightSegment>();

        float lightLen = _totalPerimeter * _lightLengthRatio;
        if (lightLen <= 0) return result;
        float halfLen = lightLen * 0.5f;

        foreach (var edge in _edges)
        {
            for (float d = 0; d <= edge.Length; d += 1f)
            {
                float globalPos = edge.GlobalOffset + d;
                float dirDist = globalPos - _headPosition;
                if (dirDist < 0) dirDist += _totalPerimeter;
                if (dirDist >= lightLen) continue;

                float centered = Math.Abs(dirDist - halfLen) / halfLen;
                float trailBrightness = (float)Math.Cos(centered * Math.PI * 0.5);
                trailBrightness *= trailBrightness;
                if (trailBrightness <= 0.01f) continue;

                float colorT = centered;
                var baseColor = _theme.Lerp(colorT);
                float t = edge.Length > 0f ? d / edge.Length : 0f;
                float x = edge.X1 + (edge.X2 - edge.X1) * t;
                float y = edge.Y1 + (edge.Y2 - edge.Y1) * t;
                float rx = x - offsetX;
                float ry = y - offsetY;

                EmitSubSegments(result, edge, rx, ry, halfWidth, subCount,
                    trailBrightness * _opacity, baseColor);
            }
        }
        return result;
    }

    private List<LightSegment> GetSteadySegments(
        float offsetX, float offsetY, float halfWidth, int subCount)
    {
        var result = new List<LightSegment>();
        var baseColor = _theme.Primary;

        foreach (var edge in _edges)
        {
            for (float d = 0; d <= edge.Length; d += 1f)
            {
                float t = edge.Length > 0f ? d / edge.Length : 0f;
                float x = edge.X1 + (edge.X2 - edge.X1) * t;
                float y = edge.Y1 + (edge.Y2 - edge.Y1) * t;
                float rx = x - offsetX;
                float ry = y - offsetY;

                EmitSubSegments(result, edge, rx, ry, halfWidth, subCount,
                    _opacity, baseColor);
            }
        }
        return result;
    }

    private List<LightSegment> GetBlinkSegments(
        float offsetX, float offsetY, float halfWidth, int subCount)
    {
        var result = new List<LightSegment>();
        var baseColor = _theme.Primary;
        float blink = (float)Math.Sin(_headPosition * Math.PI);
        blink = blink * blink;
        if (blink < 0.05f) return result;

        foreach (var edge in _edges)
        {
            for (float d = 0; d <= edge.Length; d += 1f)
            {
                float t = edge.Length > 0f ? d / edge.Length : 0f;
                float x = edge.X1 + (edge.X2 - edge.X1) * t;
                float y = edge.Y1 + (edge.Y2 - edge.Y1) * t;
                float rx = x - offsetX;
                float ry = y - offsetY;

                EmitSubSegments(result, edge, rx, ry, halfWidth, subCount,
                    blink * _opacity, baseColor);
            }
        }
        return result;
    }

    private void EmitSubSegments(List<LightSegment> result, ScreenEdge edge,
        float rx, float ry, float halfWidth, int subCount,
        float brightness, Color baseColor)
    {
        for (int si = 0; si < subCount; si++)
        {
            float offsetFromCenter = (si + 0.5f) * SubStripSize - halfWidth;
            float edgeDistFactor = Math.Abs(offsetFromCenter) / halfWidth;
            float ef = edgeDistFactor;
            float edgeFade = 1f - ef * ef * ef * ef * ef;
            if (edgeFade <= 0.01f) continue;

            float effective = brightness * edgeFade;
            float bx = 1f - effective;
            int sr = (int)(baseColor.R * effective + TransparentColor.R * bx);
            int sg = (int)(baseColor.G * effective + TransparentColor.G * bx);
            int sb = (int)(baseColor.B * effective + TransparentColor.B * bx);
            sr = Math.Clamp(sr, 0, 255);
            sg = Math.Clamp(sg, 0, 255);
            sb = Math.Clamp(sb, 0, 255);
            var segColor = Color.FromArgb(sr, sg, sb);

            if (edge.IsVertical)
            {
                float drawX = rx + offsetFromCenter - SubStripSize / 2f;
                float drawY = ry - 1f;
                result.Add(new LightSegment(drawX, drawY, SubStripSize, 2f, segColor));
            }
            else
            {
                float drawX = rx - 1f;
                float drawY = ry + offsetFromCenter - SubStripSize / 2f;
                result.Add(new LightSegment(drawX, drawY, 2f, SubStripSize, segColor));
            }
        }
    }

    private static ScreenEdge[] BuildEdges(out int totalPerimeter, out Rectangle totalBounds)
    {
        var b = GetTotalBounds();
        totalBounds = b;
        int w = b.Width, h = b.Height;
        int offset = 0;

        var edges = new ScreenEdge[4];
        edges[0] = new(b.Left, b.Top, b.Right, b.Top, offset, IsVertical: false);
        offset += w;
        edges[1] = new(b.Right, b.Top, b.Right, b.Bottom, offset, IsVertical: true);
        offset += h;
        edges[2] = new(b.Right, b.Bottom, b.Left, b.Bottom, offset, IsVertical: false);
        offset += w;
        edges[3] = new(b.Left, b.Bottom, b.Left, b.Top, offset, IsVertical: true);
        offset += h;

        totalPerimeter = offset;
        return edges;
    }

    public readonly record struct ScreenEdge(
        int X1, int Y1, int X2, int Y2,
        int GlobalOffset, bool IsVertical)
    {
        public int Length => IsVertical
            ? Math.Abs(Y2 - Y1)
            : Math.Abs(X2 - X1);
    }
}

/// <summary>
/// Position and color of a single light segment to draw.
/// X, Y are relative to the form (bounds origin).
/// </summary>
public readonly record struct LightSegment(float X, float Y, float Width, float Height, Color Color);
