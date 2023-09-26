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
	
	#region Nodes
	
	public ItemList CardsListNode { get; private set; }
	public ProgressBar DownloadProgressNode { get; private set; }
	public Button DownloadCardsButtonNode { get; private set; }
	public HttpRequest DownloadCardsRequestNode { get; private set; }
	public HttpRequest BulkDataRequestNode { get; private set; }
	public CardViewWindow CardViewWindowNode { get; private set; }
	
	#endregion
	
	#region Signals
	
	[Signal]
	public delegate void CardAddedEventHandler(Wrapper<MTGCard> cardW, bool update);
	
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
	
	private string _cardsSrc;
	
	public override void _Ready()
	{
		#region Node fetching
		
		CardsListNode = GetNode<ItemList>("%CardsList");
		DownloadProgressNode = GetNode<ProgressBar>("%DownloadProgress");
		DownloadCardsButtonNode = GetNode<Button>("%DownloadCardsButton");
		DownloadCardsRequestNode = GetNode<HttpRequest>("%DownloadCardsRequest");
		BulkDataRequestNode = GetNode<HttpRequest>("%BulkDataRequest");
		CardViewWindowNode = GetNode<CardViewWindow>("%CardViewWindow");
		
		#endregion
		
		_cardsSrc = DownloadCardsRequestNode.DownloadFile;
		
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
			var cards = JsonSerializer.Deserialize<List<MTGCard>>(File.ReadAllText(_cardsSrc));
	//		SampleSizeNode.MaxValue = cards.Count;
			foreach (var card in cards) {
				CallDeferred("AddCard", new Wrapper<MTGCard>(card), update);
//				AddCard(card, update);
//				break;
			}
		});
	}
	
	public void AddCard(Wrapper<MTGCard> cardW, bool update) {
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
		var oracleCards = data["oracle_cards"];
		if (oracleCards is null) throw new Exception("Failed to find the oracle cards download link");

		DownloadProgressNode.MaxValue = oracleCards.Size;
		var downloadURI = oracleCards?.DownloadURI;
		DownloadCardsRequestNode.Request(downloadURI);
	}

	private void _on_download_cards_request_request_completed(long result, long response_code, string[] headers, byte[] body)
	{
		if (response_code != 200) {
			GUtil.Alert(this, "Failed to download cards! (Response code: " + response_code + ")");
			return;
		}
		
		Downloading = false;
		
		LoadCards(true);
	}

	private void _on_card_added(Wrapper<MTGCard> cardW, bool update)
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
		var index = CardsListNode.AddItem(cardW.Value.Name);
		CardsListNode.SetItemMetadata(index, cardW);
	}

	private void _on_cards_list_item_activated(int index)
	{
		var item = CardsListNode.GetItemMetadata(index).As<Wrapper<MTGCard>>();
		CardViewWindowNode.Load(item);
		CardViewWindowNode.Show();
	}
	
	#endregion

}






