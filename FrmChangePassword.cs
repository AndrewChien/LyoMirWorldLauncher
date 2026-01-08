using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Launcher;

public sealed class FrmChangePassword : Form
{
	private readonly Func<bool> _isConnected;
	private readonly Func<string, string, string, Task> _sendAsync;

	private readonly Label _lblConnectionStatus = new() { AutoSize = true };
	private readonly TextBox _txtAccount = new();
	private readonly TextBox _txtPassword = new() { UseSystemPasswordChar = true };
	private readonly TextBox _txtNewPassword = new() { UseSystemPasswordChar = true };
	private readonly TextBox _txtConfirm = new() { UseSystemPasswordChar = true };

	private readonly Button _btnOk = new() { Text = "确定" };
	private readonly Button _btnCancel = new() { Text = "取消" };

	private DateTime _lastOkUtc = DateTime.MinValue;

	public FrmChangePassword(Func<bool> isConnected, Func<string, string, string, Task> sendAsync)
	{
		_isConnected = isConnected ?? throw new ArgumentNullException(nameof(isConnected));
		_sendAsync = sendAsync ?? throw new ArgumentNullException(nameof(sendAsync));

		Text = "修改密码";
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		ShowInTaskbar = false;

		_btnOk.Click += async (_, _) => await OnOkAsync();
		_btnCancel.Click += (_, _) => Close();

		TableLayoutPanel layout = new()
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(12),
			AutoSize = true,
			ColumnCount = 2,
		};
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

		AddRow(layout, "连接状态", _lblConnectionStatus);
		AddRow(layout, "账号", _txtAccount);
		AddRow(layout, "原密码", _txtPassword);
		AddRow(layout, "新密码", _txtNewPassword);
		AddRow(layout, "确认新密码", _txtConfirm);

		FlowLayoutPanel buttonPanel = new()
		{
			Dock = DockStyle.Bottom,
			FlowDirection = FlowDirection.RightToLeft,
			Padding = new Padding(12),
			AutoSize = true,
		};
		buttonPanel.Controls.Add(_btnCancel);
		buttonPanel.Controls.Add(_btnOk);

		Controls.Add(layout);
		Controls.Add(buttonPanel);
	}

	public void Open(IWin32Window owner)
	{
		ResetFields();
		SetOkEnabled(true);
		ShowDialog(owner);
	}

	public void SetConnectionStatus(string statusText)
	{
		_lblConnectionStatus.Text = statusText;
	}

	public void SetOkEnabled(bool enabled)
	{
		_btnOk.Enabled = enabled;
	}

	public void ResetFields()
	{
		_txtAccount.Text = string.Empty;
		_txtPassword.Text = string.Empty;
		_txtNewPassword.Text = string.Empty;
		_txtConfirm.Text = string.Empty;
	}

	private async Task OnOkAsync()
	{
		if (DateTime.UtcNow - _lastOkUtc < TimeSpan.FromSeconds(5))
		{
			MessageBox.Show(this, "操作过于频繁，请稍后再试。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		if (!_isConnected())
		{
			MessageBox.Show(this, "未连接服务器。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		string uid = _txtAccount.Text.Trim();
		string password = _txtPassword.Text.Trim();
		string newPassword = _txtNewPassword.Text.Trim();

		if (uid.Length == 0)
		{
			MessageBox.Show(this, "请输入账号。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			_txtAccount.Focus();
			return;
		}

		if (password.Length == 0)
		{
			MessageBox.Show(this, "请输入原密码。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			_txtPassword.Focus();
			return;
		}

		if (newPassword.Length == 0)
		{
			MessageBox.Show(this, "请输入新密码。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			_txtNewPassword.Focus();
			return;
		}

		if (_txtNewPassword.Text != _txtConfirm.Text)
		{
			MessageBox.Show(this, "两次输入的新密码不一致。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			_txtConfirm.Focus();
			return;
		}

		SetOkEnabled(false);
		_lastOkUtc = DateTime.UtcNow;

		try
		{
			await _sendAsync(uid, password, newPassword);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "发送失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
			SetOkEnabled(true);
		}
	}

	private static void AddRow(TableLayoutPanel layout, string labelText, Control control)
	{
		int rowIndex = layout.RowCount;
		layout.RowCount++;
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		Label label = new() { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left };
		layout.Controls.Add(label, 0, rowIndex);

		control.Dock = DockStyle.Fill;
		layout.Controls.Add(control, 1, rowIndex);
	}
}

