using System;
using System.Text;
using Launcher.Core.Text;

namespace Launcher.Core.RcConfig;

public static class RcCipher
{
	private static readonly Encoding GbEncoding = LegacyEncoding.Gb;

	public static byte[] Xor(ReadOnlySpan<byte> input, int key)
	{
		byte keyByte = unchecked((byte)key);
		byte[] output = new byte[input.Length];
		for (int index = 0; index < input.Length; index++)
		{
			output[index] = (byte)(input[index] ^ keyByte);
		}

		return output;
	}

	public static byte[] EncodeString(string value, int maxLength, int key)
	{
		if (string.IsNullOrEmpty(value))
		{
			return [];
		}

		byte[] bytes = GbEncoding.GetBytes(value);
		int length = Math.Min(maxLength, bytes.Length);
		return Xor(bytes.AsSpan(0, length), key);
	}

	public static string DecodeString(ReadOnlySpan<byte> encodedBytes, int key)
	{
		if (encodedBytes.IsEmpty)
		{
			return string.Empty;
		}

		byte[] decoded = Xor(encodedBytes, key);
		return GbEncoding.GetString(decoded);
	}
}
