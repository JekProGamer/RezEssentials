using System;
using System.Collections.Generic;
using System.Linq;
using CSSharpUtils.Extensions;
using CSSharpUtils.Utils;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Cvars.Validators;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace hvhgg_essentials.Features;

public class WeaponRestrict
{
	private readonly Plugin _plugin;

	private readonly Dictionary<string, Tuple<ItemDefinition, int>> _weaponPrices = new Dictionary<string, Tuple<ItemDefinition, int>>
	{
		{
			"weapon_ssg08",
			new Tuple<ItemDefinition, int>(ItemDefinition.SSG_08, 1700)
		},
		{
			"weapon_awp",
			new Tuple<ItemDefinition, int>(ItemDefinition.AWP, 4750)
		},
		{
			"weapon_scar20",
			new Tuple<ItemDefinition, int>(ItemDefinition.SCAR_20, 5000)
		},
		{
			"weapon_g3sg1",
			new Tuple<ItemDefinition, int>(ItemDefinition.G3SG1, 5000)
		}
	};

	private readonly Dictionary<uint, float> _lastWeaponRestrictPrint = new Dictionary<uint, float>();

	public static readonly FakeConVar<int> hvh_restrict_awp = new FakeConVar<int>("hvh_restrict_awp", "Ограничить awp до X для каждой команды", -1, ConVarFlags.FCVAR_REPLICATED, new RangeValidator<int>(-1, int.MaxValue));

	public static readonly FakeConVar<int> hvh_restrict_scout = new FakeConVar<int>("hvh_restrict_scout", "Ограничить scout до X для каждой команды", -1, ConVarFlags.FCVAR_REPLICATED, new RangeValidator<int>(-1, int.MaxValue));

	public static readonly FakeConVar<int> hvh_restrict_auto = new FakeConVar<int>("hvh_restrict_auto", "Ограничить autosnaiper до X для каждой команды", -1, ConVarFlags.FCVAR_REPLICATED, new RangeValidator<int>(-1, int.MaxValue));

	public static readonly FakeConVar<string> hvh_bypass_weapon_restrict_flag = new FakeConVar<string>("hvh_bypass_weapon_restrict_flag", "Флажок разрешения для обхода ограничений на оружие", "", ConVarFlags.FCVAR_NONE);

	public WeaponRestrict(Plugin plugin)
	{
		_plugin = plugin;
		_plugin.RegisterFakeConVars(this);
		hvh_restrict_awp.Value = _plugin.Config.AllowedAwpCount;
		hvh_restrict_scout.Value = _plugin.Config.AllowedScoutCount;
		hvh_restrict_auto.Value = _plugin.Config.AllowedAutoSniperCount;
		hvh_bypass_weapon_restrict_flag.Value = _plugin.Config.WeaponRestrictBypassFlags;
	}

	public HookResult OnWeaponCanAcquire(DynamicHook hook)
	{
		CCSPlayer_ItemServices param = hook.GetParam<CCSPlayer_ItemServices>(0);
		CEconItemView param2 = hook.GetParam<CEconItemView>(1);
		AcquireMethod param3 = hook.GetParam<AcquireMethod>(2);
		ushort itemDefIndex = param2.ItemDefinitionIndex;
		string key = _weaponPrices.FirstOrDefault((KeyValuePair<string, Tuple<ItemDefinition, int>> kv) => (uint)kv.Value.Item1 == itemDefIndex).Key;
		if (key == null)
		{
			return HookResult.Continue;
		}
		CCSPlayerController player = param.Pawn.Value.Controller.Value.As<CCSPlayerController>();
		if (!player.IsPlayer())
		{
			return HookResult.Continue;
		}
		if (hvh_bypass_weapon_restrict_flag.Value != "" && hvh_bypass_weapon_restrict_flag.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any((string flag) => AdminManager.PlayerHasPermissions(player, flag)))
		{
			return HookResult.Continue;
		}
		int weaponCountInTeam = GetWeaponCountInTeam(key, player.Team);
		int allowedWeaponCount = GetAllowedWeaponCount(key);
		if (allowedWeaponCount == -1 || weaponCountInTeam + 1 <= allowedWeaponCount)
		{
			return HookResult.Continue;
		}
		if (!_lastWeaponRestrictPrint.TryGetValue(player.Pawn.Index, out var value) || value + 10f <= Server.CurrentTime)
		{
			player.PrintToChat($"{ChatUtils.FormatMessage(_plugin.Config.ChatPrefix)} {ChatColors.Red}{key}{ChatColors.Default} ограничивается {ChatColors.Red}{allowedWeaponCount}{ChatColors.Default} на каждую команду!");
			_lastWeaponRestrictPrint[player.Pawn.Index] = Server.CurrentTime;
		}
		hook.SetReturn((param3 == AcquireMethod.PickUp) ? AcquireResult.InvalidItem : AcquireResult.AlreadyOwned);
		return HookResult.Stop;
	}

	private int GetAllowedWeaponCount(string item)
	{
		switch (item)
		{
		case "weapon_awp":
			return hvh_restrict_awp.Value;
		case "weapon_ssg08":
			return hvh_restrict_scout.Value;
		case "weapon_scar20":
		case "weapon_g3sg1":
			return hvh_restrict_auto.Value;
		default:
			return -1;
		}
	}

	private int GetWeaponCountInTeam(string item, CsTeam team)
	{
		return (from player in Utilities.GetPlayers().Where((CCSPlayerController pl) =>
			{
				if ((object)pl != null && pl.IsValid)
				{
					CHandle<CCSPlayerPawn> playerPawn = pl.PlayerPawn;
					if (playerPawn != null && playerPawn.IsValid)
					{
						CCSPlayerPawn value = playerPawn.Value;
						if ((object)value != null && value.IsValid)
						{
							return (uint)pl.TeamNum == (uint)team;
						}
					}
				}
				return false;
			}).ToList()
			select player.PlayerPawn.Value.WeaponServices.MyWeapons into playerWeapons
			select playerWeapons.Where((CHandle<CBasePlayerWeapon> weapon) => weapon.IsValid && weapon.Value.IsValid).Count((CHandle<CBasePlayerWeapon> weapon) => (uint)weapon.Value.AttributeManager.Item.ItemDefinitionIndex == (uint)_weaponPrices[item].Item1)).Sum();
	}
}
