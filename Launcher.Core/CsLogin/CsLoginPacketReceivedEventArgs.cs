using System;

namespace Launcher.Core.CsLogin;

public sealed class CsLoginPacketReceivedEventArgs(DefaultMessage message, string body) : EventArgs
{
	public DefaultMessage Message { get; } = message;
	public string Body { get; } = body;
}

