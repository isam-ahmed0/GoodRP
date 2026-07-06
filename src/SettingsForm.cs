namespace GoodRP;

public class SettingsForm : Form
{
    private readonly DiscordManager _discordManager;
    private TextBox _txtDiscordClientId = new();
    private TextBox _txtImgurClientId = new();
    private CheckBox _chkAutoShow = new();
    private CheckBox _chkShowAlbumArt = new();
    private CheckBox _chkMcpServer = new();
    private CheckBox _chkUseHotkeys = new();
    private CheckBox _chkUseNotifications = new();
    private RadioButton _rbAuto = new();
    private RadioButton _rbListening = new();
    private RadioButton _rbWatching = new();
    private Button _btnSave = new();
    private Button _btnConnect = new();
    private Button _btnDisconnect = new();
    private Label _lblStatus = new();
    private Label _lblShowHotkey = new();
    private Label _lblHideHotkey = new();

    public SettingsForm(DiscordManager discordManager)
    {
        _discordManager = discordManager;

        Text = "GoodRP Settings";
        Size = new Size(420, 550);
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

        _chkMcpServer.Text = "Enable MCP Server (for AI agents)";
        _chkMcpServer.Location = new Point(15, 185);
        _chkMcpServer.AutoSize = true;

        var lblActivity = new Label { Text = "Activity Type:", Location = new Point(15, 215), AutoSize = true };

        _rbAuto.Text = "Auto";
        _rbAuto.Location = new Point(15, 235);
        _rbAuto.AutoSize = true;
        _rbAuto.Checked = true;

        _rbListening.Text = "Listening";
        _rbListening.Location = new Point(80, 235);
        _rbListening.AutoSize = true;

        _rbWatching.Text = "Watching";
        _rbWatching.Location = new Point(185, 235);
        _rbWatching.AutoSize = true;

        var lblNotifications = new Label { Text = "Notifications:", Location = new Point(15, 265), AutoSize = true };

        _chkUseNotifications.Text = "Show balloon notification";
        _chkUseNotifications.Location = new Point(15, 285);
        _chkUseNotifications.AutoSize = true;

        var lblHotkeys = new Label { Text = "Hotkeys:", Location = new Point(15, 315), AutoSize = true };

        _chkUseHotkeys.Text = "Enable global hotkeys";
        _chkUseHotkeys.Location = new Point(15, 335);
        _chkUseHotkeys.AutoSize = true;

        var lblShowKey = new Label { Text = "Show shortcut:", Location = new Point(15, 365), AutoSize = true };
        _lblShowHotkey.Text = ConfigManager.Config.ShowHotkey;
        _lblShowHotkey.Location = new Point(120, 365);
        _lblShowHotkey.AutoSize = true;

        var lblHideKey = new Label { Text = "Hide shortcut:", Location = new Point(15, 390), AutoSize = true };
        _lblHideHotkey.Text = ConfigManager.Config.HideHotkey;
        _lblHideHotkey.Location = new Point(120, 390);
        _lblHideHotkey.AutoSize = true;

        _btnConnect.Text = "Connect";
        _btnConnect.Location = new Point(15, 420);
        _btnConnect.Size = new Size(100, 30);
        _btnConnect.Click += BtnConnect_Click;

        _btnDisconnect.Text = "Disconnect";
        _btnDisconnect.Location = new Point(125, 420);
        _btnDisconnect.Size = new Size(100, 30);
        _btnDisconnect.Click += BtnDisconnect_Click;

        _lblStatus.Text = "Status: Disconnected";
        _lblStatus.Location = new Point(240, 428);
        _lblStatus.AutoSize = true;

        _btnSave.Text = "Save";
        _btnSave.Location = new Point(15, 460);
        _btnSave.Size = new Size(370, 30);
        _btnSave.Click += BtnSave_Click;

        Controls.AddRange(new Control[]
        {
            lblDiscord, _txtDiscordClientId,
            lblImgur, _txtImgurClientId,
            _chkAutoShow, _chkShowAlbumArt, _chkMcpServer,
            lblActivity, _rbAuto, _rbListening, _rbWatching,
            lblNotifications, _chkUseNotifications,
            lblHotkeys, _chkUseHotkeys,
            lblShowKey, _lblShowHotkey, lblHideKey, _lblHideHotkey,
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
        _chkMcpServer.Checked = ConfigManager.Config.McpServerEnabled;
        _chkUseHotkeys.Checked = ConfigManager.Config.UseHotkeys;
        _chkUseNotifications.Checked = ConfigManager.Config.UseNotifications;

        var overrideVal = ConfigManager.Config.ActivityTypeOverride;
        _rbAuto.Checked = overrideVal == "Auto";
        _rbListening.Checked = overrideVal == "Listening";
        _rbWatching.Checked = overrideVal == "Watching";

        _lblShowHotkey.Text = ConfigManager.Config.ShowHotkey;
        _lblHideHotkey.Text = ConfigManager.Config.HideHotkey;

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
        ConfigManager.Config.McpServerEnabled = _chkMcpServer.Checked;
        ConfigManager.Config.UseHotkeys = _chkUseHotkeys.Checked;
        ConfigManager.Config.UseNotifications = _chkUseNotifications.Checked;
        ConfigManager.Config.ActivityTypeOverride = _rbAuto.Checked ? "Auto" : _rbListening.Checked ? "Listening" : "Watching";
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
