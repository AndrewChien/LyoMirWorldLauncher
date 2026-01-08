using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Launcher.Core.CsLogin;
using Launcher.Core.Ini;
using Launcher.Core.RcConfig;

namespace Launcher;

public partial class FrmMain : Form
{
	private enum DownloadStatus
	{
		Active,
		Inactive,
		Success,
		Error,
	}

	private readonly CsLoginClient _client = new();
	private readonly HttpClient _httpClient = new();

	private RcAppendedConfig? _appendedConfig;
	private IniFile _serverIni = new();
	private DownloadStatus _downloadStatus = DownloadStatus.Inactive;
	private DateTime _downloadStartUtc;

	private string _currentCaption = string.Empty;
	private string _csIp = string.Empty;
	private string _csPort = string.Empty;
	private string _csName = string.Empty;
	private string _csWeb = string.Empty;
	private string _csInfo = string.Empty;
	private string _csShop = string.Empty;
	private string _makeNewAccount = string.Empty;

	private FrmNewAccount? _frmNewAccount;
	private FrmChangePassword? _frmChangePassword;
	private FrmGetBackPassword? _frmGetBackPassword;

	private Win32.PROCESS_INFORMATION? _gameProcess;

	public FrmMain()
	{
		InitializeComponent();

		_client.Connected += (_, _) => BeginInvoke(() => SetConnectedUi(true));
		_client.Disconnected += (_, _) => BeginInvoke(() => SetConnectedUi(false));
		_client.ReceiveLoopError += (_, ex) =>
			BeginInvoke(() => MessageBox.Show(this, ex.Message, "Socket错误", MessageBoxButtons.OK, MessageBoxIcon.Error));
		_client.PacketReceived += (_, args) =>
			BeginInvoke(() => HandleServerPacket(args.Message, args.Body));
	}

	private void FrmMain_Load(object sender, EventArgs e)
	{
		TryLoadEmbeddedConfig();
		LoadServerListIni();
		PopulateServers();

		timerTitleScroll.Enabled = lblTitle.Text.Length > 0;
		timerDownload.Enabled = true;

		if (!string.IsNullOrWhiteSpace(_csInfo))
		{
			TryNavigate(_csInfo);
		}
	}

	private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
	{
		timerTitleScroll.Enabled = false;
		timerDownload.Enabled = false;
		timerPatchAndExit.Enabled = false;

		_client.Disconnect();
		_httpClient.Dispose();
	}

