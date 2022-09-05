using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Cosmetic;

public class ConfigWindow : Window, IDisposable
{
	private readonly Plugin _plugin;

	public ConfigWindow(Plugin plugin) : base(nameof(Cosmetic), 
		ImGuiWindowFlags.AlwaysAutoResize)
	{
		_plugin = plugin;
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
	
	public override void Draw()
	{
		var shouldChangeSelf = _plugin.Config.ShouldChangeSelf;
		var selectedCharaPreset = _plugin.Config.SelectedCharaPreset;
		var enableShangTsung = _plugin.Config.EnableShangTsung;

		ImGui.BeginDisabled(enableShangTsung);
		{
			ImGui.Checkbox("Change Self", ref shouldChangeSelf);
			
			if (_plugin.CharaPresets.Count > 0)
			{
				if (ImGui.BeginCombo("Preset", _plugin.CharaPresets[selectedCharaPreset].ToString()))
				{
					for (var index = 0; index < _plugin.CharaPresets.Count; ++index)
					{
						var characterPreset = _plugin.CharaPresets[index];
						var selected = index == selectedCharaPreset;
						ImGui.PushID(selectedCharaPreset);
						if (ImGui.Selectable(characterPreset.ToString(), selected))
							selectedCharaPreset = index;
						if (selected)
							ImGui.SetItemDefaultFocus();
						ImGui.PopID();
					}

					ImGui.EndCombo();
				}
			}
			else if (ImGui.BeginCombo("Preset", "No presets found"))
			{
				ImGui.EndCombo();
			}
		}
		ImGui.EndDisabled();

		ImGui.Checkbox("Enable Shang Tsung", ref enableShangTsung);
		
		if (enableShangTsung && ImGui.Button("Steal Soul"))
			_plugin.StealTargetSoul();

		_plugin.UpdateConfig(shouldChangeSelf, selectedCharaPreset, enableShangTsung);
	}
}
