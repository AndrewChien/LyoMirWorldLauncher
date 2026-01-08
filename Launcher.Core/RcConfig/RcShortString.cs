using System;

namespace Launcher.Core.RcConfig;

public readonly struct RcShortString
{
	public RcShortString(int maxLength, byte[] contentBytes)
	{
		if (maxLength < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Max length must be non-negative.");
		}

		MaxLength = maxLength;
		ContentBytes = contentBytes ?? throw new ArgumentNullException(nameof(contentBytes));
		if (contentBytes.Length > maxLength)
		{
			throw new ArgumentOutOfRangeException(nameof(contentBytes), "Content length exceeds max length.");
		}
	}

	public int MaxLength { get; }
	public byte[] ContentBytes { get; }

	public byte[] ToFieldBytes()
	{
		byte[] field = new byte[MaxLength + 1];
		int length = Math.Min(MaxLength, ContentBytes.Length);
		field[0] = (byte)length;
		if (length > 0)
		{
			ContentBytes.AsSpan(0, length).CopyTo(field.AsSpan(1, length));
		}
		return field;
	}

	public static RcShortString FromFieldBytes(ReadOnlySpan<byte> fieldBytes, int maxLength)
	{
		if (fieldBytes.Length != maxLength + 1)
		{
			throw new ArgumentException("Unexpected short string field size.", nameof(fieldBytes));
		}

		int length = fieldBytes[0];
		if (length > maxLength)
		{
			length = maxLength;
		}

		byte[] content = length == 0 ? [] : fieldBytes.Slice(1, length).ToArray();
		return new RcShortString(maxLength, content);
	}
}

