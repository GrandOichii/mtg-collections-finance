using Godot;
using System;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

class BulkDataJson {
	[JsonPropertyName("data")]
	public List<BulkDataEntryJson> Data { get; set; } = new();

	public static BulkDataJson FromJSON(string json) {
		var result = JsonSerializer.Deserialize<BulkDataJson>(json) ?? throw new Exception("Failed to deserialize BulkDataJson: " + json);
		return result;
	}

	public BulkDataEntryJson? this[string name] {
		get {
			foreach (var entry in Data)
				if (entry.Type == name) return entry;
			return null;
		}
	}
}

class BulkDataEntryJson {
	[JsonPropertyName("type")]
	public string Type { get; set; } = "";
	[JsonPropertyName("download_uri")]
	public string DownloadURI { get; set; } = "";
	[JsonPropertyName("size")]
	public int Size { get; set; }
}

public partial class CardsTab : TabBar
{
	private readonly string BULK_DATA_INFO_URL = "https://api.scryfall.com/bulk-data";
	private readonly string DATA_PATH = "Data";
	private readonly string CARDS_DATA_PATH = "Cards";
	private readonly string CARDS_MANIFEST_FILE = "manifest.json";
	
	#region Nodes
	
	public CardsList CardsListNode { get; private set; }
	public ProgressBar DownloadProgressNode { get; private set; }
	public Button DownloadCardsButtonNode { get; private set; }
	public HttpRequest DownloadCardsRequestNode { get; private set; }
	public HttpRequest BulkDataRequestNode { get; private set; }
	public CardViewWindow CardViewWindowNode { get; private set; }
	public ProgressBar SaveVariationsProgressNode { get; private set; }
	
	#endregion
	
	#region Signals
	
	[Signal]
	public delegate void CardAddedEventHandler(Wrapper<ShortCard> cardW, bool update);
	
	#endregion
	
	private bool _downloading = false;
	public bool Downloading {
		get => _downloading;
		set {
			_downloading = value;
			DownloadProgressNode.Visible = value;
			SaveVariationsProgressNode.Visible = value;
			DownloadCardsButtonNode.Disabled = value;
		}
	}
	private void SetDownloading(bool v) {
		Downloading = v;
	}
	
	private void IncrementSaveVariationsProgressNode() {
		SaveVariationsProgressNode.Value += 1;
	}
		
	public override void _Ready()
	{
		#region Node fetching
		
		CardsListNode = GetNode<CardsList>("%CardsList");
		DownloadProgressNode = GetNode<ProgressBar>("%DownloadProgress");
		DownloadCardsButtonNode = GetNode<Button>("%DownloadCardsButton");
		DownloadCardsRequestNode = GetNode<HttpRequest>("%DownloadCardsRequest");
		BulkDataRequestNode = GetNode<HttpRequest>("%BulkDataRequest");
		CardViewWindowNode = GetNode<CardViewWindow>("%CardViewWindow");
		SaveVariationsProgressNode = GetNode<ProgressBar>("%SaveVariationsProgress");
		
		#endregion
				
		// load cards
		LoadCards(false);
	}
	
	public override void _Process(double delta) {
		if (Downloading) {
			DownloadProgressNode.Value = DownloadCardsRequestNode.GetDownloadedBytes() * 8;
		}
	}
	
	public void LoadCards(bool update) {
		Task.Run(() => {
			var path = Path.Combine(DATA_PATH, CARDS_DATA_PATH, CARDS_MANIFEST_FILE);
			if (!File.Exists(path)) return;

			var cards = ShortCard.LoadManifest(path);
			// var cards = JsonSerializer.Deserialize<List<ShortCard>>(File.ReadAllText(_cardsSrc));
	//		SampleSizeNode.MaxValue = cards.Count;
			foreach (var card in cards) {
				CallDeferred("AddCard", new Wrapper<ShortCard>(card), update);
//				AddCard(card, update);
//				break;
			}
		});
	}
	
	public void AddCard(Wrapper<ShortCard> cardW, bool update) {
		EmitSignal(SignalName.CardAdded, cardW, update);
	}

