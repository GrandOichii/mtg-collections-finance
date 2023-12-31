using Godot;
using System;
using System.IO;
using System.Collections.Generic;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

public class DownloadedCard : Card {
	[JsonPropertyName("name")]
	public string Name { get; set; }
	[JsonPropertyName("oracle_id")]
	public string OracleId { get; set; }
	[JsonPropertyName("type_line")]
	public string TypeLine { get; set; }
	[JsonPropertyName("oracle_text")]
	public string Text { get; set; }
	[JsonPropertyName("color_identity")]
	public List<string> ColorIdentity { get; set; }
}

public class ShortCard {
	public static readonly List<string> TypePriority = new() {"Planeswalker", "Creature", "Artifact", "Sorcery", "Instant", "Enchantment", "Land"};
	[JsonPropertyName("name")]
	public string Name { get; set; }
	[JsonPropertyName("path")]
	public string Path { get; set; }
	[JsonPropertyName("type_line")]
	public string TypeLine { get; set; }
	#region Legacy MTGCard info
	[JsonPropertyName("oracle_id")]
	public string OracleId { get; set; }
	[JsonPropertyName("color_identity")]
	public List<string> ColorIdentity { get; set; }
	[JsonPropertyName("layout")]
	public string Layout { get; set; }

	[JsonPropertyName("oracle_text")]
	public string Text { get; set; }
	#endregion

	public static List<ShortCard> LoadManifest(string path) {
		var text = File.ReadAllText(path);
		return JsonSerializer.Deserialize<List<ShortCard>>(text) ?? throw new Exception("Failed to deserialize manifest file: " + path);
	}

	public List<Card> GetVariations() {
		var text = File.ReadAllText(Path);
		return JsonSerializer.Deserialize<List<Card>>(text);
	}

	public bool CanBeCommander() {
		
		return Layout != "token" && TypeLine.Contains("Creature") && TypeLine.Contains("Legendary");
	}

	public string URLFriendlyName() {
		return Name
			.Replace(",", "")
			.Replace("\'", "")
			.Replace(" ", "-")
			.ToLower();
	}

	public string GetPrimaryType() {
		foreach (var type in TypePriority)
			if (TypeLine.Contains(type))
				return type;
		return "";
	}
}

public class Card {
	[JsonPropertyName("id")]
	public string ID { get; set; }
	[JsonPropertyName("image_uris")]
	public Dictionary<string, string> ImageURIs { get; set; }
	[JsonPropertyName("prices")]
	public Dictionary<string, string?> Prices { get; set; }
	[JsonPropertyName("set")]
	public string Set { get; set; }
	[JsonPropertyName("set_name")]
	public string SetName { get; set; }
	[JsonPropertyName("collector_number")]
	public string CollectorNumber { get; set; }
	[JsonPropertyName("layout")]
	public string Layout { get; set; }
	[JsonPropertyName("card_faces")]
	public List<CardFace>? Faces { get; set; }

	public string UID => SetName + " (" + Set.ToUpper() + "#" + CollectorNumber + ")";
}

public class CardFace {
	[JsonPropertyName("name")]
	public string Name { get; set; }
	[JsonPropertyName("type_line")]
	public string TypeLine { get; set; }
	[JsonPropertyName("oracle_text")]
	public string Text { get; set; }
	[JsonPropertyName("image_uris")]
	public Dictionary<string, string> ImageURIs { get; set; }
}


// public class MTGCard {
// 	[JsonPropertyName("name")]
// 	public string Name { get; set; }
// 	[JsonPropertyName("oracle_id")]
// 	public string OracleId { get; set; }

// 	[JsonPropertyName("image_uris")]
// 	public Dictionary<string, string> ImageURIs { get; set; }
// 	[JsonPropertyName("oracle_text")]
// 	public string Text { get; set; }
// 	[JsonPropertyName("prices")]
// 	public Dictionary<string, string?> Prices { get; set; } 
// }

public class CCard {
	[JsonPropertyName("oracle_id")]
	public string OracleId { get; set; }
	[JsonPropertyName("id")]
	public string Printing { get; set; } = "";
	[JsonPropertyName("amount")]
	public int Amount { get; set; }
}

public class Collection {
	[JsonPropertyName("name")]
	public string Name { get; set; }
	[JsonPropertyName("cards")]
	public List<CCard> Cards { get; set; }

	public static Collection FromJson(string text) {
		return JsonSerializer.Deserialize<Collection>(text) ?? throw new Exception("Failed to deserialize collection: " + text); 
	}
	public string ToJson() {
		return JsonSerializer.Serialize(this);
	}
}