	private void cmbServers_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbServers.SelectedItem is not string caption)
		{
			return;
		}

		_currentCaption = caption;
		_csIp = _serverIni.ReadString(caption, "ServerAdd", "127.0.0.1");
		_csPort = _serverIni.ReadString(caption, "ServerPort", "7000");
		_csName = _serverIni.ReadString(caption, "ServerName", caption);
		_csWeb = _serverIni.ReadString(caption, "WebUrl", "http://www.chengxihot.top");
		_csInfo = _serverIni.ReadString(caption, "infoUrl", _serverIni.ReadString(caption, "infourl", "http://www.chengxihot.top"));
		_csShop = _serverIni.ReadString(caption, "shopUrl", "http://www.chengxihot.top");

		TryNavigate(_csWeb);
		_ = ConnectToServerAsync(_csIp, _csPort);
	}

	private void btnRegister_Click(object sender, EventArgs e)
	{
		if (_frmNewAccount is null || _frmNewAccount.IsDisposed)
		{
			_frmNewAccount = new FrmNewAccount(
				isConnected: () => _client.IsConnected,
				sendNewAccountAsync: async (entry, entryAdd) =>
				{
					_makeNewAccount = entry.Account;
					await _client.SendNewAccountAsync(entry, entryAdd);
				});
		}

		_frmNewAccount.SetConnectionStatus(lblSocketStatus.Text);
		_frmNewAccount.Open(this);
	}

	private void btnChangePassword_Click(object sender, EventArgs e)
	{
		if (_frmChangePassword is null || _frmChangePassword.IsDisposed)
		{
			_frmChangePassword = new FrmChangePassword(
				isConnected: () => _client.IsConnected,
				sendAsync: (account, password, newPassword) => _client.SendChangePasswordAsync(account, password, newPassword));
		}

		_frmChangePassword.SetConnectionStatus(lblSocketStatus.Text);
		_frmChangePassword.Open(this);
	}

	private void btnGetBackPassword_Click(object sender, EventArgs e)
	{
		if (_frmGetBackPassword is null || _frmGetBackPassword.IsDisposed)
		{
			_frmGetBackPassword = new FrmGetBackPassword(
				isConnected: () => _client.IsConnected,
				sendAsync: (account, q1, a1, q2, a2, birthDay) => _client.SendGetBackPasswordAsync(account, q1, a1, q2, a2, birthDay));
		}

		_frmGetBackPassword.SetConnectionStatus(lblSocketStatus.Text);
		_frmGetBackPassword.Open(this);
	}

	private void btnEnterGame_Click(object sender, EventArgs e)
	{
		try
		{
			WriteGameIniFiles();
			StartGameSuspended();

			WindowState = FormWindowState.Minimized;
			Hide();

			timerPatchAndExit.Enabled = true;
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void btnOpenWeb_Click(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(_csWeb))
		{
			return;
		}

		try
		{
			Process.Start(new ProcessStartInfo(_csWeb) { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "打开失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void btnExit_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void timerTitleScroll_Tick(object sender, EventArgs e)
	{
		lblTitle.Left += 1;
		int resetX = 12;
		int maxX = ClientSize.Width - 50;
		if (lblTitle.Left > maxX)
		{
			lblTitle.Left = resetX;
		}
	}

	private void timerDownload_Tick(object sender, EventArgs e)
	{
		if (_appendedConfig is null)
		{
			cmbServers.Enabled = true;
			timerDownload.Enabled = false;
			return;
		}

		string downloadUrl = _appendedConfig.DecodeDownloadIniUrl();
		if (string.IsNullOrWhiteSpace(downloadUrl))
		{
			cmbServers.Enabled = true;
			timerDownload.Enabled = false;
			return;
		}

		if (_downloadStatus == DownloadStatus.Inactive)
		{
			_downloadStatus = DownloadStatus.Active;
			_downloadStartUtc = DateTime.UtcNow;
			_ = DownloadServerListIniAsync(downloadUrl);
		}

		if (_downloadStatus is DownloadStatus.Success or DownloadStatus.Error ||
			DateTime.UtcNow - _downloadStartUtc > TimeSpan.FromSeconds(10))
		{
			LoadServerListIni();
			PopulateServers();
			cmbServers.Enabled = true;
			timerDownload.Enabled = false;
		}
	}

	private void timerPatchAndExit_Tick(object sender, EventArgs e)
	{
		timerPatchAndExit.Enabled = false;

		if (_gameProcess is not Win32.PROCESS_INFORMATION processInfo)
		{
			Close();
			return;
		}

		try
		{
			GamePatcher.Apply(processInfo.dwProcessId);
		}
		catch
		{
		}

		Close();
	}

	private void HandleServerPacket(DefaultMessage msg, string body)
	{
		switch (msg.Ident)
		{
			case CsLoginConstants.SM_NEWID_SUCCESS:
				MessageBox.Show(this, "注册成功。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				if (_frmNewAccount is not null && !_frmNewAccount.IsDisposed)
				{
					_frmNewAccount.Close();
				}
				break;

			case CsLoginConstants.SM_NEWID_FAIL:
				switch (msg.Recog)
				{
					case 0:
						MessageBox.Show(this, $"账号“{_makeNewAccount}”已存在。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						break;
					case -2:
						MessageBox.Show(this, "注册失败。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						break;
					default:
						MessageBox.Show(this, $"注册失败，Code: {msg.Recog}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						break;
				}
				if (_frmNewAccount is not null && !_frmNewAccount.IsDisposed)
				{
					_frmNewAccount.SetOkEnabled(true);
				}
				break;

			case CsLoginConstants.SM_CHGPASSWD_SUCCESS:
				MessageBox.Show(this, "修改密码成功。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				if (_frmChangePassword is not null && !_frmChangePassword.IsDisposed)
				{
					_frmChangePassword.ResetFields();
					_frmChangePassword.Close();
				}
				break;

			case CsLoginConstants.SM_CHGPASSWD_FAIL:
				MessageBox.Show(this, $"修改密码失败，Code: {msg.Recog}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				if (_frmChangePassword is not null && !_frmChangePassword.IsDisposed)
				{
					_frmChangePassword.SetOkEnabled(true);
				}
				break;

			case CsLoginConstants.SM_GETBACKPASSWD_SUCCESS:
				string password = CsLoginCodec.DecodeString(body);
				MessageBox.Show(this, $"密码找回成功。\n您的密码为：{password}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				if (_frmGetBackPassword is not null && !_frmGetBackPassword.IsDisposed)
				{
					_frmGetBackPassword.SetOkEnabled(true);
				}
				break;

			case CsLoginConstants.SM_GETBACKPASSWD_FAIL:
				MessageBox.Show(this, $"密码找回失败，Code: {msg.Recog}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				if (_frmGetBackPassword is not null && !_frmGetBackPassword.IsDisposed)
				{
					_frmGetBackPassword.SetOkEnabled(true);
				}
				break;
		}
	}

	private void SetConnectedUi(bool connected)
	{
		lblSocketStatus.Text = connected ? "已连接服务器" : "未连接服务器";
		if (_frmNewAccount is not null && !_frmNewAccount.IsDisposed)
		{
			_frmNewAccount.SetConnectionStatus(lblSocketStatus.Text);
		}
		if (_frmChangePassword is not null && !_frmChangePassword.IsDisposed)
		{
			_frmChangePassword.SetConnectionStatus(lblSocketStatus.Text);
		}
		if (_frmGetBackPassword is not null && !_frmGetBackPassword.IsDisposed)
		{
			_frmGetBackPassword.SetConnectionStatus(lblSocketStatus.Text);
		}
	}

	private async Task ConnectToServerAsync(string ip, string portText)
	{
		if (string.IsNullOrWhiteSpace(ip) || !int.TryParse(portText, out int port))
		{
			return;
		}

		lblSocketStatus.Text = "正在连接服务器...";
		try
		{
			await _client.ConnectAsync(ip.Trim(), port);
		}
		catch (Exception ex)
		{
			lblSocketStatus.Text = "连接失败";
			//MessageBox.Show(this, ex.Message, "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void TryLoadEmbeddedConfig()
	{
		if (!RcAppendedConfig.TryReadFromExe(Application.ExecutablePath, out RcAppendedConfig? cfg, out _))
		{
			lblTitle.Text = "LyoMirWorld测试登录器，仅供学习！";
			return;
		}

		_appendedConfig = cfg;
		lblTitle.Text = cfg!.DecodeExeTitle();

		try
		{
			using MemoryStream ms = new(cfg.PictureBytes);
			picBanner.Image = Image.FromStream(ms);
		}
		catch
		{
		}

		try
		{
			IniFile ini = new();
			for (int i = 0; i < cfg.Servers.Count; i++)
			{
				var server = cfg.DecodeServer(i);
				string section = server.Caption;
				ini.WriteString(section, "ServerName", server.Name);
				ini.WriteString(section, "ServerAdd", server.Ip);
				ini.WriteString(section, "ServerPort", server.Port);
				ini.WriteString(section, "WebUrl", server.WebUrl);
				ini.WriteString(section, "infoUrl", server.InfoUrl);
				ini.WriteString(section, "shopUrl", server.ShopUrl);

				_currentCaption = section;
				_csIp = server.Ip;
				_csPort = server.Port;
				_csName = server.Name;
				_csWeb = server.WebUrl;
				_csInfo = server.InfoUrl;
				_csShop = server.ShopUrl;
			}

			ini.Save(Path.Combine(AppContext.BaseDirectory, "serverlist.ini"));
		}
		catch
		{
		}
	}

	private void LoadServerListIni()
	{
		_serverIni = IniFile.Load(Path.Combine(AppContext.BaseDirectory, "serverlist.ini"));
	}

	private void PopulateServers()
	{
		cmbServers.Items.Clear();
		foreach (string section in _serverIni.Sections)
		{
			cmbServers.Items.Add(section);
		}

		if (cmbServers.Items.Count > 0)
		{
			cmbServers.SelectedIndex = cmbServers.Items.Count - 1;
		}
	}

	private void TryNavigate(string url)
	{
		try
		{
			webBrowser.Navigate(url);
		}
		catch
		{
		}
	}

	private async Task DownloadServerListIniAsync(string url)
	{
		try
		{
			using HttpResponseMessage resp = await _httpClient.GetAsync(url);
			if ((int)resp.StatusCode is not (200 or 302))
			{
				_downloadStatus = DownloadStatus.Error;
				return;
			}

			byte[] bytes = await resp.Content.ReadAsByteArrayAsync();
			if (bytes.Length == 0 || bytes[0] == 0x3D)
			{
				_downloadStatus = DownloadStatus.Error;
				return;
			}

			File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "serverlist.ini"), bytes);
			_downloadStatus = DownloadStatus.Success;
		}
		catch
		{
			_downloadStatus = DownloadStatus.Error;
		}
	}

	private void WriteGameIniFiles()
	{
		if (string.IsNullOrWhiteSpace(_csIp) || string.IsNullOrWhiteSpace(_csPort))
		{
			throw new InvalidOperationException("未选择服务器。");
		}

		string baseDir = AppContext.BaseDirectory;
		Directory.CreateDirectory(Path.Combine(baseDir, "data"));

		IniFile gameIni = new();
		gameIni.WriteString("Config", "ServerIP", _csIp);
		gameIni.WriteString("Config", "ServerPort", _csPort);
		gameIni.WriteString("Config", "GroupNum", "1");
		gameIni.WriteString("Config", "Group0", _csName);
		gameIni.WriteString("Config", "GroupNick0", _csName);
		gameIni.WriteString("Config", "Area", "1");
		gameIni.WriteString("Config", "PayServerIP", _csShop);
		gameIni.Save(Path.Combine(baseDir, "data", "game.ini"));

		IniFile websiteIni = new();
		websiteIni.WriteString("Config", "PayServerIP", _csShop);
		websiteIni.Save(Path.Combine(baseDir, "data", "website.ini"));
	}

	private void StartGameSuspended()
	{
		string baseDir = AppContext.BaseDirectory;
		string dataDir = Path.Combine(baseDir, "data");

		string gameFile = File.Exists(Path.Combine(dataDir, "woool.dat.update"))
			? Path.Combine(dataDir, "woool.dat.update")
			: Path.Combine(dataDir, "woool.dat");

		if (!File.Exists(gameFile))
		{
			throw new FileNotFoundException("找不到游戏文件。", gameFile);
		}

		_gameProcess = Win32.StartSuspended(gameFile, dataDir);
		Win32.ResumeThread(_gameProcess.Value.hThread);
	}
}
