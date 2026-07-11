using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace GoodRP;

public class ModernScrollBar : Control
{
    private int _contentHeight;
    private int _viewportHeight;
    private int _value;
    private bool _dragging;
    private int _dragOffset;

    private static readonly Color TrackColor = Color.FromArgb(35, 35, 52);
    private static readonly Color ThumbColor = Color.FromArgb(95, 95, 120);
    private static readonly Color ThumbHover = Color.FromArgb(120, 120, 150);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ContentHeight
    {
        get => _contentHeight;
        set { _contentHeight = Math.Max(0, value); UpdateLayout(); }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ViewportHeight
    {
        get => _viewportHeight;
        set { _viewportHeight = Math.Max(0, value); UpdateLayout(); }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set
        {
            var max = MaxValue;
            _value = Math.Max(0, Math.Min(value, max));
            Invalidate();
            Scroll?.Invoke();
        }
    }

    public int MaxValue => Math.Max(0, _contentHeight - _viewportHeight);

    private int ThumbSize
    {
        get
        {
            if (_contentHeight <= 0 || _viewportHeight <= 0) return Height;
            var ratio = (double)_viewportHeight / _contentHeight;
            return (int)Math.Max(32, Math.Min(Height, ratio * Height));
        }
    }

    private int ThumbTop
    {
        get
        {
            var max = MaxValue;
            if (max <= 0) return 0;
            var track = Height - ThumbSize;
            return (int)((double)_value / max * track);
        }
    }

    public event Action? Scroll;

    public ModernScrollBar()
    {
        Width = 10;
        DoubleBuffered = true;
        Visible = false;
        Cursor = Cursors.Hand;
    }

    private void UpdateLayout()
    {
        Visible = MaxValue > 0;
        if (_value > MaxValue) _value = MaxValue;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(TrackColor);

        var top = ThumbTop;
        var size = ThumbSize;
        using var path = RoundedRect(1, top, Width - 2, size, 5);
        using var brush = new SolidBrush(ThumbColor);
        g.FillPath(brush, path);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var top = ThumbTop;
        var size = ThumbSize;
        if (e.Y >= top && e.Y <= top + size)
        {
            _dragging = true;
            _dragOffset = e.Y - top;
        }
        else
        {
            // Click on track => page jump
            var jump = e.Y < top ? -ViewportHeight : ViewportHeight;
            Value += jump;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!_dragging) return;
        var track = Height - ThumbSize;
        var desiredTop = e.Y - _dragOffset;
        var max = MaxValue;
        Value = max <= 0 ? 0 : (int)((double)desiredTop / track * max);
    }

    protected override void OnMouseUp(MouseEventArgs e) => _dragging = false;
    protected override void OnMouseLeave(EventArgs e) => Invalidate();

    private static GraphicsPath RoundedRect(int x, int y, int w, int h, int r)
    {
        r = Math.Min(r, Math.Min(w, h) / 2);
        var path = new GraphicsPath();
        path.AddArc(x, y, r * 2, r * 2, 180, 90);
        path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
        path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
        path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
        path.CloseFigure();
        return path;
    }
}
