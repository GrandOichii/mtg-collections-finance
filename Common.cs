using Godot;
using System;
using System.IO;
using System.Collections.Generic;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

public class ShortCard {
	[JsonPropertyName("name")]
	public string Name { get; set; }
	[JsonPropertyName("path")]
	public string Path { get; set; }

	public static Dictionary<string, ShortCard> LoadManifest(string path) {
		var text = File.ReadAllText(path);
		return JsonSerializer.Deserialize<Dictionary<string, ShortCard>>(text) ?? throw new Exception("Failed to deserialize manifest file: " + path);
	}

	public List<Card> GetVariations() {
		var text = File.ReadAllText(Path);
		return JsonSerializer.Deserialize<List<Card>>(text);
	}
}

public class Card {
	[JsonPropertyName("name")]
	public string Name { get; set; }
	[JsonPropertyName("id")]
	public string ID { get; set; }
	[JsonPropertyName("oracle_id")]
	public string OracleID { get; set; }
	[JsonPropertyName("image_uris")]
	public Dictionary<string, string> ImageURIs { get; set; }
	[JsonPropertyName("oracle_text")]
	public string Text { get; set; }
	[JsonPropertyName("prices")]
	public Dictionary<string, string?> Prices { get; set; }
}


public class MTGCard {
	[JsonPropertyName("name")]
	public string Name { get; set; }
	[JsonPropertyName("oracle_id")]
	public string OracleId { get; set; }

	[JsonPropertyName("image_uris")]
	public Dictionary<string, string> ImageURIs { get; set; }
	[JsonPropertyName("oracle_text")]
	public string Text { get; set; }
	[JsonPropertyName("prices")]
	public Dictionary<string, string?> Prices { get; set; } 
}

public class CCard {
	[JsonPropertyName("oracle_id")]
	public string OracleId { get; set; }
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
	public abstract CCard? Do(string line, Dictionary<string, MTGCard> index);
}

public class SimpleLineParser : CardLineParser {
	public override CCard? Do(string line, Dictionary<string, MTGCard> index)
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

	public override CCard Do(string line, Dictionary<string, MTGCard> index)
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

public class DeckCardLineParser : CardLineParser {
	private readonly Regex PATTERN = new("(\\d+) (.+)");

	public override CCard Do(string line, Dictionary<string, MTGCard> index)
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
