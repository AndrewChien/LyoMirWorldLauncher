using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Launcher;

internal static class Win32
{
	public const uint CREATE_SUSPENDED = 0x00000004;
	public const uint STARTF_USESHOWWINDOW = 0x00000001;
	public const short SW_NORMAL = 1;

	public const uint PROCESS_ALL_ACCESS = 0x001F0FFF;

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct STARTUPINFO
	{
		public int cb;
		public string? lpReserved;
		public string? lpDesktop;
		public string? lpTitle;
		public int dwX;
		public int dwY;
		public int dwXSize;
		public int dwYSize;
		public int dwXCountChars;
		public int dwYCountChars;
		public int dwFillAttribute;
		public uint dwFlags;
		public short wShowWindow;
		public short cbReserved2;
		public IntPtr lpReserved2;
		public IntPtr hStdInput;
		public IntPtr hStdOutput;
		public IntPtr hStdError;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct PROCESS_INFORMATION
	{
		public IntPtr hProcess;
		public IntPtr hThread;
		public uint dwProcessId;
		public uint dwThreadId;
	}

	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern bool CreateProcess(
		string? lpApplicationName,
		string? lpCommandLine,
		IntPtr lpProcessAttributes,
		IntPtr lpThreadAttributes,
		bool bInheritHandles,
		uint dwCreationFlags,
		IntPtr lpEnvironment,
		string? lpCurrentDirectory,
		ref STARTUPINFO lpStartupInfo,
		out PROCESS_INFORMATION lpProcessInformation);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern uint ResumeThread(IntPtr hThread);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool ReadProcessMemory(
		IntPtr hProcess,
		IntPtr lpBaseAddress,
		out uint lpBuffer,
		int dwSize,
		out IntPtr lpNumberOfBytesRead);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool WriteProcessMemory(
		IntPtr hProcess,
		IntPtr lpBaseAddress,
		byte[] lpBuffer,
		int dwSize,
		out IntPtr lpNumberOfBytesWritten);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool CloseHandle(IntPtr hObject);

	public static PROCESS_INFORMATION StartSuspended(string applicationPath, string workingDirectory)
	{
		STARTUPINFO startupInfo = new()
		{
			cb = Marshal.SizeOf<STARTUPINFO>(),
			dwFlags = STARTF_USESHOWWINDOW,
			wShowWindow = SW_NORMAL,
		};

		if (!CreateProcess(
				lpApplicationName: applicationPath,
				lpCommandLine: null,
				lpProcessAttributes: IntPtr.Zero,
				lpThreadAttributes: IntPtr.Zero,
				bInheritHandles: true,
				dwCreationFlags: CREATE_SUSPENDED,
				lpEnvironment: IntPtr.Zero,
				lpCurrentDirectory: workingDirectory,
				lpStartupInfo: ref startupInfo,
				lpProcessInformation: out PROCESS_INFORMATION processInfo))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		return processInfo;
	}
}

