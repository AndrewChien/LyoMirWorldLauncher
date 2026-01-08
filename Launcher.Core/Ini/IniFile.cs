using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Launcher.Core.Text;

namespace Launcher.Core.Ini;

public sealed class IniFile
{
	private static readonly Encoding GbEncoding = LegacyEncoding.Gb;
	private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<string> _sectionOrder = [];

	public IReadOnlyList<string> Sections => _sectionOrder;

	public static IniFile Load(string path)
	{
		if (!File.Exists(path))
		{
			return new IniFile();
		}

		string[] lines = File.ReadAllLines(path, GbEncoding);
		IniFile ini = new();

		string currentSection = string.Empty;
		foreach (string rawLine in lines)
		{
			string line = rawLine.Trim();
			if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
			{
				continue;
			}

			if (line.StartsWith('[') && line.EndsWith(']') && line.Length >= 2)
			{
				currentSection = line[1..^1].Trim();
				ini.EnsureSection(currentSection);
				continue;
			}

			int equalsIndex = line.IndexOf('=');
			if (equalsIndex < 0)
			{
				continue;
			}

			string key = line[..equalsIndex].Trim();
			string value = equalsIndex + 1 < line.Length ? line[(equalsIndex + 1)..].Trim() : string.Empty;
			if (key.Length == 0)
			{
				continue;
			}

			ini.WriteString(currentSection, key, value);
		}

		return ini;
	}

	public string ReadString(string section, string key, string defaultValue)
	{
		if (!_sections.TryGetValue(section ?? string.Empty, out var keys))
		{
			return defaultValue;
		}

		return keys.TryGetValue(key, out string? value) ? value : defaultValue;
	}

	public void WriteString(string section, string key, string value)
	{
		string normalizedSection = section ?? string.Empty;
		EnsureSection(normalizedSection);
		_sections[normalizedSection][key] = value ?? string.Empty;
	}

	public void Save(string path)
	{
		List<string> lines = new();
		foreach (string section in _sectionOrder)
		{
			lines.Add($"[{section}]");
			if (_sections.TryGetValue(section, out var keys))
			{
				foreach ((string key, string value) in keys.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
				{
					lines.Add($"{key}={value}");
				}
			}
		}

		File.WriteAllLines(path, lines, GbEncoding);
	}

	private void EnsureSection(string section)
	{
		if (_sections.ContainsKey(section))
		{
			return;
		}

		_sections[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		_sectionOrder.Add(section);
	}
}