public partial class Wrapper<T> : Node
{
	public T Value { get; set; }
	public Wrapper(T v) { Value = v; }
}

public static class GUtil
{
	public static void Alert(Node parent, string message)
	{
		// TODO use own dialog box
		OS.Alert(message, "MTG collections finance");
	}

	public static bool CheckNameTaken(string newName, string oldName, List<string> names) {
		if (newName == oldName) return true;
		foreach (var name in names) {
			if (name != newName) continue;

			return false;
		}
		return true;
	}
}


public abstract class CardLineParser {
	public abstract CCard? Do(string line, Dictionary<string, ShortCard> index);
}

public class SimpleLineParser : CardLineParser {
	public override CCard? Do(string line, Dictionary<string, ShortCard> index)
	{
		if (!index.ContainsKey(line))
			return null;
		var result = new CCard();
		result.OracleId = index[line].OracleId;
		result.Amount = 1;
		return result;
	}
}

public class XmageLineParser : CardLineParser {
	private readonly Regex PATTERN = new("(SB: )?(\\d+) \\[(.+)\\] (.+)");

	public override CCard Do(string line, Dictionary<string, ShortCard> index)
	{
		var match = PATTERN.Match(line);
		if (match.Groups[0].Length == 0) return null;
		var name = match.Groups[4].ToString();
		if (!index.ContainsKey(name))
			return null;
		var result = new CCard();
		result.OracleId = index[name].OracleId;
		result.Amount = int.Parse(match.Groups[2].ToString());
		// TODO add sideboard and printing
		return result;
	}
}

public class ArenaLineParser : CardLineParser {
	private readonly Regex PATTERN = new ("(\\d+) (.+) \\((.+\\)) (\\d+)");

	public override CCard Do(string line, Dictionary<string, ShortCard> index)
	{
		var match = PATTERN.Match(line);
		if (match.Groups[0].Length == 0) return null;
		var name = match.Groups[2].ToString();
		if (!index.ContainsKey(name))
			return null;
		var result = new CCard();
		result.OracleId = index[name].OracleId;
		result.Amount = int.Parse(match.Groups[1].ToString());
		// TODO add sideboard and printing
		return result;
	}
}

public class DeckCardLineParser : CardLineParser {
	private readonly Regex PATTERN = new("(\\d+) (.+)");

	public override CCard Do(string line, Dictionary<string, ShortCard> index)
	{
		var match = PATTERN.Match(line);
		if (match.Groups[0].Length == 0) return null;
		var name = match.Groups[2].ToString();
		if (!index.ContainsKey(name))
			return null;
		var result = new CCard();
		result.OracleId = index[name].OracleId;
		result.Amount = int.Parse(match.Groups[1].ToString());
		// TODO add sideboard and printing
		return result;
	}
}

public static class PriceUtil {
	private static Dictionary<string, Dictionary<float, string>> _priceIndex = new() {
		{"*", new() {
			{5f, "red"},
			{3f, "orange"},
			{0f, "green"}
		}}
	};

	private static Dictionary<float, string>? GetPriceIndexOf(string key) {
		if (_priceIndex.ContainsKey(key))
			return _priceIndex[key];
		if (_priceIndex.ContainsKey("*"))
			return _priceIndex["*"];
		return null;
	}

	public static string GetColoredText(double price, string priceType) {
		var result = price.ToString();
		var priceIndex = GetPriceIndexOf(priceType);
		if (priceIndex is not null) {
			foreach (var pPair in priceIndex) {
				if (price >= pPair.Key) {
					result = "[color=" + pPair.Value + "]" + result + "[/color]";
					break;
				}
			}
		}
		return result;
	}
}


public class EDHData {
	[JsonPropertyName("cardlist")]
	public List<EDHDataCard> Cards { get; set; }
}

public class EDHDataCard {
	[JsonPropertyName("name")]
	public string Name { get; set; }
	[JsonPropertyName("synergy")]
	public float Synergy { get; set; }
}


public abstract class CollectionExportProfile {
	public virtual string Name { get; }
	public abstract string Do(Collection c, Dictionary<string, ShortCard> oidIndex);
}

public class TextFileExportProfile : CollectionExportProfile {
    public override string Name => "Text file";
	public override string Do(Collection c, Dictionary<string, ShortCard> oidIndex)
    {
		var result = "";
		for (int i = 0; i < c.Cards.Count; i++) {
			var card = c.Cards[i];
			if (card.Amount == 0) continue;
			result += card.Amount + " " + oidIndex[card.OracleId].Name;
			if (i == c.Cards.Count - 1) continue;
			result += "\n";
		}
		return result;
    }
}