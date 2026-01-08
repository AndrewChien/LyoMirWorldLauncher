using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Launcher.Core.RcConfig;

namespace Launcher.Configurator;

public sealed class FrmConfigurator : Form
{
	private sealed record ServerEntry(
		string Caption,
		string Name,
		string Ip,
		string Port,
		string WebUrl,
		string InfoUrl,
		string ShopUrl);

	private readonly TextBox _txtExePath = new() { ReadOnly = true };
	private readonly Button _btnBrowseExe = new() { Text = "选择EXE..." };

	private readonly PictureBox _picImage = new() { BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };
	private readonly Button _btnBrowseImage = new() { Text = "选择图片..." };

	private readonly TextBox _txtExeTitle = new();
	private readonly TextBox _txtDownloadIniUrl = new();

	private readonly ListBox _lstServers = new();

	private readonly TextBox _txtCaption = new();
	private readonly TextBox _txtName = new();
	private readonly TextBox _txtIp = new();
	private readonly TextBox _txtPort = new();
	private readonly TextBox _txtWebUrl = new();
	private readonly TextBox _txtInfoUrl = new();
	private readonly TextBox _txtShopUrl = new();

	private readonly Button _btnAdd = new() { Text = "添加" };
	private readonly Button _btnUpdate = new() { Text = "修改" };
	private readonly Button _btnDelete = new() { Text = "删除" };
	private readonly Button _btnBuild = new() { Text = "写入EXE(生成.bak)" };

	private readonly List<ServerEntry> _servers = new();

	public FrmConfigurator()
	{
		Text = "LyoMirWorld 登录器配置器";
		StartPosition = FormStartPosition.CenterScreen;
		Width = 980;
		Height = 720;

		_btnBrowseExe.Click += (_, _) => BrowseExe();
		_btnBrowseImage.Click += (_, _) => BrowseImage();
		_lstServers.SelectedIndexChanged += (_, _) => LoadSelectedServer();

		_btnAdd.Click += (_, _) => AddServer();
		_btnUpdate.Click += (_, _) => UpdateServer();
		_btnDelete.Click += (_, _) => DeleteServer();
		_btnBuild.Click += (_, _) => Build();

		TableLayoutPanel root = new()
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(12),
			ColumnCount = 2,
		};
		root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
		root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

		root.Controls.Add(BuildLeftPanel(), 0, 0);
		root.Controls.Add(BuildRightPanel(), 1, 0);
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

