using System.Collections.Generic;
using CS2_CustomVotes.Shared.Models;
using CSSharpUtils.Extensions;
using CSSharpUtils.Utils;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using hvhgg_essentials.Extensions;

namespace hvhgg_essentials.Features;

public class TeleportFix
{
	private readonly Plugin _plugin;

	private readonly Dictionary<uint, float> _teleportBlockWarnings = new Dictionary<uint, float>();

	public static readonly FakeConVar<bool> hvh_restrict_teleport = new FakeConVar<bool>("hvh_restrict_teleport", "Restricts players from teleporting/airstucking and crashing the server", true, ConVarFlags.FCVAR_REPLICATED);

	public TeleportFix(Plugin plugin)
	{
		_plugin = plugin;
		_plugin.RegisterFakeConVars(this);
		hvh_restrict_teleport.Value = _plugin.Config.RestrictTeleport;
	}

	public static void RegisterCustomVotes(Plugin plugin)
	{
		if (plugin.Config.CustomVoteSettings.TeleportFixVote)
		{
			string defaultOption = (plugin.Config.RestrictTeleport ? "Block" : "Allow");
			Plugin.CustomVotesApi.Get()?.AddCustomVote("teleport", "Teleport/Airstuck", defaultOption, 30f, new Dictionary<string, VoteOption>
			{
				{
					"Allow",
					new VoteOption("{Green}Allow", new List<string> { "hvh_restrict_teleport 0" })
				},
				{
					"Block",
					new VoteOption("{Red}Block", new List<string> { "hvh_restrict_teleport 1" })
				}
			}, plugin.Config.CustomVoteSettings.Style);
		}
	}

	public static void UnregisterCustomVotes(Plugin plugin)
	{
		Plugin.CustomVotesApi.Get()?.RemoveCustomVote("teleport");
	}

	public HookResult RunCommand(DynamicHook h)
	{
		if (!hvh_restrict_teleport.Value)
		{
			return HookResult.Continue;
		}
		CCSPlayerController cCSPlayerController = h.GetParam<CCSPlayer_MovementServices>(0).Pawn.Value.Controller.Value?.As<CCSPlayerController>();
		if (!cCSPlayerController.IsPlayer())
		{
			return HookResult.Continue;
		}
		QAngle viewAngles = new CUserCmd(h.GetParam<nint>(1)).GetViewAngles();
		if (viewAngles == null || viewAngles.IsValid())
		{
			return HookResult.Continue;
		}
		bool flag = viewAngles.IsReasonable();
		viewAngles.Fix();
		if (_teleportBlockWarnings.TryGetValue(cCSPlayerController.Index, out var value) && !(value + 3f <= Server.CurrentTime))
		{
			return HookResult.Changed;
		}
		if (!_plugin.Config.PrintWarnings)
		{
			return HookResult.Changed;
		}
		string value2;
		if (flag)
		{
			value2 = "fake up/down";
		}
		else
		{
			value2 = (cCSPlayerController.Pawn.Value.As<CCSPlayerPawn>().OnGroundLastTick ? "teleport" : "airstuck");
		}
		Server.PrintToChatAll($"{ChatUtils.FormatMessage(_plugin.Config.ChatPrefix)} Игрок {ChatColors.Red}{cCSPlayerController.PlayerName}{ChatColors.Default} Использовал {ChatColors.Red}{value2}{ChatColors.Default}!");
		_teleportBlockWarnings[cCSPlayerController.Index] = Server.CurrentTime;
		return HookResult.Changed;
	}
}
