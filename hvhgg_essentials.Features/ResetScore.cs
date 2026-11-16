using CSSharpUtils.Extensions;
using CSSharpUtils.Utils;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace hvhgg_essentials.Features;

public class ResetScore
{
	private readonly Plugin _plugin;

	public static readonly FakeConVar<bool> hvh_resetscore = new FakeConVar<bool>("hvh_resetscore", "Enables the reset score feature", true, ConVarFlags.FCVAR_REPLICATED);

	public ResetScore(Plugin plugin)
	{
		_plugin = plugin;
		_plugin.RegisterFakeConVars(this);
		hvh_resetscore.Value = _plugin.Config.AllowResetScore;
	}

	[ConsoleCommand("css_rs", "Reset score")]
	[ConsoleCommand("css_resetscore", "Reset score")]
	public void OnResetScore(CCSPlayerController? player, CommandInfo inf)
	{
		if (hvh_resetscore.Value && player.IsPlayer())
		{
			CSMatchStats_t matchStats = player.ActionTrackingServices.MatchStats;
			if ((object)player != null && player.Score == 0 && player.MVPs == 0 && matchStats != null && matchStats.Kills == 0 && matchStats.HeadShotKills == 0 && matchStats.Deaths == 0 && matchStats.Assists == 0 && matchStats.UtilityDamage == 0 && matchStats.Damage == 0 && matchStats.Objective == 0)
			{
				player.PrintToChat(ChatUtils.FormatMessage(_plugin.Config.ChatPrefix) + " Твой Счет обнулен на 0.");
				return;
			}
			matchStats.Kills = 0;
			matchStats.HeadShotKills = 0;
			matchStats.Deaths = 0;
			matchStats.Assists = 0;
			matchStats.UtilityDamage = 0;
			matchStats.Damage = 0;
			matchStats.Objective = 0;
			player.MVPs = 0;
			player.Score = 0;
			Utilities.SetStateChanged(player, "CCSPlayerController", "m_pActionTrackingServices");
			Utilities.SetStateChanged(player, "CCSPlayerController", "m_iMVPs");
			Utilities.SetStateChanged(player, "CCSPlayerController", "m_iScore");
			Server.PrintToChatAll($"{ChatUtils.FormatMessage(_plugin.Config.ChatPrefix)} Игрок {ChatColors.Red}{player.PlayerName}{ChatColors.Default} Обновил Статус!");
		}
	}
}
