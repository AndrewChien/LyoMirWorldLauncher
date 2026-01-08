using System;
using System.Text;
using Launcher.Core.Text;

namespace Launcher.Core.CsLogin;

public sealed record UserEntry(
	string Account,
	string Password,
	string UserName,
	string SSNo,
	string Phone,
	string Quiz,
	string Answer,
	string Email
);

public sealed record UserEntryAdd(
	string Quiz2,
	string Answer2,
	string BirthDay,
	string MobilePhone,
	string Memo,
	string Memo2
);

public static class UserRecordCodec
{
	private static readonly Encoding GbEncoding = LegacyEncoding.Gb;

	public static byte[] EncodeUserEntry(UserEntry entry)
	{
		byte[] buffer = new byte[161];
		int offset = 0;
		offset += WriteShortString(buffer, offset, entry.Account, 10);
		offset += WriteShortString(buffer, offset, entry.Password, 10);
		offset += WriteShortString(buffer, offset, entry.UserName, 20);
		offset += WriteShortString(buffer, offset, entry.SSNo, 19);
		offset += WriteShortString(buffer, offset, entry.Phone, 14);
		offset += WriteShortString(buffer, offset, entry.Quiz, 20);
		offset += WriteShortString(buffer, offset, entry.Answer, 20);
		offset += WriteShortString(buffer, offset, entry.Email, 40);
		return buffer;
	}

	public static byte[] EncodeUserEntryAdd(UserEntryAdd entry)
	{
		byte[] buffer = new byte[109];
		int offset = 0;
		offset += WriteShortString(buffer, offset, entry.Quiz2, 20);
		offset += WriteShortString(buffer, offset, entry.Answer2, 20);
		offset += WriteShortString(buffer, offset, entry.BirthDay, 10);
		offset += WriteShortString(buffer, offset, entry.MobilePhone, 13);
		offset += WriteShortString(buffer, offset, entry.Memo, 20);
		offset += WriteShortString(buffer, offset, entry.Memo2, 20);
		return buffer;
	}

	private static int WriteShortString(byte[] destination, int offset, string value, int maxLength)
	{
		int fieldSize = maxLength + 1;

		Array.Clear(destination, offset, fieldSize);
		if (string.IsNullOrEmpty(value))
		{
			destination[offset] = 0;
			return fieldSize;
		}

		byte[] contentBytes = GbEncoding.GetBytes(value);
		int length = Math.Min(maxLength, contentBytes.Length);
		destination[offset] = (byte)length;
		contentBytes.AsSpan(0, length).CopyTo(destination.AsSpan(offset + 1, length));
		return fieldSize;
	}
}
