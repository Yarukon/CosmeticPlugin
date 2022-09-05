using System;
using Dalamud.Configuration;

namespace Cosmetic;

[Serializable]
public class Configuration : IPluginConfiguration
{
	public int Version { get; set; } = 1;

	public bool ShouldChangeSelf { get; set; }

	public int SelectedCharaPreset { get; set; }

	public bool EnableShangTsung { get; set; }

	public void Save() => Service.Interface.SavePluginConfig(this);
}
