using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Launcher.Core.Compatibility;
using Launcher.Core.CsLogin;

namespace Launcher;

public sealed class FrmNewAccount : Form
{
	private readonly Func<bool> _isConnected;
	private readonly Func<UserEntry, UserEntryAdd, Task> _sendNewAccountAsync;

	private readonly Label _lblConnectionStatus = new() { AutoSize = true };
	private readonly TextBox _txtAccount = new();
	private readonly TextBox _txtPassword = new() { UseSystemPasswordChar = true };
	private readonly TextBox _txtConfirm = new() { UseSystemPasswordChar = true };
	private readonly TextBox _txtBirthDay = new();
	private readonly TextBox _txtQuiz1 = new();
	private readonly TextBox _txtAnswer1 = new();
	private readonly TextBox _txtQuiz2 = new();
	private readonly TextBox _txtAnswer2 = new();
	private readonly TextBox _txtEmail = new();
	private readonly TextBox _txtPhone = new();
	private readonly TextBox _txtMobilePhone = new();

	private readonly Button _btnOk = new() { Text = "确定" };
	private readonly Button _btnCancel = new() { Text = "取消" };

	private DateTime _lastOkUtc = DateTime.MinValue;

	public FrmNewAccount(Func<bool> isConnected, Func<UserEntry, UserEntryAdd, Task> sendNewAccountAsync)
	{
		_isConnected = isConnected ?? throw new ArgumentNullException(nameof(isConnected));
		_sendNewAccountAsync = sendNewAccountAsync ?? throw new ArgumentNullException(nameof(sendNewAccountAsync));

		Text = "注册账号";
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
		AddRow(layout, "密码", _txtPassword);
		AddRow(layout, "确认密码", _txtConfirm);
		AddRow(layout, "生日(yyyy/mm/dd)", _txtBirthDay);
		AddRow(layout, "密保问题1", _txtQuiz1);
		AddRow(layout, "答案1", _txtAnswer1);
		AddRow(layout, "密保问题2", _txtQuiz2);
		AddRow(layout, "答案2", _txtAnswer2);
		AddRow(layout, "邮箱", _txtEmail);
		AddRow(layout, "电话", _txtPhone);
		AddRow(layout, "手机", _txtMobilePhone);

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

	private void ResetFields()
	{
		_txtAccount.Text = string.Empty;
		_txtPassword.Text = string.Empty;
		_txtConfirm.Text = string.Empty;
		_txtBirthDay.Text = string.Empty;
		_txtQuiz1.Text = string.Empty;
		_txtAnswer1.Text = string.Empty;
		_txtQuiz2.Text = string.Empty;
		_txtAnswer2.Text = string.Empty;
		_txtEmail.Text = string.Empty;
		_txtPhone.Text = string.Empty;
		_txtMobilePhone.Text = string.Empty;
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

		if (!CheckInputs())
		{
			return;
		}

		UserEntry entry = new(
			Account: _txtAccount.Text.Trim().ToLowerInvariant(),
			Password: _txtPassword.Text,
			UserName: _txtAccount.Text.Trim().ToLowerInvariant(),
			SSNo: "650101-1455111",
			Phone: _txtPhone.Text,
			Quiz: _txtQuiz1.Text,
			Answer: _txtAnswer1.Text.Trim(),
			Email: _txtEmail.Text.Trim()
		);

		UserEntryAdd entryAdd = new(
			Quiz2: _txtQuiz2.Text,
			Answer2: _txtAnswer2.Text.Trim(),
			BirthDay: _txtBirthDay.Text,
			MobilePhone: _txtMobilePhone.Text,
			Memo: string.Empty,
			Memo2: string.Empty
		);

		SetOkEnabled(false);
		_lastOkUtc = DateTime.UtcNow;

		try
		{
			await _sendNewAccountAsync(entry, entryAdd);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "发送失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
			SetOkEnabled(true);
		}
	}

	private bool CheckInputs()
	{
		_txtAccount.Text = _txtAccount.Text.Trim();
		_txtQuiz1.Text = _txtQuiz1.Text.Trim();

		if (_txtAccount.Text.Length < 3)
		{
			MessageBox.Show(this, "账号长度不能少于 3。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			_txtAccount.Focus();
			return false;
		}

		if (!CheckBirthDay())
		{
			return false;
		}

		if (_txtPassword.Text.Length < 3)
		{
			_txtPassword.Focus();
			return false;
		}

		if (_txtPassword.Text != _txtConfirm.Text)
		{
			_txtConfirm.Focus();
			return false;
		}

		if (_txtQuiz1.Text.Length < 1)
		{
			_txtQuiz1.Focus();
			return false;
		}

		if (_txtAnswer1.Text.Length < 1)
		{
			_txtAnswer1.Focus();
			return false;
		}

		if (_txtQuiz2.Text.Length < 1)
		{
			_txtQuiz2.Focus();
			return false;
		}

		if (_txtAnswer2.Text.Length < 1)
		{
			_txtAnswer2.Focus();
			return false;
		}

		return true;
	}

	private bool CheckBirthDay()
	{
		string source = _txtBirthDay.Text.Trim();
		source = DelphiCompat.GetValidStr3(source, out string year, '/');
		source = DelphiCompat.GetValidStr3(source, out string month, '/');
		DelphiCompat.GetValidStr3(source, out string day, '/');

		int y = DelphiCompat.StrToInt(year, 0);
		int m = DelphiCompat.StrToInt(month, 0);
		int d = DelphiCompat.StrToInt(day, 0);

		bool ok = true;
		if (y <= 1890 || y > 2101) ok = false;
		if (m <= 0 || m > 12) ok = false;
		if (d <= 0 || d > 31) ok = false;

		if (ok)
		{
			return true;
		}

		System.Media.SystemSounds.Beep.Play();
		_txtBirthDay.Focus();
		return false;
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
