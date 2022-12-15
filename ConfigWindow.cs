using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
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

    public readonly Vector4 COLOR_RED = new Vector4(255, 0, 0, 255);

    public override void Draw()
    {
        var shouldChangeSelf = _plugin.Config.ShouldChangeSelf;
        var selectedCharaPreset = _plugin.Config.SelectedCharaPreset;
        var enableShangTsung = _plugin.Config.EnableShangTsung;

        if (_plugin.CharaPresets.Count > 0 && selectedCharaPreset > _plugin.CharaPresets.Count - 1)
        {
            PluginLog.Warning("SelectedCharaPreset Out of Range! Reset to 0!");
            selectedCharaPreset = 0;
        }

        ImGui.InputText("玩家数据路径##GameSaveFolder", ref _plugin.Config.GameSaveFile, 256);

        if (ImGui.Button("刷新数据"))
        {
            _plugin.CharaPresets = CharaPreset.RetrieveAll(_plugin);
        }

        ImGui.SameLine();

        if (Directory.Exists(_plugin.Config.GameSaveFile) && ImGui.Button("打开数据文件夹"))
        {
            Process.Start("explorer.exe", @_plugin.Config.GameSaveFile);
        }

        if (!Plugin.pathValid)
        {
            ImGui.TextColored(COLOR_RED, "数据文件夹无效, 请输入其他路径!");
            return;
        }

        ImGui.BeginDisabled(enableShangTsung);
        {
            ImGui.Checkbox("更改自己", ref shouldChangeSelf);

            if (_plugin.CharaPresets.Count > 0)
            {
                if (ImGui.BeginCombo("预设", _plugin.CharaPresets[selectedCharaPreset].ToString()))
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
            else if (ImGui.BeginCombo("预设", "未找到预设"))
            {
                ImGui.EndCombo();
            }
        }
        ImGui.EndDisabled();

        ImGui.Checkbox("尚宗模式", ref enableShangTsung);

        if (enableShangTsung)
        {
            ImGui.SameLine();
            if (ImGui.Button("偷取灵魂"))
            {
                _plugin.StealTargetSoul();
            }
        }

        if (enableShangTsung && Directory.Exists(_plugin.Config.GameSaveFile) && _plugin._hasSoulData)
        {
            ImGui.SameLine();
            if (ImGui.Button("保存为预设 (自动增位)"))
            {
                int fileNum = _plugin.CharaPresets.Count + 1;
                var path = Path.Combine(_plugin.Config.GameSaveFile, $"FFXIV_CHARA_{fileNum:D2}.dat");
                if (File.Exists(path))
                {
                    _plugin.ChatLog($"预设 #{fileNum} 文件已存在, 取消保存!");
                    return;
                }

                _plugin.SaveAsPreset(_plugin._shangTsung, path);

                _plugin.ChatLog($"已保存到预设 #{fileNum}!");

                _plugin.CharaPresets = CharaPreset.RetrieveAll(_plugin);
            }
        }

        if (ImGui.Button("保存自身外观为预设 (自动增位 Glamourer搭配使用)"))
        {
            int fileNum = _plugin.CharaPresets.Count + 1;
            var path = Path.Combine(_plugin.Config.GameSaveFile, $"FFXIV_CHARA_{fileNum:D2}.dat");
            if (File.Exists(path))
            {
                _plugin.ChatLog($"预设 #{fileNum} 文件已存在, 取消保存!");
                return;
            }

            var ply = _plugin.LocalPlayer;

            if (ply == null)
            {
                _plugin.ChatLog($"玩家对象为null, 这不该发生的!");
                return;
            }

            _plugin.SaveAsPreset(ply.Customize, path);

            _plugin.ChatLog($"已保存到预设 #{fileNum}!");

            _plugin.CharaPresets = CharaPreset.RetrieveAll(_plugin);
        }


        _plugin.UpdateConfig(shouldChangeSelf, selectedCharaPreset, enableShangTsung);
    }
}
