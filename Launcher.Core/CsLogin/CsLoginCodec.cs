using System;
using System.Buffers.Binary;
using System.Text;
using Launcher.Core.Text;

namespace Launcher.Core.CsLogin;

public static class CsLoginCodec
{
	private static readonly Encoding GbEncoding = LegacyEncoding.Gb;

	public static string EncodeMessage(DefaultMessage message)
	{
		Span<byte> buffer = stackalloc byte[12];
		BinaryPrimitives.WriteInt32LittleEndian(buffer[..4], message.Recog);
		BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(4, 2), message.Ident);
		BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(6, 2), message.Param);
		BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(8, 2), message.Tag);
		BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(10, 2), message.Series);
		return Cs6BitCodec.Encode(buffer);
	}

	public static DefaultMessage DecodeMessage(string encodedHead)
	{
		byte[] decodedBytes = Cs6BitCodec.Decode(encodedHead);
		if (decodedBytes.Length < 12)
		{
			throw new ArgumentException("Encoded head is too short.", nameof(encodedHead));
		}

		ReadOnlySpan<byte> buffer = decodedBytes.AsSpan(0, 12);
		int recog = BinaryPrimitives.ReadInt32LittleEndian(buffer[..4]);
		ushort ident = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(4, 2));
		ushort param = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(6, 2));
		ushort tag = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(8, 2));
		ushort series = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(10, 2));
		return new DefaultMessage(recog, ident, param, tag, series);
	}

	public static string EncodeString(string value)
	{
		byte[] bytes = GbEncoding.GetBytes(value);
		return Cs6BitCodec.Encode(bytes);
	}

	public static string DecodeString(string encoded)
	{
		byte[] decodedBytes = Cs6BitCodec.Decode(encoded);
		int terminatorIndex = Array.IndexOf(decodedBytes, (byte)0);
		ReadOnlySpan<byte> span = terminatorIndex >= 0 ? decodedBytes.AsSpan(0, terminatorIndex) : decodedBytes;
		return GbEncoding.GetString(span);
	}

	public static string EncodeBuffer(ReadOnlySpan<byte> buffer)
	{
		if (buffer.Length >= CsLoginConstants.BufferSize)
		{
			return string.Empty;
		}

		return Cs6BitCodec.Encode(buffer);
	}

	public static void DecodeBuffer(string encoded, Span<byte> destination)
	{
		byte[] decodedBytes = Cs6BitCodec.Decode(encoded);
		if (decodedBytes.Length < destination.Length)
		{
			throw new ArgumentException("Decoded buffer is smaller than destination.", nameof(encoded));
		}

		decodedBytes.AsSpan(0, destination.Length).CopyTo(destination);
	}

	public static DefaultMessage MakeDefaultMessage(ushort ident, int recog, ushort param, ushort tag, ushort series)
	{
		return new DefaultMessage(recog, ident, param, tag, series);
	}
}