		Controls.Add(root);
	}

	private Control BuildLeftPanel()
	{
		TableLayoutPanel panel = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 3,
			AutoSize = true,
		};
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

		int row = 0;
		AddRow(panel, ref row, "目标EXE", _txtExePath, _btnBrowseExe);
		AddRow(panel, ref row, "标题", _txtExeTitle, null);
		AddRow(panel, ref row, "下载INI URL", _txtDownloadIniUrl, null);

		panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		panel.Controls.Add(new Label { Text = "图片(BMP)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
		panel.Controls.Add(_picImage, 1, row);
		panel.Controls.Add(_btnBrowseImage, 2, row);
		_picImage.Height = 160;
		_picImage.Dock = DockStyle.Fill;
		row++;

		panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		panel.Controls.Add(new Label { Text = "服务器列表", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
		panel.SetColumnSpan(_lstServers, 2);
		panel.Controls.Add(_lstServers, 1, row);
		_lstServers.Dock = DockStyle.Fill;
		panel.Controls.Add(_btnBuild, 2, row);
		_btnBuild.Anchor = AnchorStyles.Top;

		return panel;
	}

	private Control BuildRightPanel()
	{
		TableLayoutPanel panel = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			AutoSize = true,
		};
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

		int row = 0;
		AddRow(panel, ref row, "区名(ServerCaption)", _txtCaption);
		AddRow(panel, ref row, "显示名(ServerName)", _txtName);
		AddRow(panel, ref row, "IP(ServerAdd)", _txtIp);
		AddRow(panel, ref row, "端口(ServerPort)", _txtPort);
		AddRow(panel, ref row, "官网(WebUrl)", _txtWebUrl);
		AddRow(panel, ref row, "公告/信息(InfoUrl)", _txtInfoUrl);
		AddRow(panel, ref row, "充值/商店(ShopUrl)", _txtShopUrl);

		FlowLayoutPanel buttons = new()
		{
			Dock = DockStyle.Bottom,
			FlowDirection = FlowDirection.LeftToRight,
			AutoSize = true,
			Padding = new Padding(0, 12, 0, 0),
		};
		buttons.Controls.Add(_btnAdd);
		buttons.Controls.Add(_btnUpdate);
		buttons.Controls.Add(_btnDelete);

		panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		panel.Controls.Add(buttons, 0, row);
		panel.SetColumnSpan(buttons, 2);

		return panel;
	}

	private static void AddRow(TableLayoutPanel panel, ref int row, string label, Control valueControl, Control? trailingControl = null)
	{
		panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
		valueControl.Dock = DockStyle.Fill;
		panel.Controls.Add(valueControl, 1, row);
		if (panel.ColumnCount >= 3 && trailingControl is not null)
		{
			panel.Controls.Add(trailingControl, 2, row);
		}
		row++;
	}

	private void BrowseExe()
	{
		using OpenFileDialog ofd = new()
		{
			Title = "选择需要写入配置的 EXE",
			Filter = "EXE (*.exe)|*.exe|所有文件 (*.*)|*.*",
		};
		if (ofd.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}

		_txtExePath.Text = ofd.FileName;
	}

	private void BrowseImage()
	{
		using OpenFileDialog ofd = new()
		{
			Title = "选择图片",
			Filter = "图片 (*.bmp;*.png;*.jpg)|*.bmp;*.png;*.jpg|所有文件 (*.*)|*.*",
		};
		if (ofd.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}

		using System.Drawing.Image img = System.Drawing.Image.FromFile(ofd.FileName);
		_picImage.Image?.Dispose();
		_picImage.Image = new System.Drawing.Bitmap(img);
	}

	private void LoadSelectedServer()
	{
		int index = _lstServers.SelectedIndex;
		if (index < 0 || index >= _servers.Count)
		{
			return;
		}

		ServerEntry entry = _servers[index];
		_txtCaption.Text = entry.Caption;
		_txtName.Text = entry.Name;
		_txtIp.Text = entry.Ip;
		_txtPort.Text = entry.Port;
		_txtWebUrl.Text = entry.WebUrl;
		_txtInfoUrl.Text = entry.InfoUrl;
		_txtShopUrl.Text = entry.ShopUrl;
	}

	private void AddServer()
	{
		ServerEntry entry = ReadEntryFromInputs();
		_servers.Add(entry);
		RefreshServerList();
		_lstServers.SelectedIndex = _servers.Count - 1;
	}

	private void UpdateServer()
	{
		int index = _lstServers.SelectedIndex;
		if (index < 0 || index >= _servers.Count)
		{
			return;
		}

		_servers[index] = ReadEntryFromInputs();
		RefreshServerList();
		_lstServers.SelectedIndex = index;
	}

	private void DeleteServer()
	{
		int index = _lstServers.SelectedIndex;
		if (index < 0 || index >= _servers.Count)
		{
			return;
		}

		_servers.RemoveAt(index);
		RefreshServerList();
	}

	private ServerEntry ReadEntryFromInputs()
	{
		string caption = _txtCaption.Text.Trim();
		string name = _txtName.Text.Trim();
		string ip = _txtIp.Text.Trim();
		string port = _txtPort.Text.Trim();
		string web = _txtWebUrl.Text.Trim();
		string info = _txtInfoUrl.Text.Trim();
		string shop = _txtShopUrl.Text.Trim();

		if (string.IsNullOrWhiteSpace(caption))
		{
			throw new InvalidOperationException("区名不能为空。");
		}
		if (string.IsNullOrWhiteSpace(ip))
		{
			throw new InvalidOperationException("IP 不能为空。");
		}
		if (string.IsNullOrWhiteSpace(port))
		{
			throw new InvalidOperationException("端口不能为空。");
		}

		return new ServerEntry(caption, name, ip, port, web, info, shop);
	}

	private void RefreshServerList()
	{
		_lstServers.BeginUpdate();
		try
		{
			_lstServers.Items.Clear();
			foreach (ServerEntry server in _servers)
			{
				_lstServers.Items.Add(server.Caption);
			}
		}
		finally
		{
			_lstServers.EndUpdate();
		}
	}

	private void Build()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_txtExePath.Text) || !File.Exists(_txtExePath.Text))
			{
				throw new InvalidOperationException("请选择有效的 EXE 文件。");
			}

			if (_picImage.Image is null)
			{
				throw new InvalidOperationException("请选择图片。");
			}

			if (_servers.Count == 0)
			{
				throw new InvalidOperationException("请至少添加一个服务器。");
			}

			using MemoryStream ms = new();
			_picImage.Image.Save(ms, ImageFormat.Bmp);
			byte[] picBytes = ms.ToArray();

			var servers = _servers
				.Select(s => (s.Caption, s.Name, s.Ip, s.Port, s.WebUrl, s.InfoUrl, s.ShopUrl))
				.ToList();

			RcAppendedConfig.AppendToExe(
				targetExePath: _txtExePath.Text,
				exeTitle: _txtExeTitle.Text,
				downloadIniUrl: _txtDownloadIniUrl.Text,
				servers: servers,
				pictureBytes: picBytes,
				createBackup: true);

			MessageBox.Show(this, "写入成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
}
