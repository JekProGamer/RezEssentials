using System.Collections.Generic;
using CSSharpUtils.Extensions;
using CSSharpUtils.Utils;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using hvhgg_essentials.Enums;

namespace hvhgg_essentials.Features;

public class Misc
{
	private readonly Plugin _plugin;

	private readonly Dictionary<uint, float> _lastRulePrint = new Dictionary<uint, float>();

	public Misc(Plugin plugin)
	{
		_plugin = plugin;
	}

	[ConsoleCommand("settings", "Настройки печати")]
	public void OnSettings(CCSPlayerController? player, CommandInfo inf)
	{
		AnnounceRules(player, force: true);
	}

	[ConsoleCommand("hvh_cfg_reload", "Перезагрузите конфигурацию в текущем сеансе без перезапуска сервера")]
	[RequiresPermissions(new string[] { "@css/generic" })]
	[CommandHelper(0, "", CommandUsage.CLIENT_AND_SERVER)]
	public void OnReloadConfigCommand(CCSPlayerController? player, CommandInfo info)
	{
		_plugin.OnConfigParsed(new Cs2EssentialsConfig().Reload());
	}

	public void AnnounceRules(CCSPlayerController? player, bool force = false)
	{
		if (!player.IsPlayer() || (_lastRulePrint.TryGetValue(player.Pawn.Index, out var value) && value + 600f > Server.CurrentTime && !force))
		{
			return;
		}
		_lastRulePrint[player.Pawn.Index] = Server.CurrentTime;
		if (_plugin.Config.AllowSettingsPrint)
		{
			player.PrintToChat("Ruleset:");
			player.PrintToChat($"unmatched.{ChatColors.Orange}gg{ChatColors.Default} дружественный огонь: {(_plugin.Config.UnmatchedFriendlyFire ? $"{ChatColors.Lime}enabled" : $"{ChatColors.Red}disabled")}");
			player.PrintToChat("Teleport/Airstuck: " + ((!_plugin.Config.RestrictTeleport) ? $"{ChatColors.Lime}enabled" : $"{ChatColors.Red}disabled"));
			player.PrintToChat("Rapid fire: " + ((_plugin.Config.RapidFireFixMethod == FixMethod.Allow) ? $"{ChatColors.Lime}enabled" : $"{ChatColors.Red}disabled"));
			switch (_plugin.Config.RapidFireFixMethod)
			{
			case FixMethod.Ignore:
				player.PrintToChat($"Method: {ChatColors.Red}Блок выстрела");
				break;
			case FixMethod.PreventDamage:
				player.PrintToChat($"Method: {ChatColors.Red}Блок Урона");
				break;
			case FixMethod.Reflect:
				player.PrintToChat($"Method: {ChatColors.Red}отражать повреждения{ChatColors.Default} в {ChatColors.Orange}{_plugin.Config.RapidFireReflectScale}x{ChatColors.Default}");
				break;
			case FixMethod.ReflectSafe:
				player.PrintToChat($"Method: {ChatColors.Red}отражать повреждения{ChatColors.Default} в {ChatColors.Orange}{_plugin.Config.RapidFireReflectScale}x{ChatColors.Default} и смерть");
				break;
			}
			player.PrintToChat(" ");
			player.PrintToChat("Weapon restriction:");
			if (_plugin.Config.AllowedAwpCount != -1)
			{
				player.PrintToChat($"AWP: {((_plugin.Config.AllowedAwpCount == 0) ? ChatColors.Red : ChatColors.Orange)}{_plugin.Config.AllowedAwpCount} на каждую команду");
			}
			if (_plugin.Config.AllowedScoutCount != -1)
			{
				player.PrintToChat($"Scout: {((_plugin.Config.AllowedScoutCount == 0) ? ChatColors.Red : ChatColors.Orange)}{_plugin.Config.AllowedScoutCount} на каждую команду");
			}
			if (_plugin.Config.AllowedAutoSniperCount != -1)
			{
				player.PrintToChat($"Auto: {((_plugin.Config.AllowedAutoSniperCount == 0) ? ChatColors.Red : ChatColors.Orange)}{_plugin.Config.AllowedAutoSniperCount} на каждую команду");
			}
			player.PrintToChat(" ");
			player.PrintToChat($"{ChatUtils.FormatMessage(_plugin.Config.ChatPrefix)} Тип {ChatColors.Red}!settings{ChatColors.Default} чтобы снова просмотреть эти настройки,");
		}
		if (_plugin.Config.AllowAdPrint)
		{
			player.PrintToChat(ChatUtils.FormatMessage("Сделано {MentFlashka-uid138}"));
		}
	}
}
