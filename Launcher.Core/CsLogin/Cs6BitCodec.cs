using System;

namespace Launcher.Core.CsLogin;

public static class Cs6BitCodec
{
	private const byte XorKey = 0xEB;
	private const byte BaseChar = 0x3B;

	public static string Encode(ReadOnlySpan<byte> input)
	{
		if (input.IsEmpty)
		{
			return string.Empty;
		}

		int fullGroups = input.Length / 3;
		int remainder = input.Length % 3;
		int outputLength = fullGroups * 4 + remainder switch
		{
			0 => 0,
			1 => 2,
			2 => 3,
			_ => 0,
		};

		char[] output = new char[outputLength];

		byte flag2 = 0;
		int inputIndex = 0;
		int outputIndex = 0;
		int state = 0;

		while (inputIndex < input.Length)
		{
			byte value = (byte)(input[inputIndex] ^ XorKey);
			inputIndex++;

			if (state < 2)
			{
				byte calculated = (byte)(value >> 2);
				byte flag1 = calculated;
				calculated = (byte)(calculated & 0x3C);
				value = (byte)(value & 0x03);
				calculated = (byte)(calculated | value);
				output[outputIndex] = (char)(calculated + BaseChar);
				outputIndex++;

				flag2 = (byte)((flag1 & 0x03) | (flag2 << 2));
			}
			else
			{
				byte calculated = (byte)(value & 0x3F);
				output[outputIndex] = (char)(calculated + BaseChar);
				outputIndex++;

				byte shifted = (byte)((value >> 2) & 0x30);
				shifted = (byte)(shifted | flag2);
				output[outputIndex] = (char)(shifted + BaseChar);
				outputIndex++;

				flag2 = 0;
			}

			state = (state + 1) % 3;
		}

		if (state != 0)
		{
			output[outputIndex] = (char)(flag2 + BaseChar);
		}

		return new string(output);
	}

	public static byte[] Decode(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return [];
		}

		int fullGroups = input.Length / 4;
		int remainder = input.Length % 4;
		int outputLength = fullGroups * 3 + remainder switch
		{
			0 => 0,
			2 => 1,
			3 => 2,
			_ => 0,
		};

		byte[] output = new byte[outputLength];
		int outputIndex = 0;

		if (input.Length > 3)
		{
			for (int groupIndex = 0; groupIndex < fullGroups; groupIndex++)
			{
				int baseIndex = groupIndex * 4;
				byte c1 = (byte)(input[baseIndex + 0] - BaseChar);
				byte c2 = (byte)(input[baseIndex + 1] - BaseChar);
				byte c3 = (byte)(input[baseIndex + 2] - BaseChar);
				byte c4 = (byte)(input[baseIndex + 3] - BaseChar);

				byte b1 = (byte)((c1 & 0xFC) << 2);
				byte b2 = (byte)(c1 & 0x03);
				byte b3 = (byte)(c4 & 0x0C);
				output[outputIndex] = (byte)((b1 | b2 | b3) ^ XorKey);
				outputIndex++;

				b1 = (byte)((c2 & 0xFC) << 2);
				b2 = (byte)(c2 & 0x03);
				b3 = (byte)((c4 & 0x03) << 2);
				output[outputIndex] = (byte)((b1 | b2 | b3) ^ XorKey);
				outputIndex++;

				b1 = (byte)((c4 & 0x30) << 2);
				output[outputIndex] = (byte)((c3 | b1) ^ XorKey);
				outputIndex++;
			}
		}

		if (remainder == 2)
		{
			int baseIndex = fullGroups * 4;
			byte c1 = (byte)(input[baseIndex + 0] - BaseChar);
			byte c2 = (byte)(input[baseIndex + 1] - BaseChar);

			byte b1 = (byte)((c1 & 0xFC) << 2);
			byte b2 = (byte)(c1 & 0x03);
			byte b3 = (byte)((c2 & 0x03) << 2);
			output[outputIndex] = (byte)((b1 | b2 | b3) ^ XorKey);
		}
		else if (remainder == 3)
		{
			int baseIndex = fullGroups * 4;
			byte c1 = (byte)(input[baseIndex + 0] - BaseChar);
			byte c2 = (byte)(input[baseIndex + 1] - BaseChar);
			byte c4 = (byte)(input[baseIndex + 2] - BaseChar);

			byte b1 = (byte)((c1 & 0xFC) << 2);
			byte b2 = (byte)(c1 & 0x03);
			byte b3 = (byte)(c4 & 0x0C);
			output[outputIndex] = (byte)((b1 | b2 | b3) ^ XorKey);
			outputIndex++;

			b1 = (byte)((c2 & 0xFC) << 2);
			b2 = (byte)(c2 & 0x03);
			b3 = (byte)((c4 & 0x03) << 2);
			output[outputIndex] = (byte)((b1 | b2 | b3) ^ XorKey);
		}

		return output;
	}
}

