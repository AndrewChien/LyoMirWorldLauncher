using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core.Compatibility;

namespace Launcher.Core.CsLogin;

public sealed class CsLoginClient : IAsyncDisposable
{
	private static readonly Encoding SocketEncoding = Encoding.ASCII;
	private readonly object _gate = new();
	private readonly StringBuilder _receiveBuffer = new();
	private readonly SemaphoreSlim _sendLock = new(1, 1);

	private TcpClient? _tcpClient;
	private NetworkStream? _stream;
	private CancellationTokenSource? _receiveCts;
	private Task? _receiveTask;
	private byte _sequence = 1;

	public bool IsConnected
	{
		get
		{
			lock (_gate)
			{
				return _tcpClient?.Connected == true && _stream is not null;
			}
		}
	}

	public event EventHandler? Connected;
	public event EventHandler? Disconnected;
	public event EventHandler<Exception>? ReceiveLoopError;
	public event EventHandler<CsLoginPacketReceivedEventArgs>? PacketReceived;

	public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
	{
		Disconnect();

		TcpClient tcpClient = new();
		await tcpClient.ConnectAsync(host, port, cancellationToken);

		NetworkStream stream = tcpClient.GetStream();
		CancellationTokenSource receiveCts = new();

		lock (_gate)
		{
			_tcpClient = tcpClient;
			_stream = stream;
			_receiveCts = receiveCts;
			_receiveTask = Task.Run(() => ReceiveLoopAsync(receiveCts.Token));
		}

		Connected?.Invoke(this, EventArgs.Empty);
	}

	public void Disconnect()
	{
		CancellationTokenSource? receiveCts;
		NetworkStream? stream;
		TcpClient? tcpClient;

		lock (_gate)
		{
			receiveCts = _receiveCts;
			stream = _stream;
			tcpClient = _tcpClient;

			_receiveCts = null;
			_stream = null;
			_tcpClient = null;
			_receiveTask = null;

			_receiveBuffer.Clear();
			_sequence = 1;
		}

		try
		{
			receiveCts?.Cancel();
		}
		catch
		{
		}

		try
		{
			stream?.Close();
		}
		catch
		{
		}

		try
		{
			tcpClient?.Close();
		}
		catch
		{
		}
	}

	public async Task SendFramedAsync(string payload, CancellationToken cancellationToken = default)
	{
		if (payload is null)
		{
			throw new ArgumentNullException(nameof(payload));
		}

		byte sequence;
		lock (_gate)
		{
			sequence = _sequence;
			_sequence++;
			if (_sequence >= 10)
			{
				_sequence = 1;
			}
		}

		string framed = $"#{sequence}{payload}!";
		await SendRawAsync(framed, cancellationToken);
	}

	public Task SendKeepAliveAckAsync(CancellationToken cancellationToken = default)
	{
		return SendRawAsync("*", cancellationToken);
	}

	public Task SendChangePasswordAsync(string account, string password, string newPassword, CancellationToken cancellationToken = default)
	{
		DefaultMessage msg = CsLoginCodec.MakeDefaultMessage(CsLoginConstants.CM_CHANGEPASSWORD, 0, 0, 0, 0);
		string payload = CsLoginCodec.EncodeMessage(msg) + CsLoginCodec.EncodeString($"{account}\t{password}\t{newPassword}");
		return SendFramedAsync(payload, cancellationToken);
	}

	public Task SendGetBackPasswordAsync(
		string account,
		string quest1,
		string answer1,
		string quest2,
		string answer2,
		string birthDay,
		CancellationToken cancellationToken = default)
	{
		DefaultMessage msg = CsLoginCodec.MakeDefaultMessage(CsLoginConstants.CM_GETBACKPASSWORD, 0, 0, 0, 0);
		string payload = CsLoginCodec.EncodeMessage(msg) + CsLoginCodec.EncodeString(
			$"{account}\t{quest1}\t{answer1}\t{quest2}\t{answer2}\t{birthDay}");
		return SendFramedAsync(payload, cancellationToken);
	}

