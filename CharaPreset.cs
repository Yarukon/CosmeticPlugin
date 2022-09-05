using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Logging;

namespace Cosmetic;

public readonly struct CharaPreset
{
	private static readonly Dictionary<byte, string> ByteToRace = new() {
		{ 0, "Unknown" },
		{ 1, "Hyur" },
		{ 2, "Elezen" },
		{ 3, "Lalafell" },
		{ 4, "Miqo'te" },
		{ 5, "Roegadyn" },
		{ 6, "Au Ra" },
		{ 7, "Hrothgar" },
		{ 8, "Viera" }
	};

	private static readonly Dictionary<byte, string> ByteToTribe = new() {
		{ 0, "Unknown" },
		{ 1, "Midlander" },
		{ 2, "Highlander" },
		{ 3, "Wildwood" },
		{ 4, "Duskwight" },
		{ 5, "Plainsfolk" },
		{ 6, "Dunesfolk" },
		{ 7, "Seeker of the Sun" },
		{ 8, "Keeper of the Moon" },
		{ 9, "Sea Wolf" },
		{ 10, "Hellsguard" },
		{ 11, "Raen" },
		{ 12, "Xaela" },
		{ 13, "Helions" },
		{ 14, "The Lost" },
		{ 15, "Rava" },
		{ 16, "Veena" }
	};
	
	public readonly byte[] CustomizeData;
	
	private readonly int _number;
	private readonly string _description;

	private CharaPreset(byte[] buffer, int fileNumber)
	{
		using var memoryStream = new MemoryStream(buffer);
		using var binaryReader = new BinaryReader(memoryStream);
		
		_number = fileNumber;
		memoryStream.Seek(16L, 0);
		CustomizeData = binaryReader.ReadBytes(0x1A);
		memoryStream.Seek(48L, 0);
		_description = Regex.Replace(Encoding.ASCII.GetString(
			binaryReader.ReadBytes(0xA4)), "(?![ -~]|\\r|\\n).", "");
		
		if (_description.Length > 0)
			return;
		
		_description = "No Description.";
	}

	public override string ToString()
	{
		return $"(#{_number}) " +
		       $"{ByteToRace[CustomizeData[(int)CustomizeIndex.Race]]} - {ByteToTribe[CustomizeData[(int)CustomizeIndex.Tribe]]} " +
		       $"{(CustomizeData[(int)CustomizeIndex.Gender] == 1 ? "♀" : "♂")} - {_description}";
	}
	
	public static List<CharaPreset> RetrieveAll()
	{
		var presetDataList = new List<CharaPreset>();
		var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games",
			"FINAL FANTASY XIV - A Realm Reborn");
		
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
