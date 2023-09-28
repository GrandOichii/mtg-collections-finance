using Godot;
using System;
using System.Threading.Tasks;

using System.Collections.Generic;

public partial class CardViewWindow : Window
{
	#region Nodes
	
	public TextureRect ImageRectNode { get; private set; }
	public Label NameLabelNode { get; private set; }
	public Label TextLabelNode { get; private set; }
	public HttpRequest ImageRequestNode { get; private set; }
	public RichTextLabel PricesLabelNode { get; private set; }
	public ItemList PrintingsListNode { get; private set; }
	public LineEdit SetNameFilterEditNode { get; private set; }
	
	#endregion
	
	private Texture2D _defaultBack;
	private List<Card> _variations;
	
	public override void _Ready()
	{
		#region Node fetching
		
		ImageRectNode = GetNode<TextureRect>("%ImageRect");
		NameLabelNode = GetNode<Label>("%NameLabel");
		TextLabelNode = GetNode<Label>("%TextLabel");
		ImageRequestNode = GetNode<HttpRequest>("%ImageRequest");
		PricesLabelNode = GetNode<RichTextLabel>("%PricesLabel");
		PrintingsListNode = GetNode<ItemList>("%PrintingsList");
		SetNameFilterEditNode = GetNode<LineEdit>("%SetNameFilterEdit");

		#endregion
		
		_defaultBack = ImageRectNode.Texture;
		
	}

	public void Load(Wrapper<ShortCard> cardW) {
		ImageRectNode.Texture = _defaultBack;
		var card = cardW.Value;
		NameLabelNode.Text = card.Name;
		TextLabelNode.Text = card.Text;

		PrintingsListNode.Clear();
		_variations = card.GetVariations();
		SetNameFilterEditNode.Text = "";
		_on_filter_button_pressed();
		_on_printings_list_item_activated(0);
	}

	private void SetTexture(Texture2D tex) {
		ImageRectNode.Texture = tex;
	}
	
	
	#region Signal connections
	
	private void _on_close_requested()
	{
		Hide();
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

	private void _on_printings_list_item_activated(int index)
	{
		var variation = PrintingsListNode.GetItemMetadata(index).As<Wrapper<Card>>().Value;

		PricesLabelNode.Clear();
		foreach (var pair in variation.Prices) {
			var text = "-";
			if (pair.Value is not null) {
				text = pair.Value;
				var v = Math.Round(float.Parse(text), 2);
				text = PriceUtil.GetColoredText(v, pair.Key);
			}
			PricesLabelNode.AppendText(pair.Key + ": " + text + "\n");
		}
		
		ImageRequestNode.Request(variation.ImageURIs["normal"]);
	}

	private void _on_filter_button_pressed()
	{
		PrintingsListNode.Clear();
		var filter = SetNameFilterEditNode.Text;
		foreach (var v in _variations) {
			if (!v.SetName.ToLower().Contains(filter.ToLower())) continue;
			var index = PrintingsListNode.AddItem(v.UID);
			PrintingsListNode.SetItemMetadata(index, new Wrapper<Card>(v));
		}
	}
	
	#endregion
}
