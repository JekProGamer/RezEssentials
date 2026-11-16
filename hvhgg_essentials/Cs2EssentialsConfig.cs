using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using hvhgg_essentials.Enums;

namespace hvhgg_essentials;

public class Cs2EssentialsConfig : BasePluginConfig
{
	[JsonPropertyName("PrintWarnings")]
	public bool PrintWarnings { get; set; } = true;

	[JsonPropertyName("RapidFireFixMethod")]
	public FixMethod RapidFireFixMethod { get; set; } = FixMethod.Ignore;

	[JsonPropertyName("RapidFireReflectScale")]
	public float RapidFireReflectScale { get; set; } = 1f;

	[JsonPropertyName("AllowedAwpCount")]
	public int AllowedAwpCount { get; set; } = -1;

	[JsonPropertyName("AllowedScoutCount")]
	public int AllowedScoutCount { get; set; } = -1;

	[JsonPropertyName("AllowedAutoSniperCount")]
	public int AllowedAutoSniperCount { get; set; } = -1;

	[JsonPropertyName("WeaponRestrictBypassFlags")]
	public string WeaponRestrictBypassFlags { get; set; } = "";

	[JsonPropertyName("UnmatchedFriendlyFire")]
	public bool UnmatchedFriendlyFire { get; set; } = true;

	[JsonPropertyName("RestrictTeleport")]
	public bool RestrictTeleport { get; set; } = true;

	[JsonPropertyName("AllowAdPrint")]
	public bool AllowAdPrint { get; set; } = true;

	[JsonPropertyName("AllowSettingsPrint")]
	public bool AllowSettingsPrint { get; set; } = true;

	[JsonPropertyName("AllowResetScore")]
	public bool AllowResetScore { get; set; } = true;

	[JsonPropertyName("AllowRageQuit")]
	public bool AllowRageQuit { get; set; } = true;

	[JsonPropertyName("ChatPrefix")]
	public string ChatPrefix { get; set; } = "[{Gold}Rez{DarkRed}Top{Default}.cc]";[JsonPropertyName("ConfigVersion")]
	public override int Version { get; set; } = 5;
}
