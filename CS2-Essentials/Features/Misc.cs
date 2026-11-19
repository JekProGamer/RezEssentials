using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CSSharpUtils.Extensions;
using CSSharpUtils.Utils;
using hvhgg_essentials.Enums;

namespace hvhgg_essentials.Features;

public class Misc
{
    private readonly Plugin _plugin;
    private readonly Dictionary<uint, float> _lastRulePrint = new();

    public Misc(Plugin plugin)
    {
        _plugin = plugin;
    }

    [ConsoleCommand("settings", "Показать настройки сервера")]
    public void OnSettings(CCSPlayerController? player, CommandInfo inf)
    {
        AnnounceRules(player, true);
    }
    
    [ConsoleCommand("hvh_cfg_reload", "Перезагрузить конфигурацию без перезапуска сервера")]
    [RequiresPermissions("@css/generic")]
    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnReloadConfigCommand(CCSPlayerController? player, CommandInfo info)
    {
        _plugin.OnConfigParsed(new Cs2EssentialsConfig().Reload());
    }
    
    public void AnnounceRules(CCSPlayerController? player, bool force = false)
    {
        if (!player.IsPlayer())
            return;

        if (_lastRulePrint.TryGetValue(player!.Pawn.Index, out var lastPrintTime) &&
            lastPrintTime + 600 > Server.CurrentTime && !force)
            return;

        _lastRulePrint[player.Pawn.Index] = Server.CurrentTime;

        if (_plugin.Config.AllowSettingsPrint)
        {
            player.PrintToChat("Правила сервера:");
            player.PrintToChat(
                $"Дружественный огонь: {(_plugin.Config.UnmatchedFriendlyFire ? $"{ChatColors.Lime}включено" : $"{ChatColors.Red}отключено")}");
            player.PrintToChat(
                $"Телепорт / Airstuck: {(!_plugin.Config.RestrictTeleport ? $"{ChatColors.Lime}включено" : $"{ChatColors.Red}отключено")}");
            player.PrintToChat(
                $"Rapid Fire: {(_plugin.Config.RapidFireFixMethod == FixMethod.Allow ? $"{ChatColors.Lime}включено" : $"{ChatColors.Red}отключено")}");

            switch (_plugin.Config.RapidFireFixMethod)
            {
                case FixMethod.Allow:
                    break;
                case FixMethod.Ignore:
                    player.PrintToChat($"Метод: {ChatColors.Red}блокировка выстрела");
                    break;
                case FixMethod.PreventDamage:
                    player.PrintToChat($"Метод: {ChatColors.Red}блокировка урона");
                    break;
                case FixMethod.Reflect:
                    player.PrintToChat(
                        $"Метод: {ChatColors.Red}отражение урона{ChatColors.Default} с множителем {ChatColors.Orange}{_plugin.Config.RapidFireReflectScale}x{ChatColors.Default}");
                    break;
                case FixMethod.ReflectSafe:
                    player.PrintToChat(
                        $"Метод: {ChatColors.Red}отражение урона{ChatColors.Default} с множителем {ChatColors.Orange}{_plugin.Config.RapidFireReflectScale}x{ChatColors.Default} и предотвращение смерти");
                    break;
                default:
                    break;
            }

            player.PrintToChat(" ");
            player.PrintToChat("Ограничение оружия:");
            if (_plugin.Config.AllowedAwpCount != -1)
                player.PrintToChat(
                    $"AWP: {(_plugin.Config.AllowedAwpCount == 0 ? ChatColors.Red : ChatColors.Orange)}{_plugin.Config.AllowedAwpCount} на команду");
            if (_plugin.Config.AllowedScoutCount != -1)
                player.PrintToChat(
                    $"Scout: {(_plugin.Config.AllowedScoutCount == 0 ? ChatColors.Red : ChatColors.Orange)}{_plugin.Config.AllowedScoutCount} на команду");
            if (_plugin.Config.AllowedAutoSniperCount != -1)
                player.PrintToChat(
                    $"Auto: {(_plugin.Config.AllowedAutoSniperCount == 0 ? ChatColors.Red : ChatColors.Orange)}{_plugin.Config.AllowedAutoSniperCount} на команду");

            player.PrintToChat(" ");
            player.PrintToChat(
                $"{ChatUtils.FormatMessage(_plugin.Config.ChatPrefix)} Напиши {ChatColors.Red}!settings{ChatColors.Default}, чтобы снова увидеть настройки");
        }
        
        if (_plugin.Config.AllowAdPrint)
            player.PrintToChat(ChatUtils.FormatMessage("Сделано {MentFlashka-uid138}"));
        
    }
}