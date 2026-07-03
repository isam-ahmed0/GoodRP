namespace GoodRP;

public class SettingsForm : Form
{
    private readonly DiscordManager _discordManager;
    private TextBox _txtDiscordClientId = new();
    private TextBox _txtImgurClientId = new();
    private CheckBox _chkAutoShow = new();
    private CheckBox _chkShowAlbumArt = new();
    private Button _btnSave = new();
    private Button _btnConnect = new();
    private Button _btnDisconnect = new();
    private Label _lblStatus = new();

    public SettingsForm(DiscordManager discordManager)
    {
        _discordManager = discordManager;

        Text = "GoodRP Settings";
        Size = new Size(420, 340);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        InitializeControls();
        LoadSettings();
    }

    private void InitializeControls()
    {
        var lblDiscord = new Label
        {
            Text = "Discord Application ID:",
            Location = new Point(15, 20),
            AutoSize = true
        };

        _txtDiscordClientId.Location = new Point(15, 42);
        _txtDiscordClientId.Size = new Size(370, 23);
        _txtDiscordClientId.PlaceholderText = "Enter your Discord Application ID";

        var lblImgur = new Label
        {
            Text = "Imgur Client ID (optional, for album art):",
            Location = new Point(15, 75),
            AutoSize = true
        };

        _txtImgurClientId.Location = new Point(15, 97);
        _txtImgurClientId.Size = new Size(370, 23);
        _txtImgurClientId.PlaceholderText = "Enter your Imgur Client ID (optional)";

        _chkAutoShow.Text = "Auto-show on Discord (skip notification)";
        _chkAutoShow.Location = new Point(15, 135);
        _chkAutoShow.AutoSize = true;

        _chkShowAlbumArt.Text = "Show album art on Discord";
        _chkShowAlbumArt.Location = new Point(15, 160);
        _chkShowAlbumArt.AutoSize = true;

        _btnConnect.Text = "Connect";
        _btnConnect.Location = new Point(15, 200);
        _btnConnect.Size = new Size(100, 30);
        _btnConnect.Click += BtnConnect_Click;

        _btnDisconnect.Text = "Disconnect";
        _btnDisconnect.Location = new Point(125, 200);
        _btnDisconnect.Size = new Size(100, 30);
        _btnDisconnect.Click += BtnDisconnect_Click;

        _lblStatus.Text = "Status: Disconnected";
        _lblStatus.Location = new Point(240, 208);
        _lblStatus.AutoSize = true;

        _btnSave.Text = "Save";
        _btnSave.Location = new Point(15, 250);
        _btnSave.Size = new Size(370, 30);
        _btnSave.Click += BtnSave_Click;

        Controls.AddRange(new Control[]
        {
            lblDiscord, _txtDiscordClientId,
            lblImgur, _txtImgurClientId,
            _chkAutoShow, _chkShowAlbumArt,
            _btnConnect, _btnDisconnect, _lblStatus,
            _btnSave
        });
    }

    private void LoadSettings()
    {
        _txtDiscordClientId.Text = ConfigManager.Config.DiscordClientId;
        _txtImgurClientId.Text = ConfigManager.Config.ImgurClientId;
        _chkAutoShow.Checked = ConfigManager.Config.AutoShowOnDiscord;
        _chkShowAlbumArt.Checked = ConfigManager.Config.ShowAlbumArt;

        UpdateStatus();
    }

    private void BtnConnect_Click(object? sender, EventArgs e)
    {
        var clientId = _txtDiscordClientId.Text.Trim();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            MessageBox.Show("Please enter a Discord Application ID.", "GoodRP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ConfigManager.Config.DiscordClientId = clientId;
        ConfigManager.Save();

        var connected = _discordManager.Connect(clientId);
        UpdateStatus();

        if (connected)
        {
            MessageBox.Show("Connected to Discord!", "GoodRP", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show("Failed to connect to Discord. Make sure Discord is running.", "GoodRP", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDisconnect_Click(object? sender, EventArgs e)
    {
        _discordManager.Disconnect();
        UpdateStatus();
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        ConfigManager.Config.DiscordClientId = _txtDiscordClientId.Text.Trim();
        ConfigManager.Config.ImgurClientId = _txtImgurClientId.Text.Trim();
        ConfigManager.Config.AutoShowOnDiscord = _chkAutoShow.Checked;
        ConfigManager.Config.ShowAlbumArt = _chkShowAlbumArt.Checked;
        ConfigManager.Save();

        MessageBox.Show("Settings saved!", "GoodRP", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void UpdateStatus()
    {
        _lblStatus.Text = _discordManager.IsConnected ? "Status: Connected" : "Status: Disconnected";
        _lblStatus.ForeColor = _discordManager.IsConnected ? Color.Green : Color.Red;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
