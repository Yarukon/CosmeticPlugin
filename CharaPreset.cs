using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Logging;
using Dalamud.Utility;

namespace Cosmetic;

public readonly struct CharaPreset
{
	private static readonly Dictionary<byte, string> ByteToRace = new() {
		{ 0, "未知" },
		{ 1, "人族" },
		{ 2, "精灵族" },
		{ 3, "拉拉菲尔族" },
		{ 4, "猫魅族" },
		{ 5, "鲁加族" },
		{ 6, "敖龙族" },
		{ 7, "硌狮族" },
		{ 8, "维埃拉族" }
	};

	private static readonly Dictionary<byte, string> ByteToTribe = new() {
		{ 0, "未知" },
		{ 1, "中原之民" },
		{ 2, "高地之民" },
		{ 3, "森林之民" },
		{ 4, "黑影之民" },
		{ 5, "平原之民" },
		{ 6, "沙漠之民" },
		{ 7, "逐日之民" },
		{ 8, "护月之民" },
		{ 9, "北洋之民" },
		{ 10, "红焰之民" },
		{ 11, "晨曦之民" },
		{ 12, "暮晖之民" },
		{ 13, "掠日之民" },
		{ 14, "迷踪之民" },
		{ 15, "密林之民" },
		{ 16, "山林之民" }
	};
	
	public readonly byte[] CustomizeData;
	
	private readonly int _number;

	private CharaPreset(byte[] buffer, int fileNumber)
	{
		using var memoryStream = new MemoryStream(buffer);
		using var binaryReader = new BinaryReader(memoryStream);
		
		_number = fileNumber;
		memoryStream.Seek(16L, 0);
		CustomizeData = binaryReader.ReadBytes(0x1A);
	}

	public override string ToString()
	{
		return $"(#{_number}) " +
		       $"{ByteToRace[CustomizeData[(int)CustomizeIndex.Race]]} - {ByteToTribe[CustomizeData[(int)CustomizeIndex.Tribe]]} " +
		       $"{(CustomizeData[(int)CustomizeIndex.Gender] == 1 ? "♀" : "♂")}";
	}
	
	public static List<CharaPreset> RetrieveAll(Plugin plugin)
	{
		var presetDataList = new List<CharaPreset>();
		var path = plugin.Config.GameSaveFile;
		
		if (!Directory.Exists(path))
		{
			PluginLog.Error("Could not find FFXIV Directory: " + path);
			return presetDataList;
		}

		presetDataList.AddRange(from file in Directory.GetFiles(path, "FFXIV_CHARA_*.dat")
			let filenum = int.Parse(new string(Path.GetFileNameWithoutExtension(file).Where(char.IsDigit).ToArray()))
			select new CharaPreset(File.ReadAllBytes(file), filenum));

		return presetDataList;
	}
}
