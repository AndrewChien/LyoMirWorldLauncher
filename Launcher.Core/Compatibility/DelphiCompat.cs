using System;

namespace Launcher.Core.Compatibility;

public static class DelphiCompat
{
	public static string GetValidStr3(string source, out string dest, params char[] divider)
	{
		dest = string.Empty;
		if (string.IsNullOrEmpty(source))
		{
			return string.Empty;
		}

		int dividerIndex = source.IndexOfAny(divider);
		if (dividerIndex < 0)
		{
			dest = source;
			return string.Empty;
		}

		dest = source[..dividerIndex];
		return dividerIndex + 1 < source.Length ? source[(dividerIndex + 1)..] : string.Empty;
	}

	public static int StrToInt(string? source, int defaultValue)
	{
		if (string.IsNullOrWhiteSpace(source))
		{
			return defaultValue;
		}

		return int.TryParse(source, out int value) ? value : defaultValue;
	}

	public static string ArrestStringEx(string source, string searchAfter, string arrestBefore, out string arrestStr)
	{
		arrestStr = string.Empty;
		if (string.IsNullOrEmpty(source))
		{
			return string.Empty;
		}

		bool goodData = false;
		if (source.StartsWith(searchAfter, StringComparison.Ordinal))
		{
			source = source[searchAfter.Length..];
			goodData = true;
		}
		else
		{
			int index = source.IndexOf(searchAfter, StringComparison.Ordinal);
			if (index >= 0)
			{
				source = source[(index + searchAfter.Length)..];
				goodData = true;
			}
		}

		if (!goodData)
		{
			return source;
		}

		int endIndex = source.IndexOf(arrestBefore, StringComparison.Ordinal);
		if (endIndex < 0)
		{
			return searchAfter + source;
		}

		arrestStr = source[..endIndex];
		return endIndex + arrestBefore.Length < source.Length ? source[(endIndex + arrestBefore.Length)..] : string.Empty;
	}
}

