using System;
using System.IO;
using Dalamud.Configuration;

namespace Cosmetic;

[Serializable]
public class Configuration : IPluginConfiguration
{
	public int Version { get; set; } = 1;

	public string GameSaveFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "FINAL FANTASY XIV - A Realm Reborn");

    public bool ShouldChangeSelf { get; set; }

	public int SelectedCharaPreset { get; set; }

	public bool EnableShangTsung { get; set; }

	public void Save() => Service.Interface.SavePluginConfig(this);
}
