using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CS2_CustomVotes.Shared;
using CS2_CustomVotes.Shared.Models;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace hvhgg_essentials.Features;

public class FriendlyFire
{
	private readonly Plugin _plugin;

	public static readonly FakeConVar<bool> hvh_unmatched_friendlyfire = new FakeConVar<bool>("hvh_unmatched_friendlyfire", "Enables unmatched.gg style friendly fire (utility only)", true, ConVarFlags.FCVAR_REPLICATED);

	private static readonly string[] AllowedDamageInflicter = new string[6] { "inferno", "hegrenade_projectile", "flashbang_projectile", "smokegrenade_projectile", "decoy_projectile", "planted_c4" };

	public FriendlyFire(Plugin plugin)
	{
		_plugin = plugin;
		_plugin.RegisterFakeConVars(this);
		hvh_unmatched_friendlyfire.Value = _plugin.Config.UnmatchedFriendlyFire;
	}

	public static void RegisterCustomVotes(Plugin plugin)
	{
		if (plugin.Config.CustomVoteSettings.FriendlyFireVote)
		{
			string defaultOption = (plugin.Config.UnmatchedFriendlyFire ? "Enable" : "Disable");
			ICustomVoteApi? customVoteApi = Plugin.CustomVotesApi.Get();
			if (customVoteApi != null)
			{
				List<string> aliases = new List<string> { "ff" };
				Dictionary<string, VoteOption> dictionary = new Dictionary<string, VoteOption>();
				int num = 1;
				List<string> list = new List<string>(num);
				CollectionsMarshal.SetCount(list, num);
				Span<string> span = CollectionsMarshal.AsSpan(list);
				int num2 = 0;
				span[num2] = "hvh_unmatched_friendlyfire 0";
				num2++;
				dictionary.Add("Enable", new VoteOption("{Green}Enable", list));
				num2 = 1;
				List<string> list2 = new List<string>(num2);
				CollectionsMarshal.SetCount(list2, num2);
				span = CollectionsMarshal.AsSpan(list2);
				num = 0;
				span[num] = "hvh_unmatched_friendlyfire 1";
				num++;
				dictionary.Add("Disable", new VoteOption("{Red}Disable", list2));
				customVoteApi.AddCustomVote("friendlyfire", aliases, "unmatched.gg friendly fire", defaultOption, 30f, dictionary, plugin.Config.CustomVoteSettings.Style);
			}
		}
	}

	public static void UnregisterCustomVotes(Plugin plugin)
	{
		Plugin.CustomVotesApi.Get()?.RemoveCustomVote("friendlyfire");
	}

	public HookResult OnTakeDamage(DynamicHook hook)
	{
		if (!hvh_unmatched_friendlyfire.Value)
		{
			return HookResult.Continue;
		}
		try
		{
			CEntityInstance param = hook.GetParam<CEntityInstance>(0);
			CTakeDamageInfo param2 = hook.GetParam<CTakeDamageInfo>(1);
			if (param2.Attacker.Value == null || !param2.Attacker.Value.IsValid)
			{
				return HookResult.Continue;
			}
			CCSPlayerPawn cCSPlayerPawn = new CCSPlayerPawn(param2.Attacker.Value.Handle);
			CCSPlayerController cCSPlayerController = new CCSPlayerController(param.Handle);
			if (!cCSPlayerPawn.IsValid || !cCSPlayerController.IsValid)
			{
				return HookResult.Continue;
			}
			if (cCSPlayerPawn.TeamNum != cCSPlayerController.TeamNum || param.DesignerName != "player")
			{
				return HookResult.Continue;
			}
			string value = param2.Inflictor.Value?.DesignerName ?? "";
			if (Enumerable.Contains(AllowedDamageInflicter, value))
			{
				return HookResult.Continue;
			}
		}
		catch (Exception value2)
		{
			Console.WriteLine(value2);
			return HookResult.Continue;
		}
		Console.WriteLine("[RezTop] Заблокировал огонь по тимейтам");
		return HookResult.Handled;
	}
}
