using System.Text;

namespace Launcher.Core.Text;

public static class LegacyEncoding
{
	static LegacyEncoding()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		Gb = Encoding.GetEncoding(936);
		UTF = Encoding.UTF8;
    }

	public static Encoding Gb { get; }
    public static Encoding UTF { get; }
}
