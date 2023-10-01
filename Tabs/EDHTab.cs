using Godot;
using System;
using System.Threading.Tasks;
using System.IO;

public partial class EDHTab : TabBar
{
	private readonly string EDH_DATA_PATH = "EDH";
	private readonly string EDHREC_URL = "https://json.edhrec.com/pages/commanders/{0}.json";
	
	#region Nodes
	
	public CardsList CardsListNode { get; private set; }
	public HttpRequest DownloadRequestNode { get; private set; }
//	public ProgressBar DownloadProgressNode { get; private set; }
//	public Button DownloadEDHRECDataButtonNode { get; private set; }
	public TextureRect CommanderTextureNode { get; private set; }
	public Container TopContainterNode { get; private set; }
	public HttpRequest ImageRequestNode { get; private set; }
	
	#endregion

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
		
		#endregion
	}
	
	#region Signal connections
	
	private void _on_cards_card_added(Wrapper<ShortCard> cardW, bool update)
	{
		if (!cardW.Value.CanBeCommander()) return;
		
		CardsListNode.AddItem(cardW);
	}

	private void SetTexture(Texture2D tex) {
		CommanderTextureNode.Texture = tex;
	}

//	private void _on_download_edhrec_data_button_pressed()
//	{
//		var dir = Path.Combine("Data", EDH_DATA_PATH);
//		if (!Directory.Exists(dir))
//			Directory.CreateDirectory(dir);
//		DownloadProgressNode.MaxValue = CardsListNode.ListNode.ItemCount;
//		DownloadI = 0;
//	}
//
//	private void _on_download_request_request_completed(long result, long response_code, string[] headers, byte[] body)
//	{
//		++DownloadI;
//	}

	private void _on_commander_texture_mouse_entered()
	{
		GD.Print();
		CreateTween().TweenProperty(TopContainterNode, "size_flags_stretch_ratio", 3f, .2f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
	}

	private void _on_commander_texture_mouse_exited()
	{
		CreateTween().TweenProperty(TopContainterNode, "size_flags_stretch_ratio", 1f, .2f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
	}

	private void _on_cards_list_card_activated(Wrapper<ShortCard> cardW)
	{
		// Replace with function body.
		ImageRequestNode.Request(cardW.Value.GetVariations()[0].ImageURIs["normal"]);
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
	
	#endregion
}

