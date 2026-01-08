using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Launcher;

public sealed class FrmGetBackPassword : Form
{
	private readonly Func<bool> _isConnected;
	private readonly Func<string, string, string, string, string, string, Task> _sendAsync;

	private readonly Label _lblConnectionStatus = new() { AutoSize = true };
	private readonly TextBox _txtAccount = new();
	private readonly TextBox _txtQuiz1 = new();
	private readonly TextBox _txtAnswer1 = new();
	private readonly TextBox _txtQuiz2 = new();
	private readonly TextBox _txtAnswer2 = new();
	private readonly TextBox _txtBirthDay = new();

	private readonly Button _btnOk = new() { Text = "确定" };
	private readonly Button _btnCancel = new() { Text = "取消" };

	private DateTime _lastOkUtc = DateTime.MinValue;

	public FrmGetBackPassword(Func<bool> isConnected, Func<string, string, string, string, string, string, Task> sendAsync)
	{
		_isConnected = isConnected ?? throw new ArgumentNullException(nameof(isConnected));
		_sendAsync = sendAsync ?? throw new ArgumentNullException(nameof(sendAsync));

		Text = "找回密码";
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
		AddRow(layout, "问题1", _txtQuiz1);
		AddRow(layout, "答案1", _txtAnswer1);
		AddRow(layout, "问题2", _txtQuiz2);
		AddRow(layout, "答案2", _txtAnswer2);
		AddRow(layout, "生日(yyyy/mm/dd)", _txtBirthDay);

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
		_txtQuiz1.Text = string.Empty;
		_txtAnswer1.Text = string.Empty;
		_txtQuiz2.Text = string.Empty;
		_txtAnswer2.Text = string.Empty;
		_txtBirthDay.Text = string.Empty;
	}

	private async Task OnOkAsync()
	{
		if (DateTime.UtcNow - _lastOkUtc < TimeSpan.FromSeconds(10))
		{
			MessageBox.Show(this, "请等待 10 秒后再尝试。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		if (!_isConnected())
		{
			MessageBox.Show(this, "未连接服务器。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		_lastOkUtc = DateTime.UtcNow;

		string account = _txtAccount.Text.Trim();
		string q1 = _txtQuiz1.Text.Trim();
		string a1 = _txtAnswer1.Text.Trim();
		string q2 = _txtQuiz2.Text.Trim();
		string a2 = _txtAnswer2.Text.Trim();
		string birthDay = _txtBirthDay.Text.Trim();

		if (account.Length == 0)
		{
			MessageBox.Show(this, "请输入账号。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			_txtAccount.Focus();
			return;
		}

		if (q1.Length == 0 && a1.Length == 0 && q2.Length == 0 && a2.Length == 0)
		{
			MessageBox.Show(this, "请填写至少一组密保问题和答案。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		if (birthDay.Length == 0)
		{
			MessageBox.Show(this, "请输入生日。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			_txtBirthDay.Focus();
			return;
		}

		if (q1.Length == 0) q1 = "test";
		if (a1.Length == 0) a1 = "test";
		if (q2.Length == 0) q2 = "test";
		if (a2.Length == 0) a2 = "test";

		SetOkEnabled(false);

		try
		{
			await _sendAsync(account, q1, a1, q2, a2, birthDay);
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