	private void SaveVariations(Dictionary<string, List<DownloadedCard>> index) {
		var manifest = new List<ShortCard>();
		
		if (Directory.Exists(DATA_PATH))
			Directory.Delete(DATA_PATH, true);
		Directory.CreateDirectory(DATA_PATH);
		// for Godot
		File.Create(Path.Combine(DATA_PATH, ".gdignore"));
		Directory.CreateDirectory(Path.Combine(DATA_PATH, CARDS_DATA_PATH));
		
		foreach (var pair in index) {
			// TODO save variations
			var card = new ShortCard();
			var original = pair.Value[0];
			card.Name = original.Name;
			card.OracleId = original.OracleId;
			card.ColorIdentity = original.ColorIdentity;
			card.Layout = original.Layout;
			card.Text = original.Text;
			card.TypeLine = original.TypeLine;
			card.Path = Path.Combine(DATA_PATH, CARDS_DATA_PATH, card.OracleId + ".json");
			
			var lT = JsonSerializer.Serialize(pair.Value);
			File.WriteAllText(card.Path, lT);
			// TODO create a task list and execute them all at once

			CallDeferred("IncrementSaveVariationsProgressNode");
			manifest.Add(card);
		}
		var mT = JsonSerializer.Serialize(manifest);
		File.WriteAllText(Path.Combine(DATA_PATH, CARDS_DATA_PATH, CARDS_MANIFEST_FILE), mT);
		
		CallDeferred("SetDownloading", false);
		CallDeferred("LoadCards", true);
	}
	

	#region Signal connections
	
	private void _on_download_cards_button_pressed()
	{
		Downloading = true;
		BulkDataRequestNode.Request(BULK_DATA_INFO_URL);
	}
	
	private void _on_bulk_data_request_request_completed(long result, long response_code, string[] headers, byte[] body)
	{
		if (response_code != 200) {
			GUtil.Alert(this, "Failed to fetch bulk data! (Response code: " + response_code + ")");
			return;
		}
		
		var text = System.Text.Encoding.Default.GetString(body);
		var data = BulkDataJson.FromJSON(text);
		var defaultCards = data["default_cards"];
		if (defaultCards is null) throw new Exception("Failed to find the default cards download link");

		DownloadProgressNode.MaxValue = defaultCards.Size;
		var downloadURI = defaultCards?.DownloadURI;
		DownloadCardsRequestNode.Request(downloadURI);
	}

	private async void _on_download_cards_request_request_completed(long result, long response_code, string[] headers, byte[] body)
	{
		if (response_code != 200) {
			GUtil.Alert(this, "Failed to download cards! (Response code: " + response_code + ")");
			return;
		}
		
		// index cards
		var text = System.Text.Encoding.Default.GetString(body);
		var data = JsonSerializer.Deserialize<List<DownloadedCard>>(text);
		var index = new Dictionary<string, List<DownloadedCard>>();
		foreach (var item in data) {
			if (item.OracleId is null || item.OracleId == "") continue;
			if (!index.ContainsKey(item.OracleId))
				index.Add(item.OracleId, new());
			index[item.OracleId].Add(item);
		}
		
		SaveVariationsProgressNode.Value = 0;
		SaveVariationsProgressNode.MaxValue = index.Count;
		Task.Run(() => SaveVariations(index));
	}

	private void _on_card_added(Wrapper<ShortCard> cardW, bool update)
	{
		
		// check if card already exists
		// TODO too slow
//		if (update) {
//			for (int i = 0; i < CardsListNode.ItemCount; i++) {
//				if (CardsListNode.GetItemText(i) == cardW.Value.Name) {
//					CardsListNode.SetItemMetadata(i, cardW);
//					return;
//				}
//			}
//		}
		
		// add card
		CardsListNode.AddItem(cardW);
	}
	
	private void _on_cards_list_card_activated(Wrapper<ShortCard> cardW)
	{
		CardViewWindowNode.Load(cardW);
		CardViewWindowNode.Show();
	}
	
	#endregion

}


