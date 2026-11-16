using System;
using System.Collections.Generic;
using CS2_CustomVotes.Shared.Models;
using CSSharpUtils.Extensions;
using CSSharpUtils.Utils;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Cvars.Validators;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using hvhgg_essentials.Enums;

namespace hvhgg_essentials.Features;

public class RapidFire
{
	private readonly Dictionary<uint, int> _lastPlayerShotTick = new Dictionary<uint, int>();

	private readonly HashSet<uint> _rapidFireBlockUserIds = new HashSet<uint>();


	private readonly Plugin _plugin;

	public static readonly FakeConVar<int> hvh_restrict_rapidfire = new FakeConVar<int>("hvh_restrict_rapidfire", "Restrict rapid fire", 0, ConVarFlags.FCVAR_REPLICATED, new RangeValidator<int>(0, 4));

	public static readonly FakeConVar<float> hvh_rapidfire_reflect_scale = new FakeConVar<float>("hvh_rapidfire_reflect_scale", "Reflect scale", 1f, ConVarFlags.FCVAR_REPLICATED, new RangeValidator<float>(0f, 1f));

	public RapidFire(Plugin plugin)
	{
		_plugin = plugin;
		_plugin.RegisterFakeConVars(this);
		hvh_restrict_rapidfire.Value = (int)_plugin.Config.RapidFireFixMethod;
		hvh_rapidfire_reflect_scale.Value = _plugin.Config.RapidFireReflectScale;
	}

	public static void RegisterCustomVotes(Plugin plugin)
	{
		string defaultOption = plugin.Config.RapidFireFixMethod switch
		{
			FixMethod.Allow => "Allow", 
			FixMethod.Ignore => "Block", 
			FixMethod.Reflect => "Reflect", 
			FixMethod.ReflectSafe => "Reflect (safe)", 
			FixMethod.PreventDamage => "Prevent damage", 
			_ => "Allow", 
		};
		Dictionary<string, VoteOption> dictionary = new Dictionary<string, VoteOption>();
		if (plugin.Config.CustomVoteSettings.RapidFireVote != "off")
		{
			dictionary.Add("Allow", new VoteOption("{Green}Allow", new List<string> { "hvh_restrict_rapidfire 0" }));
			dictionary.Add("Block", new VoteOption("{Red}Block", new List<string> { "hvh_restrict_rapidfire 1" }));
			if (plugin.Config.CustomVoteSettings.RapidFireVote == "full")
			{
				dictionary.Add("Reflect", new VoteOption("{Orange}Reflect", new List<string> { "hvh_restrict_rapidfire 2" }));
				dictionary.Add("Reflect (safe)", new VoteOption("{Orange}Reflect (safe)", new List<string> { "hvh_restrict_rapidfire 3" }));
				dictionary.Add("Prevent damage", new VoteOption("{Red}Prevent damage", new List<string> { "hvh_restrict_rapidfire 4" }));
			}
		}
		Plugin.CustomVotesApi.Get()?.AddCustomVote("rapidfire", new List<string> { "rf" }, "Rapid fire", defaultOption, 30f, dictionary, plugin.Config.CustomVoteSettings.Style);
	}

	public static void UnregisterCustomVotes(Plugin plugin)
	{
		Plugin.CustomVotesApi.Get()?.RemoveCustomVote("rapidfire");
	}

	public HookResult OnWeaponFire(EventWeaponFire eventWeaponFire, GameEventInfo info)
	{
		if (!eventWeaponFire.Userid.IsPlayer())
		{
			return HookResult.Continue;
		}
		CBasePlayerWeapon cBasePlayerWeapon = eventWeaponFire.Userid.Pawn.Value?.WeaponServices?.ActiveWeapon.Value;
		if (cBasePlayerWeapon?.DesignerName == "weapon_healthshot")
		{
			return HookResult.Continue;
		}
		if (hvh_restrict_rapidfire.Value == 1)
		{
			DelayNextAttack(eventWeaponFire.Userid);
		}
		CCSWeaponBaseVData cCSWeaponBaseVData = cBasePlayerWeapon?.GetVData<CCSWeaponBaseVData>();
		uint index = eventWeaponFire.Userid.Pawn.Index;
		if (!_lastPlayerShotTick.TryGetValue(index, out var value))
		{
			_lastPlayerShotTick[index] = Server.TickCount;
			return HookResult.Continue;
		}
		_lastPlayerShotTick[index] = Server.TickCount;
		int num = Server.TickCount - value;
		float num2 = ((cCSWeaponBaseVData != null) ? (cCSWeaponBaseVData.CycleTime.Values[0] * 64f) : 0f) - 1f;
		if ((float)num > num2 || cBasePlayerWeapon?.DesignerName == "weapon_revolver")
		{
			return HookResult.Continue;
		}
		if (hvh_restrict_rapidfire.Value == 0)
		{
			return HookResult.Continue;
		}
		Console.WriteLine("[RezTop.cc] Обнаружен rapid fire у " + eventWeaponFire.Userid.PlayerName);
		if (_rapidFireBlockUserIds.Count == 0)
		{
			Server.NextFrame(_rapidFireBlockUserIds.Clear);
		}
		_rapidFireBlockUserIds.Add(index);
		return HookResult.Continue;
	}

	public HookResult OnTakeDamage(DynamicHook h)
	{
		if (hvh_restrict_rapidfire.Value == 1)
		{
			return HookResult.Continue;
		}
		CTakeDamageInfo param = h.GetParam<CTakeDamageInfo>(1);
		if (param.Attacker.Value == null)
		{
			return HookResult.Continue;
		}
		if (!_rapidFireBlockUserIds.Contains(param.Attacker.Index))
		{
			return HookResult.Continue;
		}
		switch (hvh_restrict_rapidfire.Value)
		{
		case 4:
			param.Damage = 0f;
			break;
		case 2:
		case 3:
			param.Damage *= hvh_rapidfire_reflect_scale.Value;
			h.SetParam(0, (CEntityInstance)param.Attacker.Value);
			if (hvh_restrict_rapidfire.Value == 3)
			{
				param.DamageFlags |= TakeDamageFlags_t.DFLAG_PREVENT_DEATH;
			}
			break;
		}
		return HookResult.Changed;
	}

	public HookResult OnBulletImpact(EventBulletImpact eventBulletImpact, GameEventInfo info)
	{
		if (hvh_restrict_rapidfire.Value != 1)
		{
			return HookResult.Continue;
		}
		if (!eventBulletImpact.Userid.IsPlayer())
		{
			return HookResult.Continue;
		}
		DelayNextAttack(eventBulletImpact.Userid);
		return HookResult.Continue;
	}

	private static void DelayNextAttack(CCSPlayerController player)
	{
		CBasePlayerWeapon cBasePlayerWeapon = player.Pawn.Value?.WeaponServices?.ActiveWeapon.Value;
		if (cBasePlayerWeapon == null || cBasePlayerWeapon.DesignerName == "weapon_revolver" || cBasePlayerWeapon.DesignerName == "weapon_healthshot")
		{
			return;
		}
		CCSWeaponBaseVData vData = cBasePlayerWeapon.GetVData<CCSWeaponBaseVData>();
		if (vData != null)
		{
			int num = (int)Math.Round(vData.CycleTime.Values[0] * 64f) - 3;
			int num2 = (int)player.TickBase + num;
			if (cBasePlayerWeapon.NextPrimaryAttackTick < num2)
			{
				cBasePlayerWeapon.NextPrimaryAttackTick = num2;
				Utilities.SetStateChanged(cBasePlayerWeapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
			}
		}
	}
}
