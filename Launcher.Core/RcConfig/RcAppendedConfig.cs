using System;
using System.Collections.Generic;
using System.IO;

namespace Launcher.Core.RcConfig;

public sealed class RcAppendedConfig
{
	private RcAppendedConfig(RcHeader header, IReadOnlyList<RcServerInfo> servers, byte[] pictureBytes)
	{
		Header = header;
		Servers = servers;
		PictureBytes = pictureBytes;
	}

	public RcHeader Header { get; }
	public IReadOnlyList<RcServerInfo> Servers { get; }
	public byte[] PictureBytes { get; }

	public static bool TryReadFromExe(string exePath, out RcAppendedConfig? config, out string? errorMessage)
	{
		config = null;
		errorMessage = null;

		if (string.IsNullOrWhiteSpace(exePath))
		{
			errorMessage = "Executable path is empty.";
			return false;
		}

		if (!File.Exists(exePath))
		{
			errorMessage = "Executable file does not exist.";
			return false;
		}

		using FileStream stream = File.Open(exePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		if (stream.Length < RcHeader.Size)
		{
			return false;
		}

		stream.Seek(-RcHeader.Size, SeekOrigin.End);
		byte[] headerBytes = ReadExact(stream, RcHeader.Size);
		RcHeader header = ParseHeader(headerBytes);

		if (!header.HasValidMarker)
		{
			return false;
		}

		long suffixSize = (long)RcHeader.Size + (long)RcServerInfo.Size * header.ServerCount + header.PicSize;
		if (suffixSize <= 0 || stream.Length < suffixSize)
		{
			errorMessage = "Appended config size is invalid.";
			return false;
		}

		long pictureStart = stream.Length - suffixSize;
		long serverInfoStart = stream.Length - (RcHeader.Size + (long)RcServerInfo.Size * header.ServerCount);

		stream.Seek(pictureStart, SeekOrigin.Begin);
		byte[] pictureBytes = ReadExact(stream, header.PicSize);

		stream.Seek(serverInfoStart, SeekOrigin.Begin);
		List<RcServerInfo> servers = new(capacity: header.ServerCount);
		for (int index = 0; index < header.ServerCount; index++)
		{
			byte[] serverBytes = ReadExact(stream, RcServerInfo.Size);
			servers.Add(ParseServerInfo(serverBytes));
		}

		config = new RcAppendedConfig(header, servers, pictureBytes);
		return true;
	}

	public static void AppendToExe(
		string targetExePath,
		string exeTitle,
		string downloadIniUrl,
		IReadOnlyList<(string Caption, string Name, string Ip, string Port, string WebUrl, string InfoUrl, string ShopUrl)> servers,
		byte[] pictureBytes,
		bool createBackup = true)
	{
		if (!File.Exists(targetExePath))
		{
			throw new FileNotFoundException("Target executable not found.", targetExePath);
		}

		if (HasMarker(targetExePath))
		{
			throw new InvalidOperationException("Target executable already contains appended config.");
		}

		if (createBackup)
		{
			File.Copy(targetExePath, targetExePath + ".bak", overwrite: true);
		}

		if (pictureBytes is null || pictureBytes.Length == 0)
		{
			throw new ArgumentException("Picture bytes are empty.", nameof(pictureBytes));
		}

		int picSize = pictureBytes.Length;
		byte keyByte = unchecked((byte)picSize);

		RcHeader header = new(
			HasRcId: RcHeader.CreateMarker(),
			ServerCount: checked((ushort)servers.Count),
			ExeTitle: new RcShortString(20, RcCipher.EncodeString(exeTitle, 20, keyByte)),
			ExeVer: new RcShortString(5, []),
			ExeType: 1,
			PicSize: picSize,
			NoticeSize: 0,
			DownloadIni: new RcShortString(50, RcCipher.EncodeString(downloadIniUrl, 50, keyByte))
		);

		using FileStream stream = File.Open(targetExePath, FileMode.Append, FileAccess.Write, FileShare.Read);
		stream.Write(pictureBytes, 0, pictureBytes.Length);
		foreach (var server in servers)
		{
			RcServerInfo info = new(
				ServerName: new RcShortString(20, RcCipher.EncodeString(server.Name, 20, keyByte)),
				ServerCaption: new RcShortString(20, RcCipher.EncodeString(server.Caption, 20, keyByte)),
				ServerIp: new RcShortString(15, RcCipher.EncodeString(server.Ip, 15, keyByte)),
				ServerPort: new RcShortString(10, RcCipher.EncodeString(server.Port, 10, keyByte)),
				ServerUrl: new RcShortString(40, RcCipher.EncodeString(server.WebUrl, 40, keyByte)),
				InfoUrl: new RcShortString(40, RcCipher.EncodeString(server.InfoUrl, 40, keyByte)),
				ShopUrl: new RcShortString(40, RcCipher.EncodeString(server.ShopUrl, 40, keyByte))
			);

			byte[] serverBytes = SerializeServerInfo(info);
			stream.Write(serverBytes, 0, serverBytes.Length);
		}

		byte[] serializedHeader = SerializeHeader(header);
		stream.Write(serializedHeader, 0, serializedHeader.Length);
	}

	public string DecodeExeTitle() => RcCipher.DecodeString(Header.ExeTitle.ContentBytes, Header.PicSize);
	public string DecodeDownloadIniUrl() => RcCipher.DecodeString(Header.DownloadIni.ContentBytes, Header.PicSize);

	public (string Caption, string Name, string Ip, string Port, string WebUrl, string InfoUrl, string ShopUrl) DecodeServer(int index)
	{
		RcServerInfo server = Servers[index];
		int key = Header.PicSize;
		return (
			Caption: RcCipher.DecodeString(server.ServerCaption.ContentBytes, key),
			Name: RcCipher.DecodeString(server.ServerName.ContentBytes, key),
			Ip: RcCipher.DecodeString(server.ServerIp.ContentBytes, key),
			Port: RcCipher.DecodeString(server.ServerPort.ContentBytes, key),
			WebUrl: RcCipher.DecodeString(server.ServerUrl.ContentBytes, key),
			InfoUrl: RcCipher.DecodeString(server.InfoUrl.ContentBytes, key),
			ShopUrl: RcCipher.DecodeString(server.ShopUrl.ContentBytes, key)
		);
	}

	private static byte[] ReadExact(Stream stream, int size)
	{
		byte[] buffer = new byte[size];
		int read = 0;
		while (read < size)
		{
			int chunk = stream.Read(buffer, read, size - read);
			if (chunk <= 0)
			{
				throw new EndOfStreamException();
			}
			read += chunk;
		}

		return buffer;
	}

	private static bool HasMarker(string exePath)
	{
		using FileStream stream = File.Open(exePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		if (stream.Length < RcHeader.Size)
		{
			return false;
		}

		stream.Seek(-RcHeader.Size, SeekOrigin.End);
		byte[] headerBytes = ReadExact(stream, RcHeader.Size);
		RcHeader header = ParseHeader(headerBytes);
		return header.HasValidMarker;
	}

	private static RcHeader ParseHeader(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length != RcHeader.Size)
		{
			throw new ArgumentException("Invalid header size.", nameof(bytes));
		}

		int offset = 0;
		RcShortString hasRcId = RcShortString.FromFieldBytes(bytes.Slice(offset, 21), 20);
		offset += 21;
		ushort serverCount = BitConverter.ToUInt16(bytes.Slice(offset, 2));
		offset += 2;
		RcShortString exeTitle = RcShortString.FromFieldBytes(bytes.Slice(offset, 21), 20);
		offset += 21;
		RcShortString exeVer = RcShortString.FromFieldBytes(bytes.Slice(offset, 6), 5);
		offset += 6;
		ushort exeType = BitConverter.ToUInt16(bytes.Slice(offset, 2));
		offset += 2;
		int picSize = BitConverter.ToInt32(bytes.Slice(offset, 4));
		offset += 4;
		int noticeSize = BitConverter.ToInt32(bytes.Slice(offset, 4));
		offset += 4;
		RcShortString downloadIni = RcShortString.FromFieldBytes(bytes.Slice(offset, 51), 50);

		return new RcHeader(hasRcId, serverCount, exeTitle, exeVer, exeType, picSize, noticeSize, downloadIni);
	}

	private static byte[] SerializeHeader(RcHeader header)
	{
		using MemoryStream stream = new();
		stream.Write(header.HasRcId.ToFieldBytes());
		stream.Write(BitConverter.GetBytes(header.ServerCount));
		stream.Write(header.ExeTitle.ToFieldBytes());
		stream.Write(header.ExeVer.ToFieldBytes());
		stream.Write(BitConverter.GetBytes(header.ExeType));
		stream.Write(BitConverter.GetBytes(header.PicSize));
		stream.Write(BitConverter.GetBytes(header.NoticeSize));
		stream.Write(header.DownloadIni.ToFieldBytes());
		return stream.ToArray();
	}

	private static RcServerInfo ParseServerInfo(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length != RcServerInfo.Size)
		{
			throw new ArgumentException("Invalid server info size.", nameof(bytes));
		}

		int offset = 0;
		RcShortString serverName = RcShortString.FromFieldBytes(bytes.Slice(offset, 21), 20);
		offset += 21;
		RcShortString serverCaption = RcShortString.FromFieldBytes(bytes.Slice(offset, 21), 20);
		offset += 21;
		RcShortString serverIp = RcShortString.FromFieldBytes(bytes.Slice(offset, 16), 15);
		offset += 16;
		RcShortString serverPort = RcShortString.FromFieldBytes(bytes.Slice(offset, 11), 10);
		offset += 11;
		RcShortString serverUrl = RcShortString.FromFieldBytes(bytes.Slice(offset, 41), 40);
		offset += 41;
		RcShortString infoUrl = RcShortString.FromFieldBytes(bytes.Slice(offset, 41), 40);
		offset += 41;
		RcShortString shopUrl = RcShortString.FromFieldBytes(bytes.Slice(offset, 41), 40);

		return new RcServerInfo(serverName, serverCaption, serverIp, serverPort, serverUrl, infoUrl, shopUrl);
	}

	private static byte[] SerializeServerInfo(RcServerInfo server)
	{
		using MemoryStream stream = new();
		stream.Write(server.ServerName.ToFieldBytes());
		stream.Write(server.ServerCaption.ToFieldBytes());
		stream.Write(server.ServerIp.ToFieldBytes());
		stream.Write(server.ServerPort.ToFieldBytes());
		stream.Write(server.ServerUrl.ToFieldBytes());
		stream.Write(server.InfoUrl.ToFieldBytes());
		stream.Write(server.ShopUrl.ToFieldBytes());
		return stream.ToArray();
	}
}
