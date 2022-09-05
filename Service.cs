using System.Diagnostics.CodeAnalysis;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace Cosmetic;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
internal class Service
{
	[PluginService, RequiredVersion("1.0")]
	internal static DalamudPluginInterface Interface { get; private set; } = null!;

	[PluginService, RequiredVersion("1.0")]
	internal static CommandManager Commands { get; private set; } = null!;

	[PluginService, RequiredVersion("1.0")] 
	internal static SigScanner SigScanner { get; private set; } = null!;
	
	[PluginService, RequiredVersion("1.0")] 
	internal static  ObjectTable ObjectTable { get; private set; } = null!;

	[PluginService, RequiredVersion("1.0")] 
	internal static  ClientState ClientState { get; private set; } = null!;
}
