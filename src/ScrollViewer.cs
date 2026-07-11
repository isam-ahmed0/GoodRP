using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace GoodRP;

public class ScrollViewer : Panel
{
    private readonly Panel _content;
    private readonly ModernScrollBar _vScroll;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Panel Content => _content;

    public ScrollViewer()
    {
        BorderStyle = BorderStyle.None;
        BackColor = Color.Transparent;
        AutoScroll = false;

        _content = new Panel
        {
            Location = new Point(0, 0),
            BackColor = Color.Transparent,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        _vScroll = new ModernScrollBar();
        _vScroll.Scroll += OnVScroll;

        Controls.Add(_content);
        Controls.Add(_vScroll);
    }

    public void AddControl(Control c) => _content.Controls.Add(c);
    public void AddControls(params Control[] controls) => _content.Controls.AddRange(controls);

    private void OnVScroll() => _content.Top = -_vScroll.Value;

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        Recompute();
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        Recompute();
    }

    private void Recompute()
    {
        if (_content == null || _vScroll == null || !IsHandleCreated) return;
        var show = _content.Height > ClientSize.Height && _content.Height > 0;
        if (show)
        {
            _vScroll.Visible = true;
            _vScroll.Location = new Point(ClientSize.Width - _vScroll.Width, 0);
            _vScroll.Height = ClientSize.Height;
            _vScroll.ContentHeight = _content.Height;
            _vScroll.ViewportHeight = ClientSize.Height;
        }
        else
        {
            _vScroll.Visible = false;
            _vScroll.Value = 0;
        }
        _content.Top = -_vScroll.Value;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (_vScroll.Visible)
        {
            Value += -Math.Sign(e.Delta) * (Math.Abs(e.Delta) > SystemInformation.MouseWheelScrollDelta ? 40 : 20);
        }
        base.OnMouseWheel(e);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _vScroll.Value;
        set
        {
            _vScroll.Value = value;
            _content.Top = -_vScroll.Value;
        }
    }
}
