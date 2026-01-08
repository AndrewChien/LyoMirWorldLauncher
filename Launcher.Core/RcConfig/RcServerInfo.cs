namespace Launcher.Core.RcConfig;

public sealed record RcServerInfo(
	RcShortString ServerName,
	RcShortString ServerCaption,
	RcShortString ServerIp,
	RcShortString ServerPort,
	RcShortString ServerUrl,
	RcShortString InfoUrl,
	RcShortString ShopUrl
)
{
	public const int Size = 192;
}