	public Task SendNewAccountAsync(UserEntry entry, UserEntryAdd entryAdd, CancellationToken cancellationToken = default)
	{
		DefaultMessage msg = CsLoginCodec.MakeDefaultMessage(CsLoginConstants.CM_ADDNEWUSER, 0, 0, 0, 0);

		byte[] entryBytes = UserRecordCodec.EncodeUserEntry(entry);
		byte[] entryAddBytes = UserRecordCodec.EncodeUserEntryAdd(entryAdd);

		string payload =
			CsLoginCodec.EncodeMessage(msg) +
			CsLoginCodec.EncodeBuffer(entryBytes) +
			CsLoginCodec.EncodeBuffer(entryAddBytes);

		return SendFramedAsync(payload, cancellationToken);
	}

	public async ValueTask DisposeAsync()
	{
		Task? receiveTask;
		CancellationTokenSource? receiveCts;

		lock (_gate)
		{
			receiveTask = _receiveTask;
			receiveCts = _receiveCts;
		}

		Disconnect();

		if (receiveCts is not null)
		{
			receiveCts.Dispose();
		}

		if (receiveTask is not null)
		{
			try
			{
				await receiveTask;
			}
			catch
			{
			}
		}

		_sendLock.Dispose();
	}

	private async Task SendRawAsync(string text, CancellationToken cancellationToken)
	{
		NetworkStream stream = GetStreamOrThrow();
		byte[] bytes = SocketEncoding.GetBytes(text);

		await _sendLock.WaitAsync(cancellationToken);
		try
		{
			await stream.WriteAsync(bytes, cancellationToken);
		}
		finally
		{
			_sendLock.Release();
		}
	}

	private NetworkStream GetStreamOrThrow()
	{
		lock (_gate)
		{
			if (_stream is null)
			{
				throw new InvalidOperationException("Socket is not connected.");
			}

			return _stream;
		}
	}

	private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
	{
		try
		{
			NetworkStream stream = GetStreamOrThrow();
			byte[] buffer = new byte[4096];

			while (!cancellationToken.IsCancellationRequested)
			{
				int read = await stream.ReadAsync(buffer, cancellationToken);
				if (read <= 0)
				{
					break;
				}

				string chunk = SocketEncoding.GetString(buffer, 0, read);
				if (chunk.Contains('*', StringComparison.Ordinal))
				{
					chunk = chunk.Replace("*", string.Empty, StringComparison.Ordinal);
					await SendKeepAliveAckAsync(cancellationToken);
				}

				lock (_gate)
				{
					_receiveBuffer.Append(chunk);
				}

				ProcessReceiveBuffer();
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			ReceiveLoopError?.Invoke(this, ex);
		}
		finally
		{
			Disconnect();
			Disconnected?.Invoke(this, EventArgs.Empty);
		}
	}

	private void ProcessReceiveBuffer()
	{
		while (true)
		{
			string currentBuffer;
			lock (_gate)
			{
				currentBuffer = _receiveBuffer.ToString();
			}

			if (currentBuffer.Length < 2 || currentBuffer.IndexOf('!', StringComparison.Ordinal) < 0)
			{
				return;
			}

			string remaining = DelphiCompat.ArrestStringEx(currentBuffer, "#", "!", out string packet);
			lock (_gate)
			{
				_receiveBuffer.Clear();
				_receiveBuffer.Append(remaining);
			}

			if (packet.Length == 0)
			{
				continue;
			}

			HandlePacket(packet);
		}
	}

	private void HandlePacket(string datablock)
	{
		if (datablock.Length == 0)
		{
			return;
		}

		if (datablock[0] == '+')
		{
			return;
		}

		if (datablock.Length < CsLoginConstants.DefBlockSize)
		{
			return;
		}

		string head = datablock[..CsLoginConstants.DefBlockSize];
		string body = datablock.Length > CsLoginConstants.DefBlockSize ? datablock[CsLoginConstants.DefBlockSize..] : string.Empty;

		DefaultMessage message;
		try
		{
			message = CsLoginCodec.DecodeMessage(head);
		}
		catch
		{
			return;
		}

		PacketReceived?.Invoke(this, new CsLoginPacketReceivedEventArgs(message, body));
	}
}

