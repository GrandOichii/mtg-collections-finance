using Godot;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

public partial class EDHTab : TabBar
{
	static readonly PackedScene EDHCardPS = ResourceLoader.Load("res://EDHCard.tscn") as PackedScene;

	private readonly string EDH_DATA_DIR = "EDH";
	private readonly string EDHREC_URL = "https://json.edhrec.com/pages/commanders/{0}.json";
	
	#region Nodes
	
	public CardsList CardsListNode { get; private set; }
	public HttpRequest DownloadRequestNode { get; private set; }
//	public ProgressBar DownloadProgressNode { get; private set; }
//	public Button DownloadEDHRECDataButtonNode { get; private set; }
	public TextureRect CommanderTextureNode { get; private set; }
	public Container TopContainterNode { get; private set; }
	public HttpRequest ImageRequestNode { get; private set; }
	public FlowContainer CardsContainerNode { get; private set; }

	#endregion
	
	private ShortCard _current;
	private List<ShortCard> _cards = new();

//	private int _downloadI;
//	private int DownloadI {
//		get => _downloadI;
//		set {
//			_downloadI = value;
//			bool isDownloading = _downloadI < CardsListNode.ListNode.ItemCount;
//			DownloadProgressNode.Visible = isDownloading;
//			DownloadProgressNode.Value = value;
//			DownloadEDHRECDataButtonNode.Disabled = isDownloading;
//
//
//			if (!isDownloading) {
//				return;
//			}
//
//			var item = CardsListNode.ListNode.GetItemMetadata(value).As<Wrapper<ShortCard>>().Value;
//			var url = string.Format(EDHREC_URL, item.URLFriendlyName());
//			DownloadRequestNode.DownloadFile = Path.Combine("Data", EDH_DATA_PATH, item.OracleId + ".json");
//			DownloadRequestNode.Request(url);
//		}
//	}
	
	public override void _Ready()
	{
		#region Node fetching
		
		CardsListNode = GetNode<CardsList>("%CardsList");
		DownloadRequestNode = GetNode<HttpRequest>("%DownloadRequest");
//		DownloadProgressNode = GetNode<ProgressBar>("%DownloadProgress");
//		DownloadEDHRECDataButtonNode = GetNode<Button>("%DownloadEDHRECDataButton");
		CommanderTextureNode = GetNode<TextureRect>("%CommanderTexture");
		TopContainterNode = GetNode<Container>("%TopContainer");
		ImageRequestNode = GetNode<HttpRequest>("%ImageRequest");	
		CardsContainerNode = GetNode<FlowContainer>("%CardsContainer");

		#endregion
	}
	
	private string CommanderDataPath(ShortCard card) => Path.Combine("Data", EDH_DATA_DIR, card.OracleId + ".json");

	private void SetTexture(Texture2D tex) {
		CommanderTextureNode.Texture = tex;
	}

	private void LoadCommanderData() {
		var path = CommanderDataPath(_current);
		var data = JsonSerializer.Deserialize<EDHData>(File.ReadAllText(path));
		var failedToFind = new List<string>();
		foreach (var card in data.Cards) {
			var actualCard = GetCard(card);
			if (actualCard is null) {
				failedToFind.Add(card.Name);
				continue;
			}
			
			var child = EDHCardPS.Instantiate() as EDHCard;
			CardsContainerNode.AddChild(child);
			child.Load(actualCard, card);
		}
		if (failedToFind.Count == 0) return;
		
		var message = "Failed to find cards with the following names:";
		message += string.Join("\n\t", failedToFind);
		GUtil.Alert(this, message);
	}
	
	private void ClearCardsContainer() {
		foreach (var child in CardsContainerNode.GetChildren()) {
			child.Free();
		}	
	}

	private ShortCard? GetCard(EDHDataCard c) {
		foreach (var card in _cards)
			if (c.Name == card.Name && card.Layout != "token")
				return card;
		return null;
//		throw new Exception(message);
	}

	#region Signal connections
	
	private void _on_cards_card_added(Wrapper<ShortCard> cardW)
	{
		_cards.Add(cardW.Value);
		if (!cardW.Value.CanBeCommander()) return;
		
		CardsListNode.AddItem(cardW);

	}

//	private void _on_download_edhrec_data_button_pressed()
//	{
//		var dir = Path.Combine("Data", EDH_DATA_DIR);
//		if (!Directory.Exists(dir))
//			Directory.CreateDirectory(dir);
//		DownloadProgressNode.MaxValue = CardsListNode.ListNode.ItemCount;
//		DownloadI = 0;
//	}
//
	private void _on_download_request_request_completed(long result, long response_code, string[] headers, byte[] body)
	{
		GUtil.Alert(this, "Downloaded data for " + _current.Name);
		LoadCommanderData();
		// ++DownloadI;
	}

	private void _on_commander_texture_mouse_entered()
	{
		CreateTween().TweenProperty(TopContainterNode, "size_flags_stretch_ratio", 6f, .2f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
	}

	private void _on_commander_texture_mouse_exited()
	{
		CreateTween().TweenProperty(TopContainterNode, "size_flags_stretch_ratio", 1f, .2f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
	}

	private void _on_cards_list_card_activated(Wrapper<ShortCard> cardW)
	{
		_current = cardW.Value;
		// TODO slow?
		ImageRequestNode.CancelRequest();
		ImageRequestNode.Request(_current.GetVariations()[0].ImageURIs["normal"]);
		ClearCardsContainer();
		var dataPath = CommanderDataPath(_current);
		if (!File.Exists(dataPath)) {
			return;
		}

		LoadCommanderData();
	}

	private void _on_image_request_request_completed(long result, long response_code, string[] headers, byte[] body)
	{
		Task.Run(() => {
			var image = new Image();
			image.LoadJpgFromBuffer(body);
			var tex = ImageTexture.CreateFromImage(image);
			CallDeferred("SetTexture", tex);
		});
	}
	
	private void _on_download_data_button_pressed()
	{
		if (_current is null) {
			GUtil.Alert(this, "Select commander first");
			return;
		}

		var dir = Path.Combine("Data", EDH_DATA_DIR);
		if (!Directory.Exists(dir))
			Directory.CreateDirectory(dir);

		var url = string.Format(EDHREC_URL, _current.URLFriendlyName());
		DownloadRequestNode.DownloadFile = CommanderDataPath(_current);
		DownloadRequestNode.Request(url);
	}

	private void _on_synergy_sort_button_pressed()
	{
		var children = CardsContainerNode.GetChildren();
		var button = children[^1];
		var sorted = children.OrderBy(child => {
			var card = child as EDHCard;
			return card.Data.Synergy;
		}).ToList();
		foreach (var child in sorted)
			CardsContainerNode.MoveChild(child, 0);
		CardsContainerNode.MoveChild(button, children.Count - 1);
	}

	private void _on_type_sort_button_pressed()
	{
		var children = CardsContainerNode.GetChildren();
		var button = children[^1];
		var sorted = children.OrderBy(child => {
			var card = child as EDHCard;
			return ShortCard.TypePriority.IndexOf(card.Card.GetPrimaryType());
		}).Reverse().ToList();
		foreach (var child in sorted)
			CardsContainerNode.MoveChild(child, 0);
		CardsContainerNode.MoveChild(button, children.Count - 1);
	}

	private void _on_cards_clear_cards()
	{
		_cards = new();
	}

	#endregion
}






