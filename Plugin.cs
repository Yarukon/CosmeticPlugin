using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace Cosmetic;

public class Plugin : IDalamudPlugin
{
    public string Name => "Cosmetic";

    public Configuration Config { get; }

    private static readonly short[,] RaceStarterGearIdMap = {
        {84, 85}, // Hyur
		{86, 87}, // Elezen
		{92, 93}, // Lalafell
		{88, 89}, // Miqo
		{90, 91}, // Roe
		{257, 258}, // Au Ra
		{597, -1}, // Hrothgar
		{-1, 581}, // Viera
	};

    private const string CharaMountedAddr =
        "40 53 48 83 EC 20 48 8B 01 48 8B D9 FF 50 10 83 F8 08 75 08 B0 01 48 83 C4 20 5B C3 48 8B 03 48";

    private const string CharaInitAddr =
        "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B F9 48 8B EA 48 81 C1 ?? ?? ?? ?? E8 ?? ?? ?? ??";

    private const string FlagSlotAddr = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A";

    // private const uint CharaWindowActorId = 0xE0000000;

    private const int OffsetRenderToggle = 0x104;

    private const uint InvisFlag = (1 << 1) | (1 << 11);

    private static readonly short[] RaceStarterGearIds;

    public List<CharaPreset> CharaPresets;
    private PlayerCharacter? _localPlayer;
    private IntPtr _lastActor;
    private byte _lastPlayerGender;
    private Race _lastPlayerRace;
    private bool _lastWasModified;
    private bool _lastWasPlayer;
    private readonly byte[] _shangTsung = new byte[28];
    public bool _hasSoulData;

    private delegate IntPtr CharacterInitialize(IntPtr actorPtr, IntPtr customizeDataPtr);
    private delegate IntPtr CharacterIsMounted(IntPtr actor);
    private delegate IntPtr FlagSlotUpdate(IntPtr actorPtr, uint slot, IntPtr equipData);

    private readonly Hook<CharacterInitialize> _charaInitHook;
    private readonly Hook<CharacterIsMounted> _charaMountedHook;
    private readonly Hook<FlagSlotUpdate> _flagSlotUpdateHook;

    private PlayerCharacter? LocalPlayer => _localPlayer ??= Service.ClientState.LocalPlayer;

    private readonly WindowSystem _windowSystem = new(nameof(Cosmetic));

    static Plugin()
    {
        RaceStarterGearIds = RaceStarterGearIdMap.Cast<short>().Where(id => id != -1).ToArray();
    }

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        _windowSystem.AddWindow(new ConfigWindow(this));

        Service.Interface.UiBuilder.Draw += _windowSystem.Draw;
        Service.Interface.UiBuilder.OpenConfigUi += OpenConfigWindow;

        Service.Commands.AddHandler("/xlcosmetic", new CommandInfo(OnCommand)
        {
            HelpMessage = "打开Cosmetic设置窗口.",
            ShowInHelp = true
        });

        Service.Commands.AddHandler("/stealsoul", new CommandInfo(OnCommand)
        {
            HelpMessage = "变形/偷取目标玩家的灵魂.",
            ShowInHelp = true
        });

        var charaInitPtr = Service.SigScanner.ScanText(CharaInitAddr);
        _charaInitHook ??= Hook<CharacterInitialize>.FromAddress(charaInitPtr, CharacterInitializeDetour);
        _charaInitHook.Enable();

        var charaMountedPtr = Service.SigScanner.ScanText(CharaMountedAddr);
        _charaMountedHook ??= Hook<CharacterIsMounted>.FromAddress(charaMountedPtr, CharacterIsMountedDetour);
        _charaMountedHook.Enable();

        var flagSlotPtr = Service.SigScanner.ScanText(FlagSlotAddr);
        _flagSlotUpdateHook ??= Hook<FlagSlotUpdate>.FromAddress(flagSlotPtr, FlagSlotUpdateDetour);
        _flagSlotUpdateHook.Enable();

        CharaPresets = CharaPreset.RetrieveAll(this);
        RefreshLocalPlayer();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private IntPtr CharacterInitializeDetour(IntPtr drawObjectBase, IntPtr customizeDataPtr)
    {
        if (!_lastWasPlayer || LocalPlayer == null)
            return _charaInitHook.Original(drawObjectBase, customizeDataPtr);

        _lastWasModified = false;
        var actor = Service.ObjectTable.CreateObjectReference(_lastActor);

        if (actor == null || actor.ObjectId != LocalPlayer.ObjectId ||
            (!Config.ShouldChangeSelf && !Config.EnableShangTsung))
            return _charaInitHook.Original(drawObjectBase, customizeDataPtr);

        if (_hasSoulData && Config.EnableShangTsung)
            Marshal.Copy(_shangTsung, 0, customizeDataPtr, 26);
        else if (Config.ShouldChangeSelf && CharaPresets.Count > 0)
            Marshal.Copy(CharaPresets[Config.SelectedCharaPreset].CustomizeData, 0, customizeDataPtr, 26);

        _lastPlayerRace = (Race)CharaPresets[Config.SelectedCharaPreset].CustomizeData[0];
        _lastPlayerGender = CharaPresets[Config.SelectedCharaPreset].CustomizeData[1];
        _lastWasModified = true;

        return _charaInitHook.Original(drawObjectBase, customizeDataPtr);
    }

