using Godot;
using System;

using System.Collections.Generic;

public partial class CardViewWindow : Window
{
	private Dictionary<string, Dictionary<float, string>> _priceIndex = new() {
		{"*", new() {
			{5f, "red"},
			{3f, "orange"},
			{0f, "green"}
		}}
	};
	
	#region Nodes
	
	public TextureRect ImageRectNode { get; private set; }
	public Label NameLabelNode { get; private set; }
	public Label TextLabelNode { get; private set; }
	public HttpRequest ImageRequestNode { get; private set; }
	public RichTextLabel PricesLabelNode { get; private set; }
	
	#endregion
	
	private Texture2D _defaultBack;
	
	public override void _Ready()
	{
		#region Node fetching
		
		ImageRectNode = GetNode<TextureRect>("%ImageRect");
		NameLabelNode = GetNode<Label>("%NameLabel");
		TextLabelNode = GetNode<Label>("%TextLabel");
		ImageRequestNode = GetNode<HttpRequest>("%ImageRequest");
		PricesLabelNode = GetNode<RichTextLabel>("%PricesLabel");
		
		#endregion
		
		_defaultBack = ImageRectNode.Texture;
		
	}

	public void Load(Wrapper<MTGCard> cardW) {
		ImageRectNode.Texture = _defaultBack;
		var card = cardW.Value;
		NameLabelNode.Text = card.Name;
		TextLabelNode.Text = card.Text;
		
		PricesLabelNode.Clear();
		foreach (var pair in card.Prices) {
			var text = "-";
			if (pair.Value is not null) {
				text = pair.Value;
				var v = float.Parse(text);
				var priceIndex = GetPriceIndexOf(pair.Key);
				if (priceIndex is not null) {
					foreach (var pPair in priceIndex) {
						if (v >= pPair.Key) {
							text = "[color=" + pPair.Value + "]" + text + "[/color]";
							break;
						}
					}
				}
			}
			PricesLabelNode.AppendText(pair.Key + ": " + text + "\n");
		}
		
		ImageRequestNode.Request(card.ImageURIs["normal"]);
	}
	
	private Dictionary<float, string>? GetPriceIndexOf(string key) {
		if (_priceIndex.ContainsKey(key))
			return _priceIndex[key];
		if (_priceIndex.ContainsKey("*"))
			return _priceIndex["*"];
		return null;
	}
	
	#region Signal connections
	
	private void _on_close_requested()
	{
		Hide();
	}
	
	private void _on_image_request_request_completed(long result, long response_code, string[] headers, byte[] body)
	{
		var image = new Image();
		image.LoadJpgFromBuffer(body);
		var tex = ImageTexture.CreateFromImage(image);
		ImageRectNode.Texture = tex;
	}
	
	#endregion
}




