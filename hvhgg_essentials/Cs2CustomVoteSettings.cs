using System.Text.Json.Serialization;

namespace hvhgg_essentials;

public class Cs2CustomVoteSettings
{
	[JsonPropertyName("FriendlyFireVote")]
	public bool FriendlyFireVote { get; set; }

	[JsonPropertyName("TeleportFixVote")]
	public bool TeleportFixVote { get; set; }

	[JsonPropertyName("RapidFireVote")]
	public string RapidFireVote { get; set; } = "full";

	[JsonPropertyName("Style")]
	public string Style { get; set; } = "center";
}
