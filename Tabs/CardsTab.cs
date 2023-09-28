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
	private readonly string CARDS_DATA_PATH = "Data";
	private readonly string CARDS_MANIFEST_FILE = "manifest.json";
	
	#region Nodes
	
	public CardsList CardsListNode { get; private set; }
	public ProgressBar DownloadProgressNode { get; private set; }
	public Button DownloadCardsButtonNode { get; private set; }
	public HttpRequest DownloadCardsRequestNode { get; private set; }
	public HttpRequest BulkDataRequestNode { get; private set; }
	public CardViewWindow CardViewWindowNode { get; private set; }
	
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
			DownloadCardsButtonNode.Disabled = value;
		}
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
			var path = Path.Combine(CARDS_DATA_PATH, CARDS_MANIFEST_FILE);
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

	private void _on_download_cards_request_request_completed(long result, long response_code, string[] headers, byte[] body)
	{
		if (response_code != 200) {
			GUtil.Alert(this, "Failed to download cards! (Response code: " + response_code + ")");
			return;
		}
		
		Downloading = false;

		// index cards
		var text = System.Text.Encoding.Default.GetString(body);
		var data = JsonSerializer.Deserialize<List<Card>>(text);
		var index = new Dictionary<string, List<Card>>();
		foreach (var item in data) {
			if (item.OracleId is null || item.OracleId == "") continue;
			if (!index.ContainsKey(item.OracleId))
				index.Add(item.OracleId, new());
			index[item.OracleId].Add(item);
		}

		var manifest = new List<ShortCard>();
		foreach (var pair in index) {
			// TODO save variations
			var card = new ShortCard();
			var original = pair.Value[0];
			card.Name = original.Name;
			card.OracleId = original.OracleId;
			// TODO remove
			card.ImageURIs = original.ImageURIs;
			card.Text = original.Text;
			// TODO remove
			card.Prices = original.Prices;
			card.Path = Path.Combine(CARDS_DATA_PATH, card.OracleId + ".json");
			// TODO save variations
			manifest.Add(card);
		}
		var mT = JsonSerializer.Serialize(manifest);
		File.WriteAllText(Path.Combine(CARDS_DATA_PATH, CARDS_MANIFEST_FILE), mT);
		
		LoadCards(true);
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