    private IntPtr CharacterIsMountedDetour(IntPtr actorPtr)
    {
        var actor = Service.ObjectTable.CreateObjectReference(actorPtr);
        if (actor == null || actor.ObjectKind != ObjectKind.Player)
        {
            _lastWasPlayer = false;
            return _charaMountedHook.Original(actorPtr);
        }

        _lastActor = actorPtr;
        _lastWasPlayer = true;

        return _charaMountedHook.Original(actorPtr);
    }

    private IntPtr FlagSlotUpdateDetour(IntPtr actorPtr, uint slot, IntPtr equipDataPtr)
    {
        if (_lastWasPlayer && _lastWasModified)
            Marshal.StructureToPtr(
                MapRacialEquipModels(_lastPlayerRace, _lastPlayerGender, Marshal.PtrToStructure<EquipData>(equipDataPtr)),
                equipDataPtr, true);

        return _flagSlotUpdateHook.Original(actorPtr, slot, equipDataPtr);
    }

    private void RefreshLocalPlayer()
    {
        if (LocalPlayer == null)
            return;

        var player = Service.ObjectTable.CreateObjectReference(LocalPlayer.Address);
        if (player == null || player.ObjectKind != ObjectKind.Player)
            return;

        RerenderActor(player);
    }

    private static async void RerenderActor(GameObject actor)
    {
        try
        {
            var renderToggleAddr = actor.Address + OffsetRenderToggle;
            var val = Marshal.ReadInt32(renderToggleAddr);

            val |= (int)InvisFlag;
            Marshal.WriteInt32(renderToggleAddr, val);
            await Task.Delay(300);
            val &= ~(int)InvisFlag;
            Marshal.WriteInt32(renderToggleAddr, val);
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex.ToString());
        }
    }

    public void StealTargetSoul()
    {
        if (LocalPlayer == null) return;

        try
        {
            var target = LocalPlayer.TargetObject;

            if (!target || target is not PlayerCharacter chara || chara.ObjectId == LocalPlayer.ObjectId)
                return;

            Array.Copy(chara.Customize, _shangTsung, 28);

            _hasSoulData = true;
            RefreshLocalPlayer();
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex.ToString());
        }
    }

    // From Anamensis
    public void SaveShangTsungData(string filePath)
    {
        byte[] saveData = new byte[32];
        Array.Copy(_shangTsung, saveData, _shangTsung.Length);

        // Unix time
        byte[] unixTime = BitConverter.GetBytes(DateTimeOffset.Now.ToUnixTimeSeconds());
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(unixTime);
        Array.Copy(unixTime, 0, saveData, 0x1C, 4);

        // Calculate checksum
        int checksum = 0;
        for (int i = 0; i < saveData.Length; i++)
            checksum ^= saveData[i] << (i % 24);

        byte[] chkDigest = BitConverter.GetBytes(checksum);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(chkDigest);

        // Save data to buffer
        byte[] buffer = new byte[0xD4];

        using MemoryStream stream = new MemoryStream(buffer);
        using BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(0x2013FF14); // Magic
        writer.Write(0x03);  // Version
        writer.Seek(0x08, 0);
        writer.Write(chkDigest); // Checksum
        writer.Seek(0x10, 0);
        writer.Write(saveData); // Appearance + Timestamp

        using var fileStream = File.Create(filePath);
        fileStream.Write(buffer, 0, buffer.Length);
    }

    public void ChatLog(string message)
    {
        Service.ChatGui.Print($"[{Name}] {message}");
    }

    private static EquipData MapRacialEquipModels(Race race, int gender, EquipData eq)
    {
        if (Array.IndexOf(RaceStarterGearIds, eq.model) <= -1) return eq;

        eq.model = RaceStarterGearIdMap[(byte)(race - 1), gender];
        eq.variant = 1;

        return eq;
    }

    public void UpdateConfig(string gameDataFolder, bool shouldChangeSelf, int selectedCharaPreset, bool enableShangTsung)
    {
        var unsavedChanges = shouldChangeSelf != Config.ShouldChangeSelf ||
                             selectedCharaPreset != Config.SelectedCharaPreset ||
                             enableShangTsung != Config.EnableShangTsung ||
                             gameDataFolder != Config.GameSaveFile;

        if (!unsavedChanges) return;

        Config.GameSaveFile = gameDataFolder;
        Config.ShouldChangeSelf = shouldChangeSelf;
        if (CharaPresets.Count > 0)
        {
            Config.SelectedCharaPreset = Math.Clamp(selectedCharaPreset, 0, CharaPresets.Count - 1);
        }
        Config.EnableShangTsung = enableShangTsung;
        Config.Save();

        RefreshLocalPlayer();
    }

    private void OpenConfigWindow()
    {
        CharaPresets = CharaPreset.RetrieveAll(this);
        _windowSystem.GetWindow(nameof(Cosmetic))!.IsOpen = true;
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;

        Service.Interface.UiBuilder.OpenConfigUi -= OpenConfigWindow;
        Service.Interface.UiBuilder.Draw -= _windowSystem.Draw;

        _charaMountedHook.Disable();
        _charaInitHook.Disable();
        _flagSlotUpdateHook.Disable();

        _charaMountedHook.Dispose();
        _charaInitHook.Dispose();
        _flagSlotUpdateHook.Dispose();

        Service.Commands.RemoveHandler("/xlcosmetic");
        Service.Commands.RemoveHandler("/stealsoul");

        Config.Save();
        RefreshLocalPlayer();
    }

    private void OnCommand(string command, string args)
    {
        switch (command)
        {
            case "/xlcosmetic":
                OpenConfigWindow();
                break;
            case "/stealsoul":
                StealTargetSoul();
                break;
        }
    }
}
