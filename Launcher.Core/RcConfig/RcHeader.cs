using System;
using System.Linq;
using System.Text;

namespace Launcher.Core.RcConfig;

public sealed record RcHeader(
	RcShortString HasRcId,
	ushort ServerCount,
	RcShortString ExeTitle,
	RcShortString ExeVer,
	ushort ExeType,
	int PicSize,
	int NoticeSize,
	RcShortString DownloadIni
)
{
	public const int Size = 111;

	private static readonly byte[] MarkerBytes = RcCipher.Xor(Encoding.ASCII.GetBytes("ckdsmfvju"), 1);

	public bool HasValidMarker => HasRcId.ContentBytes.SequenceEqual(MarkerBytes);

	public static RcShortString CreateMarker()
	{
		return new RcShortString(20, MarkerBytes);
	}
}

