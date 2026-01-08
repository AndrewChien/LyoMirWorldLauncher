using System;
using System.ComponentModel;

namespace Launcher;

internal static class GamePatcher
{
	public static void Apply(uint processId)
	{
		IntPtr processHandle = Win32.OpenProcess(Win32.PROCESS_ALL_ACCESS, bInheritHandle: false, processId);
		if (processHandle == IntPtr.Zero)
		{
			throw new Win32Exception();
		}

		try
		{
			ApplyIfMatch(processHandle, 0x0040370E, 0xF474860F, [0xE9, 0x75, 0xF4, 0xFF, 0xFF, 0x90]);
			ApplyIfMatch(processHandle, 0x0040373A, 0xF458860F, [0xE9, 0x59, 0xF4, 0xFF, 0xFF, 0x90]);
			ApplyIfMatch(processHandle, 0x00404DAA, 0xF470860F, [0xE9, 0x71, 0xF4, 0xFF, 0xFF, 0x90]);
			ApplyIfMatch(processHandle, 0x00404F0C, 0xF45E860F, [0xE9, 0x5F, 0xF4, 0xFF, 0xFF, 0x90]);
			ApplyIfMatch(processHandle, 0x00404D1A, 0xF450860F, [0xE9, 0x51, 0xF4, 0xFF, 0xFF, 0x90]);
			ApplyIfMatch(processHandle, 0x004042B9, 0xF5E1860F, [0xE9, 0xE2, 0xF5, 0xFF, 0xFF, 0x90]);
			ApplyIfMatch(processHandle, 0x004052FD, 0x0D8B2576, [0xE9, 0xF1, 0xF5, 0xFF, 0xFF, 0x90]);
			ApplyIfMatch(processHandle, 0x00403EC2, 0x0D8B1E76, [0xE9, 0x49, 0xF6, 0xFF, 0xFF, 0x90]);
			ApplyIfMatch(processHandle, 0x00403F04, 0xF607860F, [0xE9, 0x08, 0xF6, 0xFF, 0xFF, 0x90]);
			ApplyIfMatch(processHandle, 0x00403ED4, 0xF607860F, [0xE9, 0x08, 0xF6, 0xFF, 0xFF, 0x90]);
			ApplyIfMatch(processHandle, 0x00403E92, 0x0D8B1E76, [0xEB]);
		}
		finally
		{
			Win32.CloseHandle(processHandle);
		}
	}

	private static void ApplyIfMatch(IntPtr processHandle, int address, uint expectedValue, byte[] patchBytes)
	{
		if (!Win32.ReadProcessMemory(processHandle, new IntPtr(address), out uint value, 4, out _))
		{
			return;
		}

		if (value != expectedValue)
		{
			return;
		}

		Win32.WriteProcessMemory(processHandle, new IntPtr(address), patchBytes, patchBytes.Length, out _);
	}
}

