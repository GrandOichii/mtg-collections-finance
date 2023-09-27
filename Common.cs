using Godot;
using System;
using System.Collections.Generic;

using System.Text.Json;
using System.Text.Json.Serialization;

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
